# Grouped lists (ListViewHeaderItem, GridViewHeaderItem): plan

[#131](https://github.com/wdcossey/Cornflower/issues/131). A grouped ListView and a grouped GridView: each group's
header, then its items, in one list, as an 8.1 app writes with a grouped `CollectionViewSource` and a `GroupStyle`.

**Sources.** The SDK's `generic.xaml` for `ListViewHeaderItem`, `GridViewHeaderItem` and `GroupItem` (read as a
specification, ADR 0002; the generated checklist is the ListView's, below), and #211's reads: the probe
`20261118-2221-dev11` (`-Probe grouped`) and its recording `20261118-2249-dev11`, in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "Tier 4's reads", Grouped lists, with the frames in
[`ref/wp81/ListView/`](../../../ref/wp81/ListView/README.md) and [`ref/wp81/GridView/`](../../../ref/wp81/GridView/README.md).

## What Avalonia 12.1.4 offers

Nothing for grouping (checked in #119, ListView's PLAN): no `GroupStyle`, no collection view, no header item, no
`GroupItem`. What it does offer, read from the 12.1.4 assembly:

- **An item that is its own container.** `ListBox.NeedsContainerOverride` makes no `ListBoxItem` for an item that
  already is one. So a header can be an item of the list: a `ListBoxItem` subclass placed in `ItemsSource`.
- **The panels lay a header out as an item.** `VirtualizingStackPanel` (the ListView's) and `WrapPanel` (the
  GridView's) both measure a child with the list's width, so a header that asks for all of it fills its row, and the
  next item starts under it.
- **Selection is by index into `ItemsSource`.** `SelectedIndex`, `Selection` and the keys all count what is in
  `ItemsSource`. An item selects in `ListBoxItem`'s own `OnPointerPressed`, `OnPointerReleased` and `OnKeyDown`;
  arrow keys skip a container that isn't `Focusable` (`ItemsControl.GetNextControl`).
- **`ItemContainerTheme` is put on every container whose type its `TargetType` takes**, own containers too
  (`ItemsControl.PrepareItemContainer`), so the GridView's item theme would land on a header.
- **`ContainerPrepared`, `GetRealizedContainers`, `ContainerFromIndex`, `ScrollIntoView(int)`**: public, enough to
  find a header and put it at the top.

## The API

Two attached properties on `ListBox`, in `ListBoxOptions` (Cornflower.Controls), and one control:

```xml
<ListBox cf:ListBoxOptions.GroupedItemsSource="{Binding Groups}"
         cf:ListBoxOptions.GroupHeaderTemplate="{StaticResource LetterHeader}"
         ItemTemplate="{StaticResource Contact}" />
```

- **`GroupedItemsSource`** (`IEnumerable?`): the groups, WinRT's `CollectionViewSource` with `IsSourceGrouped`. Each
  group is an `IEnumerable` of its items, as `IGrouping<TKey, TItem>` is (`list.GroupBy(...)`). The list's
  `ItemsSource` becomes the groups flattened: each group's header, then its items. A group with no items shows no
  header (WinRT's `GroupStyle.HidesIfEmpty`, here always on). A change to the groups, or to a group that raises
  `CollectionChanged`, lays the list out again; the items still there stay selected (Avalonia's selection keeps them across the new `ItemsSource`).
- **`GroupHeaderTemplate`** (`IDataTemplate?`): WinRT's `GroupStyle.HeaderTemplate`. Its data is the group (so
  `{Binding Key}` for an `IGrouping`).
- **`ListViewHeaderItem`** (Cornflower.Controls): the header container, WinRT's `ListViewHeaderItem`, made by the list,
  one per group, its `Content` the group. A `ListBoxItem` subclass, so the list takes it as its own container. It is
  never selected: it doesn't select on a press or a key, isn't `Focusable` (the arrow keys pass it), and a selection
  that takes it in (select all, a Shift range, or code) lets it go. It never clicks (`ItemClick`). It asks for the
  list's whole width. A GridView's headers are the same type: WinRT's `GridViewHeaderItem` adds nothing to
  `ListViewBaseHeaderItem` and has the same template (the reads), so one type and one theme serve both.
- **For SemanticZoom (#215), internal:** `ListBoxOptions.ScrollGroupToTop(list, group)` puts a group's header at the
  top of the list (as far as the list scrolls). The jump list's groups are the app's own `GroupedItemsSource`.

The themes, in Cornflower.Themes.Wp81: `Controls/ListViewHeaderItem.axaml`, the header's `ControlTheme` by type; and
in `Controls/ListView.axaml`, the ListView's template gains the sticky header, below.

## The sticky header

The ListView's header sticks at the top; the GridView's doesn't (the recording). On the phone this is the panel's:
`ItemsStackPanel` pins, `ItemsWrapGrid` doesn't. So here: the ListView's template (and so the GridView's, based on it)
holds an empty, hidden `Border` named `PART_StickyHeader` over the top of its ScrollViewer. A grouped list puts a
`ListViewHeaderItem` in it (an ungrouped list's template holds no `ListBoxItem` of its own, which the AutoSuggestBox's
and ListPickerFlyout's tests count), and, if its items panel is a vertical stack (`VirtualizingStackPanel` or
`StackPanel`), shows it with the group whose items are at the top of the viewport, once that group's own header has
gone above the top; the next group's header pushes it up as it arrives. Ours follows the list's `LayoutUpdated`. The
GridView's `WrapPanel` never shows it. The `Border` is drawn over the page's brush
(`ApplicationPageBackgroundThemeBrush`), so the items going under it don't show through: in the recording nothing of
"a item 4" shows round the pinned "a", though it sits under it.

## Behaviours

Each with its evidence and its test in `tests/Cornflower.Tests/Controls/GroupedListTests.cs`.

| Behaviour | Evidence | Test |
|---|---|---|
| Header: transparent, no margin or padding, content Left and Top, the phone's font keys | the file; the probe's log | `The_Headers_Style_Sets_What_The_Phone_Read` |
| Header: the list's width, its content's height; items straight under it, the next header straight after the last item | the probe: header a at 70, item 1 at 100, header c at 295.8; width 412.08 | `A_Grouped_ListView_Lays_Out_As_The_Probe_Read` |
| GridView: each group starts a row under its header; headers 69.2 apart for one row of five | the probe | `A_Grouped_GridView_Lays_Out_As_The_Probe_Read` |
| Empty groups show nothing | the probe (`HidesIfEmpty`, b and e) | as the two above |
| No `GroupItem` | the probe's trees | nothing made (the layout tests find only headers and items) |
| A press on a header tilts it | the probe: `Pressed` runs `PointerDownThemeAnimation` | `A_Press_Tilts_A_Header` |
| A header doesn't select, by touch, mouse, select all or code; `AlwaysSelected` takes the first item | WinRT's headers aren't items | `A_Header_Is_Never_Selected` |
| The arrow keys pass a header | as above | `The_Arrow_Keys_Pass_A_Header` |
| A header doesn't click | as above | `A_Header_Never_Clicks` |
| The GridView's item theme doesn't land on its headers | Avalonia's `PrepareItemContainer` | `A_GridViews_Headers_Wear_The_Header_Theme` |
| The ListView's header sticks, pushed up by the next | the recording | `A_ListViews_Header_Sticks_At_The_Top` |
| The GridView's doesn't | the recording | `A_GridViews_Header_Scrolls_Away` |
| A change to a group lays the list out again, keeping the selection | WinRT's view follows its source | `A_Change_To_A_Group_Shows_And_Keeps_The_Selection` |
| A new `GroupHeaderTemplate` takes effect, the pinned header's too | no dead props | `A_New_Header_Template_Takes_Effect` |
| `GroupedItemsSource` cleared: the list is empty | no dead props | `Clearing_The_Groups_Clears_The_List` |
| `ScrollGroupToTop` | for #215 | `A_Group_Scrolls_To_The_Top` |

## The visual states

The header's (the file): `Normal`, `PointerOver` and `Disabled` change nothing; `Pressed` runs
`PointerDownThemeAnimation` on `TiltContainer`, and every way out of it `PointerUpThemeAnimation`. So the theme's
template is `TiltContainer` (a `Border` in the header's `Background`) round the presenter, with `Tilt.IsEnabled`: the
same tilt as the ListView's items. The phone goes `Pressed` > `PointerOver` on release, then `Normal` 355 ms later;
`PointerOver` draws nothing, so both return the tilt the same way, and there is nothing more to draw.

## Decided on the user's behalf

- **`SelectedIndex` counts the headers.** WinRT's `SelectedIndex` counts items alone; here the headers are in
  `ItemsSource`, and Avalonia's selection is by index into it (`SelectedItem` and `SelectedItems` never hold a header).
  Counting items alone would need the headers outside the list's items: drawn by a panel of our own for each of the
  ListView and the GridView, or a selection model of our own that maps indexes, at a cost far past this issue's. To
  reverse: a `GroupedItemsSource` that drives such panels.
- **Empty groups are always hidden** (WinRT's `HidesIfEmpty` true, its default false): the reads' and SemanticZoom's
  case. To reverse: a `ListBoxOptions.GroupHidesIfEmpty`.
- **A group is an `IEnumerable` of its items** (no `ItemsPath`): `IGrouping` needs nothing more. To reverse: a
  `GroupItemsPath`.
- **One header type for both lists**, `ListViewHeaderItem`: WinRT's `GridViewHeaderItem` has no member and no template
  of its own. To reverse: a `GridViewHeaderItem` subclass, made when the panel is a `WrapPanel`.
- **The header isn't focusable**, so the arrow keys and Tab pass it; on the phone it has no keys to take. To reverse:
  `Focusable` back on, with the keys still taken.
- **The pinned header is drawn over `ApplicationPageBackgroundThemeBrush`**: the phone's header is transparent, but
  nothing scrolling under it shows. On a page with a background of its own, the app restyles `PART_StickyHeader`'s
  `Background`. To reverse: `Transparent`.
- **A header taken into the selection is let go just after**, posted to the dispatcher: changed while the list raises
  its `SelectionChanged`, Avalonia's `SelectedItems` takes the items in twice. So after select all, a Shift range or a
  header's index from code, the app sees one `SelectionChanged` with the header and one without. To reverse: a
  selection model of our own.
- **The list's `Padding` doesn't move the pinned header**, which spans the list's width less its border.

## Checklists

The generated checklists are the ListView's, `docs/controls/ListView/PLAN.md`, "ListViewHeaderItem" and "GroupItem"
(`dotnet run --project tools/winrt-api -- checklist ListViewHeaderItem ...`), ticked here. `GridViewHeaderItem`'s is the
same rows: the class has no members of its own, and its default style is `ListViewHeaderItem`'s, setter for setter
(the reads: "one template").

### ListViewHeaderItem (and GridViewHeaderItem)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| ✓ | `ContentTransitions` | `TransitionCollection` | none | not applicable: no transitions run on a header's content in the reads |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Avalonia's `IDataTemplate` chooses itself: `GroupHeaderTemplate` takes any |
| ✓ | `ContentTemplate` | `DataTemplate` | `ContentTemplate` | from `ListBoxOptions.GroupHeaderTemplate` |
| ✓ | `Content` | `Object` | `Content` | the group |
| [-] | `ContentTemplateRoot` | `UIElement` | none | Avalonia's `Presenter.Child` |
| [-] | `OnContent*Changed` (3, protected) | | `OnPropertyChanged` | not needed |

| ✓ | Setter / part / state | Phone | Ours |
|---|---|---|---|
| ✓ | `FontFamily` | `ContentControlThemeFontFamily` | the same key |
| ✓ | `FontSize` | `ControlContentThemeFontSize` | the same key |
| ✓ | `Background` | `Transparent` | `Transparent` |
| ✓ | `HorizontalContentAlignment` | `Left` | `Left` |
| ✓ | `VerticalContentAlignment` | `Top` | `Top` |
| ✓ | part `TiltContainer` | `Border` | `Border#TiltContainer`, the template's root, which `Tilt` tilts |
| ✓ | `Pressed` | `PointerDownThemeAnimation` on `TiltContainer` | `Tilt.IsEnabled` |
| ✓ | `Normal`, `PointerOver`, `Disabled` | nothing | nothing |
| ✓ | `Pressed` → `PointerOver`, → `Normal`; `PointerOver` → `Normal` | `PointerUpThemeAnimation` | `Tilt`'s return |

### GroupItem

| ✓ | Row | Action |
|---|---|---|
| [-] | every row | not applicable: the phone's modern panels (`ItemsStackPanel`, `ItemsWrapGrid`) make no `GroupItem` (the reads: none in either tree); it is the older panels' |

## Dependencies

ListView (#119) and GridView (#213) themes, `Tilt`, `ListBoxOptions`' item click (#132). Owed: the device run on the
Pixel 8 and the real-window run.
