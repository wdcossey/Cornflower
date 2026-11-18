# ComboBox and ComboBoxItem: plan

Issue [#155](https://github.com/wdcossey/Cornflower/issues/155). The 8.1 ComboBox on Avalonia's own `ComboBox` and
`ComboBoxItem`: five items or fewer open inline, the box growing in place; six or more open the full-screen
`ListPickerFlyout` (#153, #176).

## Sources

- The style and template: the 8.1 Update 1 runtime's `combobox.json` and `comboboxitem.json` (`flash-update1/xbf-json/`),
  values from its `themeresources.json`. Update 1's, not RTM's: the content's margin 0,0.8 and `MinHeight` 32.5.
- The captures (22, closed): run `20261108-1705-dev11`, `ComboBox { Header = "combo box" }`, "one", "two", "three", "one"
  selected, at rest and forced into each state, dark and light, on a 450 × 140 stage.
- Opened with 3 to 10 items ("item 1" to "item N", "item 1" selected): run `20261109-2039-dev11`, on a 450 × 400 stage,
  with the dumps of the full-screen picker's popup for 6 to 10.
- Where it flips to full screen: 5 inline and 6 full screen, on devices 10 and 11 (#25; `tools/refapp/README.md`, "Where
  ComboBox flips to full screen").
- `TemplateSettings`: `tools/refapp/README.md`, "TemplateSettings" (gap register A9, B5).
- The motion and the press: #149, run `20261116-1835-dev11` ("The popups' bands, and the inline ComboBox") and
  `20261116-1904-dev11` ("A press on a closed ComboBox").

## The ladder

| Difference | Rung | How |
|---|---|---|
| Avalonia's ComboBox opens a popup; the phone's grows in place | 2 (behaviour) and 1 (theme) | The template holds the items in the box; `PhoneComboBox` (internal, turned on by the theme) shows it open and closed; `PhoneComboBoxItemsHost` (internal) lays out the phone's `TemplateSettings` |
| Six items or more open the full-screen picker | 2 | `PhoneComboBox` opens a `ListPickerFlyout` at the box |
| `Header`, `HeaderTemplate` | 2 | `cf:ControlHeader`, as Slider, TextBox and ToggleSwitch |
| `PART_Popup` is required by Avalonia's control | 1 | an empty overlay `Popup`: its opening and closing raise Avalonia's `DropDownOpened` and `DropDownClosed` |

**No subclass** (decided on the user's behalf). The issue planned `Cornflower.Controls.ComboBox` for the full-screen
form. A behaviour on Avalonia's own `ComboBox` does both forms, as `PhoneToggleSwitch` does for ToggleSwitch: an app's
`<ComboBox>` gets the phone's ComboBox with no new type, and a plain ComboBox with six items doesn't do something else.
To reverse: add the subclass and have its theme set `wp81:PhoneComboBox.IsEnabled`; the theme above stays.

## The behaviours

| Behaviour | Evidence | Test (`ComboBoxTests`) |
|---|---|---|
| Closed: the header, the box 4.5 into its overhang (26.67, 38.33 tall), the selected item's text in it at 9.17, 29.8; colours | the closed dumps | `Closed_It_Is_Laid_Out_And_Coloured_As_The_Phones` |
| Closed, the items are lifted so the selected one shows (`DropDownOffset`: -8.5 less its index times 51.67) | "TemplateSettings" | `Closed_The_Selected_Item_Shows_In_The_Box` |
| Inline, the box holds its closed size four frames and is open on the fifth, 78 ms after `IsDropDownOpen`, the items' height (155 for 3, 258.33 for 5) | #149 | `Inline_It_Opens_In_One_Frame_78_Ms_After_The_Call` |
| Open: white, accent border, black text, the selected item in the accent | the open-3 and open-5 dumps | `Open_The_Box_Is_White_With_The_Accent_Border_And_The_Selected_Item_In_The_Accent` |
| Inline, it closes at once | #149 | `Inline_It_Closes_At_Once` |
| A tap on an item selects it and closes; `DropDownOpened`, `DropDownClosed` | WinRT; Avalonia's events | `A_Tap_On_An_Item_Selects_It_And_Closes_The_Box_With_Avalonias_Events` |
| A tap outside closes it and reaches what it was on | not read: decided (below) | `A_Tap_Outside_Closes_The_Box_And_Reaches_What_It_Was_On` |
| Back closes it before anything else sees Back | not read: decided (below) | `Back_Closes_The_Box` |
| A press: the Button's tilt on the box, accent fill; the release opens it, the tilt gone at once | #149 | `A_Press_Tilts_The_Box_In_The_Accent_And_The_Release_Opens_It_With_The_Tilt_Gone` |
| Disabled: header, border and text in `PhoneBaseLowColor` | the Disabled dumps | `Disabled_It_Dims_The_Header_The_Border_And_The_Text` |
| A disabled item: 0.4 | `comboboxitem.json` | `A_Disabled_Item_Is_Dimmed_To_0_4` |
| Nothing selected, inline: "choose an item" in `PhoneMidBrush` at 9, 29.97, the items hidden | the InlinePlaceholder dumps | `Inline_With_Nothing_Selected_It_Shows_The_Placeholder` |
| 5 items inline, 6 and 10 full screen | #25 | `Five_Items_Open_Inline_And_Six_Full_Screen` |
| Full: the selected item at 9.17, 30, or the placeholder at 0.6 (dark) | the Full dumps; open-6 | `Full_The_Box_Shows_The_Selected_Item_Or_The_Placeholder` |
| Full: the picker titled "CHOOSE AN ITEM", the selected item in the accent; a pick sets `SelectedIndex`, the close `IsDropDownOpen` | the open-6 popup dump | `Full_It_Opens_The_List_Picker_Which_Picks_And_Closes_The_Drop_Down` |
| Full: Back closes the picker, the selection kept | #150 (the list picker's Back) | `Full_Back_Closes_The_Picker_With_The_Selection_Kept` |
| No header: the box 9.5 down | the template (the header collapsed with its margin) | `With_No_Header_The_Header_Is_Collapsed` |

## The motion: the screen, not the storyboard

The template's `Opened` storyboard eases `ItemsPresenterHost.Height` and the items' offset over 250 ms
(`ExponentialEase` EaseInOut 6), and `Closed` over 200 ms. #149 read that the layout doesn't follow: the box holds its
closed size four frames and jumps open on the fifth (78 ms of RenderingTime), and is closed on the first frame after
`IsDropDownOpen` goes false. So the screen shows no ease, and **we reproduce the screen**: open in one frame at 5 ticks
(78.125 ms on `CornflowerClock`), closed at once. A user sees the screen; the storyboard's values never reach it
(ADR 0001). To reverse: run the storyboard's ease in `PhoneComboBoxItemsHost` from the 78 ms mark.

## Decided on the user's behalf

- **No subclass**: above.
- **The motion is the screen's**: above.
- **A tap outside the open inline box closes it**, and the tap goes on to what it was on; **Back closes it** first. Not
  read (#150 read the popups; the inline box isn't one). The phone's control loses focus to a tap elsewhere; Back
  closing it matches every other open thing on the phone. To reverse: drop the window handler or the Back taker in
  `PhoneComboBox`.
- **PlaceholderText defaults to "choose an item"**, the text every capture shows with none set; and the full-screen
  picker's title is the placeholder in capitals ("CHOOSE AN ITEM" in the open-6 dump, the header being "combo box").
  Which of the two the phone takes the title from isn't told apart by the reads. To reverse: drop the theme's
  `PlaceholderText` setter or `PickerFlyoutBase.SetTitle` in `PhoneComboBox`.
- **The selected item is drawn in the accent only while open**: the closed dump draws it as the rest.
- **While the picker is open, the box shows the selected item**: the open-6 dump's box is empty then. Not followed: an
  empty box for the moment the picker covers the screen is nothing a user sees.
- **Open with nothing selected**, the items show (not the placeholder): the phone's states would leave an empty white
  box; not read.

## Extensions

None public. `cf:ControlHeader.Header` and `HeaderTemplate` are the existing extension.

## Dependencies

`ListPickerFlyout` (#153, done), `ControlHeader`, `Tilt`, `PhonePopupHost.TakeBackFirst`, `TemplateParts`,
`PhoneHeaderLine`. The device run and the real-window run passed on 2026-11-17 (the README's status line). No capture of the open full-screen
ComboBox as a picture exists (the open-6 dump has its tree); its look is the ListPickerFlyout's.

## The checklists

Generated on 2026-11-17 from the phone SDK's `Windows.winmd` and design folder, as Button's PLAN says.

### ComboBox

> Generated by `dotnet run --project tools/winrt-api -- checklist ComboBox <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ComboBox`; Avalonia `Avalonia.Controls.ComboBox`.

#### Properties (10)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `MaxDropDownHeight` | `Double` | `ComboBox.MaxDropDownHeight` | not applicable: the phone opens inline or full screen, never a sized drop-down; Avalonia's stands and does nothing |
| [x] | `IsDropDownOpen` | `Boolean` | `ComboBox.IsDropDownOpen` | Avalonia's; opens inline (shown 78 ms later) or the full-screen picker (PhoneComboBox) |
| [-] | `IsEditable` (read-only) (not a dependency property) | `Boolean` | `ComboBox.IsEditable` | the phone's is always false; Avalonia's editable box isn't themed (known gap) |
| [-] | `IsSelectionBoxHighlighted` (read-only) (not a dependency property) | `Boolean` | none | not carried: Highlighted shows while open (IsShownOpen) |
| [x] | `SelectionBoxItem` (read-only) (not a dependency property) | `Object` | `ComboBox.SelectionBoxItem` | Avalonia's; shown by the Full form |
| [x] | `SelectionBoxItemTemplate` (read-only) (not a dependency property) | `DataTemplate` | `ComboBox.SelectionBoxItemTemplate` | Avalonia's; the Full form's ContentTemplate |
| [x] | `TemplateSettings` (read-only) (not a dependency property) | `ComboBoxTemplateSettings` | none | rung 2: PhoneComboBoxItemsHost lays out DropDownClosedHeight, DropDownOpenedHeight and DropDownOffset; not exposed |
| [x] | `PlaceholderText` | `String` | `ComboBox.PlaceholderText` | Avalonia's; the theme defaults it to the phone's "choose an item" |
| [x] | `HeaderTemplate` | `DataTemplate` | none | cf:ControlHeader.HeaderTemplate (extension, as Slider's) |
| [x] | `Header` | `Object` | none | cf:ControlHeader.Header (extension, as Slider's) |

#### Events (2)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `DropDownClosed` | `EventHandler<Object>` | `ComboBox.DropDownClosed` | Avalonia's, from the empty PART_Popup |
| [x] | `DropDownOpened` | `EventHandler<Object>` | `ComboBox.DropDownOpened` | Avalonia's, from the empty PART_Popup |

#### Methods (2)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnDropDownClosed(Object e)` (protected, overridable) | none | not carried: Avalonia has the event only |
| [-] | `void OnDropDownOpened(Object e)` (protected, overridable) | none | not carried: Avalonia has the event only |

#### Properties, from `Selector` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectedValuePath` | `String` | none | Avalonia's SelectedValueBinding |
| [x] | `SelectedValue` | `Object` | `SelectingItemsControl.SelectedValue` | Avalonia's |
| [x] | `SelectedItem` | `Object` | `SelectingItemsControl.SelectedItem` | Avalonia's |
| [x] | `SelectedIndex` | `Int32` | `SelectingItemsControl.SelectedIndex` | Avalonia's; the full-screen pick sets it |
| [-] | `IsSynchronizedWithCurrentItem` | `Boolean?` | none | not applicable: Avalonia has no current-item view |

#### Events, from `Selector` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectionChanged` | `SelectionChangedEventHandler` | `SelectingItemsControl.SelectionChanged` | Avalonia's |

#### Methods, from `Selector` (0)

_none_

#### Properties, from `ItemsControl` (14)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ItemsPanelRoot` (read-only) (not a dependency property) | `Panel` | `ItemsControl.ItemsPanelRoot` | Avalonia's |
| [x] | `ItemsSource` | `Object` | `ItemsControl.ItemsSource` | Avalonia's; the picker gets the same items |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsControl.ItemsPanel` | the theme sets a StackPanel, the phone's |
| [-] | `ItemTemplateSelector` | `DataTemplateSelector` | none | not carried: Avalonia selects by data template |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemsControl.ItemTemplate` | Avalonia's; passed to the picker |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | not carried |
| [-] | `ItemContainerStyleSelector` | `StyleSelector` | none | not carried |
| [-] | `ItemContainerStyle` | `Style` | none | Avalonia uses ItemContainerTheme or styles |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | not applicable: no grouping |
| [x] | `DisplayMemberPath` | `String` | none | Avalonia's DisplayMemberBinding; passed to the picker |
| [-] | `GroupStyle` (read-only) (not a dependency property) | `IObservableVector<GroupStyle>` | none | not applicable: no grouping |
| [-] | `IsGrouping` (read-only) | `Boolean` | none | not applicable: no grouping |
| [x] | `ItemContainerGenerator` (read-only) (not a dependency property) | `ItemContainerGenerator` | `ItemsControl.ItemContainerGenerator` | Avalonia's |
| [x] | `Items` (read-only) (not a dependency property) | `ItemCollection` | `ItemsControl.Items` | Avalonia's |

#### Events, from `ItemsControl` (0)

_none_

#### Methods, from `ItemsControl` (14)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object ItemFromContainer(DependencyObject container)` | `ItemsControl.ItemFromContainer()` | Avalonia's |
| [x] | `DependencyObject ContainerFromItem(Object item)` | `ItemsControl.ContainerFromItem()` | Avalonia's |
| [x] | `Int32 IndexFromContainer(DependencyObject container)` | `ItemsControl.IndexFromContainer()` | Avalonia's |
| [x] | `DependencyObject ContainerFromIndex(Int32 index)` | `ItemsControl.ContainerFromIndex()` | Avalonia's |
| [-] | `Boolean IsItemItsOwnContainerOverride(Object item)` (protected, overridable) | none | not carried: Avalonia's own container overrides |
| [-] | `DependencyObject GetContainerForItemOverride()` (protected, overridable) | none | not carried: Avalonia's own container overrides |
| [-] | `void ClearContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `SelectingItemsControl.ClearContainerForItemOverride()` | not carried: Avalonia's own container overrides |
| [-] | `void PrepareContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `SelectingItemsControl.PrepareContainerForItemOverride()` | not carried: Avalonia's own container overrides |
| [-] | `void OnItemsChanged(Object e)` (protected, overridable) | none | not carried |
| [x] | `void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)` (protected, overridable) | none | by its key (DynamicResource) |
| [x] | `void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)` (protected, overridable) | none | by its key (DynamicResource) |
| [-] | `void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)` (protected, overridable) | none | not carried |
| [-] | `void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)` (protected, overridable) | none | not carried |
| [x] | `void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)` (protected, overridable) | none | by its key (DynamicResource) |

#### Default style: setters (8)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource ComboBoxForegroundThemeBrush}` | `TemplatedControl.Foreground` | themed |
| [x] | `Background` | `{ThemeResource ComboBoxBackgroundThemeBrush}` | `TemplatedControl.Background` | themed; the part Background is the box, the tilt target |
| [x] | `BorderBrush` | `{ThemeResource ComboBoxBorderThemeBrush}` | `TemplatedControl.BorderBrush` | themed |
| [x] | `BorderThickness` | `{ThemeResource ComboBoxBorderThemeThickness}` | `TemplatedControl.BorderThickness` | themed |
| [x] | `HorizontalContentAlignment` | `Left` | `ComboBox.HorizontalContentAlignment` | themed |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | `TemplatedControl.FontFamily` | themed |
| [x] | `FontSize` | `{ThemeResource ContentControlFontSize}` | `TemplatedControl.FontSize` | themed |
| [x] | `Margin` | `0` | `Layoutable.Margin` | themed |

#### Template parts (12)

Avalonia's control looks for: `PART_Popup`, `PART_EditableTextBox`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ComboBoxGrid` | `Grid` | none | a StackPanel (a Grid's Auto row rounds the header a pixel taller) |
| [x] | `HeaderContentPresenter` | `ContentPresenter` | none | Wp81HeaderContentPresenterTheme |
| [x] | `FlyoutButton` | `Button` | none | Panel FlyoutContent in the one Background box (the same 38.33 box; one tilt target) |
| [x] | `ContentPresenter` | `ContentPresenter` | none | PART_ContentPresenter (the item); ContentPresenter (the Full form) |
| [x] | `PlaceholderTextBlock` | `TextBlock` | none | ComboBoxPlaceholderTextBlockStyle |
| [x] | `ShortListOuterBorder` | `Border` | none | Panel ShortList; the overhang is on Background |
| [x] | `Background` | `Border` | none | themed; the part Background is the box, the tilt target |
| [x] | `UserControl` | `UserControl` | none | Panel UserControl; the states' foreground is set on the control, whose items inherit it |
| [x] | `ItemsPresenterHost` | `Canvas` | none | PhoneComboBoxItemsHost |
| [x] | `PlaceholderTextContentPresenter` | `ContentPresenter` | none | themed; PlaceholderContentPresenterStyle written out on it |
| [x] | `ItemsPresenter` | `ItemsPresenter` | none | PART_ItemsPresenter |
| [x] | `ItemsPresenterTranslateTransform` | `TranslateTransform` | none | the host lifts the items in its arrange |

#### Visual states (10)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | `PlaceholderTextBlock.Opacity: DoubleAnimation To={ThemeResource ComboBoxFlyoutListPlaceholderTextOpacity} Duration=0` | the theme's plain setters | the plain setters; the placeholder's opacity is its text style's |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | nothing, as the phone |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on Background`<br>`Background.Background = {ThemeResource ComboBoxPressedBackgroundThemeBrush}`<br>`Background.BorderBrush = {ThemeResource ComboBoxPressedBorderThemeBrush}`<br>`UserControl.Foreground = {ThemeResource ComboBoxPressedForegroundThemeBrush}` | `^:pressed` | :pressed with cf:Tilt on Background (the box); the item's tilt on its content |
| [x] | `CommonStates` | `Highlighted` | `Background.Background = {ThemeResource ComboBoxHighlightedBackgroundThemeBrush}`<br>`Background.BorderBrush = {ThemeResource ComboBoxHighlightedBorderThemeBrush}`<br>`UserControl.Foreground = {ThemeResource ComboBoxHighlightedForegroundThemeBrush}` | no rule | with Opened, while IsShownOpen |
| [x] | `CommonStates` | `Disabled` | `HeaderContentPresenter.Foreground = {ThemeResource ComboBoxDisabledForegroundThemeBrush}`<br>`ContentPresenter.Foreground = {ThemeResource ComboBoxDisabledForegroundThemeBrush}`<br>`Background.Background = {ThemeResource ComboBoxDisabledBackgroundThemeBrush}`<br>`Background.BorderBrush = {ThemeResource ComboBoxDisabledBorderThemeBrush}`<br>`FlyoutButton.BorderBrush = {ThemeResource ComboBoxDisabledBorderThemeBrush}`<br>`UserControl.Foreground = {ThemeResource ComboBoxDisabledForegroundThemeBrush}`<br>`PlaceholderTextBlock.Foreground = {ThemeResource ComboBoxDisabledForegroundThemeBrush}`<br>`PlaceholderTextBlock.Opacity: DoubleAnimation To=1 Duration=0`<br>`PlaceholderTextContentPresenter.Foreground = {ThemeResource ComboBoxDisabledForegroundThemeBrush}` | `^:disabled` | :disabled on the control; the item's own IsEnabled for its 0.4 |
| [x] | `DropDownStates` | `Opened` | `ItemsPresenterHost.Height: DoubleAnimation Duration=0:0:0.25 From={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.DropDownClosedHeight} To={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.DropDownOpenedHeight} EnableDependentAnimation=True`<br>`ItemsPresenterTranslateTransform.Y: DoubleAnimation Duration=0:0:0.25 To={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.DropDownOffset}`<br>`UserControl.IsHitTestVisible = True`<br>`Background.Background = {ThemeResource ComboBoxHighlightedBackgroundThemeBrush}`<br>`UserControl.Foreground = {ThemeResource ComboBoxHighlightedForegroundThemeBrush}` | no rule | IsShownOpen, 78 ms after IsDropDownOpen, in one frame (what the screen shows) |
| [x] | `DropDownStates` | `Closed` | `ItemsPresenterHost.Height: DoubleAnimation Duration=0:0:0.2 From={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.DropDownOpenedHeight} To={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.DropDownClosedHeight} EnableDependentAnimation=True`<br>`ItemsPresenterTranslateTransform.Y: DoubleAnimation Duration=0:0:0.2 To={Binding RelativeSource={RelativeSource TemplatedParent}, Path=TemplateSettings.DropDownOffset}` | no rule | at once (what the screen shows) |
| [x] | `PresenterStates` | `Full` | nothing | no rule | PhoneComboBox.IsFull (6 items or more) |
| [x] | `PresenterStates` | `InlineNormal` | `FlyoutButton.Visibility = Collapsed`<br>`ShortListOuterBorder.Visibility = Visible` | no rule | the default |
| [x] | `PresenterStates` | `InlinePlaceholder` | `FlyoutButton.Visibility = Collapsed`<br>`ShortListOuterBorder.Visibility = Visible`<br>`PlaceholderTextContentPresenter.Opacity: DoubleAnimation To=1 Duration=0`<br>`ItemsPresenter.Opacity: DoubleAnimation To=0 Duration=0` | no rule | [SelectedIndex=-1], closed |

#### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on Background` | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on Background`<br>`PlaceholderTextBlock.Foreground = {ThemeResource PhoneMidBrush}`<br>`PlaceholderTextBlock.Opacity = 1` | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on Background` | the tilt's return (cf:Tilt); open, the box's tilt goes at once |

#### Resources (23)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ComboBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `ComboBoxBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `ComboBoxBorderThemeThickness` | `2.5` | same | yes | by its key (DynamicResource) |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | by its key (DynamicResource) |
| [x] | `ContentControlFontSize` | `20.26` | same | yes | Avalonia's |
| [x] | `ComboBoxFlyoutListPlaceholderTextOpacity` | `0.6` | `0.45` | yes | by its key (DynamicResource) |
| [x] | `ComboBoxPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxPressedBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `ComboBoxPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `ComboBoxHighlightedBackgroundThemeBrush` | `SolidColorBrush #FFFFFFFF` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxHighlightedBorderThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxHighlightedForegroundThemeBrush` | `SolidColorBrush #FF000000` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `ComboBoxDisabledBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `PhoneMidBrush` | `SolidColorBrush {ThemeResource PhoneBaseMidColor} → #99FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMidColor} → #73000000` | yes | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `HeaderContentPresenterStyle` | `Style TargetType=ContentPresenter` | same | no | Wp81HeaderContentPresenterTheme (#111) |
| [x] | `ComboBoxItemMinHeightThemeSize` | `33.5` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxPlaceholderTextBlockStyle` | `Style TargetType=TextBlock` | same | no | Controls/TextBlock.axaml's ComboBoxPlaceholderTextBlockStyle |
| [x] | `PhoneTouchTargetOverhang` | `0,9.5` | same | yes | by its key (DynamicResource) |
| [x] | `PlaceholderContentPresenterStyle` | `Style TargetType=ContentPresenter` | same | no | not a resource here: its four setters written out on PlaceholderTextContentPresenter |
| [x] | `ComboBoxPlaceholderTextThemeMargin` | `6.5,0.8` | same | yes | by its key (DynamicResource) |


### ComboBoxItem

> Generated by `dotnet run --project tools/winrt-api -- checklist ComboBoxItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ComboBoxItem`; Avalonia `Avalonia.Controls.ComboBoxItem`.

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `SelectorItem` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsSelected` | `Boolean` | `ListBoxItem.IsSelected` | Avalonia's |

#### Events, from `SelectorItem` (0)

_none_

#### Methods, from `SelectorItem` (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ContentTransitions` | `TransitionCollection` | none | Avalonia's |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | not carried |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Avalonia's |
| [x] | `Content` | `Object` | `ContentControl.Content` | Avalonia's |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | not carried |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not carried |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | not carried |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | not carried |

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | themed; the part Background is the box, the tilt target |
| [x] | `Padding` | `6.5,8.5,0,16` | `TemplatedControl.Padding` | themed (Avalonia rounds each side to the pixel: 0.42 short an item, a gap) |
| [x] | `HorizontalContentAlignment` | `Stretch` | `ContentControl.HorizontalContentAlignment` | themed |
| [x] | `VerticalAlignment` | `Top` | `Layoutable.VerticalAlignment` | themed |

#### Template parts (2)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HighContrastBackground` | `Border` | none | kept, transparent |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | PART_ContentPresenter (the item); ContentPresenter (the Full form) |

#### Visual states (10)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | the plain setters; the placeholder's opacity is its text style's |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | nothing, as the phone |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on ContentPresenter`<br>`PointerDownThemeAnimation on HighContrastBackground` | `^:pressed` | :pressed with cf:Tilt on Background (the box); the item's tilt on its content |
| [x] | `CommonStates` | `Disabled` | `ContentPresenter.Opacity: DoubleAnimation To={ThemeResource ComboBoxItemDisabledThemeOpacity} Duration=0` | `^:disabled` | :disabled on the control; the item's own IsEnabled for its 0.4 |
| [x] | `SelectionStates` | `Unselected` | nothing | the theme's plain setters | nothing |
| [x] | `SelectionStates` | `Selected` | `ContentPresenter.Foreground = {ThemeResource PhoneHighContrastSelectedForegroundThemeBrush}`<br>`HighContrastBackground.Background = {ThemeResource PhoneHighContrastSelectedBackgroundThemeBrush}` | `^:selected` | the accent while the box shows open (closed, the phone draws it as the rest) |
| [x] | `SelectionStates` | `SelectedPressed` | `ContentPresenter.Foreground = {ThemeResource PhoneHighContrastSelectedForegroundThemeBrush}`<br>`HighContrastBackground.Background = {ThemeResource PhoneHighContrastSelectedBackgroundThemeBrush}` | `^:selected:pressed` | as Selected |
| [x] | `SelectionStates` | `SelectedPointerOver` | `ContentPresenter.Foreground = {ThemeResource PhoneHighContrastSelectedForegroundThemeBrush}`<br>`HighContrastBackground.Background = {ThemeResource PhoneHighContrastSelectedBackgroundThemeBrush}` | `^:selected:pointerover` | as Selected |
| [x] | `SelectionStates` | `SelectedUnfocused` | `ContentPresenter.Foreground = {ThemeResource PhoneHighContrastSelectedForegroundThemeBrush}`<br>`HighContrastBackground.Background = {ThemeResource PhoneHighContrastSelectedBackgroundThemeBrush}` | `^:selected` | as Selected |
| [x] | `SelectionStates` | `SelectedDisabled` | nothing | `^:selected:disabled` | nothing |

#### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on ContentPresenter`<br>`PointerUpThemeAnimation on HighContrastBackground` | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on ContentPresenter`<br>`PointerUpThemeAnimation on HighContrastBackground` | the tilt's return (cf:Tilt); open, the box's tilt goes at once |
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on ContentPresenter`<br>`PointerUpThemeAnimation on HighContrastBackground` | the tilt's return (cf:Tilt); open, the box's tilt goes at once |

#### Resources (5)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ComboBoxItemDisabledThemeOpacity` | `0.4` | same | yes | by its key (DynamicResource) |
| [x] | `PhoneHighContrastSelectedForegroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | by its key (DynamicResource) |
| [x] | `PhoneHighContrastSelectedBackgroundThemeBrush` | `SolidColorBrush #00000000` | `SolidColorBrush #00FFFFFF` | yes | by its key (DynamicResource) |
| [x] | `ComboBoxItemMinHeightThemeSize` | `33.5` | same | yes | by its key (DynamicResource) |
| [x] | `ComboBoxItemHighContrastBackgroundThemeMargin` | `0,0,0,3.5` | same | yes | by its key (DynamicResource) |


