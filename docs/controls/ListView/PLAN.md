# ListView, ListViewItem, ListViewHeaderItem and GroupItem: plan

[#119](https://github.com/wdcossey/CornflowerUI/issues/119). The 8.1 ListView and its items: Avalonia's `ListBox` and
`ListBoxItem` themed as the phone's ListView and ListViewItem, their default styles and the item's template as
Avalonia `ControlTheme`s; the phone's check mode (WinRT's `SelectionMode="Multiple"`) by a behaviour; the item's press
the Button's tilt, as [#113](https://github.com/wdcossey/CornflowerUI/issues/113) measured it. ListViewHeaderItem and
GroupItem, a grouped list's parts, are deferred: Avalonia 12.1.4 has no grouping (below). Reorder is deferred.
[#142](https://github.com/wdcossey/CornflowerUI/issues/142) brought the check mode to #136's probe (runs
`20261115-2352-dev11` and `20261116-0040-dev11`, `tools/refapp/README.md`, "ListView: the check mode"): the press
on the box and off it, Extended, and a `SelectedIndex` set from code.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default styles,
`R:\cornflower-reference\flash-update1\xbf-json\listview.json` and `listviewitem.json`, and the values of their keys
from `themeresources.json` beside them. The runtime's templates and theme resources equal the SDK's design files, but
for one value, `ListViewItemDisabledThemeOpacity`: the SDK and RTM say 0.55, the Update 1 runtime 0.4 (gap register,
part A), and Update 1 is the reference, so 0.4, as `Tokens/Sizes.axaml` already holds it.

The captures **check** the theme. They are the reference app's, run `20261108-1443-dev11` on the reference profile
(device 11, 1080 × 1920 at 2.4): a `ListViewItem { Content = "list view item" }`, unselected and selected, at rest and
in each of its 21 states, and a ListView of "one", "two" and "three" with the second selected, dark and light, 86 in
all; the distinct ones are committed in [`ref/wp81/ListView/`](../../../ref/wp81/ListView/README.md). The press comes
from #113's probes, runs `20261115-1941-dev11` (12 items) and `20261115-1948-dev11` (40, so the list pans):
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "ListView: an item's press". The tick is the CheckBox's
glyph, `Wp81.Glyph.CheckMark` (#20).

## The phone's ListView

`listview.json`: one unkeyed style, 14 setters and a template.

| Setter | Value | Ours |
|---|---|---|
| `IsTabStop` | `False` | the same |
| `TabNavigation` | `Once` | `KeyboardNavigation.TabNavigation` `Once` |
| `IsSwipeEnabled` | `True` | none: Windows 8.1's cross-slide, unused on the phone |
| `HorizontalContentAlignment`, `VerticalContentAlignment` | `Stretch`, `Top` | `ListBox` has neither: the ItemsPresenter is `Stretch` and `Top`, where the phone binds them |
| `ScrollViewer.HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility` | `Disabled`, `Auto` | the same, bound to the ScrollViewer |
| `ScrollViewer.HorizontalScrollMode`, `VerticalScrollMode`, `ZoomMode` | `Disabled`, `Auto`, `Disabled` | none: Avalonia's ScrollViewer has no modes or zoom |
| `ScrollViewer.IsDeferredScrollingEnabled`, `BringIntoViewOnFocusChange` | `False`, `True` | the same, bound |
| `ItemContainerTransitions` | `AddDeleteThemeTransition`, `ReorderThemeTransition` | none (a gap) |
| `ItemsPanel` | `ItemsStackPanel`, vertical | `VirtualizingStackPanel` |

The template: a `Border` (`BorderBrush`, `Background`, `BorderThickness` bound) round a `ScrollViewer` named
`ScrollViewer`, its scroll values bound from the ListView's attached ones, round an `ItemsPresenter` with the
ListView's `Header`, `Footer` and `Padding`. Ours is the same, with the ScrollViewer named `PART_ScrollViewer`, as
Avalonia's ListBox looks for it, a plain one, so it is the phone's with its thin indicators
([ScrollViewer README](../ScrollViewer/README.md), "How a later control holds it"); Avalonia's `IsScrollChainingEnabled`,
`AllowAutoHide`, `IsScrollInertiaEnabled` and the two snap point types are bound too. `Header` and `Footer` have nothing in Avalonia to bind (a gap).

The style sets no `Foreground`; the dump reads `PhoneForegroundColor`, inherited from the page. As the CheckBox's,
ours sets it, as Wp81 doesn't theme `Window`.

## The phone's ListViewItem

`listviewitem.json`: one unkeyed style, eight setters and a template. (`ListViewBaseItem`'s implicit style, in the
runtime alone, sets the font from other keys, the background, `TabNavigation` and `IsHoldingEnabled` True;
ListViewItem's own style replaces it.)

| Setter | Value | Resource value | Dump |
|---|---|---|---|
| `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `Segoe WP` | `Segoe WP` |
| `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `12` | 12 |
| `Background` | `Transparent` | | `#00FFFFFF` |
| `TabNavigation` | `Local` | | |
| `IsHoldingEnabled` | `False` | | |
| `Margin` | `{ThemeResource ListViewItemMargin}` | `0` | |
| `HorizontalContentAlignment` | `Left` | | `Left` |
| `VerticalContentAlignment` | `Top` | | `Top` |

So a string in a ListView is small, 12 px Segoe WP: the Catalog's capture draws "one", "two" and "three" at that
size. Apps give their items a template in `ListViewItemTextBlockStyle`.

### The template

```
Border "OuterContainer"                 RenderTransformOrigin 0.5,0.5; RenderTransform ScaleTransform (reorder)
  Grid "ReorderHintContent"             Background Transparent
    Border "CheckboxTiltContainer"      Left, Top; Margin {ThemeResource ListViewItemMultiselectCheckBoxMargin} (0,9.5,0,0)
      Border "CheckboxOuterContainer"   Clip 0,0,25.5,25.5
        Grid "CheckboxContainer"        RenderTransform Translate X {ThemeResource ListViewItemContentOffsetX} (-40.5)
          Rectangle "NormalRectangle"   25.5 × 25.5; Fill CheckBoxBackgroundThemeBrush; Stroke CheckBoxBorderThemeBrush;
                                        StrokeThickness CheckBoxBorderThemeThickness (2.5)
          Path "CheckGlyph"             18.5 × 17, Stretch Fill, centred; Opacity 0; Fill CheckBoxForegroundThemeBrush
    Border "ContentContainer"
      Border "TiltContainer"
        Border "ContentBorder"          Background, BorderBrush, BorderThickness: TemplateBinding; RenderTransform Translate
          Grid
            ContentPresenter "contentPresenter"   Margin Padding; H/VAlignment H/VContentAlignment
            TextBlock "PlaceholderTextBlock"     "Xg", Opacity 0 (DataPlaceholder)
            Rectangle "PlaceholderRect"          Collapsed (DataPlaceholder)
    Border "SelectedBorder"             Opacity 0; BorderBrush ListViewItemSelectedBackgroundThemeBrush;
                                        BorderThickness GridViewItemMultiselectBorderThickness (2.5)
      Grid "SelectedCheckMark"          34 × 34, Right, Top; Opacity 0
        Path "SelectedEarmark"          Fill ListViewItemSelectedBackgroundThemeBrush
        Path "SelectedGlyph"            17 × 14.5, Right, Top, Margin 0,1,1,0; Fill ListViewItemCheckThemeBrush
```

Ours, in `src/Cornflower.Themes.Wp81/Controls/ListView.axaml`, is the same tree with these changes:

- `contentPresenter` is `PART_ContentPresenter`, the name Avalonia's `ContentControl` looks for. Its text gets the
  phone's line, 1.33 em, through `PhoneHeaderLine`: the phone's presenter recognises no access keys.
- `TiltContainer` is marked `Tilt.IsTarget`; `OuterContainer` and `ReorderHintContent` stay untilted, and the Grid's
  transparent background takes the hits across the item.
- The two translations are `RenderTransform` values, `translateX(...)`, so that a `TransformOperationsTransition` can
  run the check mode's 0.15 s. A transition takes `TransformOperations`, which a resource double can't make, so the
  two offsets are written as literals, -40.5 and 40.5, `ListViewItemContentOffsetX` and `ListViewItemContentTranslateX`;
  a test holds them equal to the keys.
- The tick is `Wp81.Glyph.CheckMark` at its own size, as the CheckBox's (#92): the phone's is 18.5 × 17 stretched and
  arranged at 18.33 × 17.08, the glyph's own size.
- **Left out:** the placeholder `TextBlock` and `Rectangle` (DataPlaceholder: Avalonia has no data virtualisation);
  the `ScaleTransform` (Reorderable, deferred with reorder); `SelectedEarmark` and `SelectedGlyph`, which only
  GridMultiSelect shows, a GridView's state a ListView never enters. They are GridViewItem's, with the earmark glyph
  (#63), in Tier 4.
- **Kept, though never drawn:** `SelectedBorder` at nought and `SelectedCheckMark`, an empty 34-square panel: they
  make an item at least 2.5 + 34 + 2.5 tall, 93.6 raw pixels, which both the phone and Avalonia lay out as 94, 39.1667.
  That is why a list of strings has its items 39.17 apart.

### The visual states

| Group | State | What it sets | Ours |
|---|---|---|---|
| CommonStates | Normal | nothing | the plain setters |
| | Pressed | `PointerDownThemeAnimation` on TiltContainer; `PointerUpThemeAnimation` on the way back | `:pressed`: `Tilt`, on TiltContainer |
| | CheckboxPressed | `PointerDownThemeAnimation` on CheckboxTiltContainer, with no transition back; the box `CheckBoxPressedBackgroundThemeBrush` (the accent), the tick `CheckBoxPressedForegroundThemeBrush` | `PhoneListMultiSelect.IsBoxPressed`, a press on the box in the check mode: the same colours, and the box's own tilt (below) |
| | Disabled | the content at `ListViewItemDisabledThemeOpacity`; the box's stroke `CheckBoxDisabledBorderThemeBrush`, the tick `CheckBoxDisabledForegroundThemeBrush`; the selected parts transparent | `:disabled`: the same (the selected parts are never drawn) |
| SelectionStates | Unselected; Selected and SelectedUnfocused | the tick, and SelectedCheckMark, at 1 | `:selected`: the tick |
| DataVirtualizationStates | DataAvailable; DataPlaceholder | the placeholders shown | left out |
| MultiSelectStates | NoMultiSelect; ListMultiSelect | CheckboxContainer to X 0, ContentBorder to X 40.5, 0.15 s each way | `PhoneListMultiSelect.IsOn`: the same, 0.15 s, linear |
| | GridMultiSelect | SelectedBorder at 1 | left out: a GridView's |
| ReorderModeStates, ReorderHintStates | five states | the reorder theme animations | deferred |

**Selected draws nothing in a ListView.** The tick turns on, but the check box is 40.5 to the left of its clip, and
`SelectedBorder`, which holds the check mark, stays at nought. So a ListView in Single mode shows no selection: the
Catalog's list, with "two" selected, draws all three alike, and the item's `selected` captures equal `default`'s,
byte for byte, but in ListMultiSelect and GridMultiSelect.

**CheckboxPressed** (#136's probe): WinRT goes there for a press **on the box** in the check mode, 0.6 to 39.7 ms
after the press. It tilts only the box, about the box's own centre, with the item's rule and depth: a press at its
centre holds -2.70°, 0.15°, -20.86, the screen-position bias. The bias is the item's: the box's `CompositeTransform`
is the item's (-0.167, 209 for item 1), so the eye sits from the item's centre towards the screen's, not from the
box's. The first frame shows the depth with almost no angle, the next overshoots, the one after settles, as an item's
press does. On release the box is back at rest the next frame: no hold and no return (the template has no transition
out of CheckboxPressed). A press **anywhere else** on the item, in the check mode, is the Single mode's Pressed: the
whole content tilts at the Single mode's angles, the box stays flat, and the release holds and returns as the
Button's.

**Ours.** `Tilt` is the Button's tilt, with its hold and return, and draws on one target; so the box's press is
`PhoneListMultiSelect`'s. A press on the box (`CheckboxTiltContainer`'s bounds), by a touch, a pen or the left button,
in the check mode, sets `IsBoxPressed`, which the theme's selectors read for the colours. Its class handler runs as
the press bubbles, after the item's `Tilt` (a tunnelling handler) has taken it, and turns the item's `Tilt` off and on
again at once: that puts the content back at rest with nothing drawn, and the press forgotten. It then steps the box's
own tilt each frame by `TiltModel`, as `Tilt` steps an item's, with the eye from the item's centre. On release, or a
lost capture (a pan), the box's transform and `IsBoxPressed` go at once. A press off the box is left to `Tilt` on
TiltContainer, as in Single mode.

## Avalonia's ListBox against the phone's ListView

| | Phone | Avalonia 12.1.4 | Rung |
|---|---|---|---|
| The look | the ListView style and ListViewItem template | `ListBox`, `ListBoxItem` | 1, theme |
| Pressed | at once on a press, in the same input dispatch (#113: 0.3 to 2.6 ms) | `:pressed` on `PointerPressed`, at once, mouse and touch (tested) | 1 |
| The tilt | the Button's rule, depth, eye and release (#113) | `Tilt`, as the Button's | 1, an existing behaviour |
| A tap | selects, `ItemClick` and `Tapped` with the release | selects on a touch's release (tested); `ItemClick` with `ListBoxOptions` | 1; `ItemClick` by `ListBoxOptions` (#132) |
| A pan | the ScrollViewer takes the pointer about 50 ms into the move; the press is cancelled, no `ItemClick` | the `ScrollGestureRecognizer` takes it once the move passes its threshold; the item loses capture, `:pressed` goes, nothing is selected (tested) | 1 |
| Dragged off where the list can't pan | the tilt follows on the item, returns about 190 ms after the finger leaves; no `ItemClick` | `Tilt` holds 182.6 ms and returns; Avalonia selects nothing on a release off the item (tested) | 1 |
| Single | tap selects one | `SelectionMode.Single` | 1 |
| Multiple | the check boxes come in; every tap, on the box or off it, checks or unchecks, at the release (#136) | `Multiple` and `Toggle` together toggle on a tap (tested); the check mode `PhoneListMultiSelect.IsOn`, set on the list by its theme and inherited by its items | 1, the theme |
| Extended | no check boxes; under a tap, Single: a tap replaces the selection, a tap on the selected item changes nothing (#136) | `SelectionMode.Multiple`: a tap replaces the selection (tested); Ctrl and Shift choose several, which draws nothing, as on the phone | 1 |
| A `SelectedIndex` set from code | scrolls at once, in the same call, by the least that brings the item into view, with no animation (#136) | `AutoScrollToSelectedItem`, on by default: the same, before the next frame (tested) | 1 |
| None | no selection | no such mode | recorded gap |
| `ItemClick`, `IsItemClickEnabled` | yes | none | `ListBoxOptions` (#132) |
| `Header`, `Footer` | scroll with the items | none | recorded gap |
| Add, delete and reorder transitions | theme transitions | none | recorded gap |
| Reorder | `CanReorderItems`, `ReorderMode` | none | deferred (the issue) |
| Grouping, ListViewHeaderItem, GroupItem | `GroupStyle`, a grouped `CollectionViewSource` | none | deferred (below) |
| The scroll physics | the phone's | Avalonia's | the ScrollViewer's recorded deviation (#116) |

### ListViewHeaderItem and GroupItem: to check

Checked: Avalonia 12.1.4 has no grouping. `ItemsControl` takes a flat list, and `Avalonia.Controls` has no
`GroupItem`, no `GroupStyle`, no collection view and no header item type (searched by name in the assembly and its
XML docs: only `HeaderedItemsControl`, `GroupBox` and the radio groups). The phone's two types are a grouped list's
parts: `ListViewHeaderItem` is the header container `ItemsStackPanel` makes for each group (a `ContentControl` with
`TiltContainer`, Pressed's tilt and no colours), and `GroupItem` the container of a group, a header `ContentControl`
over an `ItemsControl` of `GroupItems`, for the older panels. Without grouping there is nothing to put them in: a
header item in a ListBox's items would be wrapped in a `ListBoxItem` and selected like any item.

So both are deferred, with grouping (rung 5, or a ListBox subclass under rung 4 that makes header containers), to an
issue of their own beside SemanticZoom and GridViewHeaderItem (Tier 4), whose jump list is the same grouped source.
Their checklists are below, every row deferred. The reference has no capture of either: a grouped ListView with a
pressed header, and a GroupItem, are owed to that issue.

## Extensions

None. `PhoneListMultiSelect` is internal to the theme; the tilt is `Tilt`'s, already public.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/ListViewTests.cs` unless named.

1. **The ListView's style**, dark and light: `IsTabStop`, `TabNavigation`, the ScrollViewer's attached values, the
   foreground, the `VirtualizingStackPanel`, the check mode turned on. `The_Lists_Style_Sets_What_The_Phone_Read`.
2. **Its ScrollViewer is the phone's**, `PART_ScrollViewer`, with `PhoneScrollIndicators`, every attached value
   bound. `Its_ScrollViewer_Is_The_Phones_With_The_Lists_Values`.
3. **The item's style and parts**, dark and light: the eight setters but `IsHoldingEnabled`, the box's margin, fill,
   stroke and thickness, the tick's glyph, fill and opacity, `SelectedBorder`'s opacity, thickness and brush.
   `The_Items_Style_Sets_What_The_Phone_Read`.
4. **The bounds**, against the Catalog list's dump: the list at 19.1667, 12.0833; three items 39.1667 tall one under
   the other; the content 16.25 tall at the item's top left; the box at 0, 9.5833, 40.5 to the left; the tick 3.75,
   4.1667 in it, 18.3333 × 17.0833; the check mark at the right, 2.5 down. Ours is one raw pixel narrower (411.6667
   against 412.0833) and its box one wider in layout (25.8333 against 25.4167; ADR 0003).
   `The_Bounds_Are_The_Phones_Within_Avalonias_Rounding`.
5. **A string's line is the phone's**, 1.33 em at 12, and an underscore is drawn.
   `A_Strings_Line_Is_The_Phones_And_An_Underscore_Is_Drawn`.
6. **A single selection draws nothing**: a list with "two" selected is the same frame as one with none, dark and
   light, as the Catalog's capture shows. `A_Single_Selection_Draws_Nothing`.
7. **The check mode is `Multiple` with `Toggle`** (WinRT's Multiple), with or without `AlwaysSelected`, and nothing
   else (seven modes): `Multiple` alone, WinRT's Extended, has no boxes. `The_Check_Mode_Is_Multiple_With_Toggle`.
8. **Every item follows the list's mode**, inherited: the realised ones when it changes, those made or recycled as the
   list scrolls, an item that is its own container, `Every_Item_Follows_The_Lists_Mode`; one the list recycled while
   the mode changed comes back in the new mode with no slide,
   `A_Recycled_Item_Comes_Back_In_The_Lists_Mode_Without_A_Slide`; the focused one the panel keeps out of view,
   `A_Focused_Item_Scrolled_Away_Follows_The_Lists_Mode`; and a list in an item has its own,
   `A_List_In_An_Item_Has_Its_Own_Mode`.
9. **Outside a list, or in a list in an app's own theme, an item isn't in the check mode**, an item that is its own
   container included. `An_Item_Outside_A_List_Or_In_Another_Theme_Isnt_In_The_Check_Mode`,
   `Own_Items_Lose_The_Check_Mode_With_The_Lists_Theme`.
10. **In the check mode the box comes in and the content goes over**, -40.5 to 0 and 0 to 40.5, the tokens' values,
    by 0.15 s linear transitions, and back. `In_The_Check_Mode_The_Box_Comes_In_And_The_Content_Goes_Over`.
11. **The box is drawn where the phone draws it** in the check mode, against the ListMultiSelect PNGs, dark and light,
    empty or with the tick, `In_The_Check_Mode_The_Box_Is_Drawn_Where_The_Phone_Draws_It`; and at rest it is clipped
    away, nothing drawn left of an item on its own, as `base` shows, `At_Rest_The_Box_Is_Clipped_Away`.
12. **In the check mode a tap checks and unchecks**, `In_The_Check_Mode_A_Tap_Checks_And_Unchecks`; **every tap
    toggles its item**, on the box or off it, the probe's eight taps in its order,
    `Every_Tap_In_The_Check_Mode_Toggles_Its_Item`.
13. **A tap anywhere on the item selects it**, with no Background of its own.
    `A_Tap_Anywhere_On_The_Item_Selects_It_Even_With_No_Background`.
14. **A press tilts TiltContainer inside an untilted root.** `A_Press_Tilts_The_Content_Inside_An_Untilted_Root`.
15. **CheckboxPressed**, a press on the box: the accent box, the tick over it, the box tilted and the content not,
    dark and light, `A_Press_On_The_Box_Fills_It_And_Tilts_The_Box_Alone`; it settles at the probe's tilt within 0.2°
    and 0.35 of depth (our box is 0.4 px wider, ADR 0003), the first frame the depth with almost no angle,
    `A_Press_On_The_Box_Settles_At_The_Probes_Reading`; on release it is at rest at once, its colours too, with no
    hold or return, `On_Release_The_Box_Is_At_Rest_At_Once`; a pan from it puts it at rest,
    `A_Pan_From_The_Box_Puts_It_At_Rest_At_Once`. Outside the check mode a press changes no colour,
    `Outside_The_Check_Mode_A_Press_Changes_No_Colour`.
15a. **In the check mode a press off the box is the Single mode's**: four probed presses (the centre, 95% across, 10%
    and 90% down) at the phone's tilts on TiltContainer, the box flat and unfilled, the release holding and returning.
    `In_The_Check_Mode_A_Press_Off_The_Box_Is_The_Single_Modes`.
15b. **Leaving the check mode mid-press** puts the box at rest. `Leaving_The_Check_Mode_Puts_A_Pressed_Box_At_Rest`.
15c. **Extended under a tap is Single**, with no boxes, each press tilting the content. `Extended_Under_A_Tap_Is_Single`.
15d. **A `SelectedIndex` set from code scrolls at once by the least**, the probe's 30, 5 and 39 to its offsets, and
    doesn't move on the frames after. `A_SelectedIndex_Set_From_Code_Scrolls_At_Once_By_The_Least`.
16. **Each probed press settles at the phone's tilt**, ten presses (centres, 5%, 25%, 75% and 95% across, 10% and
    90% down) within 0.1° and 0.15 of depth. `A_Held_Press_Settles_At_The_Probes_Reading`.
17. **The press is at once, and a tap selects on its release.** `The_Press_Is_At_Once_And_A_Tap_Selects_On_Its_Release`.
18. **The release holds 182.6 ms, then eases back over 167.0 ms.** `A_Release_Holds_Then_Eases_Back_As_The_Buttons`.
19. **A pan takes the press**: the item loses the pointer, its tilt holds and returns from then, nothing is selected.
    `A_Pan_Takes_The_Press_The_Tilt_Returns_And_Nothing_Is_Selected`.
20. **Dragged off where the list can't pan**, the tilt follows on the item, then holds and returns, and nothing is
    selected. `Dragged_Off_The_Item_The_Tilt_Returns_And_Nothing_Is_Selected`.
21. **A press moved 10 px sideways is still a tap**, the tilt following. `A_Press_Moved_Ten_Pixels_Sideways_Is_Still_A_Tap`.
22. **Disabled** dims the content to 0.4 (its fullest ink 0.4 of the enabled text's) and the box's stroke and tick to
    `PhoneBaseLowColor`. `Disabled_Dims_The_Content_And_The_Box`.
23. **The colours follow the variant**; **keyboard focus and the pointer over an item draw nothing.**
    `The_Colours_Follow_A_Change_Of_Variant`, `Keyboard_Focus_And_The_Pointer_Over_Draw_Nothing`.
24. **Holes**: `ListBox` and `ListBoxItem` are themed (`Completeness/HolesTests`, their lines deleted from
    `Holes.txt`). **No global rule**: `StyleBudgetTests`, unchanged.
25. **The sample's page** (`Sample/ListViewPageTests.cs`): in the catalogue after the ScrollViewer's, every list on
    it the theme's, its header, a tap's selection shown, and "select" putting the list in the check mode.

## Decided on the user's behalf

- **The ListView's look is `ListBox`'s own**, keyed by type, not a keyed theme an app opts into (as PasswordBox's is).
  An 8.1 app's list is a ListView; the phone's ListBox is kept for 8.0's apps and is a later tier's (Tier 4), which can
  take the key `ListBox` by name, as the mapping rules do for a WinRT control Avalonia has under another name. To
  reverse: key these themes `ListView` and `ListViewItem`, set `ItemContainerTheme` in the first, and give `ListBox` the
  phone's ListBox look.
- **WinRT's `Multiple` is Avalonia's `Multiple, Toggle`; `Extended` is `Multiple`, with no check boxes** (#142).
  #130 showed the boxes for `Multiple` too, as the phone's `Extended` wasn't read and a selection of several would
  otherwise draw nothing. #136 read it: the phone's `Extended` has no boxes (leaving Multiple for it took every item to
  `NoMultiSelect`), and a tap there is Single's, which Avalonia's `Multiple` already does. Boxes that a tap doesn't
  toggle would say what the list doesn't do. So a selection of several made with Ctrl or Shift draws nothing, as a
  ListView's Single selection draws nothing. To reverse: put `1` and `5` back in the ListBox theme's check-mode
  selector.
- **The box's press is "on the box" within `CheckboxTiltContainer`'s bounds**, the 25.5 square (#142). The probe
  pressed its centre and the item's text; where between them the phone's edge is wasn't read. To reverse: test
  another part's bounds in `PhoneListMultiSelect.OnPressed`.
- **A move during the box's press doesn't re-aim its tilt** (#142): not read, and a drag on the box becomes a pan,
  which ends the press. To reverse: follow `PointerMoved` in `PhoneListMultiSelect`, as `Tilt` does.
- **`IsHoldingEnabled="False"` isn't set.** In Avalonia a hold is how a touch opens an item's `ContextFlyout`; turning
  it off on the item would take that from apps. To reverse: add the setter.
- **Holes keep Wp81's list.** Simple's `AutoCompleteBox` and the managed file dialog hold a plain `ListBox`, which now
  takes the phone's look, as the theme sets no `HoleParts.FallBack`. The phone's own AutoSuggestBox shows its
  suggestions in a ListView (`AutoSuggestListViewItemMargin`), so the phone's list there is the nearer. To reverse:
  set `wp81:HoleParts.FallBack` on the `ListBox` theme (its items would still be Wp81's: `HoleParts` follows template
  parts, and items have no templated parent). Cost, from the first review: there, a selection made with the keyboard
  or the mouse draws nothing, as a ListView item's doesn't outside the check mode, until those holes get themes of
  their own (AutoCompleteBox is Tier 3's AutoSuggestBox, whose suggestions the phone doesn't select either).
- **ListViewHeaderItem and GroupItem are deferred** with grouping (above). To reverse: build grouping first.
- **The check mode's two offsets are literals** in the theme (above), held to the keys by a test.

## Dependencies and captures

- #116 ScrollViewer (`PhoneScrollIndicators`), #113's press readings, `Tilt` (#22, #65), `PhoneHeaderLine` (#111), the
  tokens (#19) and the glyphs (#20, #63): merged. Every resource the two styles read is in `Tokens/`.
- Captures used: the item at rest, Pressed, Disabled and ListMultiSelect (unselected and selected), the list at rest,
  dark and light, run `20261108-1443-dev11`. The rest are byte for byte `base`, or states left out or deferred
  ([`ref/wp81/ListView/README.md`](../../../ref/wp81/ListView/README.md)).
- **The Pressed capture is a forced state with no finger**: the tilt is ±17.19°, which no touch gives; the probes
  give the tilt.
- **Read since** (#136, runs `20261115-2352-dev11` and `20261116-0040-dev11`): the check mode's press on the box and
  off it, every tap toggling, `Extended` and `None` under a tap, and a `SelectedIndex` set from code (#142 follows them;
  `None` stays a gap, as Avalonia has no such mode).
- **Owed:** captures of a grouped ListView (ListViewHeaderItem at rest and pressed) and a GroupItem, for grouping's
  issue; the device run on the Pixel 8 of #142's change.

## Checklists

Generated on 2026-11-15 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with
its reason. The generator matches Avalonia's control by name, so it found none for these four; the Avalonia column is
filled by hand against `ListBox` and `ListBoxItem`.

### ListView

> Generated by `dotnet run --project tools/winrt-api -- checklist ListView <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ListView`; Avalonia none: a new control (rung 5).

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `ListViewBase` (20)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `SemanticZoomOwner` | `SemanticZoom` | none | not applicable: SemanticZoom is a later tier's (Tier 4) |
| [-] | `IsZoomedInView` | `Boolean` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `IsActiveView` | `Boolean` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `ShowsScrollingPlaceholders` | `Boolean` | none | not applicable: Avalonia has no data virtualisation placeholders |
| [-] | `FooterTransitions` | `TransitionCollection` | none | gap: no Footer (below) |
| [-] | `FooterTemplate` | `DataTemplate` | none | gap: as Footer |
| [-] | `Footer` | `Object` | none | gap: Avalonia's ListBox has no footer that scrolls with the items; an app puts one round the list |
| [-] | `IsSwipeEnabled` | `Boolean` | none | not applicable: Windows 8.1's cross-slide select; the phone's style sets it and nothing on the phone uses it |
| [x] | `IsItemClickEnabled` | `Boolean` | none | `ListBoxOptions.IsItemClickEnabled` (#132, [ItemClick](../ItemClick/README.md)) |
| [-] | `IncrementalLoadingTrigger` | `IncrementalLoadingTrigger` | none | not applicable: Avalonia has no ISupportIncrementalLoading |
| [-] | `IncrementalLoadingThreshold` | `Double` | none | not applicable: incremental loading |
| [-] | `HeaderTransitions` | `TransitionCollection` | none | gap: no Header (below) |
| [-] | `HeaderTemplate` | `DataTemplate` | none | gap: as Header |
| [-] | `Header` | `Object` | none | gap: Avalonia's ListBox has no header that scrolls with the items (ControlHeader is a control's fixed header, not this) |
| [-] | `DataFetchSize` | `Double` | none | not applicable: incremental loading |
| [-] | `CanReorderItems` | `Boolean` | none | deferred: reorder |
| [-] | `CanDragItems` | `Boolean` | none | deferred: with reorder |
| [x] | `SelectionMode` | `ListViewSelectionMode` | `SelectionMode` (flags) | Mapped: Single is `Single`; Multiple is `Multiple, Toggle`, the check mode (`PhoneListMultiSelect`); Extended is `Multiple`, with no check boxes, as the phone's; None has no mode (a gap) |
| [x] | `SelectedItems` (read-only) (not a dependency property) | `IList<Object>` | `SelectedItems` | Avalonia's API stands |
| [-] | `ReorderMode` | `ListViewReorderMode` | none | deferred: with reorder |

#### Events, from `ListViewBase` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `DragItemsStarting` | `DragItemsStartingEventHandler` | none | deferred: with reorder |
| [x] | `ItemClick` | `ItemClickEventHandler` | none | `ListBoxOptions.ItemClick` (#132) |
| [-] | `ContainerContentChanging` | `TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>` | `ContainerPrepared`, nearest | not applicable: phased rendering has no equivalent |

#### Methods, from `ListViewBase` (12)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ScrollIntoView(Object item)` | `ScrollIntoView(object)` | Avalonia's API stands |
| [x] | `void SelectAll()` | `SelectAll()` | Avalonia's API stands |
| [-] | `IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync()` | none | not applicable: incremental loading |
| [-] | `void ScrollIntoView(Object item, ScrollIntoViewAlignment alignment)` | none | gap: Avalonia brings an item into view with no alignment |
| [-] | `void SetDesiredContainerUpdateDuration(TimeSpan duration)` | none | not applicable: phased rendering |
| [-] | `void InitializeViewChange()` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void CompleteViewChange()` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void MakeVisible(SemanticZoomLocation item)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |
| [-] | `void CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)` | none | not applicable: SemanticZoom (Tier 4) |

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
| [x] | `ItemsPanelRoot` (read-only) (not a dependency property) | `Panel` | `ItemsPanelRoot` | Avalonia's API stands |
| [x] | `ItemsSource` | `Object` | `ItemsSource` | Avalonia's API stands |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsPanel` | Themed: a `VirtualizingStackPanel` for the phone's `ItemsStackPanel` |
| [x] | `ItemTemplateSelector` | `DataTemplateSelector` | `ItemTemplate` (an `IDataTemplate` chooses) | Avalonia's API stands |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemTemplate` | Avalonia's API stands |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | gap: AddDelete and Reorder theme transitions; Avalonia has no implicit item animations (ARCHITECTURE, Theme animations) |
| [x] | `ItemContainerStyleSelector` | `StyleSelector` | `ItemContainerTheme` | Avalonia's API stands (one theme, no selector) |
| [x] | `ItemContainerStyle` | `Style` | `ItemContainerTheme` | Avalonia's API stands |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | deferred: grouping, with ListViewHeaderItem and GroupItem (below) |
| [x] | `DisplayMemberPath` | `String` | `DisplayMemberBinding` | Avalonia's API stands |
| [-] | `GroupStyle` (read-only) (not a dependency property) | `IObservableVector<GroupStyle>` | none | deferred: grouping |
| [-] | `IsGrouping` (read-only) | `Boolean` | none | deferred: grouping |
| [x] | `ItemContainerGenerator` (read-only) (not a dependency property) | `ItemContainerGenerator` | `ItemContainerGenerator` | Avalonia's API stands |
| [x] | `Items` (read-only) (not a dependency property) | `ItemCollection` | `Items` | Avalonia's API stands |

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

#### Default style: setters (14)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsTabStop` | `False` | `IsTabStop` | Themed: False |
| [x] | `TabNavigation` | `Once` | `KeyboardNavigation.TabNavigation` | Themed: Once |
| [-] | `IsSwipeEnabled` | `True` | none | not applicable: Windows 8.1's cross-slide select; the phone's style sets it and nothing on the phone uses it |
| [x] | `HorizontalContentAlignment` | `Stretch` | none on `ListBox` | Themed: the ItemsPresenter stretches, as the phone binds it |
| [x] | `VerticalContentAlignment` | `Top` | none on `ListBox` | Themed: the ItemsPresenter is at the top, as the phone binds it |
| [x] | `ScrollViewer.HorizontalScrollBarVisibility` | `Disabled` | `ScrollViewer.HorizontalScrollBarVisibility (attached)` | Themed: Disabled, bound to the ScrollViewer |
| [x] | `ScrollViewer.VerticalScrollBarVisibility` | `Auto` | `ScrollViewer.VerticalScrollBarVisibility (attached)` | Themed: Auto, bound to the ScrollViewer |
| [-] | `ScrollViewer.HorizontalScrollMode` | `Disabled` | none | not applicable: Avalonia's ScrollViewer has no scroll modes (ScrollViewer README) |
| [-] | `ScrollViewer.VerticalScrollMode` | `Auto` | none | not applicable: as HorizontalScrollMode |
| [-] | `ScrollViewer.ZoomMode` | `Disabled` | none | not applicable: no zoom |
| [x] | `ScrollViewer.IsDeferredScrollingEnabled` | `False` | `ScrollViewer.IsDeferredScrollingEnabled (attached)` | Themed: False, bound to the ScrollViewer |
| [x] | `ScrollViewer.BringIntoViewOnFocusChange` | `True` | `ScrollViewer.BringIntoViewOnFocusChange (attached)` | Themed: True, bound to the ScrollViewer |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | gap: AddDelete and Reorder theme transitions; Avalonia has no implicit item animations (ARCHITECTURE, Theme animations) |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsPanel` | Themed: a `VirtualizingStackPanel` for the phone's `ItemsStackPanel` |

#### Template parts (1)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ScrollViewer` | `ScrollViewer` | `PART_ScrollViewer` | Themed: a plain ScrollViewer, the phone's with its indicators, values bound |

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (0)

_none_

### ListViewItem

> Generated by `dotnet run --project tools/winrt-api -- checklist ListViewItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ListViewItem`; Avalonia none: a new control (rung 5).

#### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `TemplateSettings` (read-only) (not a dependency property) | `ListViewItemTemplateSettings` | none | not applicable: its one value, DragItemsCount, is reorder's |

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
| [-] | `ContentTransitions` | `TransitionCollection` | none | not applicable: Avalonia has no ContentTransitions (ARCHITECTURE) |
| [x] | `ContentTemplateSelector` | `DataTemplateSelector` | `ContentTemplate` (an `IDataTemplate` chooses) | Avalonia's API stands |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentTemplate` | Avalonia's API stands |
| [x] | `Content` | `Object` | `Content` | Avalonia's API stands |
| [x] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | `Presenter.Child` | Avalonia's API stands |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |
| [x] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | `OnPropertyChanged` | Avalonia's API stands |

#### Default style: setters (8)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | `FontFamily` | Themed: ContentControlThemeFontFamily |
| [x] | `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | `FontSize` | Themed: ControlContentThemeFontSize, 12 |
| [x] | `Background` | `Transparent` | `Background` | Themed: Transparent |
| [x] | `TabNavigation` | `Local` | `KeyboardNavigation.TabNavigation` | Themed: Local |
| [-] | `IsHoldingEnabled` | `False` | `IsHoldingEnabled` | deliberate difference: not set, so a long press still opens an item's ContextFlyout, which Avalonia raises from the hold |
| [x] | `Margin` | `{ThemeResource ListViewItemMargin}` | `Margin` | Themed: ListViewItemMargin, 0 |
| [x] | `HorizontalContentAlignment` | `Left` | `HorizontalContentAlignment` | Themed: Left |
| [x] | `VerticalContentAlignment` | `Top` | `VerticalContentAlignment` | Themed: Top |

#### Template parts (20)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `OuterContainer` | `Border` | `Border#OuterContainer` | Themed: the root, untilted |
| [-] | `ContentScaleTransform` | `ScaleTransform` | none | deferred: Reorderable's scale, with reorder |
| [x] | `ReorderHintContent` | `Grid` | `Grid#ReorderHintContent` | Themed: transparent, takes the hits |
| [x] | `CheckboxTiltContainer` | `Border` | `Border#CheckboxTiltContainer` | Themed: CheckboxPressed's tilt target |
| [x] | `CheckboxOuterContainer` | `Border` | `Border#CheckboxOuterContainer` | Themed: its 25.5 clip |
| [x] | `CheckboxContainer` | `Grid` | `Grid#CheckboxContainer` | Themed: at -40.5, at 0 in the check mode |
| [x] | `CheckboxContainerTranslateTransform` | `TranslateTransform` | CheckboxContainer's `RenderTransform` | Themed: a TransformOperations, for the transition |
| [x] | `NormalRectangle` | `Rectangle` | `Rectangle#NormalRectangle` | Themed |
| [x] | `CheckGlyph` | `Path` | `Path#CheckGlyph` | Themed: `Wp81.Glyph.CheckMark` |
| [x] | `ContentContainer` | `Border` | `Border#ContentContainer` | Themed |
| [x] | `TiltContainer` | `Border` | `Border#TiltContainer` | Themed: `Tilt.IsTarget` |
| [x] | `ContentBorder` | `Border` | `Border#ContentBorder` | Themed |
| [x] | `ContentBorderTranslateTransform` | `TranslateTransform` | ContentBorder's `RenderTransform` | Themed: 40.5 across in the check mode |
| [x] | `contentPresenter` | `ContentPresenter` | `PART_ContentPresenter` | Themed: Avalonia's name |
| [-] | `PlaceholderTextBlock` | `TextBlock` | none | not applicable: DataPlaceholder; Avalonia has no data virtualisation |
| [-] | `PlaceholderRect` | `Rectangle` | none | not applicable: as PlaceholderTextBlock |
| [x] | `SelectedBorder` | `Border` | `Border#SelectedBorder` | Themed: at nought, for the item's 39.17 minimum |
| [x] | `SelectedCheckMark` | `Grid` | `Panel#SelectedCheckMark` | Themed: empty, 34 square, for the minimum |
| [-] | `SelectedEarmark` | `Path` | none | left out: drawn only in GridMultiSelect, a GridView's state; GridViewItem's (Tier 4) |
| [-] | `SelectedGlyph` | `Path` | none | left out: as SelectedEarmark |

#### Visual states (20)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | Themed |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on TiltContainer` | `^:pressed` | Themed: the tilt on TiltContainer |
| [x] | `CommonStates` | `CheckboxPressed` | `PointerDownThemeAnimation on CheckboxTiltContainer`<br>`NormalRectangle.Fill = {ThemeResource CheckBoxPressedBackgroundThemeBrush}`<br>`CheckGlyph.Fill = {ThemeResource CheckBoxPressedForegroundThemeBrush}` | `^[(wp81|PhoneListMultiSelect.IsBoxPressed)=True]` | Themed: a press on the box in the check mode; `PhoneListMultiSelect` tilts CheckboxTiltContainer, with no return |
| [x] | `CommonStates` | `Disabled` | `contentPresenter.Opacity: DoubleAnimation Duration=0 To={ThemeResource ListViewItemDisabledThemeOpacity}`<br>`NormalRectangle.Stroke = {ThemeResource CheckBoxDisabledBorderThemeBrush}`<br>`CheckGlyph.Fill = {ThemeResource CheckBoxDisabledForegroundThemeBrush}`<br>`SelectedBorder.BorderBrush = Transparent`<br>`SelectedEarmark.Fill = Transparent`<br>`SelectedGlyph.Fill = Transparent` | `^:disabled` | Themed (the selected parts' Transparent: never drawn in a ListView anyway) |
| [x] | `SelectionStates` | `Unselected` | nothing | the theme's plain setters | Themed |
| [x] | `SelectionStates` | `Selected` | `CheckGlyph.Opacity: DoubleAnimation Duration=0 To=1`<br>`SelectedCheckMark.Opacity: DoubleAnimation Duration=0 To=1` | `^:selected` | Themed: the tick |
| [x] | `SelectionStates` | `SelectedUnfocused` | `CheckGlyph.Opacity: DoubleAnimation Duration=0 To=1`<br>`SelectedCheckMark.Opacity: DoubleAnimation Duration=0 To=1` | `^:selected` | Themed: the same as Selected |
| [x] | `DataVirtualizationStates` | `DataAvailable` | nothing | the theme's plain setters | Themed: always |
| [-] | `DataVirtualizationStates` | `DataPlaceholder` | `PlaceholderTextBlock.Visibility = Visible`<br>`PlaceholderRect.Visibility = Visible` | none | not applicable: no data virtualisation |
| [x] | `MultiSelectStates` | `NoMultiSelect` | nothing | the theme's plain setters | Themed |
| [x] | `MultiSelectStates` | `ListMultiSelect` | `CheckboxContainerTranslateTransform.X: DoubleAnimation To=0 Duration=0`<br>`ContentBorderTranslateTransform.X: DoubleAnimation To={ThemeResource ListViewItemContentTranslateX} Duration=0` | `^[(wp81|PhoneListMultiSelect.IsOn)=True]` | Themed: set on the list by the ListView theme, inherited |
| [-] | `MultiSelectStates` | `GridMultiSelect` | `SelectedBorder.Opacity: DoubleAnimation Duration=0 To=1` | none | not applicable: a GridView's state (Tier 4) |
| [-] | `ReorderModeStates` | `ReorderEnabled` | `DropTargetItemThemeAnimation on OuterContainer` | none | deferred: reorder |
| [-] | `ReorderModeStates` | `Reorderable` | `ContentScaleTransform.ScaleX: DoubleAnimationUsingKeyFrames 00:00:00.075 → 1.05, 00:00:00.2 → 1.0`<br>`ContentScaleTransform.ScaleY: DoubleAnimationUsingKeyFrames 00:00:00.075 → 1.05, 00:00:00.2 → 1.0` | none | deferred: reorder |
| [x] | `ReorderModeStates` | `ReorderDisabled` | nothing | the theme's plain setters | Themed: always |
| [x] | `ReorderHintStates` | `NoReorderHint` | nothing | the theme's plain setters | Themed: always |
| [-] | `ReorderHintStates` | `BottomReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset={ThemeResource ListViewItemReorderHintThemeOffset} Direction=Bottom)` | none | deferred: reorder |
| [-] | `ReorderHintStates` | `RightReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset={ThemeResource ListViewItemReorderHintThemeOffset} Direction=Right)` | none | deferred: reorder |
| [-] | `ReorderHintStates` | `TopReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset=0 Direction=Top)` | none | deferred: reorder |
| [-] | `ReorderHintStates` | `LeftReorderHint` | `DragOverThemeAnimation on ReorderHintContent (ToOffset=0 Direction=Left)` | none | deferred: reorder |

#### Transitions (5)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on TiltContainer` | Themed: the tilt's release, the Button's (a 182.6 ms hold, then 167.0 ms), as #113 read it |
| [x] | `MultiSelectStates` | `ListMultiSelect` → `NoMultiSelect` | `0:0:0.15` | nothing | Themed: a 0.15 s linear `TransformOperationsTransition` on both parts |
| [x] | `MultiSelectStates` | `NoMultiSelect` → `ListMultiSelect` | `0:0:0.15` | nothing | Themed: the same transitions |
| [-] | `ReorderModeStates` | `ReorderEnabled` → `ReorderDisabled` | `00:00:00.2` | nothing | deferred: reorder |
| [-] | `ReorderHintStates` | `*` → `NoReorderHint` | `0:0:0.2` | nothing | deferred: reorder |

#### Resources (20)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | Used, by DynamicResource |
| [x] | `ControlContentThemeFontSize` | `12` | same | yes | Used, by DynamicResource |
| [x] | `ListViewItemMargin` | `0` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxPressedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxPressedForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | Used, by DynamicResource |
| [x] | `ListViewItemDisabledThemeOpacity` | `0.4` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | Used, by DynamicResource |
| [x] | `CheckBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | Used, by DynamicResource |
| [x] | `ListViewItemContentTranslateX` | `40.5` | same | yes | Used, as the literal 40.5; a test holds the two equal |
| [-] | `ListViewItemReorderHintThemeOffset` | `32` | same | yes | deferred: reorder |
| [x] | `ListViewItemMultiselectCheckBoxMargin` | `0,9.5,0,0` | same | yes | Used, by DynamicResource |
| [x] | `ListViewItemContentOffsetX` | `-40.5` | same | yes | Used, as the literal -40.5 (a transition takes TransformOperations); a test holds the two equal |
| [x] | `CheckBoxBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | Used, by DynamicResource |
| [x] | `CheckBoxBorderThemeThickness` | `2.5` | same | yes | Used, by DynamicResource |
| [x] | `CheckBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | Used, by DynamicResource |
| [-] | `ListViewItemPlaceholderBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhonePageBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhonePageBackgroundColor} → #FFFFFFFF` | yes | not applicable: PlaceholderRect |
| [x] | `ListViewItemSelectedBackgroundThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | Used, by DynamicResource |
| [x] | `GridViewItemMultiselectBorderThickness` | `2.5` | same | yes | Used, by DynamicResource |
| [-] | `ListViewItemCheckThemeBrush` | `SolidColorBrush {ThemeResource PhoneTextOverAccentColor} → #FFFFFFFF` | same | yes | left out: SelectedGlyph's |

### ListViewHeaderItem

Every row deferred with grouping (above).

> Generated by `dotnet run --project tools/winrt-api -- checklist ListViewHeaderItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ListViewHeaderItem`; Avalonia none: a new control (rung 5).

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `ListViewBaseHeaderItem` (0)

_none_

#### Events, from `ListViewBaseHeaderItem` (0)

_none_

#### Methods, from `ListViewBaseHeaderItem` (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | deferred: grouping |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | deferred: grouping |
| [-] | `ContentTemplate` | `DataTemplate` | none | deferred: grouping |
| [-] | `Content` | `Object` | none | deferred: grouping |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | deferred: grouping |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | deferred: grouping |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | deferred: grouping |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | deferred: grouping |

#### Default style: setters (5)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [-] | `FontFamily` | `{ThemeResource ContentControlThemeFontFamily}` | none | deferred: grouping |
| [-] | `FontSize` | `{ThemeResource ControlContentThemeFontSize}` | none | deferred: grouping |
| [-] | `Background` | `Transparent` | none | deferred: grouping |
| [-] | `HorizontalContentAlignment` | `Left` | none | deferred: grouping |
| [-] | `VerticalContentAlignment` | `Top` | none | deferred: grouping |

#### Template parts (1)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [-] | `TiltContainer` | `Border` | `PART_TiltContainer` | deferred: grouping |

#### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [-] | `CommonStates` | `Disabled` | nothing | `^:disabled` | deferred: grouping |
| [-] | `CommonStates` | `Normal` | nothing | the theme's plain setters | deferred: grouping |
| [-] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on TiltContainer` | `^:pressed` | deferred: grouping |
| [-] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | deferred: grouping |

#### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [-] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on TiltContainer` | deferred: grouping |
| [-] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on TiltContainer` | deferred: grouping |
| [-] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on TiltContainer` | deferred: grouping |

#### Resources (2)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [-] | `ContentControlThemeFontFamily` | `Segoe WP` | same | yes | deferred: grouping |
| [-] | `ControlContentThemeFontSize` | `12` | same | yes | deferred: grouping |

### GroupItem

Every row deferred with grouping (above).

> Generated by `dotnet run --project tools/winrt-api -- checklist GroupItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.GroupItem`; Avalonia none: a new control (rung 5).

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | deferred: grouping |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | deferred: grouping |
| [-] | `ContentTemplate` | `DataTemplate` | none | deferred: grouping |
| [-] | `Content` | `Object` | none | deferred: grouping |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | deferred: grouping |

#### Events, from `ContentControl` (0)

_none_

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | deferred: grouping |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | none | deferred: grouping |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | none | deferred: grouping |

#### Default style: setters (1)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [-] | `IsTabStop` | `False` | none | deferred: grouping |

#### Template parts (2)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [-] | `HeaderContent` | `ContentControl` | `PART_HeaderContent` | deferred: grouping |
| [-] | `ItemsControl` | `ItemsControl` | `PART_ItemsControl` | deferred: grouping |

#### Visual states (0)

_none_

#### Transitions (0)

_none_

#### Resources (0)

_none_
