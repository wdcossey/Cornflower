# Hub, from the emulator

Two sets, both from the reference app ([`tools/refapp/`](../../../tools/refapp/README.md)) on Emulator 8.1 U1 1080P
5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)): 1080 × 1920
raw pixels, `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800.

## The catalogue's captures (run `20261108-1443-dev11`, 2026-11-08)

A `Hub` with the header "hub" and two `HubSection`s, headers "one" and "two", no content
([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs), variant `two-sections`), full width on a
450 × 400 stage, dark and light, at rest (`base`) and in each state of its template's `BackgroundImageLoadStates`
group, forced with `VisualStateManager.GoToState` on a fresh instance. Copied unchanged:
`<theme>__Hub__two-sections__<state>.png`.

**Each was taken during the Hub's entrance animation**: the template's `EntranceAnimationProjection` turns the root
−53.7° (dark `base`), −46.4° (light `base`) and −3.2° to −5.1° (the six state captures) about Y, so the sections and
the header are drawn turned and shifted. They show the header's size and weight and the sections' headers, but no
bound can be read from them at rest. With no `Background`, the three states draw the same.

## The recording's frames at rest (run `20261115-2059-dev11`, 2026-11-15)

The `-Probe hub` run's screen recording (1.2 window pixels a view pixel, 540 × 960), decoded to gray by
`tools/motion/decode.py`, two frames written out as PNG unchanged: the probes' Hub (the header "hub", four sections of
30 px text, below a 60 px band; [`Probe.Shell.cs`](../../../tools/refapp/app/Probe.Shell.cs)) at rest at the first
section (`dark__Hub__four-sections__first__recording.png`, frame 100) and at the last
(`dark__Hub__four-sections__last__recording.png`, frame 650), where the first section's edge shows past the last.

The template-tree dumps and the probes' logs stay outside the repository
([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md)); the numbers read from them are quoted in
[`docs/controls/Hub/PLAN.md`](../../../docs/controls/Hub/PLAN.md) and written out in
`tests/Cornflower.Tests/Controls/HubReadings.cs`. The motion has no capture: it is read frame by frame from #128's and
#137's probe logs (`tools/refapp/README.md`, "Hub's header" and "Hub: a background image, and the end of the sections").
