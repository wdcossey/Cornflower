# CommandBar, AppBarButton, AppBarToggleButton and AppBarSeparator: comparison

**The phone:** the shell's screenshots of a page's app bar, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920
at 2.4, run `20261109-2124-dev11`, in [`ref/wp81/CommandBar/`](../../../ref/wp81/CommandBar/README.md). Segoe WP and
Segoe UI Symbol.
**For #141:** the screenshots of run `20261116-0005-dev11` (the bar open with 0, 1, 3, 4, 5 and 10 secondary
commands) and stills from the recording of run `20261115-2354-dev11` (540 × 960, 1.2 recording px a view px), in the
same folder; the motion from that recording and `20261116-0042-dev11`'s, read by `tools/motion/appbar.py`.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/CommandBarFrames.cs`, Avalonia 12.1.4 with Skia, the
same page (450 × 800, the same commands, the icons Lucide's `Plus`, `Star` and `Trash`), in [`frames/`](frames).
Selawik.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).
The icons aren't either: Lucide's are their own.

| Variant | Phone, dark | Ours, dark | Phone, light | Ours, light |
|---|---|---|---|---|
| `compact` | ![](../../../ref/wp81/CommandBar/dark__AppBar__compact__popup__screen.png) | ![](frames/dark__AppBar__compact__ours.png) | ![](../../../ref/wp81/CommandBar/light__AppBar__compact__popup__screen.png) | ![](frames/light__AppBar__compact__ours.png) |
| `minimal` | ![](../../../ref/wp81/CommandBar/dark__AppBar__minimal__popup__screen.png) | ![](frames/dark__AppBar__minimal__ours.png) | ![](../../../ref/wp81/CommandBar/light__AppBar__minimal__popup__screen.png) | ![](frames/light__AppBar__minimal__ours.png) |
| `compact-open` | ![](../../../ref/wp81/CommandBar/dark__AppBar__compact-open__popup__screen.png) | ![](frames/dark__AppBar__compact-open__ours.png) | ![](../../../ref/wp81/CommandBar/light__AppBar__compact-open__popup__screen.png) | ![](frames/light__AppBar__compact-open__ours.png) |
| `minimal-open` | ![](../../../ref/wp81/CommandBar/dark__AppBar__minimal-open__popup__screen.png) | ![](frames/dark__AppBar__minimal-open__ours.png) | ![](../../../ref/wp81/CommandBar/light__AppBar__minimal-open__popup__screen.png) | ![](frames/light__AppBar__minimal-open__ours.png) |

The phone's page shows its title, "page with an app bar"; ours is the bare page.

**The open bar with other numbers of secondary commands** (#141): "first menu item", "second menu item", then "menu
item 3" and on.

| Variant | Phone, dark | Ours, dark | Phone, light | Ours, light |
|---|---|---|---|---|
| `compact-open-0-secondary` | ![](../../../ref/wp81/CommandBar/dark__BottomAppBar__compact-open-0-secondary__popup__screen.png) | ![](frames/dark__BottomAppBar__compact-open-0-secondary__ours.png) | ![](../../../ref/wp81/CommandBar/light__BottomAppBar__compact-open-0-secondary__popup__screen.png) | ![](frames/light__BottomAppBar__compact-open-0-secondary__ours.png) |
| `compact-open-1-secondary` | ![](../../../ref/wp81/CommandBar/dark__BottomAppBar__compact-open-1-secondary__popup__screen.png) | ![](frames/dark__BottomAppBar__compact-open-1-secondary__ours.png) | ![](../../../ref/wp81/CommandBar/light__BottomAppBar__compact-open-1-secondary__popup__screen.png) | ![](frames/light__BottomAppBar__compact-open-1-secondary__ours.png) |
| `compact-open-3-secondary` | ![](../../../ref/wp81/CommandBar/dark__BottomAppBar__compact-open-3-secondary__popup__screen.png) | ![](frames/dark__BottomAppBar__compact-open-3-secondary__ours.png) | ![](../../../ref/wp81/CommandBar/light__BottomAppBar__compact-open-3-secondary__popup__screen.png) | ![](frames/light__BottomAppBar__compact-open-3-secondary__ours.png) |
| `compact-open-4-secondary` | ![](../../../ref/wp81/CommandBar/dark__BottomAppBar__compact-open-4-secondary__popup__screen.png) | ![](frames/dark__BottomAppBar__compact-open-4-secondary__ours.png) | ![](../../../ref/wp81/CommandBar/light__BottomAppBar__compact-open-4-secondary__popup__screen.png) | ![](frames/light__BottomAppBar__compact-open-4-secondary__ours.png) |
| `compact-open-5-secondary` | ![](../../../ref/wp81/CommandBar/dark__BottomAppBar__compact-open-5-secondary__popup__screen.png) | ![](frames/dark__BottomAppBar__compact-open-5-secondary__ours.png) | ![](../../../ref/wp81/CommandBar/light__BottomAppBar__compact-open-5-secondary__popup__screen.png) | ![](frames/light__BottomAppBar__compact-open-5-secondary__ours.png) |
| `compact-open-10-secondary` | ![](../../../ref/wp81/CommandBar/dark__BottomAppBar__compact-open-10-secondary__popup__screen.png) | ![](frames/dark__BottomAppBar__compact-open-10-secondary__ours.png) | ![](../../../ref/wp81/CommandBar/light__BottomAppBar__compact-open-10-secondary__popup__screen.png) | ![](frames/light__BottomAppBar__compact-open-10-secondary__ours.png) |

**A finger held on add** (#141), closed and open: the recording's stills (half the screenshots' size; favorite unchecked
and delete enabled there) and ours.

| | Phone, at rest | Phone, pressed | Ours, pressed |
|---|---|---|---|
| closed | ![](../../../ref/wp81/CommandBar/dark__recording__compact-at-rest__frame227.png) | ![](../../../ref/wp81/CommandBar/dark__recording__add-pressed-closed__frame268.png) | ![](frames/dark__AppBar__add-pressed-closed__ours.png) |
| open | ![](../../../ref/wp81/CommandBar/dark__recording__open-at-rest__frame553.png) | ![](../../../ref/wp81/CommandBar/dark__recording__add-pressed-open__frame595.png) | ![](frames/dark__AppBar__add-pressed-open__ours.png) |

## Measurements

Raw pixels (px) in the 1080 × 1920 frames, read the same way from both (the bounding box of each mark's ink against
the bar's colour).

| | Phone | Ours | |
|---|---|---|---|
| The bar's top, compact / minimal / open | 1769 / 1857 / 1235 | 1769 / 1857 / 1235 | match |
| The bar, dark / light | `#1F1F1F` / `#DDDDDD` | the same | match |
| The circles' boxes, compact | 312..409, 491..584 (checked, dark), 666..763; rows 1795..1892 | 312..409, 489..586, 666..763; rows 1794..1891 | match, within a raw pixel; the checked circle 98 across in dark, as the phone's is in light (the phone's dark one is 94: its outer 2 px are the bar's colour) |
| The ring, left column on row 1844 | 312..318, 6.44 px of ink | 312..318 | match |
| The ring, dark / light | 255 / 0 | 255 / 0 | match |
| The disabled ring, dark / light | 132 / 121 | 132 / 120 | match, within a level |
| The checked circle's fill, dark / light | 255 / 0 | 255 / 0 | match |
| The ellipsis's dots, compact | 976..987, 998..1009, 1019..1030; rows 1794..1805 | 976..987, 998..1008, 1019..1030; rows 1794..1805 | match, within a raw pixel (the glyph's own test, #63) |
| The dots' colour, dark / light | 255 / 29 | 255 / 28 | match, within a level |
| The labels' baseline, open | row 1406 | row 1407 | match, within a raw pixel |
| The label "add", open | x 338..382 | x 337..385 | text; Selawik SemiBold against Segoe WP |
| The list's baselines, open | rows 1537, 1679 | rows 1537, 1679 | match |
| The list's first ink ("f"), open | x 51, rows 1486..1536 | x 51, rows 1486..1536 | match |
| The open bar's foot | 100 view px below the last baseline | the same | match, for two secondary commands |
| The open bar's top, 0 / 1 / 3 / 4 / 5 / 10 secondary commands (#141) | 1706 / 1518 / 1235 / 915 / 915 / 915 | the same | match, to the raw pixel |
| The list's ink rows with 10, from the bar's top (#141) | 251..301, 394..443, 537..585, 679..727, 821..869, 963..1004 | 251..302, 394..444, 538..586, 680..728, 822..870, 964..1004 | match, within a raw pixel; text |
| The pressed disc, closed bar, recording px (#141) | x 158..203, y 896..942; 45.85 across from its area; centroid 2.54 above the circle's at rest | x 157..203, y 896..941; 45.98; 2.64 above | match, within a recording pixel |
| The pressed disc, open bar (#141) | its centroid 0.99 recording px above the circle's at rest | 2.64 above | 1.65 recording px (1.4 view) high: ours draws the closed bar's rise on both |
| The pressed disc's colour (#141) | #3E65FE | #3E65FF (`PhoneAccentBrush`, Cobalt) | match, within a level |
| The pressed icon (#141) | white, the same ink as at rest | white, the same size | match |
| The icons | Segoe UI Symbol: the plus inks 51 px | Lucide 20 × 20: the plus inks 32 px | deliberate difference: Lucide's shapes and sizes |

## Behaviours

| Behaviour | |
|---|---|
| The closed compact and minimal bars, and the open bar, as the shell draws them | match |
| The labels show only while the bar is open | match |
| The ellipsis always shows, and opens and closes the bar | match |
| The checked toggle: the circle filled, the icon the bar's colour | match |
| A disabled command at 0x73 | match |
| The pressed command: the circle filled with the accent, the icon white, the circle sunk (#141) | match, dark (the recording is dark only; ours is the same in light). The rise is the closed bar's, open or closed |
| Checked and pressed | the shell's press, as unchecked: decided (#141), as the recording holds an unchecked command and the template's ring alone isn't read on the bar |
| A pressed line of the list tilts and keeps its colours (#141) | match: ours is `cf:Tilt` |
| A tap above the open bar closes it (#141) | match: Avalonia's light dismiss |
| Invoking a command closes the open bar: a primary one, a menu line, the toggle (#141) | match. The order differs: the phone raises `Closed`, then `Click` 2 to 21 ms later; ours raises `Click`, then closes (#118: a bound `Command` runs with the bar still open) |
| A tap on the minimal bar away from the ellipsis opens it (#141) | match (`PhoneAppBar`). The compact bar's isn't read, and ours doesn't |
| The open bar's height with other numbers of secondary commands (#141) | match: 0, 1, 2 or 3, 4 or more, to the raw pixel. A list of more than five scrolls in ours; whether the shell's does isn't read |
| The opening and closing motion (#141) | match, within the recording's ranges (below) |
| `ActualHeight`: 57.5 on the phone, the control under the shell's 62.92 | deliberate difference: ours is the drawn 62.9166 (PLAN, "The bar") |
| The open bar: on the phone the shell draws it over the page and the control stays 57.5 | deliberate difference: ours grows upwards from its foot; a page lays it over its content (README, "On a page") |
| A command off the bar | deliberate difference: ours looks as on the bar; the phone's template draws a smaller circle and label ([`ref/wp81/AppBarButton/`](../../../ref/wp81/AppBarButton/README.md)) |
| `AppBarSeparator`: no style on the phone, so nothing drawn | match |

## Are the open bar's steps whole menu lines? (#141)

No, but one is. A line is 142 raw px (59.17 view), baseline to baseline, on the phone and in ours. The open bar's
heights are 214, 402, 685 and 1005 raw px, so the steps are 188 (1.32 lines), 283 (1.99) and 320 (2.25). Only the
step from one line to two or three is whole lines, within a raw pixel: with one line and with three, the bar leaves
the same 100 raw px (41.7 view) under the last baseline, and two lines are drawn in three lines' room. The first step
isn't (no list to one line: 188 raw), nor is the limit: 1005 raw leaves 277 under a fourth line's baseline and 135
under a fifth's. So the shell seems to size the bar for an odd number of lines, one or three, and then stop at a height
of its own, 418.75 view px (the top at 381.25). Ours doesn't model why: a style per count sets the read height.

## The motion (#141)

The bar's top moves; nothing fades. Read the way `appbar.py` reads the recording (the time constant of a straight
line through the log of the fraction to go, from 90% to 1.5% to go; when the top is 90% there; when it is at rest),
ours on 16 ms frames:

| | The phone: median (range), 14 each | Ours |
|---|---|---|
| Opening: the time constant | 83 ms (76 to 92) | 83 ms |
| Opening: 90% there | 200 ms (145 to 255) | 208 ms |
| Opening: at rest | 417 ms (384 to 455) | 416 ms |
| Closing: the time constant | 35 ms (27 to 52) | 34 ms |
| Closing: 90% there | 79 ms (67 to 127) | 96 ms |
| Closing: at rest | 200 ms (163 to 233) | 192 ms |

Ours: WinRT's `ExponentialEase`, easing out, exponent 4.5 over 420 ms opening and 5.5 over 200 ms closing
(`PhoneAppBar`), fitted to the medians. **The clock**: the phone's times are the recording's, timed by the VM's clock,
which ADR 0005 leaves open (the emulator's stopwatch or its animation time, up to 4% apart on these runs). Ours takes
them as `CornflowerClock` milliseconds, unconverted: 4% of the opening is 17 ms, under the recording's frame (18 to 36
ms) and inside each range. The tests (`The_Bar_Opens_And_Closes_As_The_Shells_Does`) read ours the same way and hold
each number inside the phone's range, compact and minimal.

Not read, so not drawn: `ClosedDisplayMode` changed while the bar is closed moves the phone's top over 36 to 50 ms
(to minimal) or 91 to 134 ms (back); ours changes at once.
