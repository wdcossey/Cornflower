"""LoopingSelector's physics, its columns' dimming, and a press on a closed ComboBox, from a `-Probe looping` run's log
(#149).

Usage: python -I looping.py <run folder>

The probe (tools/refapp/app/Probe.Pickers.cs) opens a DatePickerFlyout and touches its three LoopingSelectors (month,
day, year: `sel0`, `sel1`, `sel2`). Every frame something changed it logs, with the frame's RenderingTime (rt, the
animation clock), each selector's ScrollViewer offset and its realised items (number@top:opacity, top relative to the
selector); and each ViewChanging, SelectionChanged and item visual state. The ScrollViewer takes the touch over, so the
release is taken from touch.log, put on the app's clock by the start tap (motionlib.touches), as in release.py.

It prints:
- the layout: each selector's box, ItemHeight, whether it loops, and the items' pitch and scale from the frames;
- for each touch: the offset at the press and at the release, the speed over the drag's last 100 ms, the first inertial
  ViewChanging (how long after the release, and its FinalView from the press in items), the motion from the release to
  rest frame by frame on the animation clock, with exponential fits to what is left to go (motionlib.print_fits), and
  where it ended in items; a recentring (the offset jumping back by whole items as the selection changes); the
  selection; and a second press during the motion, with the offset it held;
- each column's fades: runs of frames in which its unselected items' opacity moves, with their start (rt), length in
  frames and the opacity a frame, and the touch they followed;
- the closed ComboBox's press: its Background's projection while held, read as the tilt is (README "Tilt").

Python 3 standard library only.
"""
import math
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml
import scroll

FRAME = re.compile(r'rt ([\d.]+) (sel\d) offset ([-\d.]+) items ?(.*)$')
ITEM = re.compile(r'([-\d.NaN]+)@([-\d.]+):([-\d.]+)(?:x([-\d.]+))?')
COLUMN = {'month': 'sel0', 'day': 'sel1', 'year': 'sel2'}


def frames(rows):
    """Per selector, the frames: (stopwatch ms, rt, offset, [(number, top, opacity, scale)])."""
    out = {}
    for t, w, d in rows:
        if w != 'frame':
            continue
        m = FRAME.match(d)
        if not m:
            continue
        items = [(float(a), float(b), float(c), float(e) if e else 1.0) for a, b, c, e in ITEM.findall(m.group(4))]
        out.setdefault(m.group(2), []).append((t, float(m.group(1)), float(m.group(3)), items))
    return out


def pitch(fs):
    """The items' pitch: the median gap between neighbours' tops, over every frame."""
    gaps = [b[1] - a[1] for _, _, _, items in fs for a, b in zip(items, items[1:])]
    return ml.median(gaps) if gaps else float('nan')


def fades(fs):
    """Runs of frames in which a column's unselected items' opacity moves one way: [(rt, opacity)], the first the last
    frame before it moved. The unselected items' opacity is the one most of the items have (the selected item stays at
    1, and the others move together)."""
    series = []
    for _, rt, _, items in fs:
        ops = [o for _, _, o, _ in items]
        if len(ops) > 2:
            series.append((rt, max(set(ops), key=ops.count)))
    out, cur, way = [], None, 0
    for (ra, a), (rb, b) in zip(series, series[1:]):
        w = (b > a) - (b < a)
        if w and cur is not None and w == way and rb - ra < 4.5 * ml.TICK:
            cur.append((rb, b))
            continue
        if cur is not None:
            out.append(cur)
            cur = None
        if w:
            # A frame is logged only when something changed: the one before the first change held the old value.
            cur, way = [(rb - ml.TICK, a), (rb, b)], w
    if cur is not None:
        out.append(cur)
    return [c for c in out if len(c) > 2]


CURVES = (('constant deceleration (quadratic ease-out)', lambda u: 1 - (1 - u) ** 2),
          ('cubic ease-out', lambda u: 1 - (1 - u) ** 3),
          ('ExponentialEase out 2', ml.expo_out(2)), ('ExponentialEase out 4', ml.expo_out(4)),
          ('ExponentialEase out 6', ml.expo_out(6)),
          ('sine ease-in-out', lambda u: (1 - math.cos(math.pi * u)) / 2))


def best_curves(run_, fin, n=3):
    """The curves of CURVES that fit what is left to go best, from the run's first frame: (rms of the fraction,
    name, duration ms, phase in ticks), best first."""
    rt0, a = run_[0][0], run_[0][1] - fin
    if abs(a) < 1:
        return []
    ticks = [(rt - rt0) / ml.TICK for rt, _ in run_]
    fr = [(off - fin) / a for _, off in run_]
    span = max(ticks) * ml.TICK
    out = []
    for name, f in CURVES:
        e, d, ph = ml.fit_fraction(fr, f, durations=range(max(50, int(span * 0.6)), int(span * 1.6) + 50, 10),
                                   phases=[i / 4 for i in range(-8, 9)], ticks=ticks, refine=True)
        out.append((e, name, d, ph))
    return sorted(out)[:n]


def main():
    (run,), _ = ml.parse_args(__doc__)
    rows = ml.probe(run)
    fs = frames(rows)
    for t, w, d in rows:
        if w == 'layout' and d.startswith('selector') and ' item ' not in d.split(' x ')[0][:20]:
            print('layout: ' + d)
    p = {k: pitch(v) for k, v in fs.items()}
    for k in sorted(fs):
        scales = sorted({sc for _, _, _, items in fs[k] for _, _, _, sc in items})
        print('%s: items %.3f px apart (median over its frames), scale %s' % (k, p[k], ', '.join('%g' % s for s in scales)))

    played = ml.touch_times(os.path.join(run, 'touch.log'))
    touches = ml.touches(rows, played)
    segs = ml.step_segments(rows, times=True)
    print()
    for label, t0, t1, seg in segs:
        mine = [x for x in touches if x[0] >= t0 and (t1 is None or x[0] < t1)]
        col = COLUMN.get(label.split(' ')[0])
        if not mine or col is None or col not in fs:
            if label.startswith('combo'):
                combo(label, seg)
            continue
        press, release = mine[0]
        pp = p[col]
        # This column's ViewChangings: the probe ends each line with its selector's tag.
        ev = ml.view_changes([r for r in seg if r[1] == 'viewchanging' and r[2].endswith(' ' + col)])
        before = [x for x in fs[col] if x[0] <= press]
        start = before[-1][2] if before else float('nan')
        drag = [e for e in ev if not e[3] and e[0] >= press]
        inert = [e for e in ev if e[3] and e[0] >= press]
        at_rel = inert[0][1] if inert else next((e[1] for e in reversed(drag) if e[0] <= release), start)
        last = [e for e in drag if drag and e[0] >= drag[-1][0] - 100]
        speed = ml.linfit([e[0] for e in last], [e[1] for e in last])[1] if len(last) >= 3 else float('nan')
        print('%s (%s): pressed %.0f ms, held %.0f ms; offset %.3f at the press, %+.1f px (%+.2f items) at the release, '
              '%.3f px/ms over the last 100 ms' % (label, col, press, release - press, start, at_rel - start, (at_rel - start) / pp, speed))
        if inert:
            fin = inert[0][2]
            print('  first inertial ViewChanging %+.0f ms after the release: FinalView %+.1f px from the press (%+.3f items)'
                  % (inert[0][0] - release, fin - start, (fin - start) / pp))
            # The motion from the first inertial frame to rest, before any recentring jump.
            t_in = inert[0][0]
            mv = [(t, rt, off) for t, rt, off, _ in fs[col] if t >= t_in - 1 and (t1 is None or t < t1)]
            # Up to the first frame at FinalView (rest), or a frame that jumps by more than half an item (a press that
            # took the touch again, or a recentring).
            run_, jump = [], None
            for k, x in enumerate(mv):
                if run_ and abs(x[2] - run_[-1][2]) > pp / 2:
                    break
                run_.append(x)
                if abs(x[2] - fin) < 0.05:
                    if k + 1 < len(mv) and abs(mv[k + 1][2] - x[2]) > pp / 2:
                        jump = (x, mv[k + 1])
                    break
            if len(run_) >= 2:
                rt0 = run_[0][1]
                print('  %d frames from the first inertial one to rest at %+.1f px (%.0f ms of rt): %s' % (
                    len(run_), run_[-1][2] - start, run_[-1][1] - rt0,
                    ' '.join('%.0f:%+.1f' % (rt - rt0, off - fin) for _, rt, off in run_)))
                fits = [(rt, off) for _, rt, off in run_]
                peak = max(range(len(fits)), key=lambda i: abs(fits[i][1] - fin))
                if peak > 0:
                    print('  first away from the end: %+.1f px over %d frames (%.2f px a frame), then back' % (
                        fits[peak][1] - fits[0][1], peak, (fits[peak][1] - fits[0][1]) / peak))
                if abs(fin - start) > pp / 2 and peak == 0 and len(fits) > 8:
                    pts = [(rt - fits[0][0], off) for rt, off in fits]
                    cd = scroll.fit_constant_deceleration(pts)
                    v0, a, c, e, stop = scroll.fit_drag(pts)
                    if cd:
                        print('  a constant deceleration: from %.3f px/ms, %.6f px/ms^2, rms %.2f px, stopping at %.0f ms'
                              % cd)
                    print('  a deceleration a + c v^2 (scroll.py): from %.3f px/ms, a %.6f px/ms^2, c %.6f /px, rms %.2f px,'
                          ' stopping at %.0f ms' % (v0, a, c, e, stop))
                for e, name, d, ph in best_curves(fits[peak:], fin):
                    print('  %s%s over %.0f ms, its start %.2f frames before the %s, within %.4f of the way' % (
                        '' if peak == 0 else 'back: ', name, d, ph, 'first inertial frame' if peak == 0 else 'turn', e))
            if jump:
                a, b = jump
                print('  recentred at rt %.3f: the offset jumps %+.3f px (%+.3f items), the frame after rest' % (
                    b[1], b[2] - a[2], (b[2] - a[2]) / pp))
        else:
            print('  no inertial ViewChanging: %s' % ('no ViewChanging at all' if not ev else '%d ViewChanging' % len(ev)))
        sc = [d for t, w, d in seg if w == 'event' and d.startswith(col + ' SelectionChanged')]
        print('  selection: %s' % ('; '.join(s.split(' ', 1)[1] for s in sc) or 'unchanged'))
        if len(mine) > 1:
            p2, r2 = mine[1]
            during = [(t, off) for t, rt, off, _ in fs[col] if p2 <= t <= r2]
            at = max((x for x in fs[col] if x[0] <= p2), key=lambda x: x[0])
            moved = max((abs(off - at[2]) for _, off in during), default=0.0)
            print('  a second press %.0f ms after the release, held %.0f ms: offset %.3f at it, moved %.1f px while held' % (
                p2 - release, r2 - p2, at[2], moved))
    print()
    for k in sorted(fs):
        for run_ in fades(fs[k]):
            rt0 = run_[0][0]
            vals = [o for _, o in run_]
            n = (run_[-1][0] - rt0) / ml.TICK
            a, b = ml.linfit([(rt - rt0) / ml.TICK for rt, _ in run_], vals)
            worst = max(abs(a + b * (rt - rt0) / ml.TICK - o) for rt, o in run_)
            press = max((t for t in (x[0] for x in touches) if t <= next(t for t, r, _, _ in fs[k] if r >= rt0)), default=None)
            print('%s fade from rt %.3f: %.3f to %.3f over %.0f frames (%.0f ms), %+.4f a frame, within %.4f of a straight '
                  'line; the last press %s' % (k, rt0, vals[0], vals[-1], n, n * ml.TICK, b, worst,
                                               '-' if press is None else '%.0f ms before' % (next(t for t, r, _, _ in fs[k] if r >= rt0) - press)))


def combo(label, seg):
    """The closed ComboBox's press: its Background's projection, from the tree lines."""
    proj = [(t, d) for t, w, d in seg if w == 'tree' and 'Border#Background' in d and 'projection' in d]
    pressed = next((t for t, w, d in seg if w == 'pressed'), None)
    print('%s: pressed at %s ms' % (label, '-' if pressed is None else '%.0f' % pressed))
    for t, d in proj:
        m = re.search(r'projection rot ([-\d.]+) ([-\d.]+) ([-\d.]+) .* global [-\d.]+ [-\d.]+ ([-\d.]+)', d)
        if m:
            print('  %+6.0f ms  RotationX %s RotationY %s GlobalOffsetZ %s' % (t - (pressed or 0), m.group(1), m.group(2), m.group(4)))
    for t, w, d in seg:
        if w in ('state', 'event') or (w == 'tree' and ('ComboBox#' in d or 'ItemsPresenter' in d)):
            print('  %+6.0f ms  %s %s' % (t - (pressed or 0), w, d))


if __name__ == '__main__':
    main()
