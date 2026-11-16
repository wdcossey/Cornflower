# Flyout, from the emulator

A `Flyout` shown with `ShowAt` on a `Button` ("open") on the reference app's stage, its content a `TextBlock`, "flyout
content", wrapping, in two variants: `default` (`Placement` Top) and `full` (`Placement` Full), dark and light. Each is
the emulator's own screenshot of the whole display (`run.ps1 -Popups only -Grab`;
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "Popups on the reference profile" and "Popup pixels"):
`RenderTargetBitmap` can't see a popup on the phone. Copied unchanged from refapp run `20261108-2254-dev11`; run
`20261108-2248-dev11` (from a warm emulator, this one from a cold one) drew the same to the byte.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-08 |

The presenter's tree and bounds come from the same runs' popup dumps (outside the repository); the numbers read from
both are in [`docs/controls/Flyout/PLAN.md`](../../../docs/controls/Flyout/PLAN.md) and
`tests/Cornflower.Tests/Controls/FlyoutReadings.cs`. The names are the reference app's:
`<theme>__Flyout__<variant>__popup__screen.png`.

The flyout opening and closing is in [`motion/`](motion/README.md), stills of #149's recording (run
`20261116-1835-dev11`), read in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "The popups' bands, and
the inline ComboBox".
