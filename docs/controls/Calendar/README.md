# Calendar

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#240](https://github.com/wdcossey/Cornflower/issues/240) · a look on the Pixel is owed (no timings)

Avalonia's `Calendar`, themed by type in `src/Cornflower.Themes.Wp81/Controls/Calendar.axaml`. The phone had no calendar:
8.1 picked a date with [`DatePicker`](../DatePicker/README.md)'s looping lists, which Wp81 themes. This is a month on
the page, with no box: a plain [`CalendarItem`](../CalendarItem/README.md) of
[`CalendarDayButton`](../CalendarDayButton/README.md)s, or of [`CalendarButton`](../CalendarButton/README.md)s for the
year and the decade.

## Usage

```xml
<Calendar SelectedDate="2026-11-24" />
```

## What it sets

| Property | Value |
|---|---|
| `Foreground` | `PhoneForegroundBrush`, which the days inherit |
| `Template` | `PART_Root` holding `PART_CalendarItem`, stretched to the Calendar's width |

No `Background`, `BorderBrush` or `BorderThickness` of its own (Simple's are a box): the app's, passed to the item,
unset. `HeaderBackground` isn't passed or drawn: Simple draws its accent there at rest (rule 3). Avalonia's keyboard, selection modes and blackout dates
are the Calendar's own.

## The template's parts

- `PART_Root`: a `StackPanel`, clipped.
- `PART_CalendarItem`: the plain `CalendarItem`, the app's brushes and thickness passed to it.

## A selection never scrolls the page

A `SelectedDate` or `DisplayDate` set at load or from code doesn't move a page's `ScrollViewer` (as #220, #251 and #252
had to fix for their controls): Avalonia's `Calendar` brings nothing into view, and takes the focus only from a press or
a key. `CalendarTests.A_Selection_Doesnt_Scroll_The_Page` holds it to that.

## Known gaps

- Avalonia shows the month, the year and the decade at once: no transition between them, and none for a month's change
  (rule 8). Simple 12.1.4 has none either.
- No swipe between months: the arrows and the keyboard only.
- No focus adorner on the day the keyboard is on (Simple's `:dayfocused` rectangle), as the phone's buttons have none.
  As in Simple's, neither the days nor (by Avalonia's default) the Calendar are focusable. A Calendar an app makes
  `Focusable` takes the focus from a press, and the arrow keys move the day.
- `HeaderBackground` is ignored.
- A Calendar given Simple's theme under Wp81 still gets Wp81's plain `CalendarItem` by type, as only the Button family's
  parts fall back (`HoleParts`): Simple's root round the plain page.
