# Hub and HubSection

Status: done · [#122](https://github.com/wdcossey/Cornflower/issues/122) · compared against the probes' layout and recording (run `20261115-2059-dev11`), the catalogue's captures (run `20261108-1443-dev11`, mid-entrance) and #128's and #137's motion probes (runs `20261115-2059-dev11`, `20261116-0001-`, `-0007-`, `-0009-`, `-0025-dev11`) · device run passed 2026-11-16 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page hub` on `main` at `ae652d3`, 22:48 to 22:50, cold starts 716 to 826 ms over the run's pages: the page opened in 339 ms) · real-window run passed 2026-11-16 (`tools/real-window/drive.ps1`, Debug, on #161's head `d0a9ef6`: 160 of 160, every page opened from the list, ready, and left by Back) · read in [#148](https://github.com/wdcossey/Cornflower/issues/148) (#170): where a release goes, a press while it moves, `ScrollToSection` and a plain-colour background match; the loop backwards ([#165](https://github.com/wdcossey/Cornflower/issues/165)), the entrance ([#166](https://github.com/wdcossey/Cornflower/issues/166)) and an image narrower than the screen with `BackgroundImageFadingIn` ([#167](https://github.com/wdcossey/Cornflower/issues/167)) now built from those reads · since #165 to #167, device run passed 2026-11-17 (Pixel 8, over wireless adb, `run.ps1 -Page hub` on `main` at `03082b3`, 20:42 to 20:45, cold starts 757 to 879 ms over the run's pages: the page opened in 388 ms) and real-window run passed 2026-11-17 (`tools/real-window/drive.ps1`, Debug, on `main` at `03082b3`: 195 of 195) · owed: the end in landscape

The Windows Phone 8.1 Hub: Cornflower's own `Hub` and `HubSection` (`Cornflower.Controls`), behaviour only, with the
phone's motion read from the emulator frame by frame and run on `CornflowerClock` (`HubMotion`, internal); themed by
`Wp81Theme` in `src/Cornflower.Themes.Wp81/Controls/Hub.axaml`. The plan, with every value and its source, is
[PLAN.md](PLAN.md); the phone's frames and readings beside ours are in [COMPARISON.md](COMPARISON.md).

## Usage

As on the phone, the Hub is the page, full width (not in the page's 19 px margin), and each section's content is a
template:

```xml
<!-- 8.1 -->
<Hub Header="my app">
  <HubSection Header="recent"><DataTemplate>…</DataTemplate></HubSection>
  <HubSection Header="all" IsHeaderInteractive="True"><DataTemplate>…</DataTemplate></HubSection>
</Hub>
<!-- Avalonia, xmlns:cf="using:Cornflower.Controls" -->
<cf:Hub Header="my app" SectionHeaderClick="OnSectionHeaderClick">
  <cf:HubSection Header="recent"><DataTemplate>…</DataTemplate></cf:HubSection>
  <cf:HubSection Header="all" IsHeaderInteractive="True"><DataTemplate>…</DataTemplate></cf:HubSection>
</cf:Hub>
```

A section's content takes the section's `DataContext` (the Hub's, unless it has its own). `ScrollToSection` moves to a
section; `DefaultSectionIndex` is the one shown first; `SectionsInView` and `SectionsInViewChanged` say which are on
the screen; `SectionHeaders` lists their headers. For the phone's background image write the brush's stretch, as
Avalonia's `ImageBrush` defaults to `Uniform` and WinRT's to `Fill`:

```xml
<cf:Hub.Background><ImageBrush Source="/Assets/panorama.png" Stretch="Fill" /></cf:Hub.Background>
```

A swipe left goes to the next section, right to the one before; on past the last, the Hub loops to the first, and right
at the first it loops backwards to the last. Each time it is placed it turns in, as the phone's. A mouse doesn't pan, as
Avalonia's ScrollViewer doesn't; it taps interactive headers.

## Behaviours

- **The look**: the phone's templates, part for part: the 78 px Light header at 15, 1; the sections 0.9 of the Hub's
  width side by side, each inset 19, its 19 px SemiBold header below a placeholder as tall as the Hub's header, its
  content 31.5 below that; at the last section, the first section's edge past it.
- **The pan**: the sections follow the finger; the header moves at 1/18 of their speed (#128).
- **Let go**: a flick goes on one section, or back to this one's start; a slow release goes to the nearer start, on
  the phone's curves (#137): every swipe ends exactly on a section.
- **The loop**: past the last section going on, the sections carry on until the first is back at its place and the
  offset is 0 again; the header holds, flies out left, jumps a view's width right and eases back in, an
  `ExponentialEase` of 7 over 420 ms (#137).
- **The loop backwards**: at the first section a drag right pulls the sections right with the finger, nothing drawn left
  of the first, the header at 1/18. Held and let go, they come back on the phone's curve (781.25 ms); swiped, they carry
  on right on the loop's curve, and on frame 19 the Hub is at the end, the last section with the first after it; the
  header holds, flies out right, and on frame 30 jumps to 33.8 px left of its place and eases in (#148).
- **The entrance**: each time the Hub is placed, its template's root turns in about a vertical axis half its width to
  its left, from −120° to 0 over 1 s on an `ExponentialEase` (ln 100), seen through the phone's `PlaneProjection`
  camera (#148).
- **The background image**: drawn at its pixel width by the window's height, moving (W − V) / (L − V) of the offset
  until its right edge meets the view's with the last section's, then on with the sections; a copy W − 1 px along, left
  then right, fills in for the loop (#137). Hidden (`:backgroundhidden`) until the brush has its image, then shown
  (`:backgroundshowing`), straight there as #137 read. Placed with a brush (an image or a colour), the Hub is
  `:backgroundimagefadingin` for 90 ms, then showing, as #148 read; the background is at 1 throughout. An image narrower
  than the view fills a box the view's width by the window's height and doesn't move (#148). Any other brush fills the
  Hub and stays.
- **The finger**: a section's ScrollViewer keeps a drag up or down, and a Slider or ToggleSwitch its drag, through
  Avalonia's gesture arbitration; a press on moving sections holds them.
- **`SectionHeaderClick`**: a tap or click on an interactive section's header.
- **Keyboard focus**: a control in a section off screen brought into view moves the Hub to its section, at once.
- **Where it opens**: on `DefaultSectionIndex` the first time it is laid out with sections; shown again (a page cached
  and navigated back to), where it was.

The tests are `tests/Cornflower.Tests/Controls/HubTests.cs` and `Sample/HubPageTests.cs`; PLAN.md lists each behaviour
beside its test.

## The checklist

PLAN.md has the two generated checklists, every row ticked or explained. Left out: the semantic zoom members (Avalonia
has no `SemanticZoom`); `Orientation` (the phone's template can't scroll a vertical Hub); and the section's `StickyHeaderTransform` (nothing
moves it sideways). The entrance's `PlaneProjection` is a render transform the Hub sets on `PART_HubRoot`, not a part.

## Resource keys

The phone's own, in `Tokens/`: `HubHeaderMarginThickness`, `HubHeaderFontSize`, `HubHeaderCharacterSpacing`,
`HubSectionMarginThickness`, `HubSectionHeaderMarginThickness`, `HubSectionHeaderFontSize`,
`HubSectionHeaderCharacterSpacing`, `PhoneFontFamilyNormal`, and `PhoneForegroundBrush` for the Hub's foreground.
Cornflower's: `Wp81ScrollViewerScrollBarlessTheme` and `Wp81ContentControlTheme`, which the templates set.

## The ladder

Rung 5: new controls. `Hub` and `HubSection` are `TemplatedControl`s (WinRT's are `Control`s); the Hub keeps its
sections as its logical children and puts them in its template's panel. The motion is the control's own (`HubMotion`),
as the phone's is its code's and DirectManipulation's, not its template's; Pivot's is the same. The theme takes Pivot's
`PhoneCharacterSpacing` for the headers' character spacing.

## What it takes from Pivot

- Pivot's recogniser: since the Tier 2 roll-up both controls use one, `SidewaysSwipeGestureRecognizer`, through
  `ISidewaysSwipeTarget`: the same start distance, axis and pointer types. Only the Hub holds moving content on a press.
- `PivotMotion`'s frame loop: an offset of the control's own, moved by the finger and then by a timeline on
  `CornflowerClock`, drawn by moving render transforms; the ScrollViewer held in the bar-less theme, not scrolled.
- `PhoneCharacterSpacing.CharacterSpacing` and `LetterSpacing` for the two headers' spacing.
- The tests' harness: gestures on a `TestClock` on the emulator's 15.625 ms grid, the release on a frame.

## Known gaps

- **The offset's curves are fits** to the phone's frames: the swipe's 0.36 px rms (worst 3.3), the way back's 1.05
  (worst 5.3), the loop's 0.93 (worst 3.9); backwards, the way back's 0.25 (worst 1.03) and the loop's 0.68 (worst 1.9),
  each from one read (COMPARISON.md).
- **Where a release right of the first section goes** is read for a 150 px drag held (back) and a swipe (on round);
  ours mirrors the loop forward for the rest: on past half, or at 0.3 px/ms right; back otherwise (decided).
- **The header's jump in the loop backwards** (33.8 px left, then 66 ms) is one swipe on one profile; whether it
  scales with the view isn't read.
- **Where a release goes on or back** isn't read: ours goes on past 0.3 px/ms, else to the nearer start (owed).
- **A press while it moves** holds the sections where they are; not read (owed).
- **The image in the loop backwards** isn't read: ours moves it by the same rate right of 0, its copy filling in on
  the left, and it steps by about 6 px at the reset to the end (the forward loop's step is 1.4 px).
- **`ScrollToSection`** moves at once; whether the phone's animates isn't read (owed).
- **A background that isn't an image** fills the Hub and stays; the phone's sizes its background grid only for an image
  (its width was 0 with none); not read with a colour (owed).
- **The background's clip** is the Hub's edge; the phone's is a rectangle its code sets, dumped as the window's size.
- **The entrance's look**: the angles are the phone's; how they are drawn is PlaneProjection's camera as #24 read it
  (999 px over the root's centre; further for a Hub wider than the phone's 450, as the turnstile's), not checked
  against a recording of the Hub turning (owed). Both start at `Loaded`, as the phone's. The phone's input during the
  turn isn't read; ours takes it, hit through the turned root.
- **`BackgroundImageFadingIn`'s fade** can't be read in the app (its `Background` reads 1 throughout), so none is drawn;
  an image given to a Hub on the screen goes straight to showing (#137), where #148's stretch runs stayed fading in.
- **Landscape and other sizes**: the section's 0.9 is read on three portrait profiles; the header's 1/18 and the curves
  on one.
- **The last 0.417 px**: the phone's offset ends at 1,215.417, its extent a raw pixel longer than ours, so at the end
  ours is 0.42 px to the right, and the phone's loop stops 1.42 px short of where ours does before the reset.
- **The fonts**: Selawik for Segoe WP, so the headers' ink differs by a pixel or two.
- **A mouse** doesn't pan; on the phone there was none.
