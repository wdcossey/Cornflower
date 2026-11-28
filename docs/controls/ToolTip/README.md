# ToolTip

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#236](https://github.com/wdcossey/Cornflower/issues/236) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `ToolTip`, themed by type in `src/Cornflower.Themes.Wp81/Controls/ToolTip.axaml`. The phone had no tooltip:
touch has no hover, so it shows only with a pointer (a desktop head, or a mouse on the phone).

## Usage

```xml
<Button Content="save" ToolTip.Tip="save the note" />
```

## What it sets

| Property | Value |
|---|---|
| `Background` | `FlyoutBackgroundThemeBrush` (`PhoneChromeColor`), the flyout's surface, by `DynamicResource` |
| `Foreground` | `PhoneForegroundBrush`, by `DynamicResource` |
| `FontFamily` | `PhoneFontFamilyNormal`, by `DynamicResource` |
| `FontSize` | `TextStyleMediumFontSize` (16), the small print's size, by `DynamicResource` |
| `Padding` | `CornflowerPlainToolTipPadding` (9.5,4.8), by `DynamicResource` |

## Resource keys

`CornflowerPlainToolTipPadding`, in `Tokens/Plain.axaml`: 9.5 at the sides (the phone's button content's) and 4.8 above
and below (the control header's gap).

## The template's parts

`PART_ContentPresenter`: the surface, bound to the app's `BorderBrush`, `BorderThickness` and `CornerRadius` (unset: no
border, square, as the flyout), with the phone's 1.33 em line for the text it makes (`PhoneHeaderLine`).

## Known gaps

- No `MaxWidth` and no wrapping: a long string is one line, as under Simple.
- No motion: it opens and closes as Avalonia's popup does (ADR 0007, rule 8).
