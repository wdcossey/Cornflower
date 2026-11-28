# CalendarButton

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#240](https://github.com/wdcossey/Cornflower/issues/240) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `CalendarButton`, a month in a [`Calendar`](../Calendar/README.md)'s year or a year in its decade, themed
by type in `src/Cornflower.Themes.Wp81/Controls/CalendarButton.axaml`. The day's look
([`CalendarDayButton`](../CalendarDayButton/README.md)) for a month's or a year's name.

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent` |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal`, `TextStyleLargeFontSize` (18.14) |
| `MinHeight` | `PhoneButtonMinHeight` (57.5). Its width is the year's quarter |
| `HorizontalAlignment`, `VerticalAlignment` | `Stretch` |
| `Tilt.IsEnabled` | `True` (rule 7) |
| `Focusable` | `False`, as in Simple's: a Calendar an app makes `Focusable` takes the focus, and its arrow keys move the month |
| `FocusAdorner` | none |

## The states

| State | Look |
|---|---|
| At rest | the name in `PhoneForegroundBrush`, nothing behind it |
| `:pressed`, `:selected` (the month or year shown) | `PhoneAccentBrush` behind it, the name `PhoneButtonBasePressedForegroundBrush` |
| `:inactive` (a year outside the decade) | `PhoneMidBrush` |
| `:disabled` | `PhoneDisabledBrush`, and no fill when selected |

No pointer-over look and no `:btnfocused` rectangle.

## The template's parts

- `Border`: the fill.
- `PART_ContentPresenter`: the name, centred.
