# MenuFlyout, MenuFlyoutPresenter and the menu's items: plan

[#152](https://github.com/wdcossey/Cornflower/issues/152). The 8.1 MenuFlyout: Avalonia's `MenuFlyout` and
`MenuFlyoutPresenter`, the presenter themed (rung 1) and the popup round it a behaviour the theme turns on (rung 2),
on the Flyout's popup host (#151). Avalonia has no `MenuFlyoutItem`, `ToggleMenuFlyoutItem` or `MenuFlyoutSeparator`:
a `MenuFlyout` holds `MenuItem`s (`ToggleType` `CheckBox` is the toggle item) and `Separator`s, so the phone's items are
keyed themes for those, given only to a `MenuFlyoutPresenter`'s own items (rung 1). No type is substituted.

## Where the values come from

- **The styles**: the 8.1 Update 1 runtime's `menuflyoutpresenter.json`, `menuflyoutitem.json` and
  `togglemenuflyoutitem.json` (`R:\cornflower-reference\flash-update1\xbf-json\`), equal to the SDK's. The two items'
  styles are the same but for their target type. `generic.xaml` has no style for `MenuFlyout` or `MenuFlyoutSeparator`.
- **The screenshots**: refapp runs `20261116-1934-dev11` and `-1940` (#150), a `MenuFlyout` shown with `ShowAt` on a
  Button, dark and light, 1080 × 1920 at 2.4 (device 11): `states` (an item forced into `Pressed`, a toggle item
  checked and one not, a separator, a disabled item, a last item), `long` (24 items), and four items on targets at
  312.08, 512.08, 652.08 and 732.08. Committed by #150 in [`ref/wp81/MenuFlyout/`](../../../ref/wp81/MenuFlyout/README.md).
- **The popup dumps** of runs `20261108-1705-dev11` and `-1734`: a full-window Rectangle, then the presenter, the menu
  at 0, 85, 450 × 193.33.
- **The motion**: the app's values of runs `20261110-0745-dev11` and `-0753`, and the recording of `20261110-0819-dev11`
  (`tools/refapp/README.md`, "MenuFlyout, from the app's values" and "Flyouts, pickers and the dialog"; gap register A8).
- **The dismissal**: runs `20261116-1951-dev11` and `-1955` (#150, "How each popup is dismissed").

## The values

| What | Phone | Source |
|---|---|---|
| Width | the window's, 450 | dumps, screenshots |
| Height | items and padding (39.5); the borders (6) not added: 4 items 244.58, 3 items 193.33 | dumps, screenshots |
| Items | 51.25 tall and apart, the first 16.67 below the top (17.08 lower down), text from x 19.17 | #150 |
| Lines | 1.5 at the top and bottom (4 raw rows), `MenuFlyoutBorderThemeBrush` | screenshots |
| Background | `MenuFlyoutBackgroundThemeBrush`, `#000000` / `#FFFFFF` | screenshots, tokens |
| Font | `TextStyleExtraLargeFontSize` 25.5, `PhoneFontFamilySemiLight`, `CharacterSpacing` -15 | style |
| Placement | below the target if it fits by the window's bottom, else above; a menu taller than the window fills it, 0 to 800 | #150 |
| Toggle item | the same checked and unchecked: no check mark, no check states | #150, style |
| Separator | draws nothing, 0 tall | #150 |
| Disabled | text `MenuFlyoutItemDisabledForegroundThemeBrush` (`#66FFFFFF` / `#66000000`); a tap does nothing | style, #150 |
| Pressed | `PointerDownThemeAnimation` on `InnerBorder`: the tilt, no colour | style, #150 |
| Opening | `OuterBorder` `ScaleX` 0 to 1 over 380 ms about (0.5, 0); then `InnerBorder` `ScaleY` 0 to 1 by 500 ms, from (0, 1) above the target or (0, 0) below; until 500 ms the borders `0,0,0,1.5` or `0,1.5,0,0` | A8, the app's values |
| Closing | no storyboard: the menu goes in one frame; the full-window layer under it about 300 ms later | recording |
| Events | an item's `Click` (a toggle's `IsChecked` flipped first), then `Closed`, in one frame; Back: `Closed` at once | #150 |
| Taps off it | on the full-window layer above or below the menu: closes it in that frame, reaching nothing under it | #150 |
| A long menu | doesn't move under a drag; the press is the menu's | #150 |

## The behaviours

- **`PhoneMenuFlyout`**, turned on by the presenter's theme: a `PhonePopupHost` for the presenter's popup (the overlay
  layer, the window's width, Back), the menu placed against its target, the opening storyboard on `CornflowerClock`,
  and the item themes given to the presenter's own containers at style priority.
- **`PhoneMenuFlyoutRoot`**, the template's root: the menu as tall as its items and padding, the borders taking from the
  bottom padding, up to the window's height.
- **The host's motion is off** (`PhonePopupHost.Moves` false, since the shared host change): it turns no band of the
  menu, and Back closes the menu at once. (Until then, a stand-in and a second Back handler worked round it.)

## Avalonia's MenuFlyout against the phone's

| | Phone | Avalonia, Wp81 |
|---|---|---|
| Item | `MenuFlyoutItem` | `MenuItem`, `Wp81MenuFlyoutItemTheme` |
| Toggle item | `ToggleMenuFlyoutItem` | `MenuItem` with `ToggleType="CheckBox"`, the same theme |
| Separator | `MenuFlyoutSeparator` | `Separator`, `Wp81MenuFlyoutSeparatorTheme`; a `MenuItem` with `Header="-"` draws nothing too |
| Sub-menus, icons, gestures | none (8.1) | not drawn: the item template has no popup, icon or gesture part |
| Placement | `Placement` | ignored: the phone's rule |

## Dependencies

#151 (Flyout: `PhonePopupHost`, `PhonePopupDismissal`), #150 (the items, placement and dismissal), #149 (the
motion's clock), `Tilt` (#60, #65), `PhoneContentLine`, `PhoneCharacterSpacing`, `TemplateParts`.

## The checklists

Generated on 2026-11-16 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with its
reason. `MenuFlyoutItem`, `ToggleMenuFlyoutItem` and `MenuFlyoutSeparator` show "a new control (rung 5)" as generated:
they are themes for `MenuItem` and `Separator`, not new controls (above).

### MenuFlyout: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist MenuFlyout <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.MenuFlyout`; Avalonia `Avalonia.Controls.MenuFlyout`.

#### Properties (2)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `MenuFlyoutPresenterStyle` | `Style` | none | Avalonia's `MenuFlyout.FlyoutPresenterTheme` is the same hook; the theme keys the presenter by type |
| [x] | `Items` (read-only) (not a dependency property) | `IList<MenuFlyoutItemBase>` | `MenuFlyout.Items` | Avalonia's stands: `MenuItem`s and `Separator`s (no `MenuFlyoutItemBase`) |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `FlyoutBase` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Placement` | `FlyoutPlacementMode` | `PopupFlyoutBase.Placement` | The phone places a menu against its target, below it if it fits by the window's bottom, else above (#150); Avalonia's `PlacementMode` is ignored under Wp81 |

#### Events, from `FlyoutBase` (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Closed` | `EventHandler<Object>` | `FlyoutBase.Closed` | Avalonia's; in the frame the menu goes (#150: an item's `Click`, then `Closed`, in one frame; Back's at once) |
| [x] | `Opened` | `EventHandler<Object>` | `FlyoutBase.Opened` | Avalonia's, in `ShowAt`'s frame, as the phone's |
| [x] | `Opening` | `EventHandler<Object>` | `PopupFlyoutBase.Opening` | Avalonia's, in `ShowAt`'s frame, as the phone's |

#### Methods, from `FlyoutBase` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void ShowAt(FrameworkElement placementTarget)` | `PopupFlyoutBase.ShowAt()` | Avalonia's |
| [x] | `void Hide()` | `PopupFlyoutBase.Hide()` | Avalonia's: the menu goes at once |
| [x] | `Control CreatePresenter()` (protected, overridable) | `MenuFlyout.CreatePresenter()` | Avalonia's: a `MenuFlyoutPresenter` |

#### Properties, from `DependencyObject` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Dispatcher` (read-only) (not a dependency property) | `CoreDispatcher` | `AvaloniaObject.Dispatcher` | Avalonia's |

#### Events, from `DependencyObject` (0)

_none_

#### Methods, from `DependencyObject` (5)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object GetValue(DependencyProperty dp)` | `AvaloniaObject.GetValue()` | Avalonia's |
| [x] | `void SetValue(DependencyProperty dp, Object value)` | `AvaloniaObject.SetValue()` | Avalonia's |
| [x] | `void ClearValue(DependencyProperty dp)` | `AvaloniaObject.ClearValue()` | Avalonia's |
| [-] | `Object ReadLocalValue(DependencyProperty dp)` | none | No Avalonia equivalent; not a look |
| [-] | `Object GetAnimationBaseValue(DependencyProperty dp)` | none | No Avalonia equivalent; not a look |

#### Default style

_`generic.xaml` has no default style for `MenuFlyout`._


### MenuFlyoutPresenter: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist MenuFlyoutPresenter <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.MenuFlyoutPresenter`; Avalonia `Avalonia.Controls.MenuFlyoutPresenter`.

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `ItemsControl` (14)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ItemsPanelRoot` (read-only) (not a dependency property) | `Panel` | `ItemsControl.ItemsPanelRoot` | Avalonia's |
| [x] | `ItemsSource` | `Object` | `ItemsControl.ItemsSource` | Avalonia's: the flyout's `Items` |
| [x] | `ItemsPanel` | `ItemsPanelTemplate` | `ItemsControl.ItemsPanel` | Avalonia's; the template binds it |
| [-] | `ItemTemplateSelector` | `DataTemplateSelector` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemsControl.ItemTemplate` | Avalonia's, bound to the flyout's |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `ItemContainerStyleSelector` | `StyleSelector` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [x] | `ItemContainerStyle` | `Style` | none | Avalonia's `ItemContainerTheme`, bound to the flyout's; it wins over the Wp81 item theme |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `DisplayMemberPath` | `String` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `GroupStyle` (read-only) (not a dependency property) | `IObservableVector<GroupStyle>` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `IsGrouping` (read-only) | `Boolean` | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [x] | `ItemContainerGenerator` (read-only) (not a dependency property) | `ItemContainerGenerator` | `ItemsControl.ItemContainerGenerator` | Avalonia's |
| [x] | `Items` (read-only) (not a dependency property) | `ItemCollection` | `ItemsControl.Items` | Avalonia's |

#### Events, from `ItemsControl` (0)

_none_

#### Methods, from `ItemsControl` (14)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Object ItemFromContainer(DependencyObject container)` | `ItemsControl.ItemFromContainer()` | Avalonia's |
| [x] | `DependencyObject ContainerFromItem(Object item)` | `ItemsControl.ContainerFromItem()` | Avalonia's |
| [x] | `Int32 IndexFromContainer(DependencyObject container)` | `ItemsControl.IndexFromContainer()` | Avalonia's |
| [x] | `DependencyObject ContainerFromIndex(Int32 index)` | `ItemsControl.ContainerFromIndex()` | Avalonia's |
| [-] | `Boolean IsItemItsOwnContainerOverride(Object item)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `DependencyObject GetContainerForItemOverride()` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [x] | `void ClearContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `SelectingItemsControl.ClearContainerForItemOverride()` | Avalonia's |
| [x] | `void PrepareContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | `SelectingItemsControl.PrepareContainerForItemOverride()` | Avalonia's |
| [-] | `void OnItemsChanged(Object e)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |
| [-] | `void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)` (protected, overridable) | none | Avalonia has none; the flyout makes the presenter and an app doesn't reach this |

#### Default style: setters (8)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `{ThemeResource MenuFlyoutBackgroundThemeBrush}` | `TemplatedControl.Background` | Setter, the token |
| [x] | `BorderBrush` | `{ThemeResource MenuFlyoutBorderThemeBrush}` | `TemplatedControl.BorderBrush` | Setter, the token |
| [x] | `BorderThickness` | `{ThemeResource MenuFlyoutPortraitBorderThemeThickness}` | `TemplatedControl.BorderThickness` | Setter, the token; the template's borders bind to it (the phone's get it from the storyboard's end, held) |
| [x] | `Padding` | `{ThemeResource MenuFlyoutPortraitThemePadding}` | `TemplatedControl.Padding` | Setter, the token; the ItemsPresenter's margin |
| [x] | `MinHeight` | `{ThemeResource FlyoutThemeMinHeight}` | `Layoutable.MinHeight` | Setter, the token |
| [x] | `FontSize` | `{ThemeResource TextStyleExtraLargeFontSize}` | `TemplatedControl.FontSize` | Setter, the token; the items inherit it |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilySemiLight}` | `TemplatedControl.FontFamily` | Setter, the token (Selawik Semilight) |
| [x] | `CharacterSpacing` | `-15` | none | `PhoneCharacterSpacing.CharacterSpacing` -15 and the `LetterSpacing` it makes, inherited by the items |

#### Template parts (6)

Avalonia's control looks for: none.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `OuterBorder` | `Border` | none | `Border#OuterBorder` |
| [x] | `OuterScaleTransform` | `ScaleTransform` | none | Set by `PhoneMenuFlyout` at animation priority while the menu opens; none at rest |
| [x] | `CenterBorder` | `Border` | none | `Border#CenterBorder` |
| [x] | `InnerBorder` | `StackPanel` | none | `StackPanel#InnerBorder`, `ClipToBounds` for WinRT's layout clip |
| [x] | `InnerScaleTransform` | `ScaleTransform` | none | Set by `PhoneMenuFlyout` at animation priority while the menu opens; none at rest |
| [x] | `ItemsPresenter` | `ItemsPresenter` | none | `ItemsPresenter#ItemsPresenter` |

#### Visual states (5)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `PlacementStates` | `None` | nothing | no rule | At rest |
| [x] | `PlacementStates` | `TopPortrait` | `InnerBorder.RenderTransformOrigin: PointAnimation Duration=0:0:0 To=0,1`<br>`OuterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 0,0,0,1.5, 0:0:0.50 → {ThemeResource MenuFlyoutPortraitBorderThemeThickness}`<br>`CenterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 0,0,0,1.5, 0:0:0.50 → {ThemeResource MenuFlyoutPortraitBorderThemeThickness}`<br>`ItemsPresenter.Margin = {ThemeResource MenuFlyoutPortraitThemePadding}`<br>`OuterScaleTransform.ScaleX: DoubleAnimation Duration=0:0:0.38 From=0.0 To=1.0`<br>`InnerScaleTransform.ScaleY: DoubleAnimationUsingKeyFrames 0:0:0 → 0.0, 0:0:0.38 → 0.0, 0:0:0.50 → 1.0` | no rule | `PhoneMenuFlyout`, the menu above its target, on `CornflowerClock` frame by frame |
| [x] | `PlacementStates` | `BottomPortrait` | `InnerBorder.RenderTransformOrigin: PointAnimation Duration=0:0:0 To=0,0`<br>`OuterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 0,1.5,0,0, 0:0:0.50 → {ThemeResource MenuFlyoutPortraitBorderThemeThickness}`<br>`CenterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 0,1.5,0,0, 0:0:0.50 → {ThemeResource MenuFlyoutPortraitBorderThemeThickness}`<br>`ItemsPresenter.Margin = {ThemeResource MenuFlyoutPortraitThemePadding}`<br>`OuterScaleTransform.ScaleX: DoubleAnimation Duration=0:0:0.38 From=0.0 To=1.0`<br>`InnerScaleTransform.ScaleY: DoubleAnimationUsingKeyFrames 0:0:0 → 0.0, 0:0:0.38 → 0.0, 0:0:0.50 → 1.0` | no rule | `PhoneMenuFlyout`, the menu below its target (and a menu that fills the window), on `CornflowerClock` |
| [-] | `PlacementStates` | `LeftLandscape` | `InnerBorder.RenderTransformOrigin: PointAnimation Duration=0:0:0 To=1,0`<br>`OuterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 0,0,1.5,0, 0:0:0.50 → {ThemeResource MenuFlyoutLeftLandscapeBorderThemeThickness}`<br>`CenterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 0,0,1.5,0, 0:0:0.50 → {ThemeResource MenuFlyoutLeftLandscapeBorderThemeThickness}`<br>`ItemsPresenter.Margin = {ThemeResource MenuFlyoutLandscapeThemePadding}`<br>`OuterScaleTransform.ScaleY: DoubleAnimation Duration=0:0:0.38 From=0.0 To=1.0`<br>`InnerScaleTransform.ScaleX: DoubleAnimationUsingKeyFrames 0:0:0 → 0.0, 0:0:0.38 → 0.0, 0:0:0.50 → 1.0` | no rule | Not drawn: no landscape capture; the portrait placement and motion stand in landscape (Known gaps) |
| [-] | `PlacementStates` | `RightLandscape` | `InnerBorder.RenderTransformOrigin: PointAnimation Duration=0:0:0 To=0,0`<br>`OuterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 1.5,0,0,0, 0:0:0.50 → {ThemeResource MenuFlyoutRightLandscapeBorderThemeThickness}`<br>`CenterBorder.BorderThickness: ObjectAnimationUsingKeyFrames 0 → 1.5,0,0,0, 0:0:0.50 → {ThemeResource MenuFlyoutRightLandscapeBorderThemeThickness}`<br>`ItemsPresenter.Margin = {ThemeResource MenuFlyoutLandscapeThemePadding}`<br>`OuterScaleTransform.ScaleY: DoubleAnimation Duration=0:0:0.38 From=0.0 To=1.0`<br>`InnerScaleTransform.ScaleX: DoubleAnimationUsingKeyFrames 0:0:0 → 0.0, 0:0:0.38 → 0.0, 0:0:0.50 → 1.0` | no rule | Not drawn, as `LeftLandscape` |

#### Transitions (0)

_none_

#### Resources (10)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `MenuFlyoutBackgroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FF000000` | `SolidColorBrush {ThemeResource PhoneBackgroundColor} → #FFFFFFFF` | yes | In `Tokens/` |
| [x] | `MenuFlyoutBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | In `Tokens/` |
| [x] | `MenuFlyoutPortraitBorderThemeThickness` | `0,1.5` | same | yes | In `Tokens/`; the opening's one-edge thickness is its top or bottom alone |
| [x] | `MenuFlyoutPortraitThemePadding` | `19,13.5,0,26` | same | yes | In `Tokens/` |
| [x] | `FlyoutThemeMinHeight` | `43` | same | yes | In `Tokens/` |
| [x] | `TextStyleExtraLargeFontSize` | `25.5` | same | yes | In `Tokens/` |
| [x] | `PhoneFontFamilySemiLight` | `Segoe WP SemiLight` | same | yes | In `Tokens/` (Selawik Semilight, ADR 0001) |
| [-] | `MenuFlyoutLeftLandscapeBorderThemeThickness` | `0,0,1.5,0` | same | yes | In `Tokens/`; unused until landscape is drawn |
| [-] | `MenuFlyoutLandscapeThemePadding` | `19,59.5,0,65` | same | yes | In `Tokens/`; unused until landscape is drawn |
| [-] | `MenuFlyoutRightLandscapeBorderThemeThickness` | `1.5,0,0,0` | same | yes | In `Tokens/`; unused until landscape is drawn |


### MenuFlyoutItem: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist MenuFlyoutItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.MenuFlyoutItem`; Avalonia none: a new control (rung 5).

#### Properties (3)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Text` | `String` | none | `MenuItem.Header`, shown by the template's `ContentPresenter#TextBlock` at the phone's line |
| [x] | `CommandParameter` | `Object` | none | `MenuItem.CommandParameter` |
| [x] | `Command` | `ICommand` | none | `MenuItem.Command` |

#### Events (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | none | `MenuItem.Click`; then the menu closes, in the same frame (#150) |

#### Methods (0)

_none_

#### Properties, from `MenuFlyoutItemBase` (0)

_none_

#### Events, from `MenuFlyoutItemBase` (0)

_none_

#### Methods, from `MenuFlyoutItemBase` (0)

_none_

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | none | Setter, `Transparent`; `LayoutRoot` takes the hits |
| [x] | `Padding` | `{ThemeResource MenuFlyoutItemThemePadding}` | none | Setter, the token; `InnerBorder`'s padding |
| [x] | `HorizontalContentAlignment` | `Left` | none | `MenuItem` has none: the template's text is `Left` |
| [x] | `VerticalContentAlignment` | `Center` | none | `MenuItem` has none: the template's text is `Center` |

#### Template parts (3)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `Border` | `PART_LayoutRoot` | `Border#LayoutRoot` |
| [x] | `InnerBorder` | `Border` | `PART_InnerBorder` | `Border#InnerBorder`, `Tilt.IsTarget` |
| [x] | `TextBlock` | `TextBlock` | `PART_TextBlock` | `ContentPresenter#TextBlock` (a header may be any content), `PhoneContentLine` |

#### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The theme's plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | Nothing, as the phone's |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on InnerBorder` | `^:pressed` | `Tilt.IsEnabled` on the item, the tilt on `InnerBorder`; no colour |
| [x] | `CommonStates` | `Disabled` | `TextBlock.Foreground = {ThemeResource MenuFlyoutItemDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled`: the text's `Foreground` |

#### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on InnerBorder` | The tilt's return |
| [x] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on InnerBorder` | The tilt's return |
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on InnerBorder` | The tilt's return |

#### Resources (2)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `MenuFlyoutItemThemePadding` | `0,8.5` | same | yes | In `Tokens/` |
| [x] | `MenuFlyoutItemDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | In `Tokens/` |


### ToggleMenuFlyoutItem: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist ToggleMenuFlyoutItem <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem`; Avalonia none: a new control (rung 5).

#### Properties (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `IsChecked` | `Boolean` | none | `MenuItem.IsChecked` with `ToggleType` `CheckBox`; flips before `Click` (#150); drawn the same checked or not |

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `MenuFlyoutItem` (3)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Text` | `String` | none | `MenuItem.Header`, shown by the template's `ContentPresenter#TextBlock` at the phone's line |
| [x] | `CommandParameter` | `Object` | none | `MenuItem.CommandParameter` |
| [x] | `Command` | `ICommand` | none | `MenuItem.Command` |

#### Events, from `MenuFlyoutItem` (1)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Click` | `RoutedEventHandler` | none | `MenuItem.Click`; then the menu closes, in the same frame (#150) |

#### Methods, from `MenuFlyoutItem` (0)

_none_

#### Properties, from `MenuFlyoutItemBase` (0)

_none_

#### Events, from `MenuFlyoutItemBase` (0)

_none_

#### Methods, from `MenuFlyoutItemBase` (0)

_none_

#### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Background` | `Transparent` | none | Setter, `Transparent`; `LayoutRoot` takes the hits |
| [x] | `Padding` | `{ThemeResource MenuFlyoutItemThemePadding}` | none | Setter, the token; `InnerBorder`'s padding |
| [x] | `HorizontalContentAlignment` | `Left` | none | `MenuItem` has none: the template's text is `Left` |
| [x] | `VerticalContentAlignment` | `Center` | none | `MenuItem` has none: the template's text is `Center` |

#### Template parts (3)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `LayoutRoot` | `Border` | `PART_LayoutRoot` | `Border#LayoutRoot` |
| [x] | `InnerBorder` | `Border` | `PART_InnerBorder` | `Border#InnerBorder`, `Tilt.IsTarget` |
| [x] | `TextBlock` | `TextBlock` | `PART_TextBlock` | `ContentPresenter#TextBlock` (a header may be any content), `PhoneContentLine` |

#### Visual states (4)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Normal` | nothing | the theme's plain setters | The theme's plain setters |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | Nothing, as the phone's |
| [x] | `CommonStates` | `Pressed` | `PointerDownThemeAnimation on InnerBorder` | `^:pressed` | `Tilt.IsEnabled` on the item, the tilt on `InnerBorder`; no colour |
| [x] | `CommonStates` | `Disabled` | `TextBlock.Foreground = {ThemeResource MenuFlyoutItemDisabledForegroundThemeBrush}` | `^:disabled` | `^:disabled`: the text's `Foreground` |

#### Transitions (3)

| ✓ | Group | From → To | Duration | What it runs | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Pressed` → `PointerOver` |  | `PointerUpThemeAnimation on InnerBorder` | The tilt's return |
| [x] | `CommonStates` | `PointerOver` → `Normal` |  | `PointerUpThemeAnimation on InnerBorder` | The tilt's return |
| [x] | `CommonStates` | `Pressed` → `Normal` |  | `PointerUpThemeAnimation on InnerBorder` | The tilt's return |

#### Resources (2)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `MenuFlyoutItemThemePadding` | `0,8.5` | same | yes | In `Tokens/` |
| [x] | `MenuFlyoutItemDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66FFFFFF` | `SolidColorBrush {ThemeResource PhoneDisabledColor} → #66000000` | yes | In `Tokens/` |


### MenuFlyoutSeparator: checklist

> Generated by `dotnet run --project tools/winrt-api -- checklist MenuFlyoutSeparator <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.MenuFlyoutSeparator`; Avalonia none: a new control (rung 5).

#### Properties (0)

_none_

#### Events (0)

_none_

#### Methods (0)

_none_

#### Properties, from `MenuFlyoutItemBase` (0)

_none_

#### Events, from `MenuFlyoutItemBase` (0)

_none_

#### Methods, from `MenuFlyoutItemBase` (0)

_none_

#### Default style

_`generic.xaml` has no default style for `MenuFlyoutSeparator`._

