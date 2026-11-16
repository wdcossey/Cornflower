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
  menu line. They are the reference for the pressed look, which no screenshot has. The open bar at rest before the
  menu line's press, frame 728, is the same to the byte as frame 553, so only 553 is kept.

The heights, the pressed look and the motion read from them are in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "The app bar under touches, and with other numbers of
secondary commands".

## Added for #148 (2026-11-16)

- `light__recording__<what>__frame<n>.png`: frames of the recording of refapp run `20261116-1724-dev11`
  (`run.ps1 -Probe appbarlight -Record`): the same bar in the light theme (its `RequestedTheme` Light, on the probe's
  dark page), at rest, with a finger held on add (closed and open) and on the first menu line, and favorite checked, at
  rest and held. They are the reference for the light theme's pressed look.
- `dark__recording__twelve-secondary-<what>__frame<n>.png`: frames of the recording of refapp run
  `20261116-1726-dev11` (`run.ps1 -Probe appbarlong -Record`): the bar open with twelve secondary commands, at rest,
  with the list dragged up 250 px and held, and after a flick up, at the list's end.

Both are 540 × 960, unchanged. What they show is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "The
app bar: a long list of secondary commands, and a press in the light theme".
