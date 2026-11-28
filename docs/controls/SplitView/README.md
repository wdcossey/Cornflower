# SplitView

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#239](https://github.com/wdcossey/Cornflower/issues/239) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `SplitView`, themed by type in `src/Cornflower.Themes.Wp81/Controls/SplitView.axaml`. The phone had no
SplitView: the pane over or beside the content came with Windows 10. Simple's templates for each `PanePlacement` and
`DisplayMode`, with the plain look and none of Simple's motion.

## Usage

```xml
<SplitView IsPaneOpen="True" DisplayMode="Inline">
  <SplitView.Pane>
    <TextBlock Text="pane" />
  </SplitView.Pane>
  <TextBlock Text="content" />
</SplitView>
```

## What it sets

| Property | Value |
|---|---|
| `PaneBackground` | `FlyoutBackgroundThemeBrush` (`PhoneChromeColor`): a floating surface (rule 5) |
| `Template` | Simple's, per placement and mode (below) |

`OpenPaneLength` and `CompactPaneLength` aren't set: Avalonia's own defaults, 320 and 48 (ADR 0004: no phone width in a
template).

## The template's parts

- `PART_PaneRoot`: the pane, on `PaneBackground`, clipped; its width (or height, at the top or bottom) is its open or closed
  length at once: Simple's transitions are left out (rule 8).
- `PART_PaneEdge`: the 1.5 line (`CornflowerPlainLineWidth`) in `PhoneLowBrush`, on the pane's side that faces the content
  (rule 4: a pane's edge). Square, no shadow.
- `PART_ContentPresenter`: the content, over the SplitView's own `Background`, unset, so the page shows.
- `LightDismissLayer`: over the content while an overlay pane is open, taking the finger; it draws nothing, with
  `UseLightDismissOverlayMode` or without (rule 5: nothing translucent). Simple dims the content there.

No tilt: a pane is a large surface (rule 7).

## Known gaps

- With no dim, an open overlay pane is set apart from the content by its surface and its edge alone.
- 320 is a desktop width: on a 411-wide phone it leaves 91 of the content. The Adaptive pack can set it.
