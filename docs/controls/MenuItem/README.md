# MenuItem

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#237](https://github.com/wdcossey/Cornflower/issues/237) · a look on the Pixel is owed (no timings)

Avalonia's `MenuItem`, themed by type in `src/Cornflower.Themes.Wp81/Controls/MenuItem.axaml`: the items of a
`ContextMenu` and of a sub-menu (a desktop `Menu`'s drop-down included). The phone had no MenuItem; its
MenuFlyoutItem is Wp81's keyed `Wp81MenuFlyoutItemTheme`, which a `MenuFlyout`'s own items still wear. This is that
item, with what a desktop menu has and the phone's didn't: an icon, a check, gesture text and a sub-menu. A `Menu`'s
own headers wear `Wp81PlainTopLevelMenuItemTheme` instead ([Menu](../Menu/README.md)).

## Usage

```xml
<ContextMenu>
  <MenuItem Header="_copy" InputGesture="Ctrl+C">
    <MenuItem.Icon><cf:LucideIcon Kind="Copy" /></MenuItem.Icon>
  </MenuItem>
  <MenuItem Header="word wrap" ToggleType="CheckBox" IsChecked="True" />
  <MenuItem Header="sort by">
    <MenuItem Header="name" ToggleType="Radio" GroupName="sort" IsChecked="True" />
    <MenuItem Header="date" ToggleType="Radio" GroupName="sort" />
  </MenuItem>
</ContextMenu>
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent`, so the whole row takes the finger |
| `Padding` | `MenuFlyoutItemThemePadding` (0,8.5), the MenuFlyout item's, by `DynamicResource` |
| `FontFamily`, `FontSize` | `PhoneFontFamilySemiLight` at `TextStyleExtraLargeFontSize` (25.5), the MenuFlyout's, by `DynamicResource` |
| `LetterSpacing` | the MenuFlyout's -15 character spacing, through `PhoneCharacterSpacing` |
| `Tilt.IsEnabled` | true: the tilt on `ContentBorder` (rule 7) |
| `FocusAdorner` | none |

The type is set on the item, as an item inherits from the item or menu above it, not from the popup it is drawn in.

| State | Look |
|---|---|
| Selected (a pointer over it, the keyboard on it), not pressed | header, icon and chevron in `PhoneAccentBrush` (rule 3) |
| Pressed | the tilt alone, as the MenuFlyout's |
| Checked (`ToggleType` `CheckBox` / `Radio`) | `Wp81.Glyph.CheckMark` / a 10 dot, in `PhoneAccentBrush` |
| Disabled | `MenuFlyoutItemDisabledForegroundThemeBrush`, the MenuFlyout's, on all of it |
| `Header="-"` | the plain `Separator`'s line |

## Resource keys

In `Tokens/Plain.axaml`:

- `CornflowerPlainMenuItemIconSize` (20, LucideIcon's own size) and `CornflowerPlainMenuItemIconMargin` (9.5 after it).
- `CornflowerPlainMenuItemRadioMargin` (5: a 10 dot in the 20 slot).
- `CornflowerPlainMenuItemGestureMargin` (19 before the gesture text, the MenuFlyout's left padding).
- `CornflowerPlainSubMenuPadding` (19,8.5), the sub-menu's.
- `CornflowerPlainChevronMargin` (9.5 before the chevron), the DropDownButton's.

And `CornflowerPlainKeyGestureConverter`, in the theme file: Avalonia's `PlatformKeyGestureConverter`.

## The template's parts

- `LayoutRoot` (the `Background`) round `ContentBorder` (the `Padding`, `Tilt.IsTarget`) round a grid of:
  - `PART_IconColumn`, 20 square, shown for an `Icon` or a `ToggleType`, as wide in every item of the menu (a shared
    size group): `PART_IconPresenter` (the `Icon`), `PART_Check`, `PART_Radio`.
  - `PART_HeaderPresenter`: the `Header`, left and centred, at the phone's 1.33 em line (`PhoneContentLine`), access
    keys recognised.
  - `PART_InputGestureText`: the `InputGesture` in `PhoneMidBrush`, the item's size, shown only when there is one.
  - `PART_Chevron`: Lucide's `ChevronRight` (rule 10: Lucide in a plain theme), for a sub-menu.
- `PART_Popup`: the sub-menu, to the right (`RightEdgeAlignedTop`), not light-dismissed, its surface
  `PART_SubMenu` in `Wp81PlainSubMenuTheme` (`Controls/PlainSubMenu.axaml`: `MenuFlyoutBackgroundThemeBrush`,
  `MenuFlyoutBorderThemeBrush` in the plain 1.5 line, square, `CornflowerPlainSubMenuPadding`) round Wp81's
  `ScrollViewer` and `PART_ItemsPresenter`. The 19 at its sides indents a `Separator` with the items' text.

## Known gaps

- No motion: a sub-menu opens and closes as Avalonia's popup does (ADR 0007, rule 8).
- A sub-menu opens to the right of its item. In a `ContextMenu`, which is the window's width, there is no room there,
  so it goes where Avalonia's positioner puts it, over the menu.
- The gesture text is in the item's size, not the small print's (16).
