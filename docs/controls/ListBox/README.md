# ListBox and ListBoxItem

Status: done · [#212](https://github.com/wdcossey/Cornflower/issues/212) · against #211's captures (run `20261118-2210-dev11`) · device run passed 2026-11-21 (Pixel 8, Release, over wireless adb, on `45b8c0a`: cold start 770 ms, the page opened in 400 ms; at rest, a press (the tilt, paler), a tap (the accent moves), a press on the selected row (the pale tint, black text), the disabled list (no response) and the light list, each as described) · real-window run passed 2026-11-21 (`tools/real-window/drive.ps1`, Debug, on `45b8c0a`: 200 of 200)

The Windows Phone 8.1 ListBox and ListBoxItem, the WinRT list 8.1 brought over from Windows 8: Avalonia's `ListBox` and `ListBoxItem`
in two keyed themes, `Wp81ListBoxTheme` and `Wp81ListBoxItemTheme`, with the phone's default styles and the item's
template, in `src/Cornflower.Themes.Wp81/Controls/ListBox.axaml`; the press the Button's tilt. The plan, with every value
and its source, is [PLAN.md](PLAN.md); the phone's captures beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

A `ListBox` with no style is the 8.1 **ListView** ([ListView](../ListView/README.md)), as an 8.1 app's list is. For the
phone's ListBox, name its theme; it gives its items theirs:

```xml
<ListBox Theme="{DynamicResource Wp81ListBoxTheme}" ItemsSource="{Binding Names}" />
```

- An item declared on its own, outside such a list, takes `Theme="{DynamicResource Wp81ListBoxItemTheme}"`.
- **No check mode and no item click.** The phone's ListBox has neither (they are `ListViewBase`'s): `SelectionMode` is
  Avalonia's, and `Multiple` selects several in the accent, with no check boxes. Don't set `ListBoxOptions.IsItemClickEnabled`
  on it: it would work, as on any ListBox, but the phone's ListBox has no `ItemClick`.
- The list holds the phone's ScrollViewer, with its thin indicator; set `ScrollViewer.*` on the ListBox as on the phone.
  A ListBox in a page's ScrollViewer needs a height, as on the phone.

## Behaviours

- **The list's style**: a pale box in the dark theme, `ListBoxBackgroundThemeBrush` (#CCFFFFFF; light #BF000000), drawn
  inside a transparent 1.5 border; black text in both (`ListBoxForegroundThemeBrush`), `ContentControlThemeFontFamily` at
  `ControlContentThemeFontSize` (12); `IsTabStop` off, `TabNavigation` once; no horizontal scroll, the vertical
  indicator `Auto`; items in a `VirtualizingStackPanel`.
- **The light theme is hard to read, as on the phone.** The phone's palette keys that give the dark theme its pale box
  give the light theme black text on a 75% black box (about #404040 over the white page), and a press draws the same;
  only the selected item, white on the accent, reads well. This is 8.1's own: the Update 1 image's `themeresources.xaml`
  (Light: `PhoneBaseMediumHighColor` #BF000000, `PhoneBaseBlackColor` #FF000000) and the emulator's capture
  ([`light__ListBox__three-items__base.png`](../../../ref/wp81/ListBox/light__ListBox__three-items__base.png)) agree.
  Cornflower keeps it faithful. For a readable light list, an app overrides `ListBoxBackgroundThemeBrush` and
  `ListBoxItemPressedBackgroundThemeBrush` (and, if it changes the text, `ListBoxForegroundThemeBrush` and
  `ListBoxItemPressedForegroundThemeBrush`) in its own `Light` theme dictionary; setting the ListBox's `Background` alone
  fixes the list at rest but not the press. Or it uses the [ListView](../ListView/README.md).
- **The item's style**: a transparent background, padding 6.5, 8, the content at the left and centred down; 32.08 tall
  for one line of text, in the phone's 1.33 em line.
- **Selected**: the item in the accent, its text white, in both themes.
- **The press**: at once, the item's background in `ListBoxItemPressedBackgroundThemeBrush` (the list's pale brush) and its
  text black, with the Button's tilt on `InnerGrid` inside an untilted root, so a press at the very edge still selects;
  on release it holds 182.6 ms and eases back over 167.0 ms. A press on a selected item draws the pale brush and the
  press's black text.
- **Disabled**: the item's text dimmed (#80FFFFFF; light #80000000); a selected one #73FFFFFF on #66FFFFFF. A disabled
  list loses its background and draws its border in `ListBoxDisabledForegroundThemeBrush`.
- **Focus and pointer-over** draw nothing.

The tests are `tests/Cornflower.Tests/Controls/ListBoxTests.cs` and `Sample/ListBoxPageTests.cs`; PLAN.md lists each
behaviour beside its test. The sample's page is `listbox`.

## Resource keys

The themes' own: `Wp81ListBoxTheme`, `Wp81ListBoxItemTheme`. The phone's, in `Tokens/`: `ListBoxForegroundThemeBrush`,
`ListBoxBackgroundThemeBrush`, `ListBoxBorderThemeBrush`, `ListBoxBorderThemeThickness`,
`ListBoxDisabledForegroundThemeBrush`, `ContentControlThemeFontFamily`, `ControlContentThemeFontSize`,
`ListBoxItemPressedBackgroundThemeBrush`, `ListBoxItemPressedForegroundThemeBrush`,
`ListBoxItemSelectedBackgroundThemeBrush`, `ListBoxItemSelectedForegroundThemeBrush`,
`ListBoxItemSelectedPointerOverBackgroundThemeBrush`, `ListBoxItemSelectedDisabledBackgroundThemeBrush`,
`ListBoxItemSelectedDisabledForegroundThemeBrush`, `ListBoxItemDisabledForegroundThemeBrush`.

## The template's parts

The phone's names: the list's `LayoutRoot`; the item's `LayoutRoot`, `InnerGrid` and `PressedBackground`. The content
presenter is Avalonia's `PART_ContentPresenter`; the list's ScrollViewer `PART_ScrollViewer`, its presenter
`PART_ItemsPresenter`.

## The ladder

Rung 1, the themes, keyed; rung 2, `Tilt` (the press). No type is substituted.

## Extensions

None.

## Known gaps

- **An item with `IsSelected` outside a list** draws selected; the phone's draws it unselected.
- **A press or Disabled on a selected item**: which state's text colour the phone draws wasn't read; ours, a
  press's black (legible on its pale brush), and SelectedDisabled's.
- **No grouping** (Avalonia 12.1.4 has none), and no add and delete transitions.
- **The scroll physics** are Avalonia's (the ScrollViewer's recorded deviation).
- **One raw pixel** here and there (ADR 0003).

## The checklist

In [PLAN.md](PLAN.md), "Checklists": ListBox and ListBoxItem.
