# ItemsControl

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `ItemsControl`, themed by type in `src/Cornflower.Themes.Wp81/Controls/ItemsControl.axaml`. An 8.1 app used
`ItemsControl` itself, which has no default style on the phone, so this is a bare `ItemsPresenter` in a `Border` that
draws nothing unless the app sets it.

## Usage

```xml
<ItemsControl ItemsSource="{Binding Lines}" />
```

Its items keep their own themes; `ItemsPanel` lays them out (a vertical `StackPanel` unless set). Wp81's keyed
`ItemsControl` themes (`Wp81CommandBarItemsTheme`) are unchanged, and `ListBox` and the other lists keep theirs.

## What it sets

Nothing but the template. Its `Border` is bound to the control's `Background`, `BorderBrush`, `BorderThickness`,
`CornerRadius` and `Padding`, all unset, so nothing is drawn at rest (rule 5, transparent at rest). An app's own
background or border is drawn, and `Background="Transparent"` takes taps between the items, as with Avalonia's own.

## The template's parts

`PART_Border` round `PART_ItemsPresenter`.

## Known gaps

None known.
