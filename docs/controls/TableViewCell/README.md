# TableViewCell

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#242](https://github.com/wdcossey/Cornflower/issues/242) · its look on the Pixel done 2026-11-28 (no timings)

Avalonia 12's `TableViewCell`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TableViewCell.axaml`: a
[`TableView`](../TableView/README.md) row's cell, its column's value in the row's type and colour.

## What it sets

| Property | Value |
|---|---|
| `Padding` | `CornflowerPlainTableCellPadding`, 0,0,9.5,0: nothing before the text, so the first column's lines up with the page's, and 9.5 after it, so the columns stand 9.5 apart |
| `VerticalContentAlignment` | `Center`, in the row's height |
| `Template` | `PART_ContentPresenter`, with the phone's 1.33 em line (`PhoneHeaderLine`) on the text it makes |

Left-aligned unless the column says otherwise (`TableViewColumn.HorizontalContentAlignment`). No grid lines and no
box: Metro's lists had none, and the row's rule is the only line. A column's own `CellTheme` replaces this theme.
