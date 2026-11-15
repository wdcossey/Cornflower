# ScrollViewer, ScrollBar and Thumb

Status: done · [#116](https://github.com/wdcossey/CornflowerUI/issues/116) · compared against [#113](https://github.com/wdcossey/CornflowerUI/issues/113)'s captures, probes and recording (runs `20261115-1921-dev11`, `-1938-`, `-1946-`) · the scroll physics a recorded deviation · device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page scrollviewer` on `main` at `c5ee6fb`: cold 745 ms, the page opened in 309 ms, the thin indicators drawn dark, light and disabled; on the `slider` page (cold 703, opened 295) a touch drag shows the thin indicator and it is gone 3 s later, where Simple drew its wide bar (#99)) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the scrollviewer page opened in 11 ms with 161 elements) · owed: a real-window section driving its touch, drag and wheel (#124's helpers)

The Windows Phone 8.1 ScrollViewer and its thin scroll indicators: Avalonia's `ScrollViewer`, `ScrollBar` and `Thumb`,
themed by `Wp81Theme` with the phone's default styles and templates, in
`src/Cornflower.Themes.Wp81/Controls/ScrollViewer.axaml`, `ScrollBar.axaml` and `Thumb.axaml`, and the indicators shown
and hidden as WinRT's ScrollViewer shows them, by `PhoneScrollIndicators`, internal to the theme. The plan, with every
value and its source, is [PLAN.md](PLAN.md); the phone's captures and measurements beside ours are in
[COMPARISON.md](COMPARISON.md).

## Usage

As on the phone: a `ScrollViewer` with no style is the phone's.

```xml
<ScrollViewer>
  <StackPanel>…</StackPanel>
</ScrollViewer>
```

It scrolls up and down; set `HorizontalScrollBarVisibility` to `Auto` or `Visible` to scroll sideways too, as on the
phone. Nothing is drawn at rest. While the content scrolls, a thin indicator, 2.4 wide in `PhoneBaseLowColor`, shows
at the right edge (or the bottom), over the content, as long as the view is to the content and as far down as the
scroll. It fades in over 370 ms as the scroll starts, and out over 340 ms from 50 to 150 ms after the scroll stops,
from wherever it had got to, as the phone's does. A touch on it pans the content: nothing drags it. The scrolling
itself, its inertia and the ends, is Avalonia's: a recorded deviation (PLAN.md, "Scroll physics").

- `Padding` insets what scrolls, and clips it there, as the phone's does.
- `AllowAutoHide="False"`, Avalonia's, keeps the indicator shown.
- A disabled ScrollViewer draws its indicator at half.

## Behaviours

- **The style**: the phone's setters, `VerticalScrollBarVisibility` `Visible` and `HorizontalScrollBarVisibility`
  `Disabled`, no padding, border or background.
- **The indicator**: the bar 7 wide (`ScrollBarMinThemeWidth`), its thumb 2.4 (`ScrollBarPanningThumbThemeWidth`)
  centred in it, as long as the viewport is to the extent and at least 7, placed by the offset, in
  `ScrollBarPanningBackgroundThemeBrush`; hidden when there is nothing to scroll.
- **When**: on a scroll, once loaded, and while a mouse or a pen moves over the ScrollViewer (a move the content
  handles doesn't count, as in WinRT). It lasts the template's
  0.1 s, and goes 0.05 s into the transition, unless the scroll moved again.
- **Which**: the touch's, unless a mouse or a pen was last over it (then the mouse's, until a touch presses or a key
  goes down). The phone's template shows a mouse nothing; ours shows the same thin indicator: a deliberate difference.
- **The bar-less theme**: `Wp81ScrollViewerScrollBarlessTheme`, the phone's `ScrollViewerScrollBarlessTemplate`, the
  same presenter with no bars.
- **A Thumb** an app places has the phone's Thumb style, and its Pressed state.
- **A ScrollBar on its own**, outside a ScrollViewer, shows the thin indicator and its thumb can be dragged; the phone's
  would draw nothing (a deliberate difference, so a bar an app or a library places isn't lost).

The tests are `tests/Cornflower.Tests/Controls/ScrollViewerTests.cs` and `Sample/ScrollViewerPageTests.cs`; PLAN.md
lists each behaviour beside its test.

## How a later control holds it

Every later Tier 2 control (ListView, Pivot, Hub, Page) holds this ScrollViewer. In its template:

- **With the phone's indicators** (ListView, a Page's content): put a plain `ScrollViewer`, with no `Theme`. It takes
  this theme by type, with the indicators. Template-bind the attached `ScrollViewer.*` values the phone's template
  binds (`HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility`, `IsScrollChainingEnabled`,
  `IsDeferredScrollingEnabled`, `BringIntoViewOnFocusChange`, and Avalonia's `AllowAutoHide`), as the phone's ListView
  does. Name it as the control's own code looks for it (Avalonia's `ListBox` finds `PART_ScrollViewer`).
- **With no indicators** (Hub, Pivot, and LoopingSelector, whose phone templates set
  `ScrollViewerScrollBarlessTemplate`): set `Theme="{DynamicResource Wp81ScrollViewerScrollBarlessTheme}"`, and the
  snap points and visibilities on the ScrollViewer, as the phone's do (Pivot: `HorizontalSnapPointsType`
  `MandatorySingle`, `HorizontalSnapPointsAlignment` `Center`). The bar-less theme binds them to its presenter.
- **The parts**, in both: `PART_ContentPresenter`, the `ScrollContentPresenter`, with Avalonia's gesture recogniser;
  where scroll physics of our own would go if they become a part class, so a control holding the ScrollViewer gets them with no
  change. In the indicator theme only: `PART_VerticalScrollBar` and `PART_HorizontalScrollBar`.
- **Don't** style the ScrollBars from the control's theme: the indicator's state comes from the ScrollViewer
  (`PhoneScrollIndicators.IndicatorMode`, internal), passed to its bars by the ScrollViewer theme.
- **Inside a ScrollViewer**: a control whose drag isn't a scroll (Slider, ToggleSwitch) sets `PhoneNoManipulation`, as
  the phone sets `ManipulationMode="None"`.

**TextBox** keeps its own ScrollViewer theme, `Wp81TextBoxScrollViewerTheme` (#101). The phone's TextBox holds a plain
ScrollViewer with both bars `Hidden`, so it draws nothing; Avalonia's TextBox sets its ScrollViewer's horizontal
visibility itself (`Auto` with `AcceptsReturn`), so on this theme a multi-line TextBox would show an indicator the
phone's doesn't. Its theme is the bar-less one by another name; the PR proposes it move onto
`Wp81ScrollViewerScrollBarlessTheme`, so the presenter, and the physics if they become a part, are in one place.

## Resource keys

The phone's own: `ScrollBarMinThemeWidth`, `ScrollBarMinThemeHeight`, `ScrollBarPanningThumbThemeWidth`,
`ScrollBarPanningThumbThemeHeight`, `ScrollBarPanningBackgroundThemeBrush`, `ThumbBackgroundThemeBrush`,
`ThumbBorderThemeBrush`, `ThumbPressedBackgroundThemeBrush`, `ThumbPressedBorderThemeBrush`, in `Tokens/`.
Cornflower's: `Wp81ScrollViewerScrollBarlessTheme` (a `ScrollViewer` theme) and `Wp81ScrollBarPanningThumbTheme` (the
indicator's `Thumb`).

## The ladder

Rung 1, the themes; rung 2, `PhoneScrollIndicators`, set by the ScrollViewer theme, and `PhoneIndicatorFade`, set by
the ScrollBar theme. The physics are a recorded deviation: Avalonia's inertia, no snap points by default, no squash at
the ends (PLAN.md has the phone's numbers beside Avalonia's, and a part class as the way back). No type is
substituted.

## Known gaps

- **The physics**: Avalonia's inertia, with a long tail (2.2 to 3.4 s where the phone's stops in 0.8 to 2.1), no snap
  points by default (the phone's flick ends on a StackPanel's child), and no squash at the ends: a recorded deviation.
- **One raw pixel** here and there: the thumb's length, its centring, a slot's width (ADR 0003's rounding).
- **A press in the `Padding`** doesn't pan: Avalonia's gesture recogniser is the presenter's, and the `Padding` is
  outside it. The phone's pans from anywhere in the ScrollViewer.
- **Zoom, rails and scroll modes**: WinRT's, which Avalonia's ScrollViewer lacks.
