# Flyout and FlyoutPresenter: plan

[#151](https://github.com/wdcossey/CornflowerUI/issues/151). The 8.1 Flyout, and the worked example for Tier 3's popups:
how a popup is hosted over the page, placed, dimmed, opened and closed on `CornflowerClock`, and how it takes Back from
the page. WinRT's `Flyout` and `FlyoutPresenter` are Avalonia's. `FlyoutPresenter` is themed (rung 1); the popup round it
is a behaviour the theme turns on (rung 2); WinRT's `Placement="Full"` is one attached property Avalonia lacks,
`FlyoutOptions.IsFullScreen` (rung 2). No type is substituted.

The motion is [#149](https://github.com/wdcossey/CornflowerUI/issues/149)'s reading (PR #171). The **dismissal** waits
on [#150](https://github.com/wdcossey/CornflowerUI/issues/150): this is a draft, with it in one marked place
([What waits](#what-waits-on-150)).

## Where the values come from

- **The style**: the 8.1 Update 1 runtime's `flyoutpresenter.json` (`R:\cornflower-reference\flash-update1\xbf-json\`),
  equal to the SDK's. A `Border` named `Background` round a `ScrollViewer` named `ScrollViewer`, its margin
  `FlyoutContentPanelPortraitThemeMargin` (0,25.5,0,0; landscape `FlyoutContentPanelLandscapeThemeMargin`, 0), round a
  `ContentPresenter` whose margin is the `Padding`. Setters below. `generic.xaml` has no style for `Flyout` itself.
- **The screenshots**: refapp run `20261108-2254-dev11` (`-Popups only -Grab`; `-2248` the same to the byte), a
  `Flyout` shown with `ShowAt` on a Button "open", its content a wrapping `TextBlock` "flyout content", `Placement`
  Top and Full, dark and light, 1080 × 1920 at 2.4 (device 11). Committed in
  [`ref/wp81/Flyout/`](../../../ref/wp81/Flyout/README.md).
- **The popup dumps** of runs `20261108-1705-dev11`, `-1734` and `-2254` (outside the repository): the presenter's tree
  in window coordinates.
- **The recording** of run `20261116-1835-dev11` (`-Probe popupcurves -Record`), read by #149 with
  `tools/motion/strips.py` ([`tools/refapp/README.md`](../../../tools/refapp/README.md), "The popups' bands, and the
  inline ComboBox"), and its stills in [`ref/wp81/Flyout/motion/`](../../../ref/wp81/Flyout/motion/README.md).

## The behaviours

Each has its test in `tests/Cornflower.Tests/Controls/FlyoutTests.cs`, against `FlyoutReadings`.

| Behaviour | Evidence | Test |
|---|---|---|
| The popup is drawn in the window, in its overlay layer, never as a window of its own | the phone has one window; the dumps' popups are in window coordinates | `The_Popup_Is_In_The_Windows_Own_Overlay_Layer` |
| A top flyout is at the window's top left, the window's width, its own height: 0, 0, 450 × 78.33 (188 raw rows) | dump, screenshot | `A_Top_Flyout_Is_At_The_Windows_Top_Left_Its_Width` |
| WinRT's Top, Bottom, Left and Right are all drawn at the top | the phone has no room beside a target; Avalonia's `Pointer` too | `Any_Placement_But_Full_Is_At_The_Top` |
| A full flyout fills the window: 0, 0, 450 × 800 | dump, screenshot | `A_Full_Flyout_Fills_The_Window` |
| The content is 25.42 down (25.5 on the raw grid), at the left edge; its ScrollViewer 52.92 tall in the top flyout, its ScrollBar's panning root's 53 | dump: `ScrollViewer` at 25.4167, 52.9167 tall; the text at 0 | `The_Content_Sits_Below_The_Portrait_Margin_At_The_Left_Edge`, the top flyout's test |
| In landscape the content's top margin is 0 | the style's `Landscape` state | `In_Landscape_The_Content_Has_No_Top_Margin_And_The_Flyout_The_Windows_Width` |
| The chrome `#FF1F1F1F` dark, `#FFDDDDDD` light; the page under a top flyout isn't dimmed | screenshots: row 187 the chrome, row 190 the page's black or white | `The_Chrome_Is_The_Phones_And_The_Page_Under_A_Top_Flyout_Isnt_Dimmed` |
| It follows the window's size while open | the placement is the window's | `The_Flyout_Follows_The_Windows_Size_While_Open` |
| Opening, the top flyout is one band turned about its centre line, its lower edge toward the eye, from 80° at 9.31° a tick, at rest in 134 ms; the popup hidden under its bands | #149: the recording's distinct angles, linear, one step a tick | `A_Top_Flyout_Opens_As_One_Band_From_80_Degrees_At_9_31_A_Tick`, `The_Bands_Are_Drawn_Over_The_Hidden_Popup` |
| One camera for the screen, 999 px in front over y 400 | #149 | `A_Band_Turns_About_Its_Centre_Line_Its_Lower_Edge_Toward_One_Camera_Over_The_Window` |
| The full flyout opens in 100 px bands, all from 80°, at 12.61, 9.57 and 7.58° a tick for bands 0 to 2, none below 400 px turning, at rest within 208 to 248 ms | #149 | `A_Full_Flyout_Opens_In_100_Px_Bands_All_From_80_Degrees_The_Lower_Ones_Slower`, `A_Full_Flyout_Is_At_Rest_Between_208_And_248_Ms` |
| Closing, the top flyout doesn't turn: gone in one frame 98 to 122 ms after `Hide()`, closed 9 ticks after | #149 | `A_Top_Flyout_Closing_Doesnt_Turn_Goes_In_One_Frame_And_Is_Closed_9_Ticks_After`, `A_Top_Flyout_Closed_Mid_Opening_Rests_Until_It_Goes`, `Setting_IsOpen_False_Closes_It_The_Same_Way` |
| The full flyout closes band by band, band k from 2k ticks, the lower edge back at 10.95° a tick to 90°, closed 22 ticks after; mid-opening, from where each band was | #149 | `A_Full_Flyout_Closes_Band_By_Band_Two_Ticks_Apart_At_10_95_A_Tick_And_Is_Closed_22_Ticks_After`, `A_Full_Flyout_Closed_Mid_Opening_Turns_Away_From_Where_Each_Band_Was` |
| A tap below a top flyout closes it (PENDING #150) | Avalonia's light dismiss, WinRT's documented flyout | `A_Tap_Below_A_Top_Flyout_Closes_It` |
| Back closes the topmost popup before the page's `NavigationPage` sees it, and only its own window's (PENDING #150) | WinRT's documented hardware Back; the order proved against Avalonia's `PageNavigationHost` | `Back_Closes_The_Flyout_Before_The_NavigationPage_Goes_Back`, `Back_With_No_Popup_Open_Is_Left_Alone`, `Back_Takes_Only_Its_Own_Windows_Popup` |
| Opened again, or by `IsOpen`, it is placed and moves the same way | Avalonia opens by `ShowAt` or `IsOpen` | `It_Opens_Again_Placed_And_Moving`, `Opened_By_IsOpen_It_Is_Placed_From_Its_Own_Options_Too` |
| No flyout dims; a host given a dim brush covers the window under its popup while open | the phone's MenuFlyout and ContentDialog open a full-window Rectangle popup under theirs; the Flyout opens none | `A_Flyout_Doesnt_Dim`, `A_Host_With_A_Dim_Brush_Dims_The_Window_Under_Its_Popup_While_Open` |
| Under Simple alone the flyout is Avalonia's own | the theme turns the behaviour on | `Simple_Alone_Keeps_Avalonias_Own_Flyout` |

## The visual states

| Group | State | Keys | Here |
|---|---|---|---|
| `OrientationStates` | `Portrait` | `FlyoutContentPanelPortraitThemeMargin` | the rest look |
| `OrientationStates` | `Landscape` | `FlyoutContentPanelLandscapeThemeMargin` | `PhonePopupHost.IsLandscape` on the presenter |

## Avalonia's Flyout against the phone's

| Difference | Rung | What we do |
|---|---|---|
| Avalonia's popup sits beside its target (`Placement`, below by default); the phone's at the window's top or filling it | 2 | `PhonePopupHost` anchors the popup to the window's top left, inside the safe area, the window's width (and height for Full), whatever `Placement` says |
| Avalonia's popup is a window of its own on desktop (and headless); the phone's is in its one window | 2 | the host sets `Popup.ShouldUseOverlayLayer` |
| `PlacementMode` has no Full | 2 | `FlyoutOptions.IsFullScreen`, Cornflower's own attached property (an extension) |
| Avalonia's popup opens and closes at once | 2 | the host's bands on `CornflowerClock` (a picture of the popup, cut and turned, over the popup hidden); `PhoneFlyout` holds the flyout's `Closing` until the closing motion ends |
| Nothing in Avalonia closes a flyout on Back | 2 | a class handler on `TopLevel.BackRequestedEvent` closes the window's topmost host and marks the request handled |
| `Flyout` isn't a styled element: a theme can't reach it | 2 | the presenter's theme turns `PhoneFlyout` on, which finds its popup (its parent) and its flyout (as it opens) |
| The phone's presenter is in a root of its own: its text is XAML's default 11 px "Global User Interface" | — | deliberate: ours inherits the page's text, as Avalonia's popups do; the chrome's `Foreground` is `PhoneForegroundBrush` |
| The phone rounds the ScrollBar's 53 to the nearest raw pixel (127), Avalonia's layout up (128): 189 raw rows, not 188 | — | ADR 0003's recorded deviation |

## Extensions

- `FlyoutOptions.IsFullScreen` (attached, on `Flyout`, `Cornflower.Controls`): WinRT's `FlyoutPlacementMode.Full`. A
  bool, not an enum, as on the phone only Full differs from the rest; named apart from Avalonia's own `Placement`.

## What the popups share

The pieces later popups copy (ContentDialog #157, ListPickerFlyout and PickerFlyout #153, MenuFlyout #152, the date and
time pickers #156), all internal, in `src/Cornflower.Themes.Wp81/`; the README's "What the popups share" says how:

- `PhonePopupHost`: hosting in the window's overlay layer, `Placement` (Top, Full), `Dim`, the open and close motion,
  `Dismiss()`, Back, `IsLandscape`.
- `PhonePopupMotion`: the bands, their turns and times, as #149 read them; `PhonePopupBands` draws them.
- `PhonePopupDismissal`: the rules, the one place for #150's answers.
- `PhoneFlyout`: the glue for an Avalonia flyout, whose `Closing` it holds until the motion ends.

## What waits on #150

- **#150**, into `PhonePopupDismissal`: whether Back closes a top and a full Flyout; whether a tap outside a top Flyout
  closes it, and whether the tap also reaches the page; the order of `Opening`, `Opened`, `Closing` and `Closed`.
- **Asked of the reference** (not blocking): how the full flyout's bands below 400 px come in as it opens, and the
  bands' height, which the stills put at about 111 px (COMPARISON.md).

## Dependencies

- Wp81's `ScrollViewer` theme (#116), which the template's `ScrollViewer` takes.
- `CornflowerClock` and `PhoneEase` (in `Cornflower.Controls`, visible to the theme).
- No capture is still needed for what is built; the dismissal and the two readings above are.

## The checklists

Generated on 2026-11-16 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with its
reason.

### Flyout: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist Flyout <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.Flyout`; Avalonia `Avalonia.Controls.Flyout`.

### Properties (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `FlyoutPresenterStyle` | `Style` | none | Avalonia's `Flyout.FlyoutPresenterTheme` (a `ControlTheme`) is the same hook; the theme keys the presenter by type |
| [x] | `Content` | `UIElement` | `Flyout.Content` | Avalonia's stands |

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `FlyoutBase` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Placement` | `FlyoutPlacementMode` | `PopupFlyoutBase.Placement` | The phone draws Top, Bottom, Left and Right at the window's top; Avalonia's `PlacementMode` is ignored under Wp81. Full is the extension `FlyoutOptions.IsFullScreen` |

### Events, from `FlyoutBase` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Closed` | `EventHandler<Object>` | `FlyoutBase.Closed` | Avalonia's; raised when the closing motion ends |
| [x] | `Opened` | `EventHandler<Object>` | `FlyoutBase.Opened` | Avalonia's |
| [x] | `Opening` | `EventHandler<Object>` | `PopupFlyoutBase.Opening` | Avalonia's (PENDING #150: the phone's order) |

### Methods, from `FlyoutBase` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ShowAt(FrameworkElement placementTarget)` | `PopupFlyoutBase.ShowAt()` | Avalonia's; placed by the theme, not at the target |
| [x] | `void Hide()` | `PopupFlyoutBase.Hide()` | Avalonia's; held until the closing motion ends |
| [x] | `Control CreatePresenter()` (protected, overridable) | `Flyout.CreatePresenter()` | Avalonia's |

### Properties, from `DependencyObject` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `Dispatcher` (read-only) (not a dependency property) | `CoreDispatcher` | `AvaloniaObject.Dispatcher` | not applicable: Avalonia's threading |

### Events, from `DependencyObject` (0)

_none_

### Methods, from `DependencyObject` (5)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object GetValue(DependencyProperty dp)` | `AvaloniaObject.GetValue()` | Avalonia's |
| [x] | `void SetValue(DependencyProperty dp, Object value)` | `AvaloniaObject.SetValue()` | Avalonia's |
| [x] | `void ClearValue(DependencyProperty dp)` | `AvaloniaObject.ClearValue()` | Avalonia's |
| [-] | `Object ReadLocalValue(DependencyProperty dp)` | none | not applicable: Avalonia's property system |
| [-] | `Object GetAnimationBaseValue(DependencyProperty dp)` | none | not applicable: Avalonia's property system |

### Default style

_`generic.xaml` has no default style for `Flyout`._


### FlyoutPresenter: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist FlyoutPresenter <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.FlyoutPresenter`; Avalonia `Avalonia.Controls.FlyoutPresenter`.

### Properties (0)

_none_

### Events (0)

_none_

### Methods (0)

_none_

### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | left out: the phone's flyout sets none; Avalonia has no ContentTransitions |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | not applicable: Avalonia's `ContentTemplate` is an `IDataTemplate`, which selects |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Avalonia's; bound in the template |
| [x] | `Content` | `Object` | `ContentControl.Content` | Avalonia's; bound in the template |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | not applicable: Avalonia's presenter has `Child` |

### Events, from `ContentControl` (0)

_none_

### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not applicable: Avalonia's property change |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | not applicable: Avalonia's property change |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | not applicable: no selector |

### Default style: setters (9)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HorizontalContentAlignment` | `Stretch` | `ContentControl.HorizontalContentAlignment` | Themed: Stretch |
| [x] | `VerticalContentAlignment` | `Stretch` | `ContentControl.VerticalContentAlignment` | Themed: Stretch |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | Themed: False |
| [x] | `Background` | `{ThemeResource FlyoutBackgroundThemeBrush}` | `TemplatedControl.Background` | Themed: `FlyoutBackgroundThemeBrush` |
| [x] | `Padding` | `0` | `TemplatedControl.Padding` | Themed: 0, the content presenter's margin |
| [x] | `MinHeight` | `{ThemeResource FlyoutThemeMinHeight}` | `Layoutable.MinHeight` | Themed: `FlyoutThemeMinHeight` |
| [-] | `ScrollViewer.HorizontalScrollMode` | `Auto` | none | not applicable: Avalonia has no scroll modes; horizontal is `Disabled` by `HorizontalScrollBarVisibility`, as the dump shows the bar collapsed |
| [x] | `ScrollViewer.VerticalScrollMode` | `Auto` | none | Avalonia has no scroll modes: `VerticalScrollBarVisibility` Visible, as the dump shows its bar |
| [-] | `ScrollViewer.ZoomMode` | `Disabled` | none | not applicable: Avalonia's ScrollViewer doesn't zoom |

### Template parts (2)

Avalonia's control looks for: `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Border` | none | Themed: the template's root, named `Background` |
| [x] | `ScrollViewer` | `ScrollViewer` | none | Themed: named `ScrollViewer`, in Wp81's ScrollViewer theme; the presenter inside is `PART_ContentPresenter`, as Avalonia's looks for |

### Visual states (2)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `OrientationStates` | `Portrait` | nothing | no rule | The rest look |
| [x] | `OrientationStates` | `Landscape` | `ScrollViewer.Margin = {ThemeResource FlyoutContentPanelLandscapeThemeMargin}` | no rule | `PhonePopupHost.IsLandscape` on the presenter, set while the window is wider than tall |

### Transitions (0)

_none_

### Resources (4)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `FlyoutBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FF1F1F1F` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FFDDDDDD` | yes | Used |
| [x] | `FlyoutThemeMinHeight` | `43` | same | yes | Used |
| [x] | `FlyoutContentPanelLandscapeThemeMargin` | `0,0,0,0` | same | yes | Used |
| [x] | `FlyoutContentPanelPortraitThemeMargin` | `0,25.5,0,0` | same | yes | Used |

