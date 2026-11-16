# Flyout and FlyoutPresenter

Status: draft · [#151](https://github.com/wdcossey/CornflowerUI/issues/151) · waits on [#150](https://github.com/wdcossey/CornflowerUI/issues/150) (the dismissal) · owed: the device run on the Pixel 8 · owed: the real-window run

The Windows Phone 8.1 Flyout: Avalonia's `Flyout`, its `FlyoutPresenter` themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/FlyoutPresenter.axaml`, and the phone's popup round it: drawn across the top of the
window, or filling it, turning in and out in bands on `CornflowerClock`, closed by Back. WinRT's `Placement="Full"` is
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
- **Opening and closing.** As the phone's shell draws it (#149): the popup is cut into bands, each turned about its
  own centre line, seen by one camera over the window. A top flyout is one band, turned in from 80° in 134 ms. A full
  one is 100 px bands, all from 80°, the lower ones slower, at rest in 208 ms. Closing, a top flyout doesn't turn: it
  is gone 110 ms after it is asked to close, and closed at 140.6 ms. A full one's bands turn away one after another,
  two ticks apart, and it is closed at 343.8 ms. `Hide()`, `IsOpen = false`, a tap outside and Back all close it so.
  `Closing` is raised when it is asked to close (and can cancel it, as Avalonia's can), and again as it closes;
  `Closed` when it has gone.
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
  - opens and closes it in bands: on a motion's first frame it takes a picture of the popup, lays it over the popup
    cut into bands (`PhonePopupBands`), hides the popup under them, and turns each band; at rest both go. It closes
    through `Dismiss()`: the closing motion, then the owner's close. Close through `Dismiss()`, not the popup:
    anything else that closes it ends the motion where it is;
  - takes Back: a class handler on `TopLevel.BackRequestedEvent` runs before any handler an element adds (Avalonia's
    `PageNavigationHost`'s included), dismisses the window's topmost open host that isn't already closing, and marks
    the request handled; with every popup closing, Back is the page's again (PENDING #150);
  - sets `PhonePopupHost.IsLandscape` on the moved element while the window is wider than tall, for a theme's
    landscape values.
- **`PhonePopupMotion`**: the bands and their turns, as #149 read them: the band matrix (a turn about a band's centre
  line, seen from one camera over the window), the rates, the stagger and the times. The pickers' rows are bands of
  their own heights and rates (#149 read them too); a popup with a motion of its own adds its
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

- **The full flyout's lower half, opening**: drawn flat from the start; the phone draws nothing below 427 px at 119 ms,
  and how its lower bands come in isn't read. And the stills put the bands' centres about 111 px apart, not the read
  100 (COMPARISON.md).
- **The dismissal** (#150): Back, a tap outside and the event order are the platform's documented defaults and
  Avalonia's, not read.
- **The height** of a top flyout is one raw pixel more than the phone's (189 rows, not 188): layout rounds up
  (ADR 0003).
- **The text** is the page's, not the phone's 11 px fallback.
