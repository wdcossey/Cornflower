# Flyout and FlyoutPresenter: comparison

**The phone:** the emulator's screenshots of a `Flyout` shown on a Button "open", its content "flyout content",
Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, run `20261108-2254-dev11`, in
[`ref/wp81/Flyout/`](../../../ref/wp81/Flyout/README.md). Segoe WP; the flyout's text in XAML's default 11 px "Global
User Interface", as the presenter is in a root of its own.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/FlyoutFrames.cs`, Avalonia 12.1.4 with Skia, the same
stage (450 × 800, the Button at 19, 377.08), at rest after the opening, in [`frames/`](frames). Selawik, the text in
the page's inherited font.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).

| Variant | Phone, dark | Ours, dark | Phone, light | Ours, light |
|---|---|---|---|---|
| `default` (top) | ![](../../../ref/wp81/Flyout/dark__Flyout__default__popup__screen.png) | ![](frames/dark__Flyout__default__ours.png) | ![](../../../ref/wp81/Flyout/light__Flyout__default__popup__screen.png) | ![](frames/light__Flyout__default__ours.png) |
| `full` | ![](../../../ref/wp81/Flyout/dark__Flyout__full__popup__screen.png) | ![](frames/dark__Flyout__full__ours.png) | ![](../../../ref/wp81/Flyout/light__Flyout__full__popup__screen.png) | ![](frames/light__Flyout__full__ours.png) |

## Measured

Raw pixels of the 1080 × 1920 frames unless named view pixels.

| What | Phone | Ours | |
|---|---|---|---|
| Top flyout: left, top, width | 0, 0, 1080 | 0, 0, 1080 | **match** |
| Top flyout: height (rows of the chrome) | 188 (78.33 view) | 189 (78.75 view) | **deliberate difference**: the ScrollBar's 53 rounded up to the raw grid, the phone's to the nearest (ADR 0003) |
| Full flyout | 0, 0, 1080 × 1920 | 0, 0, 1080 × 1920 | **match** |
| Chrome, dark / light | `#1F1F1F` / `#DDDDDD` | `#1F1F1F` / `#DDDDDD` | **match** |
| The page under a top flyout | black / white, not dimmed | black / white | **match** |
| Content's top (the ScrollViewer's) | 25.42 view (dump) | 25.42 view | **match** |
| Content's ink | rows 66 to 90, columns 0 to 162 | rows 68 to 96, columns 0 to 177 | text: the phone's 11 px font against the page's; position at the left edge **matches** |
| The page's Button | rows 928 to 1019 | rows 928 to 1019 | the stage, placed to match |

## The motion

| What | Phone (the recording, ±25 ms) | Ours | |
|---|---|---|---|
| Top: opens | swings down from its top edge, 50 / 100 / 142 ms | swings down from its top edge; at rest 150 ms after its first frame | **gap until #149**: the curve is a placeholder (`PhonePopupMotion`), only the hinge and the end are the phone's |
| Full: opens | sweeps in top to bottom, 141 / 208 / 208 ms | swings about its top edge; at rest 208 ms after its first frame | **gap until #149**, as above |
| Top: closes | gone in one frame, about 100 ms after `Hide()` | at rest for 100 ms, then gone in one frame | **match**, by the recording's clock (ADR 0005: open, up to 4%) |
| Full: closes | sweeps out top to bottom, 200 / 291 / 334 ms | swings away about its top edge; gone 334 ms after its first frame | **gap until #149**: the length matches; the curve and the motion itself are placeholders |

## Dismissal

| What | Phone | Ours | |
|---|---|---|---|
| Back | not read | closes the topmost popup, ahead of the page's `NavigationPage` | **gap until #150** (`PhonePopupDismissal`) |
| A tap below a top flyout | not read | closes it; the tap doesn't reach the page | **gap until #150** |
| Event order | not read | Avalonia's | **gap until #150** |
