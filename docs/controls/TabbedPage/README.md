# TabbedPage

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#238](https://github.com/wdcossey/Cornflower/issues/238) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia 12's `TabbedPage`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TabbedPage.axaml`. The phone had no
TabbedPage: its Pivot was the page of tabs. Avalonia's TabbedPage drives a `TabControl` with one `TabItem` per page, so
here it is the plain [TabControl](../TabControl/README.md) and [TabItem](../TabItem/README.md) by type, the Pivot's headers
at the top without the swipe, over the page's background as Wp81's `ContentPage`. Simple's keyed tab bar (a uniform row of
icons and small text with an accent pipe, at the bottom on a phone) is left out.

## Usage

```xml
<TabbedPage>
  <ContentPage Header="mail">...</ContentPage>
  <ContentPage Header="calendar">...</ContentPage>
</TabbedPage>
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `ApplicationPageBackgroundThemeBrush`, as Wp81's `ContentPage` |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` |
| `TabPlacement` | `Top`, the Pivot's (Avalonia's `Auto` puts the bar at the bottom on a phone); an app's own wins |

`IsGestureEnabled` stays Avalonia's `False`: no swipe between pages (answer 8).

## Resource keys

No new keys.

## The template's parts

- A `Panel` with the `Background`, round `PART_TabControl`: a `TabControl` with no theme of its own, so the plain one by
  type, and its `TabItem`s the plain ones, with `Padding` 0 rather than the TabControl's `PivotItemMargin`: each page is a
  whole page, filling the space under the headers with its own background. Avalonia's TabbedPage puts the safe area on its
  `Margin` and on the page's.

## Known gaps

- A page's `Icon` isn't drawn, and the TabbedPage's `IndicatorTemplate` isn't either: the Pivot's headers are text,
  with no pipe. A page with an icon and no `Header` has an empty tab.
