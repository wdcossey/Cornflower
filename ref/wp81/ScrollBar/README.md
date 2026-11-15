# ScrollBar, from the emulator

The reference app's captures of its three standalone ScrollBars ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)),
each 200 long with `Maximum` 100, `ViewportSize` 50 and `Value` 25: `vertical`, `Orientation` `Vertical` at the right
of its slot, and `horizontal`, `Orientation` `Horizontal` at the left, both with `IndicatorMode` `TouchIndicator`; and
`no-indicator`, the vertical one with no `IndicatorMode` set (`None`). Dark and light, at rest (`base`) and in the states
that draw something different, each forced with `VisualStateManager.GoToState` on a fresh instance. Copied unchanged
from refapp run `20261115-1921-dev11` (#113).

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 240 view pixels (1080 × 576 raw) for the vertical bars, 450 × 60 (1080 × 144) for the horizontal one, the page's background; the bar in a slot 19 from the left and right and 12 from the top, at 424.1666, 12.0833, 7.0833 × 200 (vertical) and 19.1667, 12.0833, 200 × 7.0833 (horizontal) once laid out |
| Date | 2026-11-15, 19:20 to 19:21 |

The names are the reference app's: `<theme>__ScrollBar__<specimen>__<state>.png`. Copied: for `vertical` and
`horizontal`, `base`, `CommonStates.Disabled` and `ScrollingIndicatorStates.NoIndicator`; for `no-indicator`, `base`,
`CommonStates.Disabled`, `ScrollingIndicatorStates.TouchIndicator` and `ScrollingIndicatorStates.MouseIndicator`. The
rest are byte for byte the `base` of their specimen and aren't copied: `CommonStates.Normal` and `PointerOver` for all
three; `TouchIndicator` and `MouseIndicator` for `vertical` and `horizontal`; `NoIndicator` for `no-indicator`. The
template-tree dumps stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md));
the numbers read from them are in [`docs/controls/ScrollViewer/PLAN.md`](../../../docs/controls/ScrollViewer/PLAN.md).

A forced state is drawn without its fade (`FadeInThemeAnimation`, `FadeOutThemeAnimation`); the fades, and when the
indicator shows in a ScrollViewer, are run `20261115-1946-dev11`'s recording, in `tools/refapp/README.md`, "the panning
indicator".
