# TabControl

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#238](https://github.com/wdcossey/Cornflower/issues/238) · a look on the Pixel is owed (no timings)

Avalonia's `TabControl`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TabControl.axaml`. The phone had no
TabControl. Here it takes **the Pivot's headers without its swipe** (ADR 0007, answer 8): its tabs are the plain
[TabItem](../TabItem/README.md)'s headers in a row, in the Pivot header's look, and its content sits under them inset as a
PivotItem. A tap on a header selects it and its content shows at once; a swipe on the content does nothing.

## Usage

```xml
<TabControl>
  <TabItem Header="mail"><TextBlock Text="..." /></TabItem>
  <TabItem Header="calendar"><TextBlock Text="..." /></TabItem>
</TabControl>
```

## What it sets

| Property | Value |
|---|---|
| `Padding` | `PivotItemMargin` (19,22.5,19,0), by `DynamicResource`: round the content, as a PivotItem's inset |
| `HorizontalContentAlignment`, `VerticalContentAlignment` | `Stretch`, as a PivotItem's |
| `ItemsPanel` | A horizontal `StackPanel`: each header at its own width, as the Pivot's |
| `AutoScrollToSelectedItem` | `False`: bringing the selected header into view would scroll the page round the TabControl, at load and at each selection set from code |
| `Template` | The header row over the content (below), transparent at rest (rule 5) |

## Resource keys

No new keys of its own: `PivotItemMargin`, and Wp81's `Wp81ScrollViewerScrollBarlessTheme` for the row. The headers'
one new key is the TabItem's.

## The template's parts

- A `Border` with the app's `Background`, `BorderBrush`, `BorderThickness` and `CornerRadius` (all unset), round a
  `DockPanel`.
- `PART_ScrollViewer`: the header row, Wp81's `ScrollViewerScrollBarlessTheme`, docked by `TabStripPlacement`. It pans
  sideways (`Hidden`, no bar) at the top or bottom, and up and down at the left or right, so a finger reaches a header
  past the edge. The selected header isn't brought into view.
- `PART_ItemsPresenter`: the headers; at the left or right, the `StackPanel` is vertical.
- `PART_SelectedContentHost` and `PART_SelectedContentHost2`: Avalonia's two content hosts (the second for an app's
  `PageTransition`), clipped, inside the `Padding`.

## Known gaps

- No swipe between tabs and no motion of its own (rule 8): an app's `PageTransition` still runs.
- A header selected from code past the edge stays there until the row is panned.
- At the left or right, each header keeps its 16 before it and is centred in the column, with no gap between headers.
- At the left or right, only the theme's `StackPanel` turns vertical: an app's own `ItemsPanel` sets its own orientation.
  An app's `WrapPanel` never wraps, as the row is as wide as its headers.
- The row pans; it doesn't wrap round to put the selected header first, as the Pivot's does.
- The 19 inset at the sides is the Pivot's, for a page-wide control; a TabControl in a narrow box may want `Padding`
  of its own.
