# ListPickerFlyout, from the emulator (#150)

Screen grabs of a ListPickerFlyout of five items ("one" to "five") titled "LIST PICKER"
([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs), `ListPicker`), from refapp run `20261116-1934-dev11`
(`-Popups also -Grab`), dark and light. The `motion/` folder is #149's.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels, the emulator's own screenshot of the whole display (`run.ps1 -Grab`); `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-16 |

The names are the reference app's: `<theme>__ListPickerFlyout__<variant>__popup__screen.png`.

- `multiple-two-checked`: `SelectionMode` `Multiple`, "two" and "four" in `SelectedItems`: a check box before each item,
  ticked for those two, and the bar with accept and cancel (B15).
- `single`: `SelectionMode` `Single`, "two" selected: no check boxes and no bar.

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "The pickers' bar" and
"ListPickerFlyout in Multiple".
