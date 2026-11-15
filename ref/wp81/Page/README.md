# Page, from the emulator

Five frames of the reference app's `pages` probe (`tools/refapp/run.ps1 -Probe pages -Record`), run `20261110-0810-dev11`:
a Frame with a `NavigationThemeTransition` (what the root Frame has) navigating between two pages, a white and a blue
sheet inset 19, 60, labelled "page A" and "page B". A page has no state to force, so there are no catalogue captures:
these are frames of the screen recording.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Recording | `ffmpeg gdigrab` of the emulator's display, 540 × 960 (0.83 view px a pixel), about 40 frames a second, decoded to 8-bit grey (`tools/motion/decode.py`'s command); each frame here written unchanged as a grey PNG |
| Date | 2026-11-10, 08:08 to 08:10 |
| Theme | Dark |

| File | Recording time | What |
|---|---|---|
| `dark__Page__rest.png` | 27.719 s (frame 941) | Page A at rest |
| `dark__Page__turnstile-forward-tick0.png` | 6.813 s (frame 254) | A forward turn's first frame: page B at −67.0°, tick 0 |
| `dark__Page__turnstile-back-tick0.png` | 26.500 s (frame 898) | A back turn's first frame: page A at +42.0°, tick 0 |
| `dark__Page__slide-forward-tick1.png` | 27.906 s (frame 949) | The slide forward: page B 152.8 px below its place, tick 1 |
| `dark__Page__continuum-forward-first.png` | 38.500 s (frame 1297) | The continuum forward: page B's first frame, at a scale of 0.963 |

The angles, offsets and scales are `tools/motion`'s (`turnstile.py`, `slide.py`) and
[`docs/controls/Page/PLAN.md`](../../../docs/controls/Page/PLAN.md)'s. The recording and its decoded frames stay outside
the repository, in the run's folder.
