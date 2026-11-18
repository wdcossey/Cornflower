# ListView and ListViewItem

Status: done · [#119](https://github.com/wdcossey/Cornflower/issues/119) · press compared against [#113](https://github.com/wdcossey/Cornflower/issues/113)'s probes (runs `20261115-1941-dev11`, `-1948-`) · the check mode's press, Extended and a `SelectedIndex` from code brought to [#136](https://github.com/wdcossey/Cornflower/issues/136)'s probe (runs `20261115-2352-dev11`, `20261116-0040-dev11`) in [#142](https://github.com/wdcossey/Cornflower/issues/142) · #142's device run passed 2026-11-16 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page listview` on `main` at `ae652d3`, 22:48 to 22:50, cold starts 716 to 826 ms over the run's pages: the page opened in 382 ms) · ListViewHeaderItem and GroupItem deferred with grouping · reorder deferred · device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page listview` on `main` at `fedf781`: cold 738 ms, the page opened in 315 ms, every specimen drawn, the check mode included) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the listview page opened in 37 ms with 287 elements) · owed: a real-window section driving its press, pan and check mode (#124's helpers)

The Windows Phone 8.1 ListView and ListViewItem: Avalonia's `ListBox` and `ListBoxItem`, themed by `Wp81Theme` with the
phone's default styles and the item's template, in `src/Cornflower.Themes.Wp81/Controls/ListView.axaml`; the phone's
check mode by `PhoneListMultiSelect.IsOn`, internal to the theme, which the list sets and its items inherit; the press the Button's tilt, and a press on the check box `PhoneListMultiSelect`'s own. The plan, with every value and
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
  | `Multiple` | `SelectionMode="Multiple,Toggle"` | the check mode: a check box slides in by each item over 0.15 s, and every tap checks or unchecks its item |
  | `Extended` | `Multiple` | no check boxes, as the phone's; a tap selects one, as in Single; Ctrl and Shift choose several, which draws nothing |
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
  In the check mode a press off the box is the same, the box flat.
- **A selection set from code** (`SelectedIndex`) scrolls the list at once, by the least that brings the item into
  view, with no animation, as the phone's (Avalonia's `AutoScrollToSelectedItem`).
- **Selected** turns on the tick, which only the check mode shows: in Single mode a selected item looks like the
  others, as on the phone.
- **The check mode** (`Multiple` with `Toggle`): the box, `CheckBox`'s brushes and the `Wp81.Glyph.CheckMark` tick,
  comes in from 40.5 to the left and the content moves 40.5 to the right, over 0.15 s each way. Every tap toggles its
  item. **Pressed on the box** (CheckboxPressed), the box fills with the accent and tilts about its own centre, the
  content doesn't, within 0.12° of the probe; on release it is at rest at once, with no hold and no return.
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
- **The box's press doesn't follow a move of the finger** (not read).
- **A selection set in code scrolls the item into view**, as the phone's does (#136); Avalonia's request reaches a
  page's ScrollViewer round the list too, which the phone's wasn't read for.
- **In Simple's AutoCompleteBox and file dialog**, which now hold this list, a selection made with the keyboard or the
  mouse draws nothing, until those controls get themes of their own.
- **A selection of several in `Multiple`** (WinRT's Extended, chosen with Ctrl or Shift) draws nothing, as on the phone.
- **The scroll physics** are Avalonia's (the ScrollViewer's recorded deviation).
- **One raw pixel** here and there (ADR 0003).

## The checklist

In [PLAN.md](PLAN.md), "Checklists": ListView, ListViewItem, and ListViewHeaderItem and GroupItem, all deferred.
