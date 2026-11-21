# SemanticZoom: plan

[#215](https://github.com/wdcossey/Cornflower/issues/215). A new control (rung 5): `SemanticZoom` in `Cornflower.Controls`
(behaviour only), its Wp81 theme (`Controls/SemanticZoom.axaml`), the jump list's popup round it (`PhoneSemanticZoom`, on
`PhonePopupHost`), and WinRT's two jump list converters, public in `Cornflower.Controls` so an 8.1 app's XAML ports. The
evidence is #211's (PR #217): the recording's stills in [`ref/wp81/SemanticZoom/`](../../../ref/wp81/SemanticZoom/README.md)
and the runs `20261118-2231-dev11-partial` (`-Probe semanticzoom -Record`), `20261118-2237-dev11` (the same, not recorded)
and `20261118-2246-dev11-partial` (`-Probe semanticzoomlocked`), read in [`tools/refapp/README.md`](../../../tools/refapp/README.md),
"Tier 4's reads", SemanticZoom. Device 11, the reference profile, 450 × 800 at 2.4. The probe's SemanticZoom: below a 60 px
header band; zoomed in a grouped ListView (groups a to h, five items each, b and e empty and hidden); zoomed out a GridView
of the groups in the 8.1 docs' tile (a 99 × 99 `Border`, margin 6, in `JumpListItemBackgroundConverter`'s brush; the letter
at 48 px in `JumpListItemForegroundConverter`'s, padded 6, bottom left). The motion is the recording's, about 36 frames a
second, so to about 28 ms.

## Behaviours

Each with its evidence and its test in `tests/Cornflower.Tests/Controls/SemanticZoomTests.cs`.

| Behaviour | Evidence | Test |
|---|---|---|
| The template: a presenter for the zoomed-in view; a `Popup` holding `PopupBorder` (no background) round the zoomed-out view's presenter; the views where the content alignments put them; the file's setters | generic.xaml; the reads ("the template") | `The_Template_Is_The_Phones` |
| A tap on a group's header zooms out: `ViewChangeStarted` (source the list's top item, destination the header's group) then `ViewChangeCompleted`, at once | the probe: Started 1 to 2 ms after the release, Completed 1 ms later, source the first item, destination group a | `A_Tap_On_A_Header_Raises_Both_Events_At_Once_And_Shows_The_Jump_List_50_ms_Later` |
| The tiles show 50 ms after the tap | the recording: about 50 ms, a 4 px band across the first row | the same |
| The pinned header is a header too | the ListView's pinned header (#131) is a `ListViewHeaderItem`; not read on its own | `A_Tap_On_The_Pinned_Header_Zooms_Out_To_Its_Group` |
| The jump list covers the whole window, from its top left, wherever the SemanticZoom is; no dim, no background: the list shows between the tiles | the recording: 450 × 800 from the top left, the SemanticZoom 60 down; `PopupBorder`'s background null | `The_Jump_List_Covers_The_Whole_Window_With_No_Dim_And_The_List_Between_Its_Tiles` |
| It takes the app's theme, not the page's | `recording__zoomed-out-light-page`: the page light, the app dark, the jump list dark | `The_Jump_List_Takes_The_Windows_Theme_Not_The_Pages` |
| It opens in rows of a tile's height, each from 80° to rest over 170 ms | the recording: a band across the first row at its centre line, the full height 170 ms (6 frames) later | `The_Jump_List_Opens_In_Rows_Of_A_Tile_Over_170_ms` |
| The motion runs at `CornflowerConfig`'s popup speed; with motion off it opens at once | Cornflower's rule for popups | `The_Jump_Lists_Motion_Follows_The_Popup_Speed` |
| The converters: accent for a group with items, #FF1F1F1F empty, the letter white on both (dark); light #FFDEDEDE and #FFA5A5A5 empty; the app's theme | the recording's tiles; the file's `JumpListDefault*` | `The_Converters_Give_The_Apps_Jump_List_Brushes` |
| The converters' `Enabled` and `Disabled` | WinRT's API | `The_Converters_Take_An_Apps_Own_Brushes` |
| A tap on a tile zooms in: both events (group to group) at the release, and in the same frame the list lands with that group's header at the top; the tile isn't selected | the probe: group d to group d, the offset 451.67, header d at the SemanticZoom's top | `A_Tap_On_A_Tile_Lands_The_List_With_Its_Groups_Header_At_The_Top` |
| An empty group's tile is grey and does nothing | the converters; no group header to land on | `A_Tap_On_An_Empty_Groups_Tile_Does_Nothing` |
| A tile's tap closes it after about 100 ms, top row first, in about 80 ms | the recording: from about 100 ms after the release (the tap's tilt first), over about 80 ms (3 frames) | `A_Tiles_Tap_Closes_The_Jump_List_Top_Row_First_After_The_Tilt` |
| Back closes it the same way, at once; Started with no source or destination; the list stays; the app's Back isn't called | the probe and the recording | `Back_Closes_The_Jump_List_And_Leaves_The_List_Where_It_Was` |
| `IsZoomedInViewActive` and `ToggleActiveView()` change the view from code | WinRT's API | `The_View_Changes_From_Code` |
| Zoomed out before it is shown, or taken out of the window and put back, the jump list opens | follows from `IsZoomedInViewActive` | `A_Zoomed_Out_View_Opens_As_It_Is_Shown_And_Again_As_It_Comes_Back` |
| `CanChangeViews` false: nothing changes the view | the phone closed the app; decided (#215) not to copy it | `With_CanChangeViews_False_Nothing_Changes_The_View` |

The sample page's test (`tests/Cornflower.Tests/Sample/SemanticZoomPageTests.cs`) also holds that the views take the page's
data context while the jump list is closed (they are the SemanticZoom's logical children, as WinRT's are in its tree).

### The motion's numbers

The reads give the shape, not each row's angle, so the motion is fitted to them, on `PhonePopupHost`'s bands:

- **Opening:** `PhoneSemanticZoom.OpenDelay` 50 ms after the tap, the popup opens in rows of the first tile container's
  height with its margin (111.25 for the docs' tile, `PhoneSemanticZoom.RowHeight` if there is none to measure), every row
  from `PhonePopupMotion.OpenFrom` (80°, the Flyout's) to rest at one rate, 80° over `OpenLength` 170 ms (7.35° a tick).
  The phone's first frame shows a 4 px band, nearer edge-on than 80°: the host's start angle is kept.
- **Closing:** a tile's tap holds it `TileHold` 100 ms, then the rows turn away, the top first, at `CloseRate` 30° a
  tick, each `CloseStagger` 0.3 ticks after the one above: the reference's eight rows gone in 5 ticks (78 ms), the read's
  about 80. Back closes it at once, the same way.

## The visual states

None: the file declares none. The tiles' look is the app's template; the brushes are the converters'.

## Avalonia's control against the phone's

Avalonia 12.1.4 has no SemanticZoom: a new control, rung 5. It reuses #131's grouping (`ListBoxOptions.GroupedItemsSource`,
`ListViewHeaderItem`, the internal `ListBoxOptions.ScrollGroupToTop`), #132's item click (`ListBoxOptions.IsItemClickEnabled`,
turned on on the jump list so a tap selects nothing) and the Tier 3 popup host (`PhonePopupHost`, which fits: `Full`
placement, no `Dim`, its Back handler, and its bands).

Two small changes to the shared popup code, listed in the PR: `PhonePopupRows` takes a closing rate and stagger (the
Flyout's by default), and `PhonePopupHost` closes by its rows' (`PhonePopupMotion.ClosingAngle` and `ClosedAfter` gain
overloads). Nothing else shared changes.

## Extensions

None.

## Dependencies

Grouped lists (#131), item click (#132), the GridView's theme (#213, `Wp81GridViewTheme`) and the popup host (#149,
#150): all built. Owed: the device run on the Pixel 8 and the real-window run; pinch (unread).

## Checklists

Generated with `dotnet run --project tools/winrt-api -- checklist SemanticZoom <Windows.winmd> <design folder>` (and the
converters and the event data by their full names), from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done; `[-]` is left out, with its reason.

### SemanticZoom: checklist

WinRT `Windows.UI.Xaml.Controls.SemanticZoom`; Avalonia none: a new control (rung 5).

#### Properties (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ZoomedOutView` | `ISemanticZoomInformation` | `ZoomedOutView` | `Control`: a `ListBox` of the groups clicks its tiles; another view changes view only in code |
| [x] | `ZoomedInView` | `ISemanticZoomInformation` | `ZoomedInView` | `Control`: a grouped `ListBox` (`ListBoxOptions.GroupedItemsSource`) |
| [x] | `IsZoomedInViewActive` | `Boolean` | `IsZoomedInViewActive` | A direct property, two-way, true by default |
| [-] | `IsZoomOutButtonEnabled` | `Boolean` | none | Not applicable: the phone draws no zoom-out button (#211: `True`, and nothing drawn) |
| [x] | `CanChangeViews` | `Boolean` | `CanChangeViews` | False: no change of view (decided, #215; the phone closed the app) |

#### Events (2)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ViewChangeCompleted` | `SemanticZoomViewChangedEventHandler` | `ViewChangeCompleted` | `EventHandler<SemanticZoomViewChangedEventArgs>` |
| [x] | `ViewChangeStarted` | `SemanticZoomViewChangedEventHandler` | `ViewChangeStarted` | The same |

#### Methods (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ToggleActiveView()` | `ToggleActiveView()` | |

#### Default style: setters (7)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Padding` | `0` | `Padding` | Avalonia's default; no setter |
| [x] | `Background` | `Transparent` | `Background` | Setter |
| [x] | `HorizontalContentAlignment` | `Stretch` | `HorizontalContentAlignment` | Added to the control (Avalonia's `TemplatedControl` lacks it); `Stretch` is its default |
| [x] | `VerticalContentAlignment` | `Stretch` | `VerticalContentAlignment` | The same |
| [x] | `BorderThickness` | `0` | `BorderThickness` | Avalonia's default; no setter |
| [x] | `IsTabStop` | `False` | `KeyboardNavigation.IsTabStop` | Setter |
| [x] | `TabNavigation` | `Once` | `KeyboardNavigation.TabNavigation` | Setter |

#### Template parts (4)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ZoomedInPresenter` | `ContentPresenter` | `PART_ZoomedInPresenter` | |
| [x] | `Popup` | `Popup` | `PART_Popup` | Hosted by `PhonePopupHost`; the `PopupThemeTransition` is the host's bands |
| [x] | `PopupBorder` | `Border` | `PART_PopupBorder` | No background; inside `AppTheme`, a `ThemeVariantScope` for the window's theme |
| [x] | `ZoomedOutPresenter` | `ContentPresenter` | `PART_ZoomedOutPresenter` | |

#### Visual states (0), Transitions (0), Resources (0)

_none_

### SemanticZoomViewChangedEventArgs and SemanticZoomLocation

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SourceItem` | `SemanticZoomLocation` | `SourceItem` | |
| [x] | `IsSourceZoomedInView` | `Boolean` | `IsSourceZoomedInView` | |
| [x] | `DestinationItem` | `SemanticZoomLocation` | `DestinationItem` | |
| [x] | `SemanticZoomLocation.Item` | `Object` | `Item` | Null for Back and a zoom-in in code |
| [-] | `SemanticZoomLocation.Bounds` | `Rect` | none | Left out: nothing in an 8.1 app's jump list reads it |

### JumpListItemBackgroundConverter and JumpListItemForegroundConverter

WinRT `Windows.UI.Xaml.Controls.Primitives.*`; ours in `Cornflower.Controls`, as `IValueConverter`s.

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Enabled` | `Brush` | `Enabled` (`IBrush`) | A plain property, not a dependency property: unset, the app's `JumpListDefaultEnabled*` |
| [x] | `Disabled` | `Brush` | `Disabled` (`IBrush`) | The same: unset, the app's `JumpListDefaultDisabled*` |
| [x] | `Convert` | | `Convert` | A group with items: `Enabled`; an empty group or null: `Disabled` |
| [x] | `ConvertBack` | | `ConvertBack` | Not supported, as WinRT's |
| [-] | `Dispatcher`, `GetValue` and the rest of `DependencyObject` | | none | Not applicable: plain converters |
