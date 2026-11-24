# TabStrip

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#238](https://github.com/wdcossey/Cornflower/issues/238) · a look on the Pixel is owed (no timings)

Avalonia's `TabStrip`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TabStrip.axaml`. The phone had no
TabStrip: here it is the Pivot's header row without the Pivot, its items the plain
[TabStripItem](../TabStripItem/README.md)'s headers, as the plain [TabControl](../TabControl/README.md)'s row.

## Usage

```xml
<TabStrip SelectedIndex="0">
  <TabStripItem Content="all" />
  <TabStripItem Content="unread" />
</TabStrip>
```

## What it sets

| Property | Value |
|---|---|
| `ItemsPanel` | A horizontal `StackPanel`: each header at its own width, as the Pivot's |
| `Template` | The row in Wp81's `Wp81ScrollViewerScrollBarlessTheme` (below) |

## Resource keys

No new keys: the row is Wp81's scrollbar-less `ScrollViewer`, the one the Pivot's and Hub's templates use.

## The template's parts

- `PART_ScrollViewer`: Wp81's `ScrollViewerScrollBarlessTheme`, panning sideways (`Hidden`, no bar), with the app's
  `Background` and `Padding` (both unset). A finger pans it to a header past the edge; the selected header is brought
  into view.
- `PART_ItemsPresenter`: the headers.

## Known gaps

- The row doesn't wrap round to put the selected header first, as the Pivot's does; it pans instead.
