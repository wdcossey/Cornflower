# Pivot, PivotItem and PivotHeaderItem

Status: done · [#121](https://github.com/wdcossey/CornflowerUI/issues/121) · compared against the catalogue's captures (run `20261108-1443-dev11`) and #128's and #136's motion probes (runs `20261115-2056-dev11`, `-2136-`, `-2339-`, `-2343-`) · device run passed 2026-11-16 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page pivot` on `main` at `ae652d3`, 22:48 to 22:50, cold starts 716 to 826 ms over the run's pages: the page opened in 352 ms) · real-window run passed 2026-11-16 (`tools/real-window/drive.ps1`, Debug, on #161's head `d0a9ef6`: 160 of 160, every page opened from the list, ready, and left by Back) · the item events, where a release goes and a press while it moves read in [#148](https://github.com/wdcossey/CornflowerUI/issues/148) (#170, run `20261116-1706-dev11`) and followed in [#162](https://github.com/wdcossey/CornflowerUI/issues/162), [#163](https://github.com/wdcossey/CornflowerUI/issues/163) and [#164](https://github.com/wdcossey/CornflowerUI/issues/164) · owed for those three: the device run on the Pixel 8, and the real-window run

The Windows Phone 8.1 Pivot: Cornflower's own `Pivot` and `PivotItem` (`Cornflower.Controls`) and `PivotHeaderItem`
and `PivotHeaderPanel` (`Cornflower.Controls.Primitives`), behaviour only, with the phone's motion read from the
emulator frame by frame and run on `CornflowerClock` (`PivotMotion`, internal); themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/Pivot.axaml`. The plan, with every value and its source, is [PLAN.md](PLAN.md);
the phone's captures and readings beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone, the Pivot is the page, full width (not in the page's 19 px margin):

```xml
<!-- 8.1 -->
<Pivot Title="MY APP">
  <PivotItem Header="one">…</PivotItem>
  <PivotItem Header="two">…</PivotItem>
</Pivot>
<!-- Avalonia, xmlns:cf="using:Cornflower.Controls" -->
<cf:Pivot Title="MY APP">
  <cf:PivotItem Header="one">…</cf:PivotItem>
  <cf:PivotItem Header="two">…</cf:PivotItem>
</cf:Pivot>
```

Or from data: `ItemsSource`, with `HeaderTemplate` for the strip and `ItemTemplate` for each page; each item is its own
header. `SelectedIndex`, `SelectedItem` and `SelectionChanged` are Avalonia's `SelectingItemsControl`'s; an item is
always selected while there are any. `IsLocked` holds the Pivot on its item. In a `ContentPage` (#120) with an app bar
(#133), the Pivot is the page's content.

A swipe left goes to the next item, right to the one before, and the last wraps to the first; a tap on a header goes to
its item. A mouse doesn't swipe, as Avalonia's ScrollViewer doesn't; it taps the headers.

## Behaviours

- **The look**: the phone's template, part for part: the title in `PivotTitleContentControlStyle`'s values, the strip
  of 57 px SemiLight headers, the selected one in the foreground and the rest at 40%, the item inset
  `PivotItemMargin`; landscape moves the title up (`:landscape`, by the window's shape).
- **The strip**: laid out from the selected header, in item order, wrapping round; each header at its full width, the
  view's edge cutting what doesn't fit. A header's pitch is its width plus 15.83.
- **The swipe**: the content follows the finger; the strip moves with it, a page of offset its leaving header's pitch
  (the incoming header's going back). Let go, it goes on where the offset plus 385 ms of its speed, the way it went,
  passes 0.52 of a page (234 px of 450); else back. Held still, that is on from 234 px, where the phone goes back at
  228.3 and on at 239.3. **A fit, not a read** (#162): #148 read no single speed that decides, and this rule fits every
  release it read, with T anywhere from 350 to 420 ms and H from 228.3 to 239.3 px; ours takes the middle of each.
- **The motion**, the phone's: the selection changes 78 ms after a swipe left's release (a frame after one right), so
  188 to 241 ms into the probes' swipe; the old item slides out ahead of the offset; the new one comes in a page beyond
  its place and slides in by `ExponentialEase` 4 over 400 ms; the headers already passed go to the end; the offset
  settles on its fitted curve, a page each time.
- **A press while a swipe moves** (#163): holds it where it is, the offset, the strip and the item, within the frame,
  and a selection still to come waits. Let go without a drag, it goes on where it was going; dragged, the drag goes on
  from where it was held and its release goes by the rule above. A press during a tap's or a code change's move
  doesn't hold it: a drag then ends that move at its item first.
- **A header tap**: selects at the release, then moves a page a header, the new item coming in once the old is off
  screen; a tap on the selected header does nothing.
- **A change from code**: selects at once, then moves: back one when the new item is the one before (more than two
  items), else forward.
- **IsLocked**: no swipe, no tap; the other headers fade and move 40 right over 0.33 s.
- **The finger**: an item's ScrollViewer keeps a drag up or down, and a Slider or ToggleSwitch its drag
  (`PhoneNoManipulation`), through Avalonia's gesture arbitration.
- **The events** (#164): all four together after `SelectionChanged`, with `SelectedIndex` already the new one:
  `PivotItemUnloading` and `PivotItemUnloaded` for the item left, then `PivotItemLoading` and `PivotItemLoaded` for the
  one selected. They come as the new item comes in: for a swipe at the selection, before it slides in (the phone's 0.1
  to 2.7 ms after); after a tap or a change from code going forward, 7 frames (109 ms) after it (the phone's 115, and
  107 to 121); going back from code, a frame after it. The first item loads when the Pivot is shown.

The tests are `tests/Cornflower.Tests/Controls/PivotTests.cs` and `Sample/PivotPageTests.cs`; PLAN.md lists each
behaviour beside its test.

## The checklist

PLAN.md has the three generated checklists, every row ticked or explained. Left out: the selectors (`ItemTemplateSelector`,
`ItemContainerStyleSelector`, `ContentTemplateSelector`), which Avalonia doesn't have; grouping; `ItemContainerTransitions`
and `ContentTransitions`, which the phone's styles don't set; and `PivotItem`'s `Right`, `Left` and `Center` states,
which change nothing.

## Resource keys

The phone's own, in `Tokens/`: `PhoneForegroundBrush`, `PivotPortraitThemePadding`, `PivotLandscapeThemePadding`,
`PivotItemMargin`, `PivotHeaderItemFontSize`, `PivotHeaderItemFontFamily`, `PivotHeaderItemCharacterSpacing`,
`PivotHeaderBackgroundUnselectedBrush`, `PivotHeaderForegroundUnselectedBrush`, `PivotHeaderItemMargin`,
`PivotHeaderItemPadding`, `PhonePivotLockedTranslation`, `PivotHeaderForegroundSelectedBrush`,
`PivotHeaderBackgroundSelectedBrush`; and for the title, `PhoneFontFamilyNormal` and `TextStyleLargeFontSize`.
Cornflower's: `Wp81ScrollViewerScrollBarlessTheme` and `Wp81ContentControlTheme`, which the template sets.

## The ladder

Rung 5: new controls. `Pivot` is a `SelectingItemsControl`; `PivotItem` and `PivotHeaderItem` are `ContentControl`s;
`PivotHeaderPanel` a `Panel`. The motion is the control's own (`PivotMotion`), as the phone's is its code's, not its
template's. The theme's helpers are internal: `PhoneCharacterSpacing`, the character spacing as a `LetterSpacing` binding (the
Hub's theme takes it too), and `PhonePivotHeader`, the locked translation as a value a style can set and a transition run.

## What Hub can take

Hub (#122) is next, and has a header that moves with its sections (at 1/18 of their speed, #128) and the same
bar-less ScrollViewer. From here it can take:

- **`PivotSwipeGestureRecognizer`'s shape** (since the Tier 2 roll-up, `SidewaysSwipeGestureRecognizer`, shared): a recogniser on the control, in the ScrollViewer part's area, starting
  past Avalonia's `ScrollGestureRecognizer` distance and on its axis, touch and pen only; so items' ScrollViewers and
  `PhoneNoManipulation` controls keep their drags. Hub's pan has no pages, so it would hand its offset on release to
  inertia rather than to a snap curve.
- **`PivotMotion`'s frame loop**: an offset of the control's own, moved by the finger and then by a timeline on
  `CornflowerClock`, drawn by moving render transforms, not by scrolling the ScrollViewer. Its `StripTravel` is the
  Pivot's piecewise rule; Hub's header is one straight line (offset / 18).
- **Holding the ScrollViewer** in `Wp81ScrollViewerScrollBarlessTheme` for its clip and the `Padding` as its margin,
  with scrolling off, if Hub's own motion is taken the same way; or, as the phone's Hub does, really scrolling it
  sideways and reading its offset, since Hub's pan has no wrap and Avalonia's inertia is #116's recorded deviation.
- **The header's text**: `PhoneCharacterSpacing.LetterSpacing` for any WinRT `CharacterSpacing` a style sets on a control
  (if the Hub's styles set any), and `PhoneHeaderLine` for the line.
- **The tests' harness**: `PivotTests`' gestures on a `TestClock` on the emulator's 15.625 ms grid, with the release
  on a frame, so per-frame fractions compare to the phone's.

## Known gaps

- **The offset's curves are fits** to the phone's frames: the swipe's 1.07 px rms (worst 2.2), the tap's 0.74 (worst
  3.5), the way back 0.12 of one drag (COMPARISON.md).
- **Where a release goes is a fit**, not a read (#162): the offset plus 385 ms of its speed against 0.52 of a page.
  It sends every release #148 read the phone's way; between them, nearer the edges of the fit's ranges, it may not.
  Ours reads the speed over the last 50 ms of the drag (the shared recogniser's), the probe over the last 100.
- **A press while it moves**: the phone's presses all came past 239 px, and went on; ours, let go without a drag,
  goes on where the move was going, wherever the press came. Not read: a press short of that, a drag from the press,
  a press during a tap's or a code change's move (ours doesn't hold those), what the item does while held (ours holds
  it too) and when a held swipe's selection comes (ours after the release).
- **The item events going back from code** come a frame after the selection; the phone's 36 to 43 ms. Ours come when
  the new item comes in, which going back is at once.
- **Orientation** is the window's shape: on a desktop a wide window is landscape.
- **A mouse** doesn't swipe; on the phone there was none.
- **The fonts**: Selawik for Segoe WP, so the headers' widths, and pitches, differ by a pixel or two.
