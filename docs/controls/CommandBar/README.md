# CommandBar, AppBarButton, AppBarToggleButton and AppBarSeparator

Status: done · [#118](https://github.com/wdcossey/CornflowerUI/issues/118) · owed: the device run on the Pixel 8 · owed: the real-window run

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
  26.25 minimal. The sample's page does it with its `ScrollViewer`'s `Padding`.

## What it does

- **Closed, compact**: 62.9166 tall, the bar's colour (`CommandBarBackgroundThemeBrush`), the primary commands as
  circles 40.75 across in 73.75 slots, centred, and the ellipsis at the top right. No labels.
- **Closed, minimal** (`ClosedDisplayMode` Minimal): 26.25 tall, the ellipsis alone.
- **Open** (`IsOpen`, or a tap on the ellipsis), either mode: the circles where they were with their labels under
  them, then the secondary commands as a list of words in `PhoneFontFamilySemiLight` 27.92, a line 59.1666 each, and
  the bar's foot; the bar as tall as that, growing upwards. The ellipsis closes it, and so does a press anywhere
  outside it, unless `IsSticky`.
- **The ellipsis always shows**: the phone's opens a bar with no secondary commands to show its labels.
- **States**: pressed fills the circle and shows the icon in the bar's colour; a checked toggle is filled the same,
  and shows the ring alone while pressed; disabled is the whole command at 0.451, as one layer.
- **`CommandBarSeparator`** draws nothing and takes no room: the phone has no style for `AppBarSeparator`.
- An app's `DefaultLabelPosition` shows the labels closed too; the theme sets Collapsed closed and Bottom open.

## The checklist

[PLAN.md](PLAN.md), "The checklists": every row ticked or left out with its reason.

## Extensions

`CommandBarOptions.ClosedDisplayMode` (attached, on a `CommandBar`), of `AppBarClosedDisplayMode`: `Compact` (0, the
default) or `Minimal` (1), WinRT's `AppBar.ClosedDisplayMode` and its 8.1 members. Data only: the theme reads it.

## Resource keys

`CommandBarBackgroundThemeBrush`, `CommandBarForegroundThemeBrush`, `AppBarBorderThemeBrush`,
`AppBarItemForegroundThemeBrush`, `AppBarItemBackgroundThemeBrush`, `AppBarToggleButtonCheckedBorderThemeBrush`,
`AppBarToggleButtonCheckedBackgroundThemeBrush`, `AppBarToggleButtonCheckedPressedBorderThemeBrush`,
`AppBarToggleButtonCheckedPressedBackgroundThemeBrush`, `AppBarToggleButtonCheckedPressedForegroundThemeBrush`,
`PhoneFontFamilyNormal`, `PhoneFontFamilySemiLight`, `Wp81.Glyph.AppBarEllipsis`. The themes of the bar's own parts are
keyed `Wp81CommandBarItemsTheme` (its `ItemsControl`s) and `Wp81CommandBarEllipsisButtonTheme` (the ellipsis).

## The ladder

Rung 1, a theme, for the four types; rung 2 for `ClosedDisplayMode`, an attached property. No type substitution.

## Known gaps

- **The icons are Lucide's**, at `LucideIcon`'s 20 × 20: smaller than the shell's Segoe UI Symbol glyphs (the plus
  inks 32 raw px where the shell's inks 51).
- **The opening and closing motion** isn't read; the bar opens at once.
- **The open bar with other numbers of secondary commands** isn't read: ours keeps the foot the shell draws under two,
  and a list longer than the screen doesn't scroll.
- **A pressed command and a pressed secondary command** on the shell's bar aren't read: the pressed command is the
  template's state with the bar's colour; a secondary command shows no press.
- **Invoking a command doesn't close the bar**, as Avalonia's doesn't; whether the phone's does isn't read.
- **`Bounds.Height` is the drawn 62.9166**, where the phone's `ActualHeight` is 57.5, and the open bar's control
  grows where the phone's stays 57.5.
- **A command off a bar** looks as on the bar, where the phone's template draws a smaller one
  ([`ref/wp81/AppBarButton/`](../../../ref/wp81/AppBarButton/README.md)).
- **The checked circle in dark** is 98 raw px across; the phone's is 94 (98 in light).
- **Avalonia's `DefaultLabelPosition` Right** isn't drawn as Avalonia's Simple theme draws it: the phone has no such
  layout, and the label stays under the circle.
- Landscape isn't read.
