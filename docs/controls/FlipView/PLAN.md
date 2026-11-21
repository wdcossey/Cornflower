# FlipView and FlipViewItem: plan

[#214](https://github.com/wdcossey/Cornflower/issues/214). The phone's FlipView is Avalonia's `Carousel` in the Wp81 theme
(`Controls/Carousel.axaml`, by type, rung 1), with a behaviour for the swipe, the snap and a change from code
(`PhoneFlipView` and `PhoneFlipViewMotion`, rung 2) and an items panel of the theme's (`PhoneFlipViewPanel`). The evidence is
#211's (PR #217): the captures in [`ref/wp81/FlipView/`](../../../ref/wp81/FlipView/README.md) (run `20261118-2210-dev11`),
and the probes `20261118-2213-dev11` (`-Probe flipview`) and `20261118-2216-dev11` (`-Probe flipvertical`), read in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "Tier 4's reads", FlipView, and by `tools/refapp/flip.py`.
Device 11, the reference profile, 450 × 800 at 2.4. The probes' FlipView: five grey panels, full width, 400 tall, 80 from
the top, item 1 selected, so a page is 445 px across (395 down).

## The key

- **The template** (the SDK's `generic.xaml`): a `Border` with a margin of 2.5, bound to `Background`, `BorderBrush` and
  `BorderThickness`, round a `ScrollViewer` (`ScrollingHost`, snap points `MandatorySingle`) round the items. No buttons,
  no indicator, no visual states. `FlipViewItem`: a transparent `Border` round a stretched `ContentPresenter`.
- **Avalonia's Carousel** shows one item at a time in its `VirtualizingCarouselPanel` and runs its `PageTransition`
  between them; Avalonia 12's own swipe (`IsSwipeEnabled`) drives that panel. The phone's drags the next item in beside
  the one going, edge to edge. So the theme replaces the panel with `PhoneFlipViewPanel` (every item the size of the
  Border's inside, a page apart along the axis, placed where the motion has the strip) and drops the template's
  `ScrollViewer`, whose snapping the motion replaces.
- **The PageTransition never runs**: it is run by the panel the theme replaces. Only its axis is read, as Avalonia reads
  it for its keys and layout: a `PageSlide` (or a composite holding one) with `Orientation="Vertical"` turns the FlipView up
  and down. The phone turns it by a vertical items panel.
- **The swipe** is the Pivot's and the Hub's recogniser, `SidewaysSwipeGestureRecognizer`, given an up-and-down axis
  (`Vertical`) and a factory for a target that isn't the control (`For(Visual, ISidewaysSwipeTarget)`). Shared code; its
  Pivot and Hub tests stay green.
- **The release rule** is the Pivot's: `PivotMotion.ReleaseLookAhead` (385 ms) and `ReleaseReach` (0.52 a page), used, not
  copied. The snap on is the Pivot's swipe curve, `PivotMotion.SwipeCurve` over `SwipeDuration` (515.625 ms).
- **Motion speed**: `CornflowerConfig.ControlSpeed`, the area for the other controls' motions (`MotionArea.Control`), read
  as each move starts. Not `PivotAndHubSpeed`: the FlipView is neither, and an app that slows its Pivot needn't slow its
  FlipViews.

## The curves

Fitted with a script in the PR's scratch folder (not kept) over `flip.py`'s frames, from the first frame after the release
(P at the release) to rest. P is where the strip is, in view px from item 0.

| Move | Curve | Over | Fit |
|---|---|---|---|
| On, after a swipe | `PivotMotion.SwipeCurve`, KeySpline (0.24, 0.60, 0.22, 1), reused | 515.625 ms, 33 frames (the phone's 516 to 531 in `RenderingTime`) | the README's per-frame swipe: 1.38 px rms, 2.62 px at most, of 270; the other two swipes 4.7 and 7.3 px rms. A release that goes on slowly is off by more (below) |
| Back, after a release short, and from past an end | `BackCurve`, KeySpline (0.43, −0.47, 0.25, 1), new | 640.625 ms, 41 frames (the phone's 588 to 666) | 15 releases, horizontal and vertical, the ends included: 6.6 px rms in all. The negative Y1 carries it on the way it was dragged first, as the phone's goes 10 to 28 px further in its first 125 ms, and then back |
| `SelectedIndex` ±1 from code | `CodeCurve`, KeySpline (0.22, 0.06, 0, 1), new | 515.625 ms, 33 frames (the phone's 34 frames over 516 ms) | the one read, 2 to 1: 1.56 px rms, 4.88 at most, of 445. The Pivot's tap curve was 7.99 px rms, its swipe curve 19.7 |

No existing ease fitted the way back within the reading: the Pivot's `ReturnCurve` (546.875 ms) is 9.7 to 67.6 px rms,
because the phone's carries on before it comes back. A spring (critically damped, from the release's speed) was tried for
both and fitted worse (on 5.5 px rms, back 18.9).

## Behaviours

One test each, in `tests/Cornflower.Tests/Controls/FlipViewTests.cs`.

| Behaviour | Evidence | Test |
|---|---|---|
| An item is the FlipView less 2.5 a side (445 × 395); the items abut, item 1 at 2.5, item 2 at 447.5 | the template's margin; the probe's item places (item 0 at −442.5, item 1 at 2.5, item 2 at 447.5) | `An_Item_Is_The_FlipView_Less_2_5_A_Side_And_The_Items_Abut` |
| No buttons and no indicator | the template; the captures | `The_Template_Has_No_Buttons_Or_Indicator` |
| Held still, back at 221.0 px, on from 233.8 | the release table | `Held_Still_Past_Half_A_Page_It_Goes_On` |
| Moving, the speed decides: 109.0 at 0.20 px/ms back, 107.2 at 0.33 on | the release table | `Moving_The_Speed_Decides` |
| The snap on follows the phone's frames; `SelectionChanged` on its last frame | the README's per-frame swipe; "SelectionChanged comes at the end of the snap" | `The_Snap_On_Follows_The_Phones_Frames_And_Selects_On_Its_Last` |
| The snap back carries on and comes back, by the phone's frames | `flip.py`'s frames for "left 120 px let go at 0.2" | `The_Snap_Back_Follows_The_Phones_Frames` |
| The snap back is at rest at 640.625 ms | the snap back's 588 to 666 ms | `The_Snap_Back_Comes_To_Rest_After_640_Ms` |
| Past the first item it follows 139.7 of 150 px, then eases back; the selection stays | "The first and last items" | `Past_The_First_Item_It_Follows_Damped_And_Comes_Back` |
| A flick past the first doesn't go round | "at item 0, swipe right": back | `A_Flick_Past_The_First_Doesnt_Go_Round` |
| Past the last it follows 98.1 and 100.6 of 150 px (99.35 here) | as above | `Past_The_Last_Item_It_Follows_About_Two_Thirds` |
| `SelectedIndex` ±1 from code animates by the phone's frames | "SelectedIndex from code", 2 to 1 | `A_Change_Of_One_From_Code_Animates_As_The_Phones` |
| A change of two jumps | 1 to 3 and 3 to 1 | `A_Change_Of_Two_From_Code_Jumps` |
| A press while it snaps stops it dead; let go, the rule from where it stopped (on, past half) | "A press while it snaps catches it" | `A_Press_While_It_Snaps_Stops_It_Dead_And_Let_Go_It_Goes_On` |
| A drag after the press goes on from there | as above ("then a drag continues", #214) | `A_Drag_After_A_Press_Mid_Snap_Goes_On_From_There` |
| A tap during a move from code lets it run to its target (the Pivot's rule for a move that isn't a swipe's) | the Pivot (#163); no read of the FlipView's | `A_Tap_During_A_Move_From_Code_Lets_It_Run` |
| An index shifted by a change to the items doesn't animate | the same item stays selected | `An_Index_Shifted_By_The_Items_Doesnt_Animate` |
| A move from code doesn't scroll the page round the FlipView | the phone's FlipView doesn't (`AutoScrollToSelectedItem` off) | `A_Move_From_Code_Doesnt_Scroll_The_Page_Round_It` |
| With the motion off, the panel still shows the selection | the panel's own rule | `With_The_Motion_Off_The_Panel_Follows_The_Selection` |
| Under an app's own panel the motion leaves the finger alone | so it can't fight Avalonia's swipe | `Under_Another_Panel_The_Motion_Doesnt_Swipe` |
| A `PageSlide` turned in place is read at the next press | Avalonia reads the axis on demand | `A_PageSlide_Turned_In_Place_Is_Read_At_The_Next_Press` |
| Vertical: the same along Y, a page 395; held, 247.3 up goes on; a drag across doesn't move it | "Vertical" | `A_Vertical_FlipView_Flips_Up_And_Down` |
| A ScrollViewer in an item keeps a drag up and down | the Pivot's rule (#163), #214 | `A_ScrollViewer_In_An_Item_Keeps_Its_Drag` |
| A drag more across the axis than along it isn't a swipe | the recogniser's rule (Pivot, Hub) | `A_Drag_More_Across_The_Axis_Than_Along_It_Isnt_A_Swipe` |
| A part with `PhoneNoManipulation` keeps its drag | as above | `A_Part_With_No_Manipulation_Keeps_Its_Drag` |
| `IsSwipeEnabled`, on in the theme; off, nothing drags | Avalonia's API | `IsSwipeEnabled_Off_Drags_Nothing` |
| The motion runs at `ControlSpeed`, not `PivotAndHubSpeed` | ADR 0006 | `The_Snap_Runs_At_The_Control_Speed` |

## The visual states

None: the phone's template declares none (`FlipView` and `FlipViewItem`). `themeresources.xaml`'s `FlipViewButton*`
brushes, already in `Tokens/Colors.axaml`, have no part to draw.

## Avalonia's control against the phone's

| Difference | Rung |
|---|---|
| One item shown at a time, `PageTransition` between | 1: the theme's `ItemsPanel`, `PhoneFlipViewPanel` |
| Avalonia's swipe (on its own panel) | 2: `PhoneFlipView`, on the shared recogniser; `IsSwipeEnabled` still turns it off |
| `SelectedIndex` jumps (with no transition run) | 2: a change of one animates, more jumps |
| The axis | Avalonia's: the `PageSlide`'s orientation |
| `WrapSelection` | Avalonia's, for `Next()`, `Previous()` and the keys only; a swipe never goes round, as the phone's |
| `ViewportFraction` | not read: a gap (the phone has none) |

## Extensions

None.

## Dependencies

`SidewaysSwipeGestureRecognizer` (Pivot, Hub), `PivotMotion`'s rule and swipe curve, `CornflowerClock`, `KeySplineCurve`,
`CornflowerConfig`, `PhoneNoManipulation`: all exist. The theme reaches the recogniser and `PivotMotion` through
`Cornflower.Controls`' existing `InternalsVisibleTo Cornflower.Themes.Wp81`. No new captures needed.

## Checklists

Generated with `dotnet run --project tools/winrt-api -- checklist FlipView <Windows.winmd> <design folder>` and `checklist
FlipViewItem`, from the phone SDK's `Windows.winmd` (`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and
its design folder (`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done; `[-]` is left out, with its reason.

### FlipView: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist FlipView <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.FlipView`; Avalonia none: the generator's mechanical column. Ours is Avalonia's `Carousel` themed (rung 1) with a behaviour (rung 2); each row says what of the Carousel stands in.

#### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `UseTouchAnimationsForAllNavigation` | `Boolean` | none | left out: every move here is a touch's or SelectedIndex's; the phone's default, true, is what was read |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `Selector` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectedValuePath` | `String` | `SelectedValueBinding` | Nothing: Avalonia's API stands |
| [x] | `SelectedValue` | `Object` | `SelectedValue` | Nothing: Avalonia's API stands |
| [x] | `SelectedItem` | `Object` | `SelectedItem` | Nothing: Avalonia's API stands; set from code, it moves as SelectedIndex does |
| [x] | `SelectedIndex` | `Int32` | `SelectedIndex` | Behaviour: a change of one animates (`CodeCurve`), more jumps; a swipe sets it on the snap's last frame |
| [-] | `IsSynchronizedWithCurrentItem` | `Boolean?` | none | not applicable: Avalonia has no collection view with a current item |

#### Events, from `Selector` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SelectionChanged` | `SelectionChangedEventHandler` | `SelectionChanged` | Behaviour: raised on the snap's last frame, not mid-way as the Pivot's |

#### Methods, from `Selector` (0)

_none_

#### Properties, from `ItemsControl` (14)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ItemsPanelRoot` (read-only) (not a dependency property) | `Panel` | `ItemsPanelRoot` | Nothing: Avalonia's; it is the theme's `PhoneFlipViewPanel` |
| [x] | `ItemsSource` | `Object` | `ItemsSource` | Nothing: Avalonia's API stands |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsPanel` | Themed: `PhoneFlipViewPanel`, for the phone's `VirtualizingStackPanel`; replaced by an app, the motion has nothing to move (gap) |
| [x] | `ItemTemplateSelector` | `DataTemplateSelector` | `ItemTemplate` (an `IDataTemplate` that selects) | Nothing: Avalonia's API stands |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemTemplate` | Nothing: Avalonia's API stands |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | left out: the phone's FlipView sets none |
| [-] | `ItemContainerStyleSelector` | `StyleSelector` | none | not applicable: Avalonia styles containers by selectors |
| [x] | `ItemContainerStyle` | `Style` | `ItemContainerTheme` | Nothing: Avalonia's API stands |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | not applicable: a FlipView isn't grouped |
| [x] | `DisplayMemberPath` | `String` | `DisplayMemberBinding` | Nothing: Avalonia's API stands |
| [-] | `GroupStyle` (read-only) (not a dependency property) | `IObservableVector<GroupStyle>` | none | not applicable: a FlipView isn't grouped |
| [-] | `IsGrouping` (read-only) | `Boolean` | none | not applicable: a FlipView isn't grouped |
| [x] | `ItemContainerGenerator` (read-only) (not a dependency property) | `ItemContainerGenerator` | `ItemContainerGenerator` | Nothing: Avalonia's API stands |
| [x] | `Items` (read-only) (not a dependency property) | `ItemCollection` | `Items` | Nothing: Avalonia's API stands |

#### Events, from `ItemsControl` (0)

_none_

#### Methods, from `ItemsControl` (14)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object ItemFromContainer(DependencyObject container)` | `ItemFromContainer` | Nothing: Avalonia's API stands |
| [x] | `DependencyObject ContainerFromItem(Object item)` | `ContainerFromItem` | Nothing: Avalonia's API stands |
| [x] | `Int32 IndexFromContainer(DependencyObject container)` | `IndexFromContainer` | Nothing: Avalonia's API stands |
| [x] | `DependencyObject ContainerFromIndex(Int32 index)` | `ContainerFromIndex` | Nothing: Avalonia's API stands |
| [x] | `Boolean IsItemItsOwnContainerOverride(Object item)` (protected, overridable) | `NeedsContainerOverride` | Nothing: Avalonia's API stands |
| [x] | `DependencyObject GetContainerForItemOverride()` (protected, overridable) | `CreateContainerForItemOverride` | Nothing: Avalonia's API stands |
| [x] | `void ClearContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `ClearContainerForItemOverride` | Nothing: Avalonia's API stands |
| [x] | `void PrepareContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `PrepareContainerForItemOverride` | Nothing: Avalonia's API stands |
| [x] | `void OnItemsChanged(Object e)` (protected, overridable) | `ItemsView.CollectionChanged` | Behaviour: a change to the items puts the strip at rest at the selection |
| [-] | `void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)` (protected, overridable) | none | not applicable: Avalonia raises a property change |
| [-] | `void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)` (protected, overridable) | none | not applicable: no style selector object |
| [-] | `void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)` (protected, overridable) | none | not applicable: Avalonia raises a property change |
| [-] | `void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)` (protected, overridable) | none | not applicable: no template selector property |
| [-] | `void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)` (protected, overridable) | none | not applicable: a FlipView isn't grouped |

#### Default style: setters (13)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | `Background` | Themed: Transparent, on the Border |
| [x] | `BorderThickness` | `0` | `BorderThickness` | Themed: 0, on the Border |
| [x] | `TabNavigation` | `Once` | `KeyboardNavigation.TabNavigation` | Themed: Once |
| [x] | `IsTabStop` | `False` | `IsTabStop` | Themed: False |
| [-] | `ScrollViewer.HorizontalScrollBarVisibility` | `Hidden` | `ScrollViewer.HorizontalScrollBarVisibility (attached)` | not applicable: the template has no ScrollViewer; the motion moves the items |
| [-] | `ScrollViewer.VerticalScrollBarVisibility` | `Hidden` | `ScrollViewer.VerticalScrollBarVisibility (attached)` | not applicable: the template has no ScrollViewer; the motion moves the items |
| [-] | `ScrollViewer.IsHorizontalRailEnabled` | `False` | none | not applicable: the recogniser takes a drag more along the axis than across it |
| [-] | `ScrollViewer.IsVerticalRailEnabled` | `False` | none | not applicable: as above |
| [-] | `ScrollViewer.IsHorizontalScrollChainingEnabled` | `True` | none | gap: no chaining; a ScrollViewer in an item keeps a drag along its own axis (README, known gaps) |
| [-] | `ScrollViewer.IsVerticalScrollChainingEnabled` | `True` | none | gap: as above |
| [-] | `ScrollViewer.IsDeferredScrollingEnabled` | `False` | `ScrollViewer.IsDeferredScrollingEnabled (attached)` | not applicable: the template has no ScrollViewer; the motion moves the items |
| [-] | `ScrollViewer.BringIntoViewOnFocusChange` | `True` | `ScrollViewer.BringIntoViewOnFocusChange (attached)` | not applicable: the template has no ScrollViewer; the motion moves the items |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsPanel` | Themed: `PhoneFlipViewPanel`, for the phone's `VirtualizingStackPanel`; replaced by an app, the motion has nothing to move (gap) |

#### Template parts (1)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ScrollingHost` | `ScrollViewer` | `PART_ScrollingHost` | Replaced: `PhoneFlipViewPanel`, moved by `PhoneFlipView`'s motion, for the phone's snapping ScrollViewer |

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (0)

_none_


### FlipViewItem: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist FlipViewItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.FlipViewItem`; Avalonia none: the generator's mechanical column. Ours is Avalonia's `Carousel` themed (rung 1) with a behaviour (rung 2); each row says what of the Carousel stands in.

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `SelectorItem` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsSelected` | `Boolean` | none (the Carousel has no item type) | Nothing: the container is a `ContentPresenter`; selection is the Carousel's |

#### Events, from `SelectorItem` (0)

_none_

#### Methods, from `SelectorItem` (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | left out: the phone's FlipViewItem sets none |
| [x] | `ContentTemplateSelector` | `DataTemplateSelector` | `ItemTemplate` (an `IDataTemplate` that selects) | Nothing: Avalonia's API stands |
| [x] | `ContentTemplate` | `DataTemplate` | `ItemTemplate` | Nothing: the Carousel's ItemTemplate reaches the presenter |
| [x] | `Content` | `Object` | the item | Nothing: the container presents the item |
| [x] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | `ContentPresenter.Child` | Nothing: Avalonia's API stands |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | not applicable: Avalonia raises a property change |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | not applicable: Avalonia raises a property change |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | not applicable: no template selector property |

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | none | Nothing: the ContentPresenter draws no background, as the phone's Transparent one draws none |
| [x] | `HorizontalContentAlignment` | `Stretch` | `ContentPresenter` stretches by default | Nothing: the item's content stretches, as the phone's |
| [x] | `VerticalContentAlignment` | `Stretch` | `ContentPresenter` stretches by default | Nothing: as above |
| [-] | `TabNavigation` | `Local` | none | not applicable: the container is a ContentPresenter |

#### Template parts (0)

_none_

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (0)

_none_
