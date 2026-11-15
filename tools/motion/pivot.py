"""Pivot's header strip and Hub's header against their content, from a `-Probe pivot` or `-Probe hub` run's log (#114).

Usage: python -I pivot.py <run folder>

The probes (tools/refapp/app/Probe.Shell.cs) log, every frame something changed, the frame's RenderingTime (rt, the
animation clock, 15.625 ms a step) with the control's ScrollViewer's HorizontalOffset and the parts that move. The
offset is DirectManipulation's as the app reads it. A recording of the same run (20261115-2056-dev11) showed the same
positions, the screen about a frame ahead of the log. Each touch's `step` line starts its segment.

For a Pivot it prints, for each touch:
- when the selection changed, in ms from the press or the tap's release (the stopwatch);
- the offset's travel and the rt from the press to its end;
- the header strip against the offset: a straight line through the left edge of the header selected at the start
  against the offset, over the frames before that header jumps (the headers are put in a new order, the selected
  one first, once the new item is chosen). Its slope (header px per
  content px) is printed with its worst residual, beside that header's pitch (the next header's left edge less its own,
  at rest) over the page's width (450);
- the new item's slide in: from the frame, at or after the selection change, where the ItemsPresenter's translate
  jumps to put the new item beside the old (after a header tap the old item first goes out with the offset), the
  translate less the final offset, one value a frame on rt, as a fraction of its first; its ratio a tick, and when it reaches 0.
Then the slide-in's values over every touch, each counted once, with motionlib's exponential and ExponentialEase fits.

For a Hub: the header's left edge against the offset, a straight line through every frame with the offset past 20 px
(at the start the Hub's entrance animation still moves it), its slope and worst residual.

Python 3 standard library only.
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

PIVOT = re.compile(r'rt ([\d.]+) offset ([-\d.]+) index (\d+) panel (\S+|\[[^\]]*\]) items \[translate ([-\d.]+) [-\d.]+\] itemsAt ([-\d.]+)(.*)$')
HEADER = re.compile(r'\| (\S+) ([-\d.]+|NaN) op')
HUB = re.compile(r'rt ([\d.]+) offset ([-\d.]+) header ([-\d.]+)')


def linfit(xs, ys):
    n = len(xs)
    mx, my = sum(xs) / n, sum(ys) / n
    sxx = sum((x - mx) ** 2 for x in xs)
    b = sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / sxx if sxx else 0.0
    return my - b * mx, b


def pivot_frames(seg):
    """(stopwatch ms, rt, offset, index, panel text, items translate, {header: left edge}) for each frame line."""
    out = []
    for t, what, d in seg:
        if what != 'frame':
            continue
        m = PIVOT.match(d)
        if m:
            heads = {h: float(x) for h, x in HEADER.findall(m.group(7))}
            out.append((t, float(m.group(1)), float(m.group(2)), int(m.group(3)), m.group(4), float(m.group(5)), heads))
    return out


def pivot(rows, segs):
    names = None
    slides = []
    for label, seg in segs:
        fr = pivot_frames(seg)
        press = next((t for t, w, d in seg if w == 'pressed'), None)
        release = next((t for t, w, d in seg if w in ('released', 'capturelost') and press is not None and t >= press), None)
        sel = next((t for t, w, d in seg if w == 'selection' and press is not None and t >= press), None)
        moving = [f for f in fr if press is not None and f[0] >= press]
        if not fr or not moving:
            continue
        print('\n%s' % label)
        rest = [f for f in fr if press is None or f[0] < press]
        start = rest[-1] if rest else fr[0]
        end = fr[-1]
        if names is None:
            names = list(start[6].keys())
        print('  pressed at %.1f; %s at %+.1f; selection %s at %+.1f ms (stopwatch); offset %+.2f, to its end at rt %+.1f from the first frame after the press'
              % (press, 'released' if release is not None else '-', (release - press) if release is not None else float('nan'),
                 end[3], (sel - press) if sel is not None else float('nan'), end[2] - start[2], end[1] - moving[0][1]))
        # The header selected at the start, against the offset, until the panel is laid out again.
        h = names[start[3]] if start[3] < len(names) else None
        # The header items are put in a new order once the new item is chosen, the selected one first: the frames
        # before this header's left edge jumps (by over 100 px).
        span = []
        for f in [start] + moving:
            if f[4] != start[4] or h not in f[6] or (span and abs(f[6][h] - span[-1][6][h]) > 100):
                break
            span.append(f)
        if h is not None and len(span) >= 3:
            xs = [f[2] - start[2] for f in span]
            ys = [f[6][h] for f in span]
            a, b = linfit(xs, ys)
            worst = max(abs(y - (a + b * x)) for x, y in zip(xs, ys))
            nxt = names[(start[3] + 1) % len(names)]
            pitch = start[6][nxt] - start[6][h] if start[3] + 1 < len(names) else float('nan')
            print('  header "%s" moves %.5f px a px of offset, against it, over %d frames (worst %.3f px); its pitch %.3f over 450 is %.5f'
                  % (h, -b, len(span), worst, pitch, pitch / 450 if pitch == pitch else float('nan')))
        # The new item's slide in, from the selection change.
        after = [f for f in moving if sel is not None and f[0] >= sel]
        if after:
            final = end[2]
            # The new item's slide starts where the ItemsPresenter's translate jumps (by over 300 px) to put it beside
            # the old one; after a header tap the old item first goes out with the offset, frames before that jump.
            jumps = [f for prev, f in zip(moving, moving[1:]) if f[0] >= sel and abs(f[5] - prev[5]) > 300]
            if jumps:
                after = [f for f in after if f[0] >= jumps[0][0]]
            first = after[0][5] - final
            vals = [(f[1] - after[0][1], (f[5] - final) / first) for f in after if first]
            vals = vals[:next((i for i, v in enumerate(vals) if v[1] <= 0), len(vals) - 1) + 1]
            ratios = [(b / a) ** (ml.TICK / (tb - ta)) for (ta, a), (tb, b) in zip(vals, vals[1:]) if a > 0.05 and b > 0 and tb > ta]
            print('  the new item slides in from %.2f px: at 0 after %.1f ms of rt; %s; a tick: %s'
                  % (first, vals[-1][0], ' '.join('%.0f:%.4f' % v for v in vals[:8]) + ' ...',
                     'median %.4f' % ml.median(ratios) if ratios else '-'))
            slides.extend(v for _, v in vals if v > 0.001)
    if slides:
        vals = ml.distinct(slides, decimals=3, tol=0.002, min_abs=0.002)
        vals = sorted(vals, reverse=True)
        r0, ticks = ml.tick_numbers(vals)
        print('\nThe slide in over every touch: %d distinct fractions, %s' % (len(vals), ' '.join('%.3f' % v for v in vals[:12])))
        print('  ticks %s (median ratio %.4f)' % (ticks[:24], r0))
        ml.print_fits(vals, ticks, (('ExponentialEase 6', ml.expo_out(6)), ('ExponentialEase 4', ml.expo_out(4))))


def hub(rows):
    pts = []
    for t, what, d in rows:
        if what == 'frame':
            m = HUB.match(d)
            if m:
                pts.append((float(m.group(2)), float(m.group(3))))
    use = [p for p in pts if p[0] > 20]
    if len(use) < 3:
        raise SystemExit('too few frames with the offset past 20 px')
    a, b = linfit([p[0] for p in use], [p[1] for p in use])
    worst = max(abs(y - (a + b * x)) for x, y in use)
    print('Hub: header = %.4f %+.6f x offset over %d frames, offsets %.0f to %.0f; worst %.4f px; 1 / slope = %.3f'
          % (a, b, len(use), min(p[0] for p in use), max(p[0] for p in use), worst, -1 / b if b else float('inf')))


def main():
    (run,), _ = ml.parse_args(__doc__)
    rows = ml.probe(run)
    if any(w == 'frame' and ' index ' in d for t, w, d in rows):
        pivot(rows, ml.step_segments(rows))
    else:
        hub(rows)


if __name__ == '__main__':
    main()
