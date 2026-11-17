# MenuFlyout: comparison

**The phone:** the emulator's screenshots of a `MenuFlyout` shown on a Button "open", Emulator 8.1 U1 1080P 5.5 inch
(device 11), 1080 × 1920 at 2.4, runs `20261116-1934-dev11` and `-1940` (#150), in
[`ref/wp81/MenuFlyout/`](../../../ref/wp81/MenuFlyout/README.md). Segoe WP SemiLight.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/MenuFlyoutFrames.cs`, Avalonia 12.1.4 with Skia, the
same stages (450 × 800, the Button at x 19), at rest after the opening, in [`frames/`](frames). Selawik Semilight.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)). The
phone's `states` item "pressed item" was forced into `Pressed` (tilted); ours is at rest.

| Variant | Phone, dark | Ours, dark | Phone, light | Ours, light |
|---|---|---|---|---|
| `states` | ![](../../../ref/wp81/MenuFlyout/dark__MenuFlyout__states__popup__screen.png) | ![](frames/dark__MenuFlyout__states__ours.png) | ![](../../../ref/wp81/MenuFlyout/light__MenuFlyout__states__popup__screen.png) | ![](frames/light__MenuFlyout__states__ours.png) |
| `long` | ![](../../../ref/wp81/MenuFlyout/dark__MenuFlyout__long__popup__screen.png) | ![](frames/dark__MenuFlyout__long__ours.png) | ![](../../../ref/wp81/MenuFlyout/light__MenuFlyout__long__popup__screen.png) | ![](frames/light__MenuFlyout__long__ours.png) |
| `at-300` | ![](../../../ref/wp81/MenuFlyout/dark__MenuFlyout__at-300__popup__screen.png) | ![](frames/dark__MenuFlyout__at-300__ours.png) | ![](../../../ref/wp81/MenuFlyout/light__MenuFlyout__at-300__popup__screen.png) | ![](frames/light__MenuFlyout__at-300__ours.png) |
| `at-500` | ![](../../../ref/wp81/MenuFlyout/dark__MenuFlyout__at-500__popup__screen.png) | ![](frames/dark__MenuFlyout__at-500__ours.png) | ![](../../../ref/wp81/MenuFlyout/light__MenuFlyout__at-500__popup__screen.png) | ![](frames/light__MenuFlyout__at-500__ours.png) |
| `at-640` | ![](../../../ref/wp81/MenuFlyout/dark__MenuFlyout__at-640__popup__screen.png) | ![](frames/dark__MenuFlyout__at-640__ours.png) | ![](../../../ref/wp81/MenuFlyout/light__MenuFlyout__at-640__popup__screen.png) | ![](frames/light__MenuFlyout__at-640__ours.png) |
| `at-720` | ![](../../../ref/wp81/MenuFlyout/dark__MenuFlyout__at-720__popup__screen.png) | ![](frames/dark__MenuFlyout__at-720__ours.png) | ![](../../../ref/wp81/MenuFlyout/light__MenuFlyout__at-720__popup__screen.png) | ![](frames/light__MenuFlyout__at-720__ours.png) |

## Measured

Raw pixel rows of the 1080 × 1920 frames, dark, unless named view pixels.

| What | Phone | Ours | |
|---|---|---|---|
| Width | 0 to 1080 | 0 to 1080 | **match** |
| `at-300`: the lines (menu below the target) | 887 to 890, 1470 to 1473 | 887 to 890, 1465 to 1468 | top **matches**; 5 rows shorter (below) |
| `at-500`: the lines (above) | 642 to 645, 1225 to 1228 | 647 to 650, 1225 to 1228 | bottom at the target's top **matches**; 5 rows shorter |
| `at-640`, `at-720`: the bottom line | 1561 to 1564, 1753 to 1756 | 1561 to 1564, 1753 to 1756 | **match** |
| `states`: the lines (5 items, a separator) | 1043 to 1046, 1749 to 1752 | 1043 to 1046, 1743 to 1746 | top **matches**; 6 rows shorter |
| An item | 123 rows (51.25 view) | 122 rows (50.83 view) | **deviation**: the text's line, Segoe WP's 1.33 em at 25.5 px, is a raw row short of the phone's SemiLight line (ADR 0003). The menus above are 1 row an item shorter |
| The first item's top | 16.67 view below the menu's top | 16.67 view | **match** |
| The text's left | 19.17 view | 19.17 view | **match** (the ink from 46 to 51, glyph by glyph) |
| Lines, dark / light | `#FFFFFF` / `#000000`, 4 rows | the same | **match** |
| Background, dark / light | `#000000` / `#FFFFFF` | the same | **match** |
| Disabled text, dark | `#666666` on black (`#66FFFFFF`) | the same brush | **match** |
| Toggle item checked / unchecked | the same pixels | the same pixels | **match** |
| Separator | nothing, 0 tall | nothing, 0 tall | **match** |
| `long`: the menu | 0 to 1920, lines at 0 to 3 and 1916 to 1919 | the same | **match**. Ours shows 1 row of a 17th item's ink at 1911, from the shorter items |
| The page under the menu | not dimmed | not dimmed | **match** |

## The motion and the dismissal

The template's storyboard (A8) and #150's readings; ours from `MenuFlyoutTests` on a `TestClock` on the phone's 64 Hz
ticks.

| What | Phone | Ours | |
|---|---|---|---|
| Opening, 0 to 380 ms | `OuterBorder` `ScaleX` 0 to 1, linear, about (0.5, 0): the line by the target spreads from the centre | the same | **match** |
| Opening, 380 to 500 ms | `InnerBorder` `ScaleY` 0 to 1, linear, from the target's side | the same | **match** |
| Until 500 ms | the borders `0,1.5,0,0` (below) or `0,0,0,1.5` (above) | the token's top or bottom alone | **match** |
| Closing | the menu goes in one frame; the layer under it about 300 ms later | the menu and Avalonia's light-dismiss layer go at once | the layer draws nothing: **match** to the eye |
| An item | `Click`, then `Closed`, in one frame; a toggle's `IsChecked` flips first | the same | **match** |
| A tap above or below | closes it in that frame; nothing under it sees the press | the same | **match** |
| Back | closes it, `Closed` at once; the app's Back isn't raised | the same | **match** |
| A drag on a long menu | it doesn't move; the page doesn't see it | the same | **match** |
| A disabled item's tap | nothing; the menu stays | the same | **match** |
