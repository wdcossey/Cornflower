# CommandBar, AppBarButton, AppBarToggleButton and AppBarSeparator

Status: done · [#118](https://github.com/wdcossey/CornflowerUI/issues/118) · device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page commandbar` on `main` at `fedf781`: cold 751 ms, the page opened in 329 ms, the compact bar drawn with its three commands (checked and disabled among them) and the ellipsis) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the commandbar page opened in 46 ms with 119 elements)

The Windows Phone 8.1 page's app bar: Avalonia 12's `CommandBar`, `CommandBarButton`, `CommandBarToggleButton` and
`CommandBarSeparator`, themed by `Wp81Theme` in `src/Cornflower.Themes.Wp81/Controls/CommandBar.axaml` as the
phone's shell draws a page's `BottomAppBar`. WinRT's `ClosedDisplayMode` is `CommandBarOptions.ClosedDisplayMode`, in
`Cornflower.Controls`. [PLAN.md](PLAN.md) has every value and its source; [COMPARISON.md](COMPARISON.md) has the
shell's screenshots beside ours.

## Usage

```xml
<CommandBar cf:CommandBarOptions.ClosedDisplayMode="Compact">
  <CommandBarButton Label="add" Click="OnAdd">
    <CommandBarButton.Icon>
      <cf:LucideIcon Kind="Plus" />
    </CommandBarButton.Icon>
  </CommandBarButton>
  <CommandBarToggleButton Label="favorite">
    <CommandBarToggleButton.Icon>
      <cf:LucideIcon Kind="Star" />
    </CommandBarToggleButton.Icon>
  </CommandBarToggleButton>
  <CommandBar.SecondaryCommands>
    <CommandBarButton Label="settings" />
  </CommandBar.SecondaryCommands>
</CommandBar>
```

WinRT's names map to Avalonia's: `AppBarButton` is `CommandBarButton`, `AppBarToggleButton` `CommandBarToggleButton`,
`AppBarSeparator` `CommandBarSeparator`. A WinRT `Symbol` icon is a `cf:LucideIcon` of the Lucide icon
`tools/lucide/symbols.tsv` maps it to.

## On a page

The phone's shell draws the page's bar over the page and keeps its closed height off the page's visible area
(`VisibleBounds`). The bar is a control here, so the page does that:

- Lay the bar over the page's content at its foot (a `Panel` or a `Grid` cell they share, not a `DockPanel`), so the
  open bar covers the content rather than pushing it up. The bar's `VerticalAlignment` is `Bottom`; open, it grows
  upwards.
- Keep the closed bar's height free at the content's foot: 62.9166 compact (151 raw px at 2.4, the phone's 62.92),
  26.25 minimal, the theme's keys `Wp81CommandBarCompactHeight` and `Wp81CommandBarMinimalHeight`. The sample's page
  binds its `ScrollViewer`'s `Padding` to them.

## What it does

- **Closed, compact**: 62.9166 tall, the bar's colour (`CommandBarBackgroundThemeBrush`), the primary commands as
  circles 40.75 across in 73.75 slots, centred, and the ellipsis at the top right. No labels.
- **Closed, minimal** (`ClosedDisplayMode` Minimal): 26.25 tall, the ellipsis alone.
- **Open** (`IsOpen`, or a tap on the ellipsis), either mode: the circles where they were with their labels under
  them, then the secondary commands as a list of words in `PhoneFontFamilySemiLight` 27.92, a line 59.1666 each, and
  the bar's foot (none without secondary commands); the bar as tall as that, growing upwards, and a list longer than
  the room scrolls. The ellipsis closes it; so does invoking a command, and a press anywhere outside it, unless
  `IsSticky`.
- **The ellipsis always shows**: the phone's opens a bar with no secondary commands to show its labels.
- **States**: pressed fills the circle and shows the icon in the bar's colour; a checked toggle is filled the same,
  and shows the ring alone while pressed; disabled is the whole command at 0.451, as one layer.
- **`CommandBarSeparator`** draws nothing and takes no room: the phone has no style for `AppBarSeparator`.
- An app's `DefaultLabelPosition` shows the labels closed too, the bar as tall as its commands; the theme sets
  Collapsed closed and Bottom open.
- **Dynamic overflow** (`IsDynamicOverflowEnabled`) keeps the centred row clear of the ellipsis: the theme's
  `Padding`, which nothing draws, is the room Avalonia's reckoning needs for that, so four commands fit on the 450
  view, the phone's most.

## The checklist

[PLAN.md](PLAN.md), "The checklists": every row ticked or left out with its reason.

## Extensions

`CommandBarOptions.ClosedDisplayMode` (attached, on a `CommandBar`), of `AppBarClosedDisplayMode`: `Compact` (0, the
default) or `Minimal` (1), WinRT's `AppBar.ClosedDisplayMode` and its 8.1 members. Data only: the theme reads it.

## Resource keys

`CommandBarBackgroundThemeBrush`, `CommandBarForegroundThemeBrush`,
`AppBarItemForegroundThemeBrush`, `AppBarItemBackgroundThemeBrush`, `AppBarToggleButtonCheckedBorderThemeBrush`,
`AppBarToggleButtonCheckedBackgroundThemeBrush`, `AppBarToggleButtonCheckedPressedBorderThemeBrush`,
`AppBarToggleButtonCheckedPressedBackgroundThemeBrush`, `AppBarToggleButtonCheckedPressedForegroundThemeBrush`,
`PhoneFontFamilyNormal`, `PhoneFontFamilySemiLight`, `Wp81.Glyph.AppBarEllipsis`. The theme's own:
`Wp81CommandBarCompactHeight` (62.9166) and `Wp81CommandBarMinimalHeight` (26.25), the closed bar's heights. The themes of the bar's own parts are
keyed `Wp81CommandBarItemsTheme` (its `ItemsControl`s) and `Wp81CommandBarEllipsisButtonTheme` (the ellipsis).

## The ladder

Rung 1, a theme, for the four types; rung 2 for `ClosedDisplayMode`, an attached property, and for closing the bar
when a command is invoked (`PhoneAppBarClose`, internal to the theme). No type substitution.

## Known gaps

- **The icons are Lucide's**, at `LucideIcon`'s 20 × 20: smaller than the shell's Segoe UI Symbol glyphs (the plus
  inks 32 raw px where the shell's inks 51).
- **The opening and closing motion** isn't read; the bar opens at once.
- **The open bar with other numbers of secondary commands** isn't read: ours keeps the foot the shell draws under two
  for any number, draws none with none, and scrolls a list longer than the room.
- **A pressed command and a pressed secondary command** on the shell's bar aren't read: the pressed command is the
  template's state with the bar's colour; a secondary command shows no press.
- **Invoking a command closes the bar** (`PhoneAppBarClose`, internal to the theme), as Avalonia documents
  `IsSticky`; it isn't read on the emulator. A command with a `Flyout` leaves it open, and a bound `Command` runs
  before it closes.
- **At most four primary commands**, as on the phone: without dynamic overflow a fifth runs under the ellipsis,
  which then takes the presses there.
- **Avalonia's `Content`** is drawn at the bar's left, under the commands: the phone's bar has none, and the commands
  stay centred on the bar.
- **A toggle among the secondary commands** shows no checked state, and a pressed line no press: the phone's menu has
  no toggles, and its pressed line isn't read.
- **`Bounds.Height` is the drawn 62.9166**, where the phone's `ActualHeight` is 57.5, and the open bar's control
  grows where the phone's stays 57.5.
- **A command off a bar** looks as on the bar, where the phone's template draws a smaller one
  ([`ref/wp81/AppBarButton/`](../../../ref/wp81/AppBarButton/README.md)).
- **The checked circle in dark** is 98 raw px across; the phone's is 94 (98 in light).
- **Avalonia's `DefaultLabelPosition` Right** isn't drawn as Avalonia's Simple theme draws it: the phone has no such
  layout, and the label stays under the circle.
- Landscape isn't read.
