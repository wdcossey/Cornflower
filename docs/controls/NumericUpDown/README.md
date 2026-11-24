# NumericUpDown

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#235](https://github.com/wdcossey/Cornflower/issues/235) · a look on the Pixel is owed (no timings)

Avalonia's `NumericUpDown`, themed by type in `src/Cornflower.Themes.Wp81/Controls/NumericUpDown.axaml`. An 8.1 app used
a `TextBox` with the number keyboard (`InputScope` Number): here it is Wp81's [TextBox](../TextBox/README.md) inside a
[ButtonSpinner](../ButtonSpinner/README.md).

## Usage

```xml
<NumericUpDown Value="3" Minimum="0" Maximum="10" PlaceholderText="copies" />
```

## What it sets

- `Margin`: `TextControlMarginThemeThickness` (9.5 above and below), so it sits on a page as a TextBox does; the TextBox
  inside has none.
- `PART_Spinner`, Wp81's ButtonSpinner, with its own box cleared (no border, background or padding), so its spin buttons
  sit beside the TextBox's box, as tall as it. Its `AllowSpin`, `ShowButtonSpinner` and `ButtonSpinnerLocation` are the
  control's.
- `PART_TextBox`, Wp81's TextBox by type: the phone's box, focus, placeholder and disabled looks. Its `PlaceholderText`,
  `IsReadOnly` and `TextAlignment` are the control's; one line, no returns; `TextInputOptions.ContentType` Number, for
  the number keyboard.

Avalonia's NumericUpDown keeps the text and the value in step, and spins on the arrow keys and the wheel.

## Glyphs

The spin buttons' Lucide `Minus` and `Plus`, the ButtonSpinner's (ADR 0007, rule 10).

## The template's parts

`PART_Spinner`, `PART_TextBox`.

## Known gaps

- No header: `ControlHeader.Header` isn't shown (the TextBox inside has none).
