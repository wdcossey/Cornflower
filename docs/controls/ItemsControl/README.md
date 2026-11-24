# ItemsControl

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `ItemsControl`, themed by type in `src/Cornflower.Themes.Wp81/Controls/ItemsControl.axaml`. An 8.1 app used
`ItemsControl` itself, which has no default style on the phone, so this is a bare `ItemsPresenter`.

## Usage

```xml
<ItemsControl ItemsSource="{Binding Lines}" />
```

Its items keep their own themes; `ItemsPanel` lays them out (a vertical `StackPanel` unless set). Wp81's keyed
`ItemsControl` themes (`Wp81CommandBarItemsTheme`) are unchanged, and `ListBox` and the other lists keep theirs.

## What it sets

Nothing but the template: the `Padding` is the presenter's margin, and `Background`, `BorderBrush` and
`BorderThickness` draw nothing, as Wp81's `ContentControl` draws none. A transparent background takes no taps.

## The template's parts

`PART_ItemsPresenter`, the template's only part.

## Known gaps

- An app that wants a background or border round its items puts the `ItemsControl` in a `Border`.
