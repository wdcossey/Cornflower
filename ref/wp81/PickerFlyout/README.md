# PickerFlyout, from the emulator (#150)

Screen grabs of a PickerFlyout with a line of text as its content ([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs)), from refapp run `20261116-1934-dev11`, dark and light.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels, the emulator's own screenshot of the whole display (`run.ps1 -Grab`); `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-16 |

The names are the reference app's: `<theme>__PickerFlyout__<variant>__popup__screen.png`.

- `confirmation-buttons`: `ConfirmationButtonsVisible` true: the bar with accept and cancel (B15).
- `no-confirmation-buttons`: `ConfirmationButtonsVisible` false: no bar at all.

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "The pickers' bar".
