# GridView and GridViewItem

Status: done · [#213](https://github.com/wdcossey/Cornflower/issues/213) · against #211's captures (run `20261118-2210-dev11`) and probe (`20261118-2219-dev11`) · device run passed 2026-11-21 (Pixel 8, Release, over wireless adb, on `e7fee63`: single tiles abut, a press tilts, the check mode's border and earmarks, taps toggle, a tap at a top-left corner toggles, light; the page opened scrolled down, fixed in `6094ce4`, [#220](https://github.com/wdcossey/Cornflower/issues/220) following up the cause; rechecked on `6094ce4`: it opens at the top, cold start 756 ms, the page opened in 369 ms) · real-window run passed 2026-11-21 (`tools/real-window/drive.ps1`, Debug, on `6094ce4`: 204 of 205; the one failure, 'Alt+Left goes back', stopped on a UI Automation COM error before it checked anything, and passed 4 of 4 run alone) · [#220](https://github.com/wdcossey/Cornflower/issues/220)'s fix (#229), in the theme: its Pixel and real-window runs are owed

The Windows Phone 8.1 GridView and GridViewItem: Avalonia's `ListBox` and `ListBoxItem` in two keyed themes,
`Wp81GridViewTheme` and `Wp81GridViewItemTheme`, based on the ListView's, at the end of
`src/Cornflower.Themes.Wp81/Controls/ListView.axaml`. The plan, with every value and its source, is [PLAN.md](PLAN.md);
the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

A `ListBox` with no style is the 8.1 **ListView** ([ListView](../ListView/README.md)). For the GridView, name its theme;
it gives its items theirs:

```xml
<ListBox Theme="{DynamicResource Wp81GridViewTheme}"
         ItemsSource="{Binding Photos}"
         ItemTemplate="{StaticResource Tile}" />
```

- An item declared on its own, outside such a list, takes `Theme="{DynamicResource Wp81GridViewItemTheme}"`.
- **The check mode** is the ListView's switch: `SelectionMode="Multiple,Toggle"` (WinRT's `Multiple`). `Multiple` alone is
  WinRT's `Extended`, with no check mode, as on the ListView.
- **Item click** is the ListView's: `ListBoxOptions.IsItemClickEnabled="True"` and `ListBoxOptions.ItemClick`
  ([ItemClick](../ItemClick/README.md)); a tap then clicks and selects nothing.
- Give the items one size, as the phone's panel does (below). A GridView in a page's ScrollViewer needs a height.

## Behaviours

- **The panel**: rows fill left to right and wrap, and the list scrolls down, never across; the items abut
  (`GridViewItemMargin` 0, no padding), their content centred both ways.
- **The press**: the ListView item's, the Button's tilt on the whole item inside an untilted root, at once on a press.
- **Selected**, in `Single`, draws nothing.
- **The page round the grid stays put**, as the ListView's (#220, [its behaviours](../ListView/README.md#behaviours)):
  the selection and `ScrollIntoView` scroll the grid alone; the phone's GridView does the same.
- **The check mode**: a 2.5 accent border on every item; a selected one adds the 34 × 34 accent earmark at its top right
  with a white tick; a tap toggles. No check box, and the content doesn't move.
- **Disabled**: the content at 0.4, and no border or earmark.
- **Focus and pointer-over** draw nothing.

The tests are `tests/Cornflower.Tests/Controls/GridViewTests.cs` and `Sample/GridViewPageTests.cs`; PLAN.md lists each
behaviour beside its test. The sample's page is `gridview`.

## Resource keys

The themes' own: `Wp81GridViewTheme`, `Wp81GridViewItemTheme`. The phone's, in `Tokens/`: `GridViewItemMargin`,
`GridViewItemMultiselectBorderThickness`, `ListViewItemSelectedBackgroundThemeBrush`, `ListViewItemCheckThemeBrush`,
and the ListViewItem's (see [ListView](../ListView/README.md)). The glyphs `Wp81.Glyph.SelectedEarmark` and
`Wp81.Glyph.CheckMark`.

## The template's parts

The ListViewItem's, the phone's names: `OuterContainer`, `ReorderHintContent`, `CheckboxTiltContainer` (hidden),
`ContentContainer`, `TiltContainer`, `ContentBorder`, `SelectedBorder`, `SelectedCheckMark`, `SelectedEarmark`,
`SelectedGlyph`; the content presenter is Avalonia's `PART_ContentPresenter`. The list's are the ListView's.

## The ladder

Rung 1, the themes, keyed and based on the ListView's; rung 2, `Tilt` and `PhoneListMultiSelect`, the ListView's. No
type is substituted.

## Extensions

None. The larger-screens pack (ADR 0004) gets the wrap as it is: rows that take as many tiles as the width holds, so a
wider window shows more columns with nothing set.

## Known gaps

- **One size for every item.** The phone's `ItemsWrapGrid` gives every item the first item's size; Avalonia's `WrapPanel`
  gives each its own. Tiles of one size lay out the same.
- **No virtualisation.** `WrapPanel` realises every item; the phone's `ItemsWrapGrid` virtualises.
- **Item click** on a GridView wasn't read; it is assumed to be the ListView's (#148).
- **Grouping** is `ListBoxOptions.GroupedItemsSource` (#131, [GroupedLists](../GroupedLists/README.md)): its headers are
  `ListViewHeaderItem`s, as WinRT's GridViewHeaderItem adds nothing, and don't stick. No add and delete transitions.
- **The scroll physics** are Avalonia's (the ScrollViewer's recorded deviation).
- **One raw pixel** here and there (ADR 0003).

## The checklist

In [PLAN.md](PLAN.md), "Checklists": GridView and GridViewItem.
