# LoopingSelector and LoopingSelectorItem

Status: done · [#154](https://github.com/wdcossey/Cornflower/issues/154) · device run passed 2026-11-17 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page loopingselector` on `main` at `9a24f7f`, 17:40 to 17:44, cold starts 747 to 813 ms over the run's pages: the page opened in 398 ms; its wheels opened by a tap and drawn right) · real-window run passed 2026-11-17 (`tools/real-window/drive.ps1`, Debug, on `main` at `03082b3`: 195 of 195) · owed: a capture of a wheel expanded

The Windows Phone 8.1 date and time pickers' wheel: Avalonia's `DateTimePickerPanel` in `Wp81LoopingSelectorTheme`
(`src/Cornflower.Themes.Wp81/Controls/LoopingSelector.axaml`), its items in `Wp81LoopingSelectorItemTheme`, driven by
`PhoneLoopingSelector` (internal): dragged, flicked and let go as the phone's, coming to rest on an item, a tap only
making it the active wheel, the active wheel showing every item and the others their selected one.
[PLAN.md](PLAN.md) has every value and its source, and why it is Avalonia's panel and not a new control;
[COMPARISON.md](COMPARISON.md) has the emulator's screenshots beside ours.

## Usage

The phone's LoopingSelector is a part of its DatePickerFlyout and TimePickerFlyout, which an app can't make on its own,
and Avalonia's `DatePickerPresenter` and `TimePickerPresenter` take a `DateTimePickerPanel` for each column. So the
wheel is that panel, with the theme by key:

```xml
<DateTimePickerPanel Theme="{DynamicResource Wp81LoopingSelectorTheme}"
                     PanelType="Month" ItemFormat="MM&#10;MMMM" ShouldLoop="True"
                     MaximumValue="12" MinimumValue="1" SelectedValue="7"
                     Width="138.333" Height="700" />
```

- **Not in a ScrollViewer.** Put the panel straight in its column. The behaviour moves it; a ScrollViewer would clamp
  a panel that doesn't loop short of its last items.
- **Two lines.** `ItemFormat`'s first line is drawn large, the rest small below it, as the phone's `PrimaryText` and
  `SecondaryText`: `"MM\nMMMM"` gives 07 over July, `"dd\ndddd"` 15 over Tuesday, `"yyyy"` the year alone. The day's
  name is for the month the panel's presenter tells it (`FormatDate`, internal to Avalonia). A bare panel names this
  month's days, so give it no more days than this month has: Avalonia's panel throws formatting a day the month
  lacks.
- **The active wheel** is the one last tapped or dragged among the wheels in the same template (or, outside one, the
  same window). The others show only their selected item.
- **Sizes.** The phone's columns are 138.333 wide and 700 tall; the items are 123 square drawn 1.125 larger, 138.375
  apart (`Wp81LoopingSelectorItemPitch`, the panel's `ItemHeight`).

The sample's `loopingselector` page has three wheels as the DatePickerFlyout lays them out.

### For DatePicker and TimePicker (#156)

#156 themes Avalonia's presenters. In each presenter's template, each required selector part is a panel in this theme,
in its host panel, with no ScrollViewer:

```xml
<Panel Name="PART_MonthHost">
  <DateTimePickerPanel Name="PART_MonthSelector" Theme="{DynamicResource Wp81LoopingSelectorTheme}"
                       PanelType="Month" ItemFormat="MM&#10;MMMM" ShouldLoop="True" />
</Panel>
```

The presenter sets the range, the selection and the day names, as it does for any template. Its up and down buttons
(`PART_MonthUpButton` and so on) are optional: the phone has none. The wheels in one presenter's template are one group,
so tapping one dims the others. The panel's `SelectionChanged` comes on every move (below), which the presenter uses to
keep its columns in step; the picker's own `SelectedDate` and `SelectedTime` change only at its accept button.

## Behaviours

- At rest, only each wheel's selected item shows: the accent, white text, no border.
- A tap on any item makes its wheel the active one: every item shows, bordered 2.5 in
  `LoopingSelectorForegroundThemeBrush`, the selected one in the accent. The first time at once; later, fading over
  0.33 s in a straight line, and the last active wheel fading out. A tap selects nothing and moves nothing.
- A drag (touch or pen, up or down past Avalonia's start distance) moves the items with the finger and makes the wheel
  active; no item shows as selected until it comes to rest.
- Let go, it goes to the item nearest where a coast at the finger's speed would stop, decided at once: on by
  `dv/dt = −a − c v²` (a 0.000785 px/ms², c 0.00049 /px, the wheel's own fit of #113's law), or, let go short of half
  an item past one with the finger still, a drift of 8.4 px on and a sine ease back over 46 √d ms.
- At rest its item is the panel's `SelectedValue`, shown in the accent; a looping panel is put back in the middle of its
  extent.
- Month and day (`ShouldLoop`) go round; the year stops at its first and last.
- A press on a moving wheel holds it; let go, it goes to the nearer item.
- A notch of the mouse wheel turns it one item (Cornflower's: the phone has no mouse). The panel's own keys (Up, Down,
  Page Up, Page Down) move it at once, as Avalonia's.
- All of it is frame by frame on `CornflowerClock`. Out of its window, a move or a fade ends where it was going.

## The checklist

[PLAN.md](PLAN.md#the-checklists): every row ticked or explained. None of the phone's three types is declared: an app
can't make one. Avalonia's `DateTimePickerPanel` stands for `LoopingSelector` and `LoopingSelectorPanel`, its
`ListBoxItem`s for `LoopingSelectorItem`.

## Resource keys

- The phone's: `LoopingSelectorForegroundThemeBrush`, `LoopingSelectorSelectionBackgroundThemeBrush`,
  `LoopingSelectorSelectionForegroundThemeBrush` (in `Tokens/`), `PhoneFontFamilyNormal`.
- Cornflower's, in the theme file: `Wp81LoopingSelectorTheme`, `Wp81LoopingSelectorItemTheme`,
  `Wp81LoopingSelectorItemSize` (123), `Wp81LoopingSelectorItemScale` (1.125), `Wp81LoopingSelectorItemPitch`
  (138.375), and the two text lines' heights.

## The ladder

Rungs 2 and 4: a keyed theme for Avalonia's `DateTimePickerPanel` and its items, and a behaviour (`PhoneLoopingSelector`)
for the drag, the release, the tap and the active wheel. No type of Cornflower's own. Avalonia's panel is used as it is,
out of a ScrollViewer.

## Known gaps

- **`SelectionChanged` on every move.** Avalonia's panel raises it, and changes `SelectedValue`, as its offset
  moves; the phone's comes once, at rest. The look follows the phone's (no item selected while
  it moves), the event doesn't.
- **The last wheel fades out with the new one's expansion**; the phone's starts about 47 ms later.
- **Which item a flick ends on** can differ by one from the phone's for the same finger: the speed is read from the
  moves of the last 50 ms, and the phone's own reading isn't known.
- **The end of a wheel that doesn't loop**: it stops there. The phone's isn't read (an overpan, a squash): owed.
- **No still of a wheel expanded or moving** to compare by pixel: owed (COMPARISON.md).
- **The text** is Selawik's, 1.25 px higher than Segoe WP's at 45 (by design, ADR 0001).
- Owed: the device run on the Pixel 8, and the real-window run.
