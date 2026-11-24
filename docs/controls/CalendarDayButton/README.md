# CalendarDayButton

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#240](https://github.com/wdcossey/Cornflower/issues/240) · a look on the Pixel is owed (no timings)

Avalonia's `CalendarDayButton`, a day in a [`Calendar`](../Calendar/README.md)'s month, themed by type in
`src/Cornflower.Themes.Wp81/Controls/CalendarDayButton.axaml`. The day's number in the body's type, on a cell that takes
the finger across it, with the tilt.

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent` |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal`, `TextStyleLargeFontSize` (18.14): the body's (rule 2) |
| `MinHeight` | `PhoneButtonMinHeight` (57.5): a day is pressed on its own (rule 6). Its width is the month's seventh |
| `HorizontalAlignment`, `VerticalAlignment` | `Stretch`, so the whole cell takes the finger |
| `Tilt.IsEnabled` | `True` (rule 7) |
| `FocusAdorner` | none |

## The states

| State | Look |
|---|---|
| At rest | the number in `PhoneForegroundBrush`, nothing behind it |
| `:pressed` | `PhoneAccentBrush` behind it, the number `PhoneButtonBasePressedForegroundBrush`, and the tilt |
| `:selected` | the same fill and number (rule 3: what is selected) |
| `:today` | a square `PhoneBorderThickness` (2.5) outline in `PhoneForegroundBrush` round the cell: no accent at rest (rule 3). Kept when today is selected |
| `:inactive` (another month's day) | the number `PhoneMidBrush` |
| `:blackout`, `:disabled` | the number `PhoneDisabledBrush` |

No pointer-over look and no `:dayfocused` rectangle, as the phone's buttons have neither.

## The template's parts

- `Border`: the fill.
- `TodayMark`: the outline, shown `:today`.
- `PART_ContentPresenter`: the number, centred.

## Known gaps

- A blacked-out day is only dimmed: Simple's cross is left out.
