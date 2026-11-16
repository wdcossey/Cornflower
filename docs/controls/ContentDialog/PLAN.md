# ContentDialog: plan

[#157](https://github.com/wdcossey/CornflowerUI/issues/157). The 8.1 ContentDialog: a dialog drawn from the top of the
window over a dim layer, with a title, content and up to two buttons, shown with `ShowAsync`. Avalonia has none, so it
is a new control (rung 5), `ContentDialog` in `Cornflower.Controls`, with WinRT's API; Wp81 themes it and hosts its
popup through Flyout's popup host (#151, PR #161), with its own motion and Back.

The layout and dismissal are [#150](https://github.com/wdcossey/CornflowerUI/issues/150)'s reading (PR #172), the
motion [#149](https://github.com/wdcossey/CornflowerUI/issues/149)'s (PR #171).

## Where the values come from

- **The style**: the 8.1 Update 1 runtime's `contentdialog.json` (`R:\cornflower-reference\flash-update1\xbf-json\`),
  equal to the SDK's: a `Border` `Container` round a `Grid` `LayoutRoot` round a `Border` `BackgroundElement` (the
  `Background`, `ContentDialogBackgroundThemeBrush`) round a `Grid` `ContentPanel` of three rows (each at least
  `ContentDialogTitleMinHeight` 19, `ContentDialogContentMinHeight` 35, `ContentDialogButtonsMinHeight` 45) and two star
  columns: a `ContentControl` `Title` (SemiBold, `TextStyleExtraLargeFontSize`, `ContentDialogTitleMargin` 19,33.5,19,0),
  a `ContentPresenter` `Content` (`TextStyleLargeFontSize`, `ContentDialogContentMargin` 19,16.5,19,0) and two `Border`
  hosts (`ContentDialogButton1HostPadding` 19,24,9.5,9.5 and `ContentDialogButton2HostPadding` 9.5,24,19,9.5), which the
  control fills with its Buttons. One state group, `Orientation`: `Landscape` makes `ContentPanel` 400 wide
  (`ContentDialogContentLandscapeWidth`), at the left.
- **The screen grabs** of refapp runs `20261116-1934-dev11` (`-Popups also -Grab`) and `20261116-1944-dev11` (the
  keyboard), dark and light, 1080 × 1920 at 2.4 (device 11): two buttons, one (primary or secondary), none,
  `FullSizeDesired`, a long title and forty lines of body, the same full size, and a focused TextBox. Committed in
  [`ref/wp81/ContentDialog/`](../../../ref/wp81/ContentDialog/README.md); #150 read them (`tools/refapp/README.md`,
  "ContentDialog's layout").
- **The dismissal probes** `20261116-1951-dev11` and `-1955` (`-Probe dismiss`, `dismissagain`), read by #150 ("How each
  popup is dismissed").
- **The recording** of run `20261116-1835-dev11` (`-Probe popupcurves -Record`), read by #149 ("The popups' bands"), its
  still in [`ref/wp81/ContentDialog/motion/`](../../../ref/wp81/ContentDialog/motion/README.md); and #66's times
  (`20261110-0819-dev11`, "Flyouts, pickers and the dialog").

## The behaviours

Each has its test in `tests/Cornflower.Tests/Controls/ContentDialogTests.cs`, against `ContentDialogReadings`.

| Behaviour | Evidence | Test |
|---|---|---|
| Shown in the window's overlay layer, at its top left, its width, as tall as it asks: 199.17 with buttons | grabs; the phone's popups are in its one window | `It_Is_At_The_Windows_Top_In_Its_Overlay_Layer_Over_A_Dim_Layer` |
| A `#99000000` layer (`#99FFFFFF` light) over the whole window under it | dumps: a full-window Rectangle popup; grabs | the same, `The_Chrome_Is_The_Phones_And_The_Page_Is_Dimmed_Under_It`, `The_Light_Theme_Dims_With_White_Also_After_The_Window_Changes_To_It` |
| The chrome `#FF1F1F1F` dark, `#FFDDDDDD` light | grabs, dumps | `The_Chrome_Is_The_Phones_And_The_Page_Is_Dimmed_Under_It` |
| Title at 19.17, 33.33; body from 84.17; hosts from 108.33; outlines 196.67 × 38.33 at 19.17 and 234.58, y 142.08 | #150 | `The_Title_The_Body_And_Two_Buttons_Are_Where_The_Phones_Are` |
| A string title or body doesn't wrap: one line, cut at the edge | grabs (the body "…long enough to wr") | `A_String_Body_And_Title_Dont_Wrap` |
| A lone button, primary or secondary, takes the left half | #150 | `A_Lone_Button_Takes_The_Left_Half` |
| No button: the hosts collapse, 153.33 tall | #150 | `With_No_Button_The_Hosts_Collapse_And_The_Row_Keeps_Its_45` |
| `FullSizeDesired` fills the window; the buttons go to a bar the shell draws, outlines 194.17 × 41.67 at 20.83 and 235.83, y 747.08 | #150 | `FullSizeDesired_Fills_The_Window_And_Puts_The_Buttons_In_The_Shells_Bar` |
| A long title and body fill the window as `FullSizeDesired`: the title on one line, the body clipped at the bar, no ScrollViewer | #150 (the long body's panel 1,094.58 tall in an 800 dialog) | `A_Long_Title_And_Body_Fill_The_Window_The_Title_On_One_Line_The_Body_Clipped_At_The_Bar`, `A_Body_That_Fits_Is_Not_Full` |
| In landscape the content panel is 400 wide at the left | the style's `Landscape` state | `In_Landscape_The_Content_Panel_Is_400_Wide_At_The_Left` |
| Opens as one band about its centre line, from 45° to 0 on an eased exponential (exponent 1.68), at rest at tick 9 (141 ms) | #149 | `It_Opens_As_One_Band_From_45_Degrees_Easing_Out_To_Rest_At_Tick_9` |
| `Hide()`: `Closing`, `Closed` and the result at once; drawn as it is, then gone in one frame 95 to 120 ms later, the dim with it | #149, #150 | `Hide_Raises_Closing_And_Closed_And_Completes_At_Once_And_It_Goes_In_One_Frame_About_107_Ms_Later`, `Hidden_Mid_Opening_It_Rests_Until_It_Goes` |
| A button: its click, `Closing` with its result, `Closed`, then the result, in one frame | #150 | `A_Click_On_A_Button_Raises_Its_Click_Closing_Closed_And_The_Result_In_That_Order` |
| A tap on the dim layer does nothing and reaches nothing; a tap on the body does nothing | #150 | `A_Tap_On_The_Dim_Layer_Does_Nothing_And_Reaches_Nothing`, `A_Tap_On_The_Body_Does_Nothing` |
| Back: `Closing`, `Closed` and the result `None` in one frame, before the page's `NavigationPage`; taken while it is still drawn | #150 | `Back_Hides_It_With_None_Before_The_NavigationPage_Goes_Back`, `Back_Kept_Open_By_Closing_Is_Still_Taken`, `Back_Closes_A_Flyout_Over_The_Dialog_First`, `Back_Takes_Only_Its_Own_Windows_Dialog` |
| The buttons' commands, parameters and enabled state; a click's `Cancel` and deferral; `Closing`'s `Cancel` and deferral | WinRT's API | `A_Button_Runs_Its_Own_Command_Once_With_Its_Parameter_And_Closes_With_Its_Result`, `A_Command_That_Cant_Run_And_A_Button_Not_Enabled_Disable_Their_Buttons`, `A_Cancelled_Click_Runs_No_Command_And_Keeps_It_Open`, `A_Deferred_Click_Waits_For_Its_Deferral`, `Closing_Can_Keep_It_Open_At_Once_Or_After_A_Deferral` |
| `ShowAsync` shows it again, also while the last is still going; twice at once throws | WinRT's API | `It_Shows_Again_Placed_And_Moving_Even_Before_It_Has_Gone`, `ShowAsync_While_Shown_Throws` |
| `ShowAsync()` finds the app's window; with none it throws | WinRT's has no window argument | `ShowAsync_Without_A_Window_Throws_And_On_Desktop_Shows_Over_The_Main_Window` |
| Its window closing closes it with `None` | robustness | `Its_Window_Closing_Closes_It_With_None` |
| `TitleTemplate`, `ContentTemplate` | WinRT's API | `The_Title_And_Content_Templates_Are_Used` |
| Modal: it takes the keyboard focus, Tab stays in it, and the focus goes back as it closes | WinRT's dialog is modal | `It_Takes_The_Focus_Keeps_Tab_In_It_And_Gives_The_Focus_Back` |
| A `Closed` handler may show it again; a click deferred in one show doesn't reach the next; a deferral taken after its handler returned throws | WinRT's API | `A_Closed_Handler_Can_Show_It_Again`, `A_Click_Deferred_In_One_Show_Doesnt_Reach_The_Next`, `A_Deferral_Taken_After_The_Handler_Has_Returned_Throws` |
| A dialog in a tree of its own (a page's child) can't be shown; one in resources or made in code can | Avalonia: a control has one parent | `A_Dialog_In_A_Tree_Of_Its_Own_Cant_Be_Shown` |
| Back reaches the dialog past a popup no phone host has (Avalonia's own); its theme gone and back while shown keeps one host | robustness | `Back_Hides_It_Under_A_Popup_No_Phone_Host_Has`, `Its_Theme_Gone_And_Back_While_Shown_Keeps_One_Host` |

## The visual states

| Group | State | Keys | Here |
|---|---|---|---|
| `Orientation` | `Portrait` | none | the rest look |
| `Orientation` | `Landscape` | `ContentDialogContentLandscapeWidth`, `HorizontalAlignment` Left | `PhonePopupHost.IsLandscape` on the dialog |

## The phone's ContentDialog against ours

| Difference | Rung | What we do |
|---|---|---|
| Avalonia has no ContentDialog | 5 | `ContentDialog : ContentControl` in `Cornflower.Controls`, WinRT's API; `ShowAsync` returns a `Task<ContentDialogResult>` |
| The phone shows it in its one window | 2 | the control opens a `Popup` with itself as child, in the target window's logical tree; Wp81's `PhoneContentDialog` hosts it in a `PhonePopupHost` (placement Top, the dim layer, Back) |
| The dialog's opening is not the Flyout's (45° on an ease, not 80° linear) | 2 | `PhoneContentDialog` sets the band's angle over the host's each frame, after it |
| The dialog's close is gone at about 107 ms with its dim; `Closed` at once | 2 | the control raises its events and completes at once, then asks the theme's `CloseMotion`; the theme closes the popup 107.5 ms later |
| The shell draws the full dialog's buttons in a bar of its own (a CommandBar in the tree) | 2 | `PhoneContentDialogPanel`, the template's `ContentPanel`, places the dialog's own hosts in the bar's slots: the same Buttons, larger outlines |
| WinRT's `ShowAsync()` takes no window | 5 | `ShowAsync()` uses the app's lifetime (the active or main window, or the single view); `ShowAsync(TopLevel)` names one (an extension) |
| WinRT's handlers are `TypedEventHandler<ContentDialog, …>` | 5 | `EventHandler<…>`, the sender the dialog |
| The keyboard takes the first Back | — | the platform's: Android's input method takes it (owed: the device run) |

## Extensions

- `ShowAsync(TopLevel)`: shows the dialog over a given window; WinRT's `ShowAsync()` shows it over the app's.

## Dependencies

- `PhonePopupHost`, `PhonePopupMotion`, `PhonePopupBands`, `PhonePopupDismissal` (#151), reused unchanged.
- Wp81's Button theme, which the dialog's Buttons take; `PhoneContentLine` for the title's and body's line.
- No capture is still needed for what is built. Not read: the full-size dialog's motion (taken as the default's one
  band), the bar in landscape, and the command's order against the click (ours: after the click's event).

## The checklist

Generated on 2026-11-16 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done; `[-]` is left out, with its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist ContentDialog <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ContentDialog`; Avalonia none: a new control (rung 5).

### Properties (11)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `TitleTemplate` | `DataTemplate` | none | `ContentDialog.TitleTemplate` (`IDataTemplate`) |
| [x] | `Title` | `Object` | none | `ContentDialog.Title` |
| [x] | `SecondaryButtonText` | `String` | none | `ContentDialog.SecondaryButtonText`; no button while empty |
| [x] | `SecondaryButtonCommandParameter` | `Object` | none | `ContentDialog.SecondaryButtonCommandParameter` |
| [x] | `SecondaryButtonCommand` | `ICommand` | none | `ContentDialog.SecondaryButtonCommand`, run after the click's event; its `CanExecute` disables the button |
| [x] | `PrimaryButtonText` | `String` | none | `ContentDialog.PrimaryButtonText`; no button while empty |
| [x] | `PrimaryButtonCommandParameter` | `Object` | none | `ContentDialog.PrimaryButtonCommandParameter` |
| [x] | `PrimaryButtonCommand` | `ICommand` | none | `ContentDialog.PrimaryButtonCommand`, as the secondary's |
| [x] | `IsSecondaryButtonEnabled` | `Boolean` | none | `ContentDialog.IsSecondaryButtonEnabled`, true by default |
| [x] | `IsPrimaryButtonEnabled` | `Boolean` | none | `ContentDialog.IsPrimaryButtonEnabled`, true by default |
| [x] | `FullSizeDesired` | `Boolean` | none | `ContentDialog.FullSizeDesired`; the theme fills the window and puts the buttons in the bar |

### Events (5)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Closed` | `TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs>` | none | `EventHandler<ContentDialogClosedEventArgs>` (`Result`), before the result |
| [x] | `Closing` | `TypedEventHandler<ContentDialog, ContentDialogClosingEventArgs>` | none | `EventHandler<ContentDialogClosingEventArgs>` (`Result`, `Cancel`, `GetDeferral()`) |
| [x] | `Opened` | `TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs>` | none | `EventHandler<ContentDialogOpenedEventArgs>`, in `ShowAsync`'s call |
| [x] | `PrimaryButtonClick` | `TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>` | none | `EventHandler<ContentDialogButtonClickEventArgs>` (`Cancel`, `GetDeferral()`) |
| [x] | `SecondaryButtonClick` | `TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>` | none | as the primary's |

### Methods (2)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void Hide()` | none | `ContentDialog.Hide()`: `Closing` with `None`, which may keep it open |
| [x] | `IAsyncOperation<ContentDialogResult> ShowAsync()` | none | `Task<ContentDialogResult> ShowAsync()`, and `ShowAsync(TopLevel)` |

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | left out: the dialog's template sets none; Avalonia has no ContentTransitions |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | not applicable: Avalonia's `ContentTemplate` is an `IDataTemplate`, which selects |
| [x] | `ContentTemplate` | `DataTemplate` | none | Avalonia's `ContentControl.ContentTemplate`; bound in the template |
| [x] | `Content` | `Object` | none | Avalonia's `ContentControl.Content`; bound in the template |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | not applicable: Avalonia's presenter has `Child` |

### Events, from `ContentControl` (0)

_none_

### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not applicable: Avalonia's property change |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | not applicable: Avalonia's property change |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | not applicable: no selector |

### Default style: setters (1)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `{ThemeResource ContentDialogBackgroundThemeBrush}` | none | Themed: `ContentDialogBackgroundThemeBrush`; `Foreground` `PhoneForegroundBrush` too, as the FlyoutPresenter's |

### Template parts (8)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Container` | `Border` | `PART_Container` | Themed: `Container`; the control doesn't look for it, so no `PART_` |
| [x] | `LayoutRoot` | `Grid` | `PART_LayoutRoot` | Themed: `LayoutRoot`, a `Panel` (its one Auto row) |
| [x] | `BackgroundElement` | `Border` | `PART_BackgroundElement` | Themed: `BackgroundElement` |
| [x] | `ContentPanel` | `Grid` | `PART_ContentPanel` | Themed: `ContentPanel`, a `PhoneContentDialogPanel`: the grid's rows and columns, and the full dialog's bar |
| [x] | `Title` | `ContentControl` | `PART_Title` | Themed: `Title`, a `ContentPresenter` with the phone's line |
| [x] | `Content` | `ContentPresenter` | `PART_Content` | Themed: `PART_ContentPresenter`, the part Avalonia's `ContentControl` looks for; it clips |
| [x] | `Button1Host` | `Border` | `PART_Button1Host` | `PART_Button1Host`: the control puts its first button in it |
| [x] | `Button2Host` | `Border` | `PART_Button2Host` | `PART_Button2Host`: the second |

### Visual states (2)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `Orientation` | `Portrait` | nothing | `^:portrait` | The rest look |
| [x] | `Orientation` | `Landscape` | `ContentPanel.Width = {ThemeResource ContentDialogContentLandscapeWidth}`<br>`ContentPanel.HorizontalAlignment = Left` | `^:landscape` | `PhonePopupHost.IsLandscape` on the dialog, set by its host while the window is wider than tall, as the Flyout's |

### Transitions (0)

_none_

### Resources (12)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ContentDialogBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FF1F1F1F` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FFDDDDDD` | yes | Used |
| [x] | `ContentDialogContentLandscapeWidth` | `400` | same | yes | Used |
| [x] | `ContentDialogTitleMinHeight` | `19` | same | yes | Used |
| [x] | `ContentDialogContentMinHeight` | `35` | same | yes | Used |
| [x] | `ContentDialogButtonsMinHeight` | `45` | same | yes | Used |
| [x] | `ContentDialogTitleMargin` | `19,33.5,19,0` | same | yes | Used |
| [x] | `TextStyleExtraLargeFontSize` | `25.5` | same | yes | Used |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | Used (Selawik, ADR 0001) |
| [x] | `TextStyleLargeFontSize` | `18.14` | same | yes | Used |
| [x] | `ContentDialogContentMargin` | `19,16.5,19,0` | same | yes | Used |
| [x] | `ContentDialogButton1HostPadding` | `19,24,9.5,9.5` | same | yes | Used |
| [x] | `ContentDialogButton2HostPadding` | `9.5,24,19,9.5` | same | yes | Used |

`ContentDialogDimmingThemeBrush` (`#99000000`, `#99FFFFFF` light), in Tokens, isn't in the style: the shell's dim layer
uses it, through `PhonePopupHost.Dim`.
