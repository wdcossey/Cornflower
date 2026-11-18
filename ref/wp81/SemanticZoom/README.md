# SemanticZoom, from the emulator

Frames of the screen recording of `-Probe semanticzoom -Record`, refapp run `20261118-2231-dev11-partial` (#211):
540 × 960, the emulator's display window at the zoom that fits the VM's screen (0.83 view px a pixel), not the phone's
1080 × 1920. `RenderTargetBitmap` can't see popups, and the zoomed-out view is one, so these come from the recording.

- `recording__zoomed-in-dark`: the zoomed-in view, a grouped ListView (groups a to h, b and e empty and hidden), below
  the probe's 60 px header band.
- `recording__zoomed-out-dark`: after a tap on header a. The jump list covers the window from its top; it has no
  background and no dim, so the list shows between the tiles. The tiles are the app's template (99 × 99, margin 6,
  48 px letter, the 8.1 docs' sample) in `JumpListItemBackgroundConverter`'s brushes: the accent for a group with items,
  #FF1F1F1F for an empty one, the letters white.
- `recording__zoomed-out-light-page`: the same with the page in the light theme and the app dark: the jump list stays
  dark, and the white page shows between its tiles.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile |
| Recording | ffmpeg gdigrab of the display window, about 36 frames a second |
| Date | 2026-11-18, 22:22 UTC |

The numbers and the motion are in [`tools/refapp/README.md`](../../../tools/refapp/README.md), "SemanticZoom".
