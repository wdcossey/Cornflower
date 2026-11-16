# ListPickerFlyout, its motion, from the emulator (#149)

Frames of the recording of refapp run `20261116-1835-dev11` (`run.ps1 -Probe popupcurves -Record`): a
ListPickerFlyout of five items, item two selected, on a black page with two ComboBoxes. Each row (the header, each
item) is a band of its own (gap register B11).

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../../docs/decisions/0001-what-faithful-means.md)) |
| Recording | 540 × 960 (1.2 recording px a view px), dark, unchanged |
| Date | 2026-11-16 |

- `dark__recording__opening__frame2927.png`: opening, the header and the first two items at 17.3, 32.1 and 40.9°,
  94 ms after the call.
- `dark__recording__closing__frame2979.png`: closing, rows at −55.6, −34.5 and −13.5°, 106 ms after `Hide()`.

What they show is in [`tools/refapp/README.md`](../../../../tools/refapp/README.md), "The popups' bands, and the inline
ComboBox".
