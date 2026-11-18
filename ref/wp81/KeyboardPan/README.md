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

The second read (#179, #201), dark, from `20261118-1440-dev11` (`-Popups only -Grab -Only
'^(TextBox/(low-focused|scroll-low)|AutoSuggestBox/low-nomax|PasswordBox/low-reveal)'`), the same profile and date:

- `AutoSuggestBox__low-nomax-focused`: an AutoSuggestBox 560 down with `AutoMaximizeSuggestionArea` false, focused:
  the page moves until the box's bottom meets the keyboard's top.
- `AutoSuggestBox__low-nomax-list-3`, `__low-nomax-list-12`: the same, then given "a" and 3 or 12 suggestions: the
  page moves 10.42 further and the list opens above the box, as tall as its items or up to 18.75 from the window's top.
- `PasswordBox__low-reveal-rest`, `__low-reveal-focused`: a PasswordBox 560 down with `IsPasswordRevealButtonEnabled`
  (its "Show Password" row), at rest and focused: the page moves to the caret, as without the row, which stays under
  the keyboard.
- `TextBox__scroll-low-rest`: the TextBox 560 down inside the ScrollViewer, at rest (its focused grab is the
  `scroll-low-focused` above).

What they show, in numbers, is in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "The keyboard's pan".
