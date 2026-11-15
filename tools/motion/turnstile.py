"""The page turnstile, from a `-Probe pages -Record` run (20261110-0810-dev11; 20261110-0748-dev11 has fewer turns).

Usage: python -I turnstile.py <run folder> [--gray <file>] [--axis <x>,<z>] [--no-search]

The pages probe navigates a Frame between two pages, a white and a blue sheet inset 19, 60, forward and back with
the default transition and CommonNavigationTransitionInfo (the turnstile), then the slide and continuum. The sheet's
outline is measured in every frame; each run of frames off rest is one transition, and each turnstile step is paired
with the one that starts a fixed lag after it (motionlib.match_steps, which stops on a step with no transition or two
steps on one). Each frame is solved for a rotation about one vertical axis, with the camera of the
projections probe (1000 view px, vanishing point at the element's centre, 225, 400, as the 2026-11-10 fits used it).

Prints:
- the axis that fits every unclipped frame of the forward turns best (skipped with --no-search; it takes minutes),
  and the rms at the published axis, x -44 z -99 (or --axis);
- how many frames it leaves out: those no rotation about the published axis fits within 10 px (torn: the recording
  grabbed the screen while the emulator drew it). The axis search, its rms and the angles below are without them;
- each forward turn's first angle;
- the distinct angles of the forward and back turns (|angle| >= 0.9, to 0.1, within 0.25 taken as one), the tick of
  the 64 Hz clock each is on, and fits to them: a pure exponential and an exponential ease-out ending at a tick
  (the ratio a tick, the time constant, the end and the exponent), and ExponentialEase curves with a free start
  value over a duration, and the ExponentialEase 6 starting on tick 0. A distinct angle between two others a step apart (a frame caught between two steps) is left
  out of the fits, and said so;
- when each turn's first step (tick 0) reached the screen after its step in the log, forward and back, and the
  difference: a step drawn before the first one shown would put the forward turns' a tick later.
"""
import math
import os
import statistics
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

D, CX = 1000.0, 225.0
(run,), opts = ml.parse_args(__doc__, options=('--gray', '--axis'), flags=('--no-search',))
axis = (-44.0, -99.0) if opts['--axis'] is None else tuple(float(v) for v in opts['--axis'].split(','))
search = not opts['--no-search']

rows = []
with ml.Recording(run, opts['--gray']) as rec:
    for i, t, m, level in ml.outline_rows(rec):
        if m[2] is None:  # clipped: keep an edge only if it is on the screen
            m = [m[0] if m[0] > 0.5 else None, m[1] if m[1] < 449.5 else None, None, None]
        rows.append((i, t, m, level))
segs = ml.segments(rows)
turns = [(t, d) for t, d in ml.steps(run) if d.split()[1] in ('default', 'CommonNavigationTransitionInfo')]
lag, pairs, lags = ml.match_steps([s[0][1] for s in segs], [t / 1000 for t, d in turns], what='turnstile turns')
# Each turn as (its step's time in s, its frames).
forward = [(t / 1000, segs[j]) for j, (t, d) in zip(pairs, turns) if d.startswith('forward')]
back = [(t / 1000, segs[j]) for j, (t, d) in zip(pairs, turns) if d.startswith('back')]
print('%d turns in the log, %d forward and %d back in the recording; each starts %.3f to %.3f s after its step '
      '(recording time - app time)' % (len(turns), len(forward), len(back), min(lags), max(lags)))

# A frame the recording grabbed while the emulator drew it shows two angles at once and fits no rotation: left out,
# before the axis and the angles (see motionlib.torn_frame).
TORN = 10.0  # px: real frames fit within about 0.25 px, a torn one by tens; loose enough that an axis a few px off
              # (the search, --axis) doesn't throw real frames out
torn = []


def untorn(seg):
    out = []
    for r in seg:
        bad, th, rms = ml.torn_frame(r[2], axis[0], axis[1], D, CX, 0, TORN)
        if bad:
            torn.append((round(th, 1), round(rms, 1)))
        else:
            out.append(r)
    return out


forward = [(st, untorn(sg)) for st, sg in forward]
back = [(st, untorn(sg)) for st, sg in back]
forward_steps, forward = [st for st, _ in forward], [sg for _, sg in forward]
back_steps, back = [st for st, _ in back], [sg for _, sg in back]
print('%d frames left out that no rotation about the axis fits within %.0f px (torn: grabbed mid-draw)%s'
      % (len(torn), TORN, ': ' + ', '.join('%.1f (%.1f px)' % a for a in torn[:12]) if torn else ''))

if search:
    e, xa, za = ml.fit_axis(forward, D, CX)
    print('axis fitted to the forward turns: x %.1f z %.1f (CenterOfRotationX %.3f of 450), rms %.3f px' % (xa, za, xa / 450, e))
print('axis x %.1f z %.1f: rms %.3f px over every unclipped frame of the %d forward turns'
      % (axis[0], axis[1], ml.axis_rms(forward, axis[0], axis[1], D, CX), len(forward)))


def angles(seg):
    full = [ml.solve_angle(m, axis[0], axis[1], D, CX)[0] for _, _, m, _ in seg if m[2] is not None]
    sign = 1 if sum(full) > 0 else -1
    return [((t - seg[0][1]) * 1000, round(ml.solve_angle(m, axis[0], axis[1], D, CX, sign)[0], 4)) for _, t, m, _ in seg]


fwd = [angles(s) for s in forward]
bk = [angles(s) for s in back]
firsts = {}
for a in fwd:
    if not a:
        continue
    v = round(a[0][1], 1)
    firsts[v] = firsts.get(v, 0) + 1
print('forward, the first angle shown: %s' % ', '.join('%.1f in %d' % (v, n) for v, n in sorted(firsts.items(), key=lambda x: -x[1])))

first_tick = {}
for name, runs, segs_, steps_ in (('forward', fwd, forward, forward_steps), ('back', bk, back, back_steps)):
    vals = ml.distinct([v for a in runs for _, v in a], decimals=1, tol=0.25, min_abs=0.9)
    off = ml.off_ladder(vals)
    if off:
        vals = [v for v in vals if v not in off]
    r0, ticks = ml.tick_numbers(vals)
    print('%s: %d distinct angles %s%s' % (name, len(vals), ','.join('%.1f' % v for v in vals),
                                          '; left out, between two steps a tick apart: %s' % ','.join('%.1f' % v for v in off) if off else ''))
    print('  ticks %s (median ratio %.4f)' % (ticks, r0))
    ml.print_fits(vals, ticks, (('ExponentialEase 6', ml.expo_out(6)), ('ExponentialEase 5', ml.expo_out(5))), ' deg')
    # The same ease starting on the first value shown (tick 0), its start and duration free.
    rms6, dur6, _, a6 = ml.fit_scaled(vals, ticks, ml.expo_out(6), durations=range(300, 601, 5), phases=[0])
    print('  ExponentialEase 6 from tick 0: from %.2f over %d ms, rms %.3f deg' % (a6, dur6, rms6))
    # When each turn's tick 0 was on the screen (motionlib.tick0_times).
    turns_ = [((seg[0][1] - (st + lag)) * 1000, a[0][1]) for a, seg, st in zip(runs, segs_, steps_) if a and seg]
    t0s, left = ml.tick0_times(turns_, vals, ticks)
    first_tick[name] = t0s
    if len(t0s) >= 2:
        print('  tick 0 on the screen %.1f ms after the step (the mean; sd %.1f, %d turns%s; recording time against the log, paired by the lag above)'
              % (statistics.mean(t0s), statistics.stdev(t0s), len(t0s), ', %d left out, their first value on no step' % left if left else ''))
    else:
        print('  tick 0: too few turns to time (%d)' % len(t0s))
if len(first_tick.get('forward', [])) >= 2 and len(first_tick.get('back', [])) >= 2:
    f, b = first_tick['forward'], first_tick['back']
    err = math.sqrt(statistics.variance(f) / len(f) + statistics.variance(b) / len(b))
    print('forward against back: tick 0 %+.1f ms later (standard error %.1f); a hidden step before the first one shown would make it %.1f'
          % (statistics.mean(f) - statistics.mean(b), err, ml.TICK))
