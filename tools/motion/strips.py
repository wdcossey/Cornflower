"""The popups' swing, strip by strip, from a `-Probe popupcurves -Record` run (#149, gap register B11 and B12).

Usage: python -I strips.py <run folder> [--popup <name>] [--lag <s>] [--frames]

The shell opens and closes a popup by cutting it into horizontal strips (a top Flyout is one; a full Flyout, the
dialog and the full-screen pickers are several: a header, each list item, slices of what is left) and swinging each
about a horizontal line, one after another. The app can't read it (a theme transition), so it is read here from the
recording, which the header band's flash at the start tap puts on the app's clock (as in scrollend.py), or --lag
(recording time = app time - lag, in s).

For each call in the log ("call <name> open|close"), every recorded frame from 40 ms before it to 900 ms after is
compared with the page: the frame just before an opening, or 1.2 s after a closing. In each row, the first and last
pixel that differ from the page by more than 10 (sub-pixel, by the edge pixel's share of the next one's difference) are
the popup's left and right edge there. The rows are cut into trapezoids where an edge stops being a straight line
(within 1.2 recording px). Each trapezoid's top and bottom edges are taken back off the screen with PlaneProjection's
camera (camera.py: an eye 999 view px in front of the screen, looking at y 400; one camera for every band, as the
closing bands' steps are the same size, 10.95 degrees, only with it): an edge W view px wide at rest that shows w wide
at screen y' lies at z = d (1 - W / w), y = 400 + (y' - 400) W / w. W is the popup's width at rest. From
the two edges: the strip's angle (degrees; positive when its bottom edge is nearer the eye), its length, and where
each edge is.

It prints, for each popup and each call, the strips of every frame (top and bottom y and z, angle) with --frames; and
for each strip, named by its top edge at rest, its distinct angles over all the calls, in order, as motionlib's distinct
values: one per 15.625 ms step of the animation clock where many calls are pooled.

Python 3 standard library only.
"""
import math
import os
import statistics
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

EYE = 999.0  # view px: PlaneProjection's camera (camera.py)
CY = 400.0  # view px: the camera looks at the middle of the 800 high view
THR = 10  # a pixel differs from the page by more than this
TOL = 1.2  # recording px: an edge that bends more than this starts a new trapezoid
MIN_ROWS = 4
HEADER = 60.0  # view px: the header band that flashes


def row_edges(frame, page, w, h, thr=THR):
    """Each row's (left, right) edge, in recording px, sub-pixel, or None where the row is the page's."""
    out = []
    for y in range(h):
        a, b = frame[y * w:(y + 1) * w], page[y * w:(y + 1) * w]
        if a == b:
            out.append(None)
            continue
        xl = next((x for x in range(w) if abs(a[x] - b[x]) > thr), None)
        if xl is None:
            out.append(None)
            continue
        xr = next(x for x in range(w - 1, -1, -1) if abs(a[x] - b[x]) > thr)
        out.append((_sub(a, b, xl, 1), _sub(a, b, xr, -1) + 1))
    return out


def _sub(a, b, x, step):
    """The edge's sub-pixel position at pixel x, which is the first (step 1) or last (step -1) to differ: the pixel's
    share of the next one in's difference is the share of it the popup covers."""
    full = abs(a[x + step] - b[x + step]) if 0 <= x + step < len(a) else 0
    part = abs(a[x] - b[x])
    frac = min(1.0, part / full) if full > THR else 1.0
    return x + (1 - frac) if step > 0 else x - (1 - frac)


def _line(ys, vs):
    """Least squares v = a + b y."""
    return ml.linfit(ys, vs)


def _worst(ys, vs):
    """The row, among ys, whose v is furthest from the straight line through the first and last, and how far."""
    y0, y1, v0, v1 = ys[0], ys[-1], vs[0], vs[-1]
    best, k = 0.0, None
    for i in range(1, len(ys) - 1):
        f = v0 + (v1 - v0) * (ys[i] - y0) / (y1 - y0)
        d = abs(vs[i] - f)
        if d > best:
            best, k = d, i
    return k, best


def trapezoids(edges, w, tol=TOL, min_rows=MIN_ROWS):
    """The slanted pieces in a frame's row edges: (top row, bottom row, left at top, right at top, left at bottom, right
    at bottom, rows below it that the screen's sides cut), each edge a least-squares line over its rows, in recording
    px; the bottom row's edge is the next row's top (the piece's bottom edge lies between). A row whose edges are both at
    the screen's sides is cut by them (or at rest, full width): it says nothing of its depth, so pieces are made of the
    other rows, cut where either edge bends more than tol; a run of cut rows straight after a piece is counted with it."""
    runs, cur = [], []
    for y, e in enumerate(edges):
        cut = e is not None and e[0] < 0.3 and e[1] > w - 0.3
        if e is None or cut:
            if cur:
                runs.append(cur)
            cur = []
        else:
            cur.append((y, e[0], e[1]))
    if cur:
        runs.append(cur)
    out = []

    def cut_below(y):
        n = 0
        while y + n < len(edges) and edges[y + n] is not None and edges[y + n][0] < 0.3 and edges[y + n][1] > w - 0.3:
            n += 1
        return n

    def split(rows):
        if len(rows) < min_rows:
            return
        ys = [r[0] for r in rows]
        kl, dl = _worst(ys, [r[1] for r in rows])
        kr, dr = _worst(ys, [r[2] for r in rows])
        k, d = (kl, dl) if dl >= dr else (kr, dr)
        if d > tol and k is not None:
            # The bend is between row k and its neighbour: try both cuts and keep the straighter.
            cuts = [c for c in (k, k + 1) if 0 < c < len(rows)]
            best = min(cuts, key=lambda c: _bend(rows[:c]) + _bend(rows[c:]))
            split(rows[:best])
            split(rows[best:])
            return
        al, bl = _line(ys, [r[1] for r in rows])
        ar, br = _line(ys, [r[2] for r in rows])
        y0, y1 = ys[0], ys[-1] + 1
        out.append((y0, y1, al + bl * y0, ar + br * y0, al + bl * y1, ar + br * y1, cut_below(y1)))

    for r in runs:
        split(r)
    return out


def _bend(rows):
    if len(rows) < 3:
        return 0.0
    ys = [r[0] for r in rows]
    return max(_worst(ys, [r[1] for r in rows])[1], _worst(ys, [r[2] for r in rows])[1])


def unproject(yp, wp, width, d=EYE, cy=CY):
    """An edge `width` view px wide at rest, seen wp wide at screen y yp (view px): its (y, z) in front of the screen."""
    k = width / wp
    return cy + (yp - cy) * k, d * (1 - k)


def strip(trap, s, width, d=EYE, cy=CY):
    """A slanted piece (recording px) as a strip: (top y, top z, bottom y, bottom z, angle in degrees, the y where it
    crosses the screen's plane, and its near end's y and z where the screen's sides cut rows below it), view px; the
    angle is positive when the lower edge is nearer the eye. The near end is where the
    line through the two edges meets the ray to the last cut row's bottom (None when no row is cut)."""
    y0, y1, l0, r0, l1, r1, below = trap
    w0, w1 = (r0 - l0) * s, (r1 - l1) * s
    yt, zt = unproject(y0 * s, w0, width, d, cy)
    yb, zb = unproject(y1 * s, w1, width, d, cy)
    ang = math.atan2(zb - zt, yb - yt)
    near = None
    if below:
        target = (y1 + below) * s
        # A point (yt + u cos, zt + u sin) shows at cy + (y - cy) d / (d - z): solve for u.
        c, sn = math.cos(ang), math.sin(ang)
        den = d * c + (target - cy) * sn
        if abs(den) > 1e-9:
            u = ((target - cy) * (d - zt) - (yt - cy) * d) / den
            near = (yt + u * c, zt + u * sn)
    cross = yt - zt * (yb - yt) / (zb - zt) if abs(zb - zt) > 1e-9 else float('nan')
    deg = math.degrees(ang)
    # Past edge-on (a band at 80 degrees shows its far edge below its near one): the same line, the other way round.
    deg = deg + 180 if deg < -90 else deg - 180 if deg > 90 else deg
    return yt, zt, yb, zb, deg, cross, near


def project(yt, zt, yb, zb, width, d=EYE, cy=CY, xc=225.0):
    """The inverse of strip(), for the self-test: a strip's top and bottom edges (y, z) seen on the screen, as
    (top y', width', bottom y', width'), view px."""
    out = []
    for y, z in ((yt, zt), (yb, zb)):
        k = d / (d - z)
        out += [cy + (y - cy) * k, width * k]
    return out


def flash_time(rec):
    """The recording's time of the first frame whose header band is white."""
    y0, y1 = int(10 / rec.s), int((HEADER - 10) / rec.s)
    for i, t in enumerate(rec.times):
        if all(b > 200 for b in rec.row(i, (y0 + y1) // 2)[20:rec.w - 20:7]):
            return t
    raise SystemExit('no frame shows the header flash')


def calls(run):
    """(app ms, name, open|close) for each "call" line."""
    out = []
    for t, what, d in ml.probe(run):
        if what == 'call':
            words = d.split(' rt ')[0].rsplit(' ', 1)
            out.append((t, words[0], words[1]))
    return out


def bands(readings, tol=8.0, min_angle=4.0):
    """The bands' centres: the y (view px) where the pieces' lines cross the screen's plane, over every piece turned by
    more than min_angle degrees, clustered within tol px. Each band turns about that line."""
    ys = sorted(x[5] for _, x in readings if abs(x[4]) >= min_angle and x[5] == x[5])
    out, cur = [], []
    for y in ys:
        if cur and y - cur[-1] > tol:
            out.append(cur)
            cur = []
        cur.append(y)
    if cur:
        out.append(cur)
    return [(ml.median(c), len(c)) for c in out if len(c) >= 3]


def steps(vals):
    """The size of one tick's turn from a band's distinct angles in order: the median gap, then the gaps within a third
    of it averaged (a gap of about two is a tick no call showed). Returns (degrees a tick, ticks between each pair)."""
    gaps = [abs(b - a) for a, b in zip(vals, vals[1:])]
    if not gaps:
        return float('nan'), []
    m = ml.median(gaps)
    one = [g for g in gaps if abs(g - m) < m / 3]
    m = sum(one) / len(one) if one else m
    return m, [round(g / m) for g in gaps]


def band_report(name, kind, readings, ncalls, factor):
    """Prints each band of one popup's openings or closings: the line it turns about, its distinct angles in order (one a
    tick of the 64 Hz clock), the turn a tick and whether every gap is a whole number of ticks (linear), and on the
    recording's clock, the median over the calls: when it reaches rest (an opening) or starts (a closing), from each
    reading moved along its line by the turn a tick, a tick being 15.625 ms of animation time, `factor` times a
    recording ms (the stopwatch's; README "Which clock"). Then the eases that fit its distinct angles best."""
    print('%s %s, %d calls:' % (name, kind, ncalls))
    found = [(c, n) for c, n in bands(readings) if 0 <= c <= 800]
    shown = 0
    for centre, n in found:
        mine = [(t, x) for t, x in readings if abs(x[5] - centre) <= 8.0 and x[5] == x[5] and abs(x[4]) < 90]
        vals = ml.distinct([x[4] for _, x in mine], decimals=1, tol=0.6)
        vals = sorted(vals, key=abs, reverse=(kind == 'open'))
        if len(vals) < 2:
            continue
        shown += 1
        rate, gaps = steps(vals)
        tick = ml.TICK / factor  # a tick in recording ms
        if kind == 'open':
            ends = [t + abs(x[4]) / rate * tick for t, x in mine]
            when = 'at rest %.0f ms after the call' % ml.median(ends)
        else:
            starts = [t - abs(x[4]) / rate * tick for t, x in mine]
            when = 'starting %.0f ms after the call, at 90 degrees %.0f ms after it' % (
                ml.median(starts), ml.median(starts) + 90 / rate * tick)
        print('  band about y %.1f: %d distinct angles, %s; %.2f degrees a tick (gaps in ticks %s); %s' % (
            centre, len(vals), ' '.join('%+.1f' % v for v in vals), rate, ' '.join(str(g) for g in gaps), when))
        if len(vals) < 4:
            continue
        linear = all(g in (1, 2) for g in gaps)
        if linear:
            ticks = [0]
            for g in gaps:
                ticks.append(ticks[-1] + g)
            a, b = ml.linfit(ticks, [abs(v) for v in vals])
            worst = max(abs(a + b * k - abs(v)) for k, v in zip(ticks, vals))
            print('    linear: %.2f degrees a tick, within %.2f degrees; %.1f ticks from 80 degrees to 0, %.1f from 0 to 90'
                  % (abs(b), worst, 80 / abs(b), 90 / abs(b)))
        else:
            _, ticks = ml.tick_numbers([abs(v) for v in vals])
            ml.print_fits([abs(v) for v in vals], ticks, unit=' deg')
    if not shown:
        print('  no band turns')


def main():
    (run,), opts = ml.parse_args(__doc__, options=('--popup', '--lag'), flags=('--frames',))
    with ml.Recording(run) as rec:
        if opts['--lag'] is not None:
            lag = float(opts['--lag'])
        else:
            flash = next((t for t, w, d in ml.probe(run) if w == 'flash' and d == 'on'), None)
            if flash is None:
                raise SystemExit('the log has no flash; give --lag')
            lag = flash / 1000 - flash_time(rec)
        rts = [(t, float(d.split(' rt ')[1])) for t, w, d in ml.probe(run) if w == 'call' and ' rt ' in d]
        factor = ml.linfit([x[0] for x in rts], [x[1] for x in rts])[1] if len(rts) > 2 else 1.0
        print('%s: recording time = app time - %.3f s; RenderingTime runs %.5f times the stopwatch' % (run, lag, factor))
        s = rec.s
        pooled, ncalls, over = {}, {}, {}
        for t, name, kind in calls(run):
            if opts['--popup'] and name != opts['--popup']:
                continue
            tr = t / 1000 - lag
            if kind == 'open':
                page_i = max(rec.index_at(tr - 0.03) - 1, 0)
                rest_i = rec.index_at(tr + 1.2)
            else:
                page_i = rec.index_at(tr + 1.2)
                rest_i = max(rec.index_at(tr - 0.03) - 1, 0)
            page = rec.frame(page_i)
            ncalls[(name, kind)] = ncalls.get((name, kind), 0) + 1
            rest = [e for e in row_edges(rec.frame(rest_i), page, rec.w, rec.h) if e is not None]
            if not rest:
                print('%s %s at %.0f ms: nothing differs from the page' % (name, kind, t))
                continue
            # The popup's width at rest: its rows' median (a full-screen popup's is the screen's).
            width = statistics.median(r[1] - r[0] for r in rest) * s
            if opts['--frames']:
                print('%s %s at %.0f ms (recording %.3f s), %.1f wide at rest' % (name, kind, t, tr, width))
            # When it is over: the last frame that differs from its end (rest after an opening, the page after a closing),
            # by more than 1 level on average over every 7th pixel, and the frame after it.
            end = rec.frame(rest_i if kind == 'open' else page_i)
            last, first_moved = None, None
            for i, ft, fr in rec.frames(tr - 0.04, tr + 0.9):
                d = sum(abs(a - b) for a, b in zip(fr[::7], end[::7])) / len(end[::7])
                b0 = sum(abs(a - b) for a, b in zip(fr[::7], (page if kind == 'open' else rec.frame(rest_i))[::7])) / len(end[::7])
                if d > 1.0:
                    last = (ft - tr) * 1000
                if first_moved is None and b0 > 1.0:
                    first_moved = (ft - tr) * 1000
            nxt = next(((t2 - tr) * 1000 for t2 in rec.times if last is not None and (t2 - tr) * 1000 > last + 1e-6), None)
            over.setdefault((name, kind), []).append((first_moved, last, nxt))
            for i, ft, fr in rec.frames(tr - 0.04, tr + 0.9):
                traps = trapezoids(row_edges(fr, page, rec.w, rec.h), rec.w)
                ss = [strip(tz, s, width) for tz in traps]
                if opts['--frames']:
                    print('  %5.0f ms  %s' % ((ft - tr) * 1000, '  '.join(
                        '[%.1f,%+.1f .. %.1f,%+.1f %+.1f deg about %.1f%s]' % (x[:6] + (
                            '' if x[6] is None else ', near end %.1f,%+.1f' % x[6],)) for x in ss) or '-'))
                for x in ss:
                    pooled.setdefault((name, kind), []).append(((ft - tr) * 1000, x))
        for key in ncalls:
            name, kind = key
            band_report(name, kind, pooled.get(key, []), ncalls[key], factor)
            o = [x for x in over.get(key, []) if x[0] is not None and x[1] is not None and x[2] is not None]
            if o:
                print('  the first frame that changed: %s ms after the call; the last frame still moving: %s; the first at its end: '
                      '%s (median %.0f to %.0f ms)' % (' '.join('%.0f' % x[0] for x in o), ' '.join('%.0f' % x[1] for x in o),
                                                     ' '.join('%.0f' % x[2] for x in o), ml.median([x[1] for x in o]),
                                                     ml.median([x[2] for x in o])))

if __name__ == '__main__':
    main()
