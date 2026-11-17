# LoopingSelector, from the emulator

The phone's LoopingSelectors are the DatePickerFlyout's and TimePickerFlyout's columns: an app can't make one, so they
are captured in their flyouts. A `DatePickerFlyout` (15 July 2014) and a `TimePickerFlyout`, each shown with `ShowAt`
on the reference app's stage, dark and light. Each is the emulator's own screenshot of the whole display
(`run.ps1 -Popups only -Grab`; [`tools/refapp/README.md`](../../../tools/refapp/README.md), "Popups on the reference
profile" and "Popup pixels"): `RenderTargetBitmap` can't see a popup on the phone. Copied unchanged from refapp run
`20261108-2254-dev11`, as [Flyout's](../Flyout/README.md) are.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-08 |

The names are the reference app's: `<theme>__<flyout>__default__popup__screen.png`. Each shows the wheels at rest, as
a flyout opens them: no wheel active, so each shows its selected item alone, in the accent (`Selected`), the others at
opacity 0 (`Normal`).

No capture shows a wheel active (`Expanded`) or moving: a capture is a forced state with no finger, and a wheel's state
comes from its touches. Those are #149's probe readings, every frame, in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "LoopingSelector, and a press on a closed ComboBox"
(runs `20261116-1828-dev11` and `20261116-1904-dev11`, `-Probe looping`, with no recording). A still of an expanded
wheel is owed (docs/controls/LoopingSelector/COMPARISON.md).
