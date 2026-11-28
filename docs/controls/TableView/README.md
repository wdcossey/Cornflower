# TableView

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#242](https://github.com/wdcossey/Cornflower/issues/242) · its look on the Pixel owed (no timings)

Avalonia 12's `TableView`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TableView.axaml`. The phone had
no table: an 8.1 app showed one as a `ListView` whose item template was a `Grid`. This is that list, on the plain
tree's row: [`TableViewRow`](../TableViewRow/README.md)s with the rule under each, in the body's type, under a strip
of [`TableViewColumnHeader`](../TableViewColumnHeader/README.md)s in small print, with no box and no grid lines.

## Usage

```xml
<TableView ItemsSource="{Binding Files}" SelectedIndex="0">
  <TableView.Columns>
    <TableViewColumn Header="name" Width="*" Binding="{Binding Name}" />
    <TableViewColumn Header="size" Width="90" Binding="{Binding Size}" HorizontalContentAlignment="Right" />
  </TableView.Columns>
</TableView>
```

Write the headers in lower case, as the phone's headers are: the theme shows them as written.

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent`, over the page (Simple's headers sit on a filled strip) |
| `Foreground` | `PhoneForegroundBrush`, which the rows inherit |
| `AutoScrollToSelectedItem` | `False`: a selection at load or from code brings nothing into view, the page or the table's own rows (#220, #251, #252) |
| `ListBoxOptions.KeepsScrollInList` | `True`, as on Wp81's lists: a request for a row (`ScrollIntoView`, a press's focus) stops at the table. Keyboard focus still brings a row into view |
| `ScrollViewer.HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility` | `Auto` |
| `ScrollViewer.IsScrollChainingEnabled`, `IsScrollInertiaEnabled` | `True` |
| `TextBlock.TextTrimming` | `CharacterEllipsis`, as Simple's: a cell's text that doesn't fit ends in an ellipsis |
| `Template` | a border (the app's), the table's `ScrollViewer`, the rows |

No border and no padding of its own (Simple's has a 1 border): the page insets it.

## The template's parts

- `PART_Border`: bound to the app's `BorderBrush`, `BorderThickness` and `CornerRadius`, unset.
- `PART_ScrollViewer`: in `Wp81PlainTableScrollViewerTheme` (keyed, in the same file): Wp81's `ScrollViewer`, its thin
  indicators over the rows, with `PART_HeaderRow` docked above what scrolls. `PART_HeaderRow` holds the
  `TableViewColumnHeadersPresenter`, with no fill and the row's rule under it (`CornflowerPlainRuleThickness`,
  0,0,0,1.5, in `PhoneLowBrush`). A table given a height keeps its headers while its rows scroll, and they follow
  its rows sideways. A table with no height, on a page in a `ScrollViewer`, scrolls with the page, headers and all.
- `PART_ItemsPresenter`: inside the `Padding`.

## Known gaps

- No sorting, grouping or editing: Avalonia's table has none.
- A selection from code in a table with a height of its own doesn't bring its row into view. An app calls
  `ScrollIntoView`, which moves the table's rows and stops there.
