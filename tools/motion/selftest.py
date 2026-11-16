"""Checks motionlib's measurements and fits on synthetic data with known answers.

Usage: python -I selftest.py

Each check builds data from a known curve, camera or outline, runs the code the scripts use, and asserts it gets the
answer back; and that the scripts stop, rather than go on with mispaired or missing data, where they should.
Prints one line a check and exits non-zero on the first failure. Takes about half a minute.
"""
import math
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

checks = []


def check(fn):
    checks.append(fn)
    return fn


def near(a, b, tol, what):
    if abs(a - b) > tol:
        raise AssertionError('%s: %r, expected %r within %r' % (what, a, b, tol))


@check
def crossing():
    vals = bytes([0] * 10 + [200] * 20 + [0] * 10)
    near(ml.cross(vals, 100), 9.5, 1e-9, 'rising edge')
    near(ml.cross(vals, 100, rev=True), 29.5, 1e-9, 'falling edge, from the end')
    near(ml.cross(bytes([0, 50, 150, 200]), 100), 1.5, 1e-9, 'interpolated')
    assert ml.cross(bytes([0] * 5), 100) is None


class FakeRecording:
    """Frames of a lit rectangle, columns a..b-1 and rows c..d-1 at level 200, on black."""

    def __init__(self, rects, w=540, h=960):
        self.w, self.h, self.s = w, h, ml.VIEW_W / w
        self.rects = rects

    def frames(self, t0=-1e9, t1=1e9):
        for i, (a, b, c, d) in enumerate(self.rects):
            row = bytes([0] * a + [200] * (b - a) + [0] * (self.w - b))
            blank = bytes(self.w)
            yield i, i * 0.025, b''.join(row if c <= y < d else blank for y in range(self.h))


@check
def sheet_outline():
    rec = FakeRecording([(23, 518, 72, 888), (0, 300, 72, 888)])
    rows = ml.outline(rec)
    s = rec.s
    i, t, m, level = rows[0]
    near(m[0], 22.5 * s, 0.001, 'XL')
    near(m[1], 517.5 * s, 0.001, 'XR')
    near(m[2], 816 * s, 0.001, 'HL')
    near(m[3], 816 * s, 0.001, 'HR')
    assert level == 200
    assert rows[1][2][2] is None, 'a sheet off the left edge is clipped'
    box = ml.bbox(next(rec.frames())[2], rec)
    near(box[0], round(22.5 * s, 1), 1e-9, 'bbox left')
    near(box[1], round(71.5 * s, 1), 1e-9, 'bbox top')


@check
def easings():
    near(ml.expo_out(6)(0.0), 0.0, 1e-12, 'expo at 0')
    near(ml.expo_out(6)(1.0), 1.0, 1e-12, 'expo at 1')
    near(ml.expo_out(6)(0.5), (1 - math.exp(-3)) / (1 - math.exp(-6)), 1e-12, 'expo at 0.5')
    f = ml.key_spline(0.1, 0.9, 0.2, 1)
    near(f(0.0), 0.0, 1e-9, 'spline at 0')
    near(f(1.0), 1.0, 1e-9, 'spline at 1')
    # On the Bezier itself: the point at parameter 0.5 is (0.2375, 0.8375).
    near(f(3 * 0.1 / 8 + 3 * 0.2 / 8 + 1 / 8), 3 * 0.9 / 8 + 3 / 8 + 1 / 8, 1e-6, 'spline at s = 0.5')
    near(ml.key_spline(0.25, 0.25, 0.75, 0.75)(0.3), 0.3, 1e-6, 'a straight spline')


@check
def distinct_and_ticks():
    assert ml.distinct([-67.01, -67.04, -53.8, -53.83, -0.5, -43.12], 1, 0.25, 0.9) == [-67.0, -53.8, -43.1]
    vals = [100 * 0.8 ** k for k in (0, 1, 2, 4, 5, 6, 7, 9)]
    r0, ticks = ml.tick_numbers(vals)
    assert ticks == [0, 1, 2, 4, 5, 6, 7, 9], ticks
    near(r0, 0.8, 1e-9, 'median ratio')


@check
def exponential_ease_out():
    # An exponential ease-out from -67 reaching 0 at tick 25, falling 0.8031 a tick; one tick never shown.
    r, N, A = 0.8031, 25, -67.0
    ticks = list(range(18)) + [19]
    vals = [A * (r ** k - r ** N) / (1 - r ** N) for k in ticks]
    rms, rr, NN, AA, tau, k = ml.fit_exponential(vals, ticks, eased=True)
    near(rr, r, 1e-4, 'ratio')
    assert NN == N, NN
    near(AA, A, 0.01, 'start')
    near(rms, 0, 1e-3, 'rms')
    near(k, N * ml.TICK / (-ml.TICK / math.log(r)), 1e-9, 'exponent')


@check
def scaled_expo_fit():
    # ExponentialEase 6 from 200 over 350 ms, first value on the start, tick 2 missing.
    f = ml.expo_out(6)
    ticks = [0, 1] + list(range(3, 17))
    vals = [200 * (1 - f(k * ml.TICK / 350)) for k in ticks]
    rms, d, ph, A = ml.fit_scaled(vals, ticks, f)
    assert (d, ph) == (350, 0), (d, ph)
    near(A, 200, 1e-6, 'start')
    near(rms, 0, 1e-9, 'rms')


@check
def key_spline_fit():
    # The knob: KeySpline 0.1,0.9 0.2,1 over 370 ms, the first value one tick in; a power ease-out fits worse.
    f = ml.key_spline(0.1, 0.9, 0.2, 1)
    fracs = [1 - f((1 + k) * ml.TICK / 370) for k in range(21)]
    rms, d, ph = ml.fit_fraction(fracs, f)
    assert (d, ph) == (370, 1.0), (d, ph)
    near(rms, 0, 1e-9, 'rms')
    assert ml.fit_fraction(fracs, ml.power_out(5))[0] > 0.005


@check
def camera_fit():
    # Held states seen through a camera 999 px away, vanishing point (224, 400).
    states = [(10, 0, 0), (-10, 0, 0), (-30, 0, 0), (-60, 0, 0), (10, 0.5, 0), (30, 0.5, 0), (-30, -0.25, 0),
              (0, 0, -100), (30, 0, -100)]
    held = [(st, ml.project_plane(st[0], st[1], st[2], 999, 224)) for st in states]
    e, d, cx, sgn = ml.fit_camera(held)
    assert (d, cx, sgn) == (999, 224, 1), (d, cx, sgn)
    near(e, 0, 1e-9, 'rms')


@check
def turnstile_axis():
    # Frames of a sheet turning about x -44, z -99; the angle of each and the axis come back.
    d, cx = 1000.0, 225.0
    for theta in (-67.0, -22.3, -3.6, 33.7, 5.9):
        m = ml.project_y(theta, -44, -99, d, cx)
        near(ml.solve_angle(m, -44, -99, d, cx)[0], theta, 1e-4, 'angle')
    segs = [[(0, 0.0, ml.project_y(th, -44, -99, d, cx), 255)] for th in (-53.8, -34.6, -17.8, -7.2)]
    e, xa, za = ml.fit_axis(segs, d, cx)
    near(xa, -44, 0.25, 'axis x')
    near(za, -99, 0.25, 'axis z')


def exits(fn, *args, **kw):
    """True if fn stops the script (SystemExit), as the scripts do on data they can't trust."""
    try:
        fn(*args, **kw)
    except SystemExit:
        return True
    return False


@check
def pairing_steps_to_transitions():
    # Steps 1.3 s apart; each transition starts 27.8 s earlier on the recording's clock, give or take 20 ms; two other
    # transitions in between. Each step gets its own, in order.
    times = [32.0 + 1.3 * k for k in range(18)]
    jitter = [0.02 * ((k * 7) % 5 - 2) / 2 for k in range(18)]
    starts = sorted([t - 27.8 + j for t, j in zip(times, jitter)] + [4.85, 30.0])
    lag, pairs, lags = ml.match_steps(starts, times)
    near(lag, -27.8, 0.02, 'lag')
    assert [starts[j] for j in pairs] == [t - 27.8 + j for t, j in zip(times, jitter)]
    # A transition missing: the scripts stop rather than pair the rest one off.
    assert exits(ml.match_steps, starts[:5] + starts[6:], times)
    # Steps out of step with the transitions altogether.
    assert exits(ml.match_steps, starts, [t + 0.4 * (k % 2) for k, t in enumerate(times)])


@check
def segments_need_rest_first():
    still = [19.0, 431.0, 680.0, 680.0]
    rows = [(0, 0.0, still, 255), (1, 0.1, [30.0, 420.0, 600.0, 650.0], 255), (2, 0.2, still, 255),
            (3, 0.3, [0.0, 300.0, None, None], 255), (4, 0.4, still, 255), (5, 0.5, [25.0, 425.0, 670.0, 690.0], 255)]
    assert [[r[0] for r in s] for s in ml.segments(rows)] == [[1], [3], [5]]
    assert [[r[0] for r in s] for s in ml.segments(iter(rows), limit=2)] == [[1], [3]]
    assert exits(ml.segments, rows[3:])  # clipped first: no rest to compare with
    assert exits(ml.segments, [])


@check
def median_ratio():
    # Ratios 0.5, 0.6, 0.7, 0.8: the median is 0.65, not the upper middle value.
    vals = [100, 50, 30, 21, 16.8]
    near(ml.tick_numbers(vals)[0], 0.65, 1e-9, 'median ratio')


@check
def camera_off_the_grid():
    # A camera between the grid's points: the refinement, around the grid's best, finds it.
    states = [(10, 0, 0), (-10, 0, 0), (-30, 0, 0), (-60, 0, 0), (10, 0.5, 0), (30, 0.5, 0), (0, 0, -100), (30, 0, -100)]
    held = [(st, ml.project_plane(st[0], st[1], st[2], 1003, 221.75)) for st in states]
    e, d, cx, sgn = ml.fit_camera(held)
    assert (d, cx) == (1003, 221.75), (d, cx)


@check
def axis_needs_frames():
    clipped = [[(0, 0.0, [0.0, 300.0, None, None], 255)]]
    assert exits(ml.axis_rms, clipped, -44, -99, 1000, 225)


@check
def missing_tick_on_a_curve():
    # The knob's check: positions a tick apart land about a tick apart on the curve; one tick left out, two.
    f = ml.key_spline(0.1, 0.9, 0.2, 1)
    ticks = [0, 1, 2, 3, 5, 6, 7]
    fracs = [1 - f((1 + k) * ml.TICK / 370) for k in ticks]
    at = ml.ticks_on_curve(fracs, f, 370, 1.0)
    gaps = [b - a for a, b in zip(at, at[1:])]
    for g, (a, b) in zip(gaps, zip(ticks, ticks[1:])):
        near(g, b - a, 1e-6, 'ticks apart')
    rms, d, ph = ml.fit_fraction(fracs, f, ticks=ticks)
    assert (d, ph) == (370, 1.0) and rms < 1e-9


@check
def arguments():
    saved = sys.argv
    try:
        sys.argv = ['x.py', 'run', '--lag', '29.15', '--no-search']
        assert ml.parse_args('doc', options=('--gray', '--lag'), flags=('--no-search', '--other')) == (
            ['run'], {'--gray': None, '--lag': '29.15', '--no-search': True, '--other': False})
        sys.argv = ['x.py', 'run', '--unknown', '1']
        assert exits(ml.parse_args, 'doc', options=('--gray',))
        sys.argv = ['x.py']
        assert exits(ml.parse_args, 'doc')
        sys.argv = ['x.py', 'run', '--gray']
        assert exits(ml.parse_args, 'doc', options=('--gray',))
    finally:
        sys.argv = saved


@check
def recording_files():
    # A run folder of three 10 x 4 frames: the size from rect.log, the frames found by their size, a row read
    # alone, and the file closed by `with`.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        os.mkdir(os.path.join(run, 'analysis'))
        with open(os.path.join(run, 'frames.csv'), 'w') as f:
            f.write('0.000000,\n0.025000\n0.050000\n')
        with open(os.path.join(run, 'rect.log'), 'w') as f:
            f.write('12:00:00.000 display window at 1,2, 10 x 4\n')
        with open(os.path.join(run, 'analysis', 'record.gray'), 'wb') as f:
            f.write(bytes(range(120)))
        with open(os.path.join(run, 'analysis', 'small.gray'), 'wb') as f:
            f.write(bytes(60))
        with ml.Recording(run) as rec:
            assert rec.gray.endswith('record.gray')
            assert (rec.w, rec.h) == (10, 4)
            assert rec.row(2, 1) == bytes(range(90, 100))
            assert rec.frame(1) == bytes(range(40, 80))
            assert rec.index_at(0.03) == 2
        assert rec._f.closed


def run_script(name, run):
    import subprocess
    out = subprocess.run([sys.executable, '-I', os.path.join(os.path.dirname(os.path.abspath(__file__)), name), run],
                         capture_output=True, text=True)
    return out.returncode, out.stdout + out.stderr


@check
def scripts_on_partial_logs():
    # A tilt press with no tilt logged is left out, not a crash; a MenuFlyout rise cut off by the end of the log
    # still counts.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tbutton A 19 120 120 60 root Grid\n')
            f.write('1.0\tpressed\tA 0 0 0 60 30\n2.0\tvalue\tprojection A rot ' + ' '.join(['0'] * 14) + ' -25\n3.0\treleased\tA\n')
            f.write('4.0\tpressed\tA 0 0 0 6 30\n5.0\treleased\tA\n')
        code, out = run_script('tiltcurve.py', run)
        assert code == 0 and 'presses 1, and 1 left out' in out, out
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            for k in range(25):
                f.write('%.1f\ttree\tpopup1/0 Border#OuterBorder | transform scale %g 1 centre 0 0 origin 0.5 0\n'
                        % (100 + 15.625 * k, round(k * 0.0411, 3)))
        code, out = run_script('menuflyout.py', run)
        assert code == 0 and 'OuterBorder ScaleX: 1 rises' in out and '24.3 steps' in out, out


@check
def frame_spacing():
    # Frames on a 15.0 ms grid, logged as a sparse probe logs them: a frame now and then missing (a 30 ms gap), one
    # late by 10 ms (25 then 5), a stretch of 240 ms with nothing logged; and a frame's readings 0.5 ms apart, a chain
    # of them longer than the merge (each within 3 ms of the one before) still one frame. The mean comes back 15.0.
    grid = [100 + 15.0 * k for k in range(400) if k % 7 != 3 and not 200 < k < 216]
    grid[50] += 10
    times = [t + d for t in grid for d in (0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5)]
    frames = ml.frames(times)
    assert len(frames) == len(grid), (len(frames), len(grid))
    mean, med, counted = ml.frame_spacing(frames)
    near(mean, 15.0, 1e-9, 'mean')
    # With the frames each gap held given, as a clock read each frame gives them: the time over their sum.
    near(ml.frame_spacing([0.0, 15.0, 45.0, 60.0], [1, 2, 1])[0], 15.0, 1e-9, 'mean with ticks')


@check
def clock_factor():
    # Frames 15.0 ms apart, two readings a frame, and a 240 ms stretch with nothing logged: 15.625 / 15.0.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            t = 100.0
            for k in range(200):
                t += 240 if k == 100 else 15.0
                f.write('%.1f\tvalue\ta %d\n%.1f\ttree\tb %d\n' % (t, k, t + 0.5, k))
        code, out = run_script('clock.py', run)
    assert code == 0 and '200 frames logged, 198 counted' in out and 'stopwatch 1.0417' in out, out


@check
def minimize_finds_a_minimum():
    # Rosenbrock's valley, from well off it; and a bowl, where it stops once converged, long before its iterations.
    v, x = ml.minimize(lambda p: (1 - p[0]) ** 2 + 100 * (p[1] - p[0] ** 2) ** 2, [-1.2, 1.0], [0.5, 0.5], 4000)
    near(x[0], 1, 1e-4, 'x')
    near(x[1], 1, 1e-4, 'y')
    calls = []

    def bowl(p):
        calls.append(p)
        return (p[0] - 3) ** 2 + (p[1] + 1) ** 2

    v, x = ml.minimize(bowl, [0.0, 0.0], [1.0, 1.0], 100000)
    near(x[0], 3, 1e-6, 'bowl x')
    near(x[1], -1, 1e-6, 'bowl y')
    assert len(calls) < 2000, len(calls)


@check
def key_spline_is_exact():
    # At any parameter s, the spline's value at the Bezier's x(s) is its y(s), near x1 = 0 too, where Newton's method
    # alone stalls.
    for c in ((0.1, 0.25, 0.75, 0.9), (0.0, 0.3, 0.8, 0.9), (0.001, 0.27, 0.77, 0.91), (0.1, 0.9, 0.2, 1), (0, 0, 1, 1)):
        f = ml.key_spline(*c)
        for i in range(65):
            s = i / 64
            r = 1 - s
            x = 3 * c[0] * s * r * r + 3 * c[2] * s * s * r + s ** 3
            y = 3 * c[1] * s * r * r + 3 * c[3] * s * s * r + s ** 3
            near(f(x), y, 1e-10, 'KeySpline %s at s %g' % (c, s))


@check
def tilt_release_stops_on_too_little():
    # A run whose returns log one frame each has no frame spacing of its own: the script leaves it out and says so,
    # then, with no run left, stops, not a crash.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tbutton A 19 120 120 60 root Grid\n1000.0\tpressed\tA 0 0 in-control 5 3 Touch\n')
            f.write('1020.0\tvalue\tprojection A rot -8 8 0 centre 0.5 0.5 0 local 0 0 0 global -1 -2 -0.6\n')
            f.write('1500.0\treleased\tA 0 0 in-control 5 3 Touch\n')
            f.write('1690.0\tvalue\tprojection A rot 0 0 0 centre 0.5 0.5 0 local 0 0 0 global -1 -2 0\n')
        code, out = run_script('tiltrelease.py', run)
    assert code != 0 and 'left out, no press logged two or more frames' in out and 'no run with a press' in out, out
    assert 'Traceback' not in out, out


@check
def tilt_release_in_frames():
    # Releases logged as the probe logs them: frames 15.0 ms apart by the stopwatch, each moving animation time
    # 15.625 ms, the first frame after the release at time 0; the tilt held 182.6 ms, then back down KeySpline
    # 0.1,0.25 0.75,0.9 over 167 ms; a value logged to 0.001, only when it changes. The fits must give them back.
    import re
    import tempfile
    e = ml.key_spline(0.1, 0.25, 0.75, 0.9)
    held = (-8.5, 8.2, -0.6)
    line = '%.1f\tvalue\tprojection A rot %g %g 0 centre 0.5 0.5 0 local 0 0 0 global -1 -2 %g\n'
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tbutton A 19 120 120 60 root Grid\n')
            for i in range(12):
                t = 1000.0 + 2000 * i
                f.write('%.1f\tpressed\tA 0 0 in-control 5 3 Touch handled\n' % t)
                f.write(line % ((t + 20,) + held))
                released = t + 500 + i * 1.25  # at twelve phases of a frame
                f.write('%.1f\treleased\tA 0 0 in-control 5 3 Touch handled\n' % released)
                frame = t + 507.0  # the first frame after the release
                while frame <= released:
                    frame += 15.0
                last = held
                for k in range(1, 40):
                    u = ((k - 1) * ml.TICK - 182.6) / 167
                    left = 1.0 if u <= 0 else 0.0 if u >= 1 else 1 - e(u)
                    v = tuple(round(c * left, 3) + 0.0 for c in held)
                    if v != last:
                        f.write(line % ((frame + (k - 1) * 15.0,) + v))
                    last = v
                    if not any(v):
                        break
            # And one dragged off the button (a move below it) and let go while its tilt was halfway back: left out.
            t = 1000.0 + 2000 * 12
            f.write('%.1f\tpressed\tA 0 0 in-control 5 3 Touch handled\n' % t)
            f.write(line % ((t + 20,) + held))
            f.write('%.1f\tmoved\tA 0 0 in-control 5 90 Touch\n' % (t + 300))
            f.write(line % ((t + 600,) + tuple(c / 2 for c in held)))
            f.write('%.1f\treleased\tA 0 0 in-control 5 90 Touch handled\n' % (t + 610))
            f.write(line % ((t + 625,) + tuple(c / 4 for c in held)))
            f.write(line % ((t + 640,) + (0, 0, 0)))
        code, out = run_script('tiltrelease.py', run)
    assert code == 0, out
    assert '12 presses, 1 left out' in out, out
    assert 'the first change is frame 13 after the release' in out, out
    m = re.search(r'control points fixed: hold ([\d.]+) ms, return ([\d.]+) ms; rms ([\d.]+)', out)
    assert m, out
    near(float(m.group(1)), 182.6, 0.3, 'hold')
    near(float(m.group(2)), 167.0, 0.5, 'return')
    assert float(m.group(3)) < 0.0002, out


@check
def scroll_fits():
    # The three deceleration fits get their constants back from curves made with them, on the 15.625 ms frames.
    import scroll
    ts = [k * 15.625 for k in range(140)]
    pts = [(t, 2000 + scroll.drag_distance(t, 0.4, 0.00036, 0.0)) for t in ts]
    pts = pts[:next(i for i, p in enumerate(pts) if p[1] == pts[-1][1]) + 1]
    q = scroll.fit_constant_deceleration(pts)
    near(q[1], 0.00036, 1e-7, 'constant deceleration')
    assert q[2] < 0.01, q
    pts = [(t, 2000 + scroll.drag_distance(t, 3.3, 0.0007, 0.0005)) for t in ts]
    pts = pts[:next(i for i, p in enumerate(pts) if p[1] == pts[-1][1]) + 1]
    v0, a, c, r, stop = scroll.fit_drag(pts)
    near(v0, 3.3, 0.01, 'drag v0')
    near(a, 0.0007, 0.00001, 'drag a')
    near(c, 0.0005, 0.00001, 'drag c')
    assert r < 0.05, r
    near(stop, scroll.drag_time(3.3, 0.0007, 0.0005), 1, 'drag stop')
    pts = [(t, 2500 - 400 * math.exp(-t / 300)) for t in ts] + [(ts[-1] + 15.625, 2500)]
    e = scroll.fit_exponential_decay(pts)
    near(e[0], 300, 1, 'exponential tau')
    near(e[2], 2500, 0.5, 'exponential end')


@check
def scroll_inertia_after_the_press():
    # The setup's ChangeView raises an inertial ViewChanging before the press: the inertia is the one after it.
    import scroll
    seg = [(0.0, 'viewchanging', 'next 2000 final 2000 inertial True'),
           (1.0, 'frame', 'rt 100 offset 2000'),
           (50.0, 'pressed', 'scroll 225 650 ts 1'),
           (60.0, 'viewchanging', 'next 2010 final 2010 inertial False'),
           (61.0, 'frame', 'rt 128.75 offset 2010')]  # two frames before the release's (144.375), unlogged
    for k in range(1, 6):
        seg.append((60.0 + 15 * k, 'viewchanging', 'next %d final 2050 inertial True' % (2010 + 10 * k)))
        seg.append((61.0 + 15 * k, 'frame', 'rt %g offset %d' % (160 + 15.625 * k, 2010 + 10 * k)))
    seg.append((200.0, 'frame', 'rt 300 offset 2050 root 1 bar 1 thumb 445 60 2.5 7 mode None'))
    rt0, pts, final, _ = scroll.inertia(seg)
    assert rt0 == 160 and final == 2050, (rt0, final)
    assert pts[0] == (0, 2010) and pts[-1] == (4 * 15.625, 2050), pts


def scrollend_run(run, s_min):
    """A synthetic -Probe scrollend -Record run: the flash, a pull past the top that compresses the bars to s_min about a
    point 1548 view px below the viewport's top and back, and an indicator that fades in and out; then the page turns
    light two frames after the app's step says so (the recording lags), and the indicator shows on it, #40000000 over
    white."""
    w, h, scale = 540, 960, 1.2
    top = 60 * scale
    with open(os.path.join(run, 'rect.log'), 'w') as f:
        f.write('x display window at 0,0, %d x %d\n' % (w, h))
    times = [k / 30 for k in range(50)]
    with open(os.path.join(run, 'frames.csv'), 'w') as f:
        f.write(''.join('%.6f\n' % t for t in times))
    with open(os.path.join(run, 'probe.txt'), 'w') as f:
        f.write('1000.0\tflash\ton\n1100.0\tstep\tpull 300 px past the top\n1500.0\tpressed\tscroll 225 200 ts 1\n')
        f.write('1520.0\tstate\tVerticalScrollBar ScrollingIndicatorStates NoIndicator > TouchIndicator\n')
        f.write('2200.0\tstep\theld drag, light\n')
    fp = top + 1548 * scale
    os.makedirs(os.path.join(run, 'analysis'))
    with open(os.path.join(run, 'analysis', 'bars.gray'), 'wb') as fb, open(os.path.join(run, 'analysis', 'edge.rgb'), 'wb') as fe:
        for k, t in enumerate(times):
            s = 1.0 if t < 0.6 or t > 1.0 else s_min
            col = bytearray(h)
            if t < 0.05:
                for y in range(int(top)):
                    col[y] = 255
            for b in range(8):
                c = fp + s * (top + 4.8 + 120 * b - fp)
                # Drawn with its edges' coverage, 8 view px tall: the centroid finds c, to within a row's half.
                for y in range(int(c - 6), int(c + 7)):
                    cover = max(0.0, min(y + 1, c + 4.8 * s) - max(y, c - 4.8 * s))
                    if top <= y < h and cover > 0:
                        col[y] = int(round(128 * cover))
            fb.write(bytes(v for v in col for _ in range(12)))
            # The app's light step is at 2200 ms, frame 36 by the flash (1000 ms at frame 0); the page turns at 38.
            light = k >= 38
            edge = bytearray([255 if light else 0]) * (24 * h * 3)
            if light:
                lit = {42: 217, 43: 191, 44: 191, 45: 217}.get(k, 255)
            else:
                lit = {13: 31, 14: 70, 15: 115, 16: 115, 17: 70, 18: 31}.get(k, 0)
            for y in range(300, 630):
                for x in (18, 19, 20):
                    for ch in range(3):
                        edge[(y * 24 + x) * 3 + ch] = lit
            fe.write(bytes(edge))


@check
def scrollend_compression_and_indicator():
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        scrollend_run(run, 0.91)
        code, out = run_script('scrollend.py', run)
    assert code == 0, out
    m = re.search(r'most compressed at \+\d+: s ([\d.]+), .* scaled about (\d+) view px past the pulled edge', out)
    assert m, out
    near(float(m.group(1)), 0.91, 0.002, 'compression')
    near(int(m.group(2)), 1548, 30, 'the point the content is scaled about')
    assert '2.50 view px wide' in out and 'brightest 115' in out and '275.0 view px long' in out, out
    # The light page is judged by its pixels, not by the app's step: the two black frames after the step aren't an
    # indicator the strip's width, and the light one is found.
    assert 'columns 0-23' not in out, out
    assert out.count('(light)') == 1 and 'darkest 191' in out, out


@check
def listpress_tilt():
    # A press held at 5% across an item: the held projection, the Button model beside it, and the return's fractions.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\titem2 19.167 158.333 412.083 39.167 tilt 19.167 158.333 412.083 39.167\n')
            f.write('0.0\tlayout\titem1 19.167 119.167 412.083 39.167 tilt 19.167 119.167 412.083 39.167\n')
            f.write('100.0\tstep\thold item2 at 0.05 0.5\n')
            # The end of the last touch's return, on item 1, lands after this touch's step.
            f.write('150.0\tvalue\tprojection item1 rot -0.1 0 0 centre 0.5 0.5 0 local 0 0 0 global 0 0 -1\n')
            f.write('165.0\tvalue\tprojection item1 none\n')
            f.write('170.0\tvalue\tprojection item2 none\n')
            f.write('1000.0\tpressed\tlist 39.771 177.917 handled ts 1\n')
            f.write('1000.5\tstate\titem2 CommonStates Normal > Pressed\n')
            proj = '%.1f\tvalue\tprojection item2 rot %g %g 0 centre 0.5 0.5 0 local 0 0 0 global 0 0 %g\n'
            f.write(proj % (1030.0, -2.63, 8.09, -9.41))
            f.write('1600.0\treleased\tlist 39.771 177.917 handled ts 2\n')
            for k, frac in enumerate([0.9412, 0.8039, 0.5]):
                f.write(proj % (1788.0 + 15 * k, -2.63 * frac, 8.09 * frac, -9.41 * frac))
            f.write('1900.0\tvalue\tprojection item2 none\n')
        code, out = run_script('listpress.py', run)
    assert code == 0, out
    assert 'held: RotationX -2.63, RotationY 8.09, GlobalOffsetZ -9.41' in out, out
    assert 'the Button model at -0.450, 0.000: 0.00, 8.01' in out, out
    assert '+188.0 0.9412; +203.0 0.8039; +218.0 0.5000; +300.0 rest' in out, out
    assert 'item2: projection first changes at +30.0 ms' in out, out
    # The check mode (#136): a press on the box goes to CheckboxPressed and is read on the box, against the box's bounds;
    # it is back at rest on the first frame after the release.
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\titem1 19.167 119.167 412.083 39.167 tilt 19.167 119.167 412.083 39.167 box 19.167 128.75 25.417 25.417\n')
            f.write('100.0\tstep\thold item1 on the box\n')
            f.write('1000.0\tpressed\tlist 31.88 141.46 handled ts 1\n')
            f.write('1000.9\tstate\titem1 CommonStates Normal > CheckboxPressed\n')
            f.write('1030.0\tvalue\tprojection box1 rot -2.7 0.146 0 centre 0.5 0.5 0 local 0 0 0 global 0.167 -209 -20.86\n')
            # A later move to Pressed doesn't take the read off the box: the first state the press went to counts.
            f.write('1100.0\tstate\titem1 CommonStates CheckboxPressed > Pressed\n')
            f.write('1600.0\treleased\tlist 31.88 141.46 handled ts 2\n')
            f.write('1601.0\tvalue\tprojection box1 rot 0 0 0 centre 0.5 0.5 0 local 0 0 0 global 0.167 -209 0\n')
        code, out = run_script('listpress.py', run)
    assert code == 0, out
    assert 'box1: projection first changes at +30.0 ms' in out, out
    assert 'held: RotationX -2.70, RotationY 0.15, GlobalOffsetZ -20.86; the Button model at 0.000, 0.000' in out, out
    assert 'after the release (ms from it, fraction of the held): +1.0 0.0000' in out, out


@check
def torn_frames_and_off_ladder():
    # A turn's frame fits one rotation; a frame grabbed mid-draw, half one angle and half another, fits none.
    d, cx = 1000.0, 225.0
    whole = ml.project_y(-43.1, -44, -99, d, cx)
    bad, th, rms = ml.torn_frame(whole, -44, -99, d, cx)
    assert not bad and abs(th + 43.1) < 1e-3, (bad, th, rms)
    a, b = ml.project_y(-67.0, -44, -99, d, cx), ml.project_y(-22.3, -44, -99, d, cx)
    assert ml.torn_frame([a[0], b[1], a[2], b[3]], -44, -99, d, cx)[0]
    assert not ml.torn_frame([a[0], None, None, None], -44, -99, d, cx)[0]  # clipped: never torn
    # A ladder of steps a 0.8 ratio apart, with one value caught between two of them.
    vals = [-67.0 * 0.8 ** k for k in range(12)]
    # -6.5 between steps 10 (-7.19) and 11 (-5.75), which are a step apart, in the order the list has them.
    extra = vals[:11] + [-6.5] + vals[11:]
    assert ml.off_ladder(vals) == [], ml.off_ladder(vals)
    assert ml.off_ladder(extra) == [-6.5], ml.off_ladder(extra)
    # Each turn's tick 0: its first frame's time less its tick's steps; a first value on no step is left out.
    times, left = ml.tick0_times([(10.0, -53.8), (5.0, -67.0), (40.0, -43.12), (3.0, -60.0)], [-67.0, -53.8, -43.1], [0, 1, 2])
    assert left == 1 and [round(t, 3) for t in times] == [-5.625, 5.0, 8.75], (times, left)


@check
def pivot_header_and_slide():
    # A Pivot swipe: the header moves 0.237 px a px of offset until it jumps; the new item slides in by an
    # ExponentialEase 4 over 400 ms. A Hub: the header at 1/18 of the offset.
    import tempfile
    f4 = ml.expo_out(4)
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            line = '%.1f\tframe\trt %g offset %g index %d panel [p] items [translate %g 0] itemsAt %g | one %g op 1 cp none | two %g op 1 cp none\n'
            f.write('0.0\tlayout\theader one 15.84 115.4 90.83 82.5\n0.0\tlayout\theader two 122.51 115.4 87.92 82.5\n')
            # As in a real run, the frame at rest is logged before the touch's step, not in its segment.
            f.write(line % (50.0, 1000, 1000, 0, 1000, 0, 15.84, 122.51))
            f.write('100.0\tstep\tswipe left\n')
            f.write('200.0\tpressed\tpivot 380 450 ts 1\n')
            f.write('210.0\tcapturelost\tpivot 380 450 ts 1\n')
            for k in range(1, 9):
                off = 1000 + 40 * k
                f.write(line % (200.0 + 15 * k, 1000 + 15.625 * k, off, 0, 1000, 1000 - off, 15.84 - 0.23706 * (off - 1000), 122.51 - 0.23706 * (off - 1000)))
            f.write('340.0\tselection\tindex 1\n')
            for k in range(0, 27):
                frac = 1 - f4(min(1.0, k * ml.TICK / 400))
                f.write(line % (345.0 + 15 * k, 1140.625 + 15.625 * k, 1450, 1, 1450 + 450 * frac, 450 * frac, 402, 15.83))
        code, out = run_script('pivot.py', run)
    assert code == 0, out
    assert 'header "one" moves 0.23706 px a px of offset' in out, out
    assert 'slides in from 450.00 px: at 0 after 406.' in out and '16:0.8526 31:0.7266' in out, out
    assert 'offset +450.00' in out, out  # from the frame at rest, logged before the step
    # A header wider than the screen (#136): its pitch is 744.58, so it moves 1.6546 px a px, over 100 px a frame
    # here; then it jumps to the end, the way the offset went.
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            line = '%.1f\tframe\trt %g offset %g index %d panel [p] items [translate %g 0] itemsAt %g | wide %g op 1 cp none | one %g op 1 cp none\n'
            f.write('0.0\tlayout\titems wide one\n')
            f.write(line % (50.0, 1000, 1000, 0, 1000, 0, 15.84, 760.42))
            f.write('100.0\tstep\tswipe left\n')
            f.write('200.0\tpressed\tpivot 380 450 ts 1\n')
            f.write('210.0\tcapturelost\tpivot 380 450 ts 1\n')
            for k in range(1, 6):
                off = 1000 + 70 * k
                d = 1.6546 * (off - 1000)
                f.write(line % (200.0 + 15 * k, 1000 + 15.625 * k, off, 0, 1000, 1000 - off, 15.84 - d, 760.42 - d))
            # The reorder can come on a frame where the offset is still or goes back a little: the jump still ends the line.
            f.write(line % (280.0, 1078.125, 1340, 0, 1000, -340, 15.84 - 1.6546 * 350 + 1200, 760.42 - 1.6546 * 350))
            f.write('290.0\tselection\tindex 1\n')
            f.write(line % (300.0, 1093.75, 1450, 1, 1450, 0, 15.84 - 1.6546 * 450 + 1200, 15.84))
        code, out = run_script('pivot.py', run)
    assert code == 0, out
    assert 'header "wide" moves 1.65460 px a px of offset, against it, over 6 frames' in out, out
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            for k in range(40):
                off = 30 * k
                f.write('%.1f\tframe\trt %g offset %g header %g [none] | section 1 %g\n' % (k * 15.0, k * 15.625, off, 15 - off / 18.0, -off))
        code, out = run_script('pivot.py', run)
    assert code == 0 and '1 / slope = 18.000' in out, out




@check
def hub_background_and_end():
    # A Hub with a 900 px image (#136): its left edge at -offset (W - V) / (L - V) up to L - V, then one for one; the
    # touch at the end, left out of the header's line, its offset back to 0.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\theader 15 61 123.75 103.75 scroll extent 1665.417 viewport 450\n')
            for k in range(4):
                f.write('0.0\tlayout\tsection section %d %d 60 405 740\n' % (k + 1, 405 * k))
            line = '%.1f\tframe\trt %g offset %g header %g [none] | section 1 %g | Background %g w 900 none | WrapBackground 0 w 900 none\n'
            for k in range(1, 121):
                off = 10.0 * k
                bg = -off * 450 / 1170 if off <= 1170 else -450 - (off - 1170)
                f.write(line % (k * 15.0, k * 15.625, off, 15 - off / 18, -off, bg))
            f.write('2000.0\tstep\tswipe left at the end\n')
            f.write('2000.5\tsetup\toffset 1215.417\n')
            f.write(line % (2100.0, 3000, 1200, 400, -1500, -800))
            f.write(line % (2115.0, 3015.625, 0, 300, 0, 0))
        code, out = run_script('pivot.py', run)
    assert code == 0, out
    assert '1 / slope = 18.000' in out, out
    assert '(W - V) / (L - V) = 0.384615' in out, out
    assert re.search(r'offsets up to L - V: left edge = -?0.000 -0.384615 x offset', out), out
    assert re.search(r'offsets past L - V: left edge = 720.000 -1.000000 x offset', out), out
    assert 'swipe left at the end: offset 1200.0 at the start; back to 0 at 115 ms' in out, out

@check
def appbar_motion():
    # A synthetic -Probe appbar -Record run: the flash, then the bar's top rising from 737.08 to 514.58 view px after
    # the bar's Opened, the rest of the way falling by e every 80 ms, on frames 25 ms apart; and the log's events.
    import math
    import tempfile
    w, h, scale = 540, 960, 1.2

    def top(k):
        # At rest once less than a raw pixel is left, as a drawn bar is.
        left = 222.5 * math.exp(-(k - 20) * 25 / 80.0)
        return 737.08 if k <= 20 else 514.58 + (left if left > 0.42 else 0)

    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'rect.log'), 'w') as f:
            f.write('x display window at 0,0, %d x %d\n' % (w, h))
        times = [k / 40 for k in range(80)]
        with open(os.path.join(run, 'frames.csv'), 'w') as f:
            f.write(''.join('%.6f\n' % t for t in times))
        with open(os.path.join(run, 'probe.txt'), 'w') as f:
            f.write('1000.0\tflash\ton\n1100.0\tstep\ttap the ellipsis, bar closed\n1100.5\tsetup\tCompact, closed\n')
            f.write('1480.0\tbar\tOpened\n')
        os.makedirs(os.path.join(run, 'analysis'))
        with open(os.path.join(run, 'analysis', 'left.gray'), 'wb') as fg:
            for k in range(len(times)):
                col = bytearray(h)
                if k == 2:
                    for y in range(int(60 * scale)):
                        col[y] = 255
                for y in range(int(round(top(k) * scale)), h):
                    col[y] = 31
                fg.write(bytes(v for v in col for _ in range(12)))
        code, out = run_script('appbar.py', run)
    assert code == 0, out
    assert 'the flash pairs app time 1000.0 ms with recording 0.050 s' in out, out
    # Frame 21, the first change, is at 1475 ms by the flash: 5 ms before Opened.
    assert '736.67 to 514.17; the first change -5 ms from bar at 1480 (Opened)' in out, out
    m = re.search(r'falls by e every (\d+) ms', out)
    assert m, out
    near(int(m.group(1)), 80, 6, 'the time constant')
    assert 'tap the ellipsis, bar closed: setup Compact, closed; bar Opened' in out, out


@check
def release_decisions_and_a_second_press():
    # A synthetic -Probe pivotrelease run (#148). The start tap and two touches in touch.log; the app logs a press only
    # for the first touch (the second's is held by the ScrollViewer), 5 ms after its planned time, so touch.log's times
    # go onto the app's clock by the go line and that lag. Touch 1: a drag to +180 whose last 20 px reach the phone with
    # the release, then back (FinalView the start). Touch 2: a swipe on (FinalView +450), and a press 100 ms after its
    # release, held still 600 ms. Item events after the selection.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'touch.log'), 'w', encoding='utf-8') as f:
            f.write('header line\n    46.1 ms (planned      0) down 30 30 -> 36,36\n    86.1 ms (planned     80) up 30 30 -> 36,36\n')
            f.write('  2046.1 ms (planned   2000) down 400 450 -> 480,540\n  2446.1 ms (planned   2400) move 220 450 -> 1,1\n')
            f.write('  2846.1 ms (planned   2800) up 200 450 -> 240,540\n')
            f.write('  6046.1 ms (planned   6000) down 380 450 -> 456,540\n  6196.1 ms (planned   6150) up 180 450 -> 216,540\n')
            f.write('  6296.1 ms (planned   6250) down 225 450 -> 270,540\n  6896.1 ms (planned   6850) up 225 450 -> 270,540\n')
        vc = '%.1f\tviewchanging\tnext %g final %g inertial %s\n'
        fr = '%.1f\tframe\trt %g offset %g index %d panel none items none itemsAt 0\n'
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('1000.0\tgo\ttap at 30 30\n')
            f.write('1800.0\tstep\tslow left 200 px, held\n')
            f.write(fr % (2900.0, 100, 900, 0))
            f.write('3005.0\tpressed\tpivot 400 450 ts 1\n3030.0\tcapturelost\tpivot 400 450 ts 1\n')
            for k in range(1, 19):
                f.write(vc % (3000 + 20 * k, 900 + 10 * k, 900 + 10 * k, 'False'))
            f.write(vc % (3810.0, 1100, 900, 'True'))
            f.write(fr % (3812.0, 200, 1100, 0))
            f.write(fr % (4300.0, 300, 900, 0))
            f.write('5800.0\tstep\tswipe left, then a press held 600 ms 100 ms after the release\n')
            for k in range(1, 8):
                f.write(vc % (7000 + 20 * k, 900 + 25 * k, 900 + 25 * k, 'False'))
            f.write(vc % (7155.0, 1100, 1350, 'True'))
            f.write('7160.0\tselection\tindex 1\n7161.0\tpivotitem\tUnloading one, index 1\n7161.5\tpivotitem\tLoaded two, index 1\n')
            f.write(vc % (7240.0, 1200, 1350, 'True'))
            f.write(fr % (7250.0, 400, 1200, 1))
            f.write(fr % (7500.0, 420, 1200, 1))
            f.write(vc % (7860.0, 1250, 1350, 'True'))
            f.write(fr % (8100.0, 500, 1350, 1))
        code, out = run_script('release.py', run)
    assert code == 0, out
    # Touch 1: pressed at 3005 by the lag, released 800 ms later; the release's offset is the inertial line's +200, not
    # the last drag line's +180; the speed over the drag's last 100 ms 0.5 px a ms; FinalView the start: back.
    assert re.search(r'slow left 200 px, held +800 +\+200\.0 +0\.500 +\+5 +\+0\.0 back, index 0', out), out
    assert re.search(r'then a press held 600 ms 100 ms after the release +150 +\+200\.0 +1\.250 +\+0 +\+450\.0 on \+1, index 1', out), out
    assert ('the second press 100 ms after the release, the offset +300.0 from the start; it moved 0.0 px during the '
            '600 ms press; inertial again 5 ms after its release; at the end +450.0') in out, out
    assert '+1.0  Unloading one, index 1' in out and '+1.5  Loaded two, index 1' in out, out


@check
def release_on_a_hub():
    # A synthetic -Probe hubrelease run (#148): touch.log with a decimal comma, as a VM in such a culture writes it; the
    # page the sections' pitch, 380 here, read from the frames (the first frame's layout is mid-entrance, 300); a swipe
    # right at the first section that loops to the end, which is no count of pages; a stray press far from any touch,
    # left out of the lag; and ScrollToSection, whose frames count only those with a new offset.
    import tempfile
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'touch.log'), 'w', encoding='utf-8') as f:
            f.write('header line\n    46,1 ms (planned      0) down 30 30 -> 36,36\n    86,1 ms (planned     80) up 30 30 -> 36,36\n')
            f.write('  2046,1 ms (planned   2000) down 70 500 -> 84,600\n  2196,1 ms (planned   2150) up 270 500 -> 324,600\n')
        fr = '%.1f\tframe\trt %g offset %g header 15 [none] | section 1 %g | section 2 %g | section 3 0 | section 4 0\n'
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tsection section 1 5 76 300 708\n')
            f.write('1000.0\tgo\ttap at 30 30\n')
            f.write('1800.0\tstep\tfirst section: swipe right\n')
            f.write(fr % (2900.0, 100, 0, 0, 380))
            f.write('3000.0\tpressed\thub 70 500 ts 1\n2400.0\tpressed\thub 1 1 ts 2\n')
            f.write(fr % (3050.0, 105, 0, 40, -1100))
            f.write('3400.0\tviewchanging\tnext 0 final 1140 inertial True\n')
            f.write(fr % (3900.0, 200, 1140, -1140, -760))
            f.write('5000.0\tstep\tScrollToSection 2\n5000.5\tsetup\tScrollToSection 2, offset 1140 before the call, 380 after\n')
            f.write(fr % (5010.0, 300, 380, -380, 0))
            f.write(fr % (5030.0, 316, 380, -380, 0.5))
        code, out = run_script('release.py', run)
    assert code == 0, out
    assert 'a Hub, a page 380 px' in out, out
    assert 'a press at 2400.0 ms is 600 ms from the nearest touch in touch.log: left out of the lag' in out, out
    assert re.search(r'first section: swipe right +150 .* to \+1140\.0 \(a loop, not \+3 pages\)', out), out
    assert 'ScrollToSection 2: ScrollToSection 2, offset 1140 before the call, 380 after; 1 frames with a new offset: 10:380.0' in out, out


@check
def hub_entrance_fit():
    # A synthetic -Probe hubentrance run (#148): two Hubs placed, each turning from RotationY -90 by an ExponentialEase
    # of 5 over 750 ms (48 ticks), the second with a frame missing; the fit finds 5 and 750 ms, and the two the same.
    import tempfile
    f = ml.expo_out(5)
    line = ('%.1f\tframe\trt %g hub proj rot 0 %g 0 centreX -0.5 localX 0 global 0 0 0 | op 1 hubop 1 | header 15 flyin 0'
            ' | section1 0 | bg op 1 0 x 800 at 0\n')
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as fp:
            for n, t0 in enumerate((1000.0, 5000.0)):
                fp.write('%.1f\tstep\tplace a Hub, %d\n' % (t0, n))
                fp.write(line % (t0 + 5, t0, 0))
                for k in range(49):
                    if n == 1 and k == 3:
                        continue
                    v = -90 * (1 - f(k * ml.TICK / 750)) if k < 48 else 0
                    fp.write(line % (t0 + 20 + 15 * k, t0 + ml.TICK * (k + 1), round(v, 3)))
        code, out = run_script('entrance.py', run)
    assert code == 0, out
    assert 'RotationY -90 at tick 0, 0 at tick 48 (750.0 ms of rt)' in out, out
    assert 'ExponentialEase 5.00 over 750.0 ms, the first frame its start' in out, out
    assert 'The 2 entrances have the same RotationY on every tick each shows' in out, out
    # A turn the segment cuts off before 0 isn't fitted.
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as fp:
            fp.write('5000.0\tstep\tplace a Hub, cut off\n')
            for k in range(20):
                fp.write(line % (5020 + 15 * k, 5000 + ml.TICK * k, round(-90 * (1 - f(k * ml.TICK / 750)), 3)))
        code, out = run_script('entrance.py', run)
    assert code == 0, out
    assert 'place a Hub, cut off: RotationY -90 at tick 0, never back to 0 in the segment' in out, out


def band_frame(bands, w=540, h=960, d=999.0, cy=400.0, cx=225.0, level=31):
    """A synthetic recorded frame of full-width bands on black: each (top, bottom, angle) at rest from top to bottom
    (view px), turned by angle degrees about its centre line (positive: the lower edge nearer), seen by PlaneProjection's
    camera and cut by the screen's sides. Each row is filled where a band covers its middle, with the edges' coverage."""
    s = ml.VIEW_W / w
    img = bytearray(w * h)
    for top, bottom, ang in bands:
        c, half, r = (top + bottom) / 2, (bottom - top) / 2, math.radians(ang)
        for k in range(2001):
            u = -half + 2 * half * k / 2000
            y, z = c + u * math.cos(r), u * math.sin(r)
            q = d / (d - z)
            yp = cy + (y - cy) * q
            row = int(yp / s)
            if not 0 <= row < h:
                continue
            l, rr = (cx - 225 * q) / s, (cx + 225 * q) / s
            for x in range(w):
                cover = max(0.0, min(x + 1, rr) - max(x, l))
                if cover > 0:
                    img[row * w + x] = max(img[row * w + x], int(round(level * min(cover, 1.0))))
    return bytes(img), w, h, s


@check
def strips_band_about_its_centre():
    # A band 100 px tall at rest from 100 to 200, turned 40 degrees about its centre: the pieces' line crosses the
    # screen's plane at its centre, the angle comes back, and the near half, which the screen's sides cut, ends where
    # the band's lower edge is. A second band, at rest, adds nothing.
    import strips
    frame, w, h, s = band_frame([(100, 200, 40.0), (200, 300, 0.0)])
    page = bytes(w * h)
    pieces = strips.trapezoids(strips.row_edges(frame, page, w, h), w)
    turned = [strips.strip(p, s, 450.0) for p in pieces]
    assert len(turned) == 1, turned
    yt, zt, yb, zb, ang, cross, nend = turned[0]
    near(ang, 40.0, 1.0, 'the angle')
    near(cross, 150.0, 1.5, 'the line the band turns about')
    near(yt, 150 - 50 * math.cos(math.radians(40)), 1.5, 'the far edge y')
    near(zt, -50 * math.sin(math.radians(40)), 2.0, 'the far edge z')
    assert nend is not None
    near(nend[0], 150 + 50 * math.cos(math.radians(40)), 2.5, 'the near end y')
    near(nend[1], 50 * math.sin(math.radians(40)), 3.0, 'the near end z')
    # Turned the other way, the far half is the lower one, and nothing is cut below it.
    frame, w, h, s = band_frame([(500, 600, -30.0)])
    turned = [strips.strip(p, s, 450.0) for p in strips.trapezoids(strips.row_edges(frame, page, w, h), w)]
    assert len(turned) == 1, turned
    near(turned[0][4], -30.0, 1.0, 'a negative angle')
    near(turned[0][5], 550.0, 1.5, 'its line')
    # The unprojection undoes project().
    yp, wp, _, _ = strips.project(120.0, -30.0, 180.0, 30.0, 450.0)
    y, z = strips.unproject(yp, wp, 450.0)
    near(y, 120.0, 1e-9, 'unproject y')
    near(z, -30.0, 1e-9, 'unproject z')


@check
def strips_bands_and_their_eases():
    # Readings of two bands from several calls: bands() finds each band by its line, and the opening's distinct
    # angles fit the ExponentialEase they were made with, one a tick.
    import strips
    f = ml.expo_out(4)
    readings = []
    for call in range(3):
        for k in range(0, 20):
            if (k + call) % 3 == 0:
                continue
            a = 80 * (1 - f(k * ml.TICK / 300)) if k * ml.TICK < 300 else 0.0
            readings.append((k * 15.625, (0, 0, 0, 0, a, 50.0 + 0.3 * call, None)))
            readings.append((k * 15.625 + 30, (0, 0, 0, 0, a, 150.0 - 0.3 * call, None)))
    found = strips.bands(readings)
    assert [round(c) for c, _ in found] == [50, 150], found
    vals = ml.distinct([x[4] for _, x in readings if abs(x[5] - 50) < 8], decimals=1, tol=0.6)
    assert len(vals) >= 15, vals
    e, d, ph, A = ml.fit_scaled([abs(v) for v in vals if v > 0.5], list(range(len([v for v in vals if v > 0.5]))), f,
                                durations=range(100, 601, 5), phases=[i / 5 for i in range(-10, 51)])
    near(d, 300, 10, 'the duration')
    near(A, 80, 1.5, 'the start angle')


@check
def strips_steps_and_edge_on():
    # The turn a tick from distinct angles with a tick no call showed (a gap of two), and a band at 80 degrees, whose
    # far edge shows below its near one, read as 80, not -100.
    import strips
    rate, gaps = strips.steps([80.0, 69.05, 47.15, 36.2, 25.25])
    near(rate, 10.95, 0.01, 'the turn a tick')
    assert gaps == [1, 2, 1, 1], gaps
    frame, w, h, s = band_frame([(0, 100, 80.0)])
    turned = [strips.strip(p, s, 450.0) for p in strips.trapezoids(strips.row_edges(frame, bytes(w * h), w, h), w)]
    assert turned and all(t[4] > 0 for t in turned), turned


@check
def looping_snap_coast_recentring_and_fade():
    # A synthetic -Probe looping run (#149): a month drag let go 45 px short of the next item, which then slows at a
    # constant deceleration to it over 280 ms and recentres by one item; and the day column's unselected items fading
    # out over 21.3 frames, 0.0469 a frame.
    import tempfile
    pitch, start = 138.375, 69325.875
    with tempfile.TemporaryDirectory() as run:
        with open(os.path.join(run, 'touch.log'), 'w', encoding='utf-8') as f:
            f.write('header line\n    46.1 ms (planned      0) down 30 30 -> 36,36\n    86.1 ms (planned     80) up 30 30 -> 36,36\n')
            f.write('  2046.1 ms (planned   2000) down 86 387 -> 104,464\n  2846.1 ms (planned   2800) up 86 294 -> 104,350\n')
        fin = start + pitch
        with open(os.path.join(run, 'probe.txt'), 'w', encoding='utf-8') as f:
            f.write('0.0\tlayout\tselector 0 17.5 37 138 700 item 123 x 123 loop True items 12\n')
            f.write('1000.0\tgo\ttap at 30 30\n')
            f.write('1800.0\tstep\tmonth drag up, held\n')
            f.write('3005.0\tpressed\tsel0 86 387 handled ts 1\n')
            fr = '%.1f\tframe\trt %g sel0 offset %.3f items 6@%.3f:1x1.125 7@%.3f:1x1.125\n'
            f.write(fr % (2900.0, 1000.0, start, 4.0, 4.0 + pitch))
            for k in range(1, 6):
                f.write('%.1f\tviewchanging\tnext %.3f final %.3f inertial False sel0\n' % (3500 + 50 * k, start + 18.6 * k, start + 18.6 * k))
            off0 = start + 93.375
            f.write('%.1f\tviewchanging\tnext %.3f final %.3f inertial True sel0\n' % (3812.0, off0, fin))
            T = 280.0
            for k in range(0, 19):
                t = k * ml.TICK
                u = min(t / T, 1.0)
                off = fin - 45.0 * (1 - u) ** 2
                f.write(fr % (3813.0 + 15 * k, 2000.0 + t, off, 4.0 - (off - start), 4.0 + pitch - (off - start)))
            f.write('4200.0\tevent\tsel0 SelectionChanged 7 rt 2328.125\n')
            f.write(fr % (4210.0, 2343.75, start, 4.0, 4.0 + pitch))
            fr1 = '%.1f\tframe\trt %g sel1 offset %.3f items 14@4:%.3fx1.125 15@142:1x1.125 16@280:%.3fx1.125 17@418:%.3fx1.125\n'
            f.write(fr1 % (5000.0, 3000.0, start, 1, 1, 1))
            for k in range(1, 23):
                o = max(0.0, 1 - k * ml.TICK / 333.0)
                f.write(fr1 % (5100.0 + 15 * k, 3100.0 + k * ml.TICK, start, o, o, o))
        code, out = run_script('looping.py', run)
    assert code == 0, out
    assert 'FinalView +138.4 px from the press (+1.000 items)' in out, out
    assert re.search(r'constant deceleration \(quadratic ease-out\) over 2[78]\d ms', out), out
    assert 'recentred at rt 2343.750: the offset jumps -138.375 px (-1.000 items)' in out, out
    assert re.search(r'sel1 fade from rt [\d.]+: 1\.000 to 0\.000 over 2\d frames \(\d+ ms\), -0\.04[67]\d a frame', out), out


@check
def looping_and_strips_edges():
    # The first review's cases (#149): a fade with one value repeated (two frames on one tick) is one fade; an item
    # logged with opacity NaN is kept; a run of one frame isn't fitted; and a band whose angles end at 0.0 and aren't
    # linear is reported, not stopped by the ratio test.
    import looping
    import strips
    items = lambda o: [(1.0, 0.0, 1.0, 1.0), (2.0, 138.0, o, 1.0), (3.0, 276.0, o, 1.0)]
    ops = [1.0, 0.95, 0.9, 0.9, 0.85, 0.8, 0.75]
    fs = [(100.0 + 15 * k, 1000.0 + ml.TICK * k, 0.0, items(o)) for k, o in enumerate(ops)]
    runs = looping.fades(fs)
    assert len(runs) == 1 and runs[0][-1][1] == 0.75, runs
    m = looping.FRAME.match('rt 1 sel0 offset 5 items 7@1:NaN 8@139:0x1.125')
    got = [x for x in looping.ITEM.findall(m.group(4))]
    assert len(got) == 2 and got[0][2] == 'NaN', got
    assert looping.best_curves([(0.0, 100.0)], 50.0) == []
    import contextlib
    import io as _io
    buf = _io.StringIO()
    readings = []
    for call in range(3):
        for k, a in enumerate([45.0, 35.6, 27.7, 21.1, 15.7, 11.4, 7.8, 5.2, 0.0]):
            readings.append((k * 15.6, (0, 0, 0, 0, a + 0.01 * call, 96.9, None)))
    with contextlib.redirect_stdout(buf):
        strips.band_report('dialog', 'open', readings, 3, 1.0)
    assert 'band about y 96.9' in buf.getvalue() and 'eased' in buf.getvalue(), buf.getvalue()


for fn in checks:
    fn()
    print('ok  %s' % fn.__name__)
print('%d checks passed' % len(checks))
