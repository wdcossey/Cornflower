# GridSplitter

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#235](https://github.com/wdcossey/Cornflower/issues/235) · a look on the Pixel is owed (no timings)

Avalonia's `GridSplitter`, themed by type in `src/Cornflower.Themes.Wp81/Controls/GridSplitter.axaml`. The phone had
none: here it is a bar the size of its slot.

## Usage

```xml
<Grid ColumnDefinitions="*,Auto,*">
  <Border Grid.Column="0" />
  <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
  <Border Grid.Column="2" />
</Grid>
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `PhoneLowBrush`: quiet at rest, as the Separator's line (rule 3) |
| `MinWidth`, `MinHeight` | `CornflowerPlainSplitterMinSize` (9.5), so a finger can find it |
| `FocusAdorner` | none |

Held and dragged (Thumb's `:pressed`), the bar is `PhoneAccentBrush`; disabled, `PhoneDisabledBrush`. No tilt (rule 7:
not on what is dragged), no motion of its own.

## Resource keys

`CornflowerPlainSplitterMinSize`, in `Tokens/Plain.axaml`.

## The template's parts

`Bar`, a `Border` filled with the `Background`.

## Known gaps

- The bar is the splitter's whole size: no thin line inside a wider grip, as `GridSplitter` says nothing of its
  direction to a theme.
- The managed file dialog (a hole, dropped from the plain themes) keeps Simple's GridSplitter, through `HoleParts`.
