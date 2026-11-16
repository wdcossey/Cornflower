# ContentDialog's layout, from the emulator (#150)

Screen grabs of ContentDialogs opened with `ShowAsync` over a stage the theme's background ([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs), `Dialog` and `KeyboardDialog`), dark and light: refapp run `20261116-1934-dev11` (`-Popups also -Grab`), and `keyboard` from `20261116-1944-dev11`. Title "content dialog" and a two-line body unless said. The `motion/` folder is #149's.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels, the emulator's own screenshot of the whole display (`run.ps1 -Grab`); `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-16 |

The names are the reference app's: `<theme>__ContentDialog__<variant>__popup__screen.png`.

- `default`: two buttons, "ok" and "cancel".
- `no-buttons`: no button text set.
- `primary-only`: only `PrimaryButtonText`.
- `secondary-only`: only `SecondaryButtonText`: it takes the left half, where the primary would be.
- `long`: a title too long for a line, and forty lines of body: the dialog fills the screen, the title is cut, the body is clipped, and the buttons go to a bar the shell draws at the bottom.
- `long-full-size`: the same with `FullSizeDesired`.
- `full-size`: `FullSizeDesired` with the two-line body.
- `keyboard`: a TextBox in the content, focused from code once the dialog has opened: the keyboard comes up, the dialog stays where it was.

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "ContentDialog's layout".
