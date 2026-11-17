# Flyout and FlyoutPresenter

Status: done but for the device and real-window runs · [#151](https://github.com/wdcossey/CornflowerUI/issues/151) · owed: the device run on the Pixel 8 · owed: the real-window run

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
  one is 100 px bands, all from 80°, the lower ones slower; below 400 px nothing turns and it is drawn at rest
  downwards, all of it at 269 ms. Closing, a top flyout doesn't turn: it
  is gone 110 ms after it is asked to close, and closed at 140.6 ms. A full one's bands turn away one after another,
  two ticks apart, and it is closed at 343.8 ms. `Hide()`, `IsOpen = false`, a tap outside and Back all close it so.
  `Closing` is raised when it is asked to close (and can cancel it, as Avalonia's can), and again as it closes;
  `Closed` when it has gone.
- **Back** closes the topmost open popup and is marked handled, so the page's `NavigationPage` (in Avalonia's
  `PageNavigationHost`) doesn't go back.
- **A tap below a top flyout** closes it, and doesn't reach the page; a tap on the flyout itself doesn't close it.
- **A dim layer** (`PhonePopupHost.Dim`, for the ContentDialog) takes a tap and does nothing with it.
- **Events:** `Opening` and `Opened` as it opens, `Closed` when it has gone. 8.1's flyout has no `Closing`; Avalonia's
  is raised when the flyout is asked to close and again as its motion ends, and an app may cancel at either.

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
    anything else that closes it ends the motion where it is. A full popup's bands are its `Rows` (the Flyout's
    100 px by default; the list picker's `PhonePopupMotion.ListPickerRows`); a top popup's band turns by its `Swing`
    (the Flyout's by default; the ContentDialog's `PhonePopupMotion.DialogSwing`, which also sets when it goes and is
    closed). `Moves` false is for a popup with a motion of its own (the MenuFlyout's): it opens at rest and
    `Dismiss()` closes it at once;
  - takes Back: a class handler on `TopLevel.BackRequestedEvent` runs before any handler an element adds (Avalonia's
    `PageNavigationHost`'s included). It first leaves Back to a focused element that takes it
    (`PhonePopupHost.TakeBackFirst`: an AutoSuggestBox's, `PhonePopupDismissal.BackTakesFocusFirst`); else it
    dismisses the window's topmost open host, or gives Back to that host's owner if it set `Back` (the ContentDialog,
    which asks its app first), and marks the request handled; while it closes, Back is taken and does nothing, as the
    phone raises no Back of the app's while a popup is open (#150). It is the one Back handler of the phone's popups;
  - gives its `Window` and `Area` (the window's client area inside the safe area, `PhonePopupHost.AreaOf` for any
    window) while open, and raises `Resized` after sizing the popup to the window again, for an owner that places
    the popup itself (the MenuFlyout, against its target);
  - sets `PhonePopupHost.IsLandscape` on the moved element while the window is wider than tall, for a theme's
    landscape values.
- **`PhonePopupMotion`**: the bands and their turns, as #149 read them: the band matrix (a turn about a band's centre
  line, seen from one camera over the window), the rates, the stagger and the times; the rows (`PhonePopupRows`) and
  swings (`PhonePopupSwing`) of the popups whose bands aren't the Flyout's. The date and time pickers' 160 px rows go
  beside the list picker's.
- **`PhonePopupDismissal`**: the rules (Back, a tap outside, a tap on the dim layer). The one place for #150's answers.
- **`PhoneFlyout`**: how an Avalonia flyout is hooked to a host from its presenter's theme. Its binder,
  `PhoneHeldFlyout`, holds the flyout's `Closing` until the motion ends; `PhonePopupBinders` makes one for each
  presenter its theme turns on and lets it go from the dispatcher, the new one after the old has given the popup
  back. A `PopupFlyoutBase` uses both with its own presenter and flyout types (`PhonePickerFlyout`; `PhoneMenuFlyout`
  with a binder of its own, as nothing is held).

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

- **`Closed` after Back** comes when it does after `Hide()` (140.6 and 343.8 ms), one to two ticks later than #150
  read it after Back (125 to 139 and 312 ms); after a tap outside, 140.6 ms where the phone's is about 45 ms.
- **The dismissal** (#150): Back, a tap outside and the event order are the platform's documented defaults and
  Avalonia's, not read.
- **The height** of a top flyout is one raw pixel more than the phone's (189 rows, not 188): layout rounds up
  (ADR 0003).
- **The text** is the page's, not the phone's 11 px fallback.
