# ContentControl, from the emulator

No capture shows a bare ContentControl: the reference app's catalogue has none. The phone's own templates hold them,
and these are the captures of one: Pivot's `TitleContentControl`, a ContentControl whose own style
(`PivotTitleContentControlStyle`) sets only its font and margin, so where its content goes is the default
ContentControl style's. The reference app's `Pivot` specimen ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs),
variant `two-items`: `new Pivot { Title = "CORNFLOWER" }` with two items), dark and light, at rest (`base`). Copied
unchanged from refapp run `20261108-1732-dev11`.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | 450 × 320 view pixels (1080 × 768 raw), the page's background; the title's ContentControl at 19.1667, 37.9167, 430.8333 × 24.1667 once laid out |
| Date | 2026-11-08, 17:32 UTC |

The names are the reference app's: `<theme>__Pivot__two-items__base.png`. Only the title, "CORNFLOWER", is the
ContentControl's; the headers and the item below it are Pivot's, compared in Pivot's own PR. The template-tree dumps
beside them stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the
numbers read from them are quoted in [`docs/controls/ContentControl/PLAN.md`](../../../docs/controls/ContentControl/PLAN.md)
and written out in `tests/Cornflower.Tests/Controls/ContentControlTests.cs`.

The ContentPresenter's reading, the ToggleSwitch's curtain presenters, is in the ToggleSwitch's captures,
[`ref/wp81/ToggleSwitch/`](../ToggleSwitch/README.md) (`on`, `base`), and isn't copied again.
