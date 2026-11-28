# DropDownButton

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#235](https://github.com/wdcossey/Cornflower/issues/235) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `DropDownButton`, themed by type in `src/Cornflower.Themes.Wp81/Controls/DropDownButton.axaml`. An 8.1 app
put a `Flyout` or `MenuFlyout` on a `Button`; this is Wp81's [Button](../Button/README.md) with a chevron after its
content.

## Usage

```xml
<DropDownButton Content="sort by">
  <DropDownButton.Flyout>
    <MenuFlyout>
      <MenuItem Header="name" />
      <MenuItem Header="date" />
    </MenuFlyout>
  </DropDownButton.Flyout>
</DropDownButton>
```

## What it sets

Wp81's Button's setters, by the phone's keys: `PhoneForegroundBrush` border and text, `PhoneBorderThickness` (2.5),
`PhoneFontFamilyNormal` at `TextStyleLargeFontSize`, `PhoneButtonContentPadding`, the phone's touch sizes
(`PhoneButtonMinWidth` 109, `PhoneButtonMinHeight` 57.5, drawn inside `PhoneTouchTargetOverhang`), the tilt; no focus
adorner (the theme sets none). Pressed fills it with the accent (`ButtonPressedBackgroundThemeBrush`) and draws the content and chevron in
`ButtonPressedForegroundThemeBrush`; disabled dims the border, content and chevron as the Button's do. An open flyout
changes nothing drawn: the accent isn't for open (ADR 0007, rule 3).

The template is written out, not `BasedOn` the Button's theme, as a `StaticResource` finds only its own file's.

## Resource keys

`CornflowerPlainChevronMargin` (9.5 before the chevron), in `Tokens/Plain.axaml`.

## Glyphs

The chevron is Lucide's `ChevronDown` (`LucideIcon`, ISC), a new use of Lucide in a plain theme (ADR 0007, rule 10).

## The template's parts

`Root` (untilted, takes the hits), `Grid` (`Tilt.IsTarget`), `Border`, `PART_ContentPresenter`, `Chevron`.

## Known gaps

- In the template of a control Wp81 doesn't theme yet, a `DropDownButton` takes the theme under Wp81 (`HoleParts`), as
  a Button does. No hole holds one at Avalonia 12.1.4.
