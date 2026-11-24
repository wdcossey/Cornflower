# EmbeddableControlRoot

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `EmbeddableControlRoot`, the root of a view embedded in another toolkit's, themed by type in
`src/Cornflower.Themes.Wp81/Controls/EmbeddableControlRoot.axaml`. **On Android it is every page's host**: Avalonia.Android's
`AvaloniaView` makes one (`new EmbeddableControlRoot(...)`, 12.1.4) and sets the app's main view as its content. As
[Window](../Window/README.md): the page's background and the phone's font.

## Usage

Nothing to set: with `Wp81Theme` in `Application.Styles`, the Android head's root wears it.

## What it sets

Each by `DynamicResource`:

| Property | Value |
|---|---|
| `Background` | `ApplicationPageBackgroundThemeBrush` (`PhoneBackgroundColor`: black dark, white light) |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` (Selawik) |
| `FontSize` | `TextStyleLargeFontSize` (18.14, `BodyTextBlockStyle`'s) |

## The template's parts

`PART_TransparencyFallback`, and `PART_Background` (the background) round `PART_VisualLayerManager` round
`PART_ContentPresenter`. No border, no motion.

## Known gaps

- Not seen on the Pixel yet: the coordinator's look checks the Android head's background behind the system bars.
