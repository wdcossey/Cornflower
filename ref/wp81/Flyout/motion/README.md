# Flyout, its motion, from the emulator (#149)

Frames of the recording of refapp run `20261116-1835-dev11` (`run.ps1 -Probe popupcurves -Record`), on a black page
with two ComboBoxes. The frame number is the recording's.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../../docs/decisions/0001-what-faithful-means.md)) |
| Recording | 540 × 960 (1.2 recording px a view px), dark, unchanged |
| Date | 2026-11-16 |

- `dark__recording__top-opening__frame207.png`: a top Flyout opening, its one band turned about its centre line,
  51.9°, 18 ms after the call.
- `dark__recording__full-opening__frame1141.png`: a full Flyout opening, its 100 px bands at 4.4, 22.2, 33.4 and
  41.4°, 119 ms after the call.
- `dark__recording__full-closing__frame1197.png`: a full Flyout closing, bands at −66.3, −45.6 and −24.5° (two ticks
  apart), 131 ms after `Hide()`; the top band is already gone.

What they show is in [`tools/refapp/README.md`](../../../../tools/refapp/README.md), "The popups' bands, and the inline
ComboBox".
