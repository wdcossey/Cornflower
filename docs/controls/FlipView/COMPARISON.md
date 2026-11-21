# FlipView and FlipViewItem: comparison

**The phone:** the reference app's capture, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, 2026-11-18, run
`20261118-2210-dev11` (#211), in [`ref/wp81/FlipView/`](../../../ref/wp81/FlipView/README.md), Segoe WP: a FlipView of three
grey panels ("one", "two", "three", 40 px text) with the second selected, full bleed, 450 × 300, at rest.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/FlipViewFrames.cs`, Avalonia 12.1.4 with Skia, on the same
stage, in [`frames/`](frames), Selawik: the same three panels in a `Carousel`, in the Wp81 theme.

| | Phone | Ours |
|---|---|---|
| Dark | ![](../../../ref/wp81/FlipView/dark__FlipView__three-items__base.png) | ![](frames/dark__FlipView__three-items__base.png) |
| Light | ![](../../../ref/wp81/FlipView/light__FlipView__three-items__base.png) | ![](frames/light__FlipView__three-items__base.png) |

## Pixels

Each frame against the phone's, pixels whose channels differ by more than 24 (a script in the PR's scratch folder, not
kept):

| Frame | Dark | Light | Where |
|---|---|---|---|
| Three items, the second shown | 4,591 | 3,530 | the item's name, "two" (Segoe WP and Selawik), raw (7, 31) to (159, 102) |

The grey item is raw columns 6 to 1073 and rows 6 to 713 on both: 2.5 view px in from each side. The corners outside it
are the page's black or white on both.

## Behaviours

| Behaviour | Phone | Ours | |
|---|---|---|---|
| Item inset and abutting | 2.5 a side, 445 × 395, a page apart | the same | **match** |
| Buttons, indicator | none | none | **match** |
| Held release | back at 221.0, on from 233.8 of 445 | on from 231.4 (0.52 a page, the Pivot's) | **match** |
| Moving release | 109.0 at 0.20 back, 107.2 at 0.33 on | the same, by the Pivot's look-ahead of 385 ms | **match** |
| Moving release, 34.4 px at a read 0.39 px/ms | on | back (34.4 + 0.39 × 385 = 184.6 < 231.4); planned 0.6, it goes on | **gap**: the read was "too few events"; the Pivot's rule, kept |
| Snap on | 516 to 531 ms; the per-frame swipe | 515.625 ms; within 2.62 px of every frame | **match** |
| Snap on from a slow release (held, or a short drag at 0.4 to 0.6 px/ms) | starts at the drag's speed and speeds up | the swipe's curve, which starts fast: 4 to 36 px rms off | **gap** (README) |
| Snap back | 588 to 666 ms, carries on 10 to 28 px first | 640.625 ms, carries on by the curve; 6.6 px rms over 15 | **match**, within the fit |
| A release held still: the first frames | P moves on 10 to 28 px the way it was dragged | the curve's carry, from where the finger left it | **deliberate difference**: the fit carries it in proportion |
| `SelectionChanged` | at the snap's end | on its last frame | **match** |
| Past the first, past the last | 139.7 of 150; 98.1 and 100.6 of 150; eases back in 533 to 612 ms | 139.7; 99.35; back by the snap back's curve, 640.625 ms | **match** |
| No loop | never | never | **match** |
| `SelectedIndex` ±1 | animates, 516 ms | `CodeCurve`, 515.625 ms, within 4.88 px | **match** |
| `SelectedIndex` ±2 | jumps | jumps | **match** |
| A press while it snaps | stops dead; let go, on (past half) | the same | **match** |
| A tap while it snaps | 0.4 px during it, then on | stops, then the rule (on) | **match** |
| Vertical | the same along Y; a horizontal drag doesn't move it | the same | **match** |
| Virtualising | `VirtualizingStackPanel` | every item made and laid out | **gap** (README) |
| A ScrollViewer in an item at its end | chains to the FlipView | keeps the drag | **gap** (README) |
