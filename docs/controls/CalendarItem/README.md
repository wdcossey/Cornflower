# CalendarItem

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#240](https://github.com/wdcossey/Cornflower/issues/240) · a look on the Pixel is owed (no timings)

Avalonia's `CalendarItem`, the page inside a [`Calendar`](../Calendar/README.md), themed by type in
`src/Cornflower.Themes.Wp81/Controls/CalendarItem.axaml`. The header row (two arrows and the month), then the month's
days or the year's months, sharing the width.

## What it sets

| Property | Value |
|---|---|
| `Template` | no box: the header row over `PART_MonthView` and `PART_YearView` (below) |
| `DayTitleTemplate` | a `TextBlock` in `ControlContextualInfoTextBlockStyle` (16, `PhoneMidBrush`): the small print's look, as a column header (rule 2) |

No header band: Simple draws `HeaderBackground` (its accent) behind the header at rest; this draws nothing (rule 3).

## Resource keys

- `Wp81PlainGlyphButtonTheme`, in `Controls/PlainGlyphButton.axaml` (a part theme from #235, by `DynamicResource`):
  the three header buttons. Nothing drawn at rest, the accent while pressed, `PhoneDisabledBrush` disabled.
- `PhoneButtonMinHeight` (57.5): the header row's least height, and the arrows' width, so each is a fingertip's square.
- `PhoneFontFamilySemiLight`, `GroupHeaderTextBlockStyleFontSize` (23.73) and `PhoneMidBrush`: the month's heading, in
  the group heading's type (rule 2).

## The template's parts

- `PART_Border`: bound to the app's `Background`, `BorderBrush` and `BorderThickness`, unset.
- `PART_PreviousButton`, `PART_NextButton`: Lucide's `ChevronLeft` and `ChevronRight` (rule 10), with the tilt.
- `PART_HeaderButton`: the month and year, or the year, or the decade, with the tilt. A tap goes up a level.
- `PART_MonthView`: seven star columns. The days' names, then six rows of `CalendarDayButton`s. At 373 wide (a 411 screen
  less a page's 19 margins) a day is 53.3 by 57.5.
- `PART_YearView`: four star columns, three rows of `CalendarButton`s, 93.25 by 57.5 at 373.

Disabled (`:disabled`), `PART_MonthView`'s brush, and so the days' names, is `PhoneDisabledBrush`, as the days and the
buttons are. Avalonia adds the names in code, so no style in the theme reaches them: they bind to the view's brush.

## Known gaps

- The heading has the group heading's family, size and brush, not its -3 character spacing: it is a `Button`'s content,
  whose presenter Wp81PlainGlyphButtonTheme owns.
- In the decade view the heading is `PhoneDisabledBrush`: Avalonia disables `PART_HeaderButton` there, as there is no
  level above, and the glyph button dims its content disabled.
- Lucide in a plain theme is a new use of a third-party set (ADR 0007, rule 10).
