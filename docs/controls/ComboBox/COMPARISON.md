# ComboBox: comparison

**The phone:** the reference app's captures, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, in
[`ref/wp81/ComboBox/`](../../../ref/wp81/ComboBox/README.md). Segoe WP.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/ComboBoxFrames.cs`, Avalonia 12.1.4 with Skia, on the
same stages (450 × 140 closed, 450 × 400 open, at 2.4), in [`frames/`](frames). Selawik. Ours is pressed by a mouse
120 px right of the box's centre and left 160 ms; the phone's Pressed is forced with no finger. Our Full frame has six
items and none selected; the phone's is the Full state forced on a three-item box (the same look).

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).
Sizes, positions and colours are compared by measurement, here and in the tests.

## Dark

| State | Phone | Ours |
|---|---|---|
| base | ![](../../../ref/wp81/ComboBox/dark__ComboBox__closed__base.png) | ![](frames/dark__ComboBox__closed__base.png) |
| Pressed | ![](../../../ref/wp81/ComboBox/dark__ComboBox__closed__CommonStates.Pressed.png) | ![](frames/dark__ComboBox__closed__CommonStates.Pressed.png) |
| Disabled | ![](../../../ref/wp81/ComboBox/dark__ComboBox__closed__CommonStates.Disabled.png) | ![](frames/dark__ComboBox__closed__CommonStates.Disabled.png) |
| InlinePlaceholder | ![](../../../ref/wp81/ComboBox/dark__ComboBox__closed__PresenterStates.InlinePlaceholder.png) | ![](frames/dark__ComboBox__closed__PresenterStates.InlinePlaceholder.png) |
| Full, nothing selected | ![](../../../ref/wp81/ComboBox/dark__ComboBox__closed__PresenterStates.Full.png) | ![](frames/dark__ComboBox__closed__PresenterStates.Full.png) |
| open, 3 items | ![](../../../ref/wp81/ComboBox/dark__ComboBox__open-3-items__stage.png) | ![](frames/dark__ComboBox__open-3-items__stage.png) |
| open, 5 items | ![](../../../ref/wp81/ComboBox/dark__ComboBox__open-5-items__stage.png) | ![](frames/dark__ComboBox__open-5-items__stage.png) |

## Light

| State | Phone | Ours |
|---|---|---|
| base | ![](../../../ref/wp81/ComboBox/light__ComboBox__closed__base.png) | ![](frames/light__ComboBox__closed__base.png) |
| Pressed | ![](../../../ref/wp81/ComboBox/light__ComboBox__closed__CommonStates.Pressed.png) | ![](frames/light__ComboBox__closed__CommonStates.Pressed.png) |
| Disabled | ![](../../../ref/wp81/ComboBox/light__ComboBox__closed__CommonStates.Disabled.png) | ![](frames/light__ComboBox__closed__CommonStates.Disabled.png) |
| InlinePlaceholder | ![](../../../ref/wp81/ComboBox/light__ComboBox__closed__PresenterStates.InlinePlaceholder.png) | ![](frames/light__ComboBox__closed__PresenterStates.InlinePlaceholder.png) |
| Full, nothing selected | ![](../../../ref/wp81/ComboBox/light__ComboBox__closed__PresenterStates.Full.png) | ![](frames/light__ComboBox__closed__PresenterStates.Full.png) |
| open, 3 items | ![](../../../ref/wp81/ComboBox/light__ComboBox__open-3-items__stage.png) | ![](frames/light__ComboBox__open-3-items__stage.png) |
| open, 5 items | ![](../../../ref/wp81/ComboBox/light__ComboBox__open-5-items__stage.png) | ![](frames/light__ComboBox__open-5-items__stage.png) |

## Measurements

View pixels unless raw pixels (px) are said; the phone's are the dumps' and its PNGs', ours `Bounds` and the frames.

| | Phone | Ours | |
|---|---|---|---|
| The header | 0, 0, 80 × 21.67 | the same | **match** |
| The box (closed, both forms) | 0, 26.67, 412.08 × 38.33; border rows 93-95 and 182-184 px | 0, 26.67, 411.67 × 38.33; the same rows | **match**; the right edge 1 px in (below) |
| The selected item's text, closed | 9.17, 29.8 | 9.17, 30.0 | **match** within 0.2: our lift is laid out on the pixel, the phone's is a transform |
| The Full form's text | 9.17, 30 | the same | **match** |
| The inline placeholder | 9, 29.97 | 9.17, 30.0 | **match** within 0.2 |
| An item (the step) | 51.67 (124 px) | 51.25 (123 px) | **gap** (below) |
| The box open, 3 items | 160 tall; border to row 476 px | 158.75; row 474 | **gap**: the items' step |
| The box open, 5 items | 263.33; border to row 724 px | 261.25; row 720 | **gap**: the items' step |
| Opening (inline) | the closed size four frames, open on the fifth (78 ms) | open at 78.125 ms, in one frame | **match** |
| Closing (inline) | the first frame | at once | **match** |
| Colours: border, text, header, pressed fill, open box, accent border, selected item, disabled | the dumps | the same keys and values | **match** (tested, dark and light) |
| Where it flips to full screen | 6 items | 6 items | **match** |
| The press | the Button's tilt on the box | `cf:Tilt` on the box | **match** (the Button's measured tilt) |

## Gaps

- **An item is 51.25 tall, not 51.67** (123 raw pixels to 124): Avalonia rounds each side of the 8.5 and 16 of padding to
  the pixel (20 and 38 px), the phone rounds the item as a whole. Five items open 2.1 view px shorter than the phone's.
- **The box's right edge is 1 px in** (raw pixel 1033 to the phone's 1034): the stretched width rounds the other way, as
  in every stretched control here.
- **The full-screen picker isn't compared here**: it is the ListPickerFlyout, compared in
  [its comparison](../ListPickerFlyout/COMPARISON.md); no capture of the ComboBox's own picker exists as a picture.
