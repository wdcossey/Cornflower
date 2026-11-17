# ContentDialog

Status: done but for the device and real-window runs · [#157](https://github.com/wdcossey/CornflowerUI/issues/157) · owed: the device run on the Pixel 8 · owed: the real-window run

The Windows Phone 8.1 ContentDialog: Cornflower's own `ContentDialog`, in `Cornflower.Controls`, with WinRT's API, themed
by `Wp81Theme` in `src/Cornflower.Themes.Wp81/Controls/ContentDialog.axaml`: drawn from the top of the window over a dim
layer, turning in on `CornflowerClock`, closed by its buttons, `Hide()` or Back. [PLAN.md](PLAN.md) has every value and
its source; [COMPARISON.md](COMPARISON.md) has the emulator's screenshots beside ours.

## Usage

```csharp
var dialog = new ContentDialog
{
    Title = "delete",
    Content = "Delete this photo?",
    PrimaryButtonText = "delete",
    SecondaryButtonText = "cancel",
};
if (await dialog.ShowAsync() == ContentDialogResult.Primary)
    ...
```

`ShowAsync()` shows it over the app's window (the desktop's active or main window, or the single view's);
`ShowAsync(TopLevel)` over a given one. Under Wp81:

- **Where it sits.** At the window's top, its width, as tall as it asks: 199.17 with buttons, 153.33 without. The
  title and a string body are each one line, cut at the edge, as on the phone, and show an underscore as written (no
  access keys). With `FullSizeDesired`, or when it is
  taller than the window, it fills the window: the title stays one line, the body is clipped above a bar at the foot,
  and the buttons go in that bar, as the phone's shell draws them. There is no ScrollViewer: put one in the content to
  scroll.
- **The buttons.** A button is there while its text is set. Two share the width; a lone one, primary or secondary,
  takes the left half. A click raises `PrimaryButtonClick` or `SecondaryButtonClick`; unless its `Cancel` is set, the
  button's command runs and the dialog closes with that result. `IsPrimaryButtonEnabled`, `IsSecondaryButtonEnabled`
  and a command that can't run disable a button.
- **Under it.** A dim layer, `ContentDialogDimmingThemeBrush` (`#99000000`, light `#99FFFFFF`), covers the window. A tap
  on it does nothing and reaches nothing.
- **Opening and closing.** It turns in as one band about its centre line, from 45° on an ease to rest at 141 ms
  (#149). It is drawn in the window's overlay layer, through Flyout's popup host. Closing, `Closing` is raised (it can
  keep the dialog open, at once or with a deferral), then `Closed`, then `ShowAsync`'s task completes, all at once;
  the dialog stays drawn as it was and goes in one frame 107.5 ms later, its dim layer with it.
- **Back** hides it (`Closing` with `None`, which may keep it open) when it is its window's topmost popup, and is
  marked handled, so the page's `NavigationPage` doesn't go back. A Flyout open over the dialog takes Back first. With
  the keyboard up, the first Back is the keyboard's, as on the phone: on Android the input method takes it.
- **Focus.** It takes the keyboard focus as it opens, Tab stays in it, and the focus goes back where it was once it
  has gone from the screen: the page under it can't be reached, as on the phone. While it is still drawn after
  closing, it takes no taps.
- **Theme.** The dialog takes its window's theme variant, as the phone's takes the app's.
- **Where to declare it.** In code, or in a page's resources; a dialog that is a page's child can't be shown, as a
  control has one parent.

## Extensions

| Name | On | What |
|---|---|---|
| `ShowAsync(TopLevel)` | `ContentDialog` | shows the dialog over a given window; WinRT's `ShowAsync()` takes none |

## Resource keys

`ContentDialogBackgroundThemeBrush`, `ContentDialogDimmingThemeBrush`, `ContentDialogTitleMinHeight`,
`ContentDialogContentMinHeight`, `ContentDialogButtonsMinHeight`, `ContentDialogTitleMargin`,
`ContentDialogContentMargin`, `ContentDialogButton1HostPadding`, `ContentDialogButton2HostPadding`,
`ContentDialogContentLandscapeWidth`, `TextStyleExtraLargeFontSize`, `TextStyleLargeFontSize`,
`PhoneFontFamilyNormal`, `PhoneForegroundBrush`; all in `Tokens/`. The shell's bar, read from the screen, is the
theme file's own: `Wp81ContentDialogBarHeight`, `Wp81ContentDialogBarPadding`, `Wp81ContentDialogBarSpacing`.

## The ladder

Rung 5 for the control: Avalonia has none. Rung 1 for its theme; rung 2 for its popup (`PhoneContentDialog` on
`PhonePopupHost`).

## The sample

The sample's "contentdialog" page shows each variant (two buttons, one, none, full size, long, a TextBox) and what each
returned, the MessageDialog title and content styles (B18), and a switch that makes the window light while the page is
shown.

## The checklist

[PLAN.md](PLAN.md#the-checklist), every row ticked or explained.

## Known gaps

- **The bar's text** is the dialog's Button's, where the phone's shell draws its bar's about 6% larger.
- **Not read**: the full-size dialog's opening (taken as the default's one band), the bar in landscape, and where a
  button's command runs against its click (ours after the click's event).
- **The opening's last frames**: the host's band rests at the Flyout's 134.4 ms; between that and 140.6 ms the dialog
  is drawn at rest where the curve has under 0.8° to go.
- **The rows** round up to the raw pixel grid, the phone's to the nearest (ADR 0003): the dialog one raw row taller,
  the bar's outlines one shorter.
