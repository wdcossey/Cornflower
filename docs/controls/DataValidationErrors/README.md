# DataValidationErrors

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#241](https://github.com/wdcossey/Cornflower/issues/241) · a look on the Pixel is owed (no timings)

Avalonia's `DataValidationErrors`, themed by type in `src/Cornflower.Themes.Wp81/Controls/DataValidationErrors.axaml`.
The phone had no validation display. Here each error is a line of small print under the field, in red.

## Usage

Wp81's [`TextBox`](../TextBox/README.md), `PasswordBox` and [`ComboBox`](../ComboBox/README.md) hold none in
their templates (the phone's had none), so an app wraps a field to show its errors:

```xml
<DataValidationErrors>
  <TextBox cf:ControlHeader.Header="name" Text="{Binding Name}" />
</DataValidationErrors>
```

A binding's validation error (an exception, a `DataValidationException`, `INotifyDataErrorInfo`) or
`DataValidationErrors.SetErrors` on the field shows; `ClearErrors` hides it.

## What it sets

| Property | Value |
|---|---|
| `Background` | transparent |
| `ErrorTemplate` | each error a `TextBlock` in `ControlContextualInfoTextBlockStyle` (16, rule 2: small print), `Foreground` `CornflowerPlainErrorBrush`, wrapped; an exception (one a binding catches that isn't a `DataValidationException`) shows its `Message`, not its `ToString()` with the whole stack, which Simple and Fluent show (#262) |
| `Template` | the content, then the error lines under it (below) |
| `HoleParts.FallBack` | true: in a template written for the theme under Wp81 (Simple's TextBox, which the managed file dialog still holds), it takes that theme's, so no line is drawn inside a box laid out for Simple's |

With no errors the lines are collapsed and it is only the field. No icon, no box and no colour on the field: the
accent isn't used (rule 3), and nothing moves (rule 8). The line has no margin of its own: a TextBox's own margin
(`TextControlMarginThemeThickness`, 9.5 below) is between the box and the line.

## Resource keys

- `CornflowerPlainErrorBrush` (new, in `Tokens/Plain.axaml`): `CornflowerAccentRedColor` (`#FFE51400`, the phone's
  Red accent), the same in dark and light (ADR 0007).
- `ControlContextualInfoTextBlockStyle`: the line's type.

## The template's parts

- `PART_ContentPresenter`: the content (the field), at its own size.
- `PART_OwnerErrors`: the `Owner`'s errors. In a control's template Avalonia sets `Owner` to the templated parent.
- `PART_ContentErrors`: with no `Owner`, the content's errors: the wrapped field's. Not both, as a template often
  passes its errors on to the field it wraps, which would show each twice. Each line's `DataContext` is the control
  whose errors it shows, as Simple's is; each shows only while that control has errors.

## Known gaps

- **No error on Wp81's own fields without a wrapper**: the TextBox's, PasswordBox's and ComboBox's templates have no
  `DataValidationErrors`, as decided for the TextBox (its PLAN: "left out"). Proposed in #241's validation PR.
- **The content must be the field itself**, a `Control`: a view model shown through a `ContentTemplate` shows
  nothing, as the errors are on the field the template builds. Put the `DataValidationErrors` inside the template,
  round the field, instead.
- `:error` is set on the field (Avalonia's), and nothing styles it: the field's box stays the phone's.
