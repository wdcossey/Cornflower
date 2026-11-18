# CommandBar, AppBarButton, AppBarToggleButton and AppBarSeparator

Status: done · [#118](https://github.com/wdcossey/Cornflower/issues/118), and [#141](https://github.com/wdcossey/Cornflower/issues/141) (the shell's press, the open bar's stepped height, the opening and closing motion) · #141's device run passed 2026-11-16 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page commandbar` on `main` at `ae652d3`, 22:48 to 22:50, cold starts 716 to 826 ms over the run's pages: the page opened in 379 ms, the bar opening with its labels and its two menu items) · #141's real-window run passed 2026-11-16 (`tools/real-window/drive.ps1`, Debug, on #161's head `d0a9ef6`: 160 of 160, every page opened from the list, ready, and left by Back) · light's pressed and checked look brought to [#148](https://github.com/wdcossey/Cornflower/issues/148)'s reading (run `20261116-1724-dev11`) in [#168](https://github.com/wdcossey/Cornflower/issues/168) · owed for #168: the device run on the Pixel 8 and the real-window run · before #141: device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page commandbar` on `main` at `fedf781`: cold 751 ms, the page opened in 329 ms, the compact bar drawn with its three commands (checked and disabled among them) and the ellipsis) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the commandbar page opened in 46 ms with 119 elements)

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
- **Open** (`IsOpen`, a tap on the ellipsis, or on the minimal bar anywhere), either mode: the circles where they
  were with their labels under them, then the secondary commands as a list of words in `PhoneFontFamilySemiLight`
  27.92, a line 59.1666 each. The bar grows upwards to the shell's height for the list's lines: 89.1666 with none,
  167.5 with one, 285.4166 with two or three, 418.75 with four or more, the most; a longer list scrolls. The ellipsis
  closes it; so does invoking a command, and a press anywhere outside it, unless `IsSticky`.
- **The motion**: opening, the bar's top rises into place over 420 ms, most of the way in the first 200; closing, it
  falls over 200 ms, still showing its labels and its list. Frame by frame on `CornflowerClock`, as the shell's
  recording reads. The bar's layout height changes at once, so a page sees no motion; only the drawing moves.
- **The ellipsis always shows**: the phone's opens a bar with no secondary commands to show its labels.
- **States**: in dark, pressed fills the whole circle with the accent and shows the icon white, the circle sunk a
  little (smaller, and with the icon 2.08 higher), checked or not. In light, pressed is the checked look, with no
  accent: the circle black, its ring white, the icon the bar's `#DDDDDD`, at the circle's own size and 2.08 higher
  (#168). A checked toggle is filled with the ring's colour and shows the icon in the bar's; in light its ring is
  white. Disabled is the whole command at 0.451, as one layer. A pressed line of the list tilts and keeps its
  colours, in both themes.
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
`AppBarToggleButtonCheckedBackgroundThemeBrush`, `PhoneFontFamilyNormal`, `PhoneFontFamilySemiLight`,
`Wp81.Glyph.AppBarEllipsis`. The theme's own: `Wp81CommandBarCompactHeight` (62.9166) and `Wp81CommandBarMinimalHeight`
(26.25), the closed bar's heights; and, one for each theme, the press and the checked ring, which the shell draws
differently in dark and light (#168): `Wp81CommandBarPressedFillBrush`, `Wp81CommandBarPressedRingBrush`,
`Wp81CommandBarPressedIconBrush`, `Wp81CommandBarPressedCircleTransform` and `Wp81CommandBarCheckedRingBrush`. They
take the phone's colours (the accent, `PhoneTextOverAccentColor`, `PhoneForegroundColor`, `PhoneBackgroundColor`,
`PhoneChromeColor`), so an app's accent still shows in the dark press. The themes of the bar's own parts are
keyed `Wp81CommandBarItemsTheme` (its `ItemsControl`s) and `Wp81CommandBarEllipsisButtonTheme` (the ellipsis).

## The ladder

Rung 1, a theme, for the four types; rung 2 for `ClosedDisplayMode`, an attached property, for closing the bar
when a command is invoked (`PhoneAppBarClose`, internal to the theme), and for what the shell's bar does that XAML
can't (`PhoneAppBar`, internal to the theme): it counts the list's lines for the height's steps, runs the motion, and
opens the minimal bar on a tap. The menu lines' tilt is `cf:Tilt`. No type substitution.

## Known gaps

- **The icons are Lucide's**, at `LucideIcon`'s 20 × 20: smaller than the shell's Segoe UI Symbol glyphs (the plus
  inks 32 raw px where the shell's inks 51).
- **The pressed circle rises the same open or closed**: 2.08 view px, the closed bar's. The shell's rises toward the
  screen's centre, so on the open bar it rises 0.83 and ours is 1.25 view px higher (1.4 as the frames measure it). The press jumps there and back: the
  recording holds the press and doesn't show its edges. The same in light (#168).
- **A checked toggle, pressed, in dark**, shows the shell's press as an unchecked one does; the shell's isn't read in
  dark (the recording holds an unchecked command), and the template's ring alone isn't drawn. In light it is read
  (#168): the checked look, risen, as ours.
- **A list of more than five lines** scrolls inside the 418.75 bar; whether the shell's scrolls isn't read (its sixth
  line runs off the screen). Why the shell's heights step as they do isn't modelled (COMPARISON, "Are the open bar's
  steps whole menu lines?"): a style per count sets the read height.
- **The motion's clock**: the recording's times are taken as they are; whether they are the phone's stopwatch or its
  animation time is open (ADR 0005), at most 4%, 17 ms of the opening. Changing `ClosedDisplayMode` on a closed bar
  isn't moved (the shell moves it over 36 to 134 ms). A bar whose app sets its `Height` doesn't move. While it
  closes, what is drawn above the closed bar takes no presses (Avalonia hit-tests a control within its bounds), so a
  press there reaches the page; the shell's isn't read. A bar with an app's own `DefaultLabelPosition` falls to the
  compact height and then takes its own, taller one, at once.
- **A window shorter than the open bar** cuts its top, ellipsis and all: the heights are the shell's, and the phone's
  view is never shorter than 450 (landscape), above the 418.75 limit.
- **Closing draws above the closed bar**: a host that clips the bar to its bounds (a `Border` or a cell with
  `ClipToBounds`) cuts the falling bar, which then looks shut at once. The ContentPage's bars don't clip.
- **The open heights are the shell's, fixed by the count**: a label that wraps to a second line, or an app's
  `Content`, can be cut by them (with no list, the bar is 89.1666), where #133's bar grew to fit.
- **Invoking a command closes the bar** (`PhoneAppBarClose`, internal to the theme), as the phone's does. A command
  with a `Flyout` leaves it open, and a bound `Command` runs before it closes: the phone raises `Closed`, then `Click`
  2 to 21 ms later; ours raises `Click`, then closes.
- **A tap on the compact bar away from its commands** does nothing: the shell's isn't read. On the minimal bar it
  opens the bar, as read.
- **At most four primary commands**, as on the phone: without dynamic overflow a fifth runs under the ellipsis,
  which then takes the presses there.
- **Avalonia's `Content`** is drawn at the bar's left, under the commands: the phone's bar has none, and the commands
  stay centred on the bar.
- **A toggle among the secondary commands** shows no checked state: the phone's menu has no toggles.
- **`Bounds.Height` is the drawn 62.9166**, where the phone's `ActualHeight` is 57.5, and the open bar's control
  grows where the phone's stays 57.5. While the bar moves, its drawing (the template's `LayoutRoot`) is above or
  below its bounds' top: closing, it reaches above the closed bar until it is down.
- **A command off a bar** looks as on the bar, where the phone's template draws a smaller one
  ([`ref/wp81/AppBarButton/`](../../../ref/wp81/AppBarButton/README.md)).
- **The checked circle in dark** is 98 raw px across; the phone's is 94 (98 in light).
- **Avalonia's `DefaultLabelPosition` Right** isn't drawn as Avalonia's Simple theme draws it: the phone has no such
  layout, and the label stays under the circle.
- Landscape isn't read.
