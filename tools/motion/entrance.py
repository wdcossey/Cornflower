"""The Hub's entrance, from a `-Probe hubentrance` run's log (#148).

Usage: python -I entrance.py <run folder>

The probe (tools/refapp/app/Probe.Reads.cs) places a Hub from code, three times, and logs every frame something changed
with the frame's RenderingTime (rt, the animation clock, 15.625 ms a step) and the Hub's root's projection (the
template's EntranceAnimationProjection: `rot` X Y Z, `centreX`, `localX`, `global` X Y Z), the root's and the Hub's
opacity, the header's and the first section's left edges and the background grid's opacity, size and left edge.

For each Hub placed (each `step` that starts "place a Hub"), it prints:
- the RotationY a frame, from the first frame where it isn't 0 to the first where it is 0 again, with the frames
  counted on rt (one a 15.625 ms step), and the projection's other values where they aren't 0;
- an ExponentialEase (out) fitted to the RotationY as a fraction of its first value, the first frame the ease's start
  (`fit` says why); and the ratio a tick at the start. An entrance that never gets back to 0 in its segment is said so
  and not fitted;
- the background grid at the end: its size and left edge.
Then whether every Hub's entrance had the same values a frame.

Python 3 standard library only.
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

FRAME = re.compile(r'rt ([\d.]+) hub proj rot ([-\d.]+) ([-\d.]+) ([-\d.]+) centreX ([-\d.]+) localX ([-\d.]+) global ([-\d.]+) ([-\d.]+) ([-\d.]+)'
                   r' \| op ([-\d.]+) hubop ([-\d.]+) \| header ([-\d.NaN]+) flyin ([-\d.NaN]+) \| section1 ([-\d.NaN]+)'
                   r' \| bg op ([-\d.NaN]+) ([-\d.NaN]+) x ([-\d.NaN]+) at ([-\d.NaN]+)')


def entrance(seg):
    """The entrance in a segment: [(tick, RotationY)] from the first frame turned to the first back at 0, ticks
    counted on rt from the first; and the last frame's parsed values. None with no turned frame."""
    rows = [FRAME.match(d) for t, w, d in seg if w == 'frame']
    rows = [m for m in rows if m]
    turned = [k for k, m in enumerate(rows) if float(m.group(3)) != 0]
    if not turned:
        return None
    first = turned[0]
    rt0 = float(rows[first].group(1))
    out = []
    for m in rows[first:]:
        out.append((round((float(m.group(1)) - rt0) / ml.TICK), float(m.group(3))))
        if float(m.group(3)) == 0:
            break
    return out, rows[first], rows[-1]


def fit(curve):
    """The ExponentialEase (out) whose remaining fraction fits the curve's fractions of its first value best, the first
    frame at the curve's start: (exponent, duration ms, rms of the first value). A grid (exponents 1 to 12 by 0.25,
    durations 500 to 1500 ms by a tick), then both moved off it by Nelder-Mead.

    The first frame is taken as the ease's start, not fitted: an ExponentialEase's start, exponent and duration trade
    off so closely that a fit with all of them free finds another ease within 0.00001 (a synthetic 5 over 750 ms read a
    tick late fits 4.93 over 740 ms, 0.34 ticks in). On the phone the first frame is -120 on every placement, a round
    value, which points to it being the start."""
    first = curve[0][1]
    ticks = [k for k, _ in curve]
    fracs = [v / first for _, v in curve]
    cost = lambda x: ml.rms(ml.fraction_residuals(fracs, ml.expo_out(x[0]), x[1], 0, ticks)) if x[0] > 0 and x[1] > 0 else 1e9
    grid = min((cost((i / 4, j * ml.TICK)), i / 4, j * ml.TICK) for i in range(4, 49) for j in range(32, 97))
    r, (k, d) = ml.minimize(cost, [grid[1], grid[2]], [0.25, ml.TICK])
    return k, d, r


def main():
    (run,), _ = ml.parse_args(__doc__)
    rows = ml.probe(run)
    seen = []
    for label, seg in ml.step_segments(rows):
        if not label.startswith('place a Hub'):
            continue
        got = entrance(seg)
        if got is None:
            print('\n%s: no frame with the Hub turned' % label)
            continue
        curve, start, last = got
        if curve[-1][1] != 0:
            print('\n%s: RotationY %s at tick 0, never back to 0 in the segment (%g at tick %d); not fitted'
                  % (label, start.group(3), curve[-1][1], curve[-1][0]))
            continue
        seen.append(curve)
        print('\n%s: RotationY %s at tick 0, 0 at tick %d (%.1f ms of rt), %d frames'
              % (label, start.group(3), curve[-1][0], curve[-1][0] * ml.TICK, len(curve)))
        print('  CenterOfRotationX %s, RotationX %s, RotationZ %s, LocalOffsetX %s, GlobalOffset %s %s %s, opacity %s'
              % (start.group(5), start.group(2), start.group(4), start.group(6), start.group(7), start.group(8), start.group(9), start.group(10)))
        print('  ' + ' '.join('%d:%.3f' % c for c in curve[:12]) + ' ...')
        ratios = [b / a for (ka, a), (kb, b) in zip(curve, curve[1:6]) if kb == ka + 1 and a]
        k, d, r = fit(curve)
        print('  a tick at the start: %s; ExponentialEase %.2f over %.1f ms, the first frame its start, rms %.5f of the first value'
              % (' '.join('%.4f' % x for x in ratios), k, d, r))
        print('  at the end: header %s, section 1 %s; background grid %s x %s at %s, opacity %s'
              % (last.group(12), last.group(14), last.group(16), last.group(17), last.group(18), last.group(15)))
    if len(seen) > 1:
        # A tick one entrance has and another doesn't (a frame with nothing new isn't logged) isn't compared.
        first = dict(seen[0])
        same = all(first.get(t, v) == v for c in seen[1:] for t, v in c)
        print('\nThe %d entrances %s' % (len(seen), 'have the same RotationY on every tick each shows' if same else 'differ'))


if __name__ == '__main__':
    main()
