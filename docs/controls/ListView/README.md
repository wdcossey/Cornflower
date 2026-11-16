# ListView and ListViewItem

Status: done · [#119](https://github.com/wdcossey/CornflowerUI/issues/119) · press compared against [#113](https://github.com/wdcossey/CornflowerUI/issues/113)'s probes (runs `20261115-1941-dev11`, `-1948-`) · ListViewHeaderItem and GroupItem deferred with grouping · reorder deferred · device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page listview` on `main` at `fedf781`: cold 738 ms, the page opened in 315 ms, every specimen drawn, the check mode included) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the listview page opened in 37 ms with 287 elements) · owed: a real-window section driving its press, pan and check mode (#124's helpers)

The Windows Phone 8.1 ListView and ListViewItem: Avalonia's `ListBox` and `ListBoxItem`, themed by `Wp81Theme` with the
phone's default styles and the item's template, in `src/Cornflower.Themes.Wp81/Controls/ListView.axaml`; the phone's
check mode by `PhoneListMultiSelect.IsOn`, internal to the theme, which the list sets and its items inherit; the press the Button's tilt. The plan, with every value and
its source, is [PLAN.md](PLAN.md); the phone's captures and measurements beside ours are in
[COMPARISON.md](COMPARISON.md).

## Usage

An 8.1 page's ListView is Avalonia's `ListBox`, with no style:

```xml
<ListBox ItemsSource="{Binding Items}" SelectionChanged="OnSelectionChanged">
  <ListBox.ItemTemplate>
    <DataTemplate>
      <StackPanel Margin="0,0,0,9.5">
        <TextBlock Text="{Binding Title}" Theme="{DynamicResource ListViewItemTextBlockStyle}" />
        <TextBlock Text="{Binding Detail}" Theme="{DynamicResource ListViewItemSubheaderTextBlockStyle}" />
      </StackPanel>
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

- **Selection, as WinRT's `SelectionMode`**:

  | WinRT | Avalonia | What it does |
  |---|---|---|
  | `Single` (the default) | `Single` (the default) | a tap selects one; the phone draws no selection |
  | `Multiple` | `SelectionMode="Multiple,Toggle"` | the check mode: a check box slides in by each item over 0.15 s, and a tap checks or unchecks it |
  | `Extended` | `Multiple` | Ctrl and Shift choose several; the check boxes show them (the phone's wasn't read) |
  | `None` | none | a gap: in Single mode the selection draws nothing anyway |

- A string with no template is drawn small, 12 px, as the phone draws one. Give items a template in the phone's list
  styles, as 8.1 apps do.
- The list holds the phone's ScrollViewer, with its thin indicator; set `ScrollViewer.*` on the ListBox as on the
  phone. A ListBox in a page's ScrollViewer needs a height, as on the phone.

## Behaviours

- **The ListView's style**: `IsTabStop` off, `TabNavigation` once, no horizontal scroll, the vertical indicator
  `Auto`, items in a `VirtualizingStackPanel`, the page's foreground.
- **The item's style**: `ContentControlThemeFontFamily` at `ControlContentThemeFontSize` (12), a transparent
  background, no margin, the content at the top left, its text in the phone's 1.33 em line. Each item at least
  39.17 tall, from the template's (never drawn) selected border.
- **The press**: at once, with the Button's tilt on the item's content (`TiltContainer`), within 0.1° of every press
  #113 probed; on release it holds 182.6 ms and eases back over 167.0 ms. A tap selects on its release. A pan takes the
  pointer from the item: the tilt returns and nothing is selected. A finger dragged off the item lets the tilt return.
- **Selected** turns on the tick, which only the check mode shows: in Single mode a selected item looks like the
  others, as on the phone.
- **The check mode** (any mode with `Multiple`): the box, `CheckBox`'s brushes and the `Wp81.Glyph.CheckMark` tick, comes in
  from 40.5 to the left and the content moves 40.5 to the right, over 0.15 s each way; pressed, the box fills with the
  accent and tilts, instead of the content.
- **Disabled**: the content at 0.4 (Update 1's `ListViewItemDisabledThemeOpacity`), the box and tick in
  `PhoneBaseLowColor`.
- **Focus and pointer-over** draw nothing.

The tests are `tests/Cornflower.Tests/Controls/ListViewTests.cs` and `Sample/ListViewPageTests.cs`; PLAN.md lists each
behaviour beside its test.

## Resource keys

The phone's own, in `Tokens/`: `ContentControlThemeFontFamily`, `ControlContentThemeFontSize`, `ListViewItemMargin`,
`ListViewItemMultiselectCheckBoxMargin`, `CheckBoxBackgroundThemeBrush`, `CheckBoxBorderThemeBrush`,
`CheckBoxBorderThemeThickness`, `CheckBoxForegroundThemeBrush`, `CheckBoxPressedBackgroundThemeBrush`,
`CheckBoxPressedForegroundThemeBrush`, `CheckBoxDisabledBorderThemeBrush`, `CheckBoxDisabledForegroundThemeBrush`,
`ListViewItemDisabledThemeOpacity`, `ListViewItemSelectedBackgroundThemeBrush`, `GridViewItemMultiselectBorderThickness`,
`PhoneForegroundBrush`; and Cornflower's `Wp81.Glyph.CheckMark`. `ListViewItemContentOffsetX` and
`ListViewItemContentTranslateX` are written into the theme as literals, -40.5 and 40.5: a transform's transition can't
take a resource. An app's value for them doesn't move the check mode.

## The template's parts

The phone's names: `OuterContainer`, `ReorderHintContent`, `CheckboxTiltContainer`, `CheckboxOuterContainer`,
`CheckboxContainer`, `NormalRectangle`, `CheckGlyph`, `ContentContainer`, `TiltContainer`, `ContentBorder`,
`SelectedBorder`, `SelectedCheckMark`; the content presenter is Avalonia's `PART_ContentPresenter`. The list's
ScrollViewer is Avalonia's `PART_ScrollViewer`, its presenter `PART_ItemsPresenter`.

## The ladder

Rung 1, the themes, with `PhoneListMultiSelect.IsOn` (the check mode, an inherited attached property of the theme's
own, set on the list by its theme from `SelectionMode`); rung 2, `Tilt` (the press). No type is substituted: an app's `ListBox` is the ListView.

## Extensions

None.

## Known gaps

- **`ItemClick`** is `ListBoxOptions`' ([ItemClick](../ItemClick/README.md), #132).
- **No `SelectionMode` None.**
- **No `Header` or `Footer`** that scroll with the items.
- **No grouping**: so no ListViewHeaderItem or GroupItem (Avalonia 12.1.4 has neither, nor `GroupStyle`). Deferred
  with grouping, beside SemanticZoom.
- **No reorder**, and no add and delete transitions.
- **The check mode's press** is the template's reading, not probed; nor is the phone's `Extended`.
- **A selection set in code scrolls the item into view**: Avalonia's `AutoScrollToSelectedItem`, on by default, and its
  request reaches a page's ScrollViewer round the list too. WinRT's ListView is believed not to (an app calls
  `ScrollIntoView`), but it wasn't probed, so the theme leaves Avalonia's. Set it off where that matters.
- **In Simple's AutoCompleteBox and file dialog**, which now hold this list, a selection made with the keyboard or the
  mouse draws nothing, until those controls get themes of their own.
- **The check mode's press away from the box** tilts the box as far as it goes (`Tilt` clamps to its target); the
  phone's wasn't read.
- **The scroll physics** are Avalonia's (the ScrollViewer's recorded deviation).
- **One raw pixel** here and there (ADR 0003).

## The checklist

In [PLAN.md](PLAN.md), "Checklists": ListView, ListViewItem, and ListViewHeaderItem and GroupItem, all deferred.
