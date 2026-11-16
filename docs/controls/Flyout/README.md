# Flyout and FlyoutPresenter

Status: draft · [#151](https://github.com/wdcossey/CornflowerUI/issues/151) · waits on [#149](https://github.com/wdcossey/CornflowerUI/issues/149) (the opening and closing curves) and [#150](https://github.com/wdcossey/CornflowerUI/issues/150) (the dismissal) · owed: the device run on the Pixel 8 · owed: the real-window run

The Windows Phone 8.1 Flyout: Avalonia's `Flyout`, its `FlyoutPresenter` themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/FlyoutPresenter.axaml`, and the phone's popup round it: drawn across the top of the
window, or filling it, swinging in and out on `CornflowerClock`, closed by Back. WinRT's `Placement="Full"` is
`FlyoutOptions.IsFullScreen`, in `Cornflower.Controls`. [PLAN.md](PLAN.md) has every value and its source;
[COMPARISON.md](COMPARISON.md) has the emulator's screenshots beside ours.

## Usage

```xml
<Button Content="options">
  <Button.Flyout>
    <Flyout>
      <TextBlock Text="flyout content" TextWrapping="Wrap" />
    </Flyout>
  </Button.Flyout>
</Button>

<Button Content="details">
  <Button.Flyout>
    <Flyout cf:FlyoutOptions.IsFullScreen="True">
      <StackPanel Margin="19,0">...</StackPanel>
    </Flyout>
  </Button.Flyout>
</Button>
```

`ShowAt`, `Hide`, `IsOpen`, `Opening`, `Opened`, `Closing` and `Closed` are Avalonia's. Under Wp81:

- **Where it sits.** At the window's top left, inside the safe area, the window's width, as tall as its content asks
  (at least `FlyoutThemeMinHeight`, 43): the phone's Top, Bottom, Left and Right alike, so Avalonia's `Placement` and
  offsets are ignored. With `FlyoutOptions.IsFullScreen` it fills the window. It follows the window's size while open.
- **In the window.** The popup is drawn in the window's overlay layer, on desktop too, never as a window of its own.
- **Opening and closing.** It swings in about its top edge. `Hide()`, `IsOpen = false`, a tap outside and Back all
  close it the phone's way: a top flyout stays a moment (100 ms) and goes; a full one swings out. `Closing` is raised
  when it is asked to close (and can cancel it, as Avalonia's can), and again as it closes; `Closed` when it has gone.
- **Back** closes the topmost open popup and is marked handled, so the page's `NavigationPage` (in Avalonia's
  `PageNavigationHost`) doesn't go back.
- **A tap below a top flyout** closes it, and doesn't reach the page.

The content inherits the page's text, as Avalonia's popups do (the phone's falls back to XAML's 11 px font). Pad it
yourself: the phone's flyout puts its content at the left edge, 25.5 down.

## What the popups share

This is Tier 3's worked example. ContentDialog (#157), ListPickerFlyout and PickerFlyout (#153), MenuFlyout (#152) and
the date and time pickers (#156) host their popups the same way, through these internals in
`src/Cornflower.Themes.Wp81/`:

- **`PhonePopupHost`**: one per Avalonia `Popup`. Make it with the popup, the element its motion moves (the popup's
  child, or the drawn part of it) and the owner's close; it sets `ShouldUseOverlayLayer` at once, so make it before the
  popup opens. Then, while the popup is open, it:
  - places it: `Placement` `Top` (the window's width, its own height) or `Full` (the window), anchored to the window's
    top left inside the safe area, whatever the popup's own placement asked;
  - dims what is under it: give `Dim` a brush (`ContentDialogDimmingThemeBrush`, say) and a rectangle of it covers the
    window just below the popup, in the same overlay layer, as the phone's full-window Rectangle popup does;
  - opens it with the swing, and closes it through `Dismiss()`: the closing motion, then the owner's close. Close
    through `Dismiss()`, not the popup: anything else that closes it ends the motion where it is;
  - takes Back: a class handler on `TopLevel.BackRequestedEvent` runs before any handler an element adds (Avalonia's
    `PageNavigationHost`'s included), dismisses the window's topmost open host and marks the request handled;
  - sets `PhonePopupHost.IsLandscape` on the moved element while the window is wider than tall, for a theme's
    landscape values.
- **`PhonePopupMotion`**: the swing (a turn about the top edge, seen from the phone's camera) and the lengths the
  recording gives. The one place for #149's curves; a popup with a motion of its own (the pickers' row swing) adds its
  own beside it.
- **`PhonePopupDismissal`**: the rules (Back, a tap outside, a tap on the dim layer). The one place for #150's answers.
- **`PhoneFlyout`**: how an Avalonia flyout is hooked to a host from its presenter's theme, holding the flyout's
  `Closing` until the motion ends. A `PopupFlyoutBase` (MenuFlyout, a picker flyout) copies it with its own presenter.

## Extensions

| Name | On | What |
|---|---|---|
| `FlyoutOptions.IsFullScreen` | `Flyout` | WinRT's `Placement="Full"`: the flyout fills the window |

## Resource keys

`FlyoutBackgroundThemeBrush`, `FlyoutThemeMinHeight`, `FlyoutContentPanelPortraitThemeMargin`,
`FlyoutContentPanelLandscapeThemeMargin`, `PhoneForegroundBrush`; all in `Tokens/`.

## The ladder

Rung 1 for the presenter's theme; rung 2 for the popup (`PhoneFlyout`, `PhonePopupHost`) and for
`FlyoutOptions.IsFullScreen`. No type is substituted.

## The checklist

[PLAN.md](PLAN.md#the-checklists), every row ticked or explained.

## Known gaps

- **The curves** (#149): the opening's and the full closing's curves are placeholders; only their hinge and lengths are
  the phone's. The full closing "sweeps out top to bottom" on the phone, which a hinge at the top doesn't give.
- **The dismissal** (#150): Back, a tap outside and the event order are the platform's documented defaults and
  Avalonia's, not read.
- **The height** of a top flyout is one raw pixel more than the phone's (189 rows, not 188): layout rounds up
  (ADR 0003).
- **The text** is the page's, not the phone's 11 px fallback.
