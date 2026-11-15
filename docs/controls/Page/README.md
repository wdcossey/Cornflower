# Page and Frame

Status: done · [#120](https://github.com/wdcossey/CornflowerUI/issues/120) · compared against the `pages` and `turnstile` probes' recordings (runs `20261110-0748-dev11`, `20261110-0810-dev11`, `20261115-2053-dev11`) and `-Native`'s bounds · owed: the device run on the Pixel 8 · owed: the real-window run

The Windows Phone 8.1 page and Frame: Avalonia's `ContentPage` and `NavigationPage`, themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/ContentPage.axaml` and `NavigationPage.axaml`, and the phone's three page
transitions as `IPageTransition`s, `TurnstileTransition`, `SlideTransition` and `ContinuumTransition`, run frame by frame
on `CornflowerClock` by `PhonePageRun`, internal to the theme. The plan, with every value and its source, is
[PLAN.md](PLAN.md); the phone's frames and readings beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

On the phone a `Frame` holds the pages and `Navigate` goes forward with the turnstile, or with the transition it is
given; `GoBack` goes back with the transition the page came with. Here the `NavigationPage` is the Frame and a
`ContentPage` the page:

```xml
<!-- 8.1 -->
<Frame x:Name="RootFrame" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<NavigationPage x:Name="Navigation">
  <ContentPage>
    <StackPanel Margin="19,12,19,0">
      <TextBlock Text="MY APP" Theme="{DynamicResource TitleTextBlockStyle}" />
      <TextBlock Text="page title" Theme="{DynamicResource HeaderTextBlockStyle}" />
    </StackPanel>
  </ContentPage>
</NavigationPage>
```

```csharp
// 8.1: Frame.Navigate(typeof(Details));  Frame.Navigate(typeof(Details), null, new SlideNavigationTransitionInfo());
await Navigation.PushAsync(new Details());                          // the turnstile, the theme's PageTransition
await Navigation.PushAsync(new Details(), new SlideTransition());   // the slide
await Navigation.PushAsync(new Details(), new ContinuumTransition()); // the continuum

// 8.1: Frame.GoBack(), which plays the reverse of the page's own transition. Avalonia's PopAsync plays PageTransition
// unless given one, so pass the one the page came with:
await Navigation.PopAsync(new SlideTransition());
```

The page has no navigation bar and no title of its own, as on the phone: the title is the page's content, in the
phone's text styles. A page that wants Avalonia's bar sets `NavigationPage.HasNavigationBar="True"` on itself (an
extension, below).

### The hardware back button is the app's

On the phone the app handles `HardwareButtons.BackPressed` and calls `Frame.GoBack()`; on the first page it leaves the
press alone, and the phone leaves the app. Here it is Avalonia's back request, `TopLevel.BackRequested` (the back button
or gesture on Android, Alt+Left on desktop). Either:

- **host the root page in a `PageNavigationHost`** (`<PageNavigationHost Page="{...}" />` as the window's content):
  Avalonia forwards the request to the page, and a `NavigationPage` pops with its `PageTransition` and marks it handled
  while it has a page to go back to; or
- **handle `TopLevel.BackRequested` yourself**, as the sample's `page` page does, to go back with the transition each
  page came with: mark the request handled while there is a page to go back to, and leave it alone on the first page.

A `NavigationPage` drops a navigation asked for while a transition runs (`IsNavigating`), so a second press during a
turn does nothing.

### Safe area, the tray and the app bar

- The page fills its host, and its background (`ApplicationPageBackgroundThemeBrush`) reaches under the system bars.
- Its content is laid out in the safe area, as the phone lays a page's content out in `VisibleBounds` under
  `UseVisible`; `AutomaticallyApplySafeAreaPadding="False"` lays it under the bars, as `UseCoreWindow` does.
- `BottomCommandBar` sits at the bottom of the safe area and the content above it, as the phone's app bar is taken off
  `VisibleBounds` (62.92 for a compact bar, 26.25 for a minimal one, on the reference: the CommandBar theme's heights).
  The bar draws over the content, so an open bar reaches up over the page without moving it. `TopCommandBar` sits at the
  top the same way; the phone has none.
- `NavigationPage.BottomCommandBar`, set on a page, is the NavigationPage's: docked below the pages and outside the
  transition, as the phone's shell draws its app bar apart from the page.

## Behaviours

- **The turnstile** on every push and pop: the page going out goes at once, and the page coming in turns about a
  vertical axis 44 px left of its left edge and 99 px behind the screen, forward from −67.1° over 425 ms, back from
  +42.0° over 430 ms, on an `ExponentialEase` with exponent 6, easing out.
- **The slide**, when asked for: forward the new page rises from 200 px below over 350 ms (the same ease); back the page
  going out drops 135.9 px, faster each frame, over 187.5 ms, then the page under it shows at once. It is also the
  `ModalTransition`.
- **The continuum**, when asked for: forward the old page fades in 62.5 ms and the new one grows from 0.943 about its
  centre over 359 ms; back the slide's back, then the page under it fades in from 0.39 over 344 ms.
- Each transition's time 0 is its first frame, which shows its first value, as the emulator's did. Its looks are set at
  animation priority on the page presenters and taken away at its end, or at once if it is cancelled (Avalonia cancels
  it when the NavigationPage leaves its window), so no page is left turned, moved or hidden.
- **The page and its content** as in the section above.
- **No navigation bar** on a ContentPage; one turned back on is in the phone's chrome (extension, below).

## Resource keys

None of its own. It uses `ApplicationPageBackgroundThemeBrush`, `PhoneForegroundBrush`, `PhoneFontFamilyNormal`, and
for the opt-in bar `PhoneChromeBrush`, `PhoneFontFamilySemiBold`, `TextStyleLargeFontSize`,
`ButtonPressedBackgroundThemeBrush` and `ButtonDisabledForegroundThemeBrush`.

## The templates' parts

`ContentPage`: `PART_ContentPresenter` (the whole page, padded by the safe area, drawing the background), and inside it
`PART_TopCommandBar`, `PART_BottomCommandBar` and `PART_Content`. `NavigationPage`: Avalonia's parts, `PART_ContentHost`
with `PART_PagePresenter` and `PART_PageBackPresenter`, `PART_NavigationBar` with `PART_BackButton`
(`PART_BackButtonDefaultIcon`, `PART_BackButtonContentPresenter`), `PART_Header` and `PART_TopCommandBar`,
`PART_BottomCommandBar`, `PART_NavBarSpacer`, `PART_ModalBackPresenter` and `PART_ModalPresenter`. No
`PART_NavBarShadow`: the phone draws none, so `HasShadow` does nothing.

## The ladder

Rung 1 (theme) for both pages. The transitions fill Avalonia's own extension point, `IPageTransition`. No type is
substituted; there is no `Frame` type: `NavigationPage` is the Frame.

## Extensions

- `TurnstileTransition`, `SlideTransition`, `ContinuumTransition`: public, in `Cornflower.Themes.Wp81`, with no
  properties.
- The navigation bar a page turns back on: `PhoneChromeBrush`, the page's `Header`, and a back arrow in
  `Wp81NavigationBackButtonTheme`. The phone has none to measure.

## Known gaps

- **The continuum's exit element** (`ContinuumNavigationTransitionInfo.ExitElement`), which flies out of the old page,
  isn't measured or done.
- **The continuum's black** between the old page and the new (at most 94 to 125 ms on the recording) is left out, taken
  as the new page being made (PLAN, "The continuum").
- **The continuum's times are the recording's clock**, good to about a frame (31 ms).
- **The bottom bar on a phone with a gesture area**: the bar sits above the bottom safe area, and the page's background
  shows below it, where the phone's bar is at the screen's edge.
- **Not read on the phone**: the transitions on another profile (the axis is scaled with the width), a navigation asked
  for mid-transition (Avalonia drops it), and anything in the light theme.
