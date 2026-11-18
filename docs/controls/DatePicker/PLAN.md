# DatePicker and TimePicker, and their full-screen pickers: plan

Issue [#156](https://github.com/wdcossey/CornflowerUI/issues/156). Rung 1: Wp81 themes for Avalonia's `DatePicker`,
`TimePicker`, `DatePickerPresenter` and `TimePickerPresenter`; rung 2 for the popup (`PhoneDateTimePicker`, internal to
the theme, on #151's `PhonePopupHost`). No type of Cornflower's own. The wheel is #154's (`Wp81LoopingSelectorTheme`),
the bar #153's (`Controls/PickerBar.axaml`), the popup and its rows #181's host.

## Where the values come from

- **The templates:** `datepicker.json`, `timepicker.json`, `datepickerflyoutpresenter.json` and
  `timepickerflyoutpresenter.json` (8.1 Update 1's runtime styles, equal to the SDK's `generic.xaml`).
- **The closed controls:** run `20261116-1934-dev11` (#150, B19), the dumps and grabs in `ref/wp81/DatePicker/` and
  `ref/wp81/TimePicker/`: the control 412.08 wide in a slot 19.17 in, 12.08 down; the header one 16 px line, 21.67 tall,
  3 over the button (`Margin 0,0,0,-3`), `#99FFFFFF` dark, `#73000000` light; FlyoutButton 18.75 down, 57.5 tall (the
  Button's touch target), its Border 9.5 in above and below, 38.33 tall, 2.5 thick; the text 9.17 in (2.5 + 6.5 of
  `Padding 6.5,0,0,3`), 18.14 SemiBold (the Button's own), "7/15/2014" and "12:30 PM": the culture's short date, and its
  short time.
- **The inner button's states** (B19): `Pressed`, the border's inside the accent (`#FF3E65FF`) and the text
  `PhoneTextOverAccentColor` (white), the border unchanged; `Disabled`, the border and the text `PhoneBaseLowColor`
  (`#73FFFFFF` dark, `#40000000` light), as the picker's `IsEnabled` false gives; `PointerOver` is `Normal`. They are
  Wp81's Button's (#27).
- **The open pickers:** run `20261108-2254-dev11`'s dumps and grabs (`ref/wp81/DatePickerFlyout/`,
  `ref/wp81/TimePickerFlyout/`): 0, 0, 450 × 800 in `FlyoutBackgroundThemeBrush`; `Background`'s bottom padding the
  bar's 62.92; `ContentPanel` 37.08 down (`DateTimeFlyoutContentPanelPortraitThemeMargin`), 700 tall; the title
  collapsed (empty); three hosts, 138.333 wide, at 17.5, 155.833 and 294.167: centred, with no gap. The date's items
  two lines (07 over July; 15 over Tuesday; the year alone), #154's template. The time's items one line, 43 SemiBold,
  the TimePickerFlyoutPresenter's own `ItemTemplate` (`StackPanel` at the bottom left, `Margin 5`; the `TextBlock`
  `Margin 0.8,-5`), its line 57.19 (1.33 em, as the dump's 64.34 over the item's 1.125 scale).
- **The motion:** #149 (run `20261116-1835-dev11`, `tools/refapp/README.md`, "The popups' bands"): 160 px rows, all from
  +80° at once, linear, rows 0 to 4 at 12.50, 9.60, 7.84, 6.60 and 5.67° a tick (`PhonePopupMotion.DateTimeRows`).
  Closing, five bands (below).
- **The events:** #150 (run `20261116-1951-dev11`, "How each popup is dismissed").

## Closing: 16 ticks, not 17

`PhonePopupMotion.ClosedAfter(5)` is 16 ticks (2 × 4 + 90 / 10.95 = 16.2, the last whole tick before), where #149 read the
DatePickerFlyout's and TimePickerFlyout's `Closed` at 17 (265.6 ms), in all eight calls of each. Read again with
`tools/motion/strips.py --popup DatePickerFlyout` (and `TimePickerFlyout`, `"Flyout full"`) on the same recording:

| | Its last band starts (on the recording; the model) | At 90° | `Closed` |
|---|---|---|---|
| Full Flyout, band 7 | 229 ms after the call (14 ticks in the model, 218.8) | 358 ms (22.2 in the model, 346.9) | 22 ticks |
| DatePickerFlyout, band 4 | 144 ms (8 ticks, 125) | 268 ms (16.2, 253.1) | 17 ticks |
| TimePickerFlyout, band 4 | 142 ms (8 ticks, 125) | 265 ms (16.2, 253.1) | 17 ticks |

The recording's clock runs about 1.7 ticks behind the model's at the end for both, so the date and time pickers' bands
move by the Flyout's rule: band k starts 2k ticks in, 10.95° a tick, gone at 90°, the last at 16.2. Only the event is
later: the flyout's `Closed` comes a tick after its last band has gone, where the Flyout's comes as it goes. Avalonia's
`DatePicker` and `TimePicker` have no `Closed` (nor have WinRT's), so the picker's popup closes with its bands, at
`ClosedAfter(5)`, 16 ticks (250 ms): held by
`DateTimePickerTests.Accept_Sets_The_Value_In_The_Taps_Frame_And_It_Closes_In_Five_Bands_At_16_Ticks`. A
`DatePickerFlyout` of Cornflower's own would raise its `Closed` a tick later (owed with it, below).

## Placing Avalonia's presenter full screen: no part class

The issue asks for a part class if Avalonia's presenter can't be placed full screen. It can: the presenter is any
popup's child to the host, which places and sizes it (`A_Tap_Opens_The_Picker_Filling_The_Window_And_Avalonias_Popup_Never_Shows`:
0, 0, 450 × 800). What can't be done is hold its close: Avalonia's picker closes `PART_Popup` with `Close()` the moment
the presenter confirms or dismisses, so the phone's closing bands would be cut. `PART_Popup` is therefore a stand-in
that never shows (its placement target is a control in no window, so it can't open and keeps `IsOpen` as asked), and
the presenter is in a popup of the theme's (`PickerPopup`), which `PhoneDateTimePicker` opens when the stand-in is
opened and dismisses the phone's way when it is closed. The value is set by Avalonia in the same call, the tap's frame.

## The behaviours

- **Opening:** a tap on the button opens the picker filling the window, in the date and time pickers' rows (five 160 px
  bands from 80°, the lower slower), at rest after 14.1 ticks; Avalonia's picker sets the presenter to the picker's
  value, or now, first.
- **Accept** (the bar's check, or Enter): the value is set, and `SelectedDateChanged` or `SelectedTimeChanged` raised,
  in the tap's frame; the picker closes in five bands, closed at 16 ticks.
- **Cancel, Back and Escape:** nothing is set; it closes as above. Back is taken first, before the page's
  `NavigationPage`, and while it closes.
- **The wheels:** #154's, in one group per presenter: a tap makes one the active wheel. Month and day go round, the
  year stops; hour and minute go round, AM/PM stops.
- **What shows:** `DayVisible`, `MonthVisible`, `YearVisible`, the 24-hour clock (no AM/PM wheel) and `UseSeconds`
  (Avalonia's second wheel, not the phone's) leave the wheels centred. `MinuteIncrement` is the minute wheel's step.
- **The order** of the date's wheels is the culture's (Avalonia's presenter sets each host's column).

## The visual states

The closed control's are its inner Button's, Wp81's Button theme (B19, above). The picker's are #154's wheel and #153's
bar.

## Avalonia against the phone

| The phone | Avalonia | Here |
|---|---|---|
| `Date` (a `DateTimeOffset`, never empty) | `SelectedDate` (nullable) | Avalonia's: empty, the button is empty |
| `Time` | `SelectedTime` (nullable) | the same |
| `DateChanged`, `TimeChanged` | `SelectedDateChanged`, `SelectedTimeChanged` | Avalonia's, raised as the value changes: not when accepted unchanged |
| `Header`, `HeaderTemplate` | none | `cf:ControlHeader.Header` and `HeaderTemplate` (#96) |
| `HorizontalContentAlignment` (Left) | none on the pickers | the button's content is at the left in the template |
| The flyout's `DayFormat`, `MonthFormat`, `YearFormat` | the same names, for the wheels and Avalonia's own button text | the theme sets the wheels' two-line formats; the button shows the culture's short date |
| `ClockIdentifier` | the same, from the culture by default | Avalonia's |
| The DatePickerFlyout's title | none | none: the phone's is empty under a DatePicker |

## The checklists

Generated on 2026-11-17 from the phone SDK's `Windows.winmd` and its design folder (the paths are Button's PLAN's).
`[x]` is done or Avalonia's; `[-]` is left out, with its reason.

### DatePicker

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `YearVisible`, `MonthVisible`, `DayVisible` | the same | Avalonia's; the wheels left stay centred |
| [x] | `YearFormat`, `MonthFormat`, `DayFormat` | the same | the theme sets the wheels' formats (`yyyy`, `MM↵MMMM`, `dd↵dddd`); an app's own replace them |
| [-] | `Orientation` | none | Windows 8.1's; the phone's DatePicker has no use of it |
| [x] | `MinYear`, `MaxYear` | the same | Avalonia's: the year wheel's range |
| [x] | `Header`, `HeaderTemplate` | none | `cf:ControlHeader.Header` and `HeaderTemplate` |
| [x] | `Date` | `SelectedDate` | Avalonia's (nullable) |
| [-] | `CalendarIdentifier` | none | Avalonia's presenter is Gregorian; other calendars aren't Avalonia's |
| [x] | `DateChanged` | `SelectedDateChanged` | Avalonia's, in the accept's frame |
| [x] | `FontFamily`, `FontSize`, `Foreground`, `HorizontalAlignment` | the same | set as the phone's style; the button's text is the Button's 18.14 |
| [x] | `HorizontalContentAlignment` (Left) | none | the template's |
| [x] | `LayoutRoot`, `HeaderContentPresenter`, `FlyoutButton` | `PART_FlyoutButton` | the phone's parts; Avalonia's text blocks kept unseen; `PART_ButtonContentGrid` and the spacers left out (Avalonia's control needs none) |
| [x] | `PhoneFontFamilyNormal`, `ContentControlFontSize`, `DatePickerForegroundThemeBrush` | | in `Tokens/` |
| [x] | `HeaderContentPresenterStyle` | | `Wp81HeaderContentPresenterTheme` (#111) |

### TimePicker

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `Time` | `SelectedTime` | Avalonia's (nullable) |
| [x] | `MinuteIncrement` | the same | Avalonia's: the minute wheel's `Increment` |
| [x] | `Header`, `HeaderTemplate` | none | `cf:ControlHeader.Header` and `HeaderTemplate` |
| [x] | `ClockIdentifier` | the same | Avalonia's: two wheels on `24HourClock`; the button's text on the picker's clock |
| [x] | `TimeChanged` | `SelectedTimeChanged` | Avalonia's, in the accept's frame |
| [x] | `FontFamily`, `FontSize`, `Foreground`, `HorizontalAlignment` | the same | as the DatePicker's |
| [x] | `HorizontalContentAlignment` (Left) | none | the template's |
| [x] | `LayoutRoot`, `HeaderContentPresenter`, `FlyoutButton` | `PART_FlyoutButton` | the phone's parts; Avalonia's hosts, text blocks and dividers left out (its control shows nothing without them) |
| [x] | `PhoneFontFamilyNormal`, `ContentControlFontSize`, `TimePickerForegroundThemeBrush`, `HeaderContentPresenterStyle` | | as the DatePicker's |

### The presenters

The phone's `DatePickerFlyoutPresenter` and `TimePickerFlyoutPresenter` (generated for their styles; neither has
members of its own): `Background`, `ContentPanel` (the portrait and landscape margins), `TitlePresenter`, three
`PickerHost`s, all [x] as above; `FontFamily`, `FontSize` (`ControlContentThemeFontSize`), `FontWeight` SemiBold and
`Background` (`FlyoutBackgroundThemeBrush`) set; the title [-], Avalonia's presenters have none and the phone's is
empty under a picker. Avalonia's required parts are all there: `PART_PickerContainer` kept empty (its star columns
aren't the phone's), the hosts and selectors (#154's wheel, no ScrollViewer), the spacers, `PART_AcceptButton` and
`PART_DismissButton` (#153's bar); the up and down buttons left out, as the phone has none.

### DatePickerFlyout and TimePickerFlyout: not added

The phone's flyouts are public types an app can show on its own (`ShowAtAsync`, `DatePicked`, `TimePicked`). Avalonia
has neither. **Decided: the controls alone are enough for #156**, which names DatePicker and TimePicker; the flyouts
would be two rung-5 types with their own members (`Date`, `MinYear`, `MaxYear`, the visibilities, `ClockIdentifier`,
`MinuteIncrement`, `DatePicked`/`TimePicked`, `ShowAtAsync`), on #153's `PickerFlyoutBase` and these presenters. What
#150 read of them is for that work: `DatePicked` and `TimePicked` come in the tap's frame even when nothing changed
(old and new 2014-07-15), with nothing on Back or cancel, and `Closed` 17 ticks after (above). Added by #184:
docs/controls/DatePickerFlyout/PLAN.md.

## Dependencies

#154 (the wheel), #153 (the bar), #151 and #181 (the host, `PhonePopupMotion`), #150 (B19, the dismissal), #96
(`ControlHeader`), #111 (`Wp81HeaderContentPresenterTheme`), #27 (the Button).
