# Label

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#236](https://github.com/wdcossey/Cornflower/issues/236) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `Label`, themed by type in `src/Cornflower.Themes.Wp81/Controls/Label.axaml`. The phone had no Label: a
`TextBlock` in `ControlHeaderTextBlockStyle` named a control, so a `Label` here has that look.

## Usage

```xml
<Label Content="_Name" Target="NameBox" />
<TextBox x:Name="NameBox" />
```

## What it sets

| Property | Value |
|---|---|
| `FontFamily` | `PhoneFontFamilyNormal`, by `DynamicResource` |
| `FontSize` | `TextStyleMediumFontSize` (16), by `DynamicResource` |
| `Foreground` | `PhoneMidBrush`, by `DynamicResource`; `PhoneDisabledBrush` when disabled |

## Resource keys

No new keys: the type ramp's, as `ControlHeaderTextBlockStyle` uses them (rule 2 of ADR 0007).

## The template's parts

`PART_ContentPresenter`: recognises access keys (the Label's `Target` takes focus on its key), with the phone's 1.33 em
line for the text it makes (`PhoneContentLine`). It is bound to the app's `Background`, `BorderBrush`, `BorderThickness`,
`CornerRadius`, `Padding` and content alignments, all unset, so nothing else is drawn at rest.

## Known gaps

- No padding of its own (Simple's is 3): the page spaces it, as it spaces a control header.
- The text line is the phone's 1.33 em, not `ControlHeaderTextBlockStyle`'s 17.5 baseline-to-baseline: a `Label`'s
  content is a presenter's, not a `TextBlock` in that style.
