# DatePicker and TimePicker, and their full-screen pickers

Status: done but for the device and real-window runs · [#156](https://github.com/wdcossey/CornflowerUI/issues/156) · owed: the device run on the Pixel 8 · owed: the real-window run

The Windows Phone 8.1 date and time pickers: Avalonia's `DatePicker`, `TimePicker`, `DatePickerPresenter` and
`TimePickerPresenter`, themed by `Wp81Theme` in `src/Cornflower.Themes.Wp81/Controls/DatePicker.axaml` and
`TimePicker.axaml`. Closed, a header over a button showing the value; tapped, a picker filling the window, its wheels
the LoopingSelector's (#154), the pickers' bar at its foot (#153), swinging in and out in 160 px rows on the phone's
popup host (`PhoneDateTimePicker`, internal), Back closing it. [PLAN.md](PLAN.md) has every value and its source;
[COMPARISON.md](COMPARISON.md) has the emulator's grabs beside ours.

## Usage

```xml
<DatePicker cf:ControlHeader.Header="date picker" SelectedDate="{Binding Due}" />
<TimePicker cf:ControlHeader.Header="time picker" SelectedTime="{Binding Alarm}" />
<TimePicker ClockIdentifier="24HourClock" MinuteIncrement="15" />
<DatePicker DayVisible="False" />
```

Everything is Avalonia's: `SelectedDate`, `SelectedTime`, `SelectedDateChanged`, `SelectedTimeChanged`, `MinYear`,
`MaxYear`, `DayVisible`, `MonthVisible`, `YearVisible`, `ClockIdentifier`, `MinuteIncrement`, `UseSeconds`, `Clear()`.
WinRT's `Header` and `HeaderTemplate` are `cf:ControlHeader.Header` and `HeaderTemplate`.

- **Closed:** the header (WinRT's `HeaderContentPresenterStyle`), then a button the picker's width, in the picker's
  `Foreground`, a 2.5 border, the value in the culture's short pattern ("7/15/2014"; "12:30 PM", or "12:30" on the
  24-hour clock). With no value the button is empty. Pressed, the accent inside and white text; disabled,
  `PhoneBaseLowColor`; the pointer over it changes nothing (the Button's states, #150's B19).
- **Open:** the window filled in `FlyoutBackgroundThemeBrush`, the wheels 138.333 wide and centred, 37 down, 700 tall,
  in the culture's order (month, day, year; hour, minute, AM/PM). The date's items show two lines, 07 over July; the
  time's one, at 43. Wheels hidden by `DayVisible` and the others, or the 24-hour clock, leave the rest centred.
- **Picking:** the check (or Enter) sets the value, and `SelectedDateChanged` or `SelectedTimeChanged` comes, in the
  tap's frame (none when the value is the same); the cross, Back or Escape set nothing. Either way the picker swings
  out and is closed 16 ticks (250 ms) later.
- **Motion:** in five 160 px rows, every row from 80° at once, the lower ones slower (12.5 to 5.67° a tick), at rest
  after 14.1 ticks; closing, the rows turn away two ticks apart, top first.
- The month and day formats are set by the theme for the wheels (`MM↵MMMM`, `dd↵dddd`); set your own and the wheels
  show them.

## Extensions

None. `PhoneDateTimePicker` is the theme's own (internal).

## Resource keys

The phone's, in `Tokens/`: `PhoneFontFamilyNormal`, `ContentControlFontSize`, `ControlContentThemeFontSize`,
`DatePickerForegroundThemeBrush`, `TimePickerForegroundThemeBrush`, `FlyoutBackgroundThemeBrush`, `PhoneForegroundBrush`,
`DateTimeFlyoutContentPanelPortraitThemeMargin`, `DateTimeFlyoutContentPanelLandscapeThemeMargin`. Cornflower's from
other controls: `Wp81HeaderContentPresenterTheme`, `Wp81LoopingSelectorTheme`, `Wp81CommandBarCompactHeight`,
`Wp81PickerAcceptGlyphTheme`, `Wp81PickerCancelGlyphTheme`.

## The template's parts

The phone's: `LayoutRoot`, `HeaderContentPresenter`, `FlyoutButton` (Avalonia's `PART_FlyoutButton`); the presenters'
`Background`, `ContentPanel`. Avalonia's: `PART_Popup` (a stand-in that never shows: PLAN.md has why),
`PART_PickerPresenter`, the DatePicker's three text blocks (unseen), the presenters' hosts, selectors, spacers,
`PART_PickerContainer` (kept empty) and the bar's `PART_AcceptButton` and `PART_DismissButton`. Ours: `PickerPopup` (the
popup the picker is shown in), `Columns`, `PickerBarSpace`, `PickerBar`.

## The ladder

Rung 1 for the four themes; rung 2 for the popup (`PhoneDateTimePicker`, on `PhonePopupHost`). No part class: Avalonia's
presenter is placed full screen by the host as it is (PLAN.md). `DatePickerFlyout` and `TimePickerFlyout` aren't
added (PLAN.md, "The checklists").

## Known gaps

- **Reopened after a cancel, the wheels are where they were left** when the value hasn't changed: Avalonia's picker
  gives its presenter the value again, and its presenter sets its wheels only when that changes. The phone's starts at
  the value. The value itself is right.
- **The bar turns with the bottom row**; the phone draws its bar at rest, last, by 400 ms (#149), as the list pickers'
  (#153).
- **No `Closed` event**: Avalonia's pickers have none, nor have WinRT's. The DatePickerFlyout's, a tick after its bands
  have gone (17 ticks), is for that type, not added (PLAN.md).
- **`SelectedDateChanged` isn't raised on an unchanged accept**, as Avalonia's property; the phone's `DateChanged` on an
  unchanged accept wasn't read (#150 read the flyout's `DatePicked`, which comes).
- **The selected items are one raw pixel wider** than the phone's: #154's items are drawn unrounded.
- **The text** is Selawik's (ADR 0001).
- Owed: the device run on the Pixel 8, and the real-window run.
