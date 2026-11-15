# CommandBar, AppBarButton, AppBarToggleButton and AppBarSeparator: plan

[#118](https://github.com/wdcossey/CornflowerUI/issues/118). The 8.1 page's app bar. WinRT's `CommandBar`,
`AppBarButton`, `AppBarToggleButton` and `AppBarSeparator` are Avalonia 12's `CommandBar`, `CommandBarButton`,
`CommandBarToggleButton` and `CommandBarSeparator`, new in 12.0. They are themed (rung 1), with one attached property
Avalonia lacks, WinRT's `ClosedDisplayMode` (rung 2). No type is substituted.

## Where the values come from

On the phone, a `CommandBar` set as a page's `BottomAppBar` isn't drawn by its template. The shell draws it
([`tools/refapp/README.md`](../../../tools/refapp/README.md), "The page's app bar"; #59): the template's tree is dumped
open or closed alike, and its bar starts at 742.5 where the drawn one starts at 737.08. So the reference for the bar
is the shell's screenshots:

- **The shell's screenshots**, refapp runs `20261109-2124-dev11` and `20261109-2130-dev11` (`-Popups only -Only
  BottomAppBar -Grab`), the same to the pixel row: a page with a `CommandBar` as its `BottomAppBar`, three primary
  commands (add; favorite, a checked `AppBarToggleButton`; delete, disabled) and two secondary ones ("first menu
  item", "second menu item"), compact and minimal, closed and open, dark and light, 1080 × 1920 on the reference
  profile (device 11, 2.4). Committed in [`ref/wp81/CommandBar/`](../../../ref/wp81/CommandBar/README.md). Every size
  below is read from them, in raw pixels and in view pixels (raw / 2.4).
- **The API's reading**, run `20261115-2050-dev11` (`-Native`, #114): the control's `ActualHeight` 57.5 compact
  and 26.25 minimal, at 742.5 and 773.75, and `VisibleBounds` ending at 737.08 and 773.75; opening changes none
  of them.
- **The phone's own templates**, the 8.1 Update 1 runtime's `commandbar.json`, `appbarbutton.json` and
  `appbartogglebutton.json` (`R:\cornflower-reference\flash-update1\xbf-json\`), equal to the SDK's: the keys of
  each state, and what an `AppBarButton` looks like off the shell's bar. The reference app's captures of an
  `AppBarButton` and `AppBarToggleButton` on a page (run `20261108-1443-dev11`, 12 and 36 PNGs) are in
  [`ref/wp81/AppBarButton/`](../../../ref/wp81/AppBarButton/README.md) and
  [`ref/wp81/AppBarToggleButton/`](../../../ref/wp81/AppBarToggleButton/README.md). They give the pressed state,
  which the shell's screenshots don't show.
- **The pickers' bars**, run `20261108-2254-dev11` (DatePickerFlyout, TimePickerFlyout): the shell's bar with two
  commands, a second reading of where the commands sit.
- **Avalonia 12.1.4's source** (`src/Avalonia.Controls/CommandBar/`, Simple's `CommandBar.xaml`), read on GitHub at
  the tag: what its control does and which parts it looks for.

## The shell's bar against the phone's template

| | The template (`appbarbutton.json`, the page's captures) | The shell's bar (the screenshots) |
|---|---|---|
| The bar | `MaxHeight` 57.5, a `Grid` and two `ItemsControl`s | 62.92 tall closed compact (151 raw px), 26.25 minimal (63), open as tall as it shows |
| A command | 80 wide, 48 compact | a slot 73.75 wide (177 raw), the same open and closed |
| The circle | `Ellipse` 38.5, its ring 2.5 inside it (92 raw px across) | 97.8 raw px across (40.75), its ring 6.44 raw (2.68) |
| The label | `FontSize` 9.5, Normal, `AppBarItemForegroundThemeBrush` | an em of about 28 raw px (11.67), SemiBold, the bar's `CommandBarForegroundThemeBrush` (#FF1D1D1D on the light bar) |
| The labels show | `FullSize`; `Compact` hides them | while the bar is open |
| The icon | the page's colour when pressed or checked (`PhoneBackgroundColor`) | the bar's colour, checked; no pressed command in the screenshots |
| Disabled | `AppBarItemDisabledForegroundThemeBrush`, `PhoneDisabledColor`, 0x66 | the whole command at 0x73 (0.451): ring #FF848484 dark, #FF797979 light; icon and label #FF848484 and #FF868686 |
| The glyph | Segoe UI Symbol at 19 (the plus inks 45 raw px) | larger: the plus inks 51 raw px |

The bar is themed as the shell draws it: Avalonia's `CommandBarButton` is a command on a `CommandBar`, which is the
page's bar. A command off a bar gets the same look, where the phone's would get its template's (a deliberate
difference, in the README's known gaps).

## The bar

Read from the screenshots, raw pixels from the bar's top row, in view pixels after the slash.

| | Compact, closed | Minimal, closed | Open, compact or minimal |
|---|---|---|---|
| Top row | 1769 / 737.0833 | 1857 / 773.75 | 1235 / 514.5833 |
| Height | 151 / 62.9167 | 63 / 26.25 | 685 / 285.4167, with these commands |
| Background | `#FF1F1F1F` dark, `#FFDDDDDD` light: `CommandBarBackgroundThemeBrush` (`PhoneChromeColor`) | the same | the same |
| Commands | the circles | none | the circles, their labels under them, the secondary commands under those |
| Ellipsis | dots' tops 25 / 10.49 down, the last dot's right 49.4 / 20.59 in (`Wp81.Glyph.AppBarEllipsis`, #63) | the same | the same |

- **The circles** (bounding boxes 312..409, 491..584 (checked, dark; 489..586 light), 666..763, rows 1795..1892 on
  the compact bar): centres 361, 538 and 715, 177 apart. The middle one is 2 raw px left of the screen's centre
  (540); with the pickers' two commands, 449.5 and 626.5, their middle 538.25. So the commands sit in 73.75 slots,
  the group centred 4 raw px short of the bar's right: `PART_PrimaryCommands`, centred, `Margin` right 1.6666.
- **The ring**: its edge's coverage puts the outer top at 1794.08, 25.08 below the bar's top (10.45), the circle
  97.8 across (40.75) and the ring 6.44 wide (2.68). The checked circle is filled: 98 across in light, 94 in dark (its
  outer 2 raw px are the bar's colour in dark; ours is 98 in both).
- **The labels**, open: baseline at row 1406, 171 below the top (71.25); x-height 14 raw px and the `d`'s stem 20.75,
  an em of about 28 raw px (11.67); a stem 3.25 raw px wide (0.116 em), SemiBold; centred under the circle.
- **The secondary commands**, open: baselines at 1537 and 1679, 302 and 444 below the top (125.83 and 185), 142
  apart (59.1667); x-height 33.5 raw px, an em of 67 (27.92); stems 4 raw px (0.06 em), SemiLight; the first
  letter's ink from x 51 (21.25). Then 100 view px of bar under the last.
- **Opening** moves the bar's top up and nothing in it: the circles and the dots are the same distance below the top,
  compact and minimal alike.

What the bar is in Avalonia's layout, against the phone's: the phone's control is 57.5 tall at 742.5 and the view
reserves 62.92 (`VisibleBounds`); the shell draws 62.92. Ours is the drawing: the control is 62.9166 tall (151 raw
px), so a page that keeps the closed bar's height free at its foot keeps the phone's 62.92, and `Bounds` says
62.9166 where the phone's `ActualHeight` says 57.5 (a deliberate difference). Open, the phone's control stays 57.5
and the shell draws over the page; ours grows to what it shows, upwards from its bottom (`VerticalAlignment`
Bottom), and covers the page if the page lays it over its content.

## The commands

| | Value | Source |
|---|---|---|
| Slot | 73.75 wide (177 raw) | the circles' pitch |
| Circle | 40.75 across, ring 2.68, top 10.45 below the bar's top | the ring's coverage |
| Ring | `AppBarItemForegroundThemeBrush`: `#FFFFFFFF` dark, `#FF000000` light | the screenshots: 255 and 0 |
| Icon and label | `CommandBarForegroundThemeBrush` (`PhoneTextHighColor`): white dark, `#DE000000` light | the screenshots: 255, and 29 over 221 |
| Label | 11.67, SemiBold, `PhoneFontFamilyNormal`, centred, baseline 71.25 below the bar's top | the labels' ink |
| In the list | `PhoneFontFamilySemiLight` 27.92, a line 59.1666 tall, the text 20.8333 from the left | the list's ink |
| Pressed | the circle filled with the ring's colour, the icon in the bar's colour | the template's `Pressed` and the shell's checked icon |
| Checked | the circle filled (`AppBarToggleButtonChecked*`), the icon in the bar's colour | the screenshots: the star is the bar's colour, 31 dark and 221 light |
| Checked and pressed | the ring alone | the template's `CheckedPressed` |
| Disabled | the whole command at 0.451, drawn as one layer | the screenshots: 132 over 31 in dark, 121 and 134 over 221 in light |

The icons are Lucide's, by `tools/lucide/symbols.tsv` (Add: `Plus`, Favorite: `Star`, Delete: `Trash`, Pin: `Pin`),
at `LucideIcon`'s own 20 × 20. The shell's glyphs are Segoe UI Symbol's, larger than its template's (the plus 51 raw
px where the template's is 45): Lucide's shapes and sizes are its own (ADR 0001, 3).

## Behaviours

| Behaviour | Evidence | Test |
|---|---|---|
| The closed compact bar: 62.9166 tall at the foot, the circles and the dots where the shell draws them | the screenshots | `The_Compact_Bar_Is_The_Shells` (dark, light) |
| The minimal bar: 26.25, the dots alone | the screenshots | `The_Minimal_Bar_Is_The_Shells` |
| The open bar: as tall as the shell's with these commands, the labels' and the list's baselines | the screenshots | `The_Open_Bar_Is_The_Shells` (compact dark and light, minimal), `The_Secondary_Commands_Ink_Starts_Where_The_Shells_Does` |
| The ellipsis opens and closes the bar; closed, the labels go | the screenshots; Avalonia's overflow button | `The_Ellipsis_Opens_The_Bar_And_Closes_It` |
| The ellipsis shows with no secondary commands (Avalonia's `Auto` hides it) | the shell always draws it | `The_Ellipsis_Shows_Without_Secondary_Commands` |
| A press outside the open bar closes it, and doesn't reach the page | Avalonia's light dismiss | `A_Press_Outside_The_Open_Bar_Closes_It` |
| A sticky bar stays open | WinRT's and Avalonia's `IsSticky` | `A_Sticky_Bar_Stays_Open` |
| The open bar takes its own presses | Avalonia's `OverlayInputPassThroughElement` | `The_Open_Bar_Takes_Its_Own_Presses` |
| The phone's keys, dark and light | the template, the screenshots | `The_Bar_Takes_The_Phones_Keys` |
| Pressed, checked, checked and pressed | the template's states, the shell's checked icon | `Pressed_Fills_The_Circle_...`, `Checked_Fills_The_Circle_...` |
| Disabled at 0.451, as one layer | the screenshots | `Disabled_Dims_The_Whole_Command_As_One_Layer`, and the ring's pixels in `The_Compact_Bar_Is_The_Shells` |
| An app's `DefaultLabelPosition` shows the labels closed too | Avalonia's API | `The_Labels_Follow_DefaultLabelPosition_When_An_App_Sets_It` |
| Every type has its theme; the separator draws nothing and takes no room | the phone has no `AppBarSeparator` style | `Every_Type_Has_The_Phones_Template` |
| `ClosedDisplayMode` is Compact unless set | WinRT's default | `ClosedDisplayMode_Is_Compact_Unless_Set` |

The motion of the bar opening and closing isn't measured (no recording reads it), so the bar opens at once.

## Visual states and the keys that drive them

`CommandBar` has none. `AppBarButton`: `ApplicationViewStates` (`FullSize`, `Compact`) and `CommonStates` (`Normal`,
`Pressed`, `Disabled`); `AppBarToggleButton` adds `Checked`, `CheckedPressed` and `CheckedDisabled`. Each is in the
checklists below with what it sets and what ours does.

## Avalonia's control against the phone's

| Difference | Rung | What ours does |
|---|---|---|
| The open bar: Avalonia's opens a popup below the overflow button with the secondary commands; the phone's bar grows upwards and shows the labels | 1, theme | The bar's template draws the open bar in place, `PART_OverflowPresenter` under the commands; the bar's `Height` is unset while `IsOpen` |
| The labels show only while open | 1, theme | `DefaultLabelPosition` Collapsed closed and Bottom open, which Avalonia's bar turns into each command's `IsCompact` |
| The ellipsis always shows | 1, theme | `OverflowButtonVisibility` Visible |
| Light dismiss | 1, theme | `PART_OverflowPopup` holds nothing; its light dismiss is Avalonia's, with the bar as `OverlayInputPassThroughElement` |
| `ClosedDisplayMode` | 2, attached property | `CommandBarOptions.ClosedDisplayMode`, data only; the theme reads it |
| `AppBarSeparator` has no style | 1, theme | A `ControlTheme` with no template |
| The commands' `ItemsControl`s and the ellipsis's `Button` | 1, theme | Keyed themes of their own (`Wp81CommandBarItemsTheme`, `Wp81CommandBarEllipsisButtonTheme`), as Wp81 has no `ItemsControl` theme and its `Button` is the phone's |

## Extensions

`CommandBarOptions.ClosedDisplayMode` (`Cornflower.Controls`), of WinRT's `AppBarClosedDisplayMode` (Compact 0,
Minimal 1, the 8.1 members): WinRT's `AppBar.ClosedDisplayMode`, which Avalonia's `CommandBar` lacks. It shadows no
name: the class is named for what it holds, as `ControlHeader` is.

## Dependencies

`LucideIcon` and `tools/lucide/symbols.tsv` (#64) for the icons; `Wp81.Glyph.AppBarEllipsis` (#63). The page that
lays the bar over its content is #120's (README, "On a page").

Reads still missing (no state the shell's screenshots show, nothing a probe has read):

- The open bar with other numbers of secondary commands (none, one, more than fit): whether its foot stays 100 view
  px, and whether the list scrolls. Ours keeps the foot and doesn't scroll.
- A pressed command and a pressed secondary command on the shell's bar.
- Whether a press on the minimal bar, away from the ellipsis, opens it; whether invoking a command closes the bar.
- The open and close motion.
- The bar in landscape.

## The checklists

Generated on 2026-11-15 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason. The generator matches Avalonia's type by name, so `AppBarButton`, `AppBarToggleButton` and
`AppBarSeparator` read "none" in its Avalonia column; they are Avalonia's `CommandBarButton`,
`CommandBarToggleButton` and `CommandBarSeparator`, and each Action says which member.

### CommandBar: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist CommandBar <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.CommandBar`; Avalonia `Avalonia.Controls.CommandBar`.

#### Properties (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `PrimaryCommands` (read-only) | `IObservableVector<ICommandBarElement>` | `CommandBar.PrimaryCommands` | Avalonia's stands: `PART_PrimaryCommands` shows the visible ones in 73.75 slots, centred |
| [x] | `SecondaryCommands` (read-only) | `IObservableVector<ICommandBarElement>` | `CommandBar.SecondaryCommands` | Avalonia's stands: `PART_OverflowPresenter` shows them, open, as a list of words |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `AppBar` (3)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ClosedDisplayMode` | `AppBarClosedDisplayMode` | none | Extension, rung 2: `CommandBarOptions.ClosedDisplayMode` (attached, `Cornflower.Controls`), `AppBarClosedDisplayMode` Compact or Minimal; minimal and closed, the bar is 26.25 with the ellipsis alone |
| [x] | `IsSticky` | `Boolean` | `CommandBar.IsSticky` | Avalonia's stands: it turns the light dismiss off |
| [x] | `IsOpen` | `Boolean` | `CommandBar.IsOpen` | Avalonia's stands: open, the bar shows the labels and the secondary commands, drawn in place, growing upwards; a press outside closes it |

#### Events, from `AppBar` (2)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Closed` | `EventHandler<Object>` | `CommandBar.Closed` | Avalonia's `Closed` (routed) |
| [x] | `Opened` | `EventHandler<Object>` | `CommandBar.Opened` | Avalonia's `Opened` (routed) |

#### Methods, from `AppBar` (2)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnClosed(Object e)` (protected, overridable) | none | Avalonia has no such virtual; `Closed` stands |
| [-] | `void OnOpened(Object e)` (protected, overridable) | none | Avalonia has no such virtual; `Opened` stands |

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | No equivalent property (mapping rules, "Theme animations") |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Avalonia has no selector; an app's DataTemplates choose |
| [-] | `ContentTemplate` | `DataTemplate` | none | Avalonia's CommandBar has none |
| [x] | `Content` | `Object` | `CommandBar.Content` | Avalonia's stands: shown at the bar's left, closed compact or open. The phone's template shows none |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not a dependency property; Avalonia has none |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Avalonia has no such virtual; a property change handler does it |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | Avalonia has no such virtual |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | Avalonia has no such virtual |

#### Default style: setters (7)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource CommandBarForegroundThemeBrush}` | `TemplatedControl.Foreground` | `CommandBarForegroundThemeBrush`: the dots; the commands set it too, for their icons and words |
| [x] | `Background` | `{ThemeResource CommandBarBackgroundThemeBrush}` | `TemplatedControl.Background` | `CommandBarBackgroundThemeBrush`, on `LayoutRoot` |
| [x] | `BorderBrush` | `{ThemeResource AppBarBorderThemeBrush}` | `TemplatedControl.BorderBrush` | `AppBarBorderThemeBrush`, set and not drawn, as on the phone |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | `False` |
| [-] | `HorizontalContentAlignment` | `Stretch` | none | Avalonia's CommandBar has none; the phone's template doesn't read it |
| [-] | `VerticalContentAlignment` | `Stretch` | none | Avalonia's CommandBar has none; the phone's template doesn't read it |
| [-] | `MaxHeight` | `57.5` | `Layoutable.MaxHeight` | The shell draws the page's bar, not this template: 62.9166 closed compact, 26.25 minimal, and as tall as what it shows open ("The bar", above) |

#### Template parts (3)

Avalonia's control looks for: `PART_OverflowButton`, `PART_OverflowPopup`, `PART_OverflowPresenter`, `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `Grid` | none | `LayoutRoot`, a `Grid` with the bar's background, its rows the commands and the list |
| [x] | `SecondaryItemsControl` | `ItemsControl` | none | `PART_OverflowPresenter`, Avalonia's part, under the commands, shown open |
| [x] | `PrimaryItemsControl` | `ItemsControl` | none | `PART_PrimaryCommands`, centred, 4 raw px short of the right |

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (3)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `CommandBarForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextHighColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneTextHighColor} → #DE000000` | yes | By its key |
| [x] | `CommandBarBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FF1F1F1F` | `SolidColorBrush {ThemeResource PhoneChromeColor} → #FFDDDDDD` | yes | By its key |
| [x] | `AppBarBorderThemeBrush` | `SolidColorBrush {ThemeResource PhonePageBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhonePageBackgroundColor} → #FFFFFFFF` | yes | By its key |



### AppBarButton: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist AppBarButton <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.AppBarButton`; Avalonia none: a new control (rung 5).

#### Properties (3)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsCompact` | `Boolean` | none | Avalonia's `IsCompact`, which the bar sets from `DefaultLabelPosition`: Collapsed closed, Bottom open, so the label shows while the bar is open |
| [x] | `Label` | `String` | none | Avalonia's `Label`: under the circle, open; the list's words in the overflow |
| [x] | `Icon` | `IconElement` | none | Avalonia's `Icon` (an object): a `cf:LucideIcon` for a WinRT `Symbol`, by `tools/lucide/symbols.tsv` |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `Button` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Flyout` | `FlyoutBase` | none | Avalonia's `Button.Flyout` |

#### Events, from `Button` (0)

_none_

#### Methods, from `Button` (0)

_none_

#### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | none | Avalonia's |
| [x] | `Command` | `ICommand` | none | Avalonia's |
| [x] | `ClickMode` | `ClickMode` | none | Avalonia's |
| [x] | `IsPointerOver` (read-only) | `Boolean` | none | Avalonia's; no state, as on the phone |
| [x] | `IsPressed` (read-only) | `Boolean` | none | Avalonia's: `:pressed` |

#### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | none | Avalonia's |

#### Methods, from `ButtonBase` (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | No equivalent property (mapping rules, "Theme animations") |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Avalonia has no selector; an app's DataTemplates choose |
| [x] | `ContentTemplate` | `DataTemplate` | none | Avalonia's; not shown, as the phone's template shows no content |
| [x] | `Content` | `Object` | none | Avalonia's; not shown, as the phone's template shows no content |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not a dependency property; Avalonia has none |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Avalonia has no such virtual; a property change handler does it |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | Avalonia has no such virtual |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | Avalonia has no such virtual |

#### Default style: setters (5)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource AppBarItemForegroundThemeBrush}` | none | `CommandBarForegroundThemeBrush`, the icon's and the label's colour as the shell draws them (#FF1D1D1D on the light bar); the ring is `AppBarItemForegroundThemeBrush`, in the template |
| [x] | `VerticalAlignment` | `Top` | none | `Top` |
| [x] | `HorizontalAlignment` | `Left` | none | `Left`; `Stretch` in the list |
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | none | `PhoneFontFamilyNormal` (Selawik, as `ContentControlThemeFontFamily` is); the list's words `PhoneFontFamilySemiLight` |
| [x] | `FontWeight` | `Normal` | none | Not set: the label sets SemiBold, as the shell draws it (a stem 0.116 em wide) |

#### Template parts (5)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `StackPanel` | `PART_LayoutRoot` | `PART_LayoutRoot`, inside `PART_Root` and `PART_Ink`, which dims as one layer |
| [x] | `Ellipse` | `Ellipse` | `PART_Ellipse` | `PART_Ellipse`: 40.75 across, its ring 2.68, the shell's |
| [x] | `ContentRoot` | `Grid` | `PART_ContentRoot` | `PART_ContentRoot`, 10.45 below the bar's top |
| [x] | `Content` | `ContentPresenter` | `PART_Content` | Avalonia's; not shown, as the phone's template shows no content |
| [x] | `TextLabel` | `TextBlock` | `PART_TextLabel` | `PART_TextLabel`: 11.67, SemiBold, its baseline 71.25 below the bar's top; and `PART_MenuText` for the list |

#### Visual states (5)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `ApplicationViewStates` | `FullSize` | nothing | `^:fullsize` | `IsCompact` false: the label shows |
| [x] | `ApplicationViewStates` | `Compact` | `TextLabel.Visibility = Collapsed`<br>`LayoutRoot.Width = 48` | `^:compact` | `^[IsCompact=True]` hides the label. The width stays 73.75: the shell's slot is the same open and closed |
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `Pressed` | `Ellipse.Stroke = {ThemeResource AppBarItemForegroundThemeBrush}`<br>`Ellipse.Fill = {ThemeResource AppBarItemForegroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarItemPressedForegroundThemeBrush}` | `^:pressed` | `^:pressed`: the circle filled with the ring's colour, the icon in the bar's (`CommandBarBackgroundThemeBrush`, the colour behind the circle, as the template's is its page's) |
| [x] | `CommonStates` | `Disabled` | `Ellipse.Stroke = {ThemeResource AppBarItemDisabledForegroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarItemDisabledForegroundThemeBrush}`<br>`TextLabel.Foreground = {ThemeResource AppBarItemDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled`: `PART_Ink` at 0.451, the shell's 0x73 (ring #FF848484 dark, #FF797979 light), not the template's 0x66 |

#### Transitions (0)

_none_

#### Resources (5)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `AppBarItemForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | The ring, and the pressed and checked fill |
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | As `PhoneFontFamilyNormal`, the same Selawik |
| [-] | `AppBarItemPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | The bar's colour instead, `CommandBarBackgroundThemeBrush`: the template's is the page's colour, behind the circle on its page |
| [-] | `AppBarItemDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | The shell dims a command to 0.451 instead (0x73, where this is 0x66: 11 levels off on the bar) |
| [x] | `AppBarItemBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | The circle's fill at rest |



### AppBarToggleButton: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist AppBarToggleButton <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.AppBarToggleButton`; Avalonia none: a new control (rung 5).

#### Properties (3)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsCompact` | `Boolean` | none | Avalonia's `IsCompact`, which the bar sets from `DefaultLabelPosition`: Collapsed closed, Bottom open, so the label shows while the bar is open |
| [x] | `Label` | `String` | none | Avalonia's `Label`: under the circle, open; the list's words in the overflow |
| [x] | `Icon` | `IconElement` | none | Avalonia's `Icon` (an object): a `cf:LucideIcon` for a WinRT `Symbol`, by `tools/lucide/symbols.tsv` |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `ToggleButton` (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsThreeState` | `Boolean` | none | Avalonia's; indeterminate looks unchecked, as the phone's has no third state |
| [x] | `IsChecked` | `Boolean?` | none | Avalonia's: `:checked` |

#### Events, from `ToggleButton` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Checked` | `RoutedEventHandler` | none | Avalonia's `IsCheckedChanged` |
| [x] | `Indeterminate` | `RoutedEventHandler` | none | Avalonia's `IsCheckedChanged` |
| [x] | `Unchecked` | `RoutedEventHandler` | none | Avalonia's `IsCheckedChanged` |

#### Methods, from `ToggleButton` (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnToggle()` (protected, overridable) | none | Avalonia's `Toggle()`, protected virtual |

#### Properties, from `ButtonBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `CommandParameter` | `Object` | none | Avalonia's |
| [x] | `Command` | `ICommand` | none | Avalonia's |
| [x] | `ClickMode` | `ClickMode` | none | Avalonia's |
| [x] | `IsPointerOver` (read-only) | `Boolean` | none | Avalonia's; no state, as on the phone |
| [x] | `IsPressed` (read-only) | `Boolean` | none | Avalonia's: `:pressed` |

#### Events, from `ButtonBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | none | Avalonia's |

#### Methods, from `ButtonBase` (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | No equivalent property (mapping rules, "Theme animations") |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Avalonia has no selector; an app's DataTemplates choose |
| [x] | `ContentTemplate` | `DataTemplate` | none | Avalonia's; not shown, as the phone's template shows no content |
| [x] | `Content` | `Object` | none | Avalonia's; not shown, as the phone's template shows no content |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not a dependency property; Avalonia has none |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Avalonia has no such virtual; a property change handler does it |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | Avalonia has no such virtual |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | Avalonia has no such virtual |

#### Default style: setters (5)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Foreground` | `{ThemeResource AppBarItemForegroundThemeBrush}` | none | `CommandBarForegroundThemeBrush`, the icon's and the label's colour as the shell draws them (#FF1D1D1D on the light bar); the ring is `AppBarItemForegroundThemeBrush`, in the template |
| [x] | `VerticalAlignment` | `Top` | none | `Top` |
| [x] | `HorizontalAlignment` | `Left` | none | `Left`; `Stretch` in the list |
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | none | `PhoneFontFamilyNormal` (Selawik, as `ContentControlThemeFontFamily` is); the list's words `PhoneFontFamilySemiLight` |
| [x] | `FontWeight` | `Normal` | none | Not set: the label sets SemiBold, as the shell draws it (a stem 0.116 em wide) |

#### Template parts (5)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `StackPanel` | `PART_LayoutRoot` | `PART_LayoutRoot`, inside `PART_Root` and `PART_Ink`, which dims as one layer |
| [x] | `Ellipse` | `Ellipse` | `PART_Ellipse` | `PART_Ellipse`: 40.75 across, its ring 2.68, the shell's |
| [x] | `ContentRoot` | `Grid` | `PART_ContentRoot` | `PART_ContentRoot`, 10.45 below the bar's top |
| [x] | `Content` | `ContentPresenter` | `PART_Content` | Avalonia's; not shown, as the phone's template shows no content |
| [x] | `TextLabel` | `TextBlock` | `PART_TextLabel` | `PART_TextLabel`: 11.67, SemiBold, its baseline 71.25 below the bar's top; and `PART_MenuText` for the list |

#### Visual states (8)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `ApplicationViewStates` | `FullSize` | nothing | `^:fullsize` | `IsCompact` false: the label shows |
| [x] | `ApplicationViewStates` | `Compact` | `TextLabel.Visibility = Collapsed`<br>`LayoutRoot.Width = 48` | `^:compact` | `^[IsCompact=True]` hides the label. The width stays 73.75: the shell's slot is the same open and closed |
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `Pressed` | `Ellipse.Stroke = {ThemeResource AppBarItemForegroundThemeBrush}`<br>`Ellipse.Fill = {ThemeResource AppBarItemForegroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarItemPressedForegroundThemeBrush}` | `^:pressed` | `^:pressed`: the circle filled with the ring's colour, the icon in the bar's (`CommandBarBackgroundThemeBrush`, the colour behind the circle, as the template's is its page's) |
| [x] | `CommonStates` | `Disabled` | `Ellipse.Stroke = {ThemeResource AppBarItemDisabledForegroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarItemDisabledForegroundThemeBrush}`<br>`TextLabel.Foreground = {ThemeResource AppBarItemDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled`: `PART_Ink` at 0.451, the shell's 0x73 (ring #FF848484 dark, #FF797979 light), not the template's 0x66 |
| [x] | `CommonStates` | `Checked` | `Ellipse.Stroke = {ThemeResource AppBarToggleButtonCheckedBorderThemeBrush}`<br>`Ellipse.Fill = {ThemeResource AppBarToggleButtonCheckedBackgroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarToggleButtonCheckedForegroundThemeBrush}` | `^:checked` | `^:checked`: the circle filled (`AppBarToggleButtonCheckedBorderThemeBrush`, `...CheckedBackgroundThemeBrush`), the icon in the bar's colour, as the shell draws it |
| [x] | `CommonStates` | `CheckedPressed` | `Ellipse.Stroke = {ThemeResource AppBarToggleButtonCheckedPressedBorderThemeBrush}`<br>`Ellipse.Fill = {ThemeResource AppBarToggleButtonCheckedPressedBackgroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarToggleButtonCheckedPressedForegroundThemeBrush}` | `^:checked:pressed` | `^:checked:pressed`: the ring alone, by the template's keys |
| [x] | `CommonStates` | `CheckedDisabled` | `Ellipse.Stroke = {ThemeResource AppBarToggleButtonCheckedDisabledBorderThemeBrush}`<br>`Ellipse.Fill = {ThemeResource AppBarToggleButtonCheckedDisabledBackgroundThemeBrush}`<br>`Content.Foreground = {ThemeResource AppBarToggleButtonCheckedDisabledForegroundThemeBrush}`<br>`TextLabel.Foreground = {ThemeResource AppBarItemDisabledForegroundThemeBrush}` | `^:checked:disabled` | `^:checked` with `^:disabled`: the checked look at 0.451, as the shell dims a command |

#### Transitions (0)

_none_

#### Resources (14)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `AppBarItemForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | The ring, and the pressed and checked fill |
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | As `PhoneFontFamilyNormal`, the same Selawik |
| [-] | `AppBarItemPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | The bar's colour instead, `CommandBarBackgroundThemeBrush`: the template's is the page's colour, behind the circle on its page |
| [-] | `AppBarItemDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | The shell dims a command to 0.451 instead (0x73, where this is 0x66: 11 levels off on the bar) |
| [x] | `AppBarToggleButtonCheckedBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By its key |
| [x] | `AppBarToggleButtonCheckedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By its key |
| [-] | `AppBarToggleButtonCheckedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | The bar's colour instead, as the shell draws the checked icon (the template's is the page's) |
| [x] | `AppBarToggleButtonCheckedPressedBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By its key |
| [x] | `AppBarToggleButtonCheckedPressedBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | By its key |
| [x] | `AppBarToggleButtonCheckedPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By its key |
| [-] | `AppBarToggleButtonCheckedDisabledBorderThemeBrush` | `SolidColorBrush Transparent` | same | yes | The checked look at 0.451 instead, as the shell dims a command |
| [-] | `AppBarToggleButtonCheckedDisabledBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | The same |
| [-] | `AppBarToggleButtonCheckedDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | The same |
| [x] | `AppBarItemBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | The circle's fill at rest |



### AppBarSeparator: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist AppBarSeparator <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.AppBarSeparator`; Avalonia none: a new control (rung 5).

#### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsCompact` | `Boolean` | none | Avalonia's `CommandBarSeparator.IsCompact`; the separator draws nothing either way |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Default style

_`generic.xaml` has no default style for `AppBarSeparator`._



