# ContextMenu

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#237](https://github.com/wdcossey/Cornflower/issues/237) · a look on the Pixel is owed (no timings)

Avalonia's `ContextMenu`, themed by type in `src/Cornflower.Themes.Wp81/Controls/ContextMenu.axaml`. The phone had no
ContextMenu: an 8.1 app showed a `MenuFlyout` on hold. So it takes the **MenuFlyout's look and motion** (ADR 0007,
answer 7), labelled plain, as `ContextMenu` itself isn't the phone's. Avalonia opens one on a right click, the
context-menu key, and a long press (a touch hold), so a phone app on Android meets it.

## Usage

```xml
<TextBlock Text="hold me">
  <TextBlock.ContextMenu>
    <ContextMenu>
      <MenuItem Header="copy" />
      <MenuItem Header="share" />
    </ContextMenu>
  </TextBlock.ContextMenu>
</TextBlock>
```

## What it sets

The MenuFlyoutPresenter theme's setters, by the same keys: `MenuFlyoutBackgroundThemeBrush`,
`MenuFlyoutBorderThemeBrush`, `MenuFlyoutPortraitBorderThemeThickness`, `MenuFlyoutPortraitThemePadding`,
`FlyoutThemeMinHeight` and `PhoneForegroundBrush`; the type is its items' own (the [MenuItem](../MenuItem/README.md)
theme's, the MenuFlyout's). `Focusable`, as Simple's, so the keys reach its items as it opens.

## How it opens and closes

On the MenuFlyout's own code (`PhoneContextMenu`, on `PhoneMenuFlyout`'s binder and the `PhonePopupHost`), as a
MenuFlyout does:

- In the window's overlay layer, the window's width; below its target if it fits there, else above it, kept in the
  window. The target is the menu's `PlacementTarget`, else the control that owns it: not the element a press or a
  hold came from (a Button's text), so it is placed by the whole control.
- The MenuFlyout's opening: the line by the target spreads over 380 ms, then the menu unfolds until 500 ms, on
  `CornflowerClock` at `CornflowerConfig.PopupSpeed`; `MenuFlyoutSingleStepOpen` and `Animated` apply.
- Closed at once: an item's click, `Close()`, Back (taken, as the MenuFlyout's), or a tap outside (Avalonia's light
  dismiss, with the ContextMenu's own pass-through of that tap to the page).

## The template's parts

The MenuFlyoutPresenter's: `PhoneMenuFlyoutRoot` round `OuterBorder` round `CenterBorder` round `InnerBorder` round
`PART_ItemsPresenter` (a shared size scope, so the items share one icon column). Its own `Separator`s take the
MenuFlyout's `Wp81MenuFlyoutSeparatorTheme`: nothing, 0 tall.

## Known gaps

- No `ScrollViewer`: a menu longer than the window is cut off, as a MenuFlyout's.
- A `MenuItem` with `Header="-"` in it is the plain line, where its `Separator`s draw nothing.
- `Placement`, the offsets and `PlacementRect` are the phone's popup host's, not the app's.
- A plain-menu look (Simple's shape in the plain style, no motion), behind a `CornflowerConfig` value, is for the
  maintainer's review (ADR 0007); not built.
