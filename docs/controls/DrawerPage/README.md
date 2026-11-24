# DrawerPage

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#239](https://github.com/wdcossey/Cornflower/issues/239) · a look on the Pixel is owed (no timings)

Avalonia's `DrawerPage`, themed by type in `src/Cornflower.Themes.Wp81/Controls/DrawerPage.axaml`. The phone had no
DrawerPage (the hamburger came with Windows 10). Simple's template, with the plain look: the page's background and type, the
drawer in the plain [`SplitView`](../SplitView/README.md), and the bar in Wp81's `NavigationPage` bar's look.

## Usage

```xml
<DrawerPage Header="mail">
  <DrawerPage.Drawer>
    <StackPanel>
      <TextBlock Text="inbox" />
      <TextBlock Text="sent" />
    </StackPanel>
  </DrawerPage.Drawer>
  <ContentPage>
    <TextBlock Text="the inbox" />
  </ContentPage>
</DrawerPage>
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `ApplicationPageBackgroundThemeBrush`, as Wp81's `ContentPage` |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` |
| `DrawerBackground` | `FlyoutBackgroundThemeBrush`: the drawer is a floating surface (rule 5) |
| `Template` | Simple's, with the parts below |

## Resource keys

- `CornflowerPlainDrawerBarHeight` (48, in `Tokens/Plain.axaml`): the bar's height, Avalonia's `NavigationPage.BarHeight`
  default, which Wp81's `NavigationPage` keeps, so the two bars match. The drawer button is as wide.
- `Wp81PlainHeaderToggleButtonTheme` for the drawer buttons (see [`Expander`](../Expander/README.md)).

## The template's parts

- `PART_SplitView`: the plain `SplitView` by type, on `DrawerBackground`, with no motion of its own (rule 8).
- `PART_TopBar` and `PART_BottomBar` (for a drawer at the bottom): `PhoneChromeBrush`, 48 tall, the page's `Header` in
  `PhoneFontFamilySemiBold` at `TextStyleLargeFontSize` (18.14), as Wp81's `NavigationPage` bar draws it.
- `PART_PaneButton`, `PART_BottomPaneButton`, `PART_CompactPaneToggle`: the drawer buttons, `ToggleButton`s in
  `Wp81PlainHeaderToggleButtonTheme`, with the tilt (rule 7). Each draws Lucide's `Menu` (rule 10), or the page's
  `DrawerIcon` in its place. Simple's template held them as plain `ToggleButton`s, which fell back to the theme under Wp81
  (`HoleParts`); these name their own theme.
- `PART_Backdrop`: the app's `BackdropBrush`, unset, so none (rule 5).

Avalonia's own behaviour is kept: the edge swipe, the breakpoint, the bar hidden when the content is a `NavigationPage`
with its own bar.

## Known gaps

- A `DrawerIcon` that is a control shows in one button only (Avalonia's: a control has one parent); give a string or a
  template.
- The drawer's header and footer take the app's brushes (`DrawerHeaderBackground` and the rest), unset.
