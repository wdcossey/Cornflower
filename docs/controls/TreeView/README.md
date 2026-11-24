# TreeView

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#239](https://github.com/wdcossey/Cornflower/issues/239) · a look on the Pixel is owed (no timings)

Avalonia's `TreeView`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TreeView.axaml`. The phone had no tree:
an 8.1 app showed one as a grouped `ListView`, a `SemanticZoom` or a page per level. This is a list of plain
[`TreeViewItem`](../TreeViewItem/README.md) rows with no box, over the page.

## Usage

```xml
<TreeView>
  <TreeViewItem Header="music" IsExpanded="True">
    <TreeViewItem Header="albums" />
    <TreeViewItem Header="artists" />
  </TreeViewItem>
  <TreeViewItem Header="videos" />
</TreeView>
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent` |
| `Foreground` | `PhoneForegroundBrush`, which the items inherit |
| `FocusAdorner` | none, as the phone's lists have none |
| `ScrollViewer.HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility` | `Auto` |
| `ScrollViewer.IsScrollChainingEnabled` | `True` |
| `Template` | a border (the app's), Wp81's `ScrollViewer`, the items |

No border and no padding of its own (Simple's has a 1 border and 4 padding): the page insets it.

## The template's parts

- `PART_Border`: bound to the app's `BorderBrush`, `BorderThickness` and `CornerRadius`, unset.
- `PART_ScrollViewer`: Wp81's by type, over the `Background`; it pans sideways once a deep tree is wider than its slot.
- `PART_ItemsPresenter`: inside the `Padding`.

## Known gaps

- No grouping, no zoom: a long tree is one list.
