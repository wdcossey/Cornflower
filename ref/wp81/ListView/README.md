# ListView and ListViewItem, from the emulator

The reference app's captures ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)) of a
`ListViewItem { Content = "list view item" }` on its own, `default`, and the same with `IsSelected = true`,
`selected`, at rest (`base`) and in each of the 21 states of its template's six groups, each forced with
`VisualStateManager.GoToState` on a fresh instance; and of a `ListView` of "one", "two" and "three" with the second
selected, `three-items`, at rest. Dark and light. Copied unchanged from refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | the page's background; the control in a slot 19 from the left and 12 from the top, so at 19.1667, 12.0833 once laid out. 450 × 100 view pixels for the item (it stretches to 412.0833 × 87.9167), 450 × 260 for the list (412.0833 × 247.9167) |
| Date | 2026-11-08, 14:37 to 14:42 UTC |

The names are the reference app's: `<theme>__ListViewItem__<variant>__<state>.png` and
`<theme>__ListView__three-items__base.png`. The template-tree dumps beside them stay outside the repository
([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from them are quoted in
[`docs/controls/ListView/PLAN.md`](../../../docs/controls/ListView/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/ListViewReadings.cs`.

The run has 86 of them (84 of the item, 2 of the list). Only those that differ are copied here, 24. In each theme:

- **Byte for byte `base`**, and not copied: `CommonStates.Normal`, `CommonStates.CheckboxPressed` (outside the check
  mode, the box it fills is clipped away), `DataVirtualizationStates.DataAvailable`, `MultiSelectStates.NoMultiSelect`,
  `SelectionStates.Selected`, `SelectedUnfocused` and `Unselected`, `ReorderModeStates.ReorderDisabled` and
  `Reorderable`, `ReorderHintStates.NoReorderHint`, `TopReorderHint` and `LeftReorderHint`; and every `selected`
  capture but its two multi-select states, `selected` at rest included: a selected ListView item draws nothing.
- **Copied**, `default`: `base`, `CommonStates.Pressed`, `CommonStates.Disabled`, `MultiSelectStates.ListMultiSelect`
  and the states the theme leaves out or defers, kept for the record: `DataVirtualizationStates.DataPlaceholder`,
  `MultiSelectStates.GridMultiSelect` (a GridView's), `ReorderHintStates.BottomReorderHint` and `RightReorderHint`,
  `ReorderModeStates.ReorderEnabled`. `selected`: `MultiSelectStates.ListMultiSelect` (the tick in the box) and
  `GridMultiSelect` (the earmark). The list: `three-items`, `base`.

`CommonStates.Pressed` is the state forced with no finger on the item: its `PointerDownThemeAnimation` ran with no
press point and left `TiltContainer` tilted by −17.19°, 17.19°, which no touch gives. The tilt under a finger is read
from the press probes instead (runs `20261115-1941-dev11` and `20261115-1948-dev11`, `tools/refapp/README.md`,
"ListView: an item's press").

Not every capture is 1080 × 241: the capture takes the item's whole drawn bounds, so `ListMultiSelect` is 1133 wide
(the content, moved 40.5 to the right, runs past the stage), `RightReorderHint` 1112, `Pressed` 267 tall (the forced
tilt) and `BottomReorderHint` 317. `ListMultiSelect`'s left 1080 columns line up with `base`'s (its box is at
the same raw pixels in both themes as the tests read it).

## ListViewHeaderItem (#211)

`dark__ListViewHeaderItem__default__base.png` and `light__`: a `ListViewHeaderItem { Content = "list view header" }` on
its own, at rest, from refapp run `20261118-2210-dev11` (2026-11-18, device 11; the stage 450 × 100, the header 19 from
the left and 12 from the top). Its `Normal`, `PointerOver` and `Disabled` states are byte for byte `base`, and its
forced `Pressed` is the forced tilt above, so neither is copied. The header in a grouped list, and its press, are read
by `-Probe grouped`: `tools/refapp/README.md`, "Grouped lists".

`recording__grouped-rest-dark.png` and `recording__grouped-list-panned-dark.png` are frames of the screen recording of
`-Probe grouped -Record`, run `20261118-2249-dev11` (540 × 960, the display window at the fitting zoom, 0.83 view px
a pixel): the grouped ListView (top) over the grouped GridView, at rest and with the list panned about 140 px and held,
its header a pinned at the top. `RenderTargetBitmap` leaves the pinned header out, so these come from the recording.
