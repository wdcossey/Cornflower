# GridView and GridViewItem: plan

[#213](https://github.com/wdcossey/Cornflower/issues/213). The phone's GridView and GridViewItem: keyed `ControlTheme`s
on Avalonia's `ListBox` and `ListBoxItem`, based on the ListView's, in `src/Cornflower.Themes.Wp81/Controls/ListView.axaml`.

**Sources.** The 8.1 Update 1 runtime's `gridview.json` and `gridviewitem.json` (the SDK's `generic.xaml`), read as a
specification (ADR 0002); the reference app's captures of run `20261118-2210-dev11` (#211), in
[`ref/wp81/GridView/`](../../../ref/wp81/GridView/README.md); and the probe of run `20261118-2219-dev11`, its numbers in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "Tier 4's reads", GridView.

## The key

Avalonia's `ListBox` already wears the 8.1 ListView's look by type (#130), and the phone's ListBox is keyed (#212). The
GridView is keyed the same way: **`Wp81GridViewTheme`** for the list and **`Wp81GridViewItemTheme`** for the item. The
list's theme sets `ItemContainerTheme` to the item's by `DynamicResource`, so an app names only the list:

```xml
<ListBox Theme="{DynamicResource Wp81GridViewTheme}" />
```

No `GridView` type of our own: the phone's GridView adds nothing to `ListViewBase` but its panel, and its item nothing
to the ListViewItem's template but a style, so a theme carries both (rung 1). Both themes are `BasedOn` the ListView's
(`{x:Type ListBox}` and `{x:Type ListBoxItem}`), so the GridView takes the ListView's template, its ScrollViewer's values,
its check-mode switch (`SelectionMode` `Multiple, Toggle` sets `PhoneListMultiSelect.IsOn`), its press (`Tilt` on
`TiltContainer`) and `ListBoxOptions`' item click, with nothing copied. A `StaticResource` finds only its own file's
resources, so the two themes sit at the end of `ListView.axaml` rather than in a `GridView.axaml`.

## Behaviours

Each with its evidence, and its test in `tests/Cornflower.Tests/Controls/GridViewTests.cs`.

| Behaviour | Evidence | Test |
|---|---|---|
| The keys: the list's theme gives its items the item's; the panel a horizontal `WrapPanel`; a ListBox with no Theme stays the ListView | #130; the style's `ItemsWrapGrid Orientation="Horizontal"` | `The_Keyed_List_Wraps_And_Gives_Its_Items_The_Keyed_Item_Theme` |
| The list's style is the ListView's: no horizontal scroll, vertical `Auto`, no tab stop, `Once`, the ListView's template | `gridview.json` | `The_Lists_Style_Is_The_ListViews` |
| The item's style: `GridViewItemMargin` 0, no padding, the content centred both ways (WinRT's default), the ListViewItem's font, background and tilt target | `gridviewitem.json`; the probe's `hca`, `vca` Center | `The_Items_Style_Sets_What_The_Phone_Read` |
| An item's tile centred in it | `GridViewItem__default__base`: the tile at raw (396, 62) | `An_Items_Tile_Is_Centred_In_It` |
| Three tiles a row from the left, wrapping, abutting | `GridView__six-items__base`: the grey from raw (46, 29), 864 × 576; the probe: tile 1 at 120, tile 3 at 0, 120 | `The_Tiles_Fill_A_Row_Left_To_Right_And_Wrap_Abutting` |
| It scrolls down, not across: 20 tiles, 840 in a 720 view, to 120 at its end | the probe | `The_Grid_Scrolls_Down_And_Not_Across` |
| Selected in `Single` draws nothing | the probe: `SelectedUnfocused`, the capture the rest's | `A_Single_Selection_Draws_Nothing` |
| The check mode: SelectedBorder, 2.5 of accent, on every item; no box, the content not moved | `probe__gridview-multiple-*`; the template's `GridMultiSelect` | `In_The_Check_Mode_Every_Item_Has_The_Accent_Border` |
| The border where the capture draws it, the page past it; no earmark unselected | `GridViewItem__default__MultiSelectStates.GridMultiSelect` | `The_Border_Is_Drawn_Where_The_Phone_Draws_It` |
| Selected in the check mode: the 34 × 34 accent earmark at the top right inside the border, the 17 × 14.5 white tick 1 in from the corner, in both variants | `GridViewItem__selected__MultiSelectStates.GridMultiSelect`, dark and light | `A_Selected_Item_In_The_Check_Mode_Has_The_Earmark_And_Its_Tick` |
| A tap in the check mode toggles; a press at the top left is the item's, not a box's | the probe ("a tap toggles"); no box in GridMultiSelect | `In_The_Check_Mode_A_Tap_Toggles_And_The_Top_Left_Is_No_Box` |
| A press: Pressed at once, the first frame at the centre depth alone (−24.95), on TiltContainer in an untilted root | the probe, tile 1 | `A_Press_Tilts_The_Tile_As_A_ListView_Items` |
| 6 px from the left edge the tilt settles at RotationY +8.05° | the probe, tile 1 | `A_Press_Near_The_Left_Edge_Settles_At_The_Probes_Reading` |
| With item click on, a tap clicks and selects nothing, in the check mode too | unread: #148's rule, assumed (the same base and template) | `With_Item_Click_On_A_Tap_Clicks_And_Selects_Nothing` |
| Disabled: the content at 0.4; the border, earmark and tick transparent | the template's `Disabled` | `Disabled_Dims_The_Tile_And_Hides_The_Border_And_The_Earmark` |
| A ListView in the check mode still shows its box and no earmark | the ListView's captures (#130) | `A_ListView_In_The_Check_Mode_Shows_No_Earmark` |

The sample page's tests are `tests/Cornflower.Tests/Sample/GridViewPageTests.cs`.

## The visual states

The ListViewItem's (docs/controls/ListView/PLAN.md, "The visual states"), but:

| State | Keys | Avalonia |
|---|---|---|
| `MultiSelectStates` `GridMultiSelect` | SelectedBorder at 1 (`ListViewItemSelectedBackgroundThemeBrush`, `GridViewItemMultiselectBorderThickness`) | `^[(wp81|PhoneListMultiSelect.IsOn)=True]` in the item's theme |
| `ListMultiSelect` | never, in a GridView | the box hidden, ContentBorder not moved |
| `CheckboxPressed` | never: no box | the hidden box, never laid out, has no bounds, so `PhoneListMultiSelect` takes no press on it |
| `Selected`, `SelectedUnfocused` | the tick and SelectedCheckMark at 1: SelectedEarmark (`ListViewItemSelectedBackgroundThemeBrush`), SelectedGlyph (`ListViewItemCheckThemeBrush`) | `^:selected`, in the ListViewItem's theme |
| `Disabled` | the content at `ListViewItemDisabledThemeOpacity`; SelectedBorder, SelectedEarmark and SelectedGlyph Transparent | `^:disabled`, in the ListViewItem's theme |

## Avalonia's control against the phone's

Avalonia's `ListBox` and `ListBoxItem` have every member the template needs: rung 1, the themes, with rung 2's `Tilt`
and `PhoneListMultiSelect`, the ListView's. No type is substituted.

- **The panel.** The phone's `ItemsWrapGrid`, with `MaximumRowsOrColumns` −1 and no `ItemWidth` or `ItemHeight`, gives
  every item the first item's size; Avalonia's `WrapPanel` gives each its own, and doesn't virtualise. Tiles of one size
  lay out the same. A gap, recorded in the README: a grid of mixed sizes, or of thousands of items, differs.
- **What the ListView's files gained** (for reuse, not a copy): the template's `SelectedEarmark` and `SelectedGlyph`,
  left out by #130 as only GridMultiSelect draws them, now in `SelectedCheckMark`, which `:selected` shows (inside
  SelectedBorder, which a ListView never shows), and `Disabled`'s three Transparent setters for them. `PhoneListMultiSelect` is
  unchanged: a hidden box is never laid out, so it has no bounds and its press rule (bounds of a size) never fires.
- **One raw pixel.** The item on its own is 147.92 tall; Avalonia rounds its tile one raw pixel lower than the phone
  (63, not 62), and the item one raw pixel narrower (ADR 0003), so the right border and the earmark sit one raw pixel to
  the left.

## Extensions

None.

## Dependencies

The ListView's themes and `PhoneListMultiSelect` (#130, #136), `Tilt`, `ListBoxOptions` (#132), the glyphs
`Wp81.Glyph.SelectedEarmark` and `Wp81.Glyph.CheckMark` (#63), and every token, already in `Tokens/`. Owed: ItemClick
on a GridView, unread; GridViewHeaderItem and grouping are #131's.

## Checklists

Generated with `dotnet run --project tools/winrt-api -- checklist GridView <Windows.winmd> <design folder>` and
`checklist GridViewItem`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and `themeresources.xaml`.
The rows the GridView shares with the ListView carry the ListView's actions (docs/controls/ListView/PLAN.md); the rest
are filled here.

### GridView

> Generated by `dotnet run --project tools/winrt-api -- checklist GridView <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.GridView`; Avalonia none: the generator's mechanical column. Ours is Avalonia's `ListBox` in a keyed theme (rung 1), so each row below is the ListView's, mapped as there, but where the GridView differs.

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `ListViewBase` (20)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ReorderMode` | `ListViewReorderMode` | none | deferred: with reorder |
| [-] | `ShowsScrollingPlaceholders` | `Boolean` | none | not applicable: Avalonia has no data virtualisation placeholders |
| [-] | `Footer` | `Object` | none | gap: Avalonia's ListBox has no footer that scrolls with the items; an app puts one round the list |
| [-] | `FooterTemplate` | `DataTemplate` | none | gap: as Footer |
| [-] | `FooterTransitions` | `TransitionCollection` | none | gap: no Footer (below) |
| [-] | `SemanticZoomOwner` | `SemanticZoom` | none | not applicable: SemanticZoom is a later tier's (Tier 4) |
| [-] | `IsZoomedInView` | `Boolean` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `IsActiveView` | `Boolean` | none | not applicable: SemanticZoom (Tier 4) |
| [x] | `IsItemClickEnabled` | `Boolean` | none | `ListBoxOptions.IsItemClickEnabled` (#132, [ItemClick](../ItemClick/README.md)) |
| [-] | `HeaderTemplate` | `DataTemplate` | none | gap: as Header |
| [-] | `HeaderTransitions` | `TransitionCollection` | none | gap: no Header (below) |
| [-] | `IncrementalLoadingThreshold` | `Double` | none | not applicable: incremental loading |
| [-] | `IncrementalLoadingTrigger` | `IncrementalLoadingTrigger` | none | not applicable: Avalonia has no ISupportIncrementalLoading |
| [-] | `IsSwipeEnabled` | `Boolean` | none | not applicable: Windows 8.1's cross-slide select; the phone's style sets it and nothing on the phone uses it |
| [-] | `Header` | `Object` | none | gap: Avalonia's ListBox has no header that scrolls with the items (ControlHeader is a control's fixed header, not this) |
| [-] | `DataFetchSize` | `Double` | none | not applicable: incremental loading |
| [-] | `CanReorderItems` | `Boolean` | none | deferred: reorder |
| [-] | `CanDragItems` | `Boolean` | none | deferred: with reorder |
| [x] | `SelectionMode` | `ListViewSelectionMode` | `SelectionMode` (flags) | Mapped: Single is `Single`; Multiple is `Multiple, Toggle`, the check mode (`PhoneListMultiSelect`); Extended is `Multiple`, with no check boxes, as the phone's; None has no mode (a gap) |
| [x] | `SelectedItems` (read-only) (not a dependency property) | `IList<Object>` | `SelectedItems` | Avalonia's API stands |

#### Events, from `ListViewBase` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `DragItemsStarting` | `DragItemsStartingEventHandler` | none | deferred: with reorder |
| [x] | `ItemClick` | `ItemClickEventHandler` | none | `ListBoxOptions.ItemClick` (#132) |
| [-] | `ContainerContentChanging` | `TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>` | `ContainerPrepared`, nearest | not applicable: phased rendering has no equivalent |

#### Methods, from `ListViewBase` (12)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ScrollIntoView(Object item)` | `ScrollIntoView(object)` | Avalonia's API stands |
| [x] | `void SelectAll()` | `SelectAll()` | Avalonia's API stands |
| [-] | `IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync()` | none | not applicable: incremental loading |
| [-] | `void ScrollIntoView(Object item, ScrollIntoViewAlignment alignment)` | none | gap: Avalonia brings an item into view with no alignment |
| [-] | `void SetDesiredContainerUpdateDuration(TimeSpan duration)` | none | not applicable: phased rendering |
| [-] | `void InitializeViewChange()` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void CompleteViewChange()` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void MakeVisible(SemanticZoomLocation item)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |

#### Properties, from `Selector` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectedValuePath` | `String` | `SelectedValueBinding` | Avalonia's API stands: a binding, not a path |
| [x] | `SelectedValue` | `Object` | `SelectedValue` | Avalonia's API stands |
| [x] | `SelectedItem` | `Object` | `SelectedItem` | Avalonia's API stands |
| [x] | `SelectedIndex` | `Int32` | `SelectedIndex` | Avalonia's API stands |
| [-] | `IsSynchronizedWithCurrentItem` | `Boolean?` | none | not applicable: Avalonia has no collection view currency |

#### Events, from `Selector` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectionChanged` | `SelectionChangedEventHandler` | `SelectionChanged` | Avalonia's API stands |

#### Methods, from `Selector` (0)

_none_

#### Properties, from `ItemsControl` (14)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ItemsSource` | `Object` | `ItemsSource` | Avalonia's API stands |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsPanel` | Themed: a `WrapPanel`, Orientation Horizontal, for the phone's `ItemsWrapGrid` |
| [x] | `ItemTemplateSelector` | `DataTemplateSelector` | `ItemTemplate` (an `IDataTemplate` chooses) | Avalonia's API stands |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemTemplate` | Avalonia's API stands |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | gap: AddDelete and Reorder theme transitions; Avalonia has no implicit item animations (ARCHITECTURE, Theme animations) |
| [x] | `ItemContainerStyleSelector` | `StyleSelector` | `ItemContainerTheme` | Avalonia's API stands (one theme, no selector) |
| [x] | `ItemContainerStyle` | `Style` | `ItemContainerTheme` | Avalonia's API stands |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | deferred: grouping, with ListViewHeaderItem and GroupItem (below) |
| [x] | `DisplayMemberPath` | `String` | `DisplayMemberBinding` | Avalonia's API stands |
| [-] | `GroupStyle` (read-only) (not a dependency property) | `IObservableVector<GroupStyle>` | none | deferred: grouping |
| [-] | `IsGrouping` (read-only) | `Boolean` | none | deferred: grouping |
| [x] | `ItemContainerGenerator` (read-only) (not a dependency property) | `ItemContainerGenerator` | `ItemContainerGenerator` | Avalonia's API stands |
| [x] | `Items` (read-only) (not a dependency property) | `ItemCollection` | `Items` | Avalonia's API stands |
| [x] | `ItemsPanelRoot` (read-only) (not a dependency property) | `Panel` | `ItemsPanelRoot` | Avalonia's API stands |

#### Events, from `ItemsControl` (0)

_none_

#### Methods, from `ItemsControl` (14)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object ItemFromContainer(DependencyObject container)` | `ItemFromContainer` | Avalonia's API stands |
| [x] | `DependencyObject ContainerFromItem(Object item)` | `ContainerFromItem` | Avalonia's API stands |
| [x] | `Int32 IndexFromContainer(DependencyObject container)` | `IndexFromContainer` | Avalonia's API stands |
| [x] | `DependencyObject ContainerFromIndex(Int32 index)` | `ContainerFromIndex` | Avalonia's API stands |
| [x] | `Boolean IsItemItsOwnContainerOverride(Object item)` (protected, overridable) | `NeedsContainerOverride` | Avalonia's API stands |
| [x] | `DependencyObject GetContainerForItemOverride()` (protected, overridable) | `CreateContainerForItemOverride` | Avalonia's API stands |
| [x] | `void ClearContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `ClearContainerForItemOverride` | Avalonia's API stands |
| [x] | `void PrepareContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `PrepareContainerForItemOverride` | Avalonia's API stands |
| [x] | `void OnItemsChanged(Object e)` (protected, overridable) | `Items.CollectionChanged` | Avalonia's API stands |
| [x] | `void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [-] | `void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)` (protected, overridable) | none | deferred: grouping |

#### Default style: setters (14)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsTabStop` | `False` | `IsTabStop` | Themed: False |
| [x] | `TabNavigation` | `Once` | `KeyboardNavigation.TabNavigation` | Themed: Once |
| [x] | `HorizontalContentAlignment` | `Stretch` | none on `ListBox` | Themed: the ItemsPresenter stretches, as the phone binds it |
| [x] | `VerticalContentAlignment` | `Top` | none on `ListBox` | Themed: the ItemsPresenter is at the top, as the phone binds it |
| [x] | `ScrollViewer.HorizontalScrollBarVisibility` | `Disabled` | `ScrollViewer.HorizontalScrollBarVisibility (attached)` | Themed: Disabled, bound to the ScrollViewer |
| [x] | `ScrollViewer.VerticalScrollBarVisibility` | `Auto` | `ScrollViewer.VerticalScrollBarVisibility (attached)` | Themed: Auto, bound to the ScrollViewer |
| [-] | `ScrollViewer.HorizontalScrollMode` | `Disabled` | none | not applicable: Avalonia's ScrollViewer has no scroll modes (ScrollViewer README) |
| [-] | `ScrollViewer.VerticalScrollMode` | `Auto` | none | not applicable: as HorizontalScrollMode |
| [-] | `ScrollViewer.ZoomMode` | `Disabled` | none | not applicable: no zoom |
| [x] | `ScrollViewer.IsDeferredScrollingEnabled` | `False` | `ScrollViewer.IsDeferredScrollingEnabled (attached)` | Themed: False, bound to the ScrollViewer |
| [x] | `ScrollViewer.BringIntoViewOnFocusChange` | `True` | `ScrollViewer.BringIntoViewOnFocusChange (attached)` | Themed: True, bound to the ScrollViewer |
| [-] | `IsSwipeEnabled` | `True` | none | not applicable: Windows 8.1's cross-slide select; the phone's style sets it and nothing on the phone uses it |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | gap: AddDelete and Reorder theme transitions; Avalonia has no implicit item animations (ARCHITECTURE, Theme animations) |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsPanel` | Themed: a `WrapPanel`, Orientation Horizontal, for the phone's `ItemsWrapGrid` (`MaximumRowsOrColumns` -1, no ItemWidth or ItemHeight) |

#### Template parts (1)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ScrollViewer` | `ScrollViewer` | `PART_ScrollViewer` | Themed: a plain ScrollViewer, the phone's with its indicators, values bound |

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (0)

_none_



### GridViewItem

> Generated by `dotnet run --project tools/winrt-api -- checklist GridViewItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.GridViewItem`; Avalonia none: the generator's mechanical column. Ours is Avalonia's `ListBoxItem` in a keyed theme (rung 1), so each row below is the ListView's, mapped as there, but where the GridView differs.

#### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `TemplateSettings` (read-only) (not a dependency property) | `ListViewItemTemplateSettings` | none | not applicable: its one value, DragItemsCount, is reorder's |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `SelectorItem` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsSelected` | `Boolean` | `IsSelected` | Avalonia's API stands |

#### Events, from `SelectorItem` (0)

_none_

#### Methods, from `SelectorItem` (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | `Presenter.Child` | Avalonia's API stands |
| [-] | `ContentTransitions` | `TransitionCollection` | none | not applicable: Avalonia has no ContentTransitions (ARCHITECTURE) |
| [x] | `ContentTemplateSelector` | `DataTemplateSelector` | `ContentTemplate` (an `IDataTemplate` chooses) | Avalonia's API stands |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentTemplate` | Avalonia's API stands |
| [x] | `Content` | `Object` | `Content` | Avalonia's API stands |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |

#### Default style: setters (6)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `FontFamily` | Themed: ContentControlThemeFontFamily |
| [x] | `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `FontSize` | Themed: ControlContentThemeFontSize, 12 |
| [x] | `Background` | `Transparent` | `Background` | Themed: Transparent |
| [x] | `TabNavigation` | `Local` | `KeyboardNavigation.TabNavigation` | Themed: Local |
| [-] | `IsHoldingEnabled` | `False` | `IsHoldingEnabled` | deliberate difference: not set, so a long press still opens an item's ContextFlyout, which Avalonia raises from the hold |
| [x] | `Margin` | `{ThemeResource GridViewItemMargin}` | `Margin` | Themed: GridViewItemMargin, 0: the items abut |

#### Template parts (20)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `OuterContainer` | `Border` | `Border#OuterContainer` | Themed: the root, untilted |
| [-] | `ContentScaleTransform` | `ScaleTransform` | none | deferred: Reorderable's scale, with reorder |
| [x] | `ReorderHintContent` | `Grid` | `Grid#ReorderHintContent` | Themed: transparent, takes the hits |
| [x] | `CheckboxTiltContainer` | `Border` | `Border#CheckboxTiltContainer` | Themed (the ListViewItem's): hidden, as GridMultiSelect has no box; never laid out, it has no bounds, so `PhoneListMultiSelect` takes no press on it |
| [x] | `CheckboxOuterContainer` | `Border` | `Border#CheckboxOuterContainer` | Themed (the ListViewItem's), inside the hidden CheckboxTiltContainer |
| [x] | `CheckboxContainer` | `Grid` | `Grid#CheckboxContainer` | Themed (the ListViewItem's), inside the hidden CheckboxTiltContainer |
| [x] | `CheckboxContainerTranslateTransform` | `TranslateTransform` | CheckboxContainer's `RenderTransform` | Themed (the ListViewItem's), inside the hidden CheckboxTiltContainer |
| [x] | `NormalRectangle` | `Rectangle` | `Rectangle#NormalRectangle` | Themed (the ListViewItem's), inside the hidden CheckboxTiltContainer |
| [x] | `CheckGlyph` | `Path` | `Path#CheckGlyph` | Themed (the ListViewItem's), inside the hidden CheckboxTiltContainer |
| [x] | `ContentContainer` | `Border` | `Border#ContentContainer` | Themed |
| [x] | `TiltContainer` | `Border` | `Border#TiltContainer` | Themed: `Tilt.IsTarget` |
| [x] | `ContentBorder` | `Border` | `Border#ContentBorder` | Themed |
| [x] | `ContentBorderTranslateTransform` | `TranslateTransform` | ContentBorder's `RenderTransform` | Themed: none in the check mode, as GridMultiSelect doesn't move the content |
| [x] | `contentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Themed: Avalonia's name |
| [-] | `PlaceholderTextBlock` | `TextBlock` | none | not applicable: DataPlaceholder; Avalonia has no data virtualisation |
| [-] | `PlaceholderRect` | `Rectangle` | none | not applicable: as PlaceholderTextBlock |
| [x] | `SelectedBorder` | `Border` | `Border#SelectedBorder` | Themed: at 1 in GridMultiSelect, on every item: the 2.5 accent border |
| [x] | `SelectedCheckMark` | `Grid` | `Grid#SelectedCheckMark` | Themed: 34 square at the top right inside the border; at 1 when selected |
| [x] | `SelectedEarmark` | `Path` | `Path#SelectedEarmark` | Themed: `Wp81.Glyph.SelectedEarmark`, stretched to 34 square, in the accent |
| [x] | `SelectedGlyph` | `Path` | `Path#SelectedGlyph` | Themed: `Wp81.Glyph.CheckMark` stretched to 17 × 14.5, 1 in from the corner, in `ListViewItemCheckThemeBrush` |

#### Visual states (20)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | Themed |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on TiltContainer` | `^:pressed` | Themed: the tilt on TiltContainer |
| [-] | `CommonStates` | `CheckboxPressed` | `PointerDownThemeAnimation on CheckboxTiltContainer`<br>`NormalRectangle.Fill = {ThemeResource CheckBoxPressedBackgroundThemeBrush}`<br>`CheckGlyph.Fill = {ThemeResource CheckBoxPressedForegroundThemeBrush}` | `^[(wp81|PhoneListMultiSelect.IsBoxPressed)=True]` | not applicable: a GridView item has no box (GridMultiSelect), so a press anywhere is Pressed |
| [x] | `CommonStates` | `Disabled` | `contentPresenter.Opacity: DoubleAnimation Duration=0 To={ThemeResource ListViewItemDisabledThemeOpacity}`<br>`NormalRectangle.Stroke = {ThemeResource CheckBoxDisabledBorderThemeBrush}`<br>`CheckGlyph.Fill = {ThemeResource CheckBoxDisabledForegroundThemeBrush}`<br>`SelectedBorder.BorderBrush = Transparent`<br>`SelectedEarmark.Fill = Transparent`<br>`SelectedGlyph.Fill = Transparent` | `^:disabled` | Themed: the content at 0.4, and SelectedBorder, the earmark and the tick Transparent |
| [x] | `SelectionStates` | `Unselected` | nothing | the theme's plain setters | Themed |
| [x] | `SelectionStates` | `Selected` | `CheckGlyph.Opacity: DoubleAnimation Duration=0 To=1`<br>`SelectedCheckMark.Opacity: DoubleAnimation Duration=0 To=1` | `^:selected` | Themed: the tick (hidden) and SelectedCheckMark, the earmark, drawn only in GridMultiSelect |
| [x] | `SelectionStates` | `SelectedUnfocused` | `CheckGlyph.Opacity: DoubleAnimation Duration=0 To=1`<br>`SelectedCheckMark.Opacity: DoubleAnimation Duration=0 To=1` | `^:selected` | Themed: the same as Selected |
| [x] | `DataVirtualizationStates` | `DataAvailable` | nothing | the theme's plain setters | Themed: always |
| [-] | `DataVirtualizationStates` | `DataPlaceholder` | `PlaceholderTextBlock.Visibility = Visible`<br>`PlaceholderRect.Visibility = Visible` | none | not applicable: no data virtualisation |
| [x] | `MultiSelectStates` | `NoMultiSelect` | nothing | the theme's plain setters | Themed |
| [-] | `MultiSelectStates` | `ListMultiSelect` | `CheckboxContainerTranslateTransform.X: DoubleAnimation To=0 Duration=0`<br>`ContentBorderTranslateTransform.X: DoubleAnimation To={ThemeResource ListViewItemContentTranslateX} Duration=0` | `^[(wp81|PhoneListMultiSelect.IsOn)=True]` | not applicable: a GridView's check mode is GridMultiSelect; the content stays and the box stays hidden |
| [x] | `MultiSelectStates` | `GridMultiSelect` | `SelectedBorder.Opacity: DoubleAnimation Duration=0 To=1` | `^[(wp81|PhoneListMultiSelect.IsOn)=True]` | Themed: PhoneListMultiSelect.IsOn, set on the list by the ListView's theme and inherited, shows SelectedBorder |
| [-] | `ReorderModeStates` | `ReorderEnabled` | `DropTargetItemThemeAnimation on OuterContainer` | none | deferred: reorder |
| [-] | `ReorderModeStates` | `Reorderable` | `ContentScaleTransform.ScaleX: DoubleAnimationUsingKeyFrames 00:00:00.075 → 1.05, 00:00:00.2 → 1.0`<br>`ContentScaleTransform.ScaleY: DoubleAnimationUsingKeyFrames 00:00:00.075 → 1.05, 00:00:00.2 → 1.0` | none | deferred: reorder |
| [x] | `ReorderModeStates` | `ReorderDisabled` | nothing | the theme's plain setters | Themed: always |
| [x] | `ReorderHintStates` | `NoReorderHint` | nothing | the theme's plain setters | Themed: always |
| [-] | `ReorderHintStates` | `BottomReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset={ThemeResource ListViewItemReorderHintThemeOffset} Direction=Bottom)` | none | deferred: reorder |
| [-] | `ReorderHintStates` | `RightReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset={ThemeResource ListViewItemReorderHintThemeOffset} Direction=Right)` | none | deferred: reorder |
| [-] | `ReorderHintStates` | `TopReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset=0 Direction=Top)` | none | deferred: reorder |
| [-] | `ReorderHintStates` | `LeftReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset=0 Direction=Left)` | none | deferred: reorder |

#### Transitions (5)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on TiltContainer` | Themed: the tilt's release, the Button's (a 182.6 ms hold, then 167.0 ms), as #113 read it |
| [-] | `MultiSelectStates` | `ListMultiSelect` → `NoMultiSelect` | `0:0:0.15` | nothing | not applicable: no ListMultiSelect in a GridView |
| [-] | `MultiSelectStates` | `NoMultiSelect` → `ListMultiSelect` | `0:0:0.15` | nothing | not applicable: no ListMultiSelect in a GridView |
| [-] | `ReorderModeStates` | `ReorderEnabled` → `ReorderDisabled` | `00:00:00.2` | nothing | deferred: reorder |
| [-] | `ReorderHintStates` | `*` → `NoReorderHint` | `0:0:0.2` | nothing | deferred: reorder |

#### Resources (20)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | Used, by DynamicResource |
| [x] | `ControlContentThemeFontSize` | `12` | same | yes | Used, by DynamicResource |
| [x] | `GridViewItemMargin` | `0` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | Used, by DynamicResource |
| [x] | `ListViewItemDisabledThemeOpacity` | `0.4` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | Used, by DynamicResource |
| [x] | `CheckBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | Used, by DynamicResource |
| [x] | `ListViewItemContentTranslateX` | `40.5` | same | yes | Used by the ListViewItem's template; never drawn in a GridView |
| [-] | `ListViewItemReorderHintThemeOffset` | `32` | same | yes | deferred: reorder |
| [x] | `ListViewItemMultiselectCheckBoxMargin` | `0,9.5,0,0` | same | yes | Used by the ListViewItem's template; never drawn in a GridView |
| [x] | `ListViewItemContentOffsetX` | `-40.5` | same | yes | Used by the ListViewItem's template; never drawn in a GridView |
| [x] | `CheckBoxBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | Used, by DynamicResource |
| [x] | `CheckBoxBorderThemeThickness` | `2.5` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | Used, by DynamicResource |
| [-] | `GridViewItemPlaceholderBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FF1F1F1F` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FFDDDDDD` | yes | not applicable: DataPlaceholder; Avalonia has no data virtualisation |
| [x] | `ListViewItemSelectedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Used, by DynamicResource |
| [x] | `GridViewItemMultiselectBorderThickness` | `2.5` | same | yes | Used, by DynamicResource |
| [x] | `ListViewItemCheckThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | Used, by DynamicResource: the tick |

