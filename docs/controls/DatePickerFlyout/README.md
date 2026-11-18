# DatePickerFlyout and TimePickerFlyout

Status: done · [#184](https://github.com/wdcossey/Cornflower/issues/184) · device run passed 2026-11-18 (Pixel 8, Release, over wireless adb, `run.ps1 -Page datetimepicker` on `main` at `70eea78`, 09:32: cold 881 ms, the page opened in 451 ms; both flyouts open full screen with the wheels and the bar, Back closes them with the values unchanged, no exceptions; the time flyout follows en-GB's 24-hour clock) · real-window run passed 2026-11-18 (`tools/real-window/drive.ps1`, Debug, on `main` at `7bb64ce`: 195 of 195, the datetimepicker page opened in 40 ms, layout 11.4)

The Windows Phone 8.1 date and time pickers shown on their own, from any element, with no DatePicker or TimePicker on
the page: Cornflower's `DatePickerFlyout` and `TimePickerFlyout` (on #153's `PickerFlyoutBase`) and their presenters,
`DatePickerFlyoutPresenter` and `TimePickerFlyoutPresenter` (Avalonia's `DatePickerPresenter` and
`TimePickerPresenter`), in `src/Cornflower.Controls/Controls/Pickers/`. Under `Wp81Theme` they are the DatePicker's and
TimePicker's own open pickers (#156, `Controls/DatePicker.axaml`, `TimePicker.axaml`), filling the window and turning in
and out in the date and time pickers' rows, Back closing them. [PLAN.md](PLAN.md) has every value and its source;
[COMPARISON.md](COMPARISON.md) has the emulator's grabs beside ours.

## Usage

```csharp
var date = await new DatePickerFlyout { Date = due }.ShowAtAsync(button);   // null if cancelled
var time = await new TimePickerFlyout { MinuteIncrement = 15 }.ShowAtAsync(button);

var flyout = new DatePickerFlyout { YearVisible = false };
flyout.DatePicked += (_, e) => Show(e.OldDate, e.NewDate);
flyout.ShowAt(button);
```

- **Open:** the window filled, as the DatePicker's picker: the wheels at `Date` or `Time`, every time it opens; the
  pickers' bar at the foot. `DayVisible`, `MonthVisible` and `YearVisible`, `MinYear` and `MaxYear`, `ClockIdentifier`
  and `MinuteIncrement` are read as it opens.
- **Picking:** the check (or Enter) sets `Date` or `Time` and raises `DatePicked` or `TimePicked`, in the tap's frame,
  even when the value is the same; `ShowAtAsync` returns it. The cross, Back, Escape or `Hide()` set and raise nothing,
  and `ShowAtAsync` returns null.
- **Motion:** in five rows, each a fifth of the window, from 80°, the lower slower; closing, the rows turn away two ticks
  apart and `Closed` comes 17 ticks (265.6 ms) after the tap, a tick after the last row has gone, as the phone's.
- **Defaults:** today, 100 years either side of it, now, the culture's clock, a minute step of 1.

## Extensions

None.

## Resource keys

The DatePicker's and TimePicker's (docs/controls/DatePicker/README.md, "Resource keys").

## The template's parts

The DatePicker's and TimePicker's presenters' (docs/controls/DatePicker/README.md, "The template's parts").

## The ladder

Rung 5: two types of Cornflower's own, with the WinRT names. Rung 2 for the popup: `PhonePickerFlyout`, on
`PhonePopupHost`, whose `PhonePopupRows` now takes a `ClosedLater` of its own (PLAN.md).

## Known gaps

- **No `CalendarIdentifier`:** Avalonia's presenter is Gregorian only.
- **Members set while it is open** are read at the next opening, not at once.
- The DatePicker's gaps hold here too: the bar turns with the bottom row; a window of another height is cut in five rows,
  not read on the phone; AM and PM last (docs/controls/DatePicker/README.md, "Known gaps").
- The device run and the real-window run passed on 2026-11-18 (the status line).
