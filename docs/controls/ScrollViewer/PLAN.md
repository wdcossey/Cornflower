# ScrollViewer, ScrollBar and Thumb: plan

[#116](https://github.com/wdcossey/CornflowerUI/issues/116). The 8.1 ScrollViewer and its thin scroll indicators:
Avalonia's `ScrollViewer`, `ScrollBar` and `Thumb` themed as the phone's, the indicators shown and hidden by a
behaviour that follows the scroll and the pointer type, as WinRT's ScrollViewer does. The worked example for Tier 2:
every later Tier 2 control (ListView, Pivot, Hub, Page) holds this ScrollViewer. It fixes the first half of
[#99](https://github.com/wdcossey/CornflowerUI/issues/99): Simple's wide grey bar on the sample's tall pages.

**The scroll physics** (deceleration, the compression at the end of the content) are measured by
[#113](https://github.com/wdcossey/CornflowerUI/issues/113) and are **pending #113**: see [Scroll physics](#scroll-physics).
This plan, the theme and its tests cover everything else.

## Where the values come from

Every value is read from the 8.1 Update 1 runtime's default styles, `R:\cornflower-reference\flash-update1\xbf-json\`
`scrollviewer.json`, `scrollbar.json` and `thumb.json`, and the values of their keys from `themeresources.json` beside
them, which also holds `ScrollViewerScrollBarlessTemplate`. They equal the SDK's design files, from which the checklists
below are generated.

**No capture of a ScrollViewer or a ScrollBar exists yet.** The reference app's catalogue has none, and the indicator
is only drawn while the content scrolls, which a forced-state capture can't show. #113 takes them (a recording of a
pan, and frames with the indicator shown); until then the theme is checked against the templates' values and WinRT's
sums, and [COMPARISON.md](COMPARISON.md) shows our frames alone.

The phone's control source was never published. Where the templates leave a behaviour to code (how long the indicator
is, where it is, which state a pointer picks), WinUI 3's source, `ScrollBar_Partial.cpp` and
`ScrollViewer_Partial.cpp` in microsoft/microsoft-ui-xaml, read on 2026-11-15, is the guide (ADR 0001, item 4: a hint,
not evidence). Each such behaviour is marked below, to be checked against #113's recording.

## The phone's ScrollViewer

`scrollviewer.json`: one unkeyed style, fifteen setters and a template.

| Setter | Value | Avalonia | Ours |
|---|---|---|---|
| `HorizontalScrollMode`, `VerticalScrollMode` | `Auto` | none: a ScrollViewer scrolls where its bar visibility isn't `Disabled` | not set |
| `IsHorizontalRailEnabled`, `IsVerticalRailEnabled` | `True` | none | not set: rails are physics (#113) |
| `IsTabStop` | `False` | `Focusable`, false by default | Avalonia's default stands |
| `ZoomMode`, `MaxZoomFactor` | `Disabled`, `2` | none: Avalonia's ScrollViewer doesn't zoom | not set |
| `HorizontalContentAlignment`, `VerticalContentAlignment` | `Left`, `Top` | `ContentControl`'s | not set: the phone's template binds them to nothing, and Avalonia's `ScrollContentPresenter` reads neither, so a setter would do nothing (rule 3) |
| `HorizontalScrollBarVisibility` | `Disabled` | the same, Avalonia's default | setter |
| `VerticalScrollBarVisibility` | `Visible` | the same; Avalonia's default is `Auto` | setter. With the indicator over the content and drawn only while scrolling, `Visible` and `Auto` look the same |
| `Padding`, `BorderThickness` | `0` | the same | setters |
| `BorderBrush`, `Background` | `Transparent` | the same | setters |

### The template

```
Border                       BorderBrush, BorderThickness: TemplateBinding
  Grid                       Background: TemplateBinding; columns *, Auto; rows *, Auto
    ScrollContentPresenter "ScrollContentPresenter"
                             Grid.RowSpan 2, Grid.ColumnSpan 2; Margin: TemplateBinding Padding
    ScrollBar "VerticalScrollBar"
                             Grid.Column 1; Orientation Vertical; HorizontalAlignment Right; IsTabStop False;
                             Maximum ScrollableHeight, Value VerticalOffset, ViewportSize ViewportHeight,
                             Visibility ComputedVerticalScrollBarVisibility
    ScrollBar "HorizontalScrollBar"
                             Grid.Row 1; Orientation Horizontal; the same for the width
```

Ours, in `src/Cornflower.Themes.Wp81/Controls/ScrollViewer.axaml`, is the same tree with Avalonia's part names:
`PART_ContentPresenter`, `PART_VerticalScrollBar`, `PART_HorizontalScrollBar`. The presenter's `ContentTemplate` is
bound as the phone's is (Avalonia's presenter takes the `Content` itself, but not the template, which Simple's
template leaves unbound). The presenter carries Avalonia's
`ScrollGestureRecognizer`, its snap point and inertia bindings, as Simple's template does. Avalonia's ScrollBar binds
its own `Maximum`, `Value`, `ViewportSize` and `Visibility` to its templated ScrollViewer, so the template sets none.

Two things follow from the phone's tree: the presenter lies under the indicators, so they are drawn over the content,
not beside it; and the Padding is the presenter's Margin, so what scrolls is clipped inside it (Simple's binds it to
the presenter's Padding, which scrolls).

The presenter has a transparent `Background` of its own (the Grid draws the ScrollViewer's), so a press anywhere on it,
between the content's parts too, reaches its gesture recogniser. On the phone DirectManipulation takes a touch anywhere
in the ScrollViewer. A press in the `Padding`, outside the presenter, isn't the presenter's, so it doesn't pan: a gap,
in Avalonia's design (the recogniser is the presenter's), seen only with a `Padding` set.

### The visual states

`ScrollingIndicatorStates`, on the Border:

| State | What it sets | Ours |
|---|---|---|
| `NoIndicator` | nothing | `PhoneScrollIndicators.IndicatorMode` `None` |
| `TouchIndicator` | both ScrollBars' `IndicatorMode` to `TouchIndicator`, a `DiscreteObjectKeyFrame` at 0, `Duration` 0.1 s | `TouchIndicator`, passed to both ScrollBars by the theme's styles |
| `MouseIndicator` | nothing | `MouseIndicator`, passed to both ScrollBars: a deliberate difference (below) |

One transition, `TouchIndicator` → `NoIndicator`: both ScrollBars' `IndicatorMode` to `None` at 0.05 s, `Duration` 0.1 s.

### `ScrollViewerScrollBarlessTemplate`

`themeresources.json`: a keyed `ControlTemplate` for `ScrollViewer`, a `Grid` with the `Background` holding the
`ScrollContentPresenter` with the `Padding` as its `Margin`, and no ScrollBars. The phone's Hub and Pivot templates set
it on their ScrollViewers; LoopingSelector's too. Ours is `Wp81ScrollViewerScrollBarlessTheme`, a keyed `ControlTheme`
in the same file, set by `Theme="{DynamicResource Wp81ScrollViewerScrollBarlessTheme}"`.

## The phone's ScrollBar

`scrollbar.json`: one unkeyed style, six setters and a template.

| Setter | Value | Resource value | Ours |
|---|---|---|---|
| `MinWidth` | `{ThemeResource ScrollBarMinThemeWidth}` | `7` | setter, `DynamicResource` |
| `MinHeight` | `{ThemeResource ScrollBarMinThemeHeight}` | `7` | setter, `DynamicResource` |
| `Background`, `Foreground`, `BorderBrush` | `Transparent` | | setters |
| `IsTabStop` | `False` | | `Focusable` false, Avalonia's default, set again |

### The template

```
Grid "Root"
  Grid "HorizontalPanningRoot"        MinWidth 53
    Rectangle "HorizontalPanningThumb" HorizontalAlignment Left; Fill ScrollBarPanningBackgroundThemeBrush;
                                      Height ScrollBarPanningThumbThemeHeight (2.4); MinWidth ScrollBarMinThemeWidth (7)
  Grid "VerticalPanningRoot"          MinHeight 53
    Rectangle "VerticalPanningThumb"  VerticalAlignment Top; Fill ScrollBarPanningBackgroundThemeBrush;
                                      Width ScrollBarPanningThumbThemeWidth (2.4); MinHeight ScrollBarMinThemeHeight (7)
```

No thumb to drag, no track, no buttons: on the phone the ScrollBar is only the indicator. WinRT's code
(`ScrollBar_Partial.cpp`) does the rest:

- **Orientation** (`OnOrientationChanged`): the panning root of the ScrollBar's orientation is visible, the other
  collapsed.
- **Length** (`UpdateIndicatorLengths`, `ConvertViewportSizeToDisplayUnits`): the panning thumb is
  `trackLength × viewport / max(1, viewport + maximum − minimum)`, rounded by layout, and at least its own `MinHeight`
  (`MinWidth`); hidden when there is nothing to scroll or it would be longer than the bar.
- **Place** (`RefreshTrackLayout`): the panning root's top (left) margin is
  `(value − minimum) / (maximum − minimum) × (trackLength − thumbLength)`.

Avalonia's `Track` does the same sums for its `Thumb` (`ComputeScrollBarLengths`: the thumb is
`trackLength × viewport / (range + viewport)`, at least the thumb's `MinHeight` when set, hidden with no range or when
longer than the track; the decrease part, before the thumb, is `(trackLength − thumbLength) × offset / range`). So each
panning root is a `Track` and each panning thumb a `Thumb` in a theme of its own, `Wp81ScrollBarPanningThumbTheme`,
which draws the Rectangle. The difference: WinRT's `max(1, …)` only matters for an extent under one pixel.

Ours, in `src/Cornflower.Themes.Wp81/Controls/ScrollBar.axaml`:

```
Panel "Root"
  Track "HorizontalPanningRoot"       MinWidth 53; hidden while :vertical
    Thumb "HorizontalPanningThumb"    Wp81ScrollBarPanningThumbTheme; Height 2.4; MinWidth 7
  Track "VerticalPanningRoot"         MinHeight 53; IsDirectionReversed, so the minimum is at the top; hidden while :horizontal
    Thumb "VerticalPanningThumb"      Wp81ScrollBarPanningThumbTheme; Width 2.4; MinHeight 7
```

The thumb is centred across the bar, as the phone's Rectangle, with its fixed width in a 7-wide root, is.

### The visual states

| Group, state | What it sets | Ours |
|---|---|---|
| `CommonStates`, `Normal` | nothing | |
| `CommonStates`, `PointerOver` | nothing | no style |
| `CommonStates`, `Disabled` | `Root.Opacity` 0.5 | `^:disabled /template/ Panel#Root` |
| `ScrollingIndicatorStates`, `TouchIndicator` | `FadeInThemeAnimation` on both panning roots | the roots at opacity 1, at once: the fade is owed |
| `ScrollingIndicatorStates`, `MouseIndicator` | nothing: the roots at their own opacity, 1 | the roots at opacity 1 |
| `ScrollingIndicatorStates`, `NoIndicator` | `FadeOutThemeAnimation` on both panning roots | the roots at opacity 0, at once: the fade is owed |

WinRT's ScrollBar picks the state from its `IndicatorMode` (`ChangeVisualState`): `MouseIndicator` for the mouse's,
`TouchIndicator` for the touch's, `NoIndicator` for `None`. A ScrollBar outside a ScrollViewer starts at `None`, so on
the phone it draws nothing.

**A ScrollBar on its own: a deliberate difference.** On the phone, a ScrollBar an app places, or one in a control's
template outside a ScrollViewer, draws nothing and can't be dragged. Ours defaults its mode to `MouseIndicator`, so it
shows the thin indicator, and takes the pointer, so its thumb can be dragged (the `Track`'s `Value` is bound both
ways). Inside the Wp81 ScrollViewer, its theme sets its bars' mode (`None` at rest) and makes them take no pointer.
Reason: such a bar, Avalonia.Controls.DataGrid's say, would otherwise vanish for good, as an app can't set the
internal mode.

The fades are theme animations, whose durations live in the OS's animation metrics, not in a template or in WinUI's
source. They are owed to #113's recording (or to `run.ps1 -Native` reading `AnimationMetrics` for `FadeIn` and
`FadeOut`, if the phone has it).

## The phone's Thumb

`thumb.json`: one unkeyed style for a Thumb an app places (the phone's own templates don't use it: its ScrollBar has
no Thumb and its Slider styles its own). Four setters, `Background` `ThumbBackgroundThemeBrush`, `BorderThickness`
0.8, `IsTabStop` False, `BorderBrush` `ThumbBorderThemeBrush`; a `Grid` holding `Background`, a `Border` in the
Thumb's own brushes, and `BackgroundPressed` over it in `ThumbPressedBackgroundThemeBrush` and
`ThumbPressedBorderThemeBrush` at opacity 0. `Pressed` sets `BackgroundPressed` to 1 and `Background` to 0;
`PointerOver` and `Disabled` set nothing. Ours, `src/Cornflower.Themes.Wp81/Controls/Thumb.axaml`, is the same, with
`:pressed`. Simple has no Thumb theme, so a Thumb in Simple's templates (TableViewColumnHeader's resizer) sets its own
template and isn't changed.

## When the indicators show, and which

WinRT's ScrollViewer code (`ScrollViewer_Partial.cpp`) decides the state; the template gives it its timeline.

1. **On a scroll.** Any change of the offset, once the ScrollViewer is loaded, shows the indicators (`ShowIndicators`
   on a view change; not during initialisation, `m_isLoaded`).
2. **On a mouse or a pen.** A pointer that isn't a touch entering or moving over the ScrollViewer shows them, and sets
   `m_preferMouseIndicators`; a touch manipulation starting or a key going down clears it. The move is WinRT's
   `OnPointerMoved` override, which sees only what reaches the ScrollViewer unhandled; a move bubbles, so an outer
   ScrollViewer shows its own indicator too. The key is `OnKeyDown`, which clears the preference before it scrolls for
   PageUp or PageDown; ours takes the key as it tunnels, before Avalonia's ScrollViewer scrolls for it. While it is set, the state
   is `MouseIndicator`; otherwise `TouchIndicator`. A touch shows nothing by itself: only the scroll it makes does.
3. **For how long.** The state's storyboard runs; when it completes, WinRT goes to `NoIndicator`
   (`IndicatorStateStoryboardCompleted`), through the transition. The phone's `TouchIndicator` storyboard lasts 0.1 s and
   the transition sets `None` at 0.05 s. Going to the state it is in does nothing, so the storyboard isn't restarted by
   a second scroll; a scroll in the transition goes back to `TouchIndicator`, a new 0.1 s. So while a scroll moves more
   often than every 50 ms, the indicator stays; it goes 50 to 150 ms after the last move, then fades. A finger held
   still mid-pan moves nothing, so the indicator goes then too, by this reading; whether the phone keeps it for the
   whole manipulation is for #113's recording.

`PhoneScrollIndicators`, internal to the theme, turned on by the ScrollViewer theme's setter, does this on
`CornflowerClock`, and sets the attached `PhoneScrollIndicators.IndicatorMode` on the ScrollViewer: its state. The
ScrollViewer theme's styles set the same property on its two ScrollBars, as the phone's state sets their
`IndicatorMode`, and the ScrollBar theme draws it. The timeline is the template's and WinUI's code; whether the phone's
indicator goes 50 to 150 ms after a pan stops is **to check against #113's recording**.

**The mouse: a deliberate difference.** The phone's template sets nothing in `MouseIndicator`, so on the phone a
mouse would show no indicator at all (the ScrollBars stay at `None`). Ours passes the state on, so the ScrollBar draws
its own `MouseIndicator` look (the thin thumb, without the fade), and gives it the touch's timeline, as the template
gives the mouse none: a desktop user sees where they are. To reverse it, delete the `MouseIndicator` style in
`ScrollViewer.axaml`.

**`AllowAutoHide`**, Avalonia's, which WinRT lacks: Avalonia's own themes keep a full bar while it is false. Ours
keeps the indicator shown (the ScrollBar theme's `[AllowAutoHide=False]` style), so an app that wants the position
always visible gets the phone's indicator, shown. Avalonia's `IsExpanded`, `ShowDelay` and `HideDelay` follow the
pointer over the ScrollBar, which takes no pointer here: they stay at rest and draw nothing.

## Avalonia's ScrollViewer and ScrollBar against the phone's

| Difference | The phone | Avalonia (Simple) | Rung |
|---|---|---|---|
| The bar | the thin indicator, 2.4 wide, in `PhoneBaseLowColor`, over the content | a wide grey bar beside the content, with arrows (#99) | 1, theme |
| The bar takes the pointer | not draggable; a touch on it pans (DirectManipulation takes the touch over the whole ScrollViewer) | a bar to drag, which takes the press, so the presenter's gesture never sees it | 1, the ScrollViewer theme sets its bars' `IsHitTestVisible` false |
| The indicator's size and place | WinRT's code (above) | `Track`'s, the same sums | 1, theme: a `Track` per panning root |
| When it shows, and which | the scroll and the pointer type pick the state | `IsExpanded` from the pointer over the bar, whatever its type | 2, `PhoneScrollIndicators`, internal to the theme, set by its setter |
| The fades | `FadeInThemeAnimation`, `FadeOutThemeAnimation` | none | owed: their durations aren't measured. Shown and hidden at once until then |
| The Padding | the presenter's Margin: what scrolls is clipped inside it | the presenter's Padding: what scrolls passes under it | 1, theme |
| `ScrollViewerScrollBarlessTemplate` | a keyed template | none | 1, a keyed theme, `Wp81ScrollViewerScrollBarlessTheme` |
| The scroll physics | DirectManipulation's | Avalonia's `ScrollGestureRecognizer` and the presenter's inertia | **pending #113**: a part class (rung 3) or a recorded deviation |
| Zoom, rails, scroll modes, headers | WinRT's | none | out of reach of a theme; not declared |

No type is substituted. Rungs 1 and 2; rung 3 waits on #113.

## Scroll physics

**Pending #113.** #113 measures the phone's pan, its deceleration after a flick and its compression at the end of the
content, on the VM. With its numbers this section will compare them with Avalonia's (`ScrollGestureRecognizer`: a
scroll starts past a threshold, inertia decays by `InertialResistance` each second until `InertialScrollSpeedEnd`; at
the end of the content the presenter stops, with no compression), and decide: a part class, a `ScrollContentPresenter`
subclass or a gesture recogniser of ours in `PART_ContentPresenter` (rung 3, so every theme holding this ScrollViewer
gets it, and the bar-less theme too), or a recorded deviation. Nothing in the theme depends on the choice: the
presenter is one part, named as Avalonia's.

## Extensions

None public. `PhoneScrollIndicators` and `PhoneIndicatorMode` are internal to the theme.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/ScrollViewerTests.cs` unless named.

1. **The ScrollViewer's style**: the setters above, not focusable, the indicators on.
   `The_ScrollViewers_Style_Sets_What_The_Phone_Sets`.
2. **Its template**: the Border, the Grid's columns and rows, the presenter over all four cells, the bars' cells and
   alignment; the indicators lie over the content. `The_Template_Is_The_Phones_Part_For_Part`.
3. **The Padding is the presenter's Margin**, clipped; the Border and the Background are drawn.
   `Padding_Insets_The_Presenter_And_The_Border_And_Background_Are_Drawn`.
4. **The ScrollBar's style**: 7 each way, transparent, not focusable; the panning roots' 53; in a ScrollViewer,
   taking no pointer. `The_ScrollBars_Style_Sets_What_The_Phone_Sets`.
5. **The indicator's size and place**: the bar 7 wide (7.0833 laid out, 17 px) at the right; the thumb 2.4 wide (2.5,
   6 px), centred in it; 320 long for 800 of 2000; 240 down at 600 of 1200; at the bottom at the end.
   `The_Indicator_Is_Sized_And_Placed_As_WinRTs_Code_Places_It`.
6. **At least 7 long, and hidden with nothing to scroll.** `The_Thumb_Is_At_Least_Seven_Long_And_Hidden_With_Nothing_To_Scroll`.
7. **The horizontal indicator**: in the second row, 7 tall, its thumb 2.4 tall; the other orientation's root hidden.
   `The_Horizontal_Indicator_Is_The_Phones_Too`.
8. **At rest, nothing is drawn.** `At_Rest_No_Indicator_Is_Drawn`.
9. **A scroll shows the touch's indicator** in `ScrollBarPanningBackgroundThemeBrush`, #737373 on black and #BFBFBF on
   white, through both ScrollBars' mode. `A_Scroll_Shows_The_Indicator_In_The_Phones_Brush`.
10. **It goes 150 ms after one scroll** (the template's 0.1 s and 0.05 s). `One_Scrolls_Indicator_Goes_After_The_Templates_150_Ms`.
11. **A scroll that keeps moving keeps it**, and it goes by the template's timeline after the last move.
    `A_Scroll_That_Keeps_Moving_Keeps_The_Indicator`.
12. **A touch drag pans and shows the touch's indicator.** `A_Touch_Drag_Shows_The_Touch_Indicator`.
13. **A drag on the indicator pans the content.** `A_Drag_On_The_Indicator_Pans_The_Content`.
14. **The pointer type picks the indicator**: a mouse over it shows the mouse's at once, and a scroll after it is the
    mouse's; a touch press gives the touch's back. `The_Pointer_Type_Picks_The_Indicator`.
15. **A key gives the touch's back**, before it scrolls: PageDown, which Avalonia's ScrollViewer handles, scrolls with
    the touch's. `A_Key_Gives_The_Touch_Indicator_Back`.
16. **No indicator while loading.** `An_Offset_Set_Before_It_Loads_Shows_Nothing`.
17. **`AllowAutoHide` off keeps it shown.** `AllowAutoHide_Off_Keeps_The_Indicator`.
18. **Disabled draws it at 0.5.** `Disabled_Draws_The_Indicator_At_Half`.
19. **Turned off, nothing follows the scroll.** `Turning_The_Indicators_Off_Leaves_No_Indicator`.
20. **The colours follow the variant.** `The_Colours_Follow_A_Change_Of_Variant`.
21. **A ScrollBar on its own** is themed, 7 wide, shown (`MouseIndicator` unless set), its thin thumb dragged by the
    pointer, draws its mode when set, and has no RepeatButton. `A_ScrollBar_On_Its_Own_Shows_And_Its_Thumb_Drags`.
22. **The bar-less theme**: no ScrollBars, the Padding as the presenter's Margin, and it scrolls.
    `The_Scrollbarless_Theme_Has_No_Bars_And_Scrolls`.
23. **The Thumb's style and its Pressed state.** `A_Thumb_Has_The_Phones_Style_And_Pressed_State`.
24. **Holes**: `ScrollBar` and `ScrollViewer` are themed (`Completeness/HolesTests`, their lines deleted from
    `Holes.txt`); a ScrollBar has no RepeatButtons left for `HoleParts` to keep Simple's
    (`Completeness/HoleTogglePartsTests`, its ScrollBar lines taken out).
25. **No global rule**: `StyleBudgetTests`, unchanged.
26. **The sample's page**: in the catalogue, every ScrollViewer on it the theme's with the thin indicators, its header
    from its title, its scroll shown (`Sample/ScrollViewerPageTests.cs`).
27. **The ContentTemplate is the ScrollViewer's**, in both themes, as the phone's template binds it.
    `The_ContentTemplate_Is_The_ScrollViewers`.
28. **A drag between the content's parts pans it**, in both themes. `A_Drag_Between_The_Contents_Parts_Pans_It`.
29. **A move the content handles shows nothing.** `A_Move_The_Content_Handles_Shows_Nothing`.
30. **A new theme mid-scroll comes in whole.** `A_New_Theme_Mid_Scroll_Comes_In_Whole`.
31. **A scroll inside the state doesn't start it again**: scrolls at 0, 40 and 80 ms, gone at 150.
    `A_Scroll_Inside_The_State_Doesnt_Start_It_Again`.
32. **A touch that doesn't scroll shows nothing.** `A_Touch_That_Doesnt_Scroll_Shows_Nothing`.
33. **Taken out of the window mid-scroll, it is at `NoIndicator`**, no timer left. `Taken_Out_Mid_Scroll_It_Is_At_NoIndicator`.

## Decided on the user's behalf

- **The mouse shows the thin indicator.** The phone's template would show a mouse nothing. Reason: a desktop user
  would otherwise have no sign of where they are. Reverse: delete the `MouseIndicator` style in `ScrollViewer.axaml`.
- **`AllowAutoHide` false keeps the indicator shown**, rather than doing nothing. Reason: it is Avalonia's switch for
  "always show the position", and no phone value contradicts it. Reverse: delete `^[AllowAutoHide=False]` from the
  ScrollBar theme's shown style.
- **The holes' ScrollViewers take this theme.** Simple's templates for ListBox, ComboBox, TextBox (in the holes),
  TreeView and the rest hold ScrollViewers with no theme of their own, so they now scroll with the phone's indicator,
  as the phone's lists do. Their content runs under the indicator (up to 7 wider, where Simple's bar stood), and a
  mouse can't drag a bar in them: a wheel or a touch scrolls them. ListView's theme (#119) and the rest will replace
  them. Reverse: add `ScrollViewer` to `HoleParts` (a shared file).
- **A ScrollBar on its own shows and can be dragged** (above), where the phone's would draw nothing. Reverse: drop
  the `OverrideDefaultValue` in `PhoneScrollIndicators`.
- **The Thumb is themed by type**, as the issue names it, though the phone's own templates don't use its style.
- **No fades until they are measured**, rather than a guessed duration.

## Dependencies and captures

- Tokens ([#19](https://github.com/wdcossey/CornflowerUI/issues/19)): all nine resources the three styles read are in
  `Tokens/`. `CornflowerClock`, `HoleParts` and the sample: merged.
- **Owed, from #113** (the reference agent, which has the VM): captures of a ScrollViewer with its indicator shown
  (vertical and horizontal, dark and light, and disabled), committed under `ref/wp81/ScrollViewer/`; a recording of a
  pan, to time when the indicator shows and goes and its fades; the physics. With them, the Compare step finishes.
- **Owed**: the device run on the Pixel 8 (the coordinator's) and the real-window run.

## Checklists

Generated on 2026-11-15 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`), by
`dotnet run --project tools/winrt-api -- checklist <Type> <Windows.winmd> <design folder>`. `[x]` is done or kept as
Avalonia's; `[-]` is left out, with its reason. The Avalonia column is mechanical and was checked.

### ScrollViewer

WinRT `Windows.UI.Xaml.Controls.ScrollViewer`; Avalonia `Avalonia.Controls.ScrollViewer`.

#### Properties (36)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `LeftHeader` | `UIElement` | none | Not declared: no Avalonia equivalent; no phone template uses it |
| [-] | `TopHeader` | `UIElement` | none | The same |
| [-] | `TopLeftHeader` | `UIElement` | none | The same |
| [x] | `IsVerticalScrollChainingEnabled` | `Boolean` | `ScrollViewer.IsScrollChainingEnabled` (one for both axes) | Avalonia's stands |
| [-] | `IsVerticalRailEnabled` | `Boolean` | none | Not declared: rails are physics, pending #113 |
| [x] | `IsScrollInertiaEnabled` | `Boolean` | `ScrollViewer.IsScrollInertiaEnabled` | Template-bound to the presenter and its gesture recogniser |
| [x] | `IsHorizontalScrollChainingEnabled` | `Boolean` | `ScrollViewer.IsScrollChainingEnabled` | Avalonia's stands |
| [-] | `IsHorizontalRailEnabled` | `Boolean` | none | As `IsVerticalRailEnabled` |
| [x] | `IsDeferredScrollingEnabled` | `Boolean` | `ScrollViewer.IsDeferredScrollingEnabled` | Avalonia's stands: it defers a thumb drag, and the indicator can't be dragged |
| [x] | `HorizontalSnapPointsAlignment` | `SnapPointsAlignment` | `ScrollViewer.HorizontalSnapPointsAlignment` | Template-bound to the presenter (Pivot needs it) |
| [-] | `HorizontalScrollMode` | `ScrollMode` | none | Not declared: Avalonia scrolls where the bar visibility isn't `Disabled` |
| [x] | `HorizontalScrollBarVisibility` | `ScrollBarVisibility` | `ScrollViewer.HorizontalScrollBarVisibility` | Setter, `Disabled`; Avalonia's ScrollBar binds its `Visibility` to it |
| [-] | `IsZoomInertiaEnabled` | `Boolean` | none | Not declared: Avalonia's ScrollViewer doesn't zoom |
| [-] | `IsZoomChainingEnabled` | `Boolean` | none | The same |
| [x] | `HorizontalSnapPointsType` | `SnapPointsType` | `ScrollViewer.HorizontalSnapPointsType` | Template-bound to the presenter |
| [-] | `MaxZoomFactor` | `Single` | none | Not declared: no zoom |
| [-] | `ZoomSnapPointsType` | `SnapPointsType` | none | The same |
| [x] | `BringIntoViewOnFocusChange` | `Boolean` | `ScrollViewer.BringIntoViewOnFocusChange` | Avalonia's stands |
| [-] | `ZoomMode` | `ZoomMode` | none | Not declared: no zoom |
| [-] | `VerticalScrollMode` | `ScrollMode` | none | As `HorizontalScrollMode` |
| [x] | `VerticalScrollBarVisibility` | `ScrollBarVisibility` | `ScrollViewer.VerticalScrollBarVisibility` | Setter, `Visible` |
| [-] | `MinZoomFactor` | `Single` | none | Not declared: no zoom |
| [x] | `VerticalSnapPointsAlignment` | `SnapPointsAlignment` | `ScrollViewer.VerticalSnapPointsAlignment` | Template-bound to the presenter |
| [x] | `VerticalSnapPointsType` | `SnapPointsType` | `ScrollViewer.VerticalSnapPointsType` | Template-bound to the presenter |
| [x] | `ScrollableWidth` (read-only) | `Double` | `ScrollViewer.ScrollBarMaximum.X` | Avalonia's stands; the ScrollBar's `Maximum` |
| [x] | `VerticalOffset` (read-only) | `Double` | `ScrollViewer.Offset.Y` | Avalonia's stands; the ScrollBar's `Value`; a change shows the indicator (Behaviour 9) |
| [x] | `ComputedHorizontalScrollBarVisibility` (read-only) | `Visibility` | the ScrollBar's `IsVisible`, which Avalonia's ScrollBar sets | Avalonia's stands |
| [x] | `ViewportHeight` (read-only) | `Double` | `ScrollViewer.Viewport.Height` | Avalonia's stands; the ScrollBar's `ViewportSize` |
| [x] | `ViewportWidth` (read-only) | `Double` | `ScrollViewer.Viewport.Width` | The same |
| [-] | `ZoomFactor` (read-only) | `Single` | none | Not declared: no zoom |
| [x] | `ComputedVerticalScrollBarVisibility` (read-only) | `Visibility` | the ScrollBar's `IsVisible` | Avalonia's stands |
| [x] | `ScrollableHeight` (read-only) | `Double` | `ScrollViewer.ScrollBarMaximum.Y` | Avalonia's stands |
| [x] | `HorizontalOffset` (read-only) | `Double` | `ScrollViewer.Offset.X` | Avalonia's stands |
| [x] | `ExtentWidth` (read-only) | `Double` | `ScrollViewer.Extent.Width` | Avalonia's stands |
| [x] | `ExtentHeight` (read-only) | `Double` | `ScrollViewer.Extent.Height` | Avalonia's stands |
| [-] | `ZoomSnapPoints` (read-only) | `IList<Single>` | none | Not declared: no zoom |

#### Events (2)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ViewChanged` | `EventHandler<ScrollViewerViewChangedEventArgs>` | `ScrollViewer.ScrollChanged` | Avalonia's stands (the sample page shows it) |
| [-] | `ViewChanging` | `EventHandler<ScrollViewerViewChangingEventArgs>` | none | Not declared: Avalonia raises no event before a view change |

#### Methods (6)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ScrollToHorizontalOffset(Double offset)` | setting `Offset` | Avalonia's stands |
| [x] | `void ScrollToVerticalOffset(Double offset)` | setting `Offset` | Avalonia's stands |
| [-] | `void ZoomToFactor(Single factor)` | none | Not declared: no zoom |
| [x] | `void InvalidateScrollInfo()` | `InvalidateMeasure` | Avalonia's stands |
| [x] | `Boolean ChangeView(Double? horizontalOffset, Double? verticalOffset, Single? zoomFactor)` | setting `Offset` | Avalonia's stands, without the zoom and the animation: physics, #113 |
| [x] | `Boolean ChangeView(Double? horizontalOffset, Double? verticalOffset, Single? zoomFactor, Boolean disableAnimation)` | setting `Offset` | The same |

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Not declared: no Avalonia equivalent; the template doesn't use it |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not declared: an `IDataTemplate` chooses by its own `Match` |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Template-bound to the presenter, as the phone's: Avalonia's presenter takes only the Content itself (Behaviour 27) |
| [x] | `Content` | `Object` | `ContentControl.Content` | Avalonia's stands |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not declared: `Presenter.Child` is Avalonia's |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Not declared: Avalonia's `OnPropertyChanged` |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | The same |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | Not declared: no `ContentTemplateSelector` |

#### Default style: setters (15)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [-] | `HorizontalScrollMode` | `Auto` | none | No member (above) |
| [-] | `VerticalScrollMode` | `Auto` | none | No member |
| [-] | `IsHorizontalRailEnabled` | `True` | none | No member: physics, #113 |
| [-] | `IsVerticalRailEnabled` | `True` | none | The same |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | Avalonia's ScrollViewer isn't focusable: its default stands (Behaviour 1) |
| [-] | `ZoomMode` | `Disabled` | none | No member: no zoom |
| [-] | `HorizontalContentAlignment` | `Left` | `ContentControl.HorizontalContentAlignment` | Not set: Avalonia's `ScrollContentPresenter` reads no content alignment, and the phone's template binds it to nothing, so a setter would do nothing |
| [-] | `VerticalContentAlignment` | `Top` | `ContentControl.VerticalContentAlignment` | The same |
| [x] | `HorizontalScrollBarVisibility` | `Disabled` | `ScrollViewer.HorizontalScrollBarVisibility` | Setter |
| [x] | `VerticalScrollBarVisibility` | `Visible` | `ScrollViewer.VerticalScrollBarVisibility` | Setter |
| [x] | `Padding` | `0` | `TemplatedControl.Padding` | Setter; the presenter's `Margin`, as the phone's (Behaviour 3) |
| [x] | `BorderThickness` | `0` | `TemplatedControl.BorderThickness` | Setter; the Border's |
| [x] | `BorderBrush` | `Transparent` | `TemplatedControl.BorderBrush` | Setter; the Border's |
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | Setter; the Grid's |
| [-] | `MaxZoomFactor` | `2` | none | No member: no zoom |

#### Template parts (3)

Avalonia's control looks for: `PART_HorizontalScrollBar`, `PART_VerticalScrollBar`, `PART_ContentPresenter`.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ScrollContentPresenter` | `ScrollContentPresenter` | `PART_ContentPresenter` | Named `PART_ContentPresenter`, over all four cells, the Padding its Margin; where #113's part class would go |
| [x] | `VerticalScrollBar` | `ScrollBar` | `PART_VerticalScrollBar` | Named `PART_VerticalScrollBar`, second column, at the right |
| [x] | `HorizontalScrollBar` | `ScrollBar` | `PART_HorizontalScrollBar` | Named `PART_HorizontalScrollBar`, second row |

#### Visual states (3)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `ScrollingIndicatorStates` | `NoIndicator` | nothing | no rule | `PhoneScrollIndicators.IndicatorMode` `None` (Behaviours 8, 10) |
| [x] | `ScrollingIndicatorStates` | `TouchIndicator` | `VerticalScrollBar.IndicatorMode = ScrollingIndicatorMode TouchIndicator`<br>`HorizontalScrollBar.IndicatorMode = ScrollingIndicatorMode TouchIndicator` | no rule | `TouchIndicator`, from `PhoneScrollIndicators`; a style sets both ScrollBars' mode (Behaviour 9) |
| [x] | `ScrollingIndicatorStates` | `MouseIndicator` | nothing | no rule | `MouseIndicator`, passed to both ScrollBars: a deliberate difference (Behaviour 14) |

#### Transitions (1)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `ScrollingIndicatorStates` | `TouchIndicator` → `NoIndicator` |  | `VerticalScrollBar.IndicatorMode: ObjectAnimationUsingKeyFrames 0:0:0.05 → ScrollingIndicatorMode None`<br>`HorizontalScrollBar.IndicatorMode: ObjectAnimationUsingKeyFrames 0:0:0.05 → ScrollingIndicatorMode None` | `PhoneScrollIndicators.HideKeyTime`, 50 ms after the state's 100 ms, on `CornflowerClock` (Behaviours 10, 11) |

#### Resources (0)

_none_

### ScrollBar

WinRT `Windows.UI.Xaml.Controls.Primitives.ScrollBar`; Avalonia `Avalonia.Controls.Primitives.ScrollBar`.

#### Properties (3)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ViewportSize` | `Double` | `ScrollBar.ViewportSize` | Avalonia's stands; template-bound to the Tracks |
| [x] | `Orientation` | `Orientation` | `ScrollBar.Orientation` | Avalonia's stands; `:vertical` and `:horizontal` hide the other root |
| [x] | `IndicatorMode` | `ScrollingIndicatorMode` | none | `PhoneScrollIndicators.IndicatorMode`, attached, internal: set by the ScrollViewer theme, drawn by the ScrollBar theme (Behaviour 21) |

#### Events (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Scroll` | `ScrollEventHandler` | `ScrollBar.Scroll` | Avalonia's stands; the indicator raises none, as the phone's doesn't |

#### Methods (0)

_none_

#### Properties, from `RangeBase` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Value` | `Double` | `RangeBase.Value` | Avalonia's stands; template-bound one way to the Tracks |
| [x] | `SmallChange` | `Double` | `RangeBase.SmallChange` | Avalonia's stands |
| [x] | `Minimum` | `Double` | `RangeBase.Minimum` | Avalonia's stands; template-bound to the Tracks |
| [x] | `Maximum` | `Double` | `RangeBase.Maximum` | The same |
| [x] | `LargeChange` | `Double` | `RangeBase.LargeChange` | Avalonia's stands |

#### Events, from `RangeBase` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ValueChanged` | `RangeBaseValueChangedEventHandler` | `RangeBase.ValueChanged` | Avalonia's stands |

#### Methods, from `RangeBase` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnMinimumChanged(Double oldMinimum, Double newMinimum)` (protected, overridable) | none | Not declared: Avalonia's `OnPropertyChanged` |
| [-] | `void OnMaximumChanged(Double oldMaximum, Double newMaximum)` (protected, overridable) | none | The same |
| [-] | `void OnValueChanged(Double oldValue, Double newValue)` (protected, overridable) | none | The same |

#### Default style: setters (6)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `MinWidth` | `{ThemeResource ScrollBarMinThemeWidth}` | `Layoutable.MinWidth` | Setter, `DynamicResource` (Behaviour 4) |
| [x] | `MinHeight` | `{ThemeResource ScrollBarMinThemeHeight}` | `Layoutable.MinHeight` | Setter, `DynamicResource` |
| [x] | `Background` | `Transparent` | `TemplatedControl.Background` | Setter |
| [x] | `Foreground` | `Transparent` | `TemplatedControl.Foreground` | Setter |
| [x] | `BorderBrush` | `Transparent` | `TemplatedControl.BorderBrush` | Setter |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | `Focusable` false, set |

#### Template parts (5)

Avalonia's control looks for: `PART_LineDownButton`, `PART_LineUpButton`, `PART_PageDownButton`, `PART_PageUpButton`.
None of them: the phone's ScrollBar has no buttons, and Avalonia's works without them.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Root` | `Grid` | none | A `Panel` named `Root`; `:disabled` sets its opacity |
| [x] | `HorizontalPanningRoot` | `Grid` | none | A `Track` named `HorizontalPanningRoot`, `MinWidth` 53 |
| [x] | `HorizontalPanningThumb` | `Rectangle` | none | A `Thumb` named `HorizontalPanningThumb` in `Wp81ScrollBarPanningThumbTheme`, which draws the Rectangle |
| [x] | `VerticalPanningRoot` | `Grid` | none | A `Track` named `VerticalPanningRoot`, `MinHeight` 53 |
| [x] | `VerticalPanningThumb` | `Rectangle` | none | A `Thumb` named `VerticalPanningThumb`, the same |

#### Visual states (6)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style; the ScrollBar takes no pointer |
| [x] | `CommonStates` | `Disabled` | `Root.Opacity: DoubleAnimation Duration=0 To=0.5` | `^:disabled` | `^:disabled /template/ Panel#Root` (Behaviour 18) |
| [x] | `ScrollingIndicatorStates` | `TouchIndicator` | `FadeInThemeAnimation on HorizontalPanningRoot`<br>`FadeInThemeAnimation on VerticalPanningRoot` | no rule | The roots at 1 by `IndicatorMode`; the fade owed (#113) |
| [x] | `ScrollingIndicatorStates` | `MouseIndicator` | nothing | no rule | The roots at 1, their own opacity |
| [x] | `ScrollingIndicatorStates` | `NoIndicator` | `FadeOutThemeAnimation on HorizontalPanningRoot (BeginTime=0)`<br>`FadeOutThemeAnimation on VerticalPanningRoot (BeginTime=0)` | no rule | The roots at 0; the fade owed (#113) |

#### Transitions (0)

_none_

#### Resources (5)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ScrollBarMinThemeWidth` | `7` | same | yes | The ScrollBar's `MinWidth`, the horizontal thumb's `MinWidth` |
| [x] | `ScrollBarMinThemeHeight` | `7` | same | yes | The ScrollBar's `MinHeight`, the vertical thumb's `MinHeight` |
| [x] | `ScrollBarPanningBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | The panning thumb's fill (Behaviour 9) |
| [x] | `ScrollBarPanningThumbThemeHeight` | `2.4` | same | yes | The horizontal thumb's `Height` |
| [x] | `ScrollBarPanningThumbThemeWidth` | `2.4` | same | yes | The vertical thumb's `Width` |

### Thumb

WinRT `Windows.UI.Xaml.Controls.Primitives.Thumb`; Avalonia `Avalonia.Controls.Primitives.Thumb`.

#### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsDragging` (read-only) | `Boolean` | `:pressed` | Avalonia's stands: no public property; `:pressed` from press to release |

#### Events (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `DragCompleted` | `DragCompletedEventHandler` | `Thumb.DragCompleted` | Avalonia's stands |
| [x] | `DragDelta` | `DragDeltaEventHandler` | `Thumb.DragDelta` | Avalonia's stands |
| [x] | `DragStarted` | `DragStartedEventHandler` | `Thumb.DragStarted` | Avalonia's stands |

#### Methods (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void CancelDrag()` | none | Not declared: Avalonia's Thumb has none; a lost capture ends the drag |

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `{ThemeResource ThumbBackgroundThemeBrush}` | `TemplatedControl.Background` | Setter, `DynamicResource` (Behaviour 23) |
| [x] | `BorderThickness` | `0.8` | `TemplatedControl.BorderThickness` | Setter |
| [x] | `IsTabStop` | `False` | `InputElement.IsTabStop` | `Focusable` false, set |
| [x] | `BorderBrush` | `{ThemeResource ThumbBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Setter, `DynamicResource` |

#### Template parts (2)

Avalonia's control looks for: none.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Border` | none | A `Border` named `Background`, in the Thumb's brushes |
| [x] | `BackgroundPressed` | `Border` | none | A `Border` named `BackgroundPressed`, at 0 until `:pressed` |

#### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | No style |
| [x] | `CommonStates` | `Pressed` | `BackgroundPressed.Opacity: DoubleAnimation Duration=0 To=1`<br>`Background.Opacity: DoubleAnimation Duration=0 To=0` | `^:pressed` | `^:pressed` setters on both Borders (Behaviour 23) |
| [x] | `CommonStates` | `Disabled` | nothing | `^:disabled` | No style |

#### Transitions (0)

_none_

#### Resources (4)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ThumbBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumHighColor} → #CCFFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumHighColor} → #BF000000` | yes | `Background` |
| [x] | `ThumbBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80000000` | yes | `BorderBrush` |
| [x] | `ThumbPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | `BackgroundPressed`'s `Background` |
| [x] | `ThumbPressedBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseMediumMidColor} → #80000000` | yes | `BackgroundPressed`'s `BorderBrush` |
