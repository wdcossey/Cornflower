# Separator

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `Separator`, themed by type in `src/Cornflower.Themes.Wp81/Controls/Separator.axaml`. The phone's separators
were the MenuFlyout's and the AppBar's; outside a menu, a `Separator` here is a line across its slot.

## Usage

```xml
<Separator Margin="0,9.5" />
```

In a `MenuFlyout`, the phone's keyed `Wp81MenuFlyoutSeparatorTheme` (nothing drawn, 0 tall) still applies.

## What it sets

| Property | Value |
|---|---|
| `BorderThickness` | `CornflowerPlainLineThickness` (1.5 on all sides, new: the plain themes' line), by `DynamicResource` |
| `Background` | `PhoneLowBrush`, by `DynamicResource`: quiet at rest, no accent |
| `Focusable` | false |

The line is the `Background`, as Simple's and Fluent's are, as tall as the top of the `BorderThickness`, across the
slot. It has no margin of its
own: the page spaces it.

## Resource keys

`CornflowerPlainLineThickness`, in `Tokens/Plain.axaml`: the AppBar's and MenuFlyout's 1.5 edge, on all sides, for a
line that divides or edges a surface. Later plain themes use it too.

## The template's parts

`PART_Line`, a `Border` filled with the `Background`.

## Known gaps

- **Across only**: no vertical line (an `AppBarSeparator`'s). In a horizontal panel it is 0 wide and draws nothing
  unless given a `Width`.
- A `Separator` in a desktop `Menu` or `ContextMenu`, which Wp81 doesn't theme yet (Simple's `MenuItem` template
  holds one), takes this line, not Simple's indented one, until the menus' batch.
