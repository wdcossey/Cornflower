# Window

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `Window`, themed by type in `src/Cornflower.Themes.Wp81/Controls/Window.axaml`. The phone had no window: an
8.1 app's frame was its page's background and the phone's font, so a window here is that. Every desktop head shows one.

## Usage

Nothing to set: with `Wp81Theme` in `Application.Styles`, every `Window` wears it. A page's own background and font,
as the sample's pages set, still win.

## What it sets

Each by `DynamicResource`, so the Adaptive pack can change it (ADR 0004):

| Property | Value |
|---|---|
| `Background` | `ApplicationPageBackgroundThemeBrush` (`PhoneBackgroundColor`: black dark, white light) |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` (Selawik) |
| `FontSize` | `ControlContentThemeFontSize` (12, the phone's control content): a page sets the body's 18.14, as the sample's do |
| `TransparencyBackgroundFallback` | `ApplicationPageBackgroundThemeBrush` |

## The template's parts

`PART_TransparencyFallback`, `PART_Background` (the background, behind everything), a transparent panel over the title
bar's area (`WindowDecorationMargin`), and `PART_VisualLayerManager` round `PART_ContentPresenter`: the adorner and
overlay layers, which popups and the keyboard pan use. No border, no motion.

## Known gaps

- What it changes for an app: a window with no background of its own is the page's colour, not Simple's grey, and text
  with no font of its own is Selawik. The size stays 12, as Simple's, because it reaches every control that sets none,
  and the controls Wp81 doesn't theme yet are laid out for 12. The sample's `MainWindow` already set the page's
  background.
