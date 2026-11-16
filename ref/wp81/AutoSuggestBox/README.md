# AutoSuggestBox, from the emulator (#150)

Screen grabs of `new AutoSuggestBox {{ Header = "auto suggest box", PlaceholderText = "type a fruit" }}` on a page the stage's theme ([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs), `Suggest`), from refapp run `20261116-1944-dev11` (`run.ps1 -Popups only -Grab -Only '^(AutoSuggestBox|ContentDialog/keyboard)$'`), dark and light.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels, the emulator's own screenshot of the whole display (`run.ps1 -Grab`); `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-16 |

The names are the reference app's: `<theme>__AutoSuggestBox__<variant>__popup__screen.png`.

- `rest`: at rest: the placeholder, nothing focused.
- `focused`: its TextBox part focused from code: the keyboard comes up and the page moves up until the control's top is at the window's top.
- `text`: focused, `Text` "ap", no items.
- `list-1`: focused, `Text` "ap", one suggestion, `IsSuggestionListOpen` true.
- `list-3`: focused, `Text` "a", three suggestions.
- `list-12`: focused, `Text` "a", twelve suggestions: the list runs down to the keyboard.

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "AutoSuggestBox".
