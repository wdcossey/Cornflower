# CarouselPage

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#238](https://github.com/wdcossey/Cornflower/issues/238) · a look on the Pixel is owed (no timings)

Avalonia 12's `CarouselPage`, themed by type in `src/Cornflower.Themes.Wp81/Controls/CarouselPage.axaml`. The phone had
no CarouselPage. Here it is **a FlipView of pages**: Avalonia's CarouselPage drives a `Carousel`, which Wp81 already themes
by type as the phone's FlipView (`Controls/Carousel.axaml`, [FlipView](../FlipView/README.md)), so the pages swipe, snap
and slide with the FlipView's own motion. A header-less Pivot was the other choice; it would need a `Pivot` as the part,
which the CarouselPage doesn't drive.

## Usage

```xml
<CarouselPage>
  <ContentPage Header="one">...</ContentPage>
  <ContentPage Header="two">...</ContentPage>
</CarouselPage>
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `ApplicationPageBackgroundThemeBrush`, as Wp81's `ContentPage` |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` |
| `ItemsPanel` | Wp81's `PhoneFlipViewPanel`, which the FlipView's motion moves. The CarouselPage hands its own `ItemsPanel` to the Carousel over the Carousel theme's, so it is set here; an app's own panel wins, and drops the FlipView's motion |

`IsGestureEnabled` (Avalonia's `True`) is the Carousel's `IsSwipeEnabled`.

## Resource keys

No new keys.

## The template's parts

- A `Panel` with the `Background`, round `PART_Carousel`: a `Carousel` with no theme of its own, so Wp81's FlipView by
  type, 2.5 in from each side as the phone's FlipView.

## Known gaps

- The FlipView's 2.5 inset shows the page's background round the pages.
- No page indicator: the phone's FlipView has none.
