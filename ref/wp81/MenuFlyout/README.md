# MenuFlyout, from the emulator (#150)

Screen grabs of MenuFlyouts shown with `ShowAt` on a Button ([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs), `MenuStates` and `MenuAt`), dark and light: `states` from refapp run `20261116-1940-dev11`, the others from `20261116-1934-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels, the emulator's own screenshot of the whole display (`run.ps1 -Grab`); `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-16 |

The names are the reference app's: `<theme>__MenuFlyout__<variant>__popup__screen.png`.

- `states`: an item held in `Pressed` (forced, so its tilt isn't a finger's), a ToggleMenuFlyoutItem checked and one unchecked (they draw the same), a separator (it draws nothing), a disabled item and a last item; the target in the middle of the page.
- `long`: twenty-four items: the menu fills the screen.
- `at-300`: four items, the target's top at 312.08: the menu below it.
- `at-500`: four items, the target's top at 512.08: the menu above it.

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "MenuFlyout".
