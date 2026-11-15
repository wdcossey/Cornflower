# CommandBar, from the emulator

The shell's drawing of a page's app bar: the reference app's page "page with an app bar", whose `BottomAppBar` is a
`CommandBar` with three primary commands (`AppBarButton` add; `AppBarToggleButton` favorite, checked;
`AppBarButton` delete, disabled) and two secondary ones (`AppBarButton`s "first menu item" and "second menu
item"), in four variants: `compact` and `minimal` (`ClosedDisplayMode`), closed and `-open`, dark and light. Each is
the emulator's own screenshot of the whole display (`run.ps1 -Popups only -Only BottomAppBar -Grab`;
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "The page's app bar"). Copied unchanged from refapp run
`20261109-2124-dev11`; run `20261109-2130-dev11` drew the same to the pixel row.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-09 |

The shell draws the bar, not the `CommandBar`'s template: the template's dump (outside the repository) is the same
open or closed and doesn't match these pixels. So these screenshots are the reference for the bar. The numbers read
from them are in [`docs/controls/CommandBar/PLAN.md`](../../../docs/controls/CommandBar/PLAN.md) and
`tests/Cornflower.Tests/Controls/CommandBarReadings.cs`. The names are the reference app's:
`<theme>__AppBar__<variant>__popup__screen.png`.
