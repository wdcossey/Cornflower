# HeaderedContentControl

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#236](https://github.com/wdcossey/Cornflower/issues/236) · a look on the Pixel is owed (no timings)

Avalonia's `HeaderedContentControl`, themed by type in
`src/Cornflower.Themes.Wp81/Controls/HeaderedContentControl.axaml`. The phone's nearest is a control header (the
TextBox's, the Slider's): this reuses its look, the shared `Wp81HeaderContentPresenterTheme`.

## Usage

```xml
<HeaderedContentControl Header="ringtone">
  <TextBlock Text="Nokia Tune" />
</HeaderedContentControl>
```

## What it sets

| Property | Value |
|---|---|
| `Template` | the header over the content (below) |

## Resource keys

No new keys. The header's look is `Wp81HeaderContentPresenterTheme`'s (`PhoneFontFamilyNormal`, `TextStyleMediumFontSize`,
`PhoneMidBrush`, the phone's line), and its gap below is `TextControlHeaderMarginThemeThickness` (4.8), the TextBox
header's. Disabled, the header is `PhoneDisabledBrush`.

## The template's parts

- `PART_Border`: bound to the app's `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius` and `Padding`, all unset.
- `PART_HeaderPresenter`: the `Header` and `HeaderTemplate`, collapsed with no `Header`. The name Avalonia looks for, so a
  header that is a control is the control's logical child.
- `PART_ContentPresenter`: placed by the content alignments, with the phone's 1.33 em line for the text it makes
  (`PhoneHeaderLine`); it doesn't recognise access keys, as Wp81's ContentControl doesn't.

## Known gaps

- The header's font and brush are the part theme's, not the control's own: an app's `Foreground` on the control colours
  the content only. A `HeaderTemplate` sets its own.
- `Expander` and `TabItem` are `HeaderedContentControl`s with themes of their own: this one reaches neither.
- The header doesn't recognise access keys (`_Advanced` shows its underscore), as the phone's control header doesn't;
  Simple's does. A `HeaderTemplate` with an `AccessText` gives one.
