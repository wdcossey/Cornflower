# ContentDialog, its motion, from the emulator (#149)

A frame of the recording of refapp run `20261116-1835-dev11` (`run.ps1 -Probe popupcurves -Record`): a
ContentDialog (title, body, two buttons) on a black page with two ComboBoxes, dimmed.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../../docs/decisions/0001-what-faithful-means.md)) |
| Recording | 540 × 960 (1.2 recording px a view px), dark, unchanged |
| Date | 2026-11-16 |

- `dark__recording__opening__frame1999.png`: opening, the dialog turned 21.1° about its centre line, 32 ms after the
  call.

What it shows is in [`tools/refapp/README.md`](../../../../tools/refapp/README.md), "The popups' bands, and the inline
ComboBox".
