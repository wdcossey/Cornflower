"""The ScrollViewer's end-of-content compression and its panning indicator, from a `-Probe scrollend -Record` run
(#113, gap register B8). DirectManipulation draws both: the app's values show neither, so they are read off the
recording.

Usage: python -I scrollend.py <run folder> [--decode] [--ffmpeg <path>]

The recording is read as two strips, decoded once into `<run>/analysis/` (`--decode` runs ffmpeg for them):
- `bars.gray`: 12 px wide at x 150, gray. It crosses the content's bars (grey, 8 view px tall, every 100 view px) and the
  header band, which flashes white for 250 ms at the start tap: the flash pairs the recording with the app's log.
- `edge.rgb`: the right 24 px, colour, where the indicator is drawn.
The 2026-11-15 run's strips were decoded on the reference VM with the commands `--decode` runs.

For each pull and flick (the log's `step` lines) it prints, frame by frame where it changes, the bars' spacing over
their rest spacing (the compression, s) and how far the first and last bar seen have moved, in view px; and the most
compressed frame, the point the content is scaled about (the bars' map from rest, y = f + s (y_rest - f), solved for f,
in view px past the pulled edge) and when it is back at rest. The bars repeat every 100 view px, so "at rest" is the
bars back where they were before the press, which an offset a whole number of bars away reads as too: the probe's
flicks end a whole number of bars from where they start (600 px). For each time the indicator shows: its columns and rows
(its width and length in view px), its brightest value (its colour over the page), and its brightness frame by frame as
it fades in and out, with the ScrollBar's `ScrollingIndicatorStates` changes from the log beside them.

Times are by the recording's clock (the VM's), each frame about 33 ms apart, paired with the log by the flash; a
recorded frame shows what was drawn up to a frame or so before it, so each time here is good to about 35 ms.

Python 3 standard library only; --decode needs ffmpeg.
"""
import os
import shutil
import statistics
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

BARS = (12, 150)  # strip width and left edge, in recording px
EDGE = 24  # the right strip's width
HEADER = 60.0  # view px: the header band, above the ScrollViewer
PITCH = 100.0  # view px between the bars


def decode(run, exe):
    w, h = ml.recording_size(run)
    out = os.path.join(run, 'analysis')
    os.makedirs(out, exist_ok=True)
    for vf, fmt, name in (('crop=%d:%d:%d:0,format=gray' % (BARS[0], h, BARS[1]), 'rawvideo', 'bars.gray'),
                          ('crop=%d:%d:%d:0,format=rgb24' % (EDGE, h, w - EDGE), 'rawvideo', 'edge.rgb')):
        subprocess.run([exe, '-hide_banner', '-loglevel', 'error', '-i', os.path.join(run, 'record.mkv'), '-vf', vf,
                        '-fps_mode', 'passthrough', '-f', fmt, '-y', os.path.join(out, name)], check=True)


class Strips:
    def __init__(self, run):
        self.times = ml.frame_times(run)
        self.w, self.h = ml.recording_size(run)
        self.scale = self.w / ml.VIEW_W  # recording px a view px
        n = len(self.times)
        self.bars_path = os.path.join(run, 'analysis', 'bars.gray')
        self.edge_path = os.path.join(run, 'analysis', 'edge.rgb')
        for path, size in ((self.bars_path, BARS[0] * self.h * n), (self.edge_path, EDGE * self.h * 3 * n)):
            if not os.path.exists(path) or os.path.getsize(path) != size:
                raise SystemExit('%s: missing or not %d bytes; run with --decode' % (path, size))
        self._b = open(self.bars_path, 'rb')
        self._e = open(self.edge_path, 'rb')

    def profile(self, i):
        """The bars strip's rows, each the mean of its pixels."""
        w = BARS[0]
        self._b.seek(i * w * self.h)
        b = self._b.read(w * self.h)
        return [sum(b[y * w:(y + 1) * w]) / w for y in range(self.h)]

    def edge(self, i):
        self._e.seek(i * EDGE * self.h * 3)
        return self._e.read(EDGE * self.h * 3)


def bars(profile, top, thr=50):
    """The bars' centres (brightness-weighted) from row `top` down, in recording px."""
    out, y, n = [], int(top), len(profile)
    while y < n:
        if profile[y] > thr:
            s = y
            while y < n and profile[y] > thr:
                y += 1
            w = sum(profile[s:y])
            out.append(sum(k * profile[k] for k in range(s, y)) / w)
        y += 1
    return out


def flash_time(st):
    """The recording's time of the first frame whose header band is white."""
    top = int(HEADER * st.scale)
    for i, t in enumerate(st.times):
        p = st.profile(i)
        if sum(p[5:top - 5]) / (top - 10) > 200:
            return t
    raise SystemExit('no frame shows the header flash')


def compression(bs, scale):
    """(s, first, last) of a frame's bars: the median spacing over the rest spacing, and the first and last centre."""
    if len(bs) < 3:
        return None
    sp = statistics.median([b - a for a, b in zip(bs, bs[1:])])
    return sp / (PITCH * scale), bs[0], bs[-1]


def fixed_point(rest, now, s):
    """The point f the content is scaled about: now = f + s (rest - f)."""
    return (now - s * rest) / (1 - s) if s < 1 else float('nan')


def indicator(st, i):
    """The indicator in a frame of the edge strip: (first row, last row, columns, brightest-or-darkest value, light), or
    None. The page's own colour is the strip's first column, which the indicator never reaches: on a dark page the
    indicator is brighter than 20, on a light one darker than 240. Judged a frame at a time, from its pixels, as the page
    turns light a frame or more after the app's step."""
    b = st.edge(i)
    top = int(HEADER * st.scale)
    light = b[(top * EDGE) * 3] > 128
    ys, xs, best = [], set(), None
    for y in range(top, st.h):
        row = b[y * EDGE * 3:(y + 1) * EDGE * 3:3]  # the red channel; the strip is grey
        v = min(row) if light else max(row)
        if (v < 240) if light else (v > 20):
            ys.append(y)
            xs.update(x for x, c in enumerate(row) if ((c < 240) if light else (c > 20)))
            best = v if best is None else (min(best, v) if light else max(best, v))
    if not ys:
        return None
    return ys[0], ys[-1], sorted(xs), best, light


def main():
    (run,), opts = ml.parse_args(__doc__, options=('--ffmpeg',), flags=('--decode',))
    if opts['--decode']:
        exe = shutil.which(opts['--ffmpeg'] or 'ffmpeg')
        if exe is None:
            raise SystemExit('ffmpeg not found; give it with --ffmpeg')
        decode(run, exe)
    st = Strips(run)
    rows = ml.probe(run)
    flash = next((t for t, w, d in rows if w == 'flash' and d == 'on'), None)
    if flash is None:
        raise SystemExit('the log has no flash')
    lag = flash / 1000 - flash_time(st)  # app s - recording s
    print('%s: %d frames, %.2f recording px a view px; the flash pairs app time %.1f ms with recording %.3f s'
          % (run, len(st.times), st.scale, flash, flash / 1000 - lag))
    # The touches' steps; the first runs logged a snapshot taken during a touch as a step too (as step_segments).
    steps = [(t, d) for t, w, d in rows if w == 'step' and not d.startswith('snapshot ')]
    ends = [t for t, _ in steps[1:]] + [rows[-1][0] + 1000]
    states = [(t, d.split()[-1]) for t, w, d in rows if w == 'state' and d.startswith('VerticalScrollBar ScrollingIndicatorStates')]
    top = HEADER * st.scale

    print('\nCompression (s: the bars\' spacing over their rest spacing; moves in view px; times in ms from the press)')
    for (t0, label), t1 in zip(steps, ends):
        if not (label.startswith('pull') or label.startswith('flick')):
            continue
        press = next((t for t, w, d in rows if w == 'pressed' and t0 < t < t1), None)
        if press is None:
            continue
        idx = [i for i, t in enumerate(st.times) if t0 / 1000 - lag <= t < t1 / 1000 - lag]
        rest = None
        series, last = [], None
        for i in idx:
            c = compression(bars(st.profile(i), top), st.scale)
            if c is None:
                continue
            tm = (st.times[i] + lag) * 1000 - press
            if tm < 0:
                rest = c
            key = (round(c[0], 3), round(c[1]), round(c[2]))
            if key != last:
                series.append((tm, c))
                last = key
        if rest is None:
            continue
        print('\n%s' % label)
        for tm, (s, f, l) in series:
            if tm >= 0:
                print('  %+7.0f  s %.3f  first %+6.1f  last %+6.1f' % (tm, s, (f - rest[1]) / st.scale, (l - rest[2]) / st.scale))
        moving = [(tm, c) for tm, c in series if tm >= 0]
        if not moving:
            continue
        tm, (s, f, l) = min(moving, key=lambda x: x[1][0])
        # The bar nearest the pulled edge keeps its index: the first one past the top, the last one past the bottom.
        at_top = 'top' in label
        r, n = (rest[1], f) if at_top else (rest[2], l)
        edge = top if at_top else st.h
        fp = fixed_point(r, n, s)
        back = next((t for t, c in moving if t > tm and abs(c[0] - 1) < 0.0005 and abs(c[1] - rest[1]) < 0.5), None)
        print('  most compressed at %+.0f: s %.3f, the edge bar moved %.1f view px; scaled about %.0f view px past the pulled edge; at rest at %s'
              % (tm, s, (n - r) / st.scale, abs(fp - edge) / st.scale, '%+.0f' % back if back is not None else '-'))

    print('\nThe indicator (brightness in the edge strip; times in ms from its first frame; the ScrollBar\'s states by the log)')
    ep, cur = [], None
    for i, t in enumerate(st.times):
        app = (t + lag) * 1000
        r = indicator(st, i)
        if r and cur is not None and r[4] != cur[-1][2]:
            # The page changed theme mid-episode: a new one.
            ep.append(cur)
            cur = None
        if r and cur is None:
            cur = [(app, r, r[4])]
        elif r:
            cur.append((app, r, r[4]))
        elif cur is not None:
            ep.append(cur)
            cur = None
    if cur:
        ep.append(cur)
    for e in ep:
        a, z = e[0][0], e[-1][0]
        light = e[0][2]
        mid = e[len(e) // 2][1]
        vals = [r[3] for _, r, _ in e]
        near = ', '.join('%s %+.0f' % (s, t - a) for t, s in states if a - 400 < t < z + 300)
        cols = mid[2]
        print('\n  at %.0f ms (%s): %d frames, columns %d-%d of the strip (%.2f view px wide, its right edge %.2f view px from the screen\'s), rows %d-%d (%.1f view px long); %s %d'
              % (a, 'light' if light else 'dark', len(e), cols[0], cols[-1], len(cols) / st.scale, (EDGE - 1 - cols[-1]) / st.scale,
                 mid[0], mid[1], (mid[1] - mid[0] + 1) / st.scale, 'darkest' if light else 'brightest', min(vals) if light else max(vals)))
        print('    states: %s' % near)
        print('    %s' % '  '.join('%+.0f:%d' % (t - a, r[3]) for t, r, _ in e))


if __name__ == '__main__':
    main()
