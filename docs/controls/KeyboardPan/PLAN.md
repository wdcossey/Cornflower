# The keyboard's pan (#179)

While the keyboard is up, the page moves up so the focused text box is not under it, and comes back down when the
keyboard goes. For every box with Wp81's TextBox or PasswordBox theme, the AutoSuggestBox's TextBox part included. A
TextBox that keeps Simple's theme inside a Simple template (`HoleParts`: NumericUpDown, CalendarDatePicker) doesn't
move the page (a known gap). It is the
phone shell's, not a control's, so it is a shared behaviour (rung 2): `PhoneKeyboardPan`, internal to the theme,
turned on by the TextBox and PasswordBox themes' own setter (the AutoSuggestBox's TextBox part has the TextBox theme).

Status: done · tests and breakage pass · device run passed 2026-11-18 (Pixel 8, Release: at 11:51 on `a0f1a02` it
found two faults, fixed in `07a0d02`; again at 12:02 on `07a0d02`, every step of #196's list passed on the textbox,
passwordbox and autosuggestbox pages) · no real-window run (a desktop shows no keyboard pane) · [#201](https://github.com/wdcossey/Cornflower/issues/201)
(the caret rule, `AutoMaximizeSuggestionArea`): tests and breakage pass, owed: the device run on the Pixel 8 · owed: the
pan's motion, a box in a ScrollViewer (scroll or move), and a covered AutoSuggestBox with `AutoMaximizeSuggestionArea`
false.

## What the phone does (read)

From #150 (`tools/refapp/README.md`, "Tier 3's popups: their layout, the pickers' bar and the keyboard"):

- Focusing the AutoSuggestBox brought the keyboard up in the same frame, and moved the page up until the control's
  top (its header's) was at the window's top: the box, 48.33 to 87.5 at rest, went to 26.67 to 65.83, 21.67 up. The
  keyboard covered none of it.
- The keyboard is 346.67 tall from y 453.33.
- In a ContentDialog a focused TextBox's dialog stayed at the top, the keyboard below.
- The keyboard takes Back first: one Back hides it.

From #200 (`tools/refapp/README.md`, "The keyboard's pan"; `ref/wp81/KeyboardPan/`), boxes 300 and 560 down:

- **The AutoSuggestBox goes to the top because of `AutoMaximizeSuggestionArea`** (true by default): 312.08 up from 300
  down, the keyboard nowhere near it. With it false the page doesn't move for that box.
- **A TextBox or PasswordBox the keyboard doesn't cover doesn't move.**
- **One it covers moves the least that shows its caret**, not the whole box: 176.25 (TextBox) and 116.25 (PasswordBox)
  up from 560 down, the caret's bottom at 452.92 against the keyboard's 453.33 and 512.92 against 513.75; 8.33 and 7.92
  of the box stay under the keyboard. The PasswordBox's keyboard has no suggestion strip: 60.42 lower.
- **In a ScrollViewer** the box ends where it does on the page.

## What is assumed (not read)

- **The caret is its line.** The phone's caret is 27.08 tall from 3.33 inside the border; here the caret's line (the
  text presenter's, 1.33 em), from where the presenter's layout puts it, ends 30.28 inside the border against the
  phone's 30.41. The page stops with that line's bottom at the keyboard's top, no gap: the phone stopped 0.41 and 0.83
  short (one and two raw pixels), so within a view pixel. For a box of many lines, the caret's line.
- **The box's top stays in the window.** A box taller than the room above the keyboard goes up no further than puts
  its top (its header's) at the safe area's top, as before.
- **A covered AutoSuggestBox with `AutoMaximizeSuggestionArea` false** moves as a TextBox: its TextBox part's caret,
  the control's top (its header's) kept in the window. Unread.
- **The move is at once.** Whether the phone's slides, and how fast, is unmeasured.
- **A box in a ScrollViewer moves with the page.** The whole window's content moves, as the shell's pan does; the
  ScrollViewer is not scrolled. A scroll can't take a box at the end of its content up, and the sample's pages all
  sit in the shell's ScrollViewer, most at its end.
- **Landscape** is the same rule with the keyboard Avalonia reports.

## How it works

- Avalonia shows the keyboard on focus and says what it covers (`TopLevel.InputPane`: `State`, `OccludedRect`,
  `StateChanged`). Android neither pans nor resizes the Avalonia view (the sample's activity sets no
  `windowSoftInputMode`). The keyboard is read through `PhoneSuggestionList.KeyboardOf`, the seam the AutoSuggestBox's
  list already uses, which a test replaces with a fake.
- One pan per window, made the first time a box of ours in it takes the focus. It works out the move afresh as the
  keyboard comes up, changes or goes (so a keyboard that grows shorter brings the page down with it). As the focus
  comes to or leaves a box (once the focus has settled) it changes the move the least, so a box already in view
  leaves the page where it is. The box the page moved for leaving the window brings the page back.
- The move is a `TranslateTransform` at animation priority on the window's presenter of its content, not on the
  content: the content's own transform (an app's, a page's motion) is left alone, and a new content is moved as the
  old was. Hit testing follows it: a tap on the moved box reaches it.
- The moved content is cut at the safe area's top, so it goes under Android's status bar unseen, as the phone's page
  slides under its system tray, which stays (the Pixel run of a0f1a02 showed text over the clock without it). What the
  move leaves bare, under the status bar and below the page's end, shows the page's background
  (`ApplicationPageBackgroundThemeBrush`, on the window at animation priority), not the window's own: the Pixel showed
  Simple's grey in a band between the page's end and the keyboard when the AutoSuggestBox moved the page further than
  the keyboard is tall.
- The popups (the phone's are in the overlay layer), the safe area and the keyboard stay where they are. A box in a
  popup, a ContentDialog's included, moves nothing. A box in a popup that is a window of its own (a desktop's) moves
  nothing either.
- The AutoSuggestBox's list is hung from the box and sized to the keyboard again as the page moves
  (`PhoneKeyboardPan.Moved`, which `PhoneSuggestionList` listens to): a transform lays nothing out, so nothing else
  would tell it.

## From Avonic

Avonic (Ionic for Avalonia) solved the same problem for Ionic's inputs (`InputShims`, `KeyboardAssist`). Taken:

- **Avalonia's keyboard facts.** It reports the pane's change as its animation starts, and Android's `OccludedRect` can
  still be empty then: the keyboard here reads the change's `EndRect` while the pane is open
  (`PhoneSuggestionList`'s `PaneKeyboard`, so the list gains it too).
- **Windows.** Every window has a pane, the touch keyboard, whose place can be beside or below the window: only what
  it covers of the window counts, and a pane over none of it moves nothing. macOS and Linux have no pane, so nothing
  moves on a desktop with no touch keyboard.
- **Focus moving between boxes keeps the move.** Avonic waits 120 ms after focus leaves; here the pan waits for the
  focus to settle (the dispatcher's next turn), as the move is worked out again from the next box at once.

Not taken: Ionic's mechanics and numbers. Ionic pads the scroll content at its end by the keyboard's height and
scrolls the field to 50 above the keyboard over 150 to 400 ms. The phone's shell moves the page instead, and its
numbers are #150's (the AutoSuggestBox's top at the window's top); its motion is unread.

## Tests

`tests/Cornflower.Tests/Behaviours/KeyboardPanTests.cs`, with a fake keyboard at #150's place:

- A TextBox under the keyboard goes up until its caret's line ends at the keyboard's top, its bottom left under it; a hit test at its new place reaches
  it; the keyboard going down brings it back.
- The PasswordBox theme does the same.
- A TextBox above the keyboard stays.
- The keyboard changing its height moves the page afresh.
- The box leaving the window brings the page back.
- The content's own transform is left alone.
- The moved page is cut at the safe area's top; what the move leaves bare shows the page's background.
- The focus moving to another box moves the page the least; off every box, the page comes back.
- A box at the end of a ScrollViewer moves with the page; the scroll stays.
- A box in an overlay popup moves nothing. (A popup that is a window of its own the headless platform never makes,
  so that guard has no test.)
- A keyboard over none of the window (Windows' touch keyboard beside it) moves nothing.
- Under Simple alone nothing moves.
- The AutoSuggestBox goes up until its top is at the window's top, and its list then hangs from the box and runs down
  to 2.5 above the keyboard (#150's 63.33 and 450.83).
- #200's boxes 560 down move as the phone's did: 176.25 and 116.25 within a view pixel, the caret's line at the
  keyboard's top (TextBox and PasswordBox, each with its keyboard).
- A box whose bottom the keyboard covers but not its caret stays.
- In a box of many lines the caret's line, not the last, ends at the keyboard's top.
- `AutoMaximizeSuggestionArea` false: an AutoSuggestBox the keyboard doesn't cover stays; one it covers goes up until
  its caret's line is at the keyboard's top.

`tests/Cornflower.Tests/Sample/TextBoxPageTests.cs`: the TextBox page's last box, scrolled to and tapped, moves up over
the keyboard and back.

## Left out, with reasons

- **A slide.** Unmeasured; a jump is the plain choice until it is read.
- **Moving again while the box grows or the caret moves** (a many-line box gaining a line, or the caret going down a
  line, with the keyboard up), or as the window turns without the keyboard changing. The move is worked out on the keyboard's and the focus's changes only.
- **Scrolling a ScrollViewer instead.** See above. If a read shows the phone scrolls, the pan would scroll the nearest
  ScrollViewer first and move the page only for what is left.
