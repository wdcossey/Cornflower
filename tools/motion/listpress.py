"""A ListView item's press, frame by frame, from a `-Probe listpress` run's log (#113, gap register B8).

Usage: python -I listpress.py <run folder>

The probe (tools/refapp/app/Probe.Scroll.cs, ListPress) lays out a ListView of 40 items (12 in its first run,
20261115-1941-dev11, which couldn't pan) with IsItemClickEnabled and
watches the first seven items' `TiltContainer` (the template's `Pressed` state runs a `PointerDownThemeAnimation` on
it): its projection and transform every frame they change, and each item's visual states. Each touch's `step` line,
1.2 s before its press, starts its segment. For each touch it prints, in ms by the app's stopwatch from the press:

- the item pressed (the one whose state goes to `Pressed` with the press), its state changes, ItemClick, SelectionChanged and the gesture events, and the release (or capture lost);
- the first and the last frame the projection changes, and the projection held 120 ms after the press (or at the
  release, if that is sooner): RotationX,
  RotationY and GlobalOffsetZ, beside what the Button's tilt model (README, "Tilt: the model") gives for the press
  point: g(d) = 17.19 x d x (1 + 0.08 |d|) degrees, and depth -25 x (1 - (|rx| + |ry|) / 17.19);
- after the release, each logged projection as a fraction of the held one, and when it is back at rest.

Python 3 standard library only.
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

PROJ = re.compile(r'projection (item\d+) (?:rot ([-\d.e]+) ([-\d.e]+) ([-\d.e]+) centre .* global ([-\d.e]+) ([-\d.e]+) ([-\d.e]+)|(none|\w+))$')
MAX = 17.19


def g(d):
    """The Button's tilt angle for a press at fraction d from the centre (README, "Tilt: the model"; tiltcurve.py fits
    it, and its g is this one)."""
    return MAX * d * (1 + 0.08 * abs(d))


def layout(rows):
    """{item: (x, y, w, h)} of each watched item's TiltContainer in the window."""
    out = {}
    for t, what, d in rows:
        if what == 'layout' and d.startswith('item') and ' tilt ' in d:
            name, rest = d.split(' ', 1)
            out[name] = tuple(float(v) for v in rest.split(' tilt ')[1].split())
    return out


def projection(d):
    """(item, (rx, ry, gz)) of a projection line, (item, None) for none or rest."""
    m = PROJ.match(d)
    if not m:
        return None, None
    if m.group(2) is None:
        return m.group(1), None
    return m.group(1), (float(m.group(2)), float(m.group(3)), float(m.group(7)))


def main():
    (run,), _ = ml.parse_args(__doc__)
    rows = ml.probe(run)
    boxes = layout(rows)
    print('%s: TiltContainers %s' % (run, ', '.join('%s %s' % (k, ' '.join('%g' % v for v in b)) for k, b in sorted(boxes.items()))))
    for label, seg in ml.step_segments(rows):
        press = next((r for r in seg if r[1] == 'pressed'), None)
        print('\n%s' % label)
        if press is None:
            print('  no press')
            continue
        t0 = press[0]
        p = press[2].split()
        px, py = float(p[1]), float(p[2])
        end = next((r for r in seg if r[0] > t0 and r[1] in ('released', 'capturelost', 'canceled')), None)
        print('  pressed at %.1f, %.1f%s' % (px, py, '; %s at %+.1f ms' % (end[1], end[0] - t0) if end else ''))
        for t, what, d in seg:
            if what in ('state', 'itemclick', 'selection', 'gesture') or (what == 'released' and t != t0):
                print('  %+8.1f ms  %s %s' % (t - t0, what, d))
        # The item is the one whose state went to Pressed with this press. A touch's segment starts 1.2 s before its
        # press, so it can hold the end of the last touch's return, on another item: only what follows the press counts.
        pressed = next((d.split()[0] for t, what, d in seg if what == 'state' and t >= t0 and d.endswith('> Pressed')), None)
        if pressed is None:
            print('  no item went to Pressed')
            continue
        item = pressed
        mine = [(t, v) for t, i, v in ((t, ) + projection(d) for t, what, d in seg if what == 'value' and d.startswith('projection'))
                if i == item and t >= t0]
        if not mine:
            print('  %s: no projection changed' % item)
            continue
        print('  %s: projection first changes at %+.1f ms, last at %+.1f ms, %d changes'
              % (item, mine[0][0] - t0, mine[-1][0] - t0, len(mine)))
        rel = end[0] if end else None
        # Held: the last value by 120 ms after the press (or by the release, if sooner), before any move changes it.
        upto = t0 + 120 if rel is None else min(t0 + 120, rel)
        held = [v for t, v in mine if v is not None and t <= upto]
        if held:
            rx, ry, gz = held[-1]
            line = '  held: RotationX %.2f, RotationY %.2f, GlobalOffsetZ %.2f' % (rx, ry, gz)
            box = boxes.get(item)
            if box:
                dx = (px - box[0]) / box[2] - 0.5
                dy = (py - box[1]) / box[3] - 0.5
                mrx, mry = g(dy), -g(dx)
                mz = -25 * (1 - (abs(mrx) + abs(mry)) / MAX)
                line += '; the Button model at %.3f, %.3f: %.2f, %.2f, %.2f' % (dx, dy, mrx, mry, mz)
            print(line)
            # The press's frames, from the first change to the held value.
            first = [(t, v) for t, v in mine if t <= upto]
            print('  press frames: ' + '; '.join('%+.1f %s' % (t - t0, 'rest' if v is None else '%.2f %.2f %.2f' % v) for t, v in first[:6]))
        if rel is not None and held and held[-1] == next((v for t, v in reversed(mine) if v is not None and t <= rel), None):
            h = held[-1]
            norm = sum(c * c for c in h)
            after = [(t, v) for t, v in mine if t > rel]
            if norm > 0 and after:
                fr = ['%+.1f %s' % (t - rel, 'rest' if v is None else '%.4f' % (sum(a * b for a, b in zip(v, h)) / norm)) for t, v in after]
                print('  after the release (ms from it, fraction of the held): ' + '; '.join(fr))


if __name__ == '__main__':
    main()
