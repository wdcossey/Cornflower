"""Pivot's header strip and Hub's header against their content, from a `-Probe pivot`, `pivotwide` or `pivottwo` run's
log, or a `-Probe hub`, `hubimage` or `hubimage900` one (#114, #136).

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
(at the start the Hub's entrance animation still moves it), its slope and worst residual. The touches at the end (#136,
the touches whose setup sets the offset from code to anything but 0) are left out of that line, and printed on their own: the offset, the header and, with an
image, the background, frame by frame, and whether the offset goes back to 0. With a background image (`-Probe
hubimage`), the background's left edge against the offset, as two straight lines: up to the sections' width less the
view's (L - V), beside (W - V) / (L - V), W the background's width; and from there to the end of the scroll range.

Python 3 standard library only.
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

PIVOT = re.compile(r'rt ([\d.]+) offset ([-\d.]+) index (-?\d+) panel (.*?) items (.*?) itemsAt ([-\d.]+|NaN)( \|.*)?$')
TRANSLATE = re.compile(r'^\[translate ([-\d.]+) [-\d.]+\]$')
HEADER = re.compile(r'\| (\S+) ([-\d.]+|NaN) op')
HUB = re.compile(r'rt ([\d.]+) offset ([-\d.]+) header ([-\d.]+)')
BACKGROUND = re.compile(r'\| Background ([-\d.]+) w ([\d.]+) ')


def pivot_frames(seg):
    """(stopwatch ms, rt, offset, index, panel text, items translate, {header: left edge}) for each frame line. The items'
    translate is 0 when the log has "none" (no transform, or at rest), and NaN for any other kind. A frame line that
    doesn't parse stops the script, naming it."""
    out = []
    for t, what, d in seg:
        if what != 'frame':
            continue
        m = PIVOT.match(d)
        if not m:
            raise SystemExit('a frame line this script can\'t read, at %.1f ms: %s' % (t, d))
        tr = TRANSLATE.match(m.group(5))
        items = float(tr.group(1)) if tr else 0.0 if m.group(5) == 'none' else float('nan')
        heads = {h: float(x) for h, x in HEADER.findall(m.group(7) or '')}
        out.append((t, float(m.group(1)), float(m.group(2)), int(m.group(3)), m.group(4), items, heads))
    return out


def pivot(rows, segs):
    # The items' headers in their order, which SelectedIndex counts in: the probe's "layout items" line, or, in the
    # first runs, the "layout header" lines, logged in the panel's order at rest with item 0 selected.
    listed = next((d.split()[1:] for t, w, d in rows if w == 'layout' and d.startswith('items ') and ' scroll ' not in d), None)
    names = listed or [d.split()[1] for t, w, d in rows if w == 'layout' and d.startswith('header ')]
    slides = []
    first_step = next((t for t, w, d in rows if w == 'step'), None)
    before = pivot_frames([r for r in rows if first_step is None or r[0] < first_step])
    last = before[-1] if before else None
    for label, seg in segs:
        fr = pivot_frames(seg)
        press = next((t for t, w, d in seg if w == 'pressed'), None)
        release = next((t for t, w, d in seg if w in ('released', 'capturelost') and press is not None and t >= press), None)
        sel = next((t for t, w, d in seg if w == 'selection' and press is not None and t >= press), None)
        moving = [f for f in fr if press is not None and f[0] >= press]
        if not fr or not moving:
            continue
        print('\n%s' % label)
        # A frame is logged only when something changed, so between touches there is often none: the last frame before
        # the press is then the last one logged before this segment.
        rest = [f for f in fr if press is None or f[0] < press]
        start = rest[-1] if rest else last if last is not None else fr[0]
        last = fr[-1]
        end = fr[-1]
        print('  pressed at %.1f; %s at %+.1f; selection %s at %+.1f ms (stopwatch); offset %+.2f, to its end at rt %+.1f from the first frame after the press'
              % (press, 'released' if release is not None else '-', (release - press) if release is not None else float('nan'),
                 end[3], (sel - press) if sel is not None else float('nan'), end[2] - start[2], end[1] - moving[0][1]))
        # The header selected at the start, against the offset, until the panel is laid out again.
        h = names[start[3]] if start[3] < len(names) else None
        # The header items are put in a new order once the new item is chosen, the selected one first: the frames
        # before this header's left edge jumps (by over 100 px) the way the offset went, or with the offset still.
        # Against the offset it can move faster than that: a header wider than the screen moves 1.65 px a px (#136).
        span = []
        for f in [start] + [f for f in moving if f is not start]:
            if f[4] != start[4] or h not in f[6]:
                break
            if span:
                dh, doff = f[6][h] - span[-1][6][h], f[2] - span[-1][2]
                if abs(dh) > 100 and dh * doff >= 0:
                    break
            span.append(f)
        if h is not None and len(span) >= 3:
            xs = [f[2] - start[2] for f in span]
            ys = [f[6][h] for f in span]
            a, b = ml.linfit(xs, ys)
            worst = max(abs(y - (a + b * x)) for x, y in zip(xs, ys))
            # The pitch to the next header at rest: the nearest left edge after this one's. The panel puts the selected
            # header first and the rest after it in order, wrapping, so the last item's next is the first (#136).
            after = [x for k, x in start[6].items() if k != h and x > start[6][h]]
            pitch = min(after) - start[6][h] if after else float('nan')
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
            if not vals or vals[0][1] != vals[0][1]:
                print('  no slide in read: the translate is at the final offset from the selection on, or is not a translate')
                continue
            reached = vals[-1][1] <= 0
            ratios = [(b / a) ** (ml.TICK / (tb - ta)) for (ta, a), (tb, b) in zip(vals, vals[1:]) if a > 0.05 and b > 0 and tb > ta]
            print('  the new item slides in from %.2f px: %s %.1f ms of rt; %s; a tick: %s'
                  % (first, 'at 0 after' if reached else 'not at 0 by the end of the log,', vals[-1][0], ' '.join('%.0f:%.4f' % v for v in vals[:8]) + ' ...',
                     'median %.4f' % ml.median(ratios) if ratios else '-'))
            slides.extend(v for _, v in vals if v > 0.001)
    if slides:
        vals = ml.distinct(slides, decimals=3, tol=0.002, min_abs=0.002)
        r0, ticks = ml.tick_numbers(vals)
        print('\nThe slide in over every touch: %d distinct fractions, %s' % (len(vals), ' '.join('%.3f' % v for v in vals[:12])))
        print('  ticks %s (median ratio %.4f)' % (ticks[:24], r0))
        ml.print_fits(vals, ticks, (('ExponentialEase 6', ml.expo_out(6)), ('ExponentialEase 4', ml.expo_out(4))))


def hub(rows):
    # The touches at the end: their setup sets the offset from code to the end of the scroll range, not to 0.
    end_steps = [(label, t0, t1) for label, t0, t1, seg in ml.step_segments(rows, times=True)
                 if any(w == 'setup' and d.startswith('offset ') and float(d.split()[1]) > 0 for t, w, d in seg)]
    first_end = min((t0 for _, t0, _ in end_steps), default=float('inf'))
    pts = []
    for t, what, d in rows:
        if what == 'frame':
            m = HUB.match(d)
            if m:
                bg = BACKGROUND.search(d)
                pts.append((t, float(m.group(2)), float(m.group(3)), float(bg.group(1)) if bg else None, float(bg.group(2)) if bg else None))
    use = [p for p in pts if p[1] > 20 and p[0] < first_end]
    if len(use) < 3:
        raise SystemExit('too few frames with the offset past 20 px')
    a, b = ml.linfit([p[1] for p in use], [p[2] for p in use])
    worst = max(abs(p[2] - (a + b * p[1])) for p in use)
    print('Hub: header = %.4f %+.6f x offset over %d frames, offsets %.0f to %.0f; worst %.4f px; 1 / slope = %.3f'
          % (a, b, len(use), min(p[1] for p in use), max(p[1] for p in use), worst, -1 / b if b else float('inf')))

    widths = [float(d.split()[-2]) for t, w, d in rows if w == 'layout' and d.startswith('section ')]
    view = next((float(d.split()[-1]) for t, w, d in rows if w == 'layout' and ' viewport ' in d), None)
    bgs = [p for p in use if p[3] is not None]
    if bgs and widths and view:
        width, length = bgs[0][4], sum(widths)
        knee = length - view
        print('Background: %.0f px wide; the sections %.0f px in all (L), the view %.0f (V); (W - V) / (L - V) = %.6f'
              % (width, length, view, (width - view) / knee))
        for name, part in (('offsets up to L - V', [p for p in bgs if p[1] < knee - 1]), ('offsets past L - V', [p for p in bgs if p[1] > knee + 1])):
            if len(part) >= 3:
                a, b = ml.linfit([p[1] for p in part], [p[3] for p in part])
                worst = max(abs(p[3] - (a + b * p[1])) for p in part)
                print('  %s: left edge = %.3f %+.6f x offset over %d frames, offsets %.0f to %.0f; worst %.3f px'
                      % (name, a, b, len(part), min(p[1] for p in part), max(p[1] for p in part), worst))
    for label, t0, t1 in end_steps:
        seg = [p for p in pts if p[0] >= t0 and (t1 is None or p[0] < t1)]
        if not seg:
            continue
        back = next((k for k in range(1, len(seg)) if seg[k][1] < 1 and seg[k - 1][1] > 100), None)
        print('\n%s: offset %.1f at the start; %s' % (label, seg[0][1], 'back to 0 at %.0f ms' % (seg[back][0] - t0) if back is not None else 'never back to 0'))
        for p in seg[max(0, (back or 0) - 14):(back or 0) + 16]:
            print('  %8.1f  offset %8.2f  header %8.2f%s' % (p[0] - t0, p[1], p[2], '  background %9.2f' % p[3] if p[3] is not None else ''))


def main():
    (run,), _ = ml.parse_args(__doc__)
    rows = ml.probe(run)
    if any(w == 'frame' and ' index ' in d for t, w, d in rows):
        pivot(rows, ml.step_segments(rows))
    else:
        hub(rows)


if __name__ == '__main__':
    main()
