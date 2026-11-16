"""The page's app bar opening and closing, from a `-Probe appbar -Record` run (#136, for #133). The shell draws the bar,
so the app reads only its events; its motion is the recording's.

Usage: python -I appbar.py <run folder> [--decode] [--ffmpeg <path>] [--frames]

The recording is read as one strip, decoded once into `<run>/analysis/left.gray` (`--decode` runs ffmpeg for it): 12 px
wide at x 2, gray, at the screen's left edge, where neither the page nor the bar draws anything but their colours. The
header band flashes white for 250 ms at the start tap: the flash pairs the recording with the app's log, as in
scrollend.py. In each frame the bar's top is the first row below the header band brighter than 12 (the dark bar is
#1F1F1F, 31, on a black page), in view px.

It prints each motion of the bar's top: a run of frames in which it changes, with the log's event nearest its first change
(a touch's press or release, a setup from code, the bar's Opened or Closed), the top on each frame and the time from the
last frame at rest, and the motion's travel as a fraction of the whole, frame by frame. For a motion over 100 px the rest
of the way falls about exponentially: it prints the time constant (a straight line through the log of the fraction to
go, from 90% to 1.5% to go), when the bar is 90% there and when it is at rest; then these over every opening and every
closing. Then, for each touch, the bar's events (`Opened`,
`Closed`) and the commands' clicks from the log. With `--frames` it prints, for each held touch, the index of the
recorded frame halfway through the hold, for ffmpeg to extract: the press is the plan's, 1.2 s after its step, as a press on
the shell's bar reaches nothing in the app.

Times are by the recording's clock (the VM's), each frame 18 to 36 ms apart, paired with the log by the flash; a
recorded frame shows what was drawn up to a frame or so before it, so each time is good to about 35 ms.

Python 3 standard library only; --decode needs ffmpeg.
"""
import math
import os
import shutil
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

STRIP = (12, 2)  # strip width and left edge, in recording px
HEADER = 60.0  # view px: the header band
DARK = 12  # a row brighter than this is the bar
HOLD = 1000.0  # ms: how long the probe holds each "hold" touch


def decode(run, exe):
    _, h = ml.recording_size(run)
    out = os.path.join(run, 'analysis')
    os.makedirs(out, exist_ok=True)
    subprocess.run([exe, '-hide_banner', '-loglevel', 'error', '-i', os.path.join(run, 'record.mkv'), '-vf',
                    'crop=%d:%d:%d:0,format=gray' % (STRIP[0], h, STRIP[1]), '-fps_mode', 'passthrough', '-f', 'rawvideo',
                    '-y', os.path.join(out, 'left.gray')], check=True)


def strip_frames(run):
    """The frames' times, the recording's size, and each frame's strip as bytes, read one at a time."""
    times = ml.frame_times(run)
    w, h = ml.recording_size(run)
    path = os.path.join(run, 'analysis', 'left.gray')
    size = STRIP[0] * h * len(times)
    if not os.path.exists(path) or os.path.getsize(path) != size:
        raise SystemExit('%s: missing or not %d bytes; run with --decode' % (path, size))

    def frames():
        with open(path, 'rb') as f:
            for _ in times:
                yield f.read(STRIP[0] * h)
    return times, w, h, frames


def row_mean(strip, y):
    return sum(strip[y * STRIP[0]:(y + 1) * STRIP[0]]) / STRIP[0]


def bar_top(strip, top, h):
    """The first row from `top` down brighter than DARK, or None."""
    return next((y for y in range(top, h) if row_mean(strip, y) > DARK), None)


def motions(series, rest_ms=150):
    """Runs of change in (ms, value) series: each a list of (ms, value) from the last frame before the change to the
    first frame of rest after it (no change for rest_ms)."""
    out, cur = [], None
    for k in range(1, len(series)):
        (ta, a), (tb, b) = series[k - 1], series[k]
        if a != b:
            if cur is None:
                cur = [series[k - 1]]
            cur.append(series[k])
        elif cur is not None and tb - cur[-1][0] >= rest_ms:
            out.append(cur)
            cur = None
    if cur is not None:
        out.append(cur)
    return out


def time_constant(m):
    """The time constant of the motion's fraction to go, ms: a straight line through its log against time, over the
    frames with between 1.5% and 90% of the way to go. NaN with fewer than two."""
    start, end = m[0][1], m[-1][1]
    pts = []
    for t, v in m:
        if v is not None:
            left = 1 - (v - start) / (end - start)
            if 0.015 < left < 0.9:
                pts.append((t, math.log(left)))
    if len(pts) < 2:
        return float('nan')
    _, b = ml.linfit([p[0] for p in pts], [p[1] for p in pts])
    return -1 / b if b < 0 else float('nan')


def main():
    (run,), opts = ml.parse_args(__doc__, options=('--ffmpeg',), flags=('--decode', '--frames'))
    if opts['--decode']:
        exe = shutil.which(opts['--ffmpeg'] or 'ffmpeg')
        if exe is None:
            raise SystemExit('ffmpeg not found; give it with --ffmpeg')
        decode(run, exe)
    times, w, h, frames = strip_frames(run)
    scale = w / ml.VIEW_W
    band = int(HEADER * scale)
    rows = ml.probe(run)
    flash = next((t for t, wh, d in rows if wh == 'flash' and d == 'on'), None)
    if flash is None:
        raise SystemExit('the log has no flash')
    # One pass: the first frame whose header band is white (the flash), and every frame's bar top after it.
    lit, tops = None, []
    for i, strip in enumerate(frames()):
        if lit is None:
            if sum(row_mean(strip, y) for y in range(5, band - 5)) / (band - 10) > 200:
                lit = i
            continue
        tops.append((i, bar_top(strip, band + 2, h)))
    if lit is None:
        raise SystemExit('no frame shows the header flash')
    lag = flash - times[lit] * 1000  # app ms - recording ms
    print('%s: %d frames; the flash pairs app time %.1f ms with recording %.3f s' % (run, len(times), flash, times[lit]))

    series = [((times[i] * 1000) + lag, round(y / scale, 2) if y is not None else None) for i, y in tops]
    events = [(t, wh, d) for t, wh, d in rows if wh in ('step', 'setup', 'pressed', 'released', 'bar', 'click')]
    print('\nThe bar\'s top (view px) in each motion; times in ms of app time, then from the last frame at rest')
    summary = []
    for m in motions(series):
        t0, first = m[0][0], m[1][0]
        # The log's event nearest the first change, before or after it: the pairing is good to about a frame.
        cause = min(events, key=lambda e: abs(e[0] - first)) if events else None
        start, end = m[0][1], m[-1][1]
        print('\n  %s to %s; the first change %+.0f ms from %s at %.0f (%s)'
              % (start, end, first - cause[0] if cause else float('nan'), cause[1] if cause else '-',
                 cause[0] if cause else float('nan'), cause[2] if cause else '-'))
        span = (end - start) if start is not None and end is not None else None
        for t, v in m:
            frac = (v - start) / span + 0.0 if span and v is not None else float('nan')
            print('    %+7.0f  %-8s %.3f' % (t - t0, v, frac))
        print('    from the first change to rest: %.0f ms' % (m[-1][0] - first))
        if span and abs(span) > 100:
            tau = time_constant(m)
            t90 = next(t for t, v in m if v is not None and (v - start) / span >= 0.9) - t0
            print('    the rest of the way falls by e every %.0f ms; 90%% there %.0f ms, at rest %.0f ms after the last frame at rest'
                  % (tau, t90, m[-1][0] - t0))
            summary.append(('opens' if end < start else 'closes', abs(span), tau, t90, m[-1][0] - t0))
    if summary:
        print('\nEach motion over 100 px with a time constant (two frames or more from 90% to 1.5% to go): travel; time')
        print('constant, 90%, at rest (ms from the last frame at rest)')
        for kind in ('opens', 'closes'):
            got = [x for x in summary if x[0] == kind and x[2] == x[2]]
            if got:
                print('  %s, %d: %s; time constants %.0f to %.0f ms, 90%% by %.0f to %.0f, at rest %.0f to %.0f'
                      % (kind, len(got), ', '.join('%.0f px' % x[1] for x in got), min(x[2] for x in got), max(x[2] for x in got),
                         min(x[3] for x in got), max(x[3] for x in got), min(x[4] for x in got), max(x[4] for x in got)))

    print('\nThe log, each touch')
    starts = {label: t for label, t, _, _ in ml.step_segments(rows, times=True)}
    for label, seg in ml.step_segments(rows):
        said = ['%s %s' % (wh, d) for t, wh, d in seg if wh in ('setup', 'bar', 'click')]
        print('  %s: %s' % (label, '; '.join(said) or '-'))
        if opts['--frames'] and label.startswith('hold'):
            # A press on the shell's bar reaches no element of the app's: the press is the plan's, 1.2 s after the step.
            press = starts[label] + 1200
            mid = press + HOLD / 2 - lag
            i = min(range(len(times)), key=lambda k: abs(times[k] * 1000 - mid))
            print('    held: frame %d, %.0f ms after the planned press' % (i, times[i] * 1000 + lag - press))


if __name__ == '__main__':
    main()
