# SplitButton

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#235](https://github.com/wdcossey/Cornflower/issues/235) · a look on the Pixel is owed (no timings)

Avalonia's `SplitButton` and `ToggleSplitButton` (themed as its base), themed by type in
`src/Cornflower.Themes.Wp81/Controls/SplitButton.axaml`. The phone had none (it came with Windows 10): here it is Wp81's
Button look with two pressable parts in one border.

## Usage

```xml
<SplitButton Content="reply" Click="OnReply">
  <SplitButton.Flyout>
    <MenuFlyout>
      <MenuItem Header="reply all" />
      <MenuItem Header="forward" />
    </MenuFlyout>
  </SplitButton.Flyout>
</SplitButton>
<ToggleSplitButton Content="bold" IsChecked="True" />
```

## What it sets

The Button's setters, by the phone's keys (border, text, font, padding, touch sizes, inside `PhoneTouchTargetOverhang`),
and the tilt on the whole control, whichever part is pressed (ADR 0007, rule 7).

| State | Look |
|---|---|
| Rest | Nothing drawn but the 2.5 border and a 1.5 divider in its brush |
| A part pressed | That part filled with `PhoneAccentBrush`, its content in `PhoneButtonBasePressedForegroundBrush` |
| Checked (`ToggleSplitButton`) | Both parts filled with the accent, their content in `PhoneButtonBasePressedForegroundBrush`; the border and divider stay |
| Disabled | Border, divider and content in `PhoneDisabledBrush` (checked, the accent stays behind) |
| Flyout open | Nothing more (rule 3) |

## Resource keys

In `Tokens/Plain.axaml`: `CornflowerPlainLineWidth` (1.5, the divider), and `CornflowerPlainGlyphButtonPadding` (9.5
each side of the chevron).

## The part theme

`Wp81PlainGlyphButtonTheme` (`Controls/PlainGlyphButton.axaml`), keyed, for a `Button`: the borderless button inside
a larger control's border. Nothing drawn at rest, the host's foreground, the accent behind its content while pressed,
`PhoneDisabledBrush` content when disabled; no size, tilt or focus adorner of its own (its host decides). Both parts wear
it; later batches reuse it (plan section 3).

## Glyphs

The chevron is Lucide's `ChevronDown` (`LucideIcon`, ISC), a new use of Lucide in a plain theme (ADR 0007, rule 10).

## The template's parts

`Root`, `Grid` (`Tilt.IsTarget`), `Border`, `PART_PrimaryButton` (the content), `Divider`, `PART_SecondaryButton`
(the chevron).
