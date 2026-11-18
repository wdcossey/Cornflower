# Roadmap

Era 1 is Windows Phone 8.1. This file is the map; the GitHub issues are the work. `CLAUDE.md`
lists only what exists.

Process per issue: [`PROCESS.md`](PROCESS.md) §0. Start an issue only when its blockers are
merged. Decisions behind this plan: [ADR 0001](decisions/0001-what-faithful-means.md),
[ADR 0002](decisions/0002-decisions-of-2026-11-08.md),
[ADR 0003](decisions/0003-decisions-on-the-tier-0-infrastructure.md),
[ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md) and
[ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md).

## Scope

| | |
|---|---|
| **In, first** | Windows Phone 8.1's XAML control set: a theme for Avalonia's controls plus the controls Avalonia lacks, dark and light, with the accent colour. Mobile and desktop are both priorities: the core is the phone, at any window size, and keeps the hooks a wider layout needs ([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)) |
| **Later** | Windows Phone 8.0's Silverlight-only controls (Panorama, LongListSelector, HubTile, the toolkit's ListPicker), as an add-on to era 1. Layouts for wider windows, as a separate extension pack over the core theme, labelled as Cornflower's design, after Tier 2 ([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)). Windows 10 Mobile as a second era. Windows 10 desktop as a third. High contrast |
| **Out** | MapControl, MediaElement, CaptureElement, WebView, ink, live tiles, the phone's system UI (Start screen, status bar, keyboard). Anything that needs a Microsoft font or a copied Microsoft template |

Not in scope at all: a port of UWP or WinRT XAML (Uno Platform does that), and Windows 11 Fluent
(Avalonia's own themes and FluentAvalonia cover it).

## The inventory

Read on 2026-11-07 from the phone's `Windows.winmd` and `generic.xaml`, taken from the Windows
Phone 8.1 SDK on the reference VM. The `.winmd` was read with [`tools/winrt-api`](../tools/winrt-api/Program.cs). The full type table is in
the reference folder, as `wp81-control-types.md`.

| | Count |
|---|---|
| Public types under `Windows.UI.Xaml.Controls` and `.Primitives` in the phone `Windows.winmd` | 115 |
| Of those, concrete types deriving from `Control` | 71 |
| With a default style in the phone `generic.xaml` | 56 |
| Without one | 15: AppBar, AppBarSeparator, SettingsFlyout, ToolTip, UserControl, and the bases ButtonBase, Control, ItemsControl, ListViewBase, ListViewBaseHeaderItem, MenuFlyoutItemBase, MenuFlyoutSeparator, RangeBase, Selector, SelectorItem |
| Default (unkeyed) styles in `generic.xaml` | 58, two of them for ListViewItem |
| Panels and other `FrameworkElement`s with no template | 36, among them Border, Canvas, Grid, StackPanel, Viewbox, Image, TextBlock, RichTextBlock, the icon elements, the virtualising panels, PivotPanel, PivotHeaderPanel, CarouselPanel, LoopingSelectorPanel, TickBar, Popup, WebView, MediaElement, CaptureElement |

AppBar, AppBarSeparator, SettingsFlyout and ToolTip are in the phone's metadata with no default
style: they are Windows 8.1 desktop controls carried in the API, and there is no phone look to
reproduce.

**Completeness.** Avalonia has templated controls the phone never had. The measure of era 1's
completeness is the count from the test that lists every templated Avalonia control without a
Wp81 theme ([#13](https://github.com/wdcossey/Cornflower/issues/13);
[ADR 0002](decisions/0002-decisions-of-2026-11-08.md#themes-and-scope)): 79 at Avalonia 12.1.4,
before any control (2026-11-09). Each control's PR deletes its own lines from
`tests/Cornflower.Tests/Completeness/Holes.txt`.

## Tiers

| Tier | Meaning |
|---|---|
| **0** | Tooling and acquisition. Nothing in Tier 1 starts until this is done, except the type ramp ([#26](https://github.com/wdcossey/Cornflower/issues/26)), by [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md) |
| **1** | Foundation controls, one PR each at full depth. Button first, then ProgressRing, then the rest |
| **2** | The shell: what makes a screen look like a phone app. Page, app bar, Pivot, Hub, list |
| **3** | Pickers and dialogs |
| **4** | The rest, and the plain themes for Avalonia controls the phone never had |

Button is the worked example every later themed control copies, as `ion-item` was for Avonic.
ProgressRing is the worked example for a control we own. Tier 2 starts only when every Tier 1
control is merged with its comparison.

## Controls

The Avalonia column was checked on 2026-11-06 against `Avalonia.Controls.dll` in the Avalonia
12.1.4 reference assemblies: each named type exists, or doesn't. Cells marked "to check" were
added on 2026-11-07 without that check. The rung is the planned step of
[the ladder](ARCHITECTURE.md#the-limit-of-a-theme); each is confirmed by its control's Compare
step, and a control that climbs a rung says so in its PR.

### Tier 1

| Issue | Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|---|
| [#27](https://github.com/wdcossey/Cornflower/issues/27) | Button | Button | Theme, tilt. The dry run of the whole loop; done 2026-11-11 (#75), device run passed 2026-11-11 | 1, 2 |
| [#28](https://github.com/wdcossey/Cornflower/issues/28) | ProgressRing | none | New control; done 2026-11-11 (#88), device run passed 2026-11-12 | 2, 5 |
| [#26](https://github.com/wdcossey/Cornflower/issues/26) | TextBlock and the text styles | TextBlock | The type ramp, as keyed `ControlTheme`s under the WinRT style keys, and resources; done 2026-11-10 (#69), device run passed 2026-11-11 | 1, 2 |
| [#29](https://github.com/wdcossey/Cornflower/issues/29) | HyperlinkButton | HyperlinkButton | Theme, tilt (on its content); done 2026-11-12 (#91), device run passed 2026-11-12 | 1, 2 |
| [#30](https://github.com/wdcossey/Cornflower/issues/30) | RepeatButton, ToggleButton | the same | Theme (no tilt: neither phone template has one); the slide off and the repeat off the button (`PhonePress`); done 2026-11-12 (#93), device run passed 2026-11-12 | 1, 2 |
| [#31](https://github.com/wdcossey/Cornflower/issues/31) | CheckBox | CheckBox | Theme, glyph, tilt; done 2026-11-12 (#92), device run passed 2026-11-12 | 1, 2 |
| [#32](https://github.com/wdcossey/Cornflower/issues/32) | RadioButton | RadioButton | Theme, tilt; done 2026-11-12 (#94), device run passed 2026-11-12 | 1, 2 |
| [#33](https://github.com/wdcossey/Cornflower/issues/33) | ToggleSwitch | ToggleSwitch | Theme, `Header` by `ControlHeader`; the phone's tap and drag by a behaviour (`PhoneToggleSwitch`), not a subclass; done 2026-11-12 (#102), device run passed 2026-11-12 | 1, 2 |
| [#34](https://github.com/wdcossey/Cornflower/issues/34) | Slider | Slider | Theme; `Header` by an attached property, `ControlHeader`; done 2026-11-12 (#96), device run passed 2026-11-12 | 1, 2 |
| [#35](https://github.com/wdcossey/Cornflower/issues/35) | ProgressBar | ProgressBar | Theme. The indeterminate dots are the hard part; done 2026-11-12 (#95), device run passed 2026-11-12 | 1, 2 |
| [#36](https://github.com/wdcossey/Cornflower/issues/36) | TextBox | TextBox | Theme, `Header` (no clear button: the phone's template has none); done 2026-11-12 (#101), device run passed 2026-11-12 | 1, 2 |
| [#37](https://github.com/wdcossey/Cornflower/issues/37) | PasswordBox | TextBox with `PasswordChar` | Theme, keyed `PasswordBox`, on a `TextBox`. No separate control in Avalonia; done 2026-11-12 (#103), device run passed 2026-11-12; #104 fixed by #106: a masked box tells the keyboard it holds a password | 1, 2 |

### Tier 2

| Issue | Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|---|
| [#116](https://github.com/wdcossey/Cornflower/issues/116) | ScrollViewer, ScrollBar, Thumb | all three exist | Theme; ScrollBar is the phone's thin indicator; the indicators by `PhoneScrollIndicators` and `PhoneIndicatorFade`; the scroll physics a recorded deviation, measured by [#113](https://github.com/wdcossey/Cornflower/issues/113) (a part class is [#129](https://github.com/wdcossey/Cornflower/issues/129), an enhancement carried past Tier 2). The worked example for Tier 2: ListView, Page, Pivot and Hub hold one; done 2026-11-15 (#126), device run passed 2026-11-15 | 1, 2, 6 |
| [#117](https://github.com/wdcossey/Cornflower/issues/117) | ContentControl, ContentPresenter | both exist | Theme, by type since the Tier 2 roll-up, #139 (keyed until then); ContentPresenter keeps Avalonia's own, as the phone's style reaches no template's presenter; done 2026-11-15 (#125), device run passed 2026-11-15 | 1, 2 |
| [#118](https://github.com/wdcossey/Cornflower/issues/118) | CommandBar, AppBarButton, AppBarToggleButton, AppBarSeparator | CommandBar, CommandBarButton and the rest (new in 12.0) | Theme, after the shell's drawing of the app bar; the minimised bar by `CommandBarOptions.ClosedDisplayMode`; a command closes the open bar (`PhoneAppBarClose`). The shell's press, the open bar's stepped height and its motion (`PhoneAppBar`): [#141](https://github.com/wdcossey/Cornflower/issues/141), done 2026-11-16 (#144), device run passed 2026-11-16. Light's pressed look is [#168](https://github.com/wdcossey/Cornflower/issues/168), done 2026-11-17 (#186), device and real-window runs passed 2026-11-17. AppBarSeparator has no default style on the phone, and draws nothing; done 2026-11-15 (#133), device run passed 2026-11-15 | 1, 2 |
| [#119](https://github.com/wdcossey/Cornflower/issues/119) | ListView, ListViewItem, ListViewHeaderItem, GroupItem | ListBox, ListBoxItem; no header items (Avalonia has no grouping) | Theme; the check mode by `PhoneListMultiSelect`; ListViewHeaderItem and GroupItem deferred with grouping ([#131](https://github.com/wdcossey/Cornflower/issues/131)); `ItemClick` and `IsItemClickEnabled` by `ListBoxOptions` ([#132](https://github.com/wdcossey/Cornflower/issues/132), done 2026-11-15, #138; `Multiple` alone clicks, as Single, #145); the check box's own press, and `Extended` with no boxes ([#142](https://github.com/wdcossey/Cornflower/issues/142), done 2026-11-16, #143, device run passed 2026-11-16); item click in the check mode as the phone's, [#169](https://github.com/wdcossey/Cornflower/issues/169), done 2026-11-17 (#186), device and real-window runs passed 2026-11-17; reorder deferred; done 2026-11-15 (#130), device run passed 2026-11-15 | 1, 2 |
| [#120](https://github.com/wdcossey/Cornflower/issues/120) | Page, Frame | NavigationPage, ContentPage (new in 12.0); Frame: `NavigationPage` (Avalonia has no `Frame`) | Theme, plus our own page transitions (`TurnstileTransition`, `SlideTransition`, `ContinuumTransition`). `NavigationPage` is the navigation host; done 2026-11-15 (#134), device run passed 2026-11-16 | 1 |
| [#121](https://github.com/wdcossey/Cornflower/issues/121) | Pivot, PivotItem, PivotHeaderItem | none (`TabControl` and `Carousel` are the nearest) | New control. Its header strip scrolls with the content, which neither does. The worked example for Tier 2's new controls: the swipe, strip and slides are the control's own (`PivotMotion`, on `CornflowerClock`, read by #128 and #136), its ScrollViewer held, not scrolled; done 2026-11-16 (#140), device run passed 2026-11-16. #148's reads followed in #187 (2026-11-17): [#162](https://github.com/wdcossey/Cornflower/issues/162) where a release goes, [#163](https://github.com/wdcossey/Cornflower/issues/163) a press while it moves, [#164](https://github.com/wdcossey/Cornflower/issues/164) the item events; device and real-window runs passed 2026-11-17 | 5 |
| [#122](https://github.com/wdcossey/Cornflower/issues/122) | Hub, HubSection | none | New control, on Pivot's pieces: the pan, the snap to sections, the loop and the header's and background's parallax are the control's own (`HubMotion`, read by #128 and #136); the swipe recogniser shared with Pivot since the Tier 2 roll-up; done 2026-11-16 (#146), device run passed 2026-11-16. #148's reads followed in #188 (2026-11-17): [#165](https://github.com/wdcossey/Cornflower/issues/165) the loop backwards, [#166](https://github.com/wdcossey/Cornflower/issues/166) the entrance, [#167](https://github.com/wdcossey/Cornflower/issues/167) an image narrower than the screen; device and real-window runs passed 2026-11-17 | 5 |

The reads and tools Tier 2 needs: [#113](https://github.com/wdcossey/Cornflower/issues/113)
ScrollViewer physics and ListView press feedback; [#114](https://github.com/wdcossey/Cornflower/issues/114)
the app bar's heights, the turnstile's forward angle and Pivot's header motion;
[#115](https://github.com/wdcossey/Cornflower/issues/115) real touch, drag and wheel input in
`drive.ps1`, from Avonic. All three done on 2026-11-15 (#127, #128, #124); the reads then owed for
Pivot, Hub, ListView and CommandBar, [#136](https://github.com/wdcossey/Cornflower/issues/136), done 2026-11-16 (#137). The batches: #113,
#115, #116 and #117 first; then #114, #118, #119 and #120 (both batches done 2026-11-15); then #121 and #122 (done
2026-11-16), with #136's follow-ups for the app bar (#141) and the check mode (#142). Each issue names its own blockers;
they are the record, not this list.

**Tier 2 is complete** (2026-11-16, rolled up in the Tier 2 roll-up), but for
[#129](https://github.com/wdcossey/Cornflower/issues/129), the scroll physics as a part class: an enhancement of #116's
recorded deviation, carried forward, not a gap in the shell. Grouped lists
([#131](https://github.com/wdcossey/Cornflower/issues/131)) are Tier 4's. The runs owed at the roll-up are done, on
the sample shell on `NavigationPage`:
- **The Pixel 8** (Android 17, wireless adb, Release, `main` at `ae652d3`, 2026-11-16 22:48 to 22:50): every page ready,
  `page=411.43x840`, cold starts 716 to 826 ms. `page` opened in 308 ms, `pivot` 352, `hub` 339, `commandbar` 379 (the
  bar opens with its labels and its two menu items), `listview` 382 and `itemclick` 406. So #120's, #121's, #122's,
  #141's and #142's device runs passed.
- **The real window** (`drive.ps1`, Debug, on #161's head `d0a9ef6`, 2026-11-16): 160 of 160 checks, every page opened
  from the list, ready, and left by Back.

Tier 2's reads owed after the roll-up, [#148](https://github.com/wdcossey/Cornflower/issues/148), were done on
2026-11-16 (#170). Where the phone differs from what was built, each is an issue of its own, Tier 2's, all done on
2026-11-17:
[#162](https://github.com/wdcossey/Cornflower/issues/162), [#163](https://github.com/wdcossey/Cornflower/issues/163) and
[#164](https://github.com/wdcossey/Cornflower/issues/164) (Pivot), [#165](https://github.com/wdcossey/Cornflower/issues/165),
[#166](https://github.com/wdcossey/Cornflower/issues/166) and [#167](https://github.com/wdcossey/Cornflower/issues/167) (Hub),
[#168](https://github.com/wdcossey/Cornflower/issues/168) (CommandBar, light's press) and
[#169](https://github.com/wdcossey/Cornflower/issues/169) (ItemClick in the check mode), in #187, #188 and #186. Their Pixel 8 and real-window
runs passed on the evening of 2026-11-17 (Tier 3's paragraphs, below).

### Tier 3

| Issue | Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|---|
| [#151](https://github.com/wdcossey/Cornflower/issues/151) | Flyout, FlyoutPresenter | both exist | Theme. The worked example for Tier 3: how a popup over the page is hosted, placed, dimmed, opened, closed and dismissed (`PhonePopupHost`, shared by every popup below); done 2026-11-16 (#161), device run passed 2026-11-16 (the top flyout below the status bar at full width, the full one filling the window, Back closing each), real-window run passed 2026-11-16 (the page; drive.ps1 opens no flyout) | 1, 2 |
| [#152](https://github.com/wdcossey/Cornflower/issues/152) | MenuFlyout, MenuFlyoutItem, ToggleMenuFlyoutItem, MenuFlyoutSeparator, MenuFlyoutPresenter | MenuFlyout, MenuFlyoutPresenter; no MenuFlyoutItem or ToggleMenuFlyoutItem: a MenuFlyout holds `MenuItem`s, and `MenuItem.ToggleType` is the toggle item (checked 2026-11-16) | Theme, with the phone's item as a theme for `MenuItem` inside the presenter (`PhoneMenuFlyout`). MenuFlyoutSeparator has no default style on the phone; the presenter draws it; done 2026-11-17 (#175), device run passed 2026-11-17, real-window run passed 2026-11-17 | 1, 2 |
| [#153](https://github.com/wdcossey/Cornflower/issues/153) | ListPickerFlyout, PickerFlyout, and their presenters | none | New controls. The full-screen presenters are where the phone look lives. The pickers' bar, with our accept and cancel glyphs, is built here; done 2026-11-17 (#176), the list picker's own rows motion since the shared host's consolidation (#181); device run passed 2026-11-17, real-window run passed 2026-11-17 | 5 |
| [#154](https://github.com/wdcossey/Cornflower/issues/154) | LoopingSelector, LoopingSelectorItem (primitives) | `Primitives.DateTimePickerPanel` is the nearest: a panel, not a control with items (checked 2026-11-16) | Part of the pickers. Avalonia's panel, themed by key and driven by `PhoneLoopingSelector` out of a ScrollViewer, with the wheel's own coast (#149's fit of #113's law); not [#129](https://github.com/wdcossey/Cornflower/issues/129); done 2026-11-17 (#178), device run passed 2026-11-17, real-window run passed 2026-11-17 | 5 |
| [#157](https://github.com/wdcossey/Cornflower/issues/157) | ContentDialog | none | New control, on the Flyout's popup host; done 2026-11-17 (#177), device run passed 2026-11-17, real-window run passed 2026-11-17 | 5 |
| [#158](https://github.com/wdcossey/Cornflower/issues/158) | AutoSuggestBox | AutoCompleteBox | Theme. A different API (the plan's table); the page moving up for the keyboard is [#179](https://github.com/wdcossey/Cornflower/issues/179); done 2026-11-17 (#174), device run passed 2026-11-17 (Back on the list on the evening's run), real-window run passed 2026-11-17 | 1, 2 |
| [#179](https://github.com/wdcossey/Cornflower/issues/179) | The shell's pan: the page moves up so the focused text box shows while the keyboard is up: an AutoSuggestBox to the window's top, a TextBox or PasswordBox the least that shows its caret ([#201](https://github.com/wdcossey/Cornflower/issues/201)) | none: `InputPane.OccludedRect` reports the keyboard, and nothing moves a page that doesn't scroll | A shared behaviour for every text box (TextBox, PasswordBox, AutoSuggestBox), decided by the user on 2026-11-16 (#174 left it out); run on the Pixel 8, as only a soft keyboard shows it; done 2026-11-18 (#196), device run passed 2026-11-18, no real-window run (a desktop shows no keyboard pane) | 2 |
| [#201](https://github.com/wdcossey/Cornflower/issues/201) | Keyboard pan: the caret rule and `AutoMaximizeSuggestionArea` | none | A follow-up of #179 from #200's read: a covered TextBox or PasswordBox stops with its caret's line at the keyboard's top; an AutoSuggestBox goes to the window's top, or with `cf:AutoCompleteBoxOptions.AutoMaximizeSuggestionArea` false only until its list has one suggestion's room; done 2026-11-18 (#202), device run passed 2026-11-18, no real-window run (a desktop shows no keyboard pane) | 2 |
| [#197](https://github.com/wdcossey/Cornflower/issues/197) | Back that hides the keyboard also takes the focus from the text box | none | Seen in #179's device run: the phone's first Back hides the keyboard and takes the focus (#150); a TextBox of ours kept it. Now the keyboard going down, or a Back Avalonia sees, while a TextBox or PasswordBox of ours has the focus, takes the focus; in a ContentDialog the dialog stays for the second Back; done 2026-11-18 (#204), device run passed 2026-11-18, no real-window run (a desktop shows no keyboard pane) | 2 |
| [#206](https://github.com/wdcossey/Cornflower/issues/206) | Keyboard pan: the slide, and a covered AutoSuggestBox with `AutoMaximizeSuggestionArea` false | none | A follow-up of #179 from #205's read: the page slides, up over 300 ms easing out and down over 200 ms easing in (`CornflowerConfig.KeyboardPanSlide`, 0 or less to jump); a covered AutoSuggestBox with the switch off moves until its bottom border is at the keyboard's top, its list opening above it, and 10.42 further once the list opens; done 2026-11-18 (#207), device run passed 2026-11-18, no real-window run (a desktop shows no keyboard pane) | 2 |
| [#155](https://github.com/wdcossey/Cornflower/issues/155) | ComboBox, ComboBoxItem | ComboBox, ComboBoxItem | Theme for the inline form. The full-screen form a long list gets (6 items or more, #58) opens `ListPickerFlyout`; planned as a subclass, done as an attached behaviour on Avalonia's own ComboBox (`PhoneComboBox`), so every ComboBox has both forms and there is no new type; done 2026-11-17 (#182), device run passed 2026-11-17, real-window run passed 2026-11-17 | 1, 2 |
| [#156](https://github.com/wdcossey/Cornflower/issues/156) | DatePicker, TimePicker, DatePickerFlyout, TimePickerFlyout, DatePickerFlyoutPresenter, TimePickerFlyoutPresenter | DatePicker, TimePicker, DatePickerPresenter, TimePickerPresenter | Theme. The columns are [#154](https://github.com/wdcossey/Cornflower/issues/154)'s wheel and the bar is [#153](https://github.com/wdcossey/Cornflower/issues/153)'s. Avalonia's presenters placed full screen on the phone's popup host, so no part class; the flyouts as types came after, in [#184](https://github.com/wdcossey/Cornflower/issues/184); done 2026-11-17 (#183), device run passed 2026-11-17, real-window run passed 2026-11-17 | 1, 2 |
| [#189](https://github.com/wdcossey/Cornflower/issues/189) | Motion settings | none | Cornflower's own: `CornflowerConfig`, process-wide (`Animated`, `MotionSpeed`, a speed per area, `Deviations`), the phone's by default ([ADR 0006](decisions/0006-faithful-defaults-and-cornflowerconfig.md)); every motion on `CornflowerClock` reads it as it starts; done 2026-11-17 (#190), device run passed 2026-11-17 (the theme page, with the motion settings, opened in 435 ms), real-window run passed 2026-11-17; not recorded: the sample at speed 2 on the phone | — |
| [#184](https://github.com/wdcossey/Cornflower/issues/184) | DatePickerFlyout, TimePickerFlyout, and their presenters | none; the presenters subclass Avalonia's `DatePickerPresenter` and `TimePickerPresenter` | New controls, on #153's `PickerFlyoutBase`, shown from any element; their presenters themed as the DatePicker's and TimePicker's open pickers; done 2026-11-18 (#193), device run passed 2026-11-18, real-window run passed 2026-11-18 | 5 |

The reads Tier 3 needs: [#149](https://github.com/wdcossey/Cornflower/issues/149) LoopingSelector's physics, the pickers' row swing, the popups'
open and close as curves, and the inline ComboBox's motion; [#150](https://github.com/wdcossey/Cornflower/issues/150) AutoSuggestBox's first
captures, how each popup is dismissed, ContentDialog's layout, the pickers' bar, ListPickerFlyout
in `Multiple`, MenuFlyout's items and placement, and the states on the pickers' inner parts. Both
wait for [#148](https://github.com/wdcossey/Cornflower/issues/148), Tier 2's remaining reads, as the VM runs one emulator at a time. The batches:
#149, #150 and #151 first (#151 as a draft until #149's curves and #150's dismissal are in); then
#152, #153, #154, #157 and #158; then #155 and #156. Each issue names its own blockers and the
full list of what each read covers; they are the record, not this list.

**Tier 3 is complete** (2026-11-18, once #179 and #184 were done; first rolled up in the second Tier 3 roll-up, on
2026-11-17), #179's follow-ups included. Done:
#148, #149 and #150 on 2026-11-16 (#170, #171, #172); #151 on 2026-11-16 (#161); #152, #153, #154, #157 and #158 on
2026-11-17 (#175, #176, #178, #177, #174); #155 and #156 on 2026-11-17 (#182, #183); the shared popup host's
consolidation (#181). The page moving up for the keyboard, [#179](https://github.com/wdcossey/Cornflower/issues/179), was done on 2026-11-18 (#196), and
the flyouts #156 left out, [#184](https://github.com/wdcossey/Cornflower/issues/184), on 2026-11-18 (#193). #179's follow-up, the caret rule and
`AutoMaximizeSuggestionArea` ([#201](https://github.com/wdcossey/Cornflower/issues/201)), was done on 2026-11-18 (#202), and so were Back taking the focus
as it hides the keyboard ([#197](https://github.com/wdcossey/Cornflower/issues/197), #204) and the pan's slide with #205's AutoSuggestBox rule
([#206](https://github.com/wdcossey/Cornflower/issues/206), #207).
The motion settings, [#189](https://github.com/wdcossey/Cornflower/issues/189), were done on 2026-11-17 (#190):
`CornflowerConfig`, the phone's by default ([ADR 0006](decisions/0006-faithful-defaults-and-cornflowerconfig.md)).

The device runs owed at the second roll-up are done:
- **The Pixel 8** (Android 17, wireless adb, Release, `main` at `9a24f7f`, 2026-11-17 17:40 to 17:44): every page ready,
  `page=411.43x840`, cold starts 747 to 813 ms. `menu` opened in 318 ms, `listpicker` 317, `loopingselector` 398,
  `combobox` 355, `datetimepicker` 369, `contentdialog` 368 and `autosuggestbox` 385. Each popup was opened on the phone
  by a tap and looked right: the menu (Back closes it), the list picker single and multiple, the picker flyout, the
  ComboBox inline and full screen, the date and time pickers, the dialog normal and full size, and the suggestion list
  with 'a' typed, above the keyboard (the page doesn't move up: #179). So #152's, #153's, #154's, #155's, #156's,
  #157's and #158's device runs passed, but for Back closing #158's list, which wasn't tried.

The runs owed at the third roll-up were done on the evening of 2026-11-17, on `main` at `03082b3`:
- **The Pixel 8** (over wireless adb, 20:42 to 20:45, cold starts 757 to 879 ms): `autosuggestbox` opened in 396 ms,
  and Back on its list closed the list and the keyboard and dropped the focus, a second Back going back to the
  catalogue; `pivot` 346, `hub` 388, `commandbar` 380, `listview` 380, `itemclick` 393 and `theme` 435, with the
  motion settings. So Back on #158's list and the device runs of #162 to #169 and #189 passed.
- **The real window** (`drive.ps1`, Debug): 195 of 195, once #192 had it tap Alt before it takes the foreground. So
  the real-window runs of #152 to #158, #162 to #169 and #189 passed.

The runs of 2026-11-18:
- **#184's flyouts** (#193). The Pixel 8 (Release, over wireless adb, `main` at `70eea78`, 09:32): `datetimepicker`
  cold 881 ms, opened in 451 ms; both flyouts open full screen with the wheels and the bar, Back closes them with the
  values unchanged, no exceptions; the time flyout follows en-GB's 24-hour clock. The real window (`drive.ps1`,
  Debug, `main` at `7bb64ce`): 195 of 195, `datetimepicker` in 40 ms (layout 11.4).
- **#179's pan** (#196). The Pixel 8 (Release): at 11:51 on `a0f1a02` it found two faults, fixed in `07a0d02`; again at
  12:02 on `07a0d02`, clean: the `textbox`, `passwordbox` and `autosuggestbox` pages, every step of #196's list passed.
  No real-window run: a desktop shows no keyboard pane.
- **The phone's pan, read** (#200, 8.1 Update 1 1080p, device 11): how far the phone moves the page for a box halfway
  down and for one under the keyboard, an AutoSuggestBox with `AutoMaximizeSuggestionArea` true and false, and a TextBox
  in a ScrollViewer (the move only: no `VerticalOffset` was logged, so scroll and move aren't split). The numbers are in [`tools/refapp`](../tools/refapp/README.md), "The keyboard's pan"; the grabs in
  `ref/wp81/KeyboardPan/`. #201 came of it.
- **#201's caret rule** (#202). The Pixel 8 (Release, over wireless adb, `6bce3d4`, 13:24): every check passed. A
  covered TextBox (the light section's and the last) and the PasswordBox stop with the caret's bottom on the keyboard's
  top (the PasswordBox's Show Password row stays under the keyboard, as the phone's does, #205); the default
  AutoSuggestBox goes to the window's top; the one with `AutoMaximizeSuggestionArea` false moves only until one
  suggestion fits, the list ending at the keyboard's top; the status bar stays clean. No real-window run.
- **#197's Back** (#204). The Pixel 8 (Release, over wireless adb, `840c2d9`, 15:17 to 15:21): all six steps passed. On
  the `textbox` and `passwordbox` pages the first Back hid the keyboard and left the box at rest (from the last box,
  the page came down too) and the second went to the samples list; a second box tapped with the keyboard up kept the
  focus; on `autosuggestbox` the first Back closed the list and the keyboard; in the ContentDialog's text box the first
  Back took the focus, the dialog stayed, and the second closed it. No exceptions. No real-window run.
- **The phone's pan, read again** (#205, the same profile): a covered AutoSuggestBox with `AutoMaximizeSuggestionArea`
  false moves until its bottom border meets the keyboard's top (184.58), 10.42 further with "a" typed and its list open
  above it; a PasswordBox's reveal row doesn't change the caret rule and stays under the keyboard; a TextBox in a
  ScrollViewer moves the page, the ScrollViewer doesn't scroll; the pan slides, up about 0.3 s slowing, down about
  0.2 s. The probes `keyboardpan` and `keyboardpanboxes`; the numbers in [`tools/refapp`](../tools/refapp/README.md),
  the grabs in `ref/wp81/KeyboardPan/`. #206 came of it.
- **#206's slide and AutoSuggestBox** (#207). The Pixel 8 (Release, over wireless adb, `ddb3772`, 16:37 to 16:52): all
  five steps passed. The "not maximized" box moved until its bottom met the keyboard's top, a frame part way showing a
  slide; with "a" typed its list opened above it and the page went a little further; Back took the keyboard, list and
  focus and the page came down; the default box and the last TextBox slid too. `screencap` takes about 350 ms a frame,
  so the run confirms a slide, not its curve. Gboard showed its number row for a frame as it went, once (not ours). No
  exceptions. No real-window run.

Owed: the sample at speed 2 on the phone (#189), and the reads each control's README lists. The four pan reads that
were in progress on the VM are done (#205). Of the pan, only two reads stay owed: the slide's exact curve (the
recording's 30 frames a second are too few to fit one) and why a covered AutoSuggestBox with
`AutoMaximizeSuggestionArea` false goes 10.42 further once its list opens. Nothing of the keyboard's pan is left open.
Next: Tier 4.

### Tier 4

| Windows Phone 8.1 | Avalonia 12.1.4 | Work | Rung |
|---|---|---|---|
| GridView, GridViewItem, GridViewHeaderItem | ListBox with a wrapping panel; the header item: to check | Theme | 1, 2 |
| SemanticZoom | none | New control: the jump list | 5 |
| FlipView, FlipViewItem | Carousel | Theme. Measure the snap and swipe | 1 |
| ListBox, ListBoxItem | ListBox, ListBoxItem | Theme | 1 |
| RichEditBox, RichTextBlock | none in the free packages | Later, or out | — |
| Controls Avalonia has and the phone never had: Calendar, TreeView, DataGrid, SplitView, Menu, TabControl, Expander, NumericUpDown, ColorPicker and more | — | A plain theme in the era's style, labelled as ours and not a reproduction | 1 |

### Out of scope

| Windows Phone 8.1 | Why |
|---|---|
| MapControl, MediaElement, CaptureElement | Out of scope. MapControl has a default style in the phone file; it stays out |
| WebView | Not in `Avalonia.Controls` 12.1.4; a separate package if anywhere. Nothing to theme |
| AppBar, SettingsFlyout, ToolTip | No default style on the phone |

## Tier 0

Nothing in Tier 1 starts until this list is done, except the type ramp (#26), which may start once
the rest of Tier 0 is open and green, on 2026-11-10 #20, #21, #22 and #24 (`D-tier1-start`,
[ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md)). The desktop sample (#9) didn't hold up
Tier 1.

- [x] A reference VM with the Windows Phone 8.1 tools (2026-11-07). The RTM and Update 1
      emulators are installed. The Update 1 720p emulator was started over SSH and ran a test app
      on 2026-11-08.
- [x] The phone's `generic.xaml`, `themeresources.xaml` and `Windows.winmd` located and readable
      (2026-11-07). Copies are in the reference folder, outside the repo.
- [x] The repo, with the build gate, CI, the three projects and auto-discovery (2026-11-08,
      the bootstrap commit on `main`).
- [x] An issue per item with tier and kind labels; `main` protected, with `build-test` required
      (2026-11-08).
- [x] `CLAUDE.md`, stating only what exists (2026-11-08). Mapping rules move into it as controls
      prove them.
- [x] ADR 0001 accepted, naming the reference build, image, profile and resolution (2026-11-08).
- [x] `PROCESS.md`, `ROADMAP.md`, `ARCHITECTURE.md` and ADR 0002 in the repo (2026-11-08,
      [#4](https://github.com/wdcossey/Cornflower/issues/4)).
- [x] The reference profile's resolution scale read by the reference app's first run on it, and
      ADR 0001 confirmed or amended ([#3](https://github.com/wdcossey/Cornflower/issues/3)).
- [x] This inventory regenerated, with real counts (2026-11-07).
- [x] `tools/winrt-api` written and run on the 8.1 metadata (2026-11-07). It hasn't been run on
      the Windows 10 files.
- [x] The reference app and capture script (2026-11-08): every catalogued control at rest and in
      every template state, dark and light, a PNG and a template-tree JSON each, pulled back with
      one command. First run, on the 720p profile: 504 captures, 0 errors. In
      [`tools/refapp`](../tools/refapp/README.md).
- [x] The reference app, `tools/winrt-api` and the font measurement scripts moved into `tools/`
      ([#5](https://github.com/wdcossey/Cornflower/issues/5)).
- [x] Captures committed under `ref/wp81/<control>/`, from the reference profile: PNGs only
      (`D-captures-public`), first `ref/wp81/TextBlock/` with the type ramp (#69). Each control
      commits its own.
- [x] The reference app captures flyouts, dialogs and the app bar
      ([#23](https://github.com/wdcossey/Cornflower/issues/23)). The popups have tree dumps, and 1080 × 1920
      screenshots from the emulator's Screenshot tool (`run.ps1 -Popups only -Grab`). The page's
      app bar is drawn by the shell, so its screenshots are the reference (2026-11-09, #59).
- [x] Motion recordings: the tilt, page transitions, ToggleSwitch, the pickers
      ([#24](https://github.com/wdcossey/Cornflower/issues/24)), on 2026-11-10 (#66): template
      storyboards read every frame, theme transitions from video (`run.ps1 -Record`). The numbers
      are in [`tools/refapp`](../tools/refapp/README.md) "Motion" and
      [ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-motion). The pickers'
      and the full Flyout's per-row swivel curves were read on 2026-11-16
      ([#149](https://github.com/wdcossey/Cornflower/issues/149), #171). The turnstile's forward start was read on 2026-11-15
      ([#114](https://github.com/wdcossey/Cornflower/issues/114), #128).
- [x] Read from the emulator: the item count at which ComboBox goes full screen, and the
      ToggleSwitch drag threshold ([#25](https://github.com/wdcossey/Cornflower/issues/25)), on 2026-11-09 (#58):
      5 items open inline and 6 full screen, by count, not by screen. A ToggleSwitch release is a tap
      if it moved under about 18 view px and was held under about 1,020 ms; otherwise the knob's
      halfway point decides. In [`tools/refapp`](../tools/refapp/README.md) "Probes".
- [x] The checklist generator: one control's spec from the `.winmd` and `generic.xaml`
      ([#7](https://github.com/wdcossey/Cornflower/issues/7)), as
      `tools/winrt-api checklist <Type>` (2026-11-09). Run on all 57 styled 8.1 controls.
- [x] ~~`tools/winjs-ref`~~ ([#8](https://github.com/wdcossey/Cornflower/issues/8)). Not needed: the shipped WinJS
      2.1 for Phone stylesheet was already pulled on 2026-11-07.
- [ ] A side-by-side tool: two frames in one image.
- [x] A CI check that no Microsoft font or SDK file is committed
      ([#6](https://github.com/wdcossey/Cornflower/issues/6)).
- [x] The CI actions moved off Node 20 ([#2](https://github.com/wdcossey/Cornflower/issues/2)), in the
      bootstrap.
- [x] The desktop sample: a window that opens phone-sized and can be resized, one auto-discovered
      page per control ([#9](https://github.com/wdcossey/Cornflower/issues/9)), on 2026-11-10 (#68):
      `samples/Cornflower.Sample.Desktop`, the `[SamplePage]` catalogue, and
      [`tools/real-window/drive.ps1`](../tools/real-window), from Avonic. Moved up so the extension
      pack has a window to be seen in ([ADR 0004](decisions/0004-larger-screens-as-an-extension-pack.md)).
      The phone opened `MainView` alone until 2026-11-11, when it got the catalogue too, and
      `run.ps1 -Page` to open a page of it (`Q-phone-catalogue`); the shell times every page since
      (`D-page-timing`).
- [x] The Android sample and a first device run on the Pixel 8
      ([#10](https://github.com/wdcossey/Cornflower/issues/10)), with
      [`tools/android/run.ps1`](../tools/android/README.md) (2026-11-09). Start times are in its
      README and in [`docs/performance`](performance/README.md).
- [x] The test harness: `Scene`, `Frames`, the clock hook and `TestClock`
      ([#11](https://github.com/wdcossey/Cornflower/issues/11)).
- [x] Metrics from Avonic: the style budget, `StyleTrace`, the census, the benchmark, the Debug
      performance line, the breakage tool ([#12](https://github.com/wdcossey/Cornflower/issues/12)),
      in [`docs/performance`](performance/README.md) (2026-11-09). Avonic's real-window driver came
      with the desktop sample (#9, #68).
- [x] The test that lists every templated Avalonia control without a Wp81 theme
      ([#13](https://github.com/wdcossey/Cornflower/issues/13)), with the list in
      `tests/Cornflower.Tests/Completeness/Holes.txt` (2026-11-09): 79 holes.
- [x] Spike: does swapping the theme at run time re-template every control?
      ([#14](https://github.com/wdcossey/Cornflower/issues/14))
- [x] Spike: what does a control with no `ControlTheme` do?
      ([#15](https://github.com/wdcossey/Cornflower/issues/15))
- [x] Spike: a `ControlTheme` keyed to a subclass with `BasedOn` the base's gets the base's
      template and can add to it; a subclass whose `StyleKeyOverride` returns the base type gets
      the base's theme unchanged ([#16](https://github.com/wdcossey/Cornflower/issues/16)).
- [x] Spike: an attached tilt on a stock `Button` reads the press point through
      `handledEventsToo` ([#17](https://github.com/wdcossey/Cornflower/issues/17)).
- [x] Fonts: Selawik bundled; Semilight addressed by name and the line-height metric settled by
      test ([#18](https://github.com/wdcossey/Cornflower/issues/18)). Selawik was measured
      against Segoe UI on 2026-11-06 and against Segoe WP on 2026-11-07.
- [x] Tokens: theme colours dark and light, the accent and the 20 named accents, the type ramp,
      sizes ([#19](https://github.com/wdcossey/Cornflower/issues/19)).
- [x] The template glyph set, drawn as our own paths and tested
      ([#20](https://github.com/wdcossey/Cornflower/issues/20)), on 2026-11-10 (#63). 8.1's templates
      draw two: the tick (CheckBox and list items) and the selected item's earmark; the shell
      draws a third, the app bar's ellipsis. The rest are rectangles and ellipses.
- [x] The Lucide generator and icon control ([#21](https://github.com/wdcossey/Cornflower/issues/21)),
      on 2026-11-10 (#64): [`tools/lucide`](../tools/lucide), pinned to Lucide 1.71.0, and
      `LucideIcon`. 161 of WinRT's 193 `Symbol` values have a Lucide icon
      (`tools/lucide/symbols.tsv`).
- [x] The tilt behaviour, measured and tested ([#22](https://github.com/wdcossey/Cornflower/issues/22)),
      on 2026-11-10 (#65): `Tilt.IsEnabled` and `Tilt.IsTarget`, within 0.026° of #60's 100 grid
      presses and 0.021° of the 720p grid, the screen-position bias included, with PlaneProjection's
      measured camera. A press at the very edge clicks, as on the phone, once a template marks its
      tilted part ([ADR 0005](decisions/0005-decisions-of-2026-11-09-and-10.md#the-tilt)).

## What "done" means for Button

Button ([#27](https://github.com/wdcossey/Cornflower/issues/27)) is a dry run of the loop, to
prove the pipeline before anything is batched:

1. The checklist generated from the 8.1 files, every row ticked or explained.
2. Captures from the emulator: rest, pressed, disabled, dark and light, and a recording of the
   tilt.
3. Avalonia's Button measured against them.
4. The theme written; tests assert the measured sizes, colours and tilt timeline.
5. Our frame beside the emulator's, differences listed with measurements.
6. Two reviews, deliberate breakage, and the sample on the Android device.
7. Its README, with the `Status:` line.

If a step can't be done, the gap is in Tier 0 and is fixed there, not worked around in the
control.

## Order of eras

1. Era 1 to depth: the Tier 1 controls finished and compared, before anything else.
2. The rest of era 1.
3. Then a choice between Windows Phone 8.0's controls, as an add-on to era 1, and era 2, Windows
   10 Mobile. Era 2 reuses more.

The cost of a second era is not double. The controls package, the tools, the tests' structure
and the mapping rules carry over. The tokens, the templates and every comparison are new.

## Done

Rolled up at the end of each tier ([process](PROCESS.md#rolling-up-the-tables)). For the latest,
see the `Status:` line in each README under `docs/controls/`.

| Issue | Windows Phone 8.1 | Status |
|---|---|---|
| [#26](https://github.com/wdcossey/Cornflower/issues/26) | TextBlock and the text styles | Done 2026-11-10 (#69) |
| [#27](https://github.com/wdcossey/Cornflower/issues/27) | Button | Done 2026-11-11 (#75) |
| [#28](https://github.com/wdcossey/Cornflower/issues/28) | ProgressRing | Done 2026-11-11 (#88) |
| [#29](https://github.com/wdcossey/Cornflower/issues/29) | HyperlinkButton | Done 2026-11-12 (#91) |
| [#30](https://github.com/wdcossey/Cornflower/issues/30) | RepeatButton, ToggleButton | Done 2026-11-12 (#93) |
| [#31](https://github.com/wdcossey/Cornflower/issues/31) | CheckBox | Done 2026-11-12 (#92) |
| [#32](https://github.com/wdcossey/Cornflower/issues/32) | RadioButton | Done 2026-11-12 (#94) |
| [#33](https://github.com/wdcossey/Cornflower/issues/33) | ToggleSwitch | Done 2026-11-12 (#102) |
| [#34](https://github.com/wdcossey/Cornflower/issues/34) | Slider | Done 2026-11-12 (#96) |
| [#35](https://github.com/wdcossey/Cornflower/issues/35) | ProgressBar | Done 2026-11-12 (#95) |
| [#36](https://github.com/wdcossey/Cornflower/issues/36) | TextBox | Done 2026-11-12 (#101) |
| [#37](https://github.com/wdcossey/Cornflower/issues/37) | PasswordBox | Done 2026-11-12 (#103, #106) |
| [#113](https://github.com/wdcossey/Cornflower/issues/113) | Read: ScrollViewer physics and ListView press feedback | Done 2026-11-15 (#127) |
| [#114](https://github.com/wdcossey/Cornflower/issues/114) | Read: app bar heights, the turnstile's forward angle, Pivot's header motion | Done 2026-11-15 (#128) |
| [#115](https://github.com/wdcossey/Cornflower/issues/115) | `drive.ps1`: real touch, drag and wheel input | Done 2026-11-15 (#124) |
| [#116](https://github.com/wdcossey/Cornflower/issues/116) | ScrollViewer, ScrollBar, Thumb | Done 2026-11-15 (#126) |
| [#117](https://github.com/wdcossey/Cornflower/issues/117) | ContentControl, ContentPresenter | Done 2026-11-15 (#125; by type at the Tier 2 roll-up, #139) |
| [#118](https://github.com/wdcossey/Cornflower/issues/118) | CommandBar, AppBarButton, AppBarToggleButton, AppBarSeparator | Done 2026-11-15 (#133) |
| [#119](https://github.com/wdcossey/Cornflower/issues/119) | ListView, ListViewItem (ListViewHeaderItem, GroupItem deferred) | Done 2026-11-15 (#130) |
| [#120](https://github.com/wdcossey/Cornflower/issues/120) | Page, Frame | Done 2026-11-15 (#134) |
| [#121](https://github.com/wdcossey/Cornflower/issues/121) | Pivot, PivotItem, PivotHeaderItem | Done 2026-11-16 (#140) |
| [#122](https://github.com/wdcossey/Cornflower/issues/122) | Hub, HubSection | Done 2026-11-16 (#146) |
| [#132](https://github.com/wdcossey/Cornflower/issues/132) | ListView's `ItemClick` and `IsItemClickEnabled` | Done 2026-11-15 (#138; `Multiple` alone clicks, #145) |
| [#136](https://github.com/wdcossey/Cornflower/issues/136) | Read: Pivot's wide headers and two items, the Hub's background and loop, the check mode's press, the app bar's press, heights and motion | Done 2026-11-16 (#137) |
| [#141](https://github.com/wdcossey/Cornflower/issues/141) | CommandBar: the shell's press, the open bar's stepped height, its motion | Done 2026-11-16 (#144) |
| [#142](https://github.com/wdcossey/Cornflower/issues/142) | ListView: the check box's own press, `Extended` without boxes | Done 2026-11-16 (#143) |
| [#148](https://github.com/wdcossey/Cornflower/issues/148) | Read: Tier 2's remaining reads (Pivot's and Hub's release, the item events, the Hub's entrance and background, the light and long app bar, item click in each mode) | Done 2026-11-16 (#170) |
| [#149](https://github.com/wdcossey/Cornflower/issues/149) | Read: LoopingSelector's physics, the pickers' row swing, the popups' open and close curves, the inline ComboBox's motion | Done 2026-11-16 (#171) |
| [#150](https://github.com/wdcossey/Cornflower/issues/150) | Read: AutoSuggestBox, how each popup is dismissed, ContentDialog's layout, the pickers' bar, MenuFlyout's items and placement | Done 2026-11-16 (#172) |
| [#151](https://github.com/wdcossey/Cornflower/issues/151) | Flyout, FlyoutPresenter | Done 2026-11-16 (#161) |
| [#152](https://github.com/wdcossey/Cornflower/issues/152) | MenuFlyout, MenuFlyoutPresenter (and `MenuItem` inside it) | Done 2026-11-17 (#175) |
| [#153](https://github.com/wdcossey/Cornflower/issues/153) | ListPickerFlyout, PickerFlyout, and their presenters | Done 2026-11-17 (#176) |
| [#154](https://github.com/wdcossey/Cornflower/issues/154) | LoopingSelector, LoopingSelectorItem | Done 2026-11-17 (#178) |
| [#155](https://github.com/wdcossey/Cornflower/issues/155) | ComboBox, ComboBoxItem | Done 2026-11-17 (#182) |
| [#156](https://github.com/wdcossey/Cornflower/issues/156) | DatePicker, TimePicker, and their presenters | Done 2026-11-17 (#183) |
| [#157](https://github.com/wdcossey/Cornflower/issues/157) | ContentDialog | Done 2026-11-17 (#177) |
| [#158](https://github.com/wdcossey/Cornflower/issues/158) | AutoSuggestBox | Done 2026-11-17 (#174) |
| [#162](https://github.com/wdcossey/Cornflower/issues/162) | Pivot: where a release goes | Done 2026-11-17 (#187) |
| [#163](https://github.com/wdcossey/Cornflower/issues/163) | Pivot: a press while it moves holds it | Done 2026-11-17 (#187) |
| [#164](https://github.com/wdcossey/Cornflower/issues/164) | Pivot: the item events after `SelectionChanged` | Done 2026-11-17 (#187) |
| [#165](https://github.com/wdcossey/Cornflower/issues/165) | Hub: the loop backwards | Done 2026-11-17 (#188) |
| [#166](https://github.com/wdcossey/Cornflower/issues/166) | Hub: the entrance | Done 2026-11-17 (#188) |
| [#167](https://github.com/wdcossey/Cornflower/issues/167) | Hub: an image narrower than the screen, `BackgroundImageFadingIn` | Done 2026-11-17 (#188) |
| [#168](https://github.com/wdcossey/Cornflower/issues/168) | CommandBar: light's pressed look | Done 2026-11-17 (#186) |
| [#169](https://github.com/wdcossey/Cornflower/issues/169) | ItemClick: a tap in the check mode clicks and doesn't check | Done 2026-11-17 (#186) |
| [#179](https://github.com/wdcossey/Cornflower/issues/179) | The keyboard moves the page up so the focused text box shows | Done 2026-11-18 (#196) |
| [#201](https://github.com/wdcossey/Cornflower/issues/201) | Keyboard pan: a covered box stops at its caret; `AutoMaximizeSuggestionArea` | Done 2026-11-18 (#202) |
| [#197](https://github.com/wdcossey/Cornflower/issues/197) | Back that hides the keyboard takes the focus | Done 2026-11-18 (#204) |
| [#206](https://github.com/wdcossey/Cornflower/issues/206) | Keyboard pan: the slide; a covered AutoSuggestBox not maximized | Done 2026-11-18 (#207) |
| [#184](https://github.com/wdcossey/Cornflower/issues/184) | DatePickerFlyout, TimePickerFlyout, and their presenters | Done 2026-11-18 (#193) |
| [#189](https://github.com/wdcossey/Cornflower/issues/189) | `CornflowerConfig`: motion settings, faithful by default | Done 2026-11-17 (#190) |
| [#194](https://github.com/wdcossey/Cornflower/issues/194) | `HoleParts`: the first check built every Wp81 template | Done 2026-11-18 (#195) |

Tier 1 is complete. Tier 2 is complete but for #129, an enhancement carried forward (above); its follow-ups from #148's
reads (#162 to #169) are done and run. Tier 3 is complete, #179, #184, #197 and #206 included; the owed runs are above. Each row's device run on
the Pixel 8 is in the tier tables above, Tier 3's owed runs included.
