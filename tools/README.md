# tools

| Path | What it does | Runs on |
|---|---|---|
| [`refapp/`](refapp/README.md) | The Windows Phone 8.1 reference app. `run.ps1` builds it on the reference VM, runs it in the emulator and pulls every capture back; `-Probe` runs a probe that logs values every frame (the tilt, the pages and the turnstile, scrolling, a list's press, Pivot's and Hub's headers), `-Record` records the display, `-Native` reads what the phone computes (the visible bounds, the page's app bar, `TemplateSettings`) | Anywhere with `pwsh` and OpenSSH, driving the VM over SSH; its `vm/` scripts run on the VM |
| [`android/`](android/README.md) | The device run: builds the Android sample, installs and starts it on the phone over ADB, and pulls back a frame, the start time and what the device reported | Anywhere with `pwsh`, the .NET SDK's `android` workload and `adb`, with the phone on USB or wireless debugging |
| [`real-window/`](real-window/README.md) | `drive.ps1`: opens the desktop sample's real window, opens every page with a real click and saves a frame of it, checks its size, Back, Alt+Left and resizing, and drives the ToggleSwitch, Slider and RepeatButton by touch, mouse and wheel; `selftest.ps1` checks it without a window | `drive.ps1`: Windows only, with a display; it takes the cursor for a few seconds a page. `selftest.ps1` runs anywhere |
| [`winrt-api/`](winrt-api/Program.cs) | `types`: lists the XAML control types in a `Windows.winmd`, and which have a default style in a `generic.xaml`. `checklist <Type>`: one control's spec as a Markdown checklist, from the `.winmd`, `generic.xaml` and `themeresources.xaml`, beside Avalonia's control ([PROCESS.md](../docs/PROCESS.md) §1). `enum <Type>`: an enum's members and values | Anywhere with the .NET SDK, with the SDK files |
| [`lucide/`](lucide/Program.cs) | `generate`: the Lucide icons for `Cornflower.Controls`, from the release pinned in `pin.json`, checked by SHA-256, so the same pin gives the same files. `pin <version>` bumps the pin. `symbols.tsv` maps WinRT's `Symbol` values to Lucide icons | Anywhere with the .NET SDK and the network (or the release's files, with `--zip` and `--licence`) |
| [`motion/`](motion/README.md) | Measures the reference app's motion runs, from a run's recording or probe log: the camera, the tilt and its release, the turnstile and its forward start, the slide and continuum, the ToggleSwitch, the popups, the ProgressRing, the ScrollViewer's inertia, squash and indicator, a list item's press, Pivot's and Hub's headers. `selftest.py` checks the fits on synthetic data | Anywhere with Python 3 (standard library); `decode.py` also needs `ffmpeg`. With the runs, outside the repo |
| [`breakage/run.js`](../docs/performance/README.md#breakage-runs) | Breaks each behaviour in a list on its own, in a fresh clone, and checks that a test goes red | Anywhere with git, Node and the .NET SDK |
| [`fonts/`](fonts/README.md) | Compares font metrics, weight by weight | Anywhere with Node |
| [`tokens/`](tokens/README.md) | Writes the 8.1 theme's token files and the token tests' expected values from the SDK, and cross-checks the SDK's values against a reference app run | Anywhere with Python 3, with the SDK files and a capture |
| `guard/hash-reference.js` | Writes the hashes of the pulled Microsoft files for the repository guard test (`tests/Cornflower.Tests/Repository/`). Run it whenever more files are pulled | Anywhere with Node, with the reference folder |
| `collect-vm.ps1` | Inventories a reference VM's SDK files, and with `-CopyReferenceFiles` copies them out | The VM (Windows only). Its output is Microsoft's files: keep it outside every repository |
| `setres.ps1` | Sets the VM's desktop resolution (`-W 2560 -H 1440`) | The VM (Windows only) |

None of these read Microsoft's files from the repository, because none are in it. They read them
from where they are kept, outside it, named on the command line. The one default is where
`refapp/run.ps1` puts its runs: `-Out`, or else `$CORNFLOWER_REFERENCE/refapp`, or else, on Windows
only, `R:\cornflower-reference\refapp`.

## PowerShell

The scripts are PowerShell 7 (`pwsh`), run as `pwsh -NoProfile -File tools/<tool>/<script>.ps1`
(`-NoProfile`, so that nothing in a profile changes a run); `android/` and `refapp/run.ps1` need 7.4
or later. Install it from
[Microsoft's instructions](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
(on Windows `winget install Microsoft.PowerShell`, on macOS `brew install --cask powershell`, on Linux
the distribution's package from Microsoft's repository). Windows PowerShell 5.1 isn't supported, except
by `real-window/drive.ps1`, which runs under both. The scripts in `refapp/vm/`, `collect-vm.ps1` and
`setres.ps1` run on the reference VM, in its own Windows PowerShell.

`python` in the tools' READMEs is Python 3: where the command is `python3`, as on most Linux and
macOS systems, use that. CI parses every script with `pwsh`, compiles the Python tools, checks the
Node tools' syntax and runs `motion/selftest.py`, on Linux (`tools` in
`.github/workflows/ci.yml`); the test suite runs `real-window/selftest.ps1` (`RealWindowTests`, in `build-test`).
