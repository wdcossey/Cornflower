# Menu

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#237](https://github.com/wdcossey/Cornflower/issues/237) · a look on the Pixel is owed (no timings)

Avalonia's `Menu`, a desktop menu bar, themed by type in `src/Cornflower.Themes.Wp81/Controls/Menu.axaml`. The phone had
none: its `CommandBar` and secondary commands were the app's menu.

## Usage

```xml
<Menu>
  <MenuItem Header="_file">
    <MenuItem Header="_open" InputGesture="Ctrl+O" />
    <Separator />
    <MenuItem Header="e_xit" />
  </MenuItem>
  <MenuItem Header="_edit" />
</Menu>
```

## What it sets

| Property | Value |
|---|---|
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal` at `TextStyleLargeFontSize` (18.14), the body's (rule 2), by `DynamicResource` |
| `ItemContainerTheme` | `Wp81PlainTopLevelMenuItemTheme`, its headers' |
| `Background` | none: the page's (rule 5) |

The headers, `Wp81PlainTopLevelMenuItemTheme` (in the same file): `Transparent`, in
`CornflowerPlainTopLevelMenuItemPadding` (9.5,8.5), the tilt on `ContentBorder` (rule 7). A header is
`PhoneAccentBrush` while selected (a pointer over it, the keyboard on it) and not pressed, and while its drop-down is
open (rule 3); disabled, `MenuFlyoutItemDisabledForegroundThemeBrush`. Avalonia clears the item theme from the items
below the headers, which are the plain [MenuItem](../MenuItem/README.md)'s.

## Resource keys

`CornflowerPlainTopLevelMenuItemPadding`, in `Tokens/Plain.axaml`: the phone's button sides (9.5) and a MenuFlyout
item's top and bottom (8.5).

## The template's parts

The bar: a `Border` (the app's `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`, `Padding`) round
`PART_ItemsPresenter`. A header: `LayoutRoot` round `ContentBorder` round `PART_HeaderPresenter` (access keys
recognised, the phone's line), and `PART_Popup`, its drop-down below it (`BottomEdgeAlignedLeft`, light-dismissed, the
bar passing the pointer through), on `Wp81PlainSubMenuTheme`, the MenuFlyout's surface, round Wp81's `ScrollViewer`.

## Known gaps

- No motion: a drop-down opens and closes as Avalonia's popup does (ADR 0007, rule 8).
- The phone's type and touch sizes: a drop-down's items are the MenuFlyout's (25.5), dense on a desktop. The
  Adaptive pack can make them denser (ADR 0004).
- A `Separator` straight in the bar is 0 wide and draws nothing.
