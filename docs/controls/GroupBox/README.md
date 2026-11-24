# GroupBox

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#236](https://github.com/wdcossey/Cornflower/issues/236) · a look on the Pixel is owed (no timings)

Avalonia's `GroupBox`, themed by type in `src/Cornflower.Themes.Wp81/Controls/GroupBox.axaml`. On the phone a group was
a pattern, not a control: a heading in `GroupHeaderTextBlockStyle` over the controls, with no box. That is what this
draws.

## Usage

```xml
<GroupBox Header="notifications">
  <StackPanel>
    <CheckBox Content="sounds" />
    <CheckBox Content="vibrate" />
  </StackPanel>
</GroupBox>
```

## What it sets

| Property | Value |
|---|---|
| `Template` | the heading over the content, no box (below) |

## Resource keys

- `Wp81PlainGroupHeaderTheme`, in `Controls/PlainGroupHeader.axaml` (a part theme, by `DynamicResource`):
  `GroupHeaderTextBlockStyle`'s look for a heading a template shows as content. `PhoneFontFamilySemiLight` at
  `GroupHeaderTextBlockStyleFontSize` (23.73), `PhoneMidBrush`, its character spacing
  (`GroupHeaderTextBlockStyleCharacterSpacing`, -3) and the phone's 1.33 em line. Later batches' group headings
  (`Expander`'s header, a tree's or table's group) are to wear it too.
- `CornflowerPlainGroupHeaderMargin` (0,0,0,9.5), in `Tokens/Plain.axaml`: the room under the heading.
- Disabled, the heading is `PhoneDisabledBrush`.

## The template's parts

- `PART_Border`: bound to the app's `Background`, `BorderBrush`, `BorderThickness` and `CornerRadius`, all unset, so no box.
- `PART_HeaderPresenter`: the `Header` and `HeaderTemplate` in `Wp81PlainGroupHeaderTheme`, collapsed with no `Header`.
  The name Avalonia looks for, so a header that is a control is the control's logical child.
- `PART_ContentPresenter`: inside the `Padding` (as Simple's has it, so the heading stays on the page's edge), placed by
  the content alignments, with the phone's 1.33 em line for the text it makes.

## Known gaps

- The heading's line is the phone's 1.33 em (31.6 at 23.73), not `GroupHeaderTextBlockStyle`'s 23 baseline-to-baseline:
  it is a presenter's text, not a `TextBlock` in that style.
- The heading's font and brush are the part theme's: an app's `Foreground` on the control colours the content only.
