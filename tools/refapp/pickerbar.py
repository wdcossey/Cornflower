"""The full-screen pickers' bar, B15 (#150): the accept and cancel buttons as the phone draws them, read from a refapp
run's screen grabs (`-Popups ... -Grab`), so Cornflower can draw its own glyphs to the same size, stroke and place.

Usage: python -I pickerbar.py <run folder> [more run folders]
       python -I pickerbar.py --selftest

For each `<theme>__<picker>__<variant>__popup__screen.png` of a full-screen picker (ListPickerFlyout, PickerFlyout,
DatePickerFlyout, TimePickerFlyout, a ComboBox of 6 or more), it looks at the bottom 230 raw pixels, left of the
bar's "..." (the right 160), and finds each ring: a connected piece of ink about 90 px across with a hole. For each
ring it prints, in view pixels (raw / rawPixelsPerViewPixel from the run's index.json):

- the ring's centre and outer diameter (the box of its outline at half ink), and its stroke: the ink summed across the ring on the four lines out from
  the centre (left, right, up, down), averaged;
- the glyph inside it (the ink inside the ring's inner edge, less 2 px): its box, that of its outline at half ink, its
  centre against the ring's, its ink area, and its stroke, 2 A / P, A the ink area and P the length of the glyph's
  outline at half ink (marching squares, linear between pixels): for strokes much longer than wide, their width;
- for the accept glyph (the check), three points of the stroke's centre line: the left end, the bottom corner and
  the right end, each the extreme ink point moved half a stroke in, read as the ink-weighted centre of the extreme
  row or column;
- for the cancel glyph (the cross), its box, which is the four ends';
- the bar's "..." (in the right 160 px): its dots' top row and their centres, which place the bar against the page's
  own app bar (#23 found its dots 25 raw px below the bar's top).

The phone draws these as Segoe UI Symbol glyphs (U+E10B accept, U+E10A cancel, gap register B15). This reads only
what reaches the screen: sizes, strokes and places, never the outline, which stays out of the repo (rule 6).

Python 3 standard library only; the PNGs are read with lines.py's reader.
"""
import glob
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lines import png  # noqa: E402

PICKERS = ('ListPickerFlyout', 'PickerFlyout', 'DatePickerFlyout', 'TimePickerFlyout', 'ComboBox')
BAND = 230
RIGHT = 160


def luminance(rows, bpp, w, h):
    return [[(r[x * bpp] * 299 + r[x * bpp + 1] * 587 + r[x * bpp + 2] * 114) / 1000.0 for x in range(w)] for r in rows]


def ink_map(lum, background):
    """Ink 0 to 1 from the background toward the far end of the range (white on dark, black on light)."""
    fg = 255.0 if background < 128 else 0.0
    span = fg - background
    return [[min(1.0, max(0.0, (v - background) / span)) for v in row] for row in lum]


def components(ink, threshold=0.5):
    """Connected pieces (4-neighbours) of ink above the threshold, each a list of (x, y)."""
    h, w = len(ink), len(ink[0])
    seen = [[False] * w for _ in range(h)]
    out = []
    for y in range(h):
        for x in range(w):
            if seen[y][x] or ink[y][x] <= threshold:
                continue
            stack, piece = [(x, y)], []
            seen[y][x] = True
            while stack:
                cx, cy = stack.pop()
                piece.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and ink[ny][nx] > threshold:
                        seen[ny][nx] = True
                        stack.append((nx, ny))
            out.append(piece)
    return out


def contour(ink, x0, y0, x1, y1, level=0.5):
    """The iso-line at `level` inside the box (inclusive), by marching squares, linear between pixel centres: its
    length, and its box (left, top, right, bottom) in the grid's pixel coordinates, a pixel's centre at +0.5."""
    total = 0.0
    seen = []
    for y in range(y0 - 1, y1 + 1):
        for x in range(x0 - 1, x1 + 1):
            v = [ink[y][x], ink[y][x + 1], ink[y + 1][x + 1], ink[y + 1][x]]
            corners = [(0, 0), (1, 0), (1, 1), (0, 1)]
            pts = []
            for k in range(4):
                a, b = v[k], v[(k + 1) % 4]
                if (a > level) != (b > level):
                    t = (level - a) / (b - a)
                    (ax, ay), (bx, by) = corners[k], corners[(k + 1) % 4]
                    pts.append((x + 0.5 + ax + (bx - ax) * t, y + 0.5 + ay + (by - ay) * t))
            seen.extend(pts)
            if len(pts) == 2:
                total += math.dist(pts[0], pts[1])
            elif len(pts) == 4:
                total += math.dist(pts[0], pts[1]) + math.dist(pts[2], pts[3])
    if not seen:
        return 0.0, None
    xs, ys = [p[0] for p in seen], [p[1] for p in seen]
    return total, (min(xs), min(ys), max(xs), max(ys))


def ring_stroke(ink, cx, cy, r_out):
    """The ink summed along the four lines from the centre out past the ring, averaged."""
    n = int(r_out) + 4
    icx, icy = int(round(cx)), int(round(cy))
    sums = []
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        s = 0.0
        for k in range(int(r_out * 0.6), n):
            x, y = icx + dx * k, icy + dy * k
            if 0 <= y < len(ink) and 0 <= x < len(ink[0]):
                s += ink[y][x]
        sums.append(s)
    return sum(sums) / 4


def measure(path, scale):
    w, h, bpp, rows = png(path)
    lum = luminance(rows[h - BAND:], bpp, w, BAND)
    background = lum[BAND - 3][4]
    ink = ink_map(lum, background)
    # The bar's "...": its dots' top row and their centres, from the right RIGHT px.
    dots = [p for p in components([row[w - RIGHT:] for row in ink], 0.5) if len(p) > 10]
    ellipsis = None
    if dots:
        top = min(y for p in dots for _, y in p)
        centres = sorted(w - RIGHT + sum(x for x, _ in p) / len(p) + 0.5 for p in dots)
        ellipsis = (top, centres)
    for row in ink:
        for x in range(w - RIGHT, w):
            row[x] = 0.0
    pieces = sorted(components(ink), key=lambda p: min(x for x, _ in p))
    rings = []
    for p in pieces:
        xs, ys = [x for x, _ in p], [y for _, y in p]
        bw, bh = max(xs) - min(xs) + 1, max(ys) - min(ys) + 1
        if bw < 60 or bh < 60 or len(p) > 0.5 * bw * bh:
            continue
        rings.append((min(xs), min(ys), max(xs), max(ys)))
    out = []
    for (x0, y0, x1, y1) in rings:
        # The ring's outer edge: the box of its outline at half ink, as the glyph's (a row or column sum of ink would put
        # an edge that falls part-way through a pixel up to a pixel out).
        pad = 3
        region = [[ink[y][x] if 0 <= y < len(ink) and 0 <= x < len(ink[0]) else 0.0 for x in range(x0 - pad, x1 + pad + 1)]
                  for y in range(y0 - pad, y1 + pad + 1)]
        _, (left, top, right, bottom) = contour(region, 1, 1, len(region[0]) - 2, len(region) - 2)
        left, right, top, bottom = left + x0 - pad, right + x0 - pad, top + y0 - pad, bottom + y0 - pad
        cx, cy = (left + right) / 2, (top + bottom) / 2
        diameter = ((right - left) + (bottom - top)) / 2
        stroke = ring_stroke(ink, cx, cy, diameter / 2)
        # The glyph: the ink well inside the ring.
        r_in = diameter / 2 - stroke - 2
        gx0, gx1 = int(cx - r_in), int(cx + r_in) + 1
        gy0, gy1 = int(cy - r_in), int(cy + r_in) + 1
        sub = [[ink[y][x] if math.hypot(x + 0.5 - cx, y + 0.5 - cy) < r_in else 0.0 for x in range(gx0, gx1 + 1)]
               for y in range(gy0, gy1 + 1)]
        area = sum(map(sum, sub))
        perimeter, box = contour(sub, 1, 1, len(sub[0]) - 2, len(sub) - 2)
        if box is None:
            # A ring with nothing inside: no glyph to measure.
            continue
        bl, bt, br, bb = box
        g = {'left': gx0 + bl, 'right': gx0 + br, 'top': gy0 + bt, 'bottom': gy0 + bb}
        gstroke = 2 * area / perimeter if perimeter else float('nan')
        out.append({'ring': (cx, cy, diameter, stroke), 'glyph': g, 'area': area, 'stroke': gstroke,
                    'sub': (gx0, gy0, sub)})
    return background, out, ellipsis, h


def check_points(sub, gx0, gy0, stroke):
    """The check's left end, bottom corner and right end, as centre-line points (see the module's docstring)."""
    h, w = len(sub), len(sub[0])
    cols = [[sub[y][x] for y in range(h)] for x in range(w)]

    def centre_of(values):
        s = sum(values)
        return sum(i * v for i, v in enumerate(values)) / s if s else float('nan')

    inked_cols = [x for x in range(w) if max(cols[x]) > 0.5]
    inked_rows = [y for y in range(h) if max(sub[y]) > 0.5]
    lx, rx, by = inked_cols[0], inked_cols[-1], inked_rows[-1]
    half = stroke / 2
    left = (gx0 + lx + half, gy0 + centre_of(cols[lx + int(round(half))]) + 0.5)
    right = (gx0 + rx + 1 - half, gy0 + centre_of(cols[rx - int(round(half))]) + 0.5)
    bottom = (gx0 + centre_of(sub[by - int(round(half))]) + 0.5, gy0 + by + 1 - half)
    return left, bottom, right


def view(v, scale):
    return v / scale


def report(run):
    with open(os.path.join(run, 'index.json'), encoding='utf-8') as f:
        scale = json.load(f)['display']['rawPixelsPerViewPixel']
    print(f'{run}: {scale} raw px per view px; view px below, the screen\'s bottom at 800')
    for path in sorted(glob.glob(os.path.join(run, '*__popup__screen.png'))):
        name = os.path.basename(path)[:-len('__popup__screen.png')]
        theme, picker, variant = name.split('__')
        if picker not in PICKERS or (picker == 'ComboBox' and int(variant.split('-')[1]) < 6):
            continue
        background, found, ellipsis, h = measure(path, scale)
        dots = ''
        if ellipsis:
            top, centres = ellipsis
            dots = ("; the '...' dots' top row" + f' at {view(top + h - BAND, scale):.2f}, centres at x '
                    + ', '.join(f'{view(c, scale):.2f}' for c in centres))
        print(f'  {name}: background luminance {background:.0f}, {len(found)} ring(s){dots}')
        for k, m in enumerate(found):
            cx, cy, d, s = m['ring']
            g = m['glyph']
            top_off = h - BAND
            gw, gh = view(g['right'] - g['left'], scale), view(g['bottom'] - g['top'], scale)
            gcx, gcy = (g['left'] + g['right']) / 2, (g['top'] + g['bottom']) / 2
            print(f'    ring {k}: centre {view(cx, scale):.2f}, {view(cy + top_off, scale):.2f}; outer diameter {view(d, scale):.2f};'
                  f' stroke {view(s, scale):.2f}')
            print(f'      glyph: box {gw:.2f} x {gh:.2f}, centre {view(gcx - cx, scale):+.2f}, {view(gcy - cy, scale):+.2f} from the ring\'s;'
                  f' ink {m["area"] / scale / scale:.1f} sq px; stroke {view(m["stroke"], scale):.2f}')
            # The check is the left of two rings (accept, then cancel); with any other count, which is which isn't known.
            if k == 0 and len(found) == 2:
                gx0, gy0, sub = m['sub']
                pts = check_points(sub, gx0, gy0, m['stroke'])
                rel = ', '.join(f'({view(px - cx, scale):+.2f}, {view(py - cy, scale):+.2f})' for px, py in pts)
                print(f'      check centre line, from the ring\'s centre: left end, bottom corner, right end {rel}')


def selftest():
    """A ring and a cross drawn with known sizes, anti-aliased by 8 x 8 supersampling, measured back. The cross's
    sharp corners read 0.3 to 0.9 px inside at half ink, by where they fall on the pixels, so its box is checked to 1 px."""
    failures = []
    # One on the pixel grid, and two with the centre and the edges part-way through pixels.
    for cx, cy, r_out in ((150.0, 120.0, 46.0), (150.5, 120.0, 46.0), (150.3, 120.7, 46.4)):
        failures += _selftest_one(cx, cy, r_out)
    if failures:
        print('selftest failed: ' + '; '.join(failures))
        return 1
    print('selftest passed: ring centre, diameter and stroke; the cross\'s box and stroke, on and off the pixel grid')
    return 0


def _selftest_one(cx, cy, r_out):
    import zlib
    import struct
    import tempfile
    W, H = 400, BAND
    ring_w, stroke, arm = 6.0, 7.0, 20.0

    def inked(x, y):
        d = math.hypot(x - cx, y - cy)
        if r_out - ring_w <= d <= r_out:
            return True
        for sx in (1, -1):
            # A segment from (cx - arm, cy - sx * arm) to (cx + arm, cy + sx * arm), `stroke` wide, flat ends.
            ux, uy = 1 / math.sqrt(2), sx / math.sqrt(2)
            t = (x - cx) * ux + (y - cy) * uy
            n = -(x - cx) * uy + (y - cy) * ux
            if abs(t) <= arm * math.sqrt(2) and abs(n) <= stroke / 2:
                return True
        return False

    raw = bytearray()
    for y in range(H):
        raw.append(0)
        for x in range(W):
            c = sum(inked(x + (i + 0.5) / 8, y + (j + 0.5) / 8) for i in range(8) for j in range(8)) / 64
            v = int(round(31 + c * (255 - 31)))
            raw += bytes((v, v, v))
    data = b'\x89PNG\r\n\x1a\n'
    for kind, body in ((b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 2, 0, 0, 0)), (b'IDAT', zlib.compress(bytes(raw))), (b'IEND', b'')):
        data += struct.pack('>I', len(body)) + kind + body + struct.pack('>I', zlib.crc32(kind + body))
    with tempfile.TemporaryDirectory() as d:
        path = os.path.join(d, 'x.png')
        with open(path, 'wb') as f:
            f.write(data)
        _, found, _, _ = measure(path, 1.0)
    failures = []
    at = f' (ring at {cx}, {cy}, r {r_out})'
    if len(found) != 1:
        failures.append(f'{len(found)} rings, not 1')
    else:
        m = found[0]
        rcx, rcy, dia, rs = m['ring']
        g = m['glyph']
        span = 2 * arm + stroke / math.sqrt(2)
        checks = [('centre x', rcx, cx, 0.2), ('centre y', rcy, cy, 0.2), ('diameter', dia, 2 * r_out, 0.3),
                  ('ring stroke', rs, ring_w, 0.2), ('glyph width', g['right'] - g['left'], span, 1.0),
                  ('glyph height', g['bottom'] - g['top'], span, 1.0), ('glyph stroke', m['stroke'], stroke, 0.35)]
        for label, got, want, tol in checks:
            if abs(got - want) > tol:
                failures.append(f'{label} {got:.3f}, want {want:.3f} within {tol}{at}')
    return failures


def main():
    args = sys.argv[1:]
    if args == ['--selftest']:
        return selftest()
    if not args:
        print(__doc__)
        return 2
    for run in args:
        report(run)
    return 0


if __name__ == '__main__':
    sys.exit(main())
