# DatePickerFlyout and TimePickerFlyout: plan

Issue [#184](https://github.com/wdcossey/Cornflower/issues/184). Rung 5: two types of Cornflower's own, with the WinRT
names, in `Cornflower.Controls` (`Controls/Pickers/DatePickerFlyout.cs`, `TimePickerFlyout.cs`), on #153's
`PickerFlyoutBase`, and their presenters, `DatePickerFlyoutPresenter` and `TimePickerFlyoutPresenter`: Avalonia's
`DatePickerPresenter` and `TimePickerPresenter`, as types of their own so a theme can tell them apart. Under Wp81 they are
#156's open pickers (`Controls/DatePicker.axaml`, `TimePicker.axaml`), put on the phone's popup host by
`PhonePickerFlyout`, as the list pickers' are. Followed up from #156 (docs/controls/DatePicker/PLAN.md, "DatePickerFlyout
and TimePickerFlyout: not added").

## Where the values come from

- **The look:** #156's. The flyout's presenter is the DatePicker's (or TimePicker's) presenter, so its theme is based on
  that one's, and draws the same pixels: held by `At_Rest_It_Is_Drawn_As_The_Pickers_Own_Picker`, both themes. #156's
  comparison set that against the phone's DatePickerFlyout and TimePickerFlyout (run `20261108-2254-dev11`).
- **The motion:** #149 (run `20261116-1835-dev11`): the date and time pickers' rows, `PhonePopupMotion.DateTimeRows`.
- **The events and `Closed`:** #150 (run `20261116-1951-dev11`, "How each popup is dismissed") and #149: `DatePicked` and
  `TimePicked` in the tap's frame, even when nothing changed (old and new 2014-07-15); nothing on Back or cancel;
  `Closed` 17 ticks (265.6 ms) after the call, in all eight calls of each, a tick after the last band has gone.
- **The members:** the checklists below, from the phone SDK's `Windows.winmd`.

## Closed at 17 ticks: a change to the shared host

The DatePicker's popup closes with its bands, at `PhonePopupMotion.ClosedAfter(5)`, 16 ticks, as nothing waits on it.
The flyouts raise `Closed`, and the phone's comes a tick later than the bands. So `PhonePopupRows` takes an optional
`ClosedLater`, and `PhonePopupHost` closes a full popup that much after its last band goes (`ClosedAfter(bands)`, as
before). `PhonePopupMotion.DateTimeFlyoutRows` is `DateTimeRows` a tick later: 17 ticks for five bands, and still after
the last band in a window resized to more. Every other popup gives none and is
unchanged; their tests weren't touched.

## The behaviours

- **Opening:** `ShowAt` or `ShowAtAsync` opens the picker filling the window, whatever the target, in five rows a fifth
  of the window each (160 px on the reference's 800), from 80°, the lower slower; at rest after 14.1 ticks. The wheels
  start at `Date` or `Time`, again each opening, even after a cancel left them turned.
- **Accept** (the bar's check, or Enter): `Date` or `Time` is set, and `DatePicked` or `TimePicked` raised with the old
  and new values, in the tap's frame, even when the value is the same; `ShowAtAsync` returns the value. It closes in five
  bands; `Closed` comes 17 ticks after the tap. A second accept while it closes picks nothing (#153's `TakeAnswer`).
- **Cancel, Back, Escape, `Hide()`:** nothing is set or raised; `ShowAtAsync` returns null, in `Closed`'s frame, 17
  ticks later.
- **The members:** `MinYear`, `MaxYear`, `DayVisible`, `MonthVisible`, `YearVisible`; `ClockIdentifier` and
  `MinuteIncrement` reach the presenter at each opening. The date's wheels show the DatePicker's two-line formats.
- **Defaults**, as WinRT's: today, and the years from 100 before it to 100 after (Avalonia's presenter's too); now; the
  culture's clock (`24HourClock` where its short time has no `h` outside quotes); a minute step of 1. A clock other than
  `12HourClock` or `24HourClock`, or a minute step outside 1 to 59, is refused as it is set.
- **A year range moved** past the last one opens; **a date outside the range** opens at the range's end, again each
  opening.
- **The first measure:** Avalonia measures a flyout's presenter unbounded to place it the first time, and the presenter's
  wheels throw on that; so the flyout measures it in its window first.

## Avalonia against the phone

| The phone | Here |
|---|---|
| `ShowAtAsync` → `IAsyncOperation<DateTimeOffset?>` (`TimeSpan?`) | `Task<DateTimeOffset?>` (`Task<TimeSpan?>`): the value, or null |
| `DatePicked`, `TimePicked`: `TypedEventHandler<flyout, args>` | `EventHandler<DatePickedEventArgs>` (`TimePickedEventArgs`), `OldDate`/`NewDate`, `OldTime`/`NewTime`, as `ItemsPicked` (#153) |
| `MinYear`, `MaxYear`: 100 years either side of now | the same |
| `CalendarIdentifier` | left out: Avalonia's presenter is Gregorian only (decided, below) |
| The title (`PickerFlyoutBase.Title`) | set on the presenter as the other pickers' are; the Wp81 presenter has no title, as #156's (empty under a picker on the phone) |

## The checklists

Generated on 2026-11-18 by `dotnet run --project tools/winrt-api -- checklist <Type> <Windows.winmd> <design folder>`
from the phone SDK's `Windows.winmd` (`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design
folder (`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or Avalonia's; `[-]` is left out, with its
reason.

### DatePickerFlyout

| ✓ | WinRT | Here |
|---|---|---|
| [x] | `Date` (`DateTimeOffset`) | the same, styled; today by default |
| [x] | `MinYear`, `MaxYear` (`DateTimeOffset`) | the same; 100 years either side of today by default |
| [x] | `DayVisible`, `MonthVisible`, `YearVisible` | the same, true by default |
| [-] | `CalendarIdentifier` | Avalonia's presenter is Gregorian only: a property that could take one value would do nothing |
| [x] | `DatePicked` (`DatePickedEventArgs`: `OldDate`, `NewDate`) | the same, an `EventHandler<DatePickedEventArgs>` |
| [x] | `ShowAtAsync(target)` → `DateTimeOffset?` | `Task<DateTimeOffset?>` |

### TimePickerFlyout

| ✓ | WinRT | Here |
|---|---|---|
| [x] | `Time` (`TimeSpan`) | the same, styled; now by default |
| [x] | `ClockIdentifier` | the same; the culture's by default |
| [x] | `MinuteIncrement` | the same, 1 by default, 1 to 59 |
| [x] | `TimePicked` (`TimePickedEventArgs`: `OldTime`, `NewTime`) | the same, an `EventHandler<TimePickedEventArgs>` |
| [x] | `ShowAtAsync(target)` → `TimeSpan?` | `Task<TimeSpan?>` |

### Inherited by both

As the list pickers' (docs/controls/ListPickerFlyout/PLAN.md, "Inherited by both"): `OnConfirmed()` and
`ShouldShowConfirmationButtons()` (true here, as WinRT's; the Wp81 presenter always shows its bar) on
`PickerFlyoutBase`; `Placement` ignored, as on the phone; `Opening`, `Opened`, `Closed`, `ShowAt`, `Hide`,
`CreatePresenter` Avalonia's; `Dispatcher`, `ReadLocalValue`, `GetAnimationBaseValue` [-], Avalonia has none.

### The presenters

`DatePickerFlyoutPresenter` and `TimePickerFlyoutPresenter` have no members of their own, as WinRT's have none. Their
styles are #156's (docs/controls/DatePicker/PLAN.md, "The presenters"); the Wp81 themes are based on Avalonia's
presenters' and add only `PhonePickerFlyout.IsEnabled` and, for the date, the wheels' formats a DatePicker would give
its presenter.

## Dependencies

#156 (the presenters' themes), #153 (`PickerFlyoutBase`, `PhonePickerFlyout`, the bar), #154 (the wheel), #151 and #181
(the host, `PhonePopupMotion`), #149 and #150 (the readings).
