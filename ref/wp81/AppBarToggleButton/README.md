# AppBarToggleButton, from the emulator

The reference app's captures of `new AppBarToggleButton { Label = "pin", Icon = new SymbolIcon(Symbol.Pin) }`, unchecked and with `IsChecked = true` (variants `unchecked` and `checked`) on a page, not on the app bar
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)), dark and light, at rest (`base`) and in each
state of its template's `ApplicationViewStates` (`FullSize`, `Compact`) and `CommonStates` (`Normal`, `Pressed`, `Disabled`, `Checked`, `CheckedPressed`, `CheckedDisabled`), forced with `VisualStateManager.GoToState` on a fresh instance. Copied unchanged from
refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 120 view pixels (1080 × 288 raw), the page's background; the command in a slot 19 from the left and 12 from the top |
| Date | 2026-11-08 |

These are the phone's own template, which draws a command off the shell's bar: its circle 38.5 across with its ring
inside, its label 9.5. A command on a page's app bar is drawn by the shell, larger
([`ref/wp81/CommandBar/`](../CommandBar/README.md)); Cornflower's theme follows the shell
([`docs/controls/CommandBar/PLAN.md`](../../../docs/controls/CommandBar/PLAN.md)). These give the template's
pressed and checked states, which the shell's screenshots don't show. The names are the reference app's:
`<theme>__<variant>__<state>.png`.
