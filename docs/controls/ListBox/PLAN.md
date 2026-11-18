# ListBox and ListBoxItem: plan

[#212](https://github.com/wdcossey/Cornflower/issues/212). The phone's ListBox, the list kept for 8.0's apps, and its
ListBoxItem: keyed `ControlTheme`s on Avalonia's `ListBox` and `ListBoxItem`, in
`src/Cornflower.Themes.Wp81/Controls/ListBox.axaml`.

**Sources.** The SDK's `generic.xaml` and `themeresources.xaml` (the 8.1 Update 1 runtime's `listbox.json` and
`listboxitem.json` match them), read as a specification (ADR 0002); the reference app's captures and dumps of run
`20261118-2210-dev11` (#211), in [`ref/wp81/ListBox/`](../../../ref/wp81/ListBox/README.md), and the numbers in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "ListBox and ListBoxItem".

## The key

Avalonia's `ListBox` already wears the 8.1 ListView's look by type (#130; ARCHITECTURE, "under another name"), as an
8.1 app's list is a ListView. So the phone's own ListBox is keyed, as `Wp81ContentControlTheme` and
`Wp81MenuFlyoutItemTheme` are: **`Wp81ListBoxTheme`** for the list and **`Wp81ListBoxItemTheme`** for the item. The list's
theme sets `ItemContainerTheme` to the item's, so an app names only the list:

```xml
<ListBox Theme="{DynamicResource Wp81ListBoxTheme}" />
```

An item on its own takes `Theme="{DynamicResource Wp81ListBoxItemTheme}"`. The phone's ListBox has neither the check mode
nor `IsItemClickEnabled` (both are `ListViewBase`'s): the item has no check box and doesn't read the mode, and the list
holds `PhoneListMultiSelect.IsOn` off, so a ListBox inside a ListView item in the check mode isn't in it. `ListBoxOptions`
is a behaviour on any ListBox, so an app could still turn item click on; the README says the phone's ListBox has none.

## Behaviours

Each with its evidence, and its test in `tests/Cornflower.Tests/Controls/ListBoxTests.cs`.

| Behaviour | Evidence | Test |
|---|---|---|
| The keys: the list's theme gives each item, its own ones too, the item's; a ListBox with no Theme stays the ListView | ARCHITECTURE's "under another name" row; #130 | `The_Keyed_List_Gives_Its_Items_The_Keyed_Item_Theme_And_A_Plain_ListBox_Stays_The_ListView` |
| The list's setters: foreground, background, border, the ScrollViewer's attached values, tab stop and navigation, font (over a parent's), `VirtualizingStackPanel`, no focus adorner | the style; the dump | `The_Lists_Style_Sets_What_The_Phone_Read` |
| The list's font is its own, over a parent's | the style | `The_List_Sets_Its_Own_Font_Over_Its_Parents` |
| The phone's ScrollViewer, its values bound from the list's, `Padding` on the items | the template | `Its_ScrollViewer_Is_The_Phones_With_The_Lists_Values` |
| The pale box, drawn inside the transparent 1.5 border (4 raw pixels of page), the selection in the accent | the capture `dark__ListBox__three-items__base` and light | `The_List_Draws_The_Pale_Box_Inside_Its_Border_And_The_Selection_In_The_Accent` |
| The bounds: the items at 1.6667 inside the border, 32.0833 tall; the content at 6.5, 8, 16.25 tall, its own width | the dump | `The_Bounds_Are_The_Phones_Within_Avalonias_Rounding` |
| The item's setters and parts: Transparent, `Local`, 6.5,8, Left, Center (WinRT's default), no focus adorner, unclipped, `PressedBackground` at 0 | the style; the dump's `vContentAlign` Center | `The_Items_Style_Sets_What_The_Phone_Read` |
| An item's content centred down its height | the dump of the item on its own, 87.92 tall, the text at 37.08 | `An_Items_Content_Is_Centred_Down_Its_Height` |
| Selected: InnerGrid in the accent, the text white | the template; `SelectionStates.Selected` | `Selected_Fills_The_Item_With_The_Accent_And_Turns_The_Text_White` |
| A tap selects, at the item's edge too | LayoutRoot takes the hits (the template's Background) | `A_Tap_Selects_The_Item` |
| SelectedPointerOver takes its own key | the template | `Selected_Under_The_Mouse_Takes_The_SelectedPointerOver_Key` |
| Pressed: `PressedBackground` at 1, the text black, the Button's tilt on InnerGrid in an untilted root | the template; `CommonStates.Pressed` | `A_Press_Shows_The_Pressed_Brush_And_Tilts_InnerGrid` |
| A press on a selected item keeps the selection's text | decided (below) | `A_Press_On_A_Selected_Item_Keeps_The_Selections_Text` |
| Disabled: LayoutRoot's background cleared, the text dimmed; SelectedDisabled's two brushes | the template; the captures | `Disabled_Dims_The_Text_And_Clears_The_Background` |
| A disabled list: no background, the border in ListBoxDisabledForegroundThemeBrush | the template; `dark__ListBox__three-items__CommonStates.Disabled` | `A_Disabled_List_Clears_Its_Background_And_Draws_Its_Border` |
| No check mode, inside a ListView in the check mode too | the phone's ListBox has none | `It_Has_No_Check_Mode_Even_Inside_A_ListView_In_The_Check_Mode` |
| Focus and the pointer over draw nothing | no FocusStates; PointerOver empty | `Keyboard_Focus_And_The_Pointer_Over_Draw_Nothing` |
| The colours follow the variant | DynamicResource | `The_Colours_Follow_A_Change_Of_Variant` |

The sample page's tests are `tests/Cornflower.Tests/Sample/ListBoxPageTests.cs`.

## The visual states

| State | Keys | Avalonia |
|---|---|---|
| ListBox `Disabled` | LayoutRoot Background Transparent, BorderBrush `ListBoxDisabledForegroundThemeBrush` | `^:disabled` |
| Item `Pressed` | `PointerDownThemeAnimation` on InnerGrid; PressedBackground (`ListBoxItemPressedBackgroundThemeBrush`) at 1; text `ListBoxItemPressedForegroundThemeBrush` | `^:pressed`; the tilt by `Tilt` (`IsTarget` InnerGrid) |
| Item `Pressed` to `Normal` | `PointerUpThemeAnimation` on InnerGrid | `Tilt`'s release: the Button's 182.6 ms hold and 167.0 ms return |
| Item `Disabled` | LayoutRoot Background Transparent; text `ListBoxItemDisabledForegroundThemeBrush` | `^:disabled` |
| `Selected`, `SelectedUnfocused`, `SelectedPressed` | InnerGrid `ListBoxItemSelectedBackgroundThemeBrush`; text `ListBoxItemSelectedForegroundThemeBrush` | `^:selected` |
| `SelectedPointerOver` | InnerGrid `ListBoxItemSelectedPointerOverBackgroundThemeBrush` | `^:selected:pointerover` |
| `SelectedDisabled` | InnerGrid `ListBoxItemSelectedDisabledBackgroundThemeBrush`; text `ListBoxItemSelectedDisabledForegroundThemeBrush` | `^:selected:disabled` |
| `Normal`, `PointerOver`, `Unselected` | nothing | no style |

## Avalonia's control against the phone's

Avalonia's `ListBox` and `ListBoxItem` have every member the template needs, so rung 1, the themes, with rung 2's
`Tilt` for the press. No type is substituted.

- **Selected outside a list.** The phone draws an item with `IsSelected` outside a list as an unselected one; Avalonia's
  sets `:selected`, so ours draws the selection. A gap: an item on its own is rare.
- **Two groups on one property.** Pressed and Selected, and Disabled and SelectedDisabled, both set the text's colour;
  the phone wasn't read for which wins. Ours: the selection's, its styles last.
- **A disabled list.** The reference app forced `Disabled` on the list alone, so its captured items stayed enabled. A
  list with `IsEnabled` false disables its items, on the phone as here, so ours draws them `Disabled` and
  `SelectedDisabled`: the comparison's disabled list differs there, by design.
- **The background inside the border.** WinRT's Border draws its background inside the border; Avalonia's, by
  default, to the border's centre line. LayoutRoot sets `BackgroundSizing` InnerBorderEdge, as TextBox's does.

## Extensions

None.

## Dependencies

`Tilt` (Button), the phone's ScrollViewer and its indicators, `PhoneHeaderLine`, and every token, already in `Tokens/`.
No capture is owed but a press on a selected item, and a press by touch (the reference app forced its states).

## Checklists

### ListBox

> Generated by `dotnet run --project tools/winrt-api -- checklist ListBox <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ListBox`; Avalonia `Avalonia.Controls.ListBox`.

#### Properties (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectionMode` | `SelectionMode` | `ListBox.SelectionMode` | Avalonia's API stands: `Single`, `Multiple` (WinRT's Multiple and Extended both select several; the phone's ListBox has no check boxes), `Toggle`, `AlwaysSelected` |
| [x] | `SelectedItems` (read-only) (not a dependency property) | `IList<Object>` | `SelectedItems` | Avalonia's API stands |

#### Events (0)

_none_

#### Methods (2)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ScrollIntoView(Object item)` | `ScrollIntoView(object)` | Avalonia's API stands |
| [x] | `void SelectAll()` | `SelectAll()` | Avalonia's API stands |

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
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsControl.ItemsPanel` | Themed: a `VirtualizingStackPanel` |
| [x] | `ItemTemplateSelector` | `DataTemplateSelector` | `ItemTemplate` (an `IDataTemplate` chooses) | Avalonia's API stands |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemTemplate` | Avalonia's API stands |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | gap: AddDelete and Reorder theme transitions; Avalonia has no implicit item animations (ARCHITECTURE, Theme animations) |
| [x] | `ItemContainerStyleSelector` | `StyleSelector` | `ItemContainerTheme` | Avalonia's API stands (one theme, no selector) |
| [x] | `ItemContainerStyle` | `Style` | `ItemContainerTheme` | Avalonia's API stands |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | deferred: grouping (Avalonia 12.1.4 has none) |
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

#### Default style: setters (16)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource ListBoxForegroundThemeBrush}` | `TemplatedControl.Foreground` | Themed: ListBoxForegroundThemeBrush |
| [x] | `Background` | `{ThemeResource ListBoxBackgroundThemeBrush}` | `TemplatedControl.Background` | Themed: ListBoxBackgroundThemeBrush, drawn inside the border (`BackgroundSizing` InnerBorderEdge, as WinRT's Border) |
| [x] | `BorderBrush` | `{ThemeResource ListBoxBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Themed: ListBoxBorderThemeBrush |
| [x] | `BorderThickness` | `{ThemeResource ListBoxBorderThemeThickness}` | `TemplatedControl.BorderThickness` | Themed: ListBoxBorderThemeThickness |
| [x] | `ScrollViewer.HorizontalScrollBarVisibility` | `Disabled` | `ScrollViewer.HorizontalScrollBarVisibility (attached)` | Avalonia's ListBox's default, Disabled, bound to the ScrollViewer |
| [x] | `ScrollViewer.VerticalScrollBarVisibility` | `Auto` | `ScrollViewer.VerticalScrollBarVisibility (attached)` | Themed: Auto, bound to the ScrollViewer |
| [-] | `ScrollViewer.HorizontalScrollMode` | `Disabled` | none | not applicable: Avalonia's ScrollViewer has no scroll modes (ScrollViewer README); horizontal scrolling is off by the bar's Disabled |
| [-] | `ScrollViewer.VerticalScrollMode` | `Auto` | none | not applicable: as HorizontalScrollMode |
| [-] | `ScrollViewer.ZoomMode` | `Disabled` | none | not applicable: no zoom |
| [x] | `ScrollViewer.IsDeferredScrollingEnabled` | `False` | `ScrollViewer.IsDeferredScrollingEnabled (attached)` | Themed: False, bound to the ScrollViewer |
| [x] | `ScrollViewer.BringIntoViewOnFocusChange` | `True` | `ScrollViewer.BringIntoViewOnFocusChange (attached)` | Themed: True, bound to the ScrollViewer |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | Themed: False |
| [x] | `TabNavigation` | `Once` | none | Avalonia's ListBox's default, `KeyboardNavigation.TabNavigation` Once |
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `TemplatedControl.FontFamily` | Themed: ContentControlThemeFontFamily |
| [x] | `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `TemplatedControl.FontSize` | Themed: ControlContentThemeFontSize, 12 |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsControl.ItemsPanel` | Themed: a `VirtualizingStackPanel` |

#### Template parts (2)

Avalonia's control looks for: `PART_ScrollViewer`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `Border` | none | Themed: `Border#LayoutRoot` |
| [x] | `ScrollViewer` | `ScrollViewer` | `PART_ScrollViewer` | Themed: `PART_ScrollViewer`, a plain ScrollViewer, the phone's with its indicators, values bound |

#### Visual states (2)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | Themed |
| [x] | `CommonStates` | `Disabled` | `LayoutRoot.Background = Transparent`<br>`LayoutRoot.BorderBrush = {ThemeResource ListBoxDisabledForegroundThemeBrush}` | `^:disabled` | Themed: `^:disabled /template/ Border#LayoutRoot` |

#### Transitions (0)

_none_

#### Resources (7)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ListBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseBlackColor} → #FF000000` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumHighColor} → #CCFFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumHighColor} → #BF000000` | yes | Used, by DynamicResource |
| [x] | `ListBoxBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxBorderThemeThickness` | `1.5` | same | yes | Used, by DynamicResource |
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | Used, by DynamicResource |
| [x] | `ControlContentThemeFontSize` | `12` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | Used, by DynamicResource |


### ListBoxItem

> Generated by `dotnet run --project tools/winrt-api -- checklist ListBoxItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ListBoxItem`; Avalonia `Avalonia.Controls.ListBoxItem`.

#### Properties (0)

_none_

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

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | Themed: Transparent |
| [x] | `TabNavigation` | `Local` | none | Themed: `KeyboardNavigation.TabNavigation` Local |
| [x] | `Padding` | `6.5,8` | `TemplatedControl.Padding` | Themed: 6.5,8 |
| [x] | `HorizontalContentAlignment` | `Left` | `ContentControl.HorizontalContentAlignment` | Themed: Left (and VerticalContentAlignment Center, WinRT's default, which Avalonia's isn't) |

#### Template parts (4)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `Border` | none | Themed: `Border#LayoutRoot`, untilted, takes the hits |
| [x] | `InnerGrid` | `Grid` | none | Themed: `Grid#InnerGrid`, `Tilt.IsTarget` |
| [x] | `PressedBackground` | `Rectangle` | none | Themed: `Rectangle#PressedBackground` |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Themed: `PART_ContentPresenter`, with the 1.33 em line (`PhoneHeaderLine`) |

#### Visual states (10)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | Themed |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | Themed: no style, as the phone changes nothing |
| [x] | `CommonStates` | `Disabled` | `LayoutRoot.Background = Transparent`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemDisabledForegroundThemeBrush}` | `^:disabled` | Themed: `^:disabled` |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on InnerGrid`<br>`PressedBackground.Opacity: DoubleAnimation To=1 Duration=0`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemPressedForegroundThemeBrush}` | `^:pressed` | Themed: `^:pressed` (the brush and text); the tilt by `Tilt` on InnerGrid |
| [x] | `SelectionStates` | `Unselected` | nothing | the theme's plain setters | Themed |
| [x] | `SelectionStates` | `Selected` | `InnerGrid.Background = {ThemeResource ListBoxItemSelectedBackgroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemSelectedForegroundThemeBrush}` | `^:selected` | Themed: `^:selected`, after CommonStates' styles |
| [x] | `SelectionStates` | `SelectedUnfocused` | `InnerGrid.Background = {ThemeResource ListBoxItemSelectedBackgroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemSelectedForegroundThemeBrush}` | `^:selected` | Themed: `^:selected` (Avalonia doesn't tell them apart; the same values) |
| [x] | `SelectionStates` | `SelectedDisabled` | `InnerGrid.Background = {ThemeResource ListBoxItemSelectedDisabledBackgroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemSelectedDisabledForegroundThemeBrush}` | `^:selected:disabled` | Themed: `^:selected:disabled` |
| [x] | `SelectionStates` | `SelectedPointerOver` | `InnerGrid.Background = {ThemeResource ListBoxItemSelectedPointerOverBackgroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemSelectedForegroundThemeBrush}` | `^:selected:pointerover` | Themed: `^:selected:pointerover`, its own background key |
| [x] | `SelectionStates` | `SelectedPressed` | `InnerGrid.Background = {ThemeResource ListBoxItemSelectedBackgroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ListBoxItemSelectedForegroundThemeBrush}` | `^:selected:pressed` | Themed: `^:selected` with `^:pressed` (the same values as Selected; the press's brush over it) |

#### Transitions (1)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on InnerGrid` | Themed: `Tilt`'s release, the Button's hold and return |

#### Resources (8)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ListBoxItemDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80000000` | yes | Used, by DynamicResource |
| [x] | `ListBoxItemPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseBlackColor} → #FF000000` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxItemSelectedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxItemSelectedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseWhiteColor} → #FFFFFFFF` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxItemSelectedDisabledBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | Used, by DynamicResource |
| [x] | `ListBoxItemSelectedDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | Used, by DynamicResource |
| [x] | `ListBoxItemSelectedPointerOverBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Used, by DynamicResource |
| [x] | `ListBoxItemPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumHighColor} → #CCFFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumHighColor} → #BF000000` | yes | Used, by DynamicResource |

