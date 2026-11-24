# PopupRoot

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#234](https://github.com/wdcossey/Cornflower/issues/234) · a look on the Pixel is owed (no timings)

Avalonia's `PopupRoot`, the host of a popup in a window of its own, themed by type in
`src/Cornflower.Themes.Wp81/Controls/PopupRoot.axaml`. The phone had nothing of its own here: Wp81's flyouts, menus and
dialogs draw their own surface. So the host is transparent, with the phone's foreground and font.

## Usage

Nothing to set. A desktop popup that isn't in the overlay layer opens in one; Wp81's own popups and every popup on
Android open in [OverlayPopupHost](../OverlayPopupHost/README.md) instead.

## What it sets

Each brush and size by `DynamicResource`:

| Property | Value |
|---|---|
| `Background` | none (transparent), and `TransparencyLevelHint` Transparent |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily` | `PhoneFontFamilyNormal` (Selawik) |
| `FontSize` | `ControlContentThemeFontSize` (12, the phone's control content) |
| `FontWeight`, `FontStyle`, `Inline.TextDecorations` | Normal, Normal, none: a popup's text doesn't inherit them from what it opens from |

## The template's parts

A `LayoutTransformControl` (the popup's `Transform`) round `PART_TransparencyFallback` and `PART_VisualLayerManager`
round `PART_ContentPresenter`.

## Known gaps

- **The size is the control content's, 12, not the body text's 18.14.** It keeps a popup's unstyled text as it was
  under Simple (the preview's Flyout and PickerFlyout pictures are unchanged); a popup's own theme sets its text. Wp81's
  Flyout README says its content "inherits the page's text"; it inherits this host's.
- No motion of its own: a popup's motion is its theme's (Wp81's `PhonePopupHost`).
