# LoopingSelector: plan

The wheel the 8.1 date and time pickers are made of: a column of square items that a finger drags, flicks and lets go,
coming to rest on one, the month and day going round and the year not. The phone's types are `LoopingSelector`,
`LoopingSelectorItem` and `LoopingSelectorPanel`, in `Windows.UI.Xaml.Controls.Primitives`.

## The decision: Avalonia's panel, themed and driven

**The wheel is Avalonia's `DateTimePickerPanel`, given a keyed theme and driven by a behaviour.** No new control.

- **The phone's three types can't be made by an app.** None has a constructor or an activation factory in the 8.1
  `Windows.winmd` (read with `System.Reflection.Metadata`: no `.ctor`, no `ActivatableAttribute`). They are the
  DatePickerFlyout's and TimePickerFlyout's own parts. So there is no public API to reproduce; there is a look and a
  motion.
- **Avalonia's `DatePickerPresenter` and `TimePickerPresenter` require a `DateTimePickerPanel` for each column**
  (`PART_DaySelector`, `PART_MonthSelector`, `PART_YearSelector`; `PART_HourSelector`, `PART_MinuteSelector`,
  `PART_PeriodSelector`; `NameScope.Get`, which throws). A wheel of our own couldn't stand in a column of either. #156
  themes those presenters, so the wheel it is built from has to be that panel. Held by
  `LoopingSelectorTests.Avalonias_Date_And_Time_Presenters_Need_A_DateTimePickerPanel_For_Each_Column`.
- **The panel can be driven.** It is public and not sealed; its `Offset` is public and lays the items out by index each
  time (so the behaviour can set it every frame), its `SelectedValue` puts a looping panel back in the middle of its
  extent, and its children are `ListBoxItem`s a theme can be given. Its own ways that differ from the phone's are kept
  from the finger:
  - **Its tap selects** (a handler on the panel, private): the phone's doesn't. The behaviour handles `Tapped` at each
    item, before it reaches the panel. Held by `Avalonias_Panel_Selects_On_A_Tap_And_The_Wheel_Doesnt`.
  - **In a `ScrollViewer`, a panel that doesn't loop can't reach its last items**: the presenter clamps the offset to the
    extent less the viewport, and the panel's extent for a range that doesn't loop is its items alone, while its offset
    puts the selected item in the middle. With 101 years at 138.375 in 700, the last five can't be selected. So the wheel
    isn't in a ScrollViewer: the behaviour moves the panel itself. Held by
    `In_A_ScrollViewer_A_Panel_That_Doesnt_Loop_Cant_Reach_Its_Last_Items`.
  - **Its snap is a jump**, at `ScrollGestureEnded`, which needs a ScrollViewer: with none, it never comes.
- **What it costs:** the panel's `SelectionChanged` comes on every move, where the phone's comes at rest (Avalonia's
  event; the DatePicker's own `SelectedDate` changes only at the accept button, so an app doesn't see it). The look
  doesn't follow it: the selected look is hidden while the wheel moves, as the phone's is.

So the ladder's rung is 2 (a theme) and 4 (a behaviour on Avalonia's control), not 5.

### #129, scroll physics as a part class

The wheel's motion stays apart from #129. #129 is a gesture recogniser and a `ScrollContentPresenter` for every
ScrollViewer, with #113's inertia and its end-of-content squash; the wheel has no ScrollViewer (above), and its release
is not a plain ScrollViewer's: the target is decided in whole items, a release short of half an item drifts and comes
back, and the coast's constants are the wheel's own (#113's pair is out by 36 to 53 px rms on the wheel's coasts). What
the two share is the law, `dv/dt = −a − c v²`, which `PhoneLoopingSelectorMotion` states in a few lines; if #129 lands,
its law can take the wheel's constants as a second pair.

## Where the values come from

- **The styles**: the 8.1 Update 1 runtime's `loopingselector.json` and `loopingselectoritem.json` (equal to the SDK's
  `generic.xaml`), outside the repository. LoopingSelector: `ShouldLoop` True, the `ItemTemplate` (a StackPanel at the
  bottom left, margin 5,5,0,5: `PrimaryText` in `PhoneFontFamilyNormal` 45 SemiBold, `CharacterSpacing` −24, margin
  0.8,−6.5; `SecondaryText` 17 SemiBold, margin 0.8,−0.8), and a template that is one bar-less ScrollViewer with
  `Mandatory`/`Near` vertical snap points. LoopingSelectorItem: stretched, content stretched across and at the bottom;
  `Root` (a Grid, transparent, margin 5, opacity 0, its `RenderTransform` `ContentScaleTransform`) round `Border`
  (2.5, `LoopingSelectorForegroundThemeBrush`) round `ContentPresenter` (the same brush); states below.
- **The layout and the motion**: #149's `-Probe looping` runs, `20261116-1828-dev11` and `20261116-1904-dev11`
  ([`tools/refapp/README.md`](../../../tools/refapp/README.md), "LoopingSelector, and a press on a closed ComboBox"),
  printed by [`tools/motion/looping.py`](../../../tools/motion/looping.py). Their numbers are in
  `tests/Cornflower.Tests/Controls/LoopingSelectorReadings.cs`.
- **The pixels**: the DatePickerFlyout's and TimePickerFlyout's screenshots, run `20261108-2254-dev11`, committed in
  [`ref/wp81/LoopingSelector/`](../../../ref/wp81/LoopingSelector/README.md).

## The checklists

Generated on 2026-11-16 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or Avalonia's; `[-]` is left out, with its reason.
The phone's types can't be made by an app, so no row is a member Cornflower declares.

> Generated by `dotnet run --project tools/winrt-api -- checklist LoopingSelector <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

### LoopingSelector

WinRT `Windows.UI.Xaml.Controls.Primitives.LoopingSelector`; Avalonia `Avalonia.Controls.Primitives.DateTimePickerPanel` (the checklist says none: it maps by name).

#### Properties (7)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ShouldLoop` | `Boolean` | `DateTimePickerPanel.ShouldLoop` | Avalonia's: the behaviour wraps when it is on and stops at the ends when it is off |
| [x] | `SelectedItem` | `Object` | `SelectedValue` | Avalonia's: the panel's items are values in a range, not objects |
| [x] | `SelectedIndex` | `Int32` | `SelectedValue` | Avalonia's; set at rest by the behaviour |
| [x] | `Items` | `IList<Object>` | `MinimumValue`, `MaximumValue`, `Increment`, `PanelType`, `ItemFormat` | Avalonia's: the panel makes its items from a range |
| [x] | `ItemWidth` | `Int32` | none | The item template's box, 123 (`Wp81LoopingSelectorItemSize`); the column's width is the panel's |
| [x] | `ItemTemplate` | `DataTemplate` | the item theme's `ContentTemplate` | The phone's template; the two lines are the formatted string's first line and the rest |
| [x] | `ItemHeight` | `Int32` | `ItemHeight` | 123 on the phone, drawn 1.125 larger: the panel's `ItemHeight` is the pitch, 138.375 |

#### Events (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectionChanged` | `SelectionChangedEventHandler` | `DateTimePickerPanel.SelectionChanged` | Avalonia's, raised on every move, where the phone's is at rest (a known gap, the README) |

#### Methods (0)

_none_

#### Default style: setters (2)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ShouldLoop` | `True` | none | Not set by the theme: the panel's default is false, and #156's presenter sets it per column as Avalonia's does (the year doesn't loop on the phone either) |
| [x] | `ItemTemplate` | `DataTemplate` | none | The item theme's `ContentTemplate` |

#### Template parts (1)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ScrollViewer` | `ScrollViewer` | `PART_ScrollViewer` | No ScrollViewer: it would clamp a panel that doesn't loop (above). The panel clips to its bounds, as the viewport does, and the behaviour moves it |

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (2)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | The item template's two lines |
| [-] | `ScrollViewerScrollBarlessTemplate` | not in themeresources.xaml |  | no | No ScrollViewer (above) |

### LoopingSelectorItem

> Generated by `dotnet run --project tools/winrt-api -- checklist LoopingSelectorItem <Windows.winmd> <design folder>`.

WinRT `Windows.UI.Xaml.Controls.Primitives.LoopingSelectorItem`; Avalonia the panel's `ListBoxItem`s, in `Wp81LoopingSelectorItemTheme`.

#### Properties (0), events (0), methods (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | The phone's items set none |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Avalonia's data templates choose by type |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentTemplate` | The item theme's: the phone's `ItemTemplate` |
| [x] | `Content` | `Object` | `Content` | The panel's formatted string |
| [x] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | the presenter's `Child` | Avalonia's |

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | `OnPropertyChanged` | Avalonia's |
| [x] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | `OnPropertyChanged` | Avalonia's |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | No selector (above) |

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HorizontalAlignment` | `Stretch` | `HorizontalAlignment` | Set |
| [x] | `VerticalAlignment` | `Stretch` | `VerticalAlignment` | Set |
| [x] | `HorizontalContentAlignment` | `Stretch` | `HorizontalContentAlignment` | Set, bound to the presenter |
| [x] | `VerticalContentAlignment` | `Bottom` | `VerticalContentAlignment` | The panel sets `Center` on each item it makes, as a local value, so the template puts the presenter at the bottom itself |

#### Template parts (4)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Root` | `Grid` | `Root` | Margin 5, scaled 1.125 about its top left, inside a 123 square at the top left of the 138.375 cell |
| [x] | `ContentScaleTransform` | `ScaleTransform` | the `Root`'s `RenderTransform` | 1.125, as #149 read it (`Wp81LoopingSelectorItemScale`); the phone's code sets it, the theme here |
| [x] | `Border` | `Border` | `Border` | 2.5 |
| [x] | `ContentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Avalonia's name |

#### Visual states (3)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | `Root` at the wheel's `PhoneLoopingSelector.Expansion`, 0 |
| [x] | `CommonStates` | `Expanded` | `Root.Opacity: DoubleAnimation To=1.0 Duration=0` | `PhoneLoopingSelector.Expansion` 1 | The active wheel |
| [x] | `CommonStates` | `Selected` | `Root.Opacity` 1, `Border.BorderBrush` transparent, `ContentPresenter.Foreground` `LoopingSelectorSelectionForegroundThemeBrush`, `Root.Background` `LoopingSelectorSelectionBackgroundThemeBrush` | `^:selected[(wp81\|PhoneLoopingSelector.IsMoving)=False]` | The panel's own `IsSelected`, shown only at rest |

#### Transitions (2)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` → `Expanded` | `0:0:0.33` | nothing | The behaviour moves `Expansion` in a straight line over 330 ms on `CornflowerClock`, from where it is |
| [x] | `CommonStates` | `Expanded` → `Normal` | `0:0:0.33` | nothing | The same, down |

#### Resources (3)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `LoopingSelectorSelectionForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | Selected |
| [x] | `LoopingSelectorSelectionBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Selected |
| [x] | `LoopingSelectorForegroundThemeBrush` | `SolidColorBrush #73FFFFFF` | `SolidColorBrush #40000000` | yes | The border and the text |

### LoopingSelectorPanel

> Generated by `dotnet run --project tools/winrt-api -- checklist LoopingSelectorPanel <Windows.winmd> <design folder>`. Its own members only; the rest are `Canvas`'s, `Panel`'s,
> `FrameworkElement`'s and `UIElement`'s, which `DateTimePickerPanel` has as Avalonia's `Panel`.

WinRT `Windows.UI.Xaml.Controls.Primitives.LoopingSelectorPanel`; Avalonia `DateTimePickerPanel`. No default style.

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `AreHorizontalSnapPointsRegular` (read-only) | `Boolean` | none | Snap points are a ScrollViewer's; the behaviour rests on whole items itself |
| [-] | `AreVerticalSnapPointsRegular` (read-only) | `Boolean` | none | The same |
| [-] | `HorizontalSnapPointsChanged` | `EventHandler<Object>` | none | The same |
| [-] | `VerticalSnapPointsChanged` | `EventHandler<Object>` | none | The same |
| [-] | `GetIrregularSnapPoints(Orientation, SnapPointsAlignment)` | `IReadOnlyList<Single>` | none | The same |
| [-] | `GetRegularSnapPoints(Orientation, SnapPointsAlignment, ref Single)` | `Single` | none | The same |

## The phone's layout

From the probe's layout lines (the DatePickerFlyout, 15 July 2014):

- Three wheels 138.333 wide and 700 tall from y 37.083, at x 17.5, 155.833 and 294.167: they abut.
- `ItemWidth` and `ItemHeight` 123; the items laid out 122.917 square (295 raw px), scaled 1.125, so 138.375 apart.
  The selected item's top is 317.943 (the year's 317.896): its 138.375 cell centred in the 700.
- The tile drawn: x 54 to 358, y 775 to 1079 raw (the edges part-covered), so from 22.5, 322.917, 127.08 square: `Root`
  at 5 in, 112.917 × 1.125.
- Month and day loop, the year doesn't. A looping wheel's extent is 139,213 px, the offset in its middle.

## The behaviours

Each has a test in `LoopingSelectorTests` (or `LoopingSelectorPageTests`), red without it (the breakage run).

| Behaviour | Evidence | Test |
|---|---|---|
| The items 138.375 apart, the selected one's top at 317.943 in a 700 column, unrounded | probe layout | `Each_Item_Has_The_Wheels_Item_Theme_And_The_Items_Are_The_Phones_Pitch_Apart` |
| The tile at 22.5, 322.917, 127.08 square | the screenshots | `The_Selected_Tile_Is_Where_The_Capture_Draws_It` |
| At rest only the selected item shows, in the accent, white, no border; the active wheel shows every item, bordered 2.5 in `LoopingSelectorForegroundThemeBrush` (dark and light) | the style; the probe's opacities | `At_Rest_Only_The_Selected_Item_Shows_In_The_Accent_And_The_Active_Wheel_Shows_Every_Item` |
| The item text: the first line 45 SemiBold, spacing −24, the rest 17 SemiBold, at the bottom left | the style's `ItemTemplate` | `The_Items_Show_Their_Formats_First_Line_Large_And_The_Rest_Small` |
| A tap on an item (two below, two above, the selected) selects nothing and moves nothing: it makes the wheel active, at 1 at once the first time | #149: no `ViewChanging`, no `SelectionChanged` | `A_Tap_On_An_Item_Selects_Nothing_And_Only_Makes_The_Wheel_Active` |
| Another wheel made active fades the last out, 0.0469 a frame in a straight line, and one made active again fades in from where it is | #149: the opacity, 21.3 frames, both ways | `Another_Wheel_Made_Active_Fades_The_Last_One_Out_In_A_Straight_Line` |
| A drag moves the items with the finger and makes the wheel active; no item shows as selected until rest | #149: `Selected > Expanded` as the drag starts, the new item `Selected` at rest | `A_Drag_Moves_The_Items_With_The_Finger_And_Hides_The_Selected_Look_Until_Rest` |
| Held still and let go short of half an item past: drifts on 8.4 to 10.6 px, then back | #149: 9.1 to 9.5 px over 7 or 8 frames, back over 341 to 399 ms | `Let_Go_Short_Of_Half_An_Item_It_Drifts_On_Then_Comes_Back`, `The_Way_Back_Follows_The_Phones_Frames` |
| Held still and let go past half: on to the next item at a falling speed | #149: 0.000834 px/ms², at rest in 328 ms | `Let_Go_Past_Half_An_Item_It_Goes_On_To_The_Next_At_A_Falling_Speed` |
| A flick coasts by the law to an item decided at the release, which is the selection at rest | #149: #113's `a + c v²`, 8 to 14 items | `A_Flick_Coasts_On_The_Law_To_An_Item` |
| Month and day wrap; the year stops at its ends | #149 | `Month_And_Day_Wrap_And_The_Year_Doesnt` |
| A press catches a coast within a frame; let go, it goes to the nearer item | #149: 0.0 px while held 592 ms | `A_Press_Catches_A_Coast_Within_A_Frame_And_The_Release_Goes_To_The_Nearer_Item` |
| A notch of the mouse wheel turns it one item | none: the phone has no mouse | `A_Notch_Of_The_Mouse_Wheel_Turns_It_One_Item` |
| Off (the theme taken away), the panel is Avalonia's again | rule 3 | `Taken_Off_The_Panel_It_Leaves_Avalonias_Own_Behind` |

## The motion, as modelled

`PhoneLoopingSelectorMotion`, pure, run frame by frame on `CornflowerClock` by `PhoneLoopingSelector`. The fits are
mine, from `tools/motion/looping.py`'s frames of both runs (the fitting script is not in the repository: a grid search
over the frames `looping.py` prints).

- **Where it goes**, decided at the release, in whole items: the item nearest where a coast at the release's speed would
  stop. A finger held still before it lifts has no speed (the moves of the last 50 ms give it), so that is the item
  nearest where it was let go: past half an item, the next; short, back. The probes' slow drags were all held 400 ms.
- **On to an item** (the target ahead of the drag, or a flick): #113's law, `dv/dt = −a − c v²`, from whatever speed
  lands it on the item. #113's own pair (a 0.000529, c 0.000679, for a plain ScrollViewer) is out by 36 to 53 px rms on
  the wheel's flicks. Fitted to the wheel's eight coasts (three flicks a run, and the two drags let go past half that
  were caught in full), **a = 0.000785 px/ms², c = 0.00049 /px**: 1.5 to 11.7 px rms each, at most 19.3 px off on a
  1,687 px coast, at rest within 130 ms of the phone. At low speed it is close to a constant deceleration of 0.000785,
  where the slow releases read 0.000834 to 0.000967: on the phone's 61.5 px move, within 2.8 px of every frame, at rest
  at 394 ms against its 328 (its last 50 ms move under 2 px).
- **Back to an item**: no simple ease fits (#149: sine ease-in-out within 1.7% at best, starting before the turn).
  Two pieces, fitted to the five such releases: a drift on the drag's way at **0.086 px/ms for 98 ms** (8.4 px), then a
  **sine ease-in-out to the item over 46 √d ms**, d the way back in px (316 to 397 ms for the 47 to 75 px read): 1.0 to
  2.2 px rms each, at most 4.5 px. At rest 414 to 495 ms after the first inertial frame, the phone's 438 to 531 (its last 50 ms move under a pixel).
- **A press on a moving wheel** stops it at once; let go with no drag, it goes to the nearer item by the rules above,
  its way the way it was moving.
- **At rest**, the panel's `SelectedValue` is set: the selected look shows, and a looping panel is put back in the
  middle of its extent, as the phone's offset jumps back after each rest (#149's recentring).
- **The active wheel**: a tap's release or a drag's start makes a wheel the active one of its group (the wheels in the
  same template, or with none, the same window). `PhoneLoopingSelector.Expansion` goes to 1, and the others' to 0, at
  1/330 a ms from where each is; the first expansion is at once. The phone's fade out starts about 47 ms (3 frames)
  after the other wheel's expansion; here it starts with it (a gap, the README).

## The drag

`PhoneLoopingSelector.WheelGestureRecognizer`, on the panel: a touch or a pen that moves up or down further than
Avalonia's `ScrollGestureRecognizer` waits for (its `ScrollStartDistance`), and more up or down than sideways, is the
wheel's; the items move with the finger from there, so the start distance isn't moved (the phone's drags lost 5 to
10 px the same way). A press on a moving wheel holds it. Touch and pen only, as Avalonia's recogniser and the Pivot's
and Hub's; a mouse turns the wheel by its wheel.

## Avalonia against the phone

| The phone | Avalonia's `DateTimePickerPanel` | Rung |
|---|---|---|
| A ScrollViewer round the panel | none: the panel alone | 2: the wheel isn't in one (a ScrollViewer clamps it) |
| Items 123, scaled 1.125, 138.375 apart, drawn unrounded | `ListBoxItem`s `ItemHeight` tall, rounded | 2: `ItemHeight` 138.375, the item theme, no layout rounding |
| The item template, two lines | one formatted string | 2: the item theme splits it at its first line break |
| A drag, the release's target in whole items, the coast, the drift back | the ScrollViewer's own inertia, then a jump to the nearest | 4: `PhoneLoopingSelector` |
| A tap only activates | a tap selects | 4: handled at the item |
| `Normal`, `Expanded`, `Selected` | `:selected`, continuously | 2 and 4: `Expansion`, `IsMoving` |
| `SelectionChanged` at rest | on every move | Avalonia's (a gap) |

## Extensions

None. The theme keys (`Wp81LoopingSelectorTheme`, `Wp81LoopingSelectorItemTheme` and the sizes) are Cornflower's, as
`Wp81ScrollViewerScrollBarlessTheme` is.

## Dependencies

- `CornflowerClock` (exists). `PhoneTextLine` for the two lines (exists). No popup: the wheel is placed by whoever
  holds it (#156's presenter).
- Captures still needed: a still of a wheel `Expanded`, dark and light, and one moving (the probes read every frame,
  but no recording or screenshot exists). The phone's behaviour at a non-looping wheel's ends (an overpan, a squash) is
  unread.
