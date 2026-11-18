# GridView, GridViewItem and GridViewHeaderItem, from the emulator

From refapp run `20261118-2210-dev11` (#211), the reference app's captures
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)), dark and light:

- `GridView__six-items__base`: a `GridView` of six 120 × 120 grey tiles (`Catalog.Tile`) with the second selected, at rest.
- `GridViewItem__default__base`, and `MultiSelectStates.GridMultiSelect` forced on it (the check mode's 2.5 accent border)
  and on the `selected` item (the border and the 34 × 34 earmark with its tick). Every other state of the item's
  template draws as the ListViewItem's does, as the template is the ListViewItem's (see
  [`../ListView/README.md`](../ListView/README.md)); a selected item outside the check mode draws nothing.
- `GridViewHeaderItem__default__base`: a header on its own, "grid view header".

And from `-Probe gridview`, run `20261118-2219-dev11`, the page rendered with `RenderTargetBitmap` (1080 × 1921; the
probe's page, the GridView 19 from the left and 80 from the top, 20 tiles): `probe__gridview-multiple-dark` and `-light`,
`SelectionMode` `Multiple` with tiles 0, 1 and 3 selected, and `probe__gridview-single-dark`, `Single` with tile 1
selected.

And `recording__grouped-grid-panned-dark`, a frame of the screen recording of `-Probe grouped -Record`, run
`20261118-2249-dev11` (540 × 960, 0.83 view px a pixel): the grouped GridView (below the grouped ListView) panned about
100 px and held, with no header pinned over its top row.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | the page's background; the control 19 from the left and 12 from the top; 450 × 160 for the item, 450 × 300 for the grid, 450 × 100 for the header |
| Date | 2026-11-18, 22:08 to 22:49 UTC |

The numbers are in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "GridView".
