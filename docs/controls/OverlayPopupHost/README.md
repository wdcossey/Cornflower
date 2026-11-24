# OverlayPopupHost

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `OverlayPopupHost`, the host of a popup in the window's overlay layer, themed by type in
`src/Cornflower.Themes.Wp81/Controls/OverlayPopupHost.axaml`. Every popup on Android opens in one, and so does any popup
set to `ShouldUseOverlayLayer`, as Wp81's own are. As [PopupRoot](../PopupRoot/README.md): transparent, with the
phone's foreground and font.

## Usage

Nothing to set.

## What it sets

Each brush and size by `DynamicResource`:

| Property | Value |
|---|---|
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` (Selawik) |
| `FontSize` | `ControlContentThemeFontSize` (12, the phone's control content) |
| `FontWeight`, `FontStyle`, `Inline.TextDecorations` | Normal, Normal, none: a popup's text doesn't inherit them from what it opens from |

## The template's parts

A `LayoutTransformControl` (the popup's `Transform`) round `PART_VisualLayerManager` round `PART_ContentPresenter`.

## Known gaps

- **The size is the control content's, 12, as the window's is, not the body text's 18.14.** It keeps a popup's unstyled
  text as it was under Simple (the preview's Flyout and PickerFlyout pictures are unchanged); a popup's own theme sets
  its text. Wp81's
  Flyout README says its content "inherits the page's text"; it inherits this host's.
- No motion of its own: a popup's motion is its theme's (Wp81's `PhonePopupHost`).
