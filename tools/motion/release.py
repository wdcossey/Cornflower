"""Where a Pivot's or a Hub's release goes, a press while it still moves, the Pivot's item events and the Hub's
ScrollToSection, from a `-Probe pivotrelease` or `hubrelease` run's log (#148).

Usage: python -I release.py <run folder>

The probes (tools/refapp/app/Probe.Reads.cs) log every ViewChanging of the control's ScrollViewer (DirectManipulation's
offset: `next`, `final`, `inertial`) and, every frame something changed, the frame with the offset, as the shell's
probes do (pivot.py). The ScrollViewer takes the touch over once it pans, so the app's pointer events end in
PointerCaptureLost a few tens of ms after the press, and the release isn't in the log. It is taken from touch.log: each
touch's press and release as played on the VM, put on the app's clock by the start tap (`touches`). A press the
ScrollViewer still holds from the touch before (the probes' second presses) reaches no pointer handler, so touch.log is
the only record of it.

For each touch it prints:
- the offset's travel from the press to the release (the first inertial ViewChanging's `next`), and its speed over the
  last 100 ms of the drag's ViewChangings (a straight line through them, px a ms; NaN with fewer than three), which for
  a drag held still before the release is its speed before the hold;
- the first inertial ViewChanging after the press, which is the release's decision: its FinalView less the offset at
  the press, in pages (450 for a Pivot, 405, a section, for a Hub), "on" one way or the other, or "back";
- for a Pivot, the selection after the touch.
For a touch with a second press (the probes' "then a press"), the offset at that press, how far it moved during the
press (still means the press caught it), and where it ended. Then the Pivot's item events (`pivotitem` lines) with the
selection change before them, in ms after it (the stopwatch), and for a Hub each ScrollToSection call: the offset
before and after the call (its own `setup` line) and the frames that follow, so whether the offset animates.

Python 3 standard library only.
"""
import bisect
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

FRAME = re.compile(r'rt ([\d.]+) offset ([-\d.]+)(?: index (-?\d+))?')
# touch.ps1 writes its times with PowerShell's -f, in the VM's culture: a decimal comma is read too.
TOUCH = re.compile(r'^\s*([\d.,]+) ms \(planned\s+[\d.,]+\) (down|up) ')


def touch_times(path):
    """Each touch's press and release from touch.log, in ms from the start of its playing, in the order played."""
    out, down = [], None
    with open(path, encoding='utf-8', errors='replace') as f:
        for line in f:
            m = TOUCH.match(line)
            if not m:
                continue
            ms = float(m.group(1).replace(',', '.'))
            if m.group(2) == 'down':
                down = ms
            elif down is not None:
                out.append((down, ms))
                down = None
    return out


def touches(rows, played):
    """Each touch but the start tap as (press, release) in the app's ms. touch.log's times are moved onto the app's
    clock by the start tap's press (the `go` line), then by the median lag of the app's `pressed` lines to the nearest
    planned press: a press the ScrollViewer still holds from the touch before reaches no pointer handler, so not every
    touch has a `pressed` line. A pressed line over 150 ms from every touch is left out of the lag, and said so."""
    go = next((t for t, w, d in rows if w == 'go'), None)
    if go is None or not played:
        raise SystemExit('no go line in the log, or no touch in touch.log')
    first = played[0][0]
    planned = [(go + a - first, go + b - first) for a, b in played[1:]]
    lags = []
    for t in (t for t, w, d in rows if w == 'pressed'):
        near = min(planned, key=lambda p: abs(p[0] - t))
        if abs(near[0] - t) > 150:
            print('a press at %.1f ms is %.0f ms from the nearest touch in touch.log: left out of the lag' % (t, near[0] - t))
            continue
        lags.append(t - near[0])
    lag = ml.median(lags) if lags else 0.0
    return [(a + lag, b + lag) for a, b in planned]


def offset_at(events, t, before):
    """The offset at time t: the latest ViewChanging's `next` at or before t, else `before`."""
    v = before
    for te, nxt, _, _ in events:
        if te > t:
            break
        v = nxt
    return v


def read_touch(seg, t0, t1, all_touches, page, all_frames):
    """What the touches between t0 and t1 (None: the end) did: a dict, or None with none. The offset at the press is
    the last frame before it in the whole log (`all_frames`, (ms, offset) pairs in time order): a frame is logged only
    when something changed, so a touch's segment can start with none."""
    mine = [p for p in all_touches if p[0] >= t0 and (t1 is None or p[0] < t1)]
    if not mine:
        return None
    press, release = mine[0]
    events = ml.view_changes(seg)
    frames = [(t, float(m.group(2)), int(m.group(3)) if m.group(3) is not None else None)
              for t, w, d in seg if w == 'frame' for m in [FRAME.match(d)] if m]
    k = bisect.bisect_right([f[0] for f in all_frames], press) if all_frames else 0
    start = all_frames[k - 1][1] if k else next((n for t, n, _, _ in events if t >= press), float('nan'))
    after_press = [e for e in events if e[0] >= press]
    first_inertial = next((e for e in after_press if e[3]), None)
    # The release's offset is the first inertial ViewChanging's `next`: the last moves of a drag can reach the phone
    # with the release (the emulator's bursts), after the last ViewChanging that isn't inertial.
    dragging = [e for e in after_press if not e[3] and (first_inertial is None or e[0] < first_inertial[0])]
    at_release = first_inertial[1] if first_inertial else offset_at(after_press, release, start)
    moving = [e for e in dragging if e[0] >= dragging[-1][0] - 100] if dragging else []
    speed = ml.linfit([e[0] for e in moving], [e[1] for e in moving])[1] if len(moving) >= 3 else float('nan')
    out = {
        'press': press, 'held': release - press, 'start': start,
        'travel': at_release - start, 'speed': speed,
        'decided': first_inertial[0] - release if first_inertial else None,
        'final': first_inertial[2] - start if first_inertial else None,
        'end': frames[-1][1] - start if frames else None,
        'index': frames[-1][2] if frames else None,
        'pages': round((first_inertial[2] - start) / page) if first_inertial else None,
    }
    if len(mine) > 1:
        second, second_release = mine[1]
        # The offset at the press, then every frame during it: a frame is logged only when something changed, so a
        # single move during the press shows only against the offset it moved from.
        at_second = offset_at(events, second, start)
        during = [at_second] + [f[1] for f in frames if second <= f[0] <= second_release]
        out['second'] = {
            'after': second - release,
            'at': at_second - start,
            'moved': max(during) - min(during),
            'held': second_release - second,
            'inertial_after': next((e[0] - second_release for e in events if e[0] > second_release and e[3]), None),
        }
    return out


def verdict(r):
    """Where the release went: back, on a page either way, or, more than a page and a half away, to the offset it went
    to (a Hub looping backwards from its first section goes to the end, which is no count of pages)."""
    if r['pages'] is None:
        return 'no inertial ViewChanging after the press'
    if abs(r['pages']) > 1:
        return 'to %+.1f (a loop, not %+d pages)' % (r['final'], r['pages'])
    return 'back' if r['pages'] == 0 else 'on %+d' % r['pages']


SECTIONS = re.compile(r'\| section 1 ([-\d.]+) \| section 2 ([-\d.]+)')


def page_width(rows, pivot):
    """A page's width: for a Pivot the ScrollViewer's viewport, from the layout line that gives the scroll extent; for
    a Hub the second section's left edge less the first's, the median over the frames where it is positive (the layout
    lines can be read during the entrance, under its projection, and the loop moves section 2 to the left). 450 and
    405, the reference profile's, if the log has neither."""
    if pivot:
        for t, w, d in rows:
            if w == 'layout' and ' scroll extent ' in d and ' viewport ' in d:
                return float(d.split()[-1])
        return 450.0
    pitches = [float(m.group(2)) - float(m.group(1)) for t, w, d in rows if w == 'frame' for m in [SECTIONS.search(d)] if m]
    pitches = [p for p in pitches if p > 0]
    return ml.median(pitches) if pitches else 405.0


def main():
    (run,), _ = ml.parse_args(__doc__)
    rows = ml.probe(run)
    all_touches = touches(rows, touch_times(os.path.join(run, 'touch.log')))
    pivot = any(w == 'frame' and ' index ' in d for t, w, d in rows)
    page = page_width(rows, pivot)
    print('%s: a %s, a page %.0f px; offsets in view px, times in ms by the stopwatch' % (run, 'Pivot' if pivot else 'Hub', page))
    print('\n%-62s %7s %8s %7s %9s %9s %s' % ('touch', 'held', 'travel', 'px/ms', 'decided', 'final', 'goes'))
    all_frames = sorted((t, float(m.group(2))) for t, w, d in rows if w == 'frame' for m in [FRAME.match(d)] if m)
    for label, t0, t1, seg in ml.step_segments(rows, times=True):
        r = read_touch(seg, t0, t1, all_touches, page, all_frames)
        if r is None:
            continue
        print('%-62s %7.0f %+8.1f %7.3f %+9.0f %+9.1f %s%s' % (
            label[:62], r['held'], r['travel'], r['speed'], r['decided'] if r['decided'] is not None else float('nan'),
            r['final'] if r['final'] is not None else float('nan'), verdict(r),
            ', index %d' % r['index'] if pivot and r['index'] is not None else ''))
        s = r.get('second')
        if s:
            print('    the second press %.0f ms after the release, the offset %+.1f from the start; it moved %.1f px during the %.0f ms press; '
                  'inertial again %s; at the end %+.1f'
                  % (s['after'], s['at'], s['moved'], s['held'],
                     '%.0f ms after its release' % s['inertial_after'] if s['inertial_after'] is not None else 'never',
                     r['end'] if r['end'] is not None else float('nan')))
    if pivot:
        print('\nThe item events, in ms after the selection change before them')
        sel = None
        for t, w, d in rows:
            if w == 'step':
                # A step starts afresh: an item event before its own selection change has none to be timed from.
                sel = None
                print('  %s' % d)
            elif w == 'selection':
                sel = t
                print('    %8.1f  selection %s' % (0.0, d))
            elif w == 'pivotitem':
                print('    %+8.1f  %s' % (t - sel if sel is not None else float('nan'), d))
    else:
        print('\nScrollToSection')
        for label, t0, t1, seg in ml.step_segments(rows, times=True):
            if not label.startswith('ScrollToSection '):
                continue
            said = next((d for t, w, d in seg if w == 'setup'), '-')
            # A frame is logged when anything changed (the header, a section): only those where the offset did count.
            frames, last = [], None
            for t, w, d in seg:
                m = FRAME.match(d) if w == 'frame' else None
                if m and float(m.group(2)) != last:
                    last = float(m.group(2))
                    frames.append((t - t0, last))
            print('  %s: %s; %d frames with a new offset: %s' % (label, said, len(frames),
                                                                   ' '.join('%.0f:%.1f' % f for f in frames[:12])))


if __name__ == '__main__':
    main()
