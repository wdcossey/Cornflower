# TimePicker's inner button, from the emulator (#150, B19)

The reference app's captures of a TimePicker with a header ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)),
dark and light, from refapp run `20261116-1934-dev11` (`-Popups also -Grab`): `default` at rest, and its template's
`FlyoutButton` (a Button in the default Button style) forced into a state of its own template with
`VisualStateManager.GoToState`, which the capture's `Prepare` does once the TimePicker has loaded.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | `RawPixelsPerViewPixel` 2.4; the stage is 450 × 140 view pixels (1080 × 336 raw), the page's background, the TimePicker in a slot 19 from the left and 12 from the top |
| Date | 2026-11-16 |

The names are the reference app's: `<theme>__TimePicker__<variant>__base.png`.

- `default`: at rest.
- `flyout-button-pressed`: `FlyoutButton` in `Pressed`, forced with no finger, so its tilt isn't a finger's (as
  [Button's](../Button/README.md)).
- `flyout-button-disabled`: `FlyoutButton` in `Disabled`. Setting the TimePicker's `IsEnabled` false gives the same brushes.

`PointerOver` draws the same as `default`. What they show is in [`tools/refapp/README.md`](../../../tools/refapp/README.md),
"B19: DatePicker's and TimePicker's inner button".
