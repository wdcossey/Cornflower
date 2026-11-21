# SemanticZoom

Status: built · [#215](https://github.com/wdcossey/Cornflower/issues/215) · against #211's recording (run `20261118-2231-dev11-partial`) and probes (`20261118-2237-dev11`, `20261118-2246-dev11-partial`) · owed: the device run on the Pixel 8 and the real-window run

The Windows Phone 8.1 SemanticZoom: a grouped list and its jump list. Cornflower's own control, `SemanticZoom`
(`src/Cornflower.Controls/Controls/SemanticZoom/`), its Wp81 theme (`src/Cornflower.Themes.Wp81/Controls/SemanticZoom.axaml`),
and the jump list's popup (`PhoneSemanticZoom`, on the popups' `PhonePopupHost`); with WinRT's `JumpListItemBackgroundConverter`
and `JumpListItemForegroundConverter`, public in `Cornflower.Controls`. The plan, with every value and its source, is
[PLAN.md](PLAN.md); the phone beside ours is [COMPARISON.md](COMPARISON.md).

## Usage

```xml
<UserControl.Resources>
  <cf:JumpListItemBackgroundConverter x:Key="JumpListItemBackgroundConverter" />
  <cf:JumpListItemForegroundConverter x:Key="JumpListItemForegroundConverter" />
  <DataTemplate x:Key="LetterTile" x:DataType="local:ContactGroup">
    <Border Width="99" Height="99" Margin="6"
            Background="{Binding Converter={StaticResource JumpListItemBackgroundConverter}}">
      <TextBlock Text="{Binding Key}" FontSize="48" Padding="6" HorizontalAlignment="Left" VerticalAlignment="Bottom"
                 Foreground="{Binding Converter={StaticResource JumpListItemForegroundConverter}}" />
    </Border>
  </DataTemplate>
</UserControl.Resources>

<cf:SemanticZoom>
  <cf:SemanticZoom.ZoomedInView>
    <ListBox AutoScrollToSelectedItem="False"
             cf:ListBoxOptions.GroupedItemsSource="{Binding Contacts}"
             cf:ListBoxOptions.GroupHeaderTemplate="{StaticResource LetterHeader}" />
  </cf:SemanticZoom.ZoomedInView>
  <cf:SemanticZoom.ZoomedOutView>
    <ListBox AutoScrollToSelectedItem="False" Margin="19,0" Theme="{DynamicResource Wp81GridViewTheme}"
             ItemsSource="{Binding Contacts}" ItemTemplate="{StaticResource LetterTile}" />
  </cf:SemanticZoom.ZoomedOutView>
</cf:SemanticZoom>
```

- **`ZoomedInView`**: a `ListBox` grouped by `ListBoxOptions.GroupedItemsSource` (docs/controls/GroupedLists). A tap on a
  group's header, the pinned one too, opens the jump list.
- **`ZoomedOutView`**: a `ListBox` of the same groups, all of them (the empty ones show greyed), usually in the GridView's
  theme. Its tiles are your template, as on the phone; the converters colour them. The SemanticZoom turns its
  `ListBoxOptions.IsItemClickEnabled` on, so a tap selects nothing: a group with items zooms in to it, an empty group's
  tile does nothing. Another kind of view changes view only by `IsZoomedInViewActive` or `ToggleActiveView()`.
- **The converters**: given a group (an `IEnumerable` of its items), `Enabled` for one with items, `Disabled` for an
  empty one. Unset, each is the app's `JumpListDefault*` resource in the app's theme (Wp81's: the accent, #FF1F1F1F dark
  or #FFDEDEDE light; the letter white, or #FFA5A5A5 on a light empty tile).
- **`IsZoomedInViewActive`**, **`ToggleActiveView()`**, **`ViewChangeStarted`**, **`ViewChangeCompleted`** and
  **`CanChangeViews`** as WinRT's.
- Set `AutoScrollToSelectedItem="False"` on both lists, as on every phone list (#220).

## Behaviours

- **Zooming out:** a tap on a header raises `ViewChangeStarted` (the source the list's top item, the destination the
  header's group) and `ViewChangeCompleted` at once. 50 ms later the jump list opens over the **whole window**, wherever
  the SemanticZoom is, with **no dim and no background**: the list shows between the tiles. It opens in rows of a tile's
  height, each turning in over 170 ms. It takes the window's theme, not the page's.
- **Zooming in:** a tap on a tile raises both events (the group to the group) and, in the same frame, lands the list with
  that group's header at its top; 100 ms later (the tile's tilt) the rows turn away, the top first, in about 80 ms. **Back**
  closes it the same way at once, with no source or destination, the list where it was; the app's Back isn't called.
- The motion runs on `CornflowerClock` at `CornflowerConfig.PopupSpeed`; with `CornflowerConfig.Animated` off it opens and
  closes at once.

The tests are `tests/Cornflower.Tests/Controls/SemanticZoomTests.cs` and `Sample/SemanticZoomPageTests.cs`. The sample's
page is `semantic zoom`: contacts a to z, some letters empty.

## Resource keys

`JumpListDefaultEnabledBackground`, `JumpListDefaultDisabledBackground`, `JumpListDefaultEnabledForeground` and
`JumpListDefaultDisabledForeground` (`Tokens/Colors.axaml`), the converters' defaults. The control's theme takes none.

## The template's parts

`PART_ZoomedInPresenter`, `PART_Popup`, `PART_PopupBorder` and `PART_ZoomedOutPresenter`, the phone's four; Wp81's adds
`AppTheme`, a `ThemeVariantScope` between the popup and `PART_PopupBorder`.

## The ladder

Rung 5: a new control. Avalonia 12.1.4 has none.

## Departures

- **`CanChangeViews` false: no change of view.** On the phone a tap on a header closed the app (#211, runs
  `20261118-2231-dev11-partial` and `20261118-2246-dev11-partial`); a control needn't copy a crash. Decided by the
  coordinator on #215, reversible: here nothing changes the view, from a tap, Back or code.

## Known gaps

- **Pinch is not built.** The phone's pinch wasn't read (the emulator's mouse is one finger); the jump list opens by a tap
  on a header, or in code.
- **The jump list opens at its top.** WinRT may scroll it to the destination group; unread, so not built.
- **The motion is a fit** to the recording's shape (PLAN.md, "The motion's numbers"): every row opens at one rate from the
  Flyout's 80°, where the phone's first frame is nearer edge-on.
- **The converters read the app's resources** (`Application.Current`), so Wp81 has to be in the app's styles, as an app
  has it; a brush is read as a tile is bound, so a change of the app's theme shows on tiles bound after it.
- **`IsZoomOutButtonEnabled` and `SemanticZoomLocation.Bounds` are left out**: the phone draws no zoom-out button, and
  nothing in a jump list reads the bounds.
- **Only a `ListBox` view** clicks its tiles and lands the list; the views are `Control`s, not `ISemanticZoomInformation`.

## The checklist

In [PLAN.md](PLAN.md#checklists): every row done but `IsZoomOutButtonEnabled`, `SemanticZoomLocation.Bounds` and the
converters' `DependencyObject` members, each with its reason.
