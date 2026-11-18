# MenuFlyout, MenuFlyoutPresenter and the menu's items

Status: done but for the real-window run · [#152](https://github.com/wdcossey/Cornflower/issues/152) · device run passed 2026-11-17 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page menu` on `main` at `9a24f7f`, 17:40 to 17:44, cold starts 747 to 813 ms over the run's pages: the page opened in 318 ms; the menu opened by a tap and drawn right, Back closing it) · owed: the real-window run

The Windows Phone 8.1 MenuFlyout: Avalonia's `MenuFlyout`, its `MenuFlyoutPresenter` themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/MenuFlyoutPresenter.axaml`, its `MenuItem`s and `Separator`s drawn as the phone's
`MenuFlyoutItem`, `ToggleMenuFlyoutItem` and `MenuFlyoutSeparator`, and the phone's popup round it: the window's width,
below or above its target, opening by the template's storyboard on `CornflowerClock`, closed at once by an item, a tap
off it or Back. [PLAN.md](PLAN.md) has every value and its source; [COMPARISON.md](COMPARISON.md) has the emulator's
screenshots beside ours.

## Usage

```xml
<Button Content="options">
  <Button.Flyout>
    <MenuFlyout>
      <MenuItem Header="share" Click="OnShare" />
      <MenuItem Header="pin" ToggleType="CheckBox" IsChecked="{Binding IsPinned}" />
      <Separator />
      <MenuItem Header="delete" IsEnabled="{Binding CanDelete}" />
    </MenuFlyout>
  </Button.Flyout>
</Button>
```

WinRT's `MenuFlyoutItem` is a `MenuItem`; its `Text` is the `Header`. `ToggleMenuFlyoutItem` is a `MenuItem` with
`ToggleType="CheckBox"`. `MenuFlyoutSeparator` is a `Separator`. `ShowAt`, `Hide`, `IsOpen`, `Opening`, `Opened`,
`Closing` and `Closed` are Avalonia's. Under Wp81:

- **Where it sits.** The window's width, inside the safe area. Directly below its target (the menu's top at the
  target's bottom) if it fits there by the window's bottom, else directly above it (its bottom at the target's top),
  kept inside the window. A menu as tall as the window or taller fills it, whatever the target, and what doesn't fit is
  cut off: it doesn't scroll. Avalonia's `Placement` and offsets are ignored. It is placed again as the window or the
  menu changes size.
- **How tall.** Its items and its padding (19, 13.5, 0, 26); the 1.5 lines at the top and bottom take from the bottom
  padding, as the phone's do.
- **Opening.** The template's storyboard, as written: the line by the target spreads out from the middle over 380 ms,
  then the menu unfolds from it, away from the target, by 500 ms.
- **Closing.** At once, with no motion: a tap on an item (its `Click`, a toggle item's `IsChecked` flipped first, then
  `Closed`, in one frame), a tap above or below the menu (it reaches nothing under it), `Hide()`, `IsOpen = false`, or
  Back. Back is taken from the page: its `NavigationPage` doesn't go back.
- **Items.** The text is the header, in the presenter's font (25.5, Selawik Semilight, `CharacterSpacing` -15). A toggle
  item draws the same checked or not; a `Separator` (or a `MenuItem` whose `Header` is `"-"`) draws nothing and is 0
  tall; a disabled item's text is `MenuFlyoutItemDisabledForegroundThemeBrush` and a tap on it does nothing; a pressed
  item tilts, with no colour. Sub-menus, icons and input gestures aren't drawn: 8.1 has none.

## How the items are scoped

Avalonia has no `MenuFlyoutItem`: a `MenuFlyout` holds `MenuItem`s, the same type a desktop `Menu` holds. So the
phone's item is a keyed theme, `Wp81MenuFlyoutItemTheme` (and `Wp81MenuFlyoutSeparatorTheme` for `Separator`), not the
theme for the type. The presenter's theme names them (`PhoneMenuFlyout.ItemTheme`, `PhoneMenuFlyout.SeparatorTheme`),
and `PhoneMenuFlyout` gives them to the presenter's own containers, at style priority, as each is prepared. A
`MenuItem` anywhere else (a desktop `Menu`, Tier 4) keeps its own theme. An app's own `Theme` set on an item, or a
`MenuFlyout.ItemContainerTheme`, wins; a style's `Theme` setter for `MenuItem` competes with ours at style priority.

## Extensions

None.

## Departures

Off by default, so the menu is the phone's unless an app asks ([ADR 0006](../../decisions/0006-faithful-defaults-and-cornflowerconfig.md)):

- **`CornflowerConfig.Deviations.MenuFlyoutSingleStepOpen`** (#189): the menu opens in one step, its width and height
  growing together over 380 ms, the phone's width's time, in place of the phone's two (the width over 380 ms, then the
  height until 500 ms), which can feel slow. Read as the menu starts to open. Like the phone's opening, it runs at
  `CornflowerConfig.PopupSpeed` (else `MotionSpeed`), and with `Animated` off the menu opens at rest.

## Resource keys

`MenuFlyoutBackgroundThemeBrush`, `MenuFlyoutBorderThemeBrush`, `MenuFlyoutPortraitBorderThemeThickness`,
`MenuFlyoutPortraitThemePadding`, `FlyoutThemeMinHeight`, `TextStyleExtraLargeFontSize`, `PhoneFontFamilySemiLight`,
`MenuFlyoutItemThemePadding`, `MenuFlyoutItemDisabledForegroundThemeBrush`, `PhoneForegroundBrush`; all in `Tokens/`.
The item themes: `Wp81MenuFlyoutItemTheme`, `Wp81MenuFlyoutSeparatorTheme`.

## The ladder

Rung 1 for the presenter's and the items' themes; rung 2 for the popup (`PhoneMenuFlyout`, on `PhonePopupHost`) and
the menu's height (`PhoneMenuFlyoutRoot`). No type is substituted.

## The checklist

[PLAN.md](PLAN.md#the-checklists), every row ticked or explained.

## Known gaps

- **An item is a raw pixel shorter** than the phone's (122 rows, not 123): our text line is Segoe WP's 1.33 em at
  25.5 px, the phone's SemiLight line is a row taller (ADR 0003). A menu is a row an item shorter; a menu above its
  target has its top that much lower.
- **Landscape** isn't drawn as the phone's (`LeftLandscape`, `RightLandscape`: the menu along a side, its own padding):
  there is no landscape capture. In landscape it is placed and opens as in portrait.
- **Sub-menus.** A `MenuItem` with items of its own shows no sub-menu in a MenuFlyout: a tap on it does nothing and the
  menu stays (8.1 has no sub-menus; the item template has no popup part).
- **The full-window layer** under the menu goes with it; the phone's goes about 300 ms later. It draws nothing.
