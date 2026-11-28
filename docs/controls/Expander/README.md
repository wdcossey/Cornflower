# Expander

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#239](https://github.com/wdcossey/Cornflower/issues/239) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `Expander`, themed by type in `src/Cornflower.Themes.Wp81/Controls/Expander.axaml`. On the phone this was a
pattern, not a control: a heading a tap opens to show more. The heading is a group's (`GroupHeaderTextBlockStyle`'s
look), with Lucide's chevron after it; the content shows at once, under it.

## Usage

```xml
<Expander Header="advanced">
  <StackPanel>
    <CheckBox Content="use cellular data" />
  </StackPanel>
</Expander>
```

## What it sets

| Property | Value |
|---|---|
| `HorizontalContentAlignment`, `VerticalContentAlignment` | `Stretch` |
| `Template` | the header toggle over (or beside) the content, no box (below) |

`ContentTransition` isn't set: Simple's `CrossFade` is left out (rule 8), so it opens and shuts at once. An app's own
transition still runs.

## Resource keys

- `Wp81PlainHeaderToggleButtonTheme`, in `Controls/PlainHeaderToggleButton.axaml` (a part theme, by `DynamicResource`):
  the header `ToggleButton` inside a larger control. Transparent, no border, nothing drawn checked or pressed (the host
  draws the open state and decides the tilt), no size of its own, the content `PhoneDisabledBrush` disabled. The
  `TreeViewItem`'s chevron and the `DrawerPage`'s drawer buttons wear it too.
- `Wp81PlainGroupHeaderTheme` (`Controls/PlainGroupHeader.axaml`, from #236): the heading's look, `PhoneMidBrush`.
- `PhoneButtonMinHeight` (57.5): the header's least height, as it is pressed on its own (rule 6).
- `CornflowerPlainChevronMargin` (9.5 before the chevron), from #235.

## The template's parts

- `PART_Border`: bound to the app's `Background`, `BorderBrush`, `BorderThickness` and `CornerRadius`, all unset, so no box.
- `PART_toggle`: the header, a `ToggleButton` in `Wp81PlainHeaderToggleButtonTheme`, bound two ways to `IsExpanded`, with
  the tilt (rule 7). Docked on the side the content comes from: top for `ExpandDirection` `Down`, bottom for `Up`, left for
  `Right`, right for `Left`.
- `PART_HeaderPresenter`: the `Header` and `HeaderTemplate` in `Wp81PlainGroupHeaderTheme`. The name Avalonia looks for, so
  a header that is a control is the Expander's logical child.
- `PART_Chevron`: Lucide's chevron (rule 10), in `PhoneMidBrush`: pointing where the content will come from, and back once
  open (down then up for `Down`). Open is no state for the accent (rule 3).
- `PART_ContentPresenter`: inside the `Padding`, shown while `IsExpanded`, with the phone's 1.33 em line.

Disabled, the heading and the chevron are `PhoneDisabledBrush`.

## Known gaps

- Simple's toggle sets the header's `HorizontalAlignment` from the Expander's; here the header fills the width.
- The heading's font and brush are the part theme's: an app's `Foreground` colours the content only.
- Lucide in a plain theme is a new use of a third-party set (ADR 0007, rule 10).
