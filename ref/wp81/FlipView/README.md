# FlipView, from the emulator

The reference app's capture ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)) of a `FlipView` of
three grey panels ("one", "two", "three", 40 px text) with the second selected, `three-items`, full width and 300 tall,
at rest. Dark and light. Copied unchanged from refapp run `20261118-2210-dev11` (#211). The two are the same but for the
page behind: the phone's FlipView draws no buttons and no indicator, only the item, 2.5 in from each edge.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | the page's background, the FlipView full bleed, 450 × 300 |
| Date | 2026-11-18, 22:08 to 22:10 UTC |

The motion (the release rule, the snap, the ends, `SelectedIndex` from code, a press while it moves, a vertical panel) is
read by `-Probe flipview` and `flipvertical`: [`tools/refapp/README.md`](../../../tools/refapp/README.md), "FlipView".
