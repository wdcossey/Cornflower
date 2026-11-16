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

## Added for #136 (2026-11-15 and 16)

- `<theme>__BottomAppBar__compact-open-<n>-secondary__popup__screen.png`: the same bar open with 0, 1, 3, 4, 5 and
  10 secondary commands ("first menu item", "second menu item", then "menu item 3" and on), dark and light, from refapp
  run `20261116-0005-dev11` (`run.ps1 -Popups only -Only BottomAppBar -Grab`). The reference app now names these
  `BottomAppBar`; the 2026-11-09 files kept their old name, `AppBar`.
- `dark__recording__<what>__frame<n>.png`: frames of the recording of refapp run `20261115-2354-dev11`
  (`run.ps1 -Probe appbar -Record`), 540 × 960 (1.2 recording px a view px), unchanged. A compact bar with add,
  favorite (unchecked) and delete (enabled), at rest and with a finger held on add (closed and open) and on the first
  menu line. They are the reference for the pressed look, which no screenshot has.

The heights, the pressed look and the motion read from them are in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "The app bar under touches, and with other numbers of
secondary commands".
