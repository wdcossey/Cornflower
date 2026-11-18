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
(the caret rule, `AutoMaximizeSuggestionArea`): tests and breakage pass, device run passed 2026-11-18 (Pixel 8, Release,
on `6bce3d4`, 13:24: a covered TextBox and the PasswordBox stop with the caret's bottom on the keyboard's top, the
PasswordBox's Show Password row under the keyboard; the default AutoSuggestBox at the window's top; the one with
`AutoMaximizeSuggestionArea` false moved only until one suggestion fits; the status bar clean) · [#206](https://github.com/wdcossey/Cornflower/issues/206)
(the slide, and a covered AutoSuggestBox with `AutoMaximizeSuggestionArea` false, as #205 read them): tests and
breakage pass, owed: the device run on the Pixel 8.

## What the phone does (read)

From #150 (`tools/refapp/README.md`, "Tier 3's popups: their layout, the pickers' bar and the keyboard"):

- Focusing the AutoSuggestBox brought the keyboard up in the same frame, and moved the page up until the control's
  top (its header's) was at the window's top: the box, 48.33 to 87.5 at rest, went to 26.67 to 65.83, 21.67 up. The
  keyboard covered none of it.
- The keyboard is 346.67 tall from y 453.33.
- In a ContentDialog a focused TextBox's dialog stayed at the top, the keyboard below.
- The keyboard takes Back first: one Back hides it and takes the focus from the box; in a ContentDialog the dialog
  stays, and the second Back closes it.

From #200 (`tools/refapp/README.md`, "The keyboard's pan"; `ref/wp81/KeyboardPan/`), boxes 300 and 560 down:

- **The AutoSuggestBox goes to the top because of `AutoMaximizeSuggestionArea`** (true by default): 312.08 up from 300
  down, the keyboard nowhere near it. With it false the page doesn't move for that box.
- **A TextBox or PasswordBox the keyboard doesn't cover doesn't move.**
- **One it covers moves the least that shows its caret**, not the whole box: 176.25 (TextBox) and 116.25 (PasswordBox)
  up from 560 down, the caret's bottom at 452.92 against the keyboard's 453.33 and 512.92 against 513.75; 8.33 and 7.92
  of the box stay under the keyboard. The PasswordBox's keyboard has no suggestion strip: 60.42 lower.
- **In a ScrollViewer** the box ends where it does on the page.

From #205 (`tools/refapp/README.md`, "The keyboard's pan: an AutoSuggestBox without the maximized list, ..."):

- **A covered AutoSuggestBox with `AutoMaximizeSuggestionArea` false moves its whole box** above the keyboard: 184.58
  for the control 572.08 to 637.92 down, its bottom border at the keyboard's top. With suggestions its list opens
  **above** the box (its bottom 406.25, on the box's top border; 151.25 tall for three, 387.5 for twelve, from 18.75),
  and the page goes **10.42** further, 195 in all.
- **The reveal row doesn't change the move**, and **a ScrollViewer doesn't scroll**: its `VerticalOffset` stays 0.
- **It slides, both ways**, in a recording of about 30 frames a second: up in about 0.3 s, slowing as it goes, from
  the keyboard's start (the first moved frame 35 to 70 ms after it, already 0.23 of the way); down in about 0.2 s,
  slow first and fast last.

## What is assumed (not read)

- **The caret is its line.** The phone's caret is 27.08 tall from 3.33 inside the border; here the caret's line (the
  text presenter's, 1.33 em), from where the presenter's layout puts it, ends 30.28 inside the border against the
  phone's 30.41. The page stops with that line's bottom at the keyboard's top, no gap: the phone stopped 0.41 and 0.83
  short (one and two raw pixels), so within a view pixel. For a box of many lines, the caret's line.
- **The box's top stays in the window.** A box taller than the room above the keyboard goes up no further than puts
  its top (its header's) at the safe area's top, as before.
- **The 10.42 is the list's.** #205 gave the text and the list together, so it can't say which moves the page the
  further 10.42; it is applied as the list opens (`PhoneKeyboardPan.ListAboveLift`), and kept when it closes (the
  least change, as for the focus). The list opens above whenever the keyboard leaves it less than one suggestion's
  room below, covered or not: a box just above the keyboard no longer lifts the page for its list, as #201 did (the
  phone's uncovered box is unread). One with room below opens it below. A box the page's move doesn't take (a
  ContentDialog's) keeps the old rule, the list running down past the keyboard, as above it could still be under it.
- **A slide aimed again the same way** (the list opening as the page rises, the keyboard reporting again) goes on from
  where the page is and ends when it would have, rather than start a whole slide again.
- **The list above is as tall as a maximized list could be**: no taller than the room from a box at the window's top
  down to the keyboard (387.5, which the phone's twelve showed as its `MaxHeight`), and no higher than the safe area's
  top.
- **The slide's curves.** WinRT's exponential ease (`PhoneEase`, as every fitted motion here): up, ease-out exponent
  2 over 300 ms; down, ease-in 3.5 over 200 ms, fitted to the recording's frames, which are too far apart for more.
  It starts as the keyboard's change is reported, as the fit puts the phone's start with the keyboard's (the first
  moved frame ~32 ms in), so no delay is added. A change midway slides from where the page is.
- **A box in a ScrollViewer moves with the page**, as #205 read: the ScrollViewer is not scrolled.
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
- **The slide** runs on `CornflowerClock`'s frames, so tests step it. `CornflowerConfig.KeyboardPanSlide` (300 ms)
  is the time up, two thirds of it down; 0 or less, or `CornflowerConfig.Animated` off, and the page jumps. It is a
  time of its own: `MotionSpeed` and the areas' speeds don't scale it. Where the page goes (`ShiftOf`) is worked out
  from where the box would be unmoved (the transform as it is now, mid-slide), and the AutoSuggestBox's list is sized
  for where the slide ends (`StillToGo`), so a list that opens as the page starts to rise doesn't find no room and
  close.
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
- **Back and the keyboard** (#197). On Android the keyboard takes the Back that hides it, and Avalonia never sees it:
  on the Pixel (#196's run) that Back left the page where it was and the box focused, where a Back reaching the app
  would have gone back a page. So the keyboard going down (the pane open, then not) while a box of ours has the focus
  clears the focus (not a read-only box's: it has no keyboard, so the keyboard going as the focus comes to it is the
  last box's), the AutoSuggestBox's TextBox part included, as `PhoneSuggestionList` already did for it. A
  Back Avalonia does see while a box has the focus (a hardware keyboard, a desktop) clears it too, through
  `PhonePopupHost.TakeBackFirst`, before any popup, and is taken; the next Back goes on. Android's own hide key (none
  on the phone) does the same; so does the keyboard going as the app leaves the screen, the box unfocused on return.

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
numbers are #150's and #200's, its motion #205's.

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
- The keyboard going down takes the focus from a TextBox or PasswordBox, and the next Back is the page's; a keyboard
  that changes while up takes nothing, nor does its going with the focus elsewhere, on a read-only box (which has no
  keyboard) or on a box not ours. A Back with a box focused takes the focus and is taken; one under an open popup
  leaves Back to the popup. In a ContentDialog either takes the focus, the dialog staying, and the next Back closes
  it. A keyboard already up when a box first takes the focus still takes it as it goes. Under Simple alone neither
  takes it.
- The AutoSuggestBox goes up until its top is at the window's top, and its list then hangs from the box and runs down
  to 2.5 above the keyboard (#150's 63.33 and 450.83).
- #200's boxes 560 down move as the phone's did: 176.25 and 116.25 within a view pixel, the caret's line at the
  keyboard's top (TextBox and PasswordBox, each with its keyboard).
- A box whose bottom the keyboard covers but not its caret stays.
- In a box of many lines the caret's line, not the last, ends at the keyboard's top.
- `AutoMaximizeSuggestionArea` false: an AutoSuggestBox the keyboard doesn't cover stays; #205's covered one goes
  184.58, its bottom to the keyboard's top, then with three or twelve suggestions 195, the list above the box (its
  bottom 406.25, 151.25 and 387.5 tall).
- The slide: up on the ease-out over 300 ms and down on the ease-in over 200, the box where each frame has it, the page
  given back at rest; the config's time read (150 ms), 0 or less or `Animated` off jumping; a keyboard changing midway
  aims from where the box rests; a slide turned midway goes from where the page is; the list opened as the page starts
  to rise is sized for where it ends.

`tests/Cornflower.Tests/Sample/TextBoxPageTests.cs`: the TextBox page's last box, scrolled to and tapped, moves up over
the keyboard and back.

## Left out, with reasons

- **Moving again while the box grows or the caret moves** (a many-line box gaining a line, or the caret going down a
  line, with the keyboard up), or as the window turns without the keyboard changing. The move is worked out on the keyboard's and the focus's changes only.
- **Scrolling a ScrollViewer instead.** See above. If a read shows the phone scrolls, the pan would scroll the nearest
  ScrollViewer first and move the page only for what is left.
