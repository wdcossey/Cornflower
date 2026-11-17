# DatePickerFlyout, at rest, from the emulator (#156)

The reference app's DatePickerFlyout (15 July 2014, no title) opened from code, dark and light, from refapp run
`20261108-2254-dev11` (`-Popups only -Grab`, a cold emulator): the emulator's own screenshot of the whole display once
the picker had held still.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | `RawPixelsPerViewPixel` 2.4; 1080 × 1920, the window 450 × 800 view pixels |
| Date | 2026-11-08 |

- `<theme>__DatePickerFlyout__default__popup__screen.png`: at rest, the three wheels (month, day, year) and the
  pickers' bar.

What they show is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "Popups on the reference profile" and
"Popup pixels"; [docs/controls/DatePicker/COMPARISON.md](../../../docs/controls/DatePicker/COMPARISON.md) sets ours
beside them.

[`motion/`](motion/README.md) has a frame of its opening (#149).
