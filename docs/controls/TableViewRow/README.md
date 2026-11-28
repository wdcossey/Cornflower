# TableViewRow

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#242](https://github.com/wdcossey/Cornflower/issues/242) · its look on the Pixel done 2026-11-28 (no timings)

Avalonia 12's `TableViewRow`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TableViewRow.axaml`: a
[`TableView`](../TableView/README.md)'s row, which is the plain [`TreeViewItem`](../TreeViewItem/README.md)'s row, so
a table's row reads as a list's.

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent`, so the whole row takes the finger |
| `Padding` | 0 (Simple's is 2,1): the page insets the table, so the first cell's text lines up with the page's |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal` at `TextStyleLargeFontSize` (18.14), the body's type, which the cells inherit |
| `Tilt.IsEnabled` | `True`: the press is the tilt (an item-sized press), with no fill |
| `Template` | `PART_Row`, the plain row, round the cells |

Selected, the row's text is `PhoneAccentBrush`, with no fill (Simple fills it). Disabled, `PhoneDisabledBrush`. No
pointer-over look, no focus adorner (as Simple's row, it sets none), and no motion.

## The template's parts

- `PART_Row`: a `Border` in `Wp81PlainRowTheme` (`Controls/PlainRow.axaml`): the 1.5 rule under the row in
  `PhoneLowBrush` and the phone's touch height (`PhoneButtonMinHeight`, 57.5) as its least, which the Adaptive pack
  can make denser.
- `PART_CellsPresenter`: Avalonia's `TableViewCellsPresenter`, the cells in the columns' widths. The tilt's target:
  it tilts inside the untilted row.
