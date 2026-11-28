# TableViewColumnHeader

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#242](https://github.com/wdcossey/Cornflower/issues/242) · its look on the Pixel done 2026-11-28 (no timings)

Avalonia 12's `TableViewColumnHeader`, themed by type in
`src/Cornflower.Themes.Wp81/Controls/TableViewColumnHeader.axaml`: a [`TableView`](../TableView/README.md) column's
header, in small print over the page.

## What it sets

| Property | Value |
|---|---|
| `Foreground` | `PhoneMidBrush`: small print, as `ControlContextualInfoTextBlockStyle` (the plan's rule 2 names a column header there) |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal` at `TextStyleMediumFontSize` (16), in the table's weight (Simple's is SemiBold) |
| `Background` | `Transparent` |
| `Padding` | `CornflowerPlainTableHeaderPadding`, 0,4.8,9.5,4.8: the cells' 9.5 after it, so it starts where its column's text does, and the control header's 4.8 above and below |
| `VerticalContentAlignment` | `Center` |
| `Template` | `PART_ContentPresenter` (with `PhoneHeaderLine`'s 1.33 em line) and `PART_Resizer` |

Its horizontal alignment is its column's (`TableViewColumn.HorizontalContentAlignment`, `Left` by default), so a
header stands over its column's text. The text is as the app writes it: the phone's headers are lower case because apps wrote them so, and the theme
doesn't change an app's text. Avalonia's table has no sorting, so there's no sort glyph. A header isn't pressed, so it
has no tilt. The strip of headers has the row's rule under it (`TableView`'s `PART_HeaderRow`).

## The template's parts

- `PART_ContentPresenter`: the header.
- `PART_Resizer`: the `Thumb` Avalonia's header drags to resize its column, enabled when the table's
  `CanUserResizeColumns` and the column allow it. In `Wp81PlainColumnResizerTheme` (keyed, in the same file): a
  transparent bar with nothing drawn (Simple draws a 1 line), 9.5 wide (`CornflowerPlainSplitterMinSize`, the
  GridSplitter's least), over the 9.5 after the header's text, so it covers the gap between the columns. No tilt.

## Known gaps

- On the phone the resizer is a 9.5 strip a finger can find but not easily: a table that shouldn't be resized sets
  `CanUserResizeColumns="False"`.
