# ButtonSpinner

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#235](https://github.com/wdcossey/Cornflower/issues/235) · a look on the Pixel is owed (no timings)

Avalonia's `ButtonSpinner`, themed by type in `src/Cornflower.Themes.Wp81/Controls/ButtonSpinner.axaml`. The phone had
no spinner: here it is a box with its content and two spin buttons in it.

## Usage

```xml
<ButtonSpinner Spin="OnSpin" Content="3 copies" />
<ButtonSpinner ButtonSpinnerLocation="Left" Content="left" />
```

## What it sets

| Property | Value |
|---|---|
| `BorderThickness`, `BorderBrush` | `PhoneBorderThickness` (2.5), `PhoneForegroundBrush`: a box a finger presses (rule 4) |
| `Background` | Transparent |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal`, `ContentControlFontSize`, as Wp81's TextBox |
| `Padding` | `TextControlThemePadding` |
| `MinHeight` | `TextControlThemeMinHeight` (34), the phone's text box |
| `VerticalContentAlignment` | Center |

`PART_DecreaseButton` (minus) and `PART_IncreaseButton` (plus) sit side by side after the content (`:right`,
Avalonia's default) or before it (`:left`), and go with `ShowButtonSpinner`. Avalonia enables each from `AllowSpin`
and `ValidSpinDirection`. Disabled draws the box in `PhoneDisabledBrush`. No tilt: a box, not a button (rule 7).

## The part theme

`Wp81PlainSpinnerButtonTheme` (`Controls/PlainSpinnerButton.axaml`), keyed, for a `RepeatButton`: nothing drawn at rest,
the host's foreground, the accent behind its glyph while pressed, `PhoneDisabledBrush` when disabled. A part inside a
larger control (rule 6): it fills its slot's height, at least `CornflowerPlainSpinnerButtonMinWidth` (40) wide, and isn't
focusable (the keyboard spins the control). It repeats as the phone's RepeatButton: `ClickMode` Press,
`CornflowerPlainSpinnerButtonDelay` (500 ms) and `CornflowerPlainSpinnerButtonInterval` (33 ms), the phone's RepeatButton's
values; `PhonePress` stops the repeats while the finger is off it. Later batches reuse it (plan section 3).

## Resource keys

In `Tokens/Plain.axaml`: `CornflowerPlainSpinnerButtonMinWidth`, `CornflowerPlainSpinnerButtonDelay`,
`CornflowerPlainSpinnerButtonInterval`.

## Glyphs

Lucide's `Minus` and `Plus` (`LucideIcon`, ISC), a new use of Lucide in a plain theme (ADR 0007, rule 10).

## The template's parts

`Border`, `PART_ContentPresenter`, `PART_DecreaseButton`, `PART_IncreaseButton`.
