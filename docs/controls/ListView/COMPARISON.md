# ListView and ListViewItem: comparison

**The phone:** the reference app's captures, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4,
2026-11-08, in [`ref/wp81/ListView/`](../../../ref/wp81/ListView/README.md), and #113's press probes, runs
`20261115-1941-dev11` and `20261115-1948-dev11`. Segoe WP.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/ListViewFrames.cs`, Avalonia 12.1.4 with Skia, on the
same stages: 450 × 100 at 2.4 for the item, 450 × 260 for the list, in a slot 19 in and 12 down, in [`frames/`](frames).
Selawik. Our Pressed is a touch at the item's centre, left 160 ms to settle; the phone's is the state forced with no
finger (below). Our check mode is set before the item is shown, as a forced state is, so there is no slide in it.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).
Sizes, positions and colours are compared by measurement, here and in the tests.

## Dark

| State | Phone | Ours |
|---|---|---|
| the list, "two" selected | ![](../../../ref/wp81/ListView/dark__ListView__three-items__base.png) | ![](frames/dark__ListView__three-items__base.png) |
| item, `base` | ![](../../../ref/wp81/ListView/dark__ListViewItem__default__base.png) | ![](frames/dark__ListViewItem__default__base.png) |
| item, `Pressed` | ![](../../../ref/wp81/ListView/dark__ListViewItem__default__CommonStates.Pressed.png) | ![](frames/dark__ListViewItem__default__CommonStates.Pressed.png) |
| item, `Disabled` | ![](../../../ref/wp81/ListView/dark__ListViewItem__default__CommonStates.Disabled.png) | ![](frames/dark__ListViewItem__default__CommonStates.Disabled.png) |
| item, `ListMultiSelect` | ![](../../../ref/wp81/ListView/dark__ListViewItem__default__MultiSelectStates.ListMultiSelect.png) | ![](frames/dark__ListViewItem__default__MultiSelectStates.ListMultiSelect.png) |
| selected, `ListMultiSelect` | ![](../../../ref/wp81/ListView/dark__ListViewItem__selected__MultiSelectStates.ListMultiSelect.png) | ![](frames/dark__ListViewItem__selected__MultiSelectStates.ListMultiSelect.png) |
| selected, `ListMultiSelect`, pressed (ours only) | | ![](frames/dark__ListViewItem__selected__MultiSelectStates.ListMultiSelect+CommonStates.CheckboxPressed.png) |

## Light

| State | Phone | Ours |
|---|---|---|
| the list, "two" selected | ![](../../../ref/wp81/ListView/light__ListView__three-items__base.png) | ![](frames/light__ListView__three-items__base.png) |
| item, `base` | ![](../../../ref/wp81/ListView/light__ListViewItem__default__base.png) | ![](frames/light__ListViewItem__default__base.png) |
| item, `Pressed` | ![](../../../ref/wp81/ListView/light__ListViewItem__default__CommonStates.Pressed.png) | ![](frames/light__ListViewItem__default__CommonStates.Pressed.png) |
| item, `Disabled` | ![](../../../ref/wp81/ListView/light__ListViewItem__default__CommonStates.Disabled.png) | ![](frames/light__ListViewItem__default__CommonStates.Disabled.png) |
| item, `ListMultiSelect` | ![](../../../ref/wp81/ListView/light__ListViewItem__default__MultiSelectStates.ListMultiSelect.png) | ![](frames/light__ListViewItem__default__MultiSelectStates.ListMultiSelect.png) |
| selected, `ListMultiSelect` | ![](../../../ref/wp81/ListView/light__ListViewItem__selected__MultiSelectStates.ListMultiSelect.png) | ![](frames/light__ListViewItem__selected__MultiSelectStates.ListMultiSelect.png) |
| selected, `ListMultiSelect`, pressed (ours only) | | ![](frames/light__ListViewItem__selected__MultiSelectStates.ListMultiSelect+CommonStates.CheckboxPressed.png) |

The phone's `Pressed` is tilted by −17.19°, 17.19°, the angle its `PointerDownThemeAnimation` takes with no press
point; no finger gives it, so it isn't compared. The phone has no capture of the check mode pressed: CheckboxPressed
forced outside the check mode is `base`, as its box is clipped away. Ours is pressed at the box's centre (#142), so
the box is filled and sunk, and the content isn't tilted.

## Measurements

In view pixels unless raw. The phone's from the dumps, ours from the tests.

| What | Phone | Ours | |
|---|---|---|---|
| The list, in the stage | 19.1667, 12.0833, 412.0833 wide | 19.1667, 12.0833, 411.6667 wide | one raw pixel narrower: ADR 0003's rounding of the 19 margins |
| Each item | 39.1667 tall, one under the other | 39.1667, the same | match: SelectedBorder's 2.5 + 34 + 2.5 |
| The content | 0, 0 in the item, 16.25 tall; its text 15.96, 1.33 em at 12 | 0, 0, 16.25; `LineHeight` 15.96 | match |
| The box | 0, 9.5833; 25.4167 laid out (61 raw), drawn 61 raw | 0, 9.5833; 25.8333 laid out (62 raw), drawn 25.5 (61.2 raw) | within a fifth of a raw pixel drawn |
| The box's border, raw, `ListMultiSelect` | columns 46-51 and 101-106, rows 52-57 and 107-112 | 46-51 and 102-106 whole, 101 at 0.8 and 107 at 0.2; rows likewise | match within a fifth of a raw pixel |
| At rest | the box 40.5 to the left, clipped: nothing left of the item | the same | match |
| The tick, in the box | 3.75, 4.1667; 18.3333 × 17.0833 | the same | match |
| `ListMultiSelect` | the box at 0, the content 40.5 across, 0.15 s each way | the same; 0.15 s linear transitions | match |
| SelectedCheckMark | 375.4167, 2.5; 34.1667 square | 375, 2.5; 34.1667 | the item one raw pixel narrower |
| Disabled | the content at 0.4: `#666666` stems on black | the content at 0.4: its fullest ink 0.4 of the enabled text's, within 8 of 255 (83 against 88.8) | match; Skia's coverage under an opacity isn't exactly linear |
| The selected item, Single mode | drawn as the others | the same frame | match |

**The tilt**, each held press of the probes against ours, `RotationX`, `RotationY`, `GlobalOffsetZ` (the probe's
list 19.1667, 80 in the view; ours the same, one raw pixel narrower):

| Press | Phone | Ours |
|---|---|---|
| item 1 centre | -2.127, 0.009, -21.894 | -2.105, 0.000, -21.939 |
| item 2 centre | -1.523, 0.009, -22.771 | -1.500, 0.000, -22.819 |
| item 3 centre | -1.591, 0.009, -22.673 | -1.585, 0.000, -22.695 |
| item 2 at 5% across | -2.630, 8.087, -9.413 | -2.549, 8.095, -9.519 |
| item 2 at 25% across | -1.646, 4.398, -16.209 | -1.613, 4.398, -16.257 |
| item 2 at 75% across | -1.644, -4.415, -16.187 | -1.618, -4.433, -16.200 |
| item 2 at 95% across | -2.627, -8.104, -9.393 | -2.562, -8.132, -9.446 |
| item 2 at 10% down | -8.110, 0.009, -13.191 | -8.129, 0.000, -13.177 |
| item 2 at 90% down | 5.572, 0.009, -16.882 | 5.560, 0.000, -16.914 |
| item 4 centre | -0.956, 0.009, -23.597 | -0.953, 0.000, -23.614 |

The worst is 0.081° (`RotationX`, 5% across) and 0.106 of depth: the Button's model, on a short item, within a tenth
of a degree. The phone's 0.009° of `RotationY` at the centre is its item's half raw pixel off centre.

**The check mode's press** (#136's probe, runs `20261115-2352-dev11` and `20261116-0040-dev11`; #142), item 1 of the
40-item list in Multiple, held 600 ms. Off the box, the content's tilt (TiltContainer), the box flat; on the box, the
box's (CheckboxTiltContainer), the content flat:

| Press | Phone | Ours |
|---|---|---|
| item 1 centre | -2.127, 0.009, -21.894 | -2.105, 0.000, -21.939 |
| item 1 at 95% across | -3.414, -8.097, -8.258 | -3.345, -8.125, -8.318 |
| item 1 at 10% down | -8.305, 0.009, -12.907 | -8.319, 0.000, -12.901 |
| item 1 at 90% down | 5.311, 0.009, -17.262 | 5.307, 0.000, -17.281 |
| item 1's box, at its centre | -2.699, 0.146, -20.862 | -2.772, 0.262, -20.588 |

Off the box, the Single mode's within 0.07°. On the box, within 0.12° and 0.27 of depth: our box is 25.83 across, the
phone's 25.42 (ADR 0003), so the same press is 0.2 px further from its centre, and a box that small turns 0.68° a
pixel. The bias, −2.7° at the box's centre, is the eye's: from the item's centre, as the phone's box carries the
item's offset to the screen's centre. From the box's own, ours would be several degrees off across.

## Behaviours

| Behaviour | Phone | Ours | |
|---|---|---|---|
| Pressed at once | `Normal` > `Pressed` 0.3 to 2.6 ms after `PointerPressed` | `:pressed` in the same dispatch | match |
| The release | holds 182.6 ms, eases back over 167.0 ms | `Tilt`'s, the same | match |
| A tap | selects; `ItemClick` and `Tapped` with the release | selects with the release; `ItemClick` with `ListBoxOptions` (#132) | match |
| A pan | the pointer taken about 50 ms into the move; the tilt holds and returns from then; no `ItemClick` | the pointer taken once the move passes the gesture recogniser's threshold; the same return; nothing selected | match, the moment Avalonia's |
| Dragged off, the list can't pan | the tilt follows on the item, returns about 190 ms after leaving | holds 182.6 ms, returns; nothing selected | match |
| 10 px sideways | a tap; `RotationY` −0.42° at 10 px | a tap; the tilt follows | match |
| Single selection | draws nothing | draws nothing | match |
| Multiple | the check boxes; every tap toggles, on the box or off it, at the release | `Multiple, Toggle`: the same | match |
| CheckboxPressed, a press on the box | 0.6 to 39.7 ms after the press; the box in the accent, the tick over it; only the box tilts, about its centre, with the item's depth rule and eye | at once; the same, by `PhoneListMultiSelect` | match |
| The box's release | at rest the next frame (1 to 8 ms); no hold, no return | at rest in the release's dispatch, so the next frame | match |
| A press off the box, check mode | the Single mode's `Pressed`: the content tilts, the box stays flat; holds and returns as the Button's | the same: `Tilt` on TiltContainer | match |
| Extended | no check boxes; a tap is Single's: it replaces the selection, and on the selected item changes nothing; each press tilts | `Multiple`: the same; Ctrl and Shift choose several, which draws nothing | match |
| None | presses and tilts, selects nothing | no such mode | gap |
| A `SelectedIndex` set from code | scrolls at once (the same call), by the least: 30 to 494.17, 5 to 195.83, 39 to 846.67; no animation | `AutoScrollToSelectedItem`: the same offsets before the next frame, still on the frames after | match; ours a layout pass later, inside the frame |

## Gaps and deliberate differences

- **`ItemClick` and `IsItemClickEnabled`**: Avalonia's ListBox has none; `ListBoxOptions` adds them (#132,
  [ItemClick](../ItemClick/README.md)).
- **`SelectionMode="None"`**: Avalonia has no mode without selection. In Single mode the selection draws nothing, so
  only `SelectedItem` tells the difference.
- **`Header` and `Footer`**, which scroll with the items: none in Avalonia's ListBox. An app puts its header in the
  first item, or round the list.
- **Grouping**, so ListViewHeaderItem and GroupItem: none in Avalonia (PLAN.md). Deferred.
- **Add, delete and reorder transitions**, and **reorder**: deferred.
- **The check mode's offsets** are literals in the theme, so an app's `ListViewItemContentOffsetX` doesn't move them.
- **A selection made with Ctrl or Shift in `Multiple`** (WinRT's Extended) draws nothing, as on the phone, whose
  Extended has no boxes and no modifier keys.
- **The box's press doesn't follow a move of the finger** (not read); a drag on the box becomes a pan, which ends it.
- **`IsHoldingEnabled`** isn't turned off, so a long press still opens an item's `ContextFlyout`.
- **One raw pixel** here and there: the list's width, the box's laid-out size (ADR 0003).
- **The scroll physics** are Avalonia's: the ScrollViewer's recorded deviation (#116).
