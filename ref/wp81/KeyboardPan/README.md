# The keyboard's pan, from the emulator (#179)

Screen grabs of a box focused from code with the keyboard up, the box 300 or 560 view px below a "page top" line at the
stage's top ([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs), `Panned`), from refapp runs
`20261118-1229-dev11` (`run.ps1 -Popups only -Grab -Only '^(AutoSuggestBox|TextBox)/(mid|low|scroll)'`) and
`20261118-1231-dev11` (`-Only '^PasswordBox/(mid|low)'`). Dark only: the light grabs measure the same.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels, the emulator's own screenshot of the whole display (`run.ps1 -Grab`); `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Date | 2026-11-18 |

The names are the reference app's: `dark__<control>__<variant>__popup__screen.png`.

- `AutoSuggestBox__mid-rest`: the AutoSuggestBox 300 down, at rest (the TextBox and PasswordBox at rest sit at the
  same place).
- `AutoSuggestBox__mid-focused`: focused: the page moves up until the control's top is at the window's top.
- `AutoSuggestBox__mid-nomax`: focused, `AutoMaximizeSuggestionArea` false: the page doesn't move.
- `TextBox__mid-focused`, `PasswordBox__mid-focused`: focused 300 down: the page doesn't move.
- `TextBox__low-rest`: a TextBox 560 down, at rest, under where the keyboard comes.
- `TextBox__low-focused`, `PasswordBox__low-focused`: focused 560 down: the page moves up until the caret's bottom
  meets the keyboard's top; the box's bottom border stays under the keyboard.
- `TextBox__scroll-low-focused`: the TextBox 560 down inside a ScrollViewer: it ends where it does on the page.

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "The keyboard's pan".
