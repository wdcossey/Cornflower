# Pivot, from the emulator

The reference app's captures of a `Pivot` titled "CORNFLOWER" with two `PivotItem`s, headers "one" and "two", each a
`TextBlock` ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs), variant `two-items`), full width
on its stage, dark and light, at rest (`base`) and in each state of its template's `Orientation` group, forced with
`VisualStateManager.GoToState` on a fresh instance. Copied unchanged from refapp run `20261108-1443-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 320 view pixels (1080 × 768 raw), the page's background; the Pivot over all of it, not in the page margin |
| Date | 2026-11-08 |

The names are the reference app's: `<theme>__Pivot__two-items__<state>.png`. `Orientation.Portrait` is pixel for
pixel `base`; `Orientation.Landscape` moves the title up to 25 (`PivotLandscapeThemePadding`). The template-tree dumps
beside them stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the
numbers read from them are quoted in [`docs/controls/Pivot/PLAN.md`](../../../docs/controls/Pivot/PLAN.md) and
written out in `tests/Cornflower.Tests/Controls/PivotReadings.cs`.

The Pivot's motion has no capture: it is read frame by frame from probes, runs `20261115-2056-dev11`,
`20261115-2136-dev11`, `20261115-2339-dev11` and `20261115-2343-dev11`
([`tools/refapp/README.md`](../../../tools/refapp/README.md), "Pivot's header strip" and "Pivot: wide headers, two
items, and a tap on a header cut by the edge").
