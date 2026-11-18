# Page and Frame: plan

The Windows Phone 8.1 page and its navigation host, the Frame, on Avalonia 12's own pages: `ContentPage` for the
phone's `Page` and `NavigationPage` for its `Frame`, themed by `Wp81Theme`, and the phone's three page transitions,
the turnstile, the slide and the continuum, as our own `IPageTransition`s, frame by frame on `CornflowerClock`
([#120](https://github.com/wdcossey/Cornflower/issues/120)). Rung 1 for the pages; the transitions are Avalonia's own
extension point, `IPageTransition`, filled in (rung 1: Avalonia asks for one).

## Where the values come from

| Source | What it gives |
|---|---|
| `flash-update1/xbf-json/page.json` | The phone's `Page` style: one setter, `Transitions`, a `NavigationThemeTransition`. No template: a `Page` is a `UserControl` |
| `flash-update1/xbf-json/frame.json` | The phone's `Frame` style: `HorizontalContentAlignment` and `VerticalContentAlignment` `Stretch`, `IsTabStop` `False`, `Background` `SystemColorWindowBrush`, `ContentTransitions` a `NavigationThemeTransition`; its template a `Border` (background, border) round a `ContentPresenter` that fills it, placed by the alignments, in the `Padding` |
| `tools/refapp/README.md`, "Visible bounds" (`-Native`, `20261111-1759-dev11`) | The root `Frame` and the `Page` fill the window, tray shown or not. Under `UseVisible` (the default) the page's content is laid out in `VisibleBounds`, 0, 27.92, 450 × 772.08 with the tray shown; under `UseCoreWindow` it fills the window. The page and its background reach under the tray |
| `tools/refapp/README.md`, "The page's app bar" (`20261115-2050-dev11`, #114) | `VisibleBounds` ends at 737.08 with a compact app bar (a 62.92 band, the bar's control 57.5 tall in the bottom of it) and at 773.75 with a minimal one (26.25); opening the bar changes nothing the API reports: it is drawn over the page |
| `tools/refapp/README.md`, "Page transitions" and "The turnstile's forward start" (`20261110-0748-dev11`, `20261110-0810-dev11`, `20261115-2053-dev11`; `tools/motion/turnstile.py`, `slide.py`) | The three transitions, below |
| The continuum's fits, below | Read for this plan from `20261110-0810-dev11`'s recording with `tools/motion`'s `motionlib` (a scratch script; the numbers are quoted here) |
| Avalonia 12.1.4's source (`NavigationPage.cs`, `ContentPage.cs`, `Page.cs`, `PageNavigationHost.cs`, `PageSlide.cs`, Simple's `NavigationPage.xaml` and `ContentPage.xaml`), tag `12.1.4` | What the pages do, their parts, and the `IPageTransition` contract |

## The phone's page and Frame against Avalonia's

| The phone | Avalonia 12.1.4 | Here |
|---|---|---|
| `Frame`: a stack of pages, `Navigate`, `GoBack`, a transition per navigation (`NavigationTransitionInfo`) | `NavigationPage`: a stack of pages, `PushAsync`, `PopAsync`, `PageTransition` and a transition per call | `NavigationPage` is the Frame. No `Frame` type of our own: Avalonia has none, and its `NavigationPage` does the Frame's work |
| `Page`, a `UserControl`: content only, `TopAppBar`, `BottomAppBar` | `ContentPage`: `Content`, `TopCommandBar`, `BottomCommandBar`, `Header`, `SafeAreaPadding` | `ContentPage` is the Page |
| The root `Frame` in the `Window` | `PageNavigationHost`: hosts a root page, gives it the safe area and forwards the window's back request to it | The app's, as on the phone. Documented |
| The hardware back button: `HardwareButtons.BackPressed`, which the app handles (the project template calls `Frame.GoBack`) | `TopLevel.BackRequested`; a `PageNavigationHost` forwards it to its page, and a `NavigationPage` pops on it | The app's. Documented, and the sample handles it |
| No navigation bar: the title is the page's content | A navigation bar with the page's `Header`, a back button and a top command bar, on by default | Off: the ContentPage theme sets `NavigationPage.HasNavigationBar` false. A page can turn it back on |
| `VisibleBounds`: the window less the tray and the app bar; content laid out in it under `UseVisible` | `SafeAreaPadding` on the page; `ContentPage.AutomaticallyApplySafeAreaPadding` pads the content with it | The safe area stands for the tray: `AutomaticallyApplySafeAreaPadding` true is `UseVisible`, false is `UseCoreWindow`. The bottom bar is laid out above the content as the app bar is taken off `VisibleBounds` |

`NavigationPage` drops a navigation asked for while another runs (`IsNavigating`, its docs); the phone's isn't
measured. Avalonia's own.

## Layout

- **The page fills its host**, and its background (`ApplicationPageBackgroundThemeBrush`, the brush every 8.1 page sets)
  reaches under the tray, as the phone's page does. Avalonia's ContentPage puts the safe area on its
  `PART_ContentPresenter`'s `Padding`, so in this template that presenter is the page's whole area and draws the
  background, and everything else sits inside it.
- **The content is laid out inside the safe area**, so below the tray: 27.92 down on the reference with the tray shown.
  With `AutomaticallyApplySafeAreaPadding` off it fills the page, as under `UseCoreWindow`.
- **The bottom command bar** sits over the content at the foot of what the safe area leaves, and the content gives up
  the bar's closed height (`PhonePageBars`): the content ends where `VisibleBounds` does. How tall the closed bar is
  (62.92 compact, 26.25 minimal) is the CommandBar theme's (#118). Open, the bar grows up over the content (285.42 from
  514.58 on the phone) and the content keeps the closed height free, as `VisibleBounds` doesn't change on opening. A bar
  that isn't a `CommandBar` is always closed; one open before it was laid out closed in its
  `CommandBarOptions.ClosedDisplayMode` takes the CommandBar theme's `Wp81CommandBarCompactHeight` or
  `Wp81CommandBarMinimalHeight` (#118). The top command bar the same way at the head: the phone has no top
  app bar to measure.
- **A ContentPage in a NavigationPage has no navigation bar**, so the NavigationPage gives it the whole safe area
  (Avalonia gives the top of it to the bar when the bar shows).

## The transitions

All three were measured on the reference profile (device 11) with a Frame whose transition is a
`NavigationThemeTransition`, between a white and a blue sheet inset 19, 60, in dark. The emulator animates on a 64 Hz
clock: 15.625 ms steps. Each is built as a frame loop on `CornflowerClock`, its time 0 the first frame drawn, as the
emulator's first frame of each transition showed its first value.

### The turnstile: the default, and `CommonNavigationTransitionInfo`

- **Only the page coming in moves; the page going out goes at once.**
- **It turns about a vertical axis 44 px left of the page's left edge and 99 px behind the screen** (fitted: every
  frame of nine forward turns within 0.24 px), seen from the camera that fits the phone's `PlaneProjection`: 1,000 px
  in front of the page's centre (`turnstile.py` solves with 1,000 and 225, 400; the camera probe reads 999, which moves
  the angles by less than the fit's residual). Forward it comes in with its right side away (a negative angle); back
  with its right side nearer.
- **Forward: an `ExponentialEase` (exponent 6, ease-out) from −67.10° over 425 ms**, on tick 0 (rms 0.072° over the 19
  distinct angles). The first frame shown was −67.0° in 4 turns, −53.8° in 25 and −43.1° in 1: ticks 0, 1 and 2 of one
  ladder; no step comes before −67.0° (#114, B10).
- **Back: the same ease from +41.97° over 430 ms** (rms 0.070°). The first frame shown, +42.0°.
- Within 1° of rest after about 300 ms.
- *Here:* a perspective matrix on the page presenter, set at animation priority (`TurnstileTransition.ToMatrix`): the
  point (x, y) turned about the axis is drawn at eye + (P − eye) × d / (d − z). The axis's 44 px is taken as a fraction
  of the width (44 / 450), as `PlaneProjection`'s `CenterOfRotationX` is; its depth and the camera's distance in view px.

### The slide: `SlideNavigationTransitionInfo`

- **Forward, the page going out goes at once, and the new page rises from 200 px below: an `ExponentialEase`
  (exponent 6, ease-out) from 200 px over 350 ms**, starting on the first frame shown (rms 0.047 px over 16 distinct
  offsets: 200.0, 152.8, then 89.2 on tick 3 and on).
- **Back, the page going out slides down faster each frame, then the page under it shows at once.** Its offsets read
  backwards from the last seen: 137.3, 94.2, 64.6, 44.2, 30.2, 20.7, 14.1, 9.4, 6.4, 4.2, 2.7. The best fit
  (`slide.py`, run 2026-11-15): an exponential reaching 135.9 px, ending 12 ticks (187.5 ms) after its first move,
  exponent 4.28 (rms 0.78 px): read forwards, an `ExponentialEase` easing in. *Here:* 135.9 × ease-in(4.28) over
  187.5 ms, and the page under it shown on the next frame, 15.625 ms later.
- The 200 px is taken as view px, not a fraction of the height.

### The continuum: `ContinuumNavigationTransitionInfo`

From the recording (frames about 31 ms apart, by the VM's clock), so each time is good to about a frame.

- **Forward: the page going out fades** in one or two recorded frames (its sheet's grey level 255, then 185 or 116,
  then 78 or 51, then black); **the screen stays black** for at most 94 to 125 ms; **then the new page grows about the
  screen's centre** from 0.941 to 0.963 at its first frame to 1 in 281 to 312 ms. Fitted for this plan to the new
  page's width on every frame of the four forward runs (1 − scale, in thousandths: 59.5, 43.4, 37.4, … 1.9): an
  exponential ease-out from 0.9427 ending 23 ticks (359 ms) after its start, exponent 4.61 (rms 1.24 thousandths).
  *Here:* the old page's opacity falls linearly to 0 over 62.5 ms (the two recorded frames), then the new page grows
  from 0.9427 over 359 ms on that ease. **The black is left out**: the turnstile and the slide show the same gap before
  their new page (187 ms from the old page's last frame to the new one's first, in this run), and their published
  curves start on the new page's first frame; so the black is taken as the new page being made, not the transition.
- **Back: the page going out slides down as in the slide's back, then the page under it fades in**: what is left of
  its opacity falls by about 0.84 a step. Fitted (`slide.py`): from 255 − 154.7 (opacity 0.393), an exponential ease-out
  ending 22 ticks (344 ms) after, exponent 3.85 (rms 2.0 grey levels). *Here:* the slide's back, then the fade from
  0.3933 over 344 ms.
- **Not measured: the element the continuum flies out of the old page** (`ContinuumNavigationTransitionInfo.ExitElement`
  and `IsEntranceElement`). The probe's pages had none. A gap.

### The contract

Avalonia's `IPageTransition.Start(from, to, forward, token)`: `NavigationPage` passes its two page presenters, the page
coming in already visible on top (forward) or under the page going out (back); a modal push passes no `from`, a modal
pop no `to`. As Avalonia's `PageSlide` does, the transition makes `to` visible at the start and hides `from` at the end.
Our looks are set at animation priority and taken away at the end, or at once when the token is cancelled
(`NavigationPage` cancels when it leaves the visual tree), so a presenter keeps nothing of a transition.

## Behaviours

Each with its evidence and its test (`tests/Cornflower.Tests/Controls/PageTests.cs`, `PageTransitionTests.cs`;
`PageReadings` holds the readings).

| Behaviour | Evidence | Test |
|---|---|---|
| ContentPage and NavigationPage are templated by the theme | rule: a missing theme fails silently | `A_ContentPage_And_A_NavigationPage_Are_Templated_By_The_Theme` |
| The page fills the window in `ApplicationPageBackgroundThemeBrush` and `PhoneForegroundBrush`, dark and light | "Visible bounds"; 8.1 pages set the brush | `The_Page_Fills_The_Window_In_The_Phones_Page_Background_And_Foreground` |
| The content below the tray, the background under it | "Visible bounds", `UseVisible` | `The_Content_Is_Laid_Out_Below_The_Tray_And_The_Page_Background_Reaches_Under_It` |
| No safe area applied: the content fills the window | "Visible bounds", `UseCoreWindow` | `With_The_Safe_Area_Not_Applied_The_Content_Fills_The_Window_As_Under_UseCoreWindow` |
| A bottom bar takes its height off the content, at 737.08 and 773.75, tray shown or not | "The page's app bar" | `A_Bottom_Bar_Takes_Its_Height_Off_The_Content_…` (both heights) |
| The bars draw over the content | "The page's app bar": the open bar is drawn over the page | `A_Bar_Drawn_Past_Its_Place_Draws_Over_The_Content` |
| An open bar grows over the content, which keeps the closed height free | "The page's app bar": `VisibleBounds` stays as closed | `An_Open_Bar_Grows_Up_Over_The_Content_Which_Keeps_Its_Closed_Height_Free`; with #118's CommandBar in a NavigationPage, compact and minimal, closed and open: `With_The_Themes_CommandBar_In_A_NavigationPage_…`, `A_CommandBar_Open_Before_It_Was_Ever_Laid_Out_Closed_…` |
| The top bar below the tray | chosen: no phone reading | `A_Top_Bar_Sits_Below_The_Tray` |
| No navigation bar; the page gets the whole safe area | the phone has none | `In_A_NavigationPage_A_ContentPage_Has_No_Navigation_Bar_And_Gets_The_Whole_Safe_Area` |
| A page that turns the bar on gets the header, a back arrow in its own theme, `PhoneChromeBrush` | an extension, no phone reading | `A_Page_That_Turns_The_Bar_Back_On_…` |
| Hosted in a `PageNavigationHost`, the platform's back request pops | Avalonia's; documented as the app's job | `A_NavigationPage_In_A_PageNavigationHost_Goes_Back_On_The_Platforms_Back_Request` |
| The turnstile by default, the slide for modal pages | `page.json`, `frame.json` | `The_Theme_Turns_Every_Navigation_And_Slides_A_Modal_Page` |
| The turnstile forward, every frame to 425 ms; the page going out hidden | "Page transitions" | `The_Turnstile_Forward_…` |
| The turnstile's first ticks, −67.0, −53.8, −43.1 and +42.0; within 1° by 300 ms | "The turnstile's forward start" | `The_Turnstiles_First_Ticks_…` |
| The turnstile back, every frame to 430 ms | "Page transitions" | `The_Turnstile_Back_…` |
| The axis a fraction of the width, the camera over the centre | chosen (`CenterOfRotationX` is relative) | `The_Turnstiles_Axis_Is_A_Fraction_Of_The_Pages_Width_…` |
| The slide forward, its offsets on ticks 0 to 10 | "Page transitions" | `The_Slide_Forward_…` |
| The slide back, its offsets read backwards, the page under it shown after | "Page transitions", `slide.py` | `The_Slide_Back_…` |
| The continuum forward: fade, then growth from the recording's first scale to rest in 312 ms | "Page transitions", the fit above | `The_Continuum_Forward_…` |
| The continuum back: the slide, then the fade from 0.40 | "Page transitions", `slide.py` | `The_Continuum_Back_…` |
| A navigation asked for mid-turn is dropped, the turn ends at rest | Avalonia's | `A_Navigation_In_The_Middle_Of_A_Turn_Is_Ignored_…` |
| Leaving the window mid-turn leaves no page turned or hidden, and no frame asked for | the contract | `A_NavigationPage_Leaving_Its_Window_Mid_Turn_…` |
| A modal page rises from 200 px and drops away | chosen: the slide | `A_Modal_Page_Rises_From_200_px_And_Drops_Away_Again` |
| No page, or a cancelled token: nothing done | the contract | `A_Transition_With_No_Page_Either_Side_Or_Already_Cancelled_Does_Nothing` |
| Simple alone keeps its own pages | the theme only applies under Wp81 | `Simple_Alone_Keeps_Its_Own_Pages` |

## Extensions

- **`TurnstileTransition`, `SlideTransition`, `ContinuumTransition`**, public, in `Cornflower.Themes.Wp81`: the
  `IPageTransition`s an app passes to `PushAsync` and `PopAsync`, as it passed a `NavigationTransitionInfo` to
  `Frame.Navigate`. Named for the motion, as the phone's docs name it; they don't shadow a WinRT or Avalonia name
  (WinRT's are `*NavigationTransitionInfo`). No properties: every value is the phone's.
- **The navigation bar a page turns back on**: the phone has none to measure. `PhoneChromeBrush` behind the page's
  `Header` (`PhoneFontFamilySemiBold`, `TextStyleLargeFontSize`) and a back arrow (Lucide's `ArrowLeft`) in a theme of
  its own, `Wp81NavigationBackButtonTheme`: transparent, the Button's pressed accent behind it, dimmed disabled, no tilt.

## Decided on the user's behalf

| Decision | Why | To reverse |
|---|---|---|
| `NavigationPage` is the Frame; no `Frame` type | Avalonia 12.1.4 has no `Frame` (the roadmap's "Frame exists" is WinRT's); its `NavigationPage` is the stack and the transitions | Add a `Frame` control in `Cornflower.Controls` over a `NavigationPage` |
| No navigation bar by default (`NavigationPage.HasNavigationBar` false in the ContentPage theme) | The phone has none; the page then gets the whole safe area, as the phone's content gets `VisibleBounds` | Drop the setter; the bar then shows the `Header` |
| `Page.Header` is shown only in that opt-in bar, not drawn in the page | The 8.1 page title is the page's own content (an app's `TextBlock`s), not a property of `Page` | Draw the header in the ContentPage template |
| The safe area stands for the tray; `AutomaticallyApplySafeAreaPadding` is `UseVisible` | The same rule: the page fills the window, its content doesn't go under the tray | — |
| The bars inside the safe area, over the content, which gives up each bar's closed height | `VisibleBounds` takes the closed app bar off the bottom, open or closed; the bar's own height is its theme's (#118 asked for the bar over the content) | Put the bars outside `PART_ContentPresenter`, docked, as Simple's template does |
| `ApplicationPageBackgroundThemeBrush` for the page and the NavigationPage | Every 8.1 page sets it; the Frame's `SystemColorWindowBrush` isn't in the theme's resources, and the recordings show black round the turning page, dark | Add `SystemColorWindowBrush` to `Tokens/` and set it on the NavigationPage |
| The modal transition is the slide | The phone has no modal page; the slide is its page rising from below | Set another `ModalTransition` |
| The turnstile's axis 44 / 450 of the width; its depth and the camera in view px | `PlaneProjection`'s `CenterOfRotationX` is relative, `CenterOfRotationZ` absolute | Change `AxisX` in `TurnstileTransition` |
| The continuum's black left out | The same gap shows before the turnstile's and the slide's new pages, whose curves start on their first frame | Start `ScaleAt` later by up to 94 ms |
| The continuum's fade out linear over 62.5 ms | Two recorded frames; its curve isn't measurable at 31 ms a frame | — |
| The default going back plays the transition the page came with (`TurnstileTransition` back gives way to the page's own, recorded on it as it came) | The phone's `Frame.GoBack` plays the reverse of the page's own transition; NavigationPage's `PopAsync()`, back button and Alt+Left all use `PageTransition` | `GoesBackAsThePageCame` false on the turnstile |
| The camera for the turnstile 1,000 px | The published angles were solved with it | 999, as the tilt's |
| Wider than 450, the turnstile's axis depth and camera distance grow with the width | Fixed, a page about 1,400 px wide brings its near edge to the eye at +42°; the phone was never that wide | `ReferenceWidth` in `TurnstileTransition` |
| No edge swipe (`IsGestureEnabled` off) | The phone's Frame has none; back is the hardware button, the app's | Drop the setter |
| A page a transition hides takes no input; a cancelled transition hides the page going out | Avalonia's hit test ignores opacity, and a pop's page going out is on top; NavigationPage doesn't hide it after a cancel | — |

## Dependencies and captures

- ScrollViewer (#116), merged; the turnstile's forward start (#114), merged.
- CommandBar (#118), in parallel: the page places the bar, the bar's theme sizes it (the PR's "For the CommandBar
  agent").
- Captures: no state to force, so no catalogue captures. The comparison uses five frames of `20261110-0810-dev11`'s
  recording, copied to `ref/wp81/Page/`. A light-theme recording of the transitions doesn't exist; nothing in them is
  a colour but the page's own.
- Not read: the continuum's exit element; the transitions on another profile (the axis's scaling is chosen, not
  measured); a navigation asked for mid-transition on the phone.

## Checklists

Generated on 2026-11-15 from the phone SDK's `Windows.winmd` (`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`)
and its design folder (`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is
left out, with its reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist Page <Windows.winmd> <design folder>`, and `Frame`,
> from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and `themeresources.xaml`, read as a specification
> (ADR 0002), and Avalonia 12.1.4. The Avalonia column is mechanical: checked by hand and filled in.

### Page

WinRT `Windows.UI.Xaml.Controls.Page`; Avalonia `Avalonia.Controls.Page`, and `ContentPage` for its content.

#### Properties (4)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `TopAppBar` | `AppBar` | `ContentPage.TopCommandBar` | Placed: docked at the top, below the safe area |
| [-] | `NavigationCacheMode` (not a dependency property) | `NavigationCacheMode` | none | Not applicable: Avalonia's stack keeps its page instances; an app keeps what it caches |
| [x] | `BottomAppBar` | `AppBar` | `ContentPage.BottomCommandBar`, `NavigationPage.BottomCommandBar` | Placed: docked at the bottom, the content above it |
| [x] | `Frame` (read-only) | `Frame` | `Page.Navigation` (`INavigation`) | Avalonia's stands |

#### Methods (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void OnNavigatedFrom(NavigationEventArgs e)` (protected, overridable) | `Page.OnNavigatedFrom()` | Avalonia's stands |
| [x] | `void OnNavigatedTo(NavigationEventArgs e)` (protected, overridable) | `Page.OnNavigatedTo()` | Avalonia's stands |
| [x] | `void OnNavigatingFrom(NavigatingCancelEventArgs e)` (protected, overridable) | `Page.OnNavigatingFrom()` | Avalonia's stands |

#### Properties, from `UserControl` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Content` | `UIElement` | `ContentPage.Content` | Laid out in the safe area, above the bottom bar |

#### Default style: setters (1)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Transitions` | `TransitionCollection` (`NavigationThemeTransition`) | `NavigationPage.PageTransition` | The turnstile, on the NavigationPage: Avalonia's page transitions are the host's |

Template parts, visual states, transitions, resources: none.

### Frame

WinRT `Windows.UI.Xaml.Controls.Frame`; Avalonia none by that name: `NavigationPage`.

#### Properties (8)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `SourcePageType` | `Type` | none | Not applicable: Avalonia navigates to page instances, not types |
| [-] | `CacheSize` | `Int32` | none | Not applicable: no page cache |
| [x] | `BackStackDepth` (read-only) | `Int32` | `NavigationPage.StackDepth` | Avalonia's (counts the current page too) |
| [x] | `CanGoBack` (read-only) | `Boolean` | `NavigationPage.CanGoBack` | Avalonia's |
| [-] | `CanGoForward` (read-only) | `Boolean` | none | Not applicable: no forward stack |
| [x] | `CurrentSourcePageType` (read-only) | `Type` | `NavigationPage.CurrentPage` | Avalonia's |
| [x] | `BackStack` (read-only) | `IList<PageStackEntry>` | `NavigationPage.NavigationStack` | Avalonia's |
| [-] | `ForwardStack` (read-only) | `IList<PageStackEntry>` | none | Not applicable: no forward stack |

#### Events (4)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Navigated` | `NavigatedEventHandler` | `NavigationPage.Pushed`, `Popped`, `Page.NavigatedTo` | Avalonia's |
| [x] | `Navigating` | `NavigatingCancelEventHandler` | `Page.Navigating` | Avalonia's |
| [-] | `NavigationFailed` | `NavigationFailedEventHandler` | none | Not applicable: navigating to an instance can't fail to make it |
| [-] | `NavigationStopped` | `NavigationStoppedEventHandler` | none | Not applicable: a navigation mid-transition is dropped, not stopped |

#### Methods (7)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void GoBack()` | `PopAsync()` | Avalonia's; the phone goes back with the transition the page came with, so an app passes it to `PopAsync` (README) |
| [-] | `void GoForward()` | none | Not applicable: no forward stack |
| [x] | `Boolean Navigate(Type sourcePageType, Object parameter)` | `PushAsync(Page)` | Avalonia's, with the turnstile |
| [-] | `String GetNavigationState()` | none | Not applicable: no serialised navigation state |
| [-] | `void SetNavigationState(String navigationState)` | none | Not applicable: as above |
| [x] | `Boolean Navigate(Type sourcePageType, Object parameter, NavigationTransitionInfo infoOverride)` | `PushAsync(Page, IPageTransition)` | `SlideTransition`, `ContinuumTransition`, `TurnstileTransition` |
| [x] | `Boolean Navigate(Type sourcePageType)` | `PushAsync(Page)` | Avalonia's |

#### Properties, from `ContentControl` (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ContentTransitions` | `TransitionCollection` | `PageTransition` | The turnstile |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Not applicable: pages are controls |
| [-] | `ContentTemplate` | `DataTemplate` | `NavigationPage.PageTemplate` | Avalonia's; pages are controls, as on the phone |
| [x] | `Content` | `Object` | `NavigationPage.CurrentPage` | Avalonia's |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | none | Not applicable |

#### Methods, from `ContentControl` (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | none | Not applicable: subclassing, not theming |
| [-] | `void OnContentTemplateChanged(…)` (protected, overridable) | none | Not applicable |
| [-] | `void OnContentTemplateSelectorChanged(…)` (protected, overridable) | none | Not applicable |

#### Default style: setters (5)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HorizontalContentAlignment` | `Stretch` | the page presenters stretch | Themed |
| [-] | `IsTabStop` | `False` | `Focusable` false by default | Avalonia's already |
| [x] | `VerticalContentAlignment` | `Stretch` | the page presenters stretch | Themed |
| [x] | `Background` | `{ThemeResource SystemColorWindowBrush}` | `Background` | `ApplicationPageBackgroundThemeBrush`: the key isn't in the theme's resources (Decided, above) |
| [x] | `ContentTransitions` | `TransitionCollection` (`NavigationThemeTransition`) | `PageTransition` | The turnstile |

#### Resources (1)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [-] | `SystemColorWindowBrush` | not in themeresources.xaml | | no | A system colour, not a theme resource; `ApplicationPageBackgroundThemeBrush` instead |

Template parts, visual states, transitions: none. Avalonia's `NavigationPage` parts are all in the template: the page
presenters, the content host, the modal presenters, the navigation bar and its back button, header and top command bar,
the bottom command bar; `PART_NavBarShadow` is left out (the phone draws no shadow, so `HasShadow` does nothing).
