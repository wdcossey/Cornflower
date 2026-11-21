# Grouped lists: ListViewHeaderItem and GridViewHeaderItem

Status: built · [#131](https://github.com/wdcossey/Cornflower/issues/131) · against #211's probe (run `20261118-2221-dev11`) and recording (`20261118-2249-dev11`) · owed: the device run on the Pixel 8 and the real-window run

A grouped ListView or GridView, as an 8.1 app writes one with a grouped `CollectionViewSource` and a `GroupStyle`: each
group's header, then its items. Avalonia 12.1.4 has no grouping, so `ListBoxOptions` (Cornflower.Controls) adds it to
Avalonia's `ListBox`, and `ListViewHeaderItem` is the header. The header's theme is
`src/Cornflower.Themes.Wp81/Controls/ListViewHeaderItem.axaml`; the ListView's pinned header is in its template,
`Controls/ListView.axaml`. The plan is [PLAN.md](PLAN.md); the phone beside ours is [COMPARISON.md](COMPARISON.md).

## Usage

```xml
<ListBox cf:ListBoxOptions.GroupedItemsSource="{Binding Contacts}"
         cf:ListBoxOptions.GroupHeaderTemplate="{StaticResource LetterHeader}"
         ItemTemplate="{StaticResource Contact}" />

<DataTemplate x:Key="LetterHeader" x:DataType="local:ContactGroup">
  <TextBlock Text="{Binding Key}" Theme="{DynamicResource GroupHeaderTextBlockStyle}" />
</DataTemplate>
```

- **`GroupedItemsSource`**: the groups, each an `IEnumerable` of its items, as an `IGrouping<TKey, TItem>` is
  (`contacts.GroupBy(c => c.Initial)`): WinRT's grouped `CollectionViewSource`. It sets the list's `ItemsSource` (don't
  set both). A change to the groups, or to a group that raises `CollectionChanged`, shows at once, and the items still
  there stay selected.
- **`GroupHeaderTemplate`**: WinRT's `GroupStyle.HeaderTemplate`, its data the group.
- **A GridView** is the same, in its theme (`Theme="{DynamicResource Wp81GridViewTheme}"`): each group starts a row.
- Selection and item click are the list's: `SelectedItem` and `SelectedItems` hold items, but for one
  `SelectionChanged` after a select all, a Shift range, a header's index from code or `AlwaysSelected`'s first pick,
  when a header is in them until it is let go.

## Behaviours

- **The header**: transparent, no margin or padding, the list's width and its content's height; a group's items start
  straight under it, and the next header comes straight after its last item. A group with no items shows nothing.
- **The press**: a header tilts as an item does; it is never selected (by a tap, a click, the keys, select all or
  code), the arrow keys pass it, and it never clicks.
- **The ListView's header sticks**: panned, the header of the group at the top stays there over its items, until the
  next group's header pushes it off. **The GridView's don't**: they scroll away with their groups.

The tests are `tests/Cornflower.Tests/Controls/GroupedListTests.cs` and `Sample/GroupedListsPageTests.cs`. The sample's
page is `grouped lists`.

## Known gaps

- **`SelectedIndex` counts the headers**: WinRT's counts items alone. Use `SelectedItem` or `SelectedItems`.
- **Empty groups are always hidden** (WinRT's `GroupStyle.HidesIfEmpty` on); there is no `GroupStyleSelector`, and a
  group's items are the group itself (no `ItemsPath`).
- **The pinned header is drawn over `ApplicationPageBackgroundThemeBrush`**; over a page with its own background, set
  `PART_StickyHeader`'s `Background`. The list's `Padding` doesn't move it.
- **A change to a group lays the whole list out again**: the realized items, and the focus in the list, are made anew.
- **No `GroupItem`**: the phone's panels make none.
- The phone's header goes `Pressed` > `PointerOver` on release, then `Normal` 355 ms later; `PointerOver` draws
  nothing, so there is no state to follow: ours runs the tilt's return on release, as the phone's does.
