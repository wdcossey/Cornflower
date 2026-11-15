# ScrollViewer, ScrollBar and Thumb: comparison

**Not finished: pending [#113](https://github.com/wdcossey/CornflowerUI/issues/113).** No capture of a ScrollViewer or a
ScrollBar exists yet: the indicator is drawn only while the content scrolls, which the reference app's forced-state
captures can't show. #113, which has the reference VM, takes them, with a recording of a pan and the scroll physics.
Until they come, this page has our frames and the measurements the templates and WinRT's sums give, and nothing is
marked **match** that a capture hasn't shown.

**The phone:** owed. Captures will be the reference profile's, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920
at 2.4, in `ref/wp81/ScrollViewer/`.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/ScrollViewerFrames.cs`, Avalonia 12.1.4 with Skia: a
ScrollViewer filling the 450 × 800 view at 2.4 (1080 × 1920) over the page's background, holding a page 2000 tall,
in [`frames/`](frames). No text is drawn.

## Our frames

| State | Dark | Light |
|---|---|---|
| `base`: at rest, nothing drawn | ![](frames/dark__ScrollViewer__page__base.png) | ![](frames/light__ScrollViewer__page__base.png) |
| `TouchIndicator`: scrolled to 600 of 1200 | ![](frames/dark__ScrollViewer__page__TouchIndicator.png) | ![](frames/light__ScrollViewer__page__TouchIndicator.png) |
| `TouchIndicator-end`: scrolled to the end | ![](frames/dark__ScrollViewer__page__TouchIndicator-end.png) | ![](frames/light__ScrollViewer__page__TouchIndicator-end.png) |
| `CommonStates.Disabled`: kept shown, at 0.5 | ![](frames/dark__ScrollViewer__page__CommonStates.Disabled.png) | ![](frames/light__ScrollViewer__page__CommonStates.Disabled.png) |
| `sideways`: a page 900 wide, scrolled both ways | ![](frames/dark__ScrollViewer__page__sideways.png) | ![](frames/light__ScrollViewer__page__sideways.png) |

## Measurements

In view pixels (DIP) unless raw pixels (px) are said. "Template" is the phone's style and template; "WinRT's sums"
are `ScrollBar_Partial.cpp`'s, read as a guide (PLAN.md). Each will be checked against a capture.

| What | The phone, by its template and WinRT's sums | Ours, headless | Status |
|---|---|---|---|
| The vertical bar | 7 wide (`ScrollBarMinThemeWidth`), at the right edge, over the content | 7.0833 (17 px) at 442.9167, the full height: the presenter under it is 450 × 800 | owed: a capture |
| The thumb's width | 2.4 (`ScrollBarPanningThumbThemeWidth`), centred in the 7 | 2.5 (6 px), 1069 to 1074 px: 5 px from the right edge | owed: a capture (WinRT rounds 5.76 px to the nearest, 6, too) |
| The thumb's length, 800 of 2000 | 800 × 800 / 2000 = 320 | 320 | owed: a capture |
| Its place at 600 of 1200 | 600 / 1200 × (800 − 320) = 240 | 240 | owed: a capture |
| Its shortest | 7 (`ScrollBarMinThemeHeight` on the Rectangle) | 7.0833 (17 px) | owed: a capture |
| Its colour | `ScrollBarPanningBackgroundThemeBrush`, `PhoneBaseLowColor`: #73FFFFFF dark, #40000000 light | #737373 on black, #BFBFBF on white | owed: a capture |
| Disabled | `Root` at 0.5 | `Root` at 0.5 | owed: a capture |
| The horizontal bar | 7 tall, in the second row, as wide as the first column | 7.0833 at 792.9167, 442.9167 wide | owed: a capture |
| At rest | `NoIndicator`: nothing | nothing | owed: a capture |
| When it shows | on a scroll, and on a mouse over it (WinRT's code) | the same, a mouse's too: a deliberate difference (PLAN.md) | owed: the recording |
| When it goes | 50 to 150 ms after the last scroll (the template's 0.1 s and 0.05 s) | the same, on `CornflowerClock` | owed: the recording |
| The fades | `FadeInThemeAnimation`, `FadeOutThemeAnimation` | none: shown and hidden at once | **gap**, owed: their durations |
| The physics | DirectManipulation's | Avalonia's | **pending #113** |

## Deliberate differences

- **A mouse shows the thin indicator**, which the phone's template would not (PLAN.md, "Decided on the user's
  behalf").
- **`AllowAutoHide` false keeps it shown**: Avalonia's switch, which WinRT lacks.

## Gaps

- **The fades**: their durations are the OS's theme animation metrics, not in any template. Owed to #113's recording.
- **The physics**: pending #113, a part class or a recorded deviation.
- **Layout rounding** up to the raw pixel grid, where the phone rounds to the nearest (ADR 0003): here both give 17 and
  6 px.
