"""How each popup is dismissed (#150): the `-Probe dismiss` run's log, case by case.

Usage: python -I dismiss.py <run folder> [--all]

The probe (app/Probe.Dismiss.cs) opens a popup 1.2 s before each touch: the Back key, a tap outside, a tap on the
popup or on one of its parts. For each case this prints, with milliseconds from the touch as planned (the case's
setup line plus 1,200 ms; a touch lands within a frame or two of it), what came of it:

- `back`: HardwareButtons.BackPressed reached the app, whether it was already handled, and the popups open then;
- `call`, `event` and `result`: the popup opened, its events (Opening, Opened, Closed; the dialog's Closing, Closed
  and button clicks; ItemsPicked, DatePicked, TimePicked, Confirmed; a menu item's Click; the AutoSuggestBox's
  TextChanged and SuggestionChosen) and what ShowAsync or ShowAtAsync returned, each with its RenderingTime;
- `pressed`, `released`, `capturelost` and `canceled`, tagged `under`, `page` or a popup's tag: whether the touch
  reached the "under" Button below the popup or the page, or went to the popup; `gesture` and `event` lines tagged
  `under` give its Tapped and Click;
- `keyboard`: the on-screen keyboard showing or hiding;
- `after`: the popups still open 1.5 s after the touch.

With --all every line of the case is printed, frames and pointer moves too.

Python 3 standard library only.
"""
import os
import sys

KEEP = ('back', 'call', 'event', 'result', 'after', 'keyboard', 'error', 'gesture')


def read(run):
    with open(os.path.join(run, 'probe.txt'), encoding='utf-8') as f:
        for line in f:
            parts = line.rstrip('\n').split('\t', 2)
            if len(parts) == 3:
                yield float(parts[0]), parts[1], parts[2]


def cases(lines):
    """(label, touch time, [(t, what, details)]) for each case, its lines up to the next case."""
    out, current = [], None
    for t, what, details in lines:
        if what == 'case':
            current = (details.split(', popups open')[0], t + 1200.0, [])
            out.append(current)
        elif current is not None:
            current[2].append((t, what, details))
    return out


def shown(what, details, everything):
    if everything:
        return True
    if what in KEEP:
        return True
    if what in ('pressed', 'released', 'capturelost', 'canceled'):
        return True
    return what == 'frame' and ('watched' in details or 'suggest box' in details)


def main():
    args = sys.argv[1:]
    everything = '--all' in args
    args = [a for a in args if a != '--all']
    if len(args) != 1:
        print(__doc__)
        return 2
    lines = list(read(args[0]))
    for t, what, details in lines:
        if what == 'layout':
            print(f'layout: {details}')
    found = cases(lines)
    for label, at, body in found:
        print(f'{label}')
        for t, what, details in body:
            if shown(what, details, everything):
                print(f'  {t - at:+8.0f}  {what:9} {details}')
    done = [d for _, w, d in lines if w == 'go']
    print(f'{len(found)} cases; start tap: {done[0] if done else "none"}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
