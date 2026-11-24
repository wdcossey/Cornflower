# TabItem

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#238](https://github.com/wdcossey/Cornflower/issues/238) · a look on the Pixel is owed (no timings)

Avalonia's `TabItem`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TabItem.axaml`. The phone had no TabItem:
a `TabControl` here takes the Pivot's headers without its swipe (ADR 0007, answer 8), and a `TabItem` is one of them,
in the Pivot header's look, by the Pivot header's keys (`Controls/Pivot.axaml`). Its `Content` is shown by the
`TabControl` ([TabControl](../TabControl/README.md)).

## Usage

```xml
<TabControl>
  <TabItem Header="mail"><TextBlock Text="..." /></TabItem>
  <TabItem Header="calendar"><TextBlock Text="..." /></TabItem>
</TabControl>
```

## What it sets

| Property | Value |
|---|---|
| `FontSize` | `PivotHeaderItemFontSize` (57), by `DynamicResource` |
| `FontFamily` | `PivotHeaderItemFontFamily` (Selawik Semilight), by `DynamicResource` |
| `LetterSpacing` | `PivotHeaderItemCharacterSpacing` (-25) thousandths of the size (`PhoneCharacterSpacing`) |
| `Foreground` | `PivotHeaderForegroundUnselectedBrush` (dimmed); selected, the header's text is `PivotHeaderForegroundSelectedBrush`; disabled, `PhoneDisabledBrush` |
| `Background` | `PivotHeaderBackgroundUnselectedBrush`; selected, `PivotHeaderBackgroundSelectedBrush` (both clear); an app's own wins |
| `Margin` | `CornflowerPlainTabHeaderMargin` (16 to the left) |
| `Padding` | `PivotHeaderItemPadding` (6.5 below) |
| `cf:Tilt.IsEnabled` | `True`: the tilt on a press (rule 7) |

## Resource keys

One new key, in `Tokens/Plain.axaml`: `CornflowerPlainTabHeaderMargin` (16,0,0,0), the Pivot header's 16 before it
(`PivotHeaderItemMargin`) without its -6.5 tuck up under the Pivot's title: a tab row has no title, and a header pulled
up out of the row would be clipped by it. The rest are the Pivot header's own keys, so nothing is copied and the
Adaptive pack reaches the size by `DynamicResource` (ADR 0004).

## The template's parts

- `PART_LayoutRoot`: a `Border` with the `Background`, `BorderBrush` and `BorderThickness`; the tilt is drawn on it.
- `PART_ContentPresenter`: the `Header` (and `HeaderTemplate`), centred, inside the `Padding`, with the phone's header line
  (`PhoneHeaderLine`; access keys aren't recognised, as the Pivot header's aren't).

## Known gaps

- The `Icon` (which a `TabbedPage` copies from its page) and the `IndicatorTemplate` aren't drawn: the Pivot's
  headers are text, with no pipe.
- No pointer-over look, as the phone has none; the keyboard focus shows nothing of its own.
- A disabled header (`PhoneDisabledBrush`) is close to an unselected one (`PivotHeaderForegroundUnselectedBrush`):
  the Pivot never disables a header, so there was no third shade to take.
- None of the Pivot header's motion (rule 8): the selection changes at once, and the headers don't wrap round to put the
  selected one first.
