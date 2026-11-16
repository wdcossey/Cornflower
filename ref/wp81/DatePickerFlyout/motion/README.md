# DatePickerFlyout, its motion, from the emulator (#149)

A frame of the recording of refapp run `20261116-1835-dev11` (`run.ps1 -Probe popupcurves -Record`): a
DatePickerFlyout (15 July 2014) on a black page with two ComboBoxes. Its rows are 160 px bands.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../../docs/decisions/0001-what-faithful-means.md)) |
| Recording | 540 × 960 (1.2 recording px a view px), dark, unchanged |
| Date | 2026-11-16 |

- `dark__recording__opening__frame4600.png`: opening, bands at 17.5, 31.9 and 41.0°, 131 ms after the call.

What it shows is in [`tools/refapp/README.md`](../../../../tools/refapp/README.md), "The popups' bands, and the inline
ComboBox"; its LoopingSelectors are in "LoopingSelector, and a press on a closed ComboBox".
