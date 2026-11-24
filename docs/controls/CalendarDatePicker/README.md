# CalendarDatePicker

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#240](https://github.com/wdcossey/Cornflower/issues/240) · a look on the Pixel is owed (no timings)

Avalonia's `CalendarDatePicker`, themed by type in `src/Cornflower.Themes.Wp81/Controls/CalendarDatePicker.axaml`.
8.1 picked a date with [`DatePicker`](../DatePicker/README.md), which Wp81 themes as the phone's. This is Wp81's
[`TextBox`](../TextBox/README.md) with a calendar button in its box, which opens a plain
[`Calendar`](../Calendar/README.md) below it.

## Usage

```xml
<CalendarDatePicker PlaceholderText="pick a date" />
```

## What it sets

| Property | Value |
|---|---|
| `Margin` | `TextControlMarginThemeThickness`: it sits on a page as a TextBox does, and the TextBox inside has none |
| `Template` | the TextBox with the button in it, and the popup (below) |

## Resource keys

- `Wp81PlainGlyphButtonTheme` (`Controls/PlainGlyphButton.axaml`, from #235): the calendar button.
- `FlyoutBackgroundThemeBrush`: the popup's surface (rule 5: a floating surface).

## The template's parts

- `PART_TextBox`: Wp81's TextBox by type: the phone's box, focus, placeholder and disabled looks. The picker's
  `PlaceholderText` and errors are passed to it; no brushes, so its box is the phone's (Simple's passes its own).
- `PART_Button`: in the box after the text (the TextBox's `InnerRightContent`), Lucide's `Calendar` (rule 10), with the
  tilt. Not a tab stop.
- `PART_Popup`: below the picker, light-dismissed, in the window's overlay layer (the phone has one window), opening
  and closing at once (rule 8).
- `PART_PopupSurface`: the flyout's surface, no border, square, at least as wide as the picker, so on the phone the days
  share a field's width.
- `PART_Calendar`: the plain Calendar, bound to the picker's dates.

## Known gaps

- The text is Avalonia's: a typed date is parsed in the culture's format.
- Back doesn't shut the popup: it isn't one of Wp81's popup hosts, which take Back for the flyouts. A tap outside does.
- `UseFloatingPlaceholder`, `HorizontalContentAlignment` and `VerticalContentAlignment` do nothing: Wp81's TextBox has
  no floating placeholder and places its own text.
- Lucide in a plain theme is a new use of a third-party set (ADR 0007, rule 10).
