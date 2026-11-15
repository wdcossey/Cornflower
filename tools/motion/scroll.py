"""The ScrollViewer's deceleration after a flick, and its panning indicator's timing, from a `-Probe scroll` or
`scrollend` run's log (#113, gap register B8).

Usage: python -I scroll.py <run folder> [--shared] [--csv <file>]

The probe (tools/refapp/app/Probe.Scroll.cs) logs, every frame that something changes, the frame's `RenderingTime` (rt,
the emulator's animation clock: it steps by 15.625 ms), the ScrollViewer's `VerticalOffset` and its vertical ScrollBar's
indicator; and each `ViewChanging` (NextView, FinalView, IsInertial) as it comes. Each touch's `step` line, 1.2 s before
its press, starts its segment. The inertia runs from the frame logged just before the first inertial ViewChanging after
the press (the release's frame) to the first frame at the final offset. Times are rt from that first frame.

For each touch that ends in inertia it prints:
- where the inertia starts and ends, its distance and its time, and FinalView at the first inertial ViewChanging
  against the end;
- three fits to its frames, each with its rms in view px: an exponential decay (offset = end - d0 e^(-t/tau), its end
  fitted too);
  a constant deceleration a (a parabola, until it stops); and a deceleration a + c v^2 (a constant one and a quadratic
  drag), whose speed v0 at the start, a and c are fitted (`drag`).

Then a table of every flick: the drag fit's v0, the distance and the time. With --shared, one a and c for every flick
(only v0 and the start fitted to each), and the rms of each and of all. A run of 22 flicks takes about 30 s, and
--shared about 15 s more: each flick's a + c v^2 fit is a Nelder-Mead over a golden-section search.

Last, for each touch, the ScrollBar's `IndicatorMode` changes against the press and the end of the inertia (the app sees
no change of the indicator's opacity: the fades are only in a recording, `scrollend.py`).

--csv writes every inertial frame: touch, t, offset.

Python 3 standard library only.
"""
import math
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

FRAME = re.compile(r'rt ([\d.]+) offset ([-\d.]+)(?: root ([-\d.]+) bar ([-\d.]+) thumb ([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+) mode (\w+))?')
CHANGING = re.compile(r'next ([-\d.]+) final ([-\d.]+) inertial (True|False)')


def frames(seg):
    """(stopwatch ms, rt ms, offset, indicator or None) for every frame line; the indicator is (root opacity, bar opacity,
    thumb x, y, w, h, mode)."""
    out = []
    for t, what, d in seg:
        if what != 'frame':
            continue
        m = FRAME.match(d)
        if not m:
            continue
        ind = None
        if m.group(3) is not None:
            ind = tuple(float(m.group(i)) for i in range(3, 9)) + (m.group(9),)
        out.append((t, float(m.group(1)), float(m.group(2)), ind))
    return out


def changing(seg):
    """(stopwatch ms, next, final, inertial) for every ViewChanging."""
    out = []
    for t, what, d in seg:
        if what == 'viewchanging':
            m = CHANGING.match(d)
            if m:
                out.append((t, float(m.group(1)), float(m.group(2)), m.group(3) == 'True'))
    return out


def inertia(seg):
    """The touch's inertia, or None: (rt of its first frame, [(t, offset)] with t in ms of rt from it, the first inertial
    FinalView, the stopwatch time of its last frame). Its first frame is the one before the first inertial frame (the
    frame logged with or after the first inertial ViewChanging), one tick earlier: the release's frame. A frame is logged
    only when something changed, so its offset is the last one logged before it."""
    # The setup's ChangeView, before the press, raises an inertial ViewChanging too: only what follows the press counts.
    press = next((r[0] for r in seg if r[1] == 'pressed'), None)
    if press is None:
        return None
    vc = [c for c in changing(seg) if c[0] > press]
    first = next((c for c in vc if c[3]), None)
    if first is None:
        return None
    fr = frames(seg)
    before = [f for f in fr if press < f[0] < first[0]]
    after = [f for f in fr if f[0] >= first[0]]
    if not before or len(after) < 3:
        return None
    pts = [(None, after[0][1] - ml.TICK, before[-1][2], None)] + after
    end = pts[-1][2]
    # Up to the first frame at the end: later frames only log the indicator.
    last = next(i for i, p in enumerate(pts) if p[2] == end)
    pts = pts[:last + 1]
    rt0 = pts[0][1]
    return rt0, [(p[1] - rt0, p[2]) for p in pts], first[2], pts[-1][0] if pts[-1][0] is not None else first[0]


def fit_exponential_decay(pts):
    """offset = end - d0 e^(-t / tau), by least squares in px: for each tau, end and d0 are a straight-line fit (they
    enter linearly), and tau is a golden-section search over its logarithm from 10 ms to 100 s. (tau ms, d0, end,
    rms px), or None when there are too few points."""
    if len(pts) < 4:
        return None

    def fit(tau):
        e = [math.exp(-t / tau) for t, _ in pts]
        end, slope = ml.linfit(e, [x for _, x in pts])
        return sum((x - end - slope * ei) ** 2 for (_, x), ei in zip(pts, e)), end, -slope

    g = (math.sqrt(5) - 1) / 2
    lo, hi = math.log(10.0), math.log(100000.0)
    m1, m2 = hi - g * (hi - lo), lo + g * (hi - lo)
    f1, f2 = fit(math.exp(m1))[0], fit(math.exp(m2))[0]
    for _ in range(60):
        if f1 < f2:
            hi, m2, f2 = m2, m1, f1
            m1 = hi - g * (hi - lo)
            f1 = fit(math.exp(m1))[0]
        else:
            lo, m1, f1 = m1, m2, f2
            m2 = lo + g * (hi - lo)
            f2 = fit(math.exp(m2))[0]
    tau = math.exp((lo + hi) / 2)
    sse, end, d0 = fit(tau)
    return tau, d0, end, math.sqrt(sse / len(pts))


def solve3(m, r):
    """Solves the 3 x 3 system m c = r by Cramer's rule; None if singular."""
    def det(a):
        return (a[0][0] * (a[1][1] * a[2][2] - a[1][2] * a[2][1]) - a[0][1] * (a[1][0] * a[2][2] - a[1][2] * a[2][0])
                + a[0][2] * (a[1][0] * a[2][1] - a[1][1] * a[2][0]))
    d = det(m)
    if abs(d) < 1e-30:
        return None
    out = []
    for k in range(3):
        a = [row[:] for row in m]
        for i in range(3):
            a[i][k] = r[i]
        out.append(det(a) / d)
    return out


def fit_constant_deceleration(pts):
    """offset = x0 + v0 t - a t^2 / 2, by least squares over the points (to the first at the end). (v0 px/ms, a px/ms^2,
    rms px, the stop's t), or None; v0 and a are given as sizes, whichever way the offset moves."""
    ts, xs = [p[0] for p in pts], [p[1] for p in pts]
    s = [sum(t ** k for t in ts) for k in range(5)]
    r = [sum(x * t ** k for t, x in zip(ts, xs)) for k in range(3)]
    c = solve3([[s[0], s[1], s[2]], [s[1], s[2], s[3]], [s[2], s[3], s[4]]], r)
    if c is None or c[2] * c[1] >= 0:
        return None
    c0, c1, c2 = c
    stop = -c1 / (2 * c2)
    model = lambda t: c0 + c1 * min(t, stop) + c2 * min(t, stop) ** 2
    return abs(c1), abs(2 * c2), ml.rms([x - model(t) for t, x in pts]), stop


def drag_distance(t, v0, a, c):
    """How far a speed v0 (px/ms) goes in t ms under a deceleration a + c v^2: dv/dt = -a - c v^2, so
    v = sqrt(a/c) tan(phi - w t) with w = sqrt(a c) and phi = atan(v0 sqrt(c/a)), until it stops at phi / w; c = 0 is a
    constant deceleration."""
    if c <= 1e-12:
        tt = min(max(t, 0.0), v0 / a)
        return v0 * tt - a * tt * tt / 2
    w = math.sqrt(a * c)
    phi = math.atan(v0 * math.sqrt(c / a))
    tt = min(max(t, 0.0), phi / w)
    return math.log(math.cos(phi - w * tt) / math.cos(phi)) / c


def drag_time(v0, a, c):
    """When a speed v0 stops under a deceleration a + c v^2."""
    return v0 / a if c <= 1e-12 else math.atan(v0 * math.sqrt(c / a)) / math.sqrt(a * c)


def drag_v0(pts, a, c, sign=1):
    """The v0 that fits the points best under a + c v^2, the start x0 fitted with it (it enters linearly): (v0, x0, sum
    of squares). A golden-section search over v0 from 0.005 to 40 px/ms."""
    def sse(v0):
        d = [x - sign * drag_distance(t, v0, a, c) for t, x in pts]
        x0 = sum(d) / len(d)
        return sum((r - x0) ** 2 for r in d), x0
    g = (math.sqrt(5) - 1) / 2
    lo, hi = 0.005, 40.0
    m1, m2 = hi - g * (hi - lo), lo + g * (hi - lo)
    f1, f2 = sse(m1)[0], sse(m2)[0]
    for _ in range(70):
        # Each step keeps one of the two inner points and its value, so it costs one evaluation.
        if f1 < f2:
            hi, m2, f2 = m2, m1, f1
            m1 = hi - g * (hi - lo)
            f1 = sse(m1)[0]
        else:
            lo, m1, f1 = m1, m2, f2
            m2 = lo + g * (hi - lo)
            f2 = sse(m2)[0]
    v0 = (lo + hi) / 2
    e, x0 = sse(v0)
    return v0, x0, e


def fit_drag(pts, a0=0.0004, c0=0.0008):
    """offset = x0 + the distance under a deceleration a + c v^2 from v0, all four fitted. (v0 px/ms, a px/ms^2, c /px,
    rms px, the stop's t)."""
    sign = 1 if pts[-1][1] >= pts[0][1] else -1
    f = lambda q: drag_v0(pts, q[0], q[1], sign)[2] if q[0] > 0 and q[1] >= 0 else 1e18
    _, (a, c) = ml.minimize(f, [a0, c0], [a0 / 4, c0 / 2], iterations=300)
    v0, _, e = drag_v0(pts, a, c, sign)
    return v0, a, c, math.sqrt(e / len(pts)), drag_time(v0, a, c)


def fit_drag_shared(runs_pts):
    """One a and c for every flick, v0 and x0 fitted to each: (a, c, [(v0, rms) per flick], rms over all)."""
    def total(q):
        if q[0] <= 0 or q[1] < 0:
            return 1e18
        return sum(drag_v0(p, q[0], q[1], 1 if p[-1][1] >= p[0][1] else -1)[2] for p in runs_pts)
    _, (a, c) = ml.minimize(total, [0.0005, 0.0007], [0.0001, 0.0003], iterations=150)
    per, e_all, n = [], 0.0, 0
    for p in runs_pts:
        v0, _, e = drag_v0(p, a, c, 1 if p[-1][1] >= p[0][1] else -1)
        per.append((v0, math.sqrt(e / len(p))))
        e_all += e
        n += len(p)
    return a, c, per, math.sqrt(e_all / n)


def indicator_modes(seg):
    """(stopwatch ms, mode) for each change of the ScrollBar's IndicatorMode in the segment."""
    out, last = [], None
    for t, rt, x, ind in frames(seg):
        if ind is not None and ind[6] != last:
            out.append((t, ind[6]))
            last = ind[6]
    return out


def main():
    (run,), opts = ml.parse_args(__doc__, options=('--csv',), flags=('--shared',))
    segs = ml.step_segments(ml.probe(run))
    table, csv, timing = [], [], []
    print('%s: %d touches' % (run, len(segs)))
    for label, seg in segs:
        inn = inertia(seg)
        press = next((r[0] for r in seg if r[1] == 'pressed'), None)
        modes = indicator_modes(seg)
        if press is not None:
            ends = inn[3] if inn else None
            timing.append((label, press, ends, modes))
        if inn is None:
            continue
        rt0, pts, final, _ = inn
        dist = pts[-1][1] - pts[0][1]
        if abs(dist) < 1:
            print('\n%s\n  no inertia to speak of: %.2f px' % (label, dist))
            continue
        print('\n%s' % label)
        print('  inertia from %.2f at rt %.3f to %.2f: %.2f px in %.1f ms, %d frames; FinalView at its first frame %.2f (%+.2f)'
              % (pts[0][1], rt0, pts[-1][1], dist, pts[-1][0], len(pts) - 1, final, final - pts[-1][1]))
        e = fit_exponential_decay(pts)
        q = fit_constant_deceleration(pts)
        d = fit_drag(pts)
        print('  exponential: %s' % ('tau %.1f ms, its end %.1f, rms %.2f px' % (e[0], e[2], e[3]) if e else 'no fit'))
        print('  constant deceleration: %s' % ('%.6f px/ms^2, stops at %.0f ms, rms %.2f px' % (q[1], q[3], q[2]) if q else 'no fit'))
        print('  a + c v^2: v0 %.3f px/ms, a %.6f px/ms^2, c %.6f /px, stops at %.0f ms, rms %.2f px' % (d[0], d[1], d[2], d[4], d[3]))
        table.append((label, d, dist, pts[-1][0], pts))
        csv.extend((label, t, x) for t, x in pts)

    if table:
        print('\nEvery flick, by the a + c v^2 fit (view px, ms of animation time)')
        print('  %-40s %8s %9s %8s %7s %9s %9s' % ('touch', 'v0', 'distance', 'time', 'rms', 'a', 'c'))
        for label, d, dist, T, _ in table:
            print('  %-40s %8.3f %9.1f %8.0f %7.2f %9.6f %9.6f' % (label[:40], d[0], dist, T, d[3], d[1], d[2]))
        if opts['--shared']:
            a, c, per, all_rms = fit_drag_shared([r[4] for r in table])
            print('\nOne a and c for every flick: a %.7f px/ms^2, c %.6f /px (the longest a flick can last: %.0f ms); rms %.2f px'
                  % (a, c, math.pi / 2 / math.sqrt(a * c) if c > 0 else float('inf'), all_rms))
            for (label, _, dist, T, _), (v0, r) in zip(table, per):
                print('  %-40s v0 %6.3f stops at %5.0f (ends at %5.0f) rms %.2f' % (label[:40], v0, drag_time(v0, a, c), T, r))

    print('\nThe indicator (the ScrollBar\'s IndicatorMode), ms by the stopwatch from the press')
    for label, press, ends, modes in timing:
        changes = ', '.join('%s at %+.0f' % (m, t - press) for t, m in modes if t >= press - 50)
        print('  %-40s %s%s' % (label[:40], changes, '; the inertia ends at %+.0f' % (ends - press) if ends else ''))

    if opts['--csv']:
        with open(opts['--csv'], 'w') as f:
            f.write('touch,t,offset\n')
            for label, t, x in csv:
                f.write('"%s",%.3f,%.3f\n' % (label, t, x))


if __name__ == '__main__':
    main()
