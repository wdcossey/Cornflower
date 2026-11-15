# tools/real-window

The desktop sample in a real window, with real input: the mouse, the keyboard, the wheel, and touch
through Windows' touch injection. Headless tests can't see everything: in Avonic, the first run of
its version of this found a control bug that 1,486 tests had missed.

**It takes over the cursor and focus for a few seconds a page, so ask first.** It puts the cursor
back and closes only the window it opened.

```powershell
pwsh -NoProfile -File tools/real-window/drive.ps1
# or, under Windows PowerShell 5.1:
powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
# only some sections, by their titles as -like patterns, comma-separated:
pwsh -NoProfile -File tools/real-window/drive.ps1 -Only 'toggleswitch*,slider*'
```

**Windows only**, by nature: it moves the real cursor through Win32 and finds the window through UI
Automation. It is written for PowerShell 7 (`pwsh`, [`tools/README.md`](../README.md#powershell))
and Windows PowerShell 5.1 alike. Both have run it end to end: 5.1 since #68, and `pwsh` 7.7 on
2026-11-12, first on `main` at `49e2311` (56 of 56 checks over the ten pages), last at `3bcf02e` (71 of 71 over
thirteen). The touch, drag and wheel sections (#115) are **owed their first run**: until then, only
`selftest.ps1` and `RealWindowTests` (below) have checked them.

It builds the sample first, from the repo so that `global.json` picks the SDK, as
`tools/android/run.ps1` does: a stale build runs the old code, and fails checks the new code passes.
`-Configuration Release` builds and runs Release instead of Debug; only a Debug build has the
performance line to check. `-NoBuild` runs the build as it is. **Close the sample first** if it's
open: it locks its files, so the script stops rather than build. Frames go to a
new folder in the temp folder for each run, which it prints, unless you pass `-Out`; the sample's
standard output goes there too, as `stdout.txt`. It exits 1 if a check fails.

`-Only` runs the sections whose title is like the pattern, or like one of its comma-separated parts
(`'slider*'`, `'toggleswitch*,repeatbutton*'`); it fails if none is. The whole run is the gate.

**If something else keeps the focus, it stops sending input** rather than type into another app,
and fails those checks. It stops at once if the window doesn't fit on the screen. It never starts a
press outside the sample's window, nor lets a drag that started there go off the screen; whatever an
error leaves held, the mouse button or a touch, it lets go of.

## What it does

- **Every page on the list,** with nothing to add for a new control: opens it with a real click,
  checks that the shell writes the page's ready line under its title, saves a frame, fails on a
  button with no size and, in a Debug build, on a missing performance line, then clicks Back (in the
  rail beside the page) and checks the list is back. A page below the fold of a long list is
  opened through UI Automation instead: a click never goes outside the window. The ready line and the
  performance line (which the shell shows up the rail, beside the page) are looked for under the page's
  catalogue title, from its `[SamplePage]`. Titles are matched exactly, case and all.
- **The page's size:** the theme page's ready line,
  `CORNFLOWER ready open=<ms>ms scaling=<s> view=<w>x<h> raw=<w>x<h> safe=<l>,<t>,<r>,<b> page=<w>x<h> title=<title>`,
  has to name the theme page and report a 450 × 800 page (`page=`, the page's own size, so chrome
  taking space from it shows; `view=` is the window's client area, 40 wider for Back's rail), to the
  pixel. Windows gives a window whole pixels, so at 125 % a 450 page can come out at 449.6. Only
  `page=`, `scaling=` and `title=` are read.
- **Alt+Left,** Avalonia's back on desktop, from a page to the list.
- **Resizing:** the window has to say it can be resized, and the page is laid out wider once the
  window is.

Then a section for each control with input of its own, on its sample page, by touch and by the
mouse. A control is found by its `x:Name`, which Avalonia gives UI Automation as its AutomationId,
and a `TextBlock` is read by its text, which is its name there; each section reads the line the page
shows under its first control.

- **toggleswitch** (`PhoneToggleSwitch`, `PhoneNoManipulation`): a tap on the knob turns it on and
  the knob slides over (the switch's pixels change), a second turns it off and the switch is drawn as
  it was; a press that moves 10 view px is still a tap, a drag of 24 (short of half the travel) goes
  back, one of 45 goes over, and back; the knob follows the finger while it is down; a still press held
  1.3 s is a hold and does nothing; a tap on the header, off the thumb, does nothing; the same by a
  mouse click and a mouse drag; and a drag up the knob doesn't scroll the page, where the same drag
  off the switch does. The limits are the probes' (`tools/refapp/README.md`, "Probes").
- **slider** (`PhoneNoManipulation`): in the page's ScrollViewer, a touch drag across moves the thumb
  from 20 to 80 and leaves the page; a drag mostly up the page, 150 view px up and 20 across, still
  moves the thumb, from 30 to 50, and still leaves the page; a mouse drag moves it back; a touch drag
  up off the Slider does scroll the page, so the first two prove something; and the wheel scrolls it
  back to the top and down again.
- **repeatbutton** (`PhonePress`): a click counts once and focuses it, Space counts once, a touch
  tap once; a touch press counts at once and not again within 500 ms, then repeats; slid 120 view px
  off it sideways (along the row, as the page's ScrollViewer doesn't scroll across, so no pan can take
  the touch) the repeats stop, slid back they start again with no second delay, and they stop at the
  lift; and the same with the mouse. Real time on Avalonia's own timer, so the bounds are loose.
- **pointerover under a held touch**: whether Avalonia sets `:pointerover` under a real touch, which
  `PhonePress`'s note says the headless tests found it doesn't. The rail's Back draws `PhoneLowBrush`
  behind its arrow only under `:pointerover`, and a press dims it to 0.6, so a pixel in its corner is
  the rail's black unless `:pointerover` is set. It checks that the mouse over Back changes that pixel,
  then reads it while a touch is held on Back, and prints `set` or `not set`. It reads while the touch
  is held, as Windows can promote a touch to a mouse move about a second later (Avonic saw it), which
  would hover Back as the mouse does. The answer is the desktop's: a phone's touch comes through
  Avalonia's Android backend (`tools/android`).

Where a section aims on a ToggleSwitch or a Slider is in view pixels from the control's bounds, read
from the Wp81 templates, in `helpers.ps1`.

**Look at the frames as well.** Most of what's wrong with a page isn't something it checks.

## Checking it without a run

`helpers.ps1` holds the script's pure helpers: the ready line's fields, `-Only`'s match, a drag's
points, where the knob and the Slider's thumb are, and the `:pointerover` verdict. `selftest.ps1`
checks them on known answers, and the script as parsed: that it parses, that each section's title
selects it alone, and that every page it opens and every control it finds by name is in the sample.
It takes no input and opens no window, so it runs on any OS, under pwsh 7 as the repo's tools do (and
under Windows PowerShell 5.1, as `helpers.ps1` has to, since `drive.ps1` loads it under both):

```powershell
pwsh -NoProfile -File tools/real-window/selftest.ps1
```

`tests/Cornflower.Tests/Tools/RealWindowTests.cs` runs it in the test suite, so CI runs it, and checks
the rest against the sample laid out headless: that the points `helpers.ps1` aims at are the
ToggleSwitch's knob at both rests and its header off the thumb, and the Slider's thumb for a value on
its track; that its presses fall either side of `PhoneToggleSwitch`'s tap distance and half the
travel; and that each control it finds by name gives UI Automation that name, through Avalonia's
automation peer.

## What it can't tell you

- **Scale** is whatever this PC's display is set to.
- **Windows only.** CI doesn't run it, as CI has no display.
- **Not a phone's touch.** Its touch is Windows' injected touch, through Avalonia's Win32 backend,
  with one contact; a phone's comes through Android, and the phone is the real touch test
  (`tools/android`).

The frames come from the screen, not `PrintWindow`, which offsets a window by its invisible
border, and that looks like a bug in the app. They are the window's client area: UI Automation
reports that as the window's bounds.

Ported from Avonic's `tools/real-window/drive.ps1` (`wdcossey/Avonic`, `v2` at `edf87b8`): the
window, the input, the frames and the every-page loop; then, for #115, from `v2` at `57bdd3d`: the
touch injection (`RealInput`'s `TouchDown`, `TouchMove` and `TouchUp`; `Tap`, `TouchDrag`),
`MouseDrag`, `Wheel` and `WheelUp`, the pixel reads (`Pixel`, `Region`, `Changed`), `IsFocused`,
`Within`, `OnScreen` and `-Only`. New here: the ready line's size, Alt+Left, resizing, asking UI
Automation for the focus when Windows won't give it to the script, the scaling from the ready line,
a touch lifted where it last was, `Hold` and `Slide`, the pure helpers in a file of their own, and
the self-test.
