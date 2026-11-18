"""Where a FlipView's release goes, the snap after it, its ends, a press while it moves and a SelectedIndex set from
code (#211): a `-Probe flipview` or `flipvertical` run's log, touch by touch.

Usage: python -I flip.py <run folder> [y]

The probe (app/Probe.Tier4.cs) logs, every frame something changed, the ScrollViewer's offset (in items: the phone's
FlipView scrolls by item) and the window positions of items 0, 1 and 4. Their positions give the strip's position in
view px, P (item k at rest is k pages along; a page is the item, 445 px across or 395 down). The touches' press and
release come from touch.log put on the app's clock (tools/motion's motionlib). For each touch it prints P at the press
(P0), the travel at the release, the speed over the last 100 ms before it (px a ms; nan when the drag was held still),
where it ended and whether that is on a page or back, how long after the release the last frame changed (by the
stopwatch, and in RenderingTime with its frames), the farthest it went either way, and the selection changes in ms after
the release. A second press: how far P moved during it. A step with no touch (SelectedIndex from code): P before and
after and the frames between. Give `y` for a vertical FlipView.

Python 3 standard library only.
"""
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'motion'))
import motionlib as ml
run = sys.argv[1]; axis = sys.argv[2] if len(sys.argv) > 2 else 'x'
PAGE = 445.0 if axis == 'x' else 395.0
rows = ml.probe(run)
played = ml.touch_times(os.path.join(run, 'touch.log'))
tch = ml.touches(rows, played)
FR = re.compile(r'rt ([\d.]+) offset ([-\d.]+) index (-?\d+) item0 ([-\dNa.]+),([-\dNa.]+) item1 ([-\dNa.]+),([-\dNa.]+) item4 ([-\dNa.]+),([-\dNa.]+)')
frames = []
for t, w, d in rows:
    if w != 'frame': continue
    m = FR.match(d)
    if not m: continue
    v = [float(x) for x in m.groups()]
    k = 0 if axis == 'x' else 1
    x0, x1, x4 = v[3 + k], v[5 + k], v[7 + k]
    base = 2.5 if axis == 'x' else 82.5
    if x0 == x0: P = base - x0
    elif x1 == x1: P = PAGE + base - x1
    elif x4 == x4: P = 4 * PAGE + base - x4
    else: continue
    frames.append((t, v[0], P, int(v[2])))
def at(t):
    p = None
    for f in frames:
        if f[0] > t: break
        p = f
    return p
steps = [(t, d) for t, w, d in rows if w == 'step']
for i, (ts, label) in enumerate(steps):
    te = steps[i + 1][0] if i + 1 < len(steps) else 1e12
    mine = [p for p in tch if ts <= p[0] < te]
    seg = [f for f in frames if ts <= f[0] < te]
    sel = [(t, d) for t, w, d in rows if w == 'selection' and ts <= t < te]
    if not mine:
        if seg: print('%-60s code: P %.1f -> %.1f, %d frames over %.0f ms rt' % (label, seg[0][2], seg[-1][2], len(seg), seg[-1][1] - seg[0][1]))
        continue
    press, rel = mine[0]
    s0 = at(press); P0 = s0[2] if s0 else float('nan')
    r = at(rel); Pr = r[2] if r else float('nan')
    last = [f for f in seg if rel - 100 <= f[0] <= rel]
    sp = ml.linfit([f[0] for f in last], [f[2] for f in last])[1] if len(last) >= 3 else float('nan')
    after = [f for f in seg if f[0] > rel]
    ext = [f[2] for f in seg]
    end = seg[-1] if seg else None
    settle = (end[0] - rel) if end else float('nan')
    settle_rt = (end[1] - after[0][1]) if after and end else float('nan')
    out = '%-62s P0 %.1f rel %+.1f v %.2f -> end %+.1f (%s) settle %.0f ms [rt %.0f, %d frames] min %+.1f max %+.1f sel %s' % (
        label, P0, Pr - P0, sp, end[2] - P0 if end else float('nan'), 'on' if end and abs(end[2] - P0) > PAGE / 2 else 'back',
        settle, settle_rt, len(after), min(ext) - P0 if ext else 0, max(ext) - P0 if ext else 0, ','.join('%.0f:%s' % (t - rel, d) for t, d in sel))
    print(out)
    if len(mine) > 1:
        p2, r2 = mine[1]
        a2 = at(p2); during = [f[2] for f in seg if p2 <= f[0] <= r2]
        print('    second press %.0f ms after release: P %+.1f, moved %.1f during the press, then end %+.1f' % (p2 - rel, (a2[2] - P0) if a2 else float('nan'), (max(during) - min(during)) if during else 0, end[2] - P0))
    if 'swipe left 0' in label or 'SelectedIndex 3' in label:
        print('    frames after release (rt ms, P-P0):', ' '.join('%.1f:%.1f' % (f[1] - after[0][1], f[2] - P0) for f in after[:60]))
