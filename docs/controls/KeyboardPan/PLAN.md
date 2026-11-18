# The keyboard's pan (#179)

While the keyboard is up, the page moves up so the focused text box is not under it, and comes back down when the
keyboard goes. For every text box of Wp81's: the TextBox, the PasswordBox theme and the AutoSuggestBox. It is the
phone shell's, not a control's, so it is a shared behaviour (rung 2): `PhoneKeyboardPan`, internal to the theme,
turned on by the TextBox and PasswordBox themes' own setter (the AutoSuggestBox's TextBox part has the TextBox theme).

Status: built · tests and breakage pass · owed: the device run on the Pixel 8 · owed: a read of the phone's pan for
a TextBox, its motion and a box in a ScrollViewer.

## What the phone does (read)

From #150 (`tools/refapp/README.md`, "Tier 3's popups: their layout, the pickers' bar and the keyboard"):

- Focusing the AutoSuggestBox brought the keyboard up in the same frame, and moved the page up until the control's
  top (its header's) was at the window's top: the box, 48.33 to 87.5 at rest, went to 26.67 to 65.83, 21.67 up. The
  keyboard covered none of it.
- The keyboard is 346.67 tall from y 453.33.
- In a ContentDialog a focused TextBox's dialog stayed at the top, the keyboard below.
- The keyboard takes Back first: one Back hides it.

## What is assumed (not read)

- **A TextBox or PasswordBox moves the least.** It goes up only as far as its bottom must to clear the keyboard's
  top, and never so far that its own top goes above the window's top. One the keyboard doesn't cover stays. This is
  how a phone's shell usually pans; the AutoSuggestBox going to the top is taken to be that control's own (it wants
  the room below for its list).
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
- One pan per window, made the first time a box of ours in it takes the focus. It works out the move again as the
  keyboard comes up, changes or goes, and as the focus comes to or leaves a box (once the focus has settled).
- The move is a `TranslateTransform` on the window's content, at animation priority over the content's own, so an
  app's own transform comes back as it was. Hit testing follows it: a tap on the moved box reaches it.
- The popups (the phone's are in the overlay layer), the safe area and the keyboard stay where they are. A box in a
  popup, a ContentDialog's included, moves nothing. A box in a popup that is a window of its own (a desktop's) moves
  nothing either.
- The AutoSuggestBox's list is hung from the box and sized to the keyboard again as the page moves
  (`PhoneKeyboardPan.Moved`, which `PhoneSuggestionList` listens to): a transform lays nothing out, so nothing else
  would tell it.

## Tests

`tests/Cornflower.Tests/Behaviours/KeyboardPanTests.cs`, with a fake keyboard at #150's place:

- A TextBox under the keyboard goes up until its bottom is at the keyboard's top; a hit test at its new place reaches
  it; the keyboard going down brings it back.
- The PasswordBox theme does the same.
- A TextBox above the keyboard stays.
- The focus moving to another box moves the page the least; off every box, the page comes back.
- A box at the end of a ScrollViewer moves with the page; the scroll stays.
- A box in an overlay popup moves nothing.
- Under Simple alone nothing moves.
- The AutoSuggestBox goes up until its top is at the window's top, and its list then hangs from the box and runs down
  to 2.5 above the keyboard (#150's 63.33 and 450.83).

`tests/Cornflower.Tests/Sample/TextBoxPageTests.cs`: the TextBox page's last box, scrolled to and tapped, moves up over
the keyboard and back.

## Left out, with reasons

- **A slide.** Unmeasured; a jump is the plain choice until it is read.
- **Moving again while the box grows** (a many-line box gaining a line with the keyboard up), or as the window turns
  without the keyboard changing. The move is worked out on the keyboard's and the focus's changes only.
- **Scrolling a ScrollViewer instead.** See above. If a read shows the phone scrolls, the pan would scroll the nearest
  ScrollViewer first and move the page only for what is left.
