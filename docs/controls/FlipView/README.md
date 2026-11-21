# FlipView and FlipViewItem

Status: done · [#214](https://github.com/wdcossey/Cornflower/issues/214) · against #211's captures (run `20261118-2210-dev11`) and probes (`20261118-2213-dev11`, `20261118-2216-dev11`) · device run passed 2026-11-21 on the local Android emulator (AVD `cornflower_pixel8`: the Pixel 8 profile, 1080 × 2400 at 420 dpi, API 35; the Pixel 8 was unavailable), on `214c70f`: at rest the item fills the band less 2.5 a side, no buttons or indicator; a slow short drag goes back, a flick goes on; past the first the item follows the finger, damped, and comes back; the vertical one goes on along Y and a sideways swipe on it moves nothing; the Pixel 8 run is still owed (the feel and the timing) · real-window run passed 2026-11-21 (`tools/real-window/drive.ps1`, Debug, on `214c70f`: 215 of 215)

The Windows Phone 8.1 FlipView: Avalonia's `Carousel` in the Wp81 theme, by type (`src/Cornflower.Themes.Wp81/Controls/Carousel.axaml`),
with the phone's swipe, snap and ends (`PhoneFlipView`, `PhoneFlipViewMotion`) and an items panel of the theme's
(`PhoneFlipViewPanel`). The plan, with every value and its source and the fits, is [PLAN.md](PLAN.md); the phone's capture
beside ours and every behaviour marked is in [COMPARISON.md](COMPARISON.md).

## Usage

Every `Carousel` is the 8.1 FlipView:

```xml
<Carousel ItemsSource="{Binding Photos}" ItemTemplate="{StaticResource Photo}" Height="400" />

<!-- Up and down: Avalonia's axis, a vertical PageSlide. The transition itself never runs. -->
<Carousel ItemsSource="{Binding Pages}">
  <Carousel.PageTransition>
    <PageSlide Orientation="Vertical" />
  </Carousel.PageTransition>
</Carousel>
```

- Give it a size: an item is the FlipView less 2.5 a side. The strip moves by the panel's translate, so a frame of the
  motion costs no layout.
- `IsSwipeEnabled` (Avalonia's) is on in the theme; set it off and a finger drags nothing.
- `SelectedIndex` set from code moves one item by the phone's animation; two or more, it jumps. `Next()`, `Previous()`
  and the arrow keys (Avalonia's) are changes of one.
- The motion runs at `CornflowerConfig.ControlSpeed` (or `MotionSpeed`), read as each move starts; `Animated` off, it
  ends at once.

## Behaviours

- **At rest**: the selected item fills the FlipView less 2.5 a side; no buttons, no indicator, no states. The items
  abut, a page apart.
- **The swipe**: a touch or pen drag more along the axis than across it, past 5 px, moves the items one for one, up to a
  page either way; a mouse drags nothing (the shared `SidewaysSwipeGestureRecognizer`). A ScrollViewer in an item keeps a
  drag along its own axis, and a part with `PhoneNoManipulation` (the Slider, the ToggleSwitch) keeps its own.
- **Let go**, the Pivot's rule: on where the offset plus its speed over 385 ms passes 0.52 of a page (231.4 px of 445; the
  phone's held releases went back at 221.0 and on from 233.8), else back. On: the Pivot's swipe curve, 515.625 ms. Back:
  `BackCurve`, 640.625 ms, which first carries on a little the way it was dragged, as the phone's does.
- **`SelectionChanged`** comes with the snap's last frame.
- **The ends**: past the first item the items follow 139.7/150 of the finger, past the last 99.35/150, and come back by
  the snap back's curve. It never goes round.
- **A press while it snaps** stops it where it is. A drag goes on from there; let go without one, the rule is read from
  where it stopped. A press during a move from code doesn't hold it; a drag then ends it at its target, as the Pivot's.
- **The page round it doesn't scroll** to the selection: `AutoScrollToSelectedItem` is off in the theme, as the selected
  item is laid out a page along before it slides in.
- **A change to the items** puts the strip at rest at the selection; an index shifted by one with the same item selected
  doesn't animate.

## Resource keys

None of its own. The phone's `FlipViewButton*` brushes are in `Tokens/Colors.axaml` from the token import, unused: the 8.1
phone's template has no buttons.

## The template's parts

`Border` (2.5 margin, `Background`, `BorderBrush`, `BorderThickness`, clips) round `ItemsPresenter` (`PART_ItemsPresenter`)
with `PhoneFlipViewPanel`. FlipViewItem is the Carousel's container, a `ContentPresenter`, which draws nothing of its own.

## The ladder

Rung 1 for the template and the items panel; rung 2 for the motion (`PhoneFlipView.IsEnabled`, set by the theme). No type
substitution: an app writes `Carousel`.

## Extensions

None.

## Known gaps

- **Not virtualising.** Every item is made and laid out; the phone's panel is a `VirtualizingStackPanel`. A FlipView of
  hundreds of items costs what an `ItemsControl` of them does.
- **A replaced `ItemsPanel`** has nothing moving it: the items lie as that panel lays them, and the swipe moves nothing
  you can see.
- **A slow release that goes on** (held past half a page, or a short drag at 0.4 to 0.6 px/ms) snaps by the swipe's curve,
  which starts fast; the phone's starts at the drag's speed. 4 to 36 px rms off over the snap.
- **Releases near the line**: a 34.4 px drag let go at a read 0.39 px/ms went on on the phone and goes back here, by the
  Pivot's rule (the read had too few events to trust its speed).
- **No scroll chaining**: a ScrollViewer in an item at its end doesn't hand the drag on to the FlipView.
- **`ViewportFraction`** (Avalonia's, to show the neighbours) isn't read.
- **A mouse drags nothing**, where Avalonia's own swipe takes a mouse too: touch and pen only, as the Pivot's and Hub's.
  `IsSwiping` (Avalonia's) stays false while a finger drags: its setter isn't Avalonia's public API.
- **An `Orientation` changed in place** on the `PageSlide` is read at the next press, not at once.

## The checklist

Ticked in [PLAN.md](PLAN.md#checklists).
