# Pivot, PivotItem and PivotHeaderItem

Status: done · [#121](https://github.com/wdcossey/CornflowerUI/issues/121) · compared against the catalogue's captures (run `20261108-1443-dev11`) and #128's and #136's motion probes (runs `20261115-2056-dev11`, `-2136-`, `-2339-`, `-2343-`) · owed: the device run on the Pixel 8 · owed: the real-window run · owed: probes of the item events, the flick threshold and a press while it moves

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
  (the incoming header's going back). Let go fast enough the way it went, it goes on; as fast the other way, back; slower, on past half a page and back short of it.
- **The motion**, the phone's: the selection changes 78 ms after a swipe left's release (a frame after one right), so
  188 to 241 ms into the probes' swipe; the old item slides out ahead of the offset; the new one comes in a page beyond
  its place and slides in by `ExponentialEase` 4 over 400 ms; the headers already passed go to the end; the offset
  settles on its fitted curve, a page each time.
- **A header tap**: selects at the release, then moves a page a header, the new item coming in once the old is off
  screen; a tap on the selected header does nothing.
- **A change from code**: selects at once, then moves: back one when the new item is the one before (more than two
  items), else forward.
- **IsLocked**: no swipe, no tap; the other headers fade and move 40 right over 0.33 s.
- **The finger**: an item's ScrollViewer keeps a drag up or down, and a Slider or ToggleSwitch its drag
  (`PhoneNoManipulation`), through Avalonia's gesture arbitration.
- **The events**: `PivotItemUnloading` and `PivotItemLoading` at the selection change, `PivotItemUnloaded` and
  `PivotItemLoaded` when the new item comes in; the first item loads when the Pivot is shown.

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
template's. The theme's one helper is `PhonePivotHeader`, internal: the character spacing as a `LetterSpacing` binding,
and the locked translation as a value a style can set and a transition run.

## What Hub can take

Hub (#122) is next, and has a header that moves with its sections (at 1/18 of their speed, #128) and the same
bar-less ScrollViewer. From here it can take:

- **`PivotSwipeGestureRecognizer`'s shape**: a recogniser on the control, in the ScrollViewer part's area, starting
  past Avalonia's `ScrollGestureRecognizer` distance and on its axis, touch and pen only; so items' ScrollViewers and
  `PhoneNoManipulation` controls keep their drags. Hub's pan has no pages, so it would hand its offset on release to
  inertia rather than to a snap curve.
- **`PivotMotion`'s frame loop**: an offset of the control's own, moved by the finger and then by a timeline on
  `CornflowerClock`, drawn by moving render transforms, not by scrolling the ScrollViewer. Its `StripTravel` is the
  Pivot's piecewise rule; Hub's header is one straight line (offset / 18).
- **Holding the ScrollViewer** in `Wp81ScrollViewerScrollBarlessTheme` for its clip and the `Padding` as its margin,
  with scrolling off, if Hub's own motion is taken the same way; or, as the phone's Hub does, really scrolling it
  sideways and reading its offset, since Hub's pan has no wrap and Avalonia's inertia is #116's recorded deviation.
- **The header's text**: `PhonePivotHeader.LetterSpacing` for any WinRT `CharacterSpacing` a style sets on a control
  (if the Hub's styles set any), and `PhoneHeaderLine` for the line.
- **The tests' harness**: `PivotTests`' gestures on a `TestClock` on the emulator's 15.625 ms grid, with the release
  on a frame, so per-frame fractions compare to the phone's.

## Known gaps

- **The offset's curves are fits** to the phone's frames: the swipe's 1.07 px rms (worst 2.2), the tap's 0.74 (worst
  3.5), the way back 0.12 of one drag (COMPARISON.md).
- **Where a drag goes on or back** isn't read: ours goes on past 0.3 px/ms or half a page (owed).
- **A press while it moves** ends the move at once and drags from rest; the phone's DirectManipulation catches the
  content where it is (owed).
- **The item events' order and times** aren't read on the phone (owed).
- **Orientation** is the window's shape: on a desktop a wide window is landscape.
- **A mouse** doesn't swipe; on the phone there was none.
- **The fonts**: Selawik for Segoe WP, so the headers' widths, and pitches, differ by a pixel or two.
