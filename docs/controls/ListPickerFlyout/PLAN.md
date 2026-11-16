# ListPickerFlyout, PickerFlyout and their presenters: plan

[#153](https://github.com/wdcossey/CornflowerUI/issues/153). The 8.1 full-screen pickers and the pickers' bar. WinRT's
`ListPickerFlyout`, `PickerFlyout`, `ListPickerFlyoutPresenter`, `PickerFlyoutPresenter` and `PickerFlyoutBase` have no
Avalonia type, so they are new controls in `Cornflower.Controls` (rung 5): the flyouts on Avalonia's `PopupFlyoutBase`,
the presenters on its `PickerPresenterBase`, as the issue asks. The presenters are themed (rung 1); the popup round them
is the Flyout's (#151), turned on by their themes (rung 2). The pickers' bar is Wp81's `CommandBar` with two
`CommandBarButton`s and two glyphs of our own (B15, `Q-picker-icons`: glyph).

## Where the values come from

- **The styles**: the 8.1 Update 1 runtime's `listpickerflyoutpresenter.json` and `pickerflyoutpresenter.json`
  (`R:\cornflower-reference\flash-update1\xbf-json\`), equal to the SDK's.
- **The grabs and dumps** of refapp run `20261116-1934-dev11` (#150, `-Popups also -Grab`): a ListPickerFlyout of "one"
  to "five" titled "LIST PICKER", Single with "two" selected and Multiple with "two" and "four" checked; a PickerFlyout
  of "picker flyout content" with and without its buttons; dark and light, 1080 × 1920 at 2.4 (device 11). Committed
  by #150 in [`ref/wp81/ListPickerFlyout/`](../../../ref/wp81/ListPickerFlyout/README.md) and
  [`ref/wp81/PickerFlyout/`](../../../ref/wp81/PickerFlyout/README.md). The dumps (outside the repository) give each
  part's bounds; the second popup of each is the bar, a `CommandBar` with `AppBarButton`s `AcceptBtn` ("Done") and
  `CancelBtn` ("Cancel").
- **The pickers' bar** (B15): #150's reading with `tools/refapp/pickerbar.py` ([`tools/refapp/README.md`](../../../tools/refapp/README.md),
  "The pickers' bar").
- **ListPickerFlyout in Multiple**: "ListPickerFlyout in `Multiple`" there.
- **The events and Back**: runs `20261116-1951-dev11` and `-1955-` (`-Probe dismiss`, `dismissagain`): "How each popup
  is dismissed".
- **The motion**: #149's run `20261116-1835-dev11` ("The popups' bands").

## The behaviours

Each has its test in `tests/Cornflower.Tests/Controls/ListPickerFlyoutTests.cs`, against `ListPickerFlyoutReadings`.

| Behaviour | Evidence | Test |
|---|---|---|
| The presenters are the theme's; their popup is the phone's (`PhonePickerFlyout`, a `PhonePopupHost`), in the window's overlay layer | the Flyout's | `The_Presenters_Are_Templated_By_The_Theme_And_Hosted_By_The_Phones_Popup` |
| A picker fills the window, 0, 0, 450 × 800, in `FlyoutBackgroundThemeBrush` (`#1F1F1F`, `#DDDDDD`) | dumps, grabs | `A_Picker_Fills_The_Window_In_The_Chrome` |
| The title in `FlyoutPickerTitleTextBlockStyle` (18.14 SemiBold, spacing 15) at 19.17, 37.08; the content panel 19, 37 in | dump | `The_Title_Is_The_Pickers_Title_Style_At_The_Content_Panels_Top_Left` |
| The items from 93.33, each 39.17 tall and 58.33 apart, in `PhoneFontFamilySemiLight` 25.5; the list 371.67 tall for five (an 80 footer) | dump | `The_Items_Are_Listed_Below_The_Title_58_33_Apart_In_The_Large_SemiLight_Text` |
| Single: no check boxes, no earmark on the selected item, no bar; the content to the window's foot | grab, dump | `In_Single_There_Are_No_Check_Boxes_No_Earmark_And_No_Bar` |
| Multiple: a 25.42 check box at x 19.17, 9.5 down each item, its tick shown when checked; the text from 44.67; the bar; the content panel 700 tall | grab, dump, #150 | `In_Multiple_Each_Item_Has_A_Check_Box_Its_Text_Beside_It_And_The_Bar_Is_Shown` |
| PickerFlyout: the content in a ScrollViewer from the panel's top with no title, below `TitleTextBlockStyle`'s title with one | dump | `A_PickerFlyouts_Content_Is_Below_Its_Title_And_With_No_Title_At_The_Panels_Top` |
| PickerFlyout without its buttons: no bar, the content to the foot | dump | `A_PickerFlyout_Without_Its_Buttons_Has_No_Bar_And_Its_Content_Goes_To_The_Foot` |
| Landscape: `PickerFlyoutContentPanelLandscapeThemeMargin` | the style's `Landscape` state | `In_Landscape_The_Content_Panel_Takes_The_Landscape_Margin` |
| The bar: the page's compact app bar, top 737.08, 62.92 tall; accept and cancel rings 40.80 across at x 187.50 and 261.25, y 768.33; labels "done" and "cancel" | #150 | `The_Bar_Is_The_Pages_Compact_App_Bar_With_Accept_And_Cancel_Where_The_Shells_Are` |
| The glyphs: the check's ink 19.96 × 17.18 centred in its ring, the cross's 16.01 × 15.82 centred +0.19, +0.48 | #150 | `The_Accept_And_Cancel_Glyphs_Ink_The_Shells_Boxes_At_Their_Places` |
| `Opening` and `Opened` in the call's frame | #150 | `Opening_And_Opened_Come_In_The_Calls_Frame` |
| Single: a tap picks; `ItemsPicked` (added, removed), the selection and `ShowAtAsync`'s result in the tap's frame; `Closed` after the motion | #150: added "four", removed "two", `SelectedIndex` 3, `[four]` | `In_Single_A_Tap_On_An_Item_Picks_It_The_Result_And_ItemsPicked_At_Once_And_Closed_After_The_Motion` |
| Multiple: a tap ticks or unticks, nothing raised; accept picks those checked, in one frame | #150: added `[one]`, removed `[two]`, `SelectedIndex` 0, `[one]` | `In_Multiple_A_Tap_Checks_An_Item_And_Raises_Nothing_And_Accept_Picks_Those_Checked` |
| Cancel, Back and `Hide()`: null in `Closed`'s frame, no `ItemsPicked`, the selection as it was | #150 | `Cancel_Back_And_Hide_Close_With_No_Pick_The_Result_Null_In_Closeds_Frame` |
| PickerFlyout: accept raises `Confirmed` with true at once; cancel and Back give false | #150 | `PickerFlyouts_Accept_Raises_Confirmed_With_True_At_Once_And_Cancel_And_Back_Give_False` |
| Opened again, the picks so far are checked | WinRT's `SelectedItems` | `A_Picker_Opened_Again_Shows_What_Was_Picked_Checked` |
| A picker that doesn't open gives its empty result at once; a second `ShowAtAsync` ends the first with none | WinRT's async contract | `ShowAtAsync_Gives_Its_Empty_Result_At_Once_When_The_Picker_Doesnt_Open`, `A_Second_ShowAtAsync_Ends_The_First_With_No_Result` |
| `SelectedIndex`, `SelectedItem` and `SelectedItems` follow one another; an index set before the items takes its item when they come, one past them is none, and new items drop a picked item they lack | WinRT | `SelectedIndex_SelectedItem_And_SelectedItems_Follow_One_Another`, `An_Index_Set_Before_The_Items_Takes_Its_Item_When_They_Come_And_One_Past_Them_Is_None` |
| Equal items are checked and picked by their places | a list may hold equal items | `Equal_Items_Are_Checked_And_Picked_By_Their_Places` |
| Each opening starts at the top of the list | chosen: not read | `Each_Opening_Starts_At_The_Top_Of_The_List` |
| A second tap while it closes picks nothing more: the popup takes no taps once its bands move | the host's (#161) | `A_Second_Tap_While_It_Closes_Picks_Nothing_More` |
| `ShowAtAsync` at another target while open ends the first wait and waits for the new opening | WinRT's async contract | `ShowAtAsync_At_Another_Target_While_Open_Waits_For_The_New_Opening` |
| The picker flyout presenter's `ContentTemplate` shows its content | WinRT's `ContentControl` | `The_Picker_Flyout_Presenters_ContentTemplate_Shows_Its_Content` |
| A second accept by the keyboard while it closes is nothing | one answer an opening | `A_Second_Accept_By_The_Keyboard_While_It_Closes_Is_Nothing` |
| Open, the picker follows its title, items and buttons | WinRT's properties | `Open_The_Picker_Follows_Its_Title_Items_And_Buttons` |
| A closed picker flyout lets go of its content | a control has one parent | `A_Closed_Picker_Flyout_Lets_Go_Of_Its_Content` |
| A template applied again keeps what is checked | the list's own state | `A_Template_Applied_Again_Keeps_What_Is_Checked` |
| `ItemTemplate` and `DisplayMemberBinding` show the items | WinRT's `ItemTemplate`, `DisplayMemberPath` | `The_Items_Are_Shown_By_Their_ItemTemplate_Or_DisplayMemberBinding` |
| Opening and closing are the full Flyout's bands | #149 for PickerFlyout; ListPickerFlyout below | `A_Picker_Opens_And_Closes_As_The_Full_Flyout_Does` |
| Under Simple alone the popup is Avalonia's; a theme going mid-close closes it | the Flyout's | `Simple_Alone_Keeps_Avalonias_Own_Popup_And_The_Picker_Still_Closes`, `A_Picker_Whose_Theme_Goes_Mid_Close_Closes` |

The sample page's tests, `tests/Cornflower.Tests/Sample/ListPickerFlyoutPageTests.cs`, open each picker from the page,
pick, press Back and accept, and check the light theme reaches the picker.

## The visual states

| Group | State | Keys | Here |
|---|---|---|---|
| `OrientationStates` | `Portrait` | `PickerFlyoutContentPanelPortraitThemeMargin` | the rest look |
| `OrientationStates` | `Landscape` | `PickerFlyoutContentPanelLandscapeThemeMargin` | `PhonePopupHost.IsLandscape` on the presenter |

## Avalonia against the phone

| Difference | Rung | What we do |
|---|---|---|
| Avalonia has no list picker, picker flyout or their presenters | 5 | new controls on `PopupFlyoutBase` and `PickerPresenterBase` |
| The phone's runtime fills `ItemsHostPanel` with a ListView | 1 | the template holds a `ListBox`, `PART_ItemsHost`, Wp81's ListView; the presenter fills it as the picker opens |
| The list picker's items differ from a page's ListView's: a 19 margin, 25.5 SemiLight text, in Multiple the content 25.5 over, not 40.5 | 1 | styles on the template's `ListBox` (a `BasedOn` across theme files can't be relied on) |
| The phone's bar is a second popup the shell draws | 1 | the bar is in the presenter's template, laid over its foot; it moves with the popup's bands |
| `PickerFlyoutPresenter` is a WinRT `ContentControl` | 5 | a `PickerPresenterBase` with `Content` and `ContentTemplate`; its content alignment is the style's Stretch, fixed in the template |
| A presenter has no title or confirmation-buttons property in WinRT (the runtime sets its parts) | 2 | `PickerPresenterOptions.Title` and `ShowsConfirmationButtons`, attached, which the picker sets and the theme reads |
| `DisplayMemberPath` | 5 | Avalonia's name and type, `DisplayMemberBinding` |

## The checklists

Generated on 2026-11-16 by `dotnet run --project tools/winrt-api -- checklist <Type> <Windows.winmd> <design folder>`
from the phone SDK's `Windows.winmd` (`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design
folder (`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason. The rows `ListPickerFlyout` and `PickerFlyout` inherit are given once.

### ListPickerFlyout

| ✓ | WinRT | Here |
|---|---|---|
| [x] | `SelectionMode` (`ListPickerFlyoutSelectionMode`: `Single` 0, `Multiple` 1) | `SelectionMode`, the same enum |
| [x] | `SelectedIndex`, `SelectedItem` | styled, two-way; each sets the other and `SelectedItems` |
| [x] | `SelectedItems` (read-only) | an `IList<object>`; a change sets `SelectedIndex` and `SelectedItem` to its first |
| [x] | `ItemsSource`, `ItemTemplate` | the same |
| [x] | `DisplayMemberPath` | `DisplayMemberBinding`, Avalonia's `ItemsControl`'s |
| [-] | `SelectedValue`, `SelectedValuePath` | not carried: a flyout isn't a selecting control in Avalonia; read `SelectedItem` (decided on the user's behalf) |
| [x] | `ItemsPicked` (`ItemsPickedEventArgs`: `AddedItems`, `RemovedItems`) | the same, an `EventHandler<ItemsPickedEventArgs>` |
| [x] | `ShowAtAsync(target)` → `IReadOnlyList<object>` | `Task<IReadOnlyList<object>?>`: the pick, or null |

### PickerFlyout

| ✓ | WinRT | Here |
|---|---|---|
| [x] | `Content` | `object?`, the content property |
| [x] | `ConfirmationButtonsVisible` | the same |
| [x] | `Confirmed` (`PickerConfirmedEventArgs`) | the same |
| [x] | `ShowAtAsync(target)` → `bool` | `Task<bool>` |

### Inherited by both

| ✓ | WinRT | Here |
|---|---|---|
| [x] | `PickerFlyoutBase.Title` (attached) | `PickerFlyoutBase.TitleProperty`, attached, `GetTitle`, `SetTitle` |
| [x] | `OnConfirmed()`, `ShouldShowConfirmationButtons()` (protected, overridable) | the same, on `PickerFlyoutBase` |
| [-] | `Placement` | ignored, as on the phone: a picker fills the window. Avalonia's `PopupFlyoutBase.Placement` is there and has no effect under Wp81 |
| [x] | `Opening`, `Opened`, `Closed`, `ShowAt`, `Hide`, `CreatePresenter` | Avalonia's `PopupFlyoutBase` |
| [-] | `Dispatcher`, `ReadLocalValue`, `GetAnimationBaseValue` | Avalonia has none; `GetValue`, `SetValue`, `ClearValue` are Avalonia's |

### ListPickerFlyoutPresenter

| ✓ | WinRT | Here |
|---|---|---|
| [x] | setter `Background` `FlyoutBackgroundThemeBrush` | the same; `Foreground` `PhoneForegroundBrush` too (Wp81 themes no window) |
| [-] | setter `AutomationProperties.AutomationId` | automation isn't themed in Cornflower |
| [x] | parts `Background` (Border), `ContentPanel` (Grid), `TitlePresenter` (TextBlock), `ItemsHostPanel` (Grid) | the same names; `PART_ItemsHost`, the `ListBox`, added |
| [-] | part `ContentPanelTransform` | no state or storyboard moves it; the shell's bands move the popup |
| [x] | `OrientationStates` | above |
| [x] | `PickerFlyoutContentPanelPortraitThemeMargin`, `…LandscapeThemeMargin`, `PickerFlyoutTitleThemeMargin`, `FlyoutPickerTitleTextBlockStyle`, `FlyoutContentThemeMargin` | by `DynamicResource` |

### PickerFlyoutPresenter

| ✓ | WinRT | Here |
|---|---|---|
| [x] | `Content`, `ContentTemplate` | `AddOwner` of `ContentControl`'s |
| [-] | `ContentTransitions`, `ContentTemplateSelector`, `ContentTemplateRoot`, the three `On…Changed` | Avalonia has none (a template can select itself) |
| [x] | setters `Background`, `IsTabStop` | the same |
| [x] | setters `HorizontalContentAlignment`, `VerticalContentAlignment` Stretch | fixed in the template: the presenter has no content alignment |
| [x] | setters `ScrollViewer.HorizontalScrollMode`, `VerticalScrollMode` Auto | the ScrollViewer scrolls both ways as asked by its scroll bar visibility (Disabled across, Visible down, WinRT's defaults) |
| [-] | setter `ScrollViewer.ZoomMode` | Avalonia's ScrollViewer has no zoom |
| [-] | setter `AutomationProperties.AutomationId` | as above |
| [x] | parts `Background`, `ContentPanel`, `TitlePresenter` | the same names, and the `ScrollViewer` |
| [x] | `OrientationStates`; resources (`TitleTextBlockStyle` for the title) | as the list picker's |

## The pickers' bar

The bar is the page's compact app bar (#150), so it is Wp81's `CommandBar` (#133, #144), closed and compact, with two
`CommandBarButton`s named `PART_AcceptButton` ("done") and `PART_DismissButton` ("cancel"), the names Avalonia's
date and time presenters give their buttons, so #156 can put the same bar in theirs. The glyphs are ours, in
`Controls/PickerBar.axaml`, drawn to #150's measurements (its comment has them). The README's "The pickers' bar" says
how to use it.

## Dependencies

- Flyout's `PhonePopupHost`, `PhonePopupMotion`, `PhonePopupBands` and `PhonePopupDismissal` (#151), unchanged.
- Wp81's ListView (`ListBox`, the check mode), CommandBar, ScrollViewer and text styles.
- `ListBoxOptions` (item click), in Single.
- No capture is still needed for the look. Owed for the motion: the ListPickerFlyout's 66.7 px rows (see the README's
  known gaps).
