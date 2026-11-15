# ScrollViewer, from the emulator

The reference app's captures of its ScrollViewer ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)):
280 tall over 14 lines of 20 px text, each in a Border 40 tall (560 tall in all), at rest (`rest`) and scrolled to 200
from code (`offset-200`). Dark and light, `base`. Copied unchanged from refapp run `20261115-1921-dev11` (#113).

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 300 view pixels (1080 × 720 raw), the page's background; the ScrollViewer in a slot 19 from the left and right and 12 from the top, at 19.1667, 15.8333, 412.0833 × 280 once laid out |
| Date | 2026-11-15, 19:20 to 19:21 |

**These captures can't show the indicator.** `RenderTargetBitmap` doesn't draw a ScrollViewer's own indicator in any
state, though the dump has it (`tools/refapp/README.md`, "the panning indicator"), so every
`ScrollingIndicatorStates` capture is byte for byte `base` and isn't copied. The indicator in a ScrollViewer is read from
the dumps (its size and place) and from run `20261115-1946-dev11`'s screen recording (its colour, when it shows and
goes, its fades), which stays outside the repository with the dumps
([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the standalone ScrollBar's captures,
[`../ScrollBar/`](../ScrollBar/README.md), are its pixels.
