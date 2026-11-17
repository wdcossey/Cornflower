# ContentDialog: comparison

**The phone:** the emulator's screen grabs of a `ContentDialog` shown with `ShowAsync` from a Button "open", title
"content dialog", the body "Agjy 0123 the dialog's body text, long enough to wrap onto a second line." (a string, so one
line), buttons "ok" and "cancel" unless said; Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, runs
`20261116-1934-dev11` and `-1944`, in [`ref/wp81/ContentDialog/`](../../../ref/wp81/ContentDialog/README.md).
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/ContentDialogFrames.cs`, Avalonia 12.1.4 with Skia, the
same stage (450 × 800, the Button at 19, 377.08), at rest, in [`frames/`](frames). Selawik.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).

| Variant | Phone, dark | Ours, dark | Phone, light | Ours, light |
|---|---|---|---|---|
| `default` (two buttons) | ![](../../../ref/wp81/ContentDialog/dark__ContentDialog__default__popup__screen.png) | ![](frames/dark__ContentDialog__default__ours.png) | ![](../../../ref/wp81/ContentDialog/light__ContentDialog__default__popup__screen.png) | ![](frames/light__ContentDialog__default__ours.png) |
| `primary-only` | ![](../../../ref/wp81/ContentDialog/dark__ContentDialog__primary-only__popup__screen.png) | ![](frames/dark__ContentDialog__primary-only__ours.png) | ![](../../../ref/wp81/ContentDialog/light__ContentDialog__primary-only__popup__screen.png) | ![](frames/light__ContentDialog__primary-only__ours.png) |
| `secondary-only` | ![](../../../ref/wp81/ContentDialog/dark__ContentDialog__secondary-only__popup__screen.png) | ![](frames/dark__ContentDialog__secondary-only__ours.png) | ![](../../../ref/wp81/ContentDialog/light__ContentDialog__secondary-only__popup__screen.png) | ![](frames/light__ContentDialog__secondary-only__ours.png) |
| `no-buttons` | ![](../../../ref/wp81/ContentDialog/dark__ContentDialog__no-buttons__popup__screen.png) | ![](frames/dark__ContentDialog__no-buttons__ours.png) | ![](../../../ref/wp81/ContentDialog/light__ContentDialog__no-buttons__popup__screen.png) | ![](frames/light__ContentDialog__no-buttons__ours.png) |
| `full-size` | ![](../../../ref/wp81/ContentDialog/dark__ContentDialog__full-size__popup__screen.png) | ![](frames/dark__ContentDialog__full-size__ours.png) | ![](../../../ref/wp81/ContentDialog/light__ContentDialog__full-size__popup__screen.png) | ![](frames/light__ContentDialog__full-size__ours.png) |
| `long` | ![](../../../ref/wp81/ContentDialog/dark__ContentDialog__long__popup__screen.png) | ![](frames/dark__ContentDialog__long__ours.png) | ![](../../../ref/wp81/ContentDialog/light__ContentDialog__long__popup__screen.png) | ![](frames/light__ContentDialog__long__ours.png) |

`long-full-size` looks as `long`, and `keyboard` as `default` with a TextBox in the body and the keyboard below; ours
are not drawn (headless has no keyboard).

## Measured

Raw pixels of the 1080 × 1920 frames unless named view pixels, read with the same column scans on both.

| What | Phone | Ours | |
|---|---|---|---|
| The dialog: left, top, width | 0, 0, 1080 | 0, 0, 1080 | **match** |
| The dialog's last row, with buttons | 477 (199.17 view) | 478 (199.58 view) | **deliberate difference**: one raw row; layout rounds the rows' sizes up to the raw grid, the phone to the nearest (ADR 0003) |
| The dialog's last row, no button | 367 (153.33 view) | 367 | **match** |
| Chrome, dark / light | `#1F1F1F` / `#DDDDDD` | `#1F1F1F` / `#DDDDDD` | **match** |
| The dim layer | `#99000000` (`#99FFFFFF` light) over the window | the same `ContentDialogDimmingThemeBrush` | **match** (the dump's colour; on the black page it doesn't show in the pixels) |
| Outlines, two buttons: left, top, height | 46 and 563, 341, 92 rows (19.17 and 234.58, 142.08, 38.33 view) | 46 and 563, 341, 92 rows | **match** |
| A lone button, primary or secondary | at 46, the left half | at 46 | **match** |
| `FullSizeDesired` and long: the dialog | 0, 0, 1080 × 1920 | 0, 0, 1080 × 1920 | **match** |
| The bar's outlines: left, top, rows | 50 and 566, 1793, 100 (20.83 and 235.83, 747.08, 41.67 view) | 50 and 566, 1794, 99 | **deliberate difference**: one raw row lower at the top, the same rounding (ADR 0003); the view values are the phone's |
| The bar's text ("ok": rows of ink) | 1824 to 1859 (36 rows) | 1825 to 1857 (33) | **difference**: the shell draws the bar's text about 6% larger than a Button's (#150 saw the same of the pickers' bar); ours is the dialog's own Button. 33 rows is our Button's in the dialog too (the phone's 34): the font |
| The long title | one line, cut at the right edge | one line, cut at the right edge | **match** |
| The long body | 27 lines shown, cut at the bar (737.08 view) | 27 lines, cut at the bar | **match** |
| A string body | one line, cut ("…long enough to wr") | one line, cut | **match** |

## The motion

#149's reading of the recording of run `20261116-1835-dev11` (`tools/refapp/README.md`, "The popups' bands"), in ticks
of the phone's 64 Hz clock; ours from `ContentDialogTests` on a `TestClock` on the same ticks.

| What | Phone | Ours | |
|---|---|---|---|
| Opens | one band, the dialog, about its centre line (about y 96.9), from 45.0° to 0 at tick 9 (141 ms), an eased exponential, exponent 1.68 | the same: 45 × (1 − ExponentialEase out (1.68, t / 140.6 ms)) | **match** |
| The camera | one for the screen, 999 px in front, over y 400 | the host's, the same | **match** |
| At rest | tick 9, 141 ms | at tick 9 on the phone's ticks. The host's band rests at 134.4 ms, the Flyout's length, so a frame between 134.4 and 140.6 ms shows it at rest where the curve has under 0.8° to go | **match** on the phone's ticks; a frame in that gap is under 0.8° off |
| `Hide()`: events | `Closing`, `Closed` and the result at once, in the call's frame | the same | **match** |
| `Hide()`: drawn | as it is, gone in one frame 95 to 120 ms later (#66: about 125) | gone at 107.5 ms, the dim layer with it | **match** |

Mid-opening, at the tick nearest #149's still ([`ref/wp81/ContentDialog/motion/`](../../../ref/wp81/ContentDialog/motion/README.md);
the phone's is 540 × 960, ours 1080 × 1920):

| | Phone | Ours |
|---|---|---|
| Opening (the still 21.1°; ours 21.2°, tick 3) | ![](../../../ref/wp81/ContentDialog/motion/dark__recording__opening__frame1999.png) | ![](frames/dark__motion__opening__ours.png) |

## Dismissal

#150's reading (PR #172, `tools/refapp/README.md`, "How each popup is dismissed").

| What | Phone | Ours | |
|---|---|---|---|
| Back | `Closing` (`None`), `Closed` (`None`) and `ShowAsync`'s `None` in one frame; the app's Back isn't raised | the same, marked handled ahead of the page's `NavigationPage` | **match** |
| Back with the keyboard up | the first hides the keyboard and takes the focus; the second closes the dialog | the platform's: Android's input method takes the first Back | **owed**: the device run |
| A tap on the `#99000000` layer | nothing; it stays open; nothing under it is pressed | the same | **match** |
| A tap on the body | nothing | nothing | **match** |
| A button | its click event, `Closing` with its result, `Closed`, then the result, in one frame | the same; its command after its click event | **match**; the command's place not read |
