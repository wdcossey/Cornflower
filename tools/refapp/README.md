# refapp: the Windows Phone 8.1 reference app

The reference app named in [PROCESS](../../docs/PROCESS.md), with its capture script. It runs in
the Windows Phone 8.1 emulator on the reference VM and photographs every control in the
catalogue. Each control is captured at rest and in every visual state its default template
declares, in dark and light. Beside each PNG it writes a JSON dump of the control's whole
template tree: bounds, brushes, thicknesses, fonts and transforms, as the phone resolved them.
That pair is what a Cornflower theme is measured against.

It grew out of a one-page probe on 2026-11-08, which proved the pipeline. The probe's first
numbers are kept below.

## Running it

From this PC, with the VM up and signed in:

```
pwsh -NoProfile -File tools/refapp/run.ps1             # device 11, the reference: 8.1 Update 1 1080p 5.5"
pwsh -NoProfile -File tools/refapp/run.ps1 -Device 10  # 8.1 Update 1 720p 4.7"
pwsh -NoProfile -File tools/refapp/run.ps1 -Popups only                     # the popups alone, in about 2 minutes
pwsh -NoProfile -File tools/refapp/run.ps1 -Only 'CheckBox|Radio'             # just these specimens
pwsh -NoProfile -File tools/refapp/run.ps1 -Popups only -Grab               # the popups, with a screenshot of each, in about 4 minutes
pwsh -NoProfile -File tools/refapp/run.ps1 -Demo                            # the demo page, left open in the emulator
pwsh -NoProfile -File tools/refapp/run.ps1 -Probe tilt                        # one probe, with real touches (see Probes)
pwsh -NoProfile -File tools/refapp/run.ps1 -Probe pages -Record               # a probe, with a recording of the screen (see Motion)
pwsh -NoProfile -File tools/refapp/run.ps1 -Probe scrollend -Record           # the end of the content and the indicator (see Probes)
pwsh -NoProfile -File tools/refapp/run.ps1 -Probe pivot -Record               # Pivot's header strip (see Motion; also turnstile, hub)
pwsh -NoProfile -File tools/refapp/run.ps1 -Native -Device 13                 # what the phone computes, read through its API, in about 4 minutes (see Native readings)
```

`run.ps1` runs under PowerShell 7.4 or later (`pwsh`, [`tools/README.md`](../README.md#powershell)), on
Linux, macOS or Windows, with OpenSSH's `ssh` and `scp`; the scripts in `vm\` run on the VM. `-Out`
names the folder the runs go in; without it, `$CORNFLOWER_REFERENCE/refapp`, or on Windows, with that
unset, `R:\cornflower-reference\refapp`.

Results go to a run folder, `<yyyyMMdd-HHmm>-dev<n>`, in that `-Out` folder: one PNG and one JSON per
capture, `index.json` for the run, and `index.html`, which lays every state out dark beside light.
Device indexes are in `wp81-emulator-profiles.md` in the reference folder.

`-Only <regex>` keeps the specimens and popups whose name matches. `-Popups also` adds the popups to a
run and `-Popups only` runs them alone; by default a run leaves them out, because with them inside
a full run the emulator connection broke at about 477 PNGs, twice. The app reads these from a
`run-options.txt` that `run.ps1` writes into the package for that run and then deletes.

`-Grab` holds each popup open for 5 s while `vm\grab-loop.ps1` saves a screenshot of the phone's
display, and `run.ps1` puts it beside the popup's dumps as `<id>__screen.png`: the popup's pixels,
at 1080 × 1920 on the reference profile (see [Popup pixels](#popup-pixels)). The loop's own shots
are kept in `frames\`, and each popup's hold, in UTC, in `held.txt`.

`-Demo` deploys the app with a page to use by hand instead of the suite: a button for each popup,
ContentDialog first, and dark and light. The app stays open in the emulator; nothing is captured.

A run that stops making PNGs for 3 minutes is given up, and whatever the app wrote is still
pulled back, to a folder ending `-partial`. Its `progress.txt` names the last item started.

## What it does

| Step | Where | How |
|---|---|---|
| Copy | `run.ps1` | `scp` of `app\` and `vm\` to `C:\cornflower\refapp\` on the VM |
| Build | `vm\build.ps1` over SSH | MSBuild 14, Debug, x86. About 10 s. No certificate needed |
| Deploy, run, pull | `vm\session.ps1`, as a scheduled task in the signed-in desktop | `AppDeployCmd /installlaunch` starts the emulator if needed; `ISETool dir` polls for `done.txt`; `ISETool ts` pulls the app's local folder. Progress goes to `status.txt` |
| Wait | `run.ps1` | Reads `status.txt` every 20 s |
| Collect | `run.ps1` | `scp` back, flattens, writes `index.html`, removes the task |

Each step is its own SSH command, because one long remote script that also polled a scheduled
task once hung the session. The emulator, `AppDeployCmd` and `ISETool` only see each other from
the signed-in desktop. Run from an SSH session, `ISETool` starts a hidden second emulator instead.

## The app

- **`Catalog.cs`** holds the specimens. Each is a control in one configuration, such as
  `CheckBox` `indeterminate`, with the height of its stage. Every TextBlock style key in the phone's
  `generic.xaml` and `themeresources.xaml` is a specimen too, loaded through `XamlReader`: with one line, with
  two (a `LineBreak`), and, for those that wrap, with a wrapped sentence; `lines.py` reads their line pitch and
  heights from a run. Add a control by adding a line here.
- **`Suite.cs`** builds a fresh stage per capture. The stage is the window's width and the
  specimen's height, in the requested theme, over the page's own background. The specimen sits
  at the page margin of 19 logical pixels, or full width for Pivot and Hub. The suite forces each
  state with `VisualStateManager.GoToState`, waits 450 ms, renders the stage with
  `RenderTargetBitmap` and writes the PNG and JSON. It writes `index.json` and then `done.txt` last.
- **`Popups.cs`** holds the popups: Flyout (top and full), MenuFlyout, ContentDialog (default and
  full size), ListPickerFlyout (single and multiple), PickerFlyout, DatePickerFlyout,
  TimePickerFlyout, ComboBox open with every count of items from 3 to 10 (up to 5 open
  inline, 6 or more open the full-screen picker; below), the page's app bar, compact and
  minimal, closed and open (below), and the system's `MessageDialog` (B18), with a one-line title and a long one.
  The shell shows a `MessageDialog`: it isn't among the app's open popups, it is drawn in the phone's theme, not
  the stage's (the app sets no theme of its own, so the runs, all dark, can't tell the phone's from the app's), and
  cancelling its `ShowAsync` doesn't close it (2026-11-11), so it stays over every later capture and a second can't
  open (`E_ACCESSDENIED`). So a run leaves the two out unless `-Only` picks exactly one and nothing else, and then
  opens it once, dark: `-Popups only -Only '^MessageDialog$' -Grab`, then `'^MessageDialogLongTitle$'`, each a
  fresh launch. Picked with anything else, the run captures nothing and its error says why. Where the app can't see
  the dialog, the suite reads `ShowAsync`'s status instead, after it settles and after the hold, and records an
  error if it isn't showing.
  **`Suite.Popups.cs`** opens each from a button or the control on a stage, gives each open popup
  the requested theme, renders the stage, and dumps every open popup's tree, in window
  coordinates, to `<theme>__<name>__<variant>__popup<n>.json`. It then closes the popup and waits
  up to 3 s for it to animate out.
- **`TreeDump.cs`** writes the visual tree. Bounds are relative to the specimen, and the stage
  offset is recorded, so a JSON rectangle maps to PNG pixels as
  `(elementInStage + bounds) × rawPixelsPerViewPixel`. Not `resolutionScale`: it reports 180 on
  the profiles at 1.8 to 2.4 and 140 on the three at 1.2, devices 7 and 8 (WVGA) and 13 (qHD)
  ([Native readings](#native-readings-2026-11-11-devices-7-to-13)).
- **`Suite.Native.cs`** (`run.ps1 -Native`) captures nothing: it reads the values the phone computes
  in native code through the public API and writes them to `native.json`. The display's scale and
  DPI; the system tray's `OccludedRect`, `BackgroundOpacity` and colours, the view's
  `VisibleBounds`, `Window.Current.Bounds` and where the page and its content sit, with the tray
  shown and hidden, under both `ApplicationViewBoundsMode`s, in portrait and both landscapes
  (the app asks for each with `DisplayInformation.AutoRotationPreferences`, and the phone turns
  to it); then every `TemplateSettings` property of ComboBox, ProgressBar, ProgressRing and
  ToggleSwitch, for the catalogue's specimens at rest and in every state their templates declare,
  each forced on a fresh instance as the suite forces it, and for ComboBoxes opened inline; and the page's app bar's
  size and the visible bounds with it ([the page's app bar](#the-pages-app-bar-2026-11-09-device-11-23)). It is
  the only mode that leaves the tray shown at launch; the others hide it first, as before.

What a capture is and isn't:

- It is what the template draws in that state, on a fresh instance. It is not the motion into
  the state: `useTransitions` is false. The motion is read by the probes, and recorded with
  `-Record` where the app can't read it ([Motion](#motion-2026-11-10-device-11-24)).
- A state is forced, not reached by input. Where `stateApplied` is false, the template has no
  state of that name and the PNG shows the rest state.
- **`RenderTargetBitmap` can't see popups on the phone,** so a popup's PNG comes from the screen (`-Grab`):
  rendering a popup's child throws `ArgumentException`, and rendering the whole window
  (`RenderAsync(null)`) leaves popups out. Moving a popup's child onto a stage does render, but
  not as the phone draws it: the content lays out again and loses its state (a list picker came
  out 1921 × 2066, a menu flyout with no items), so that was dropped.
- **Never close a popup by force.** Setting `IsOpen = false` on a full-screen picker or dialog
  while it animates out killed the app in all three runs that did it. Closing with `Hide()` and
  waiting for the popup to go, the next run finished.

## Facts the pipeline relies on

- `RenderTargetBitmap` sizes its bitmap to what is drawn, not to the layout slot. A stage with an
  explicit size and an opaque background gives full frames. On the probe, a 400 × 711 page with
  no explicit size gave a 652 × 262 bitmap.
- Theme resources resolve against the theme of the tree they are mounted in. That is why every
  stage carries its own `RequestedTheme`, and why the index's resource probe is mounted before it
  is read.
- First boot of the emulator takes about 2 min 20 s. A redeploy to a running emulator takes
  about 20 s.

## First run (2026-11-08, device 10, Update 1 720p 4.7")

`R:\cornflower-reference\refapp\20261108-0754-dev10\`: **504 captures, 0 errors**, 4 min 30 s
in the app and 5 min 21 s end to end on a warm VM with a cold emulator. Every forced state was
accepted.

| Read by the app | Value |
|---|---|
| Display | scale 180, window 400 × 711.11 logical px, raw DPI 309 × 312 |
| Page background | dark `#FF000000`, light `#FFFFFFFF` |
| `PhoneForegroundBrush` | dark `#FFFFFFFF`, light `#FF000000` |
| `PhoneAccentBrush` (the emulator's default accent) | `#FF3E65FF` in both themes |
| Device | Microsoft "Virtual", Windows Phone |
| Button, `Pressed` forced | Template root gets a `PlaneProjection` of −17.19° X and +17.19° Y (0.3 rad) with compensating offsets; the border fills with the accent. The visible border is 41.8 tall, inset 9.5 inside the 57.2 tall control |

Captures per control, both themes: TextBlock styles 28, Button 10, HyperlinkButton 10,
RepeatButton 10, ToggleButton 52, CheckBox 54, RadioButton 36, ToggleSwitch 60, Slider 6,
ProgressBar 24, ProgressRing 10, TextBox 16, PasswordBox 14, ComboBox 22, DatePicker 2,
TimePicker 2, AppBarButton 12, AppBarToggleButton 36, ListViewItem 84, ListView 2, Pivot 6, Hub 8. Since #113 the
catalogue also has ScrollViewer 16 and ScrollBar 42 ([the panning indicator](#scrollviewer-and-scrollbar-the-panning-indicator-2026-11-15-device-11-113-for-116)).

**What it doesn't reach yet:**
- **States on inner parts.** DatePicker, TimePicker and ListView declare no states on their own
  template root. Their states are on inner parts, such as the date picker's flyout button and
  the list's items, and the suite doesn't walk those yet. The list items are covered by the
  separate ListViewItem specimen. The Slider's template really has only Normal and Disabled.
- **Other profiles.** Devices 10, 11 and 12 have run (below). Run `-Device 13` for qHD 5", and so on.

## Runs by profile (2026-11-08)

All three: 504 captures, 0 errors, accent `#FF3E65FF`. The reference profile is device 11
([ADR 0001](../../docs/decisions/0001-what-faithful-means.md)).

| Device | Profile | Folder | `RawPixelsPerViewPixel` | View | `ResolutionScale` |
|---|---|---|---|---|---|
| 10 | U1 720P 4.7" | `20261108-0754-dev10` | 1.8 | 400 × 711.11 | 180 |
| 11 | U1 1080P 5.5" | `20261108-1443-dev11` | 2.4 | 450 × 800 | 180 |
| 12 | U1 1080P 6" | `20261108-1451-dev12` | 2.2 | 490.91 × 872.73 | 180 |

## Native readings (2026-11-11, devices 7 to 13)

`run.ps1 -Native`, once on each Update 1 profile, each from a cold emulator with no other emulator
running: 110 readings and 0 errors per run, about 2 minutes in the app. The folders, under
`R:\cornflower-reference\refapp\`, are named in the first table. All values are in view pixels
unless they say raw.

After review the app settles each tray change by polling instead of a fixed second (every
change settled in 200 to 300 ms), builds each reading apart so a failing one can't break the
file, and no longer reads the separate 3-item ComboBox. Run again on device 11
(`20261111-1839-dev11`) and device 13 (`20261111-1842-dev13`), 107 readings and 0 errors each,
it read every value below the same.

### Display

| Device | Profile | Folder | `RawPixelsPerViewPixel` | `RawDpiX` × `RawDpiY` | `LogicalDpi` | `ResolutionScale` | Window, portrait |
|---|---|---|---|---|---|---|---|
| 7 | U1 WVGA 4" 512MB | `20261111-1822-dev7` | 1.2 | 234 × 233 | 115.2 | 140 | 400 × 666.67 |
| 8 | U1 WVGA 4" | `20261111-1817-dev8` | 1.2 | 234 × 233 | 115.2 | 140 | 400 × 666.67 |
| 9 | U1 WXGA 4.5" | `20261111-1813-dev9` | 2 | 330 × 331 | 192 | 180 | 384 × 640 |
| 10 | U1 720P 4.7" | `20261111-1802-dev10` | 1.8 | 309 × 312 | 172.8 | 180 | 400 × 711.11 |
| **11** | U1 1080P 5.5" | `20261111-1759-dev11` | 2.4 | 403 × 399 | 230.4 | 180 | 450 × 800 |
| 12 | U1 1080P 6" | `20261111-1804-dev12` | 2.2 | 365 × 366 | 211.2 | 180 | 490.91 × 872.73 |
| 13 | U1 qHD 5" | `20261111-1809-dev13` | 1.2 | 221 × 219 | 115.2 | 140 | 450 × 800 |

- **WVGA runs at 1.2, not 1.0**: a 400 × 666.67 view. qHD runs at 1.2 too, with the reference's
  450 × 800 view; WXGA at 2.0, 384 × 640.
- **`ResolutionScale` is 140 on the three 1.2 profiles** and 180 on the rest, so it still isn't
  the scale. `LogicalDpi` is 96 × `RawPixelsPerViewPixel` on all seven.
- **The reference profile reads 403 × 399 dpi**, where the ToggleSwitch section above worked out
  about 400 from its diagonal: a view pixel there is 0.152 mm, as that section took it.
- Devices 7 and 8 differ only in memory, and read the same in everything below.

### The system tray

The app left the tray as it launched, then showed and hid it with `StatusBar.ShowAsync` and
`HideAsync`, under each bounds mode, in portrait, `Landscape` and `LandscapeFlipped`, and read
everything a second after each change. The tray is shown at launch: the first reading equals the
"shown" one. With the tray shown:

| Device | `OccludedRect`, portrait | raw px tall | `Landscape` | `LandscapeFlipped` | raw px wide |
|---|---|---|---|---|---|
| 7, 8 | 0, 0, 400 × 26.67 | 32 | 0, 0, 60 × 400 | 606.67, 0, 60 × 400 | 72 |
| 9 | 0, 0, 384 × 25.5 | 51 | 0, 0, 57.5 × 384 | 582.5, 0, 57.5 × 384 | 115 |
| 10 | 0, 0, 400 × 26.67 | 48 | 0, 0, 60 × 400 | 651.11, 0, 60 × 400 | 108 |
| **11** | 0, 0, 450 × 27.92 | 67 | 0, 0, 62.92 × 450 | 737.08, 0, 62.92 × 450 | 151 |
| 12 | 0, 0, 490.91 × 25.45 | 56 | 0, 0, 57.27 × 490.91 | 815.45, 0, 57.27 × 490.91 | 126 |
| 13 | 0, 0, 450 × 27.5 | 33 | 0, 0, 62.5 × 450 | 737.5, 0, 62.5 × 450 | 75 |

- **The tray is a band across the top in portrait, and down the side in landscape**: on the left
  in `Landscape`, on the right in `LandscapeFlipped`. In landscape it is more than twice as wide
  as it is tall in portrait.
- **Its size is per profile**, a whole number of raw pixels: 25.45 to 27.92 view pixels tall,
  neither a constant in view pixels nor in raw ones.
- **`BackgroundOpacity` is 0, and `BackgroundColor` and `ForegroundColor` are null** (unset), in
  every reading on every profile. Those are the app's own settings, which the app never sets;
  what colour the shell draws for the tray is not in the API.
- Hidden, `OccludedRect` is 0, 0, 0 × 0 in every orientation and mode.

### Visible bounds

`Window.Current.Bounds` and `CoreWindow.Bounds` are the whole screen, and don't change with the
tray or the bounds mode; they swap width and height in landscape. `ApplicationView.VisibleBounds`
is the window less the tray, in both bounds modes:

| Device | `Window.Current.Bounds` | `VisibleBounds`, tray shown, portrait | `Landscape` | `LandscapeFlipped` |
|---|---|---|---|---|
| 7, 8 | 400 × 666.67 | 0, 26.67, 400 × 640 | 60, 0, 606.67 × 400 | 0, 0, 606.67 × 400 |
| 9 | 384 × 640 | 0, 25.5, 384 × 614.5 | 57.5, 0, 582.5 × 384 | 0, 0, 582.5 × 384 |
| 10 | 400 × 711.11 | 0, 26.67, 400 × 684.44 | 60, 0, 651.11 × 400 | 0, 0, 651.11 × 400 |
| **11** | 450 × 800 | 0, 27.92, 450 × 772.08 | 62.92, 0, 737.08 × 450 | 0, 0, 737.08 × 450 |
| 12 | 490.91 × 872.73 | 0, 25.45, 490.91 × 847.27 | 57.27, 0, 815.45 × 490.91 | 0, 0, 815.45 × 490.91 |
| 13 | 450 × 800 | 0, 27.5, 450 × 772.5 | 62.5, 0, 737.5 × 450 | 0, 0, 737.5 × 450 |

With the tray hidden, `VisibleBounds` is the window, in every orientation and mode.

**Where the page goes.** The root `Frame` and the `Page` fill the window in every reading, tray
shown or not. What moves is the page's content (`Page.Content`, here the page's root `Grid`):

- **`UseVisible`, the default:** the content is laid out in `VisibleBounds` exactly, on all seven
  profiles and in all three orientations (on the reference, at 0, 27.92, 450 × 772.08). So the
  page itself, and its background, reach under the tray, and nothing of the content does.
- **`UseCoreWindow`:** the content fills the window, 0, 0, under the tray.

So with the tray shown, the phone lays no content under it unless the app asks for `UseCoreWindow`.
`ApplicationView.Orientation` reports `Landscape` for both landscapes; `DisplayInformation.CurrentOrientation`
tells them apart. The suite's captures are unaffected: they hide the tray, and the window and
`VisibleBounds` are then the same.

### `TemplateSettings`

Every property each type has (the winmd lists 4 on `ComboBoxTemplateSettings`, 7 on
ProgressBar's, 3 on ProgressRing's and 8 on ToggleSwitch's), dark. **No forced visual state
changes any value**: each specimen reads the same at rest and in every state its template declares,
on all seven profiles. Only opening a ComboBox, and the control's size, change them. Each is
listed as the phone returns it, rounded to 0.01.

**ComboBox** (`SelectedItemDirection` is `Top` in every reading):

| Device | `DropDownClosedHeight` | `DropDownOpenedHeight`, open, 3 / 4 / 5 items | `DropDownOffset`, closed, item 0 / 2 / 4 of 5 selected | Item step |
|---|---|---|---|---|
| 7, 8, 10, 13 | 27.17 | 155 / 206.67 / 258.33 | −8.5 / −111.83 / −215.17 | 51.67 |
| 9 | 27 | 154.5 / 206 / 257.5 | −8.5 / −111.5 / −214.5 | 51.5 |
| **11** | 27.17 | 155 / 206.67 / 258.33 | −8.5 / −111.83 / −215.17 | 51.67 |
| 12 | 27.32 | 155.45 / 207.27 / 259.09 | −8.5 / −112.14 / −215.77 | 51.82 |

- **`DropDownOpenedHeight` is 0 until the first open**, then the items' height: the item count
  times the step (51.67 on the reference). It keeps that value when the box closes again.
- **`DropDownOffset` is 0 while open**, and closed it is −8.5 less the selected index times the
  step: it lifts the items so the selected one sits in the closed box. The −8.5 is the probe's
  "`TranslateY` eases from −8.5 to 0" in [Flyouts, pickers and the dialog](#flyouts-pickers-and-the-dialog).
- The values are set as soon as `IsDropDownOpen` is set, before the next frame. The 3-item
  values are the catalogue's ComboBox, opened on its 140-tall stage; the first runs also read a
  separate 3-item box on a 400-tall stage, and it read the same, so the app no longer reads it.

**ProgressBar** (the catalogue's two, 40% and indeterminate, at the page's width less its margins):

| Device | Width | `ContainerAnimationStartPosition` | `ContainerAnimationEndPosition` | `EllipseAnimationWellPosition` | `EllipseAnimationEndPosition` | `EllipseDiameter` | `EllipseOffset` | `IndicatorLengthDelta`, 40% |
|---|---|---|---|---|---|---|---|---|
| 7, 8 | 361.67 | −63 | 94.41 | 120.56 | 241.11 | 6 | 9 | −144.67 |
| 9 | 346 | −63 | 87.59 | 115.33 | 230.67 | 6 | 9 | −138.4 |
| 10 | 362.22 | −63 | 94.65 | 120.74 | 241.48 | 6 | 9 | −144.89 |
| **11** | 412.08 | −63 | 116.35 | 137.36 | 274.72 | 6 | 9 | −164.83 |
| 12 | 452.73 | −63 | 134.04 | 150.91 | 301.82 | 6 | 9 | −181.09 |
| 13 | 411.67 | −63 | 116.17 | 137.22 | 274.44 | 6 | 9 | −164.67 |

On all seven: the well is a third of the width and the end two thirds; the container's travel,
end less start, is 0.43522 of the width; `IndicatorLengthDelta` is minus the indicator's length
(40% of the width), and 0 on the indeterminate bar, whose other values are the same.

**ProgressRing**: `EllipseDiameter`, `EllipseOffset`'s top (its other sides are 0) and
`MaxSideLength`, for the catalogue's ring (which stretches to its 120-tall stage's slot), a ring at
its default size, and one 120 square, the ring probe's two:

| Device | Catalogue's | Default size | 120 × 120 |
|---|---|---|---|
| 7, 8 | 361.67 × 108.33: 10.83, 43.33, 108.33 | 57.5 × 57.5: 5.75, 23, 57.5 | 12, 48, 120 |
| 9 | 346 × 108: 10.8, 43.2, 108 | 57.5 × 57.5: 5.75, 23, 57.5 | 12, 48, 120 |
| 10 | 362.22 × 107.78: 10.78, 43.11, 107.78 | 57.22 × 57.22: 5.72, 22.89, 57.22 | 12, 48, 120 |
| **11** | 412.08 × 107.92: 10.79, 43.17, 107.92 | 57.5 × 57.5: 5.75, 23, 57.5 | 12, 48, 120 |
| 12 | 452.73 × 108.18: 10.82, 43.27, 108.18 | 57.73 × 57.73: 5.77, 23.09, 57.73 | 12, 48, 120 |
| 13 | 411.67 × 108.33: 10.83, 43.33, 108.33 | 57.5 × 57.5: 5.75, 23, 57.5 | 12, 48, 120 |

`MaxSideLength` is the ring's smaller side, the diameter a tenth of it and the offset four tenths,
on all seven. The `SizeStates` (`Large`, `Small`) don't change them.

**ToggleSwitch** (the catalogue's three, off, on and without a header, read the same):

| Device | Width | `KnobOnToOffOffset` | `CurtainOnToOffOffset` |
|---|---|---|---|
| 7, 8, 13 | 361.67, 411.67 | 55.83 (67 raw) | 60.83 (73 raw) |
| 9 | 346 | 56.5 (113 raw) | 61 (122 raw) |
| 10 | 362.22 | 56.67 (102 raw) | 62.22 (112 raw) |
| **11** | 412.08 | 56.67 (136 raw) | 61.25 (147 raw) |
| 12 | 452.73 | 56.36 (124 raw) | 60 (132 raw) |

`KnobOffToOnOffset` and `CurtainOffToOnOffset` are the same negated. The four `Current` offsets
(`KnobCurrentToOnOffset`, `KnobCurrentToOffOffset`, `CurtainCurrentToOnOffset`,
`CurtainCurrentToOffOffset`) are 0 at rest, straight after `IsOn` is set from code and once the
slide is over. The knob's offset is a whole number of raw pixels, so it moves with the profile:
on the reference it is 56.67, where the template's `On` state sets 56.5 and the recording measured
a travel of 56.56; [the knob's slide](#toggleswitch-the-knobs-slide) says which is which. The
drag's halfway line is half of it: 28.33 on the reference, and between 27.77 and 28.04 on qHD, whose offset is
55.83 ([the hold limit, the distance and the halfway line](#toggleswitch-the-hold-limit-the-distance-and-the-halfway-line-2026-11-11-and-12-33)).

### A ContentPresenter outside a template, and a bare ContentControl (2026-11-15, device 11, #114, for #117)

`20261115-2048-dev11` (`-Only '^Content(Presenter|Control)$'`), dark and light. Each specimen sits in a 200 × 100 slot,
with a 60 × 30 rectangle as its child.

- **The phone's `ContentPresenter` style doesn't reach a ContentPresenter placed on a page.** The runtime's style sets
  `HorizontalAlignment` Left and `VerticalAlignment` Top, but a ContentPresenter in a page's Grid, outside any template,
  reads Stretch and Stretch. It fills its 200 × 100 slot and centres its child: the rectangle is at 70, 35. That is the
  same as with Stretch set (`page-stretch`).
- **A bare `ContentControl` doesn't draw its `Background`**: its template is a lone ContentPresenter. The dump has the
  Background (`#FF808080`), but the pixels under its padding are the page's (black dark, white light). Its presenter is
  Left and Top at the Padding: the child at 12.08, 12.08 (12, snapped to raw pixels), 60 × 30.

### What this didn't read

- **The `Current` offsets during a drag.** They are what a drag's release animates from, so they
  can only be non-zero mid-drag; reading them needs a touch probe (`toggleswitch` drags, but
  doesn't read them).
- **The tray's pixels.** Whether its background is opaque, and what colour, is the shell's
  drawing: the API reports only the app's own `BackgroundOpacity` (0). A `-Grab` screenshot
  with the tray shown would settle it.
- **Light theme.** The `TemplateSettings` were read in dark only; nothing in them is a colour.

## Popups on the reference profile (2026-11-08, device 11)

`20261108-1705-dev11` and `20261108-1734-dev11` (`-Popups only`): 44 popup dumps each, both themes. The open
popups, in the order `GetOpenPopups` returns them, with their window bounds in logical pixels:

| Popup | Open popups | Bounds | Chrome, dark / light |
|---|---|---|---|
| Flyout, default (top) | FlyoutPresenter | 0, 0, 450 × 78.33 | `#FF1F1F1F` / `#FFDDDDDD` |
| Flyout, full | FlyoutPresenter | 0, 0, 450 × 800 | same |
| MenuFlyout | a full-window Rectangle, then MenuFlyoutPresenter | menu at 0, 85, 450 × 193.33 | `#FF000000` / `#FFFFFFFF` |
| ContentDialog, both | a CommandBar, the ContentDialog, a `#99000000` Rectangle | 0, 0, 450 × 800 | `#FF1F1F1F` / `#FFDDDDDD` |
| ListPickerFlyout, PickerFlyout, DatePickerFlyout, TimePickerFlyout, ComboBox with 6 to 10 items | the presenter, then a CommandBar | 0, 0, 450 × 800 | `#FF1F1F1F` / `#FFDDDDDD` |
| ComboBox with 3 to 5 items | none: it opens inline, in the control | | |

A ComboBox with 6 items or more opens a `ListPickerFlyoutPresenter`: the full-screen picker.

**Where ComboBox flips to full screen** (2026-11-09, #25): 5 items open inline and 6 open the
full-screen picker, in dark and light, on the reference profile (device 11, `20261109-2039-dev11`)
and on the 720p one (device 10, `20261109-2041-dev10`). The two profiles differ in view size
(450 × 800 and 400 × 711) and scale, so the count, not the room on screen, decides it. The
inline ones report "no popup open", which is expected.

## Probes

Behaviour that only real input shows. `run.ps1 -Probe <name>` deploys the app with
one probe from `app\Probe.cs` instead of the suite. The app lays a control out on a dark page,
writes the touches it wants as `gestures.txt` (in view pixels, window coordinates) and then
`ready.txt`. `vm\session.ps1` pulls them and `vm\touch.ps1` plays them on the emulator's display
with the mouse, which the emulator turns into touches. The app logs every pointer event, visual
state change and watched value to `probe.txt`, in milliseconds, and writes `done.txt` when the
last touch has settled. `touch.log` says where each step landed on the VM's screen.

- **A touch that can't land stops the run before it starts.** Just before it writes
  `gestures.txt`, the app reads the view and checks each step is inside it, and that the topmost
  element under each press is the control it is for, or inside it. If any fails, it writes
  `gestures.txt` and a `done.txt` naming every such step by its line in `gestures.txt`, and no
  `ready.txt`, so nothing is played.
- **A run whose touches never arrive still ends with its log.** `ready.txt` gives the app's
  timeout: the touches' planned time and 3 minutes, at least 5 minutes (a tap-started probe: 9
  minutes). `touch.ps1` plays each step at its planned time from its start, so a late step doesn't
  delay the rest. If `touch.ps1` fails before its first step, `session.ps1` stops the recording
  and reports at once; if it fails mid-run, `session.ps1` stops the recording, then waits for the
  app's `done.txt`, up to a minute past that timeout, and pulls the log with it.

- **Touches move the VM's real cursor** (`-TouchMode real`, the default), so the emulator has to
  be in front and the VM's mouse left alone during the run. `-TouchMode post` posts mouse
  messages to the display window instead; the emulator ignored them (2026-11-09).
- **One emulator at a time is safest.** `touch.ps1` picks the emulator by its XDE process's
  `/displayName`, because every emulator window is titled "XDE".
- **Resolution:** at the zoom that fits the VM's 1440-high screen, one screen pixel is 0.83 view
  pixels on the reference profile, so a probe can't place a touch finer than that. The app logs
  the positions it got.
- **A still touch reports every 100 ms; a moving one, every 15 to 30 ms.** While a touch is held the emulator
  sends its position every 100 ms from the press, moved or not, and now and then drops one; the mouse's own moves
  reach the phone 15 to 30 ms apart (the logs of #33). So a probe reads a position, not a smooth speed, and a press
  is seen by whatever reads pointer events on that 100 ms beat: the ToggleSwitch's hold limit is one
  ([the hold limit](#toggleswitch-the-hold-limit-the-distance-and-the-halfway-line-2026-11-11-and-12-33)).
  Press and release times are kept.

Probes: `toggleswitch` (#25), `toggletap` and `togglehalf` (#33), `tilt`, `tiltedge` and `tiltgrid` (#22), and for #24 `motion`, `pages`,
`projections` and `ring` ([Motion](#motion-2026-11-10-device-11-24)), and `ringsize` for #28 (B6:
[the size threshold and the clock](#progressring-the-size-threshold-and-the-clock)), and for #113 `scroll`, `scrollend` and
`listpress` ([Scrolling](#scrolling-how-the-scroll-probes-run-2026-11-15-device-11-113)), and for #114 `turnstile`
([its forward start](#the-turnstiles-forward-start-2026-11-15-device-11-114)), `pivot`
([Pivot's header strip](#pivots-header-strip-2026-11-15-device-11-114)) and `hub` ([Hub's header](#hubs-header-2026-11-15-device-11-114)), and for #136 `pivotwide` and `pivottwo` ([wide headers and two items](#pivot-wide-headers-two-items-and-a-tap-on-a-header-cut-by-the-edge-2026-11-15-device-11-136)), `hubimage` and `hubimage900` ([a background image](#hub-a-background-image-and-the-end-of-the-sections-2026-11-15-and-16-device-11-136)), `listcheck` ([the check mode](#listview-the-check-mode-extended-and-none-and-a-selection-set-from-code-2026-11-15-device-11-136)) and `appbar` ([the app bar under touches](#the-app-bar-under-touches-and-with-other-numbers-of-secondary-commands-2026-11-15-and-16-device-11-136)). Each frame's value is read on the UI thread in
`CompositionTarget.Rendering`. That reads the animated values of the theme animations too: the
tilt's `PlaneProjection` moves every 15 ms in the log.

### ToggleSwitch: tap or drag (2026-11-09, device 11, #25)

Five runs, `20261109-2105-dev11`, `-2108`, `-2112`, `-2115` and `-2128`, 127 touches, each from
Off, pressing the knob and moving right. In the last, `touch.ps1` kept each step to its planned
time within a few milliseconds; before, its steps ran late, but the app logs the times it got,
and those are what is quoted. In view pixels on the reference profile:

- **Pressing goes straight to `Dragging`**: the Thumb's `DragStarted` fires on the press, and the
  knob's `KnobTranslateTransform.X` follows the finger one for one from the first move. There is
  no distance before the knob moves.
- **A release is a tap, and toggles, when the press moved less than about 18 view px and lasted
  less than about 1,020 ms** (later runs: a physical 2.7 mm, and 1,000 ms checked at each pointer event; see
  [the next section](#toggleswitch-the-hold-limit-the-distance-and-the-halfway-line-2026-11-11-and-12-33)). Moved 17.47: tap; 18.33: not, in two runs. Lasted 993, 1,001
  and 1,022 ms: tap; 1,020, 1,021, 1,033 and 1,060: not. The two limits act on their own: a still press
  of 1,124 ms doesn't toggle, nor does a quick 20 px drag of 548 ms.
- **Otherwise the knob's position decides**: released at 28.32 it goes back Off, at 29.18 it goes
  On. The line is 28.33, not half the template's `On` value, 56.5 (28.25), which 28.32 passes and
  still goes back Off. On the 720p profile 28.34 goes On (below): the two profiles' releases fall
  either side of 28.33, within 0.01. 28.33 is half the knob's offset as the control computes it,
  `KnobOnToOffOffset` 56.67 ([Native readings](#templatesettings)), but both profiles have that
  offset, so these runs can't tell half the offset from a fixed 28.33. The qHD profile, offset 55.83, did: it is
  half the offset (next section).
- **After a tap**, `ToggleStates` goes `Dragging` > `Off` at once, and `Off` > `On` about 380 ms
  later. After a drag, `Dragging` > `On` or `Off` comes about 380 ms after the release. That is
  the knob's slide into place.
- **On the 720p profile** (device 10, 1.8 raw px per view px, `20261110-0733-dev10`, the same 32
  touches) the limits are the same in view pixels: moved 17.76 taps and 18.85 doesn't (17.47 and
  18.33 on the reference); held 984 ms taps and 1,018 doesn't; released at 27.19 goes back Off and
  at 28.34 goes On; the slide still ends about 380 ms after the release. So the 18 px limit isn't
  in raw pixels (32.0 to 33.9 raw here, 41.9 to 44.0 on the reference). In millimetres it is 2.60
  to 2.76 here (a view pixel is 0.147 mm at 312 dpi) and 2.66 to 2.79 on the reference (0.152 mm,
  about 400 dpi from its 5.5 inch diagonal): the two profiles' view pixels differ by 4%, which a
  0.83 px step can't separate, so view pixels and a physical distance both fit. View pixels are
  the simpler reading. **Later runs settle it, and the hold limit too: next section.**

### ToggleSwitch: the hold limit, the distance and the halfway line (2026-11-11 and 12, #33)

Four runs, each from a cold emulator or a warm one with no other running; `tools/motion/toggle.py` prints every
number below from them. From #33 the probes also log each pointer event's `PointerPoint.Timestamp` (`ts`, in
microseconds) and the switch's gesture events (`Tapped`, `Holding`, `RightTapped`), and every ToggleSwitch probe logs
the knob's offsets from `TemplateSettings`. Times are the app's stopwatch unless they say `ts`.

| Run | Probe | Device | What |
|---|---|---|---|
| `20261111-2345-dev11` | `toggletap` | 11 | 24 presses: still, with one move of a view pixel at 905 to 975 ms, lifted 20 ms later; still, lifted at 1,050 and 1,150 ms; moved 17 and 19 px |
| `20261112-0003-dev13` | `toggleswitch` | 13, qHD, offset 55.83 | 35 presses: the grid of #25, drags about half the offset, the limits' sides |
| `20261111-2350-dev11` | `togglehalf` | 11, offset 56.67 | 34 drags of a switch drawn three times its size in a `Viewbox` |
| `20261112-0007-dev13` | `togglehalf` | 13, offset 55.83 | the same 34 |

- **A tap is the `Tapped` gesture.** In all four runs, every press that toggled raised `Tapped` on its release, and no
  press that didn't toggle raised it (127 presses, on two profiles).
- **The hold limit is 1,000 ms, by the pointer's timestamps, and it is checked when a pointer event arrives, not by a
  timer.** A press is a hold, and raises `Holding` instead of `Tapped`, at the first pointer event during the press
  whose timestamp is 1,000 ms or more after the press's; the release itself doesn't count. The timestamps step by
  15.625 ms (the system clock's tick): on `toggletap` the latest event that still tapped was 984 ms after the press
  by `ts` (63 ticks; 971.6 ms by the stopwatch), and every event at 1,000 ms (64 ticks; 988.1 ms and later by the
  stopwatch) started `Holding` at once, even with the release 1 ms later. The emulator sends a still touch's
  position every 100 ms from the press, so a press is held past its 1,000 ms event, and sometimes it drops one:
  that is what made #25's limit look scattered round 1,020 ms. Over the eleven `toggleswitch` and `toggletap` runs on
  devices 10, 11 and 13 (#25's and these), every press whose last event before the release came by 984 ms (by `ts`) or 901 ms (by the
  stopwatch, where there is no `ts`) toggled, and none with one at 999 ms or later did, however long the press: a
  still press of 1,071 ms toggled (`20261111-1942-dev11`) because its 1,000 ms event never came, and one of 1,020
  didn't because it did. On a real phone the digitiser reports the touch all along, so the limit there would be
  1,000 ms itself; that isn't checked (no device, gap register B24).
- **The distance limit is a physical distance, about 2.7 mm**, not view pixels: on the qHD profile (device 13, 221
  dpi, 1.2 raw px a view px, so 0.138 mm a view px) presses moved 18.36 and 19.16 view px tapped and 19.96 didn't,
  where 18.33 didn't on the reference. In millimetres from each profile's `RawDpiX`, the farthest taps are 2.64
  (device 13, 23 raw px), 2.64 (device 11) and 2.63 mm (device 10), and the nearest misses 2.75, 2.77 and 2.79 mm.
  A press that moves 12.48 view px still becomes a hold at its 1,000 ms event; 19.96 doesn't (no `Holding`).
- **The halfway line is half the knob's offset**, `KnobOnToOffOffset` / 2, not a fixed 28.33. A touch moves in
  whole screen pixels, 0.83 view px on these profiles, which can't put a release between 27.92 and 28.33; so
  `togglehalf` draws the switch three times its size in a `Viewbox`, where the knob follows the finger in the
  switch's own pixels, a third as far, on a 0.28 px grid. The log reads the scale (the knob 3 times as wide on
  screen as in the switch) and the offset in the switch's pixels, the same as unscaled. On device 13 (offset 55.83,
  half 27.92) the knob released at 27.77 went back Off, 6 times of 6, and at 28.04 went On, 6 of 6. On device 11
  (offset 56.67, half 28.33) 28.06 went back Off, 4 of 4, and 28.35 went On, 6 of 6, as the unscaled runs had it
  (28.32 Off, and 28.34 On on device 10). So 28.04 goes On where the offset is 55.83 and 28.06 goes back Off where it is
  56.67. Unscaled on device 13 (`toggleswitch`): 27.51 Off and 28.31 On, which also puts the line below device 11's
  (28.32 Off there).
- The slide after the release still ends about 380 ms after it (374 to 412 ms on these runs).

### Tilt: first readings (2026-11-09, device 11, for #22)

`20261109-2118-dev11`: a Button 300 × 120 view px, pressed and held for 600 ms at its centre, at
each corner and at the middle of each edge, 12 px in, then pressed at the centre and dragged 200
px down. The template root's `PlaneProjection`, read every frame:

| Pressed at (in the button) | `RotationX` | `RotationY` | `GlobalOffsetZ` |
|---|---|---|---|
| centre, 150, 60 | −0.38 | 0 | −24.45 |
| top middle, 150, 11.7 | −7.44 | 0 | −14.17 |
| bottom middle, 150, 108.3 | 6.79 | 0 | −15.12 |
| left middle, 11.7, 60 | −0.51 | 8.27 | −12.23 |
| right middle, 288.3, 60 | −0.51 | −8.27 | −12.23 |
| top left, 11.7, 11.7 | −7.65 | 8.30 | −1.80 |
| top right, 288.3, 11.7 | −7.65 | −8.30 | −1.80 |
| bottom left, 11.7, 108.3 | 6.71 | 8.28 | −3.19 |
| bottom right, 288.3, 108.3 | 6.71 | −8.28 | −3.19 |

`CenterOfRotation` is 0.5, 0.5, 0 throughout, and the root also gets a `CompositeTransform`
translating 112 down with a `GlobalOffsetY` of −112 against it. The press seemed to reach its
values in about 45 ms (three frames); the grid run below reads it closer, as a jump (see its
Timing). On release nothing moves for about 180 ms, then the tilt eases back to rest by about
345 ms, by the stopwatch ([in frames](#tilt-the-model-2026-11-09-device-11-for-22), a 182.6 ms
hold). Dragged after the press, the tilt follows the finger. These are readings, not yet the
model: #22 fits them (next section).

### Tilt: the model (2026-11-09, device 11, for #22)

`20261109-2307-dev11` (`-Probe tiltgrid`): four Buttons on one page, A 120 × 60, B 300 × 120,
C 412 × 80 and D 200 × 200 view px, at 19 px in from the left and 120, 210, 360 and 470 px down.
Each was pressed and held still for 500 ms at a 5 × 5 grid of points, at 5%, 25%, 50%, 75% and
95% across and down: 100 presses. With `dx`, `dy` the press point's offset from the button's
centre as a fraction of its width and height (−0.5 to 0.5):

| Value | Rule | Fit |
|---|---|---|
| `RotationY` | −g(`dx`): a press left of centre tilts positive | rms 0.06° once each button's offset (the bias below) is taken out; against the raw readings rms 0.17°, worst 0.41° (button D, pressed 5% across and 95% down) |
| `RotationX` | g(`dy`): a press above centre tilts negative | rms 0.10° once each button's offset is taken out; against the raw readings rms 0.51°, worst 1.26° (button A, pressed at its centre; A's bias, −0.83° below, is two thirds of it) |
| g(d) | 17.19° × d × (1 + 0.08 \|d\|); 17.19° is 0.3 rad | both axes over the four sizes: rms 0.08° with the offsets taken out, 0.38° raw |
| `GlobalOffsetZ` | −25 × (1 − (\|`RotationX`\| + \|`RotationY`\|) / 17.19°), in view px | from the angles read: worst residual 0.002 px over 100 presses, with 17.19° as 0.3 rad exactly |
| `CenterOfRotation` | 0.5, 0.5, 0 | always |
| `RenderTransform` | a `CompositeTransform` translating by 0.8 × (the screen's centre − the button's centre), with `GlobalOffsetX` and `GlobalOffsetY` the opposite | exact for all four buttons and the first run's, and on the 720p profile (`20261110-0738-dev10`, `-Probe tilt`: translate −20, 76.44) |

- **Size doesn't matter.** The angles depend on the fractions `dx` and `dy`, not on pixels: a
  press 25% from the centre gives 4.33° to 4.45° on all four buttons. Each axis is independent:
  a corner press gives the same `RotationY` as a press at the middle of that side.
- **The depth is the angles' complement.** The constants are the Silverlight toolkit
  TiltEffect's, a 0.3 rad greatest angle and a 25 px greatest depth, and its rule: a centre
  press sinks fully and tilts nothing, and a corner press tilts fully and hardly sinks. The
  angles run a little above the toolkit's straight line, by the 0.08 term.
- **Where the button is on screen adds a little.** Measured by pressing points on opposite sides
  of the centre, the angles carry a bias that grows with the button's distance from the screen's
  centre, as if the press were a little further from it:

  | Button (centre, view px) | `RotationX` bias | `RotationY` bias |
  |---|---|---|
  | A (79, 150), far up and left | −0.83° | +0.25° |
  | B (169, 270) | −0.35° | +0.07° |
  | C (225, 400), at the screen's centre | 0.00° | 0.00° |
  | D (119, 570), below and left | +0.28° | +0.18° |

  It is under 1° everywhere measured, and goes with the perspective translation above. #65 models
  it (an eye 1,055 view px away, placed 0.8 of the way from the control's centre to the screen's);
  a test of the rule above alone needs a tolerance of about 1.3° away from the screen's centre, or
  the bias taken out.
- **Timing.** The press is a jump: the first frame after the press already shows 96% to 100% of
  the held values (99% on average, at 30 to 40 ms, the first frame the emulator draws). On release
  the values hold for about 180 ms, then ease back to rest over about 195 ms by the stopwatch: a
  sine ease-out fits 1,199 frames with an rms of 0.03 of the full value (quadratic 0.036, linear
  0.037, cubic 0.059). Every press was within 1% of rest by 335 to 372 ms. In frames, below, it is
  a 182.6 ms hold and a `KeySpline` over 167.0 ms.
- **The release in frames** (re-read 2026-11-11, `tools/motion/tiltrelease.py`, all five tilt runs,
  252 releases on both profiles). Every release reads the same fraction of its held tilt at each
  frame, within 0.0002, whatever its frames' spacing by the stopwatch. So the return moves on
  animation time, 15.625 ms a frame
  ([the clock](#progressring-the-size-threshold-and-the-clock)): 0.9412, 0.8039, 0.6885, 0.5832,
  0.4845, 0.3913, 0.3030, 0.2210, 0.1431, 0.0737, 0.0163, then rest. The probe logs only changes,
  so the hold's frames aren't in the log. The hold is a stopwatch count: the first change comes
  12.4 frames' spacing after the release on average (each press's time from the release over its
  own return's frame spacing; 11.95 to 12.90 for 90% of the presses), so on the 13th frame. With
  the first frame after the release as time 0, as the first frame logged after the ProgressRing's
  `Active` storyboard began read storyboard time 0 (`20261111-2316-dev11`), that frame is at
  187.5 ms of animation time. Fitted to the eleven frames from there, a sine is out by up to 0.027
  (hold 179.2 ms, return 202.6 ms). A `KeySpline` 0.1,0.25 0.75,0.9 fits within 0.00075 (rms
  0.00024, about the log's rounding): **a hold of 182.6 ms, then 167.0 ms down the spline**, at
  rest by 349.6 ms. A free spline, 0.117,0.272 0.769,0.912 over 182.5 and 167.3 ms, fits no closer.
  Open by one frame: were time 0 the second frame after the release, the hold would be 167.0 ms.
- **A tap at the very edge clicks** (`-Probe tiltedge`, `20261110-0822-dev11`): a Button 300 × 120,
  tapped for 150 ms at the middle of each edge, 0.84, 1.7 and 3.35 view px inside it (the nearest the
  cursor gets to 1, 2 and 3), three times each: all 36 taps clicked. The tilt sinks the edge away
  from the finger, but the phone still counts the press as on the button.
- **On the 720p profile** (`20261110-0851-dev10`, `-Probe tiltgrid`, C narrowed to 362 to fit the
  400 wide view; 100 presses, no errors): the same rule fits within 0.08° once each button's offset
  is taken out (0.33° raw, worst 1.04° on `RotationX`, button A at its centre, of which A's bias
  there is −0.72°; 0.36° on `RotationY`), the depth within 0.002 px. The offsets are the bias again, for the buttons' places on a smaller
  screen (A −0.72° and D +0.35°, both on `RotationX`): #65 can check
  whether its eye distance scales with the screen.

### Scrolling: how the scroll probes run (2026-11-15, device 11, #113)

`scroll`, `scrollend` and `listpress` (`app\Probe.Scroll.cs`; gap register B8) read what scrolling does. A ScrollViewer
takes a touch over once it pans: the touch's pointer events end in `PointerCaptureLost`, not `PointerReleased`, so these
probes don't count releases. They run on a schedule. The first touch is a tap on a 60 px header band, which flashes
white for 250 ms (the mark that pairs a recording with the log). Each later touch is planned at a known time after it,
and the app sets its starting offset from code 1.2 s before it is due. Every frame the log has the frame's
`RenderingTime` with the ScrollViewer's `VerticalOffset` and its ScrollBar's indicator, and each `ViewChanging`
(`NextView`, `FinalView`, `IsInertial`) and `ViewChanged`. [`tools/motion`](../motion/README.md)'s `scroll.py`,
`scrollend.py` and `listpress.py` print every number below from the runs.

- **`RenderingTime` is the animation clock.** It moves by whole 15.625 ms steps from frame to frame, so the inertia
  below is timed in animation time, frames counted ([the clock](#progressring-the-size-threshold-and-the-clock)).
- **`touch.ps1` now asks Windows for a 1 ms timer.** Its waits were sleeps of a 15.6 ms tick, so a flick's moves, 10 ms
  apart, bunched up. Since then each step lands within about a millisecond of its plan.
- **The fastest flicks don't arrive.** A 240 px drag in 40 ms reached the phone as a press and a release (a tap) in 2 of
  3 tries, and in 60 ms in 1 of 3: the emulator passes the mouse's moves on 15 to 30 ms apart. 60 ms is the fastest
  flick read.
- **The emulator can lose its VM.** On the first try the touches never arrived: XDE showed "The connection to the
  virtual machine is lost". Turning the emulator off and booting it again cured it. Look at the VM's desktop when a
  probe's start tap never comes.

### ScrollViewer: deceleration (2026-11-15, device 11, #113)

`20261115-1938-dev11` (`-Probe scroll`). A ScrollViewer 740 view px tall over 100,000 px of content, flicked up from
offset 2,000: the finger moved 240 px in 60, 80, 120, 200 and 400 ms and 120 px in 400 ms, three times each. The content
is a Grid of bars, which has no snap points. Times are in ms of animation time, from the frame before the first inertial
frame (the release's frame) to the first frame at the end. A frame is logged only when something on it changed, so the
release's frame is taken as one tick before the first inertial one, at the offset last logged.

| Flick | Speed at the start (px/ms) | Distance (view px) | Time (ms) |
|---|---|---|---|
| 240 px in 60 ms (2 of 3) | 3.27 | 2,219.7 and 2,219.8 | 2,125 and 2,141 |
| 240 px in 80 ms | 3.40 to 3.54 | 1,934 to 2,055 | 2,141 |
| 240 px in 120 ms | 1.88 to 2.27 | 1,141 to 1,385 | 2,062 to 2,094 |
| 240 px in 200 ms | 0.90 to 1.05 | 549 to 647 | 1,609 to 1,719 |
| 240 px in 400 ms | 0.54 to 0.55 | 307 to 311 | 1,281 |
| 120 px in 400 ms | 0.30 to 0.33 | 123 to 146 | 812 to 891 |

The speed at the start is the `a + c v²` fit's (below).

- **Where it stops is decided at the release.** In every flick the first inertial `ViewChanging`'s `FinalView` is
  exactly the offset it ends on.
- **It slows smoothly to a stop**, with no overshoot and no bounce.
- **Time levels off at about 2.1 s, and distance at about 2,220 px.** Every flick that travelled more than about
  1,100 px took 2,062 to 2,141 ms. Both 60 ms flicks went 2,219.7 px, as if the start speed were capped.
- **No one simple law fits every speed:**
  - An exponential decay (`offset = end − d e^(−t/τ)`, its end fitted too) is out by rms 1.9 to 3.2 px on the slow
    flicks and 6 to 13 px on the fast ones, with τ from 440 to 760 ms, longer the faster. Its end lies 30 to 185 px
    past where the scroll really stops: the phone's comes to a stop, and an exponential never does.
  - A constant deceleration fits the slow flicks. For 120 px in 400 ms: 0.000363 px/ms² (363 view px/s²), rms 0.29 to
    0.40 px. For 240 px in 400 ms: 0.00040 px/ms², rms 1.5 px. It gets worse as the flick gets faster: rms 5.7 to
    7.9 px at 200 ms, 21 to 26 px at 120 ms, 36 to 44 px at 60 and 80 ms. A fast flick sheds more speed early on.
  - A constant deceleration plus a quadratic drag (`dv/dt = −a − c v²`) fits each flick on its own: rms 0.2 to 3.6 px. But its constants move with the speed: `a` from 0.00029 to 0.0007 px/ms², `c` from 0 (the
    slowest flicks) to 0.0019 /px. One `a` and `c` for every flick (0.000529 px/ms² and 0.000679 /px; `scroll.py
    --shared`) is out by rms 11.3 px over them all, and by up to 20 px rms on a fast one.
  - So the inertia is close to a constant deceleration of about 0.00036 px/ms² for slow flicks. Faster flicks stop
    sooner than that would give, and the curve's shape changes with the speed. The table and the per-flick fits are
    what to build or test against. Whether to build it as a part or record a deviation is #116's call.
- **Snap points.** The phone's ScrollViewer reads `VerticalSnapPointsType` `Optional` and `VerticalSnapPointsAlignment`
  `Near` (the property defaults; the style sets neither). A StackPanel gives snap points at its children. The first run,
  `20261115-1933-dev11`, scrolled a StackPanel of 100 px rows, and every flick ended on a multiple of 100. The last four
  flicks of `20261115-1938-dev11` are on a StackPanel again: they ended on 3,600, 3,500, 2,600 and 2,500, and otherwise
  moved as on the Grid (the 120 ms flicks went 1,383 and 1,296 px in 2,094 ms). On the Grid, flicks ended anywhere
  (4,322, 4,382, 2,753 and so on).
- A drag held still for 500 ms before the release raises an inertial `ViewChanging` too, but moves 0.04 px.

### ScrollViewer: the end of the content (2026-11-15, device 11, #113)

`20261115-1946-dev11` (`-Probe scrollend -Record`): the same ScrollViewer over 2,000 px of bars 100 view px apart
(1,260 px of scrolling). Pulled past the top by 100, 200, 300 and 400 px in 500 ms and held there 1.5 s; pulled 200 px
past the bottom; flicked (240 px in 60 ms) into the top and into the bottom, twice each.

- **The app sees nothing of it.** During a pull past the end there is no `ViewChanging`, no change of offset, and no
  indicator state change. DirectManipulation draws it. `scrollend.py` reads it off the recording: the bars' spacing and
  positions in a 12 px strip of each frame, paired with the log by the header's flash.
- **The content is squashed along the scroll axis, about a point about 1,550 view px past the pulled edge.** Each frame
  is the content at rest scaled by `s` about that point: 1,528 to 1,557 view px past the edge in all 9 touches, top and
  bottom. So the edge moves in by (1 − s) × 1,550 px. What sets 1,550 isn't read: it is neither the content's length
  nor the viewport's (740).
- **The squash has a limit, `s` = 0.910**, with the edge 139 view px in. A pull of 100 px held still reached `s` 0.959
  (the edge 62.7 px in), 200 px 0.913 (135.0 px), and 300 and 400 px 0.910 (139.1 px). The bottom pull of 200 px reached
  0.913. Every flick into either end reached 0.910.
- **Let go, it springs back in about 200 ms**: a held pull is at rest 185 to 240 ms after its release. A flick into the
  end squashes to 0.910 in about 130 ms, holds about 100 ms, and is back in about 130 ms more: at rest 570 to 680 ms
  after the press.
- These times are by the recording's clock (frames about 33 ms apart), paired with the log by the flash, so each is good
  to about 35 ms. Which clock they are on is open, as for the other recordings
  ([Motion](#motion-2026-11-10-device-11-24)).
- In the app, a flick into the top ends with `ViewChanging` at 0. `ViewChanged` with `IsIntermediate` false comes
  about 400 ms (stopwatch) later, as the squash ends.

### ScrollViewer and ScrollBar: the panning indicator (2026-11-15, device 11, #113, for #116)

The catalogue now has `ScrollViewer` (`rest` and `offset-200`: 280 tall over 14 lines, 560 px) and `ScrollBar`
(`vertical`, `horizontal` and `no-indicator`: 200 long, `Maximum` 100, `ViewportSize` 50, `Value` 25), each at rest and
in every state its template declares, dark and light: `20261115-1921-dev11` (`-Only '^Scroll'`, 58 captures, 0 errors).
The `scrollend` recording shows the indicator in a ScrollViewer, dark and light (`20261115-1946-dev11`).

- **`RenderTargetBitmap` doesn't draw a ScrollViewer's own indicator**, in any state, though the dump has it at full
  opacity. Forcing the ScrollViewer's `ScrollingIndicatorStates` to `TouchIndicator`, which sets the ScrollBar's
  `IndicatorMode`, doesn't help. A standalone ScrollBar does draw: its PNGs are the indicator's pixels. The recording is
  the ScrollViewer's.
- **Size.** The thumb is a Rectangle with `Width` 2.4 (`ScrollBarPanningThumbThemeWidth`), which lays out as 2.5 view
  px (6 raw px). It is centred in a ScrollBar 7.083 view px wide (`MinWidth` 7, so 17 raw px). The bar is flush with the
  ScrollViewer's right edge, so the thumb is 2.083 px in from the bar's left and 2.5 px in from the right edge. The
  recording agrees: 3 px at 1.2 a view px, its right edge 3 px from the screen's.
  - The ScrollBar's code sets the thumb's `Height` to its length × viewport ÷ extent. The vertical ScrollBar: 200 × 50
    ÷ 150 = 66.7, laid out 67.08 (161 raw px). The ScrollViewer: 280 × 280 ÷ 560 = 140. The recording: 740 × 740 ÷
    2,000 = 273.8, recorded as 275 px.
  - The code sets its `Margin`'s top to the offset ÷ the scrollable length × (the bar's length − the thumb's): 33.25 for
    `Value` 25; 100 at offset 200.
  - The thumb's `MinHeight` is 7 (`ScrollBarMinThemeHeight`), laid out 7.083: its length over 100,000 px of content.
  - The horizontal ScrollBar is the same turned through 90°. The ends are square.
- **Colour.** `ScrollBarPanningBackgroundThemeBrush` is `PhoneBaseLowColor`: `#73FFFFFF` dark, `#40000000` light. On
  the stage it is 115 grey over black and 191 over white, in the standalone PNGs and the recordings alike. The
  `Disabled` state halves the root's opacity (58 over black).
- **When it shows.** The ScrollBar's `IndicatorMode`, and its `ScrollingIndicatorStates` with it, becomes
  `TouchIndicator` 35 to 105 ms after the press (stopwatch), as the pan starts.
  - It goes back to `None` 75 to 180 ms after the inertia ends (the median about 130). It also goes back while a finger
    is held still: about 115 ms after the movement stops (the held drag's `None` at +515 ms, its moves ending at +400).
  - A `ChangeView` from code, even with `disableAnimation`, shows it for about 180 ms.
  - A pull past the end at offset 0 doesn't show it.
  - The ScrollViewer's own `ScrollingIndicatorStates` goes back to `TouchIndicator` about every 135 ms while it
    scrolls, and to `NoIndicator` about 50 ms after the ScrollBar's (the template's 0.05 s keyframe).
- **The fades.** The ScrollBar's `TouchIndicator` and `NoIndicator` states run `FadeInThemeAnimation` and
  `FadeOutThemeAnimation` on `VerticalPanningRoot`. The app reads its opacity as 1 throughout: the fades are only in the
  recording.
  - Fade in: the first frame shows it at 27% (31 of 115), about 100 ms after the state change by the log. It then
    climbs almost evenly, a frame (33 ms) apart: 37, 42, 51, 60, 69, 81, 88, 94, 99 and 100%. That is about 2.7 a
    second, so 0 to 1 in about 370 ms, starting at the state change.
  - Fade out: it starts with the `NoIndicator` state, within the recording's 35 ms. It falls almost evenly: 80, 65, 51,
    39 and 21% over 200 ms, about 2.95 a second, so about 340 ms from full to gone.
  - Shown only briefly (a `ChangeView`, a flick into the end), it fades out from wherever it had got to: 55 to 65%.
  - These are by the recording's clock, which is open, as above.
- `ScrollViewer.VerticalSnapPointsType` is `Optional` (above); `IsScrollInertiaEnabled` and `IsVerticalRailEnabled` are
  true.

### ListView: an item's press (2026-11-15, device 11, #113)

`20261115-1941-dev11` and `20261115-1948-dev11` (`-Probe listpress`): a ListView with `IsItemClickEnabled`, each item a
TextBlock in `ListViewItemTextBlockStyle`. The second run has 40 items, so that the list pans. Each item is 412.08 ×
39.17 view px. The template's `Pressed` state runs a `PointerDownThemeAnimation` on `TiltContainer`, and its `Pressed`
to `Normal` transition a `PointerUpThemeAnimation`: the tilt.

- **No delay.** `CommonStates` goes `Normal` > `Pressed` within 0.3 to 2.6 ms of `PointerPressed`, in the same input
  dispatch, on every press. It doesn't wait to tell a tap from a pan.
- **The press, frame by frame.** The first frame that changes, 30 to 45 ms after the press, shows the depth with almost
  no angle: at the centre, `GlobalOffsetZ` −24.8 and `RotationX` −0.1 to +0.3°. The next frame shows the angle a little
  past its held value (−2.38° where it holds −2.13°; −8.20° where it holds −8.11°). The one after settles.
- **The tilt is the Button's** ([the model](#tilt-the-model-2026-11-09-device-11-for-22)):
  - `RotationY`: +8.09 and −8.10° at 5% and 95% across, +4.40 and −4.42° at 25% and 75%, where the Button's rule gives
    ±8.01° and ±4.37°.
  - `GlobalOffsetZ` = −25 × (1 − (|`RotationX`| + |`RotationY`|) / 17.19°) exactly.
  - `RotationX` carries the screen-position bias: −1.5 to −2.1° at the centres of items 1 to 3, which sit 119 to 236
    view px down, well above the screen's centre. At 10% and 90% down item 2 it is −8.11° and +5.57°: ±6.84° about a bias
    of −1.27°, against the rule's ±6.97°. The bias is larger than the Button's at the same height on screen because
    the angle goes with the press's fraction of the item's height, and an item is short.
  - `CenterOfRotation` is 0.5, 0.5. As on the Button, the press gives `TiltContainer` a `CompositeTransform`
    translating it by 0.8 × (the screen's centre − the item's centre), with `GlobalOffsetX` and `GlobalOffsetY` the
    opposite: item 3, centred at 225.2, 217.1, gets −0.167, 146.333.
- **The release is the Button's, to four places.** After every release the tilt holds, then returns through the same
  fractions of the held tilt, one a frame: 0.9412, 0.8039, 0.6885, 0.5832, 0.4845, 0.3913, 0.3030, 0.2210, 0.1431,
  0.0737, 0.0163, then rest. The first change comes 182 to 190 ms after the release, and rest 352 to 371 ms after it,
  by the stopwatch. In animation time that is the Button's hold of 182.6 ms, then a `KeySpline` over 167.0 ms
  ([in frames](#tilt-the-model-2026-11-09-device-11-for-22)). `Pressed` > `Normal` comes at the end of the return.
- **A tap** (80 or 150 ms) presses and returns the same way. `ItemClick` and `Tapped` come with the release.
- **A pan cancels the press and runs the release.** A press held 300 ms and then dragged 80 px up (the list pans) or
  down (past the top) loses its pointer capture about 50 ms after the move starts. The tilt holds where it was, then
  returns as after a release, timed from the capture lost. There is no `ItemClick` and no `Tapped`. A flick loses capture
  30 to 60 ms after the press, sometimes before the tilt has shown more than its first frame.
- **Dragged off the item** where the list can't pan (the first run's 12 items), the tilt follows the finger while it is
  on the item, and returns about 190 ms after the finger leaves it, with no `ItemClick`.
- **A press moved 10 px sideways** is still a tap (`ItemClick`, `Tapped`). `RotationY` follows the finger a little
  (−0.42° at 10 px).
- Clocks: the press and release times are by the stopwatch, between input events. The return's fractions are the
  Button's, which are on animation time.

### ListView: the check mode, Extended and None, and a selection set from code (2026-11-15, device 11, #136)

For #130. `20261115-2352-dev11` (`-Probe listcheck`): the list of the `listpress` probe (40 items, each 412.08 × 39.17
view px), without item clicks, in `SelectionMode` `Multiple` (the check mode). The probe also watches each item's
`CheckboxTiltContainer`, the 25.42 px box at the item's left. Presses held 600 ms on item 1's box, on its text, at 95%
across and at 10% and 90% down; taps on item 4's box and text and on item 5's text. Then `Extended`: taps on items 1, 2
and 2 again. Then `None`: a tap and a 600 ms press on item 3. Then `Single`, the offset at 0, and `SelectedIndex` set from
code to 30, 5 and 39, 3 s apart, with no touch. `tools/motion/listpress.py` prints the presses.

- **A press on the box goes to `CheckboxPressed` (0.9 and 39.7 ms after the press) and tilts only the box.** It tilts about the box's own centre: a press at
  its centre holds `RotationX` −2.70° (the screen-position bias), `RotationY` 0.15° and `GlobalOffsetZ` −20.86. The
  depth follows the item's rule: −25 × (1 − (|`RotationX`| + |`RotationY`|) / 17.19°). The box's `CompositeTransform`
  is the item's (−0.167, 209 for item 1). The first frame shows the depth with almost no angle (0.28°, −24.39), the
  next overshoots (−3.16°), and the one after settles, as an item does.
- **The box has no return.** On release it is back at rest on the next frame (1 to 8 ms later), and
  `CheckboxPressed` > `Normal` comes 0.4 to 15 ms after the release. No 182.6 ms hold, no `KeySpline`.
- **A press anywhere else on the item is the Single mode's press.** That covers the text, 95% across, and 10% or 90%
  down. `CommonStates` goes to `Pressed` within 0.7 to 4.5 ms, and the whole item tilts, box included. Item 1 holds
  −2.13° at its centre and `RotationY` −8.10° at 95% across, as in [an item's press](#listview-an-items-press-2026-11-15-device-11-113).
  On release the item holds, then returns through the Button's fractions (0.9412, 0.8039, … 0.0163) one a frame. The
  first change comes 178 to 193 ms after the release, and `Pressed` > `Normal` 346 to 362 ms after it.
- **In the check mode every tap toggles its item**, on the box or off it. The selection changes at the release, with
  `Tapped`. `ItemClick` doesn't fire (`IsItemClickEnabled` is false).
- **Leaving `Multiple` turns every item's `MultiSelectStates` to `NoMultiSelect`** at once (the boxes go).
- **`Extended` under a tap is `Single`.** The first tap kept only the tapped item: items 1 and 5 were selected, the tap
  on item 1 removed item 5. A tap on another item replaces the selection, and a tap on the selected item changes
  nothing. Each press tilts the item as in `Single`.
- **`None` under a tap still presses and tilts** (`Pressed`, the same tilt and return) and selects nothing. Setting
  `None` cleared the selection.
- **A `SelectedIndex` set from code scrolls the list, at once and as little as it can.** From offset 0, `SelectedIndex`
  30 put the offset at 494.17 in the same call, with item 30's bottom edge on the list's bottom (30 × 39.17 + 39.17 −
  720). Then 5, above the view, put the offset at 195.83, with item 5 at the top. Then 39 put it at 846.67, the end.
  There is no animation: the next frame has the new offset.
- Clocks: the press, release and state times are the stopwatch's, between input events, as in #113. The return's
  fractions are frames (one a 15.625 ms step), the Button's.

## Motion (2026-11-10, device 11, #24)

How the theme moves: the ToggleSwitch's slide, the flyouts and pickers opening and closing, the
page transitions and the ProgressRing. Two ways of reading it:

- **In the app, every frame** (`-Probe motion`, `ring`): the probes watch a control's tree, and every
  open popup's, from `CompositionTarget.Rendering`, logging each element's opacity, render
  transform, projection and clip whenever they change. This reads a template's own storyboards (the
  MenuFlyout's, the ProgressRing's), as it read the tilt.
  Each popup is labelled `popup<n>` in the order it was first seen, so it keeps its label when
  another closes (each opening is a new popup, so the numbers climb through a run). A tree over 400
  elements is read up to the cut, and its readings past the cut carry over rather than read as rest;
  the log says so once for each tree.
- **On the screen** (`-Record`): the theme *transitions* (`PopupThemeTransition`,
  `NavigationThemeTransition`, the ToggleSwitch's `RepositionThemeAnimation`) run in the compositor
  and change nothing the app can read: their trees stay at rest every frame. `-Record` has
  `vm\session.ps1` record the emulator's display with `ffmpeg gdigrab` (lossless, each frame at the
  time it was grabbed), from before the probe's one tap, which starts its sequence, until it is done.
  It grabs about 40 frames a second (25 ms apart, ±10 ms), at 0.83 view px a pixel. The frames were
  measured with the scripts in [`tools/motion`](../motion/README.md), which says which numbers they reproduce.

**The emulator animates on a 64 Hz clock.** Every value moves in steps of 15.625 ms: the MenuFlyout's
`ScaleX` goes up by 0.041 a frame, 380 ms in 24.3 steps. An animation shows the same values in every
run, so the distinct values of many runs, in order, are its steps, one per 15.625 ms, at a finer
timing than the recording's.

**But 15.625 ms is a frame's step, not the time between frames** (`-Probe ringsize`,
`20261111-2316-dev11`; see [ProgressRing: the clock](#progressring-the-size-threshold-and-the-clock)). Read
from `Storyboard.GetCurrentTime()`, *animation* time moves exactly 15.625 ms each frame, however far apart
the frames are. On that run the frames came 15.0 ms apart by the app's stopwatch (66.7 a second), so
animations ran about 1.04 times as fast as the stopwatch. The ratio is the run's frame rate, not a
constant, though every run read so far is close: 1.037 to 1.043 where a log reads enough frames
([`tools/motion`](../motion/README.md)'s `clock.py`; the MenuFlyout runs' frames, first read as 15.4 ms apart,
are 15.0 to 15.2 ms apart once the frames with nothing logged are counted). So:

- **A duration counted in frames is animation time**: frames × 15.625 ms, as above, in the units a
  template's key times are in.
- **A duration read off the stopwatch** is shorter than its animation time by that run's frame interval
  over 15.625 ms. Convert it by counting its own frames where the log reads them (a clock or a value
  that moves every frame, as the tilt's return does); otherwise with the run's factor, `clock.py`'s 15.625 ms
  over the frames' mean spacing. That factor is the run's average: one interval's own frames can differ from
  it by a frame either way, so a duration converted with it is good to about a frame, 15.6 ms.
- **A duration from a recording** is timed by the VM's clock, not the emulator's. Whether the two agree
  isn't checked yet.

**Which clock each duration below is on** (2026-11-12, gap register B9 to B12). For a stopwatch duration whose own
frames the log doesn't read, [`tools/motion`](../motion/README.md)'s `clock.py` reads the run's factor: 15.625 ms
over the frames' mean spacing, the gaps with nothing logged counted in frames, about 0.5% (on
`20261111-2316-dev11`, 1.0418 against the storyboard's own 1.0405 to 1.0416). Each duration it converts is then good
to about a frame.

| Duration | Clock | In animation time |
|---|---|---|
| The ToggleSwitch knob's `KeySpline` over 370 ms; the MenuFlyout's 380 and 120 ms; the inline ComboBox items' 4 frames; the turnstile's 430 and 391 ms, its 71 ms time constant and 300 ms to within 1°; the slide's 350 ms in and about 190 ms out; the Continuum back's 340 ms; the tilt's return, a `KeySpline` over 167.0 ms | frames: distinct values one a 15.625 ms step, or frames counted | as given (the ComboBox items' 4 frames, 62.5 ms) |
| The tilt's hold, 182.6 ms from the first frame after the release | the stopwatch, converted by counting frames: nothing changes during it, so its frames aren't logged, and are counted from the stopwatch over each press's own frame spacing, 12.4 on average, the first change on the 13th frame ([Tilt: the model](#tilt-the-model-2026-11-09-device-11-for-22)) | 182.6 ms, open by one frame: which frame after the release is the animation's time 0 isn't read, and were it the second, the hold would be 167.0 ms |
| `ToggleStates` changing about 380 ms after a slide starts (374 to 390 ms on `20261110-0733-dev10`, 375 to 391 on `20261109-2128-dev11`) | the stopwatch | 389 to 405 ms and 389 to 406 ms (× 1.0390 and × 1.0371, each run's own) |
| The ToggleSwitch's tap limit, about 1,020 ms held | the stopwatch, between touch events | left as read: not an animation, and whether the phone times a tap on animation time isn't known |
| The ProgressRing's 4.132 s cycle; a dot hiding 3.090 s after it shows; the dots 164 ms apart (the median), 150 to 165 ms in the first cycle and 129 to 167 ms over nine | the stopwatch | 4,307.6 ms, 3,221.3 ms, 171 ms, 156 to 172 ms and 134 to 174 ms of animation time (× 1.0425 on `20261110-0829-dev11`), against the keyframes' 4,305 ms, 3,220 ms and 167 ms. The spacing is read on frames, so each is ±15 ms |
| The ProgressRing clock's own numbers (`Completed` 3,856.5 ms after `Begin()` and the like) | the stopwatch, and said so | the storyboard's own time is read beside them |
| The tilt press's first frame, 30 to 40 ms after the press | the stopwatch | about 31 to 42 ms (× 1.0405 on `20261109-2307-dev11`); a press is a jump, so there is no duration to build |
| The turnstile's forward ease, 425 ms (#114) | frames: distinct values one a 15.625 ms step | as given |
| Pivot's header strip and the new item's 400 ms slide, Hub's header (#114); the same with wide headers and two items, and the offset's settle times (#136) | frames: `RenderingTime` | as given; the selection's times are the stopwatch's |
| The turnstile's tick-0 times (#114) | the recording against the log, by the turns' median lag | only forward against back is used |
| The ScrollViewer's inertia (#113): each flick's time, 812 to 2,156 ms, and the fits' constants | frames: `RenderingTime`, which steps by 15.625 ms a frame | as given |
| The indicator's `IndicatorMode` changes and the ListView's press and release times (#113) | the stopwatch, between input events and states | left as read, as the tap limit is |
| The ListView item's tilt return (#113) | frames: the same fractions a frame as the Button's | the Button's 182.6 ms hold and 167.0 ms `KeySpline` |
| The check mode's press and release times, and the `SelectedIndex` jump (#136) | the stopwatch, between input events and states | left as read |
| Hub's background rate and the header's 420 ms ease after the loop (#136) | frames: `RenderingTime` | as given; the jump's time after the press is the stopwatch's |
| The app bar's opening and closing, its time constants and settle times (#136) | the recording, paired with the log by the header's flash | **open**, as the row below |
| The end-of-content squash and the indicator's fades (#113): about 200 ms back, 370 ms in, 340 ms out | the recording, paired with the log by the header's flash | **open**, as the row below |
| The popups' times (the table below); a top Flyout gone about 100 ms, and a ContentDialog 125 ms, after `Hide()`; the inline ComboBox opening about 140 ms after the call; the Continuum's fade (one or two recorded frames), black (at most 94 to 125 ms) and growth (281 to 312 ms) | the recording, timed by the VM's clock, and paired with the app's log by a fixed lag | **open**: whether the VM's clock runs with the emulator's stopwatch or with its animation time isn't known. If with the stopwatch, these are about 4% short of animation time on these runs, under the ±25 ms they are quoted to |

**A PlaneProjection's camera** (`-Probe projections`, `20261110-0756-dev11`): a full-window panel given
known projections (`RotationY` ±10°, ±30°, ±60° about `CenterOfRotationX` 0, 0.5 and −0.25, and
`GlobalOffsetZ` ±100) fits a pinhole 999 view px in front of the screen, looking at the element's
centre (224, 400), within 0.53 px over the 13 states that stay on the screen. A positive `RotationY`
brings the right side nearer. The page transitions below are solved with it.

### ToggleSwitch: the knob's slide

The slide is the template's `RepositionThemeAnimation`: `KnobTranslateTransform.X` jumps to 56.5 at
once, and `ToggleStates` reaches `On` or `Off` about 380 ms later by the stopwatch (389 to 406 ms of
animation time on the two runs converted), when the slide is over (all of
#58's runs, and on the 720p profile). From the recording (`20261110-0819-dev11`, 20 slides, the
switch set from code): the knob travels 56.56 view px, the same way in both directions, through the
same positions every time.

Three numbers for the travel, each something else:

- **56.5, the template's literal**: the `On` state sets `KnobTranslateTransform.X` to 56.5, where
  the knob rests when On; the probes read 56.5 there every time.
- **56.67, the control's own** (`-Native`, [`TemplateSettings`](#templatesettings)):
  `KnobOnToOffOffset` 56.67 and `KnobOffToOnOffset` −56.67 on the reference, 136 raw px. The
  control computes it in native code from its layout, a whole number of raw pixels, so it moves
  with the profile (55.83 to 56.67). A slide set from code (the `OffToOn` and `OnToOff`
  transitions) starts from it, the animation's `FromHorizontalOffset`; a slide after a drag
  (`DraggingToOn`, `DraggingToOff`) starts from the `Current` offsets instead. On the reference and 720p
  profiles (the two read) the drag's halfway line is half of it (above).
- **56.56, the recording's**: the slide's travel as measured on the screen, the distance between
  the knob's left edge at rest Off and at rest On in the recording (`knob.py`). It falls between
  the other two; they are 0.17 view px apart, a fifth of a recording pixel at 0.83 view px each,
  so the recording doesn't separate them.

The 21 distinct positions, one per 15.625 ms step, fit the `KeySpline` 0.1,0.9 0.2,1 over
370 ms, the first frame one step in, within 0.001 of the measured travel, 56.56 (0.06 px); an
exponential ease-out (k 6 to 8) fits ten times worse, the power ease-outs twenty.
370 ms and one or two steps (386 to 401 ms) is, within a frame, the state change's 389 to 406 ms of animation
time.

### Flyouts, pickers and the dialog

`20261110-0819-dev11` (`-Probe motion -Record`): each opened from code four times, held 1.2 s, and
closed with `Hide()` (the ComboBoxes with `IsDropDownOpen`). The app's values show none of it, except
the MenuFlyout's template. Times are from the first frame that changed until the screen is 50%, 90%
and 99% of the way to its final look (mean absolute difference), the median of four runs, ±25 ms, by the
recording's clock (above);
"top to bottom" gives when the top and the bottom 100 px band are half done.

| Popup | Opening | ms | Closing | ms |
|---|---|---|---|---|
| Flyout, top | swings down from its top edge (its bottom edge at 46, 55, 69, then 77 of 78 px) | 50 / 100 / 142 | gone in one frame, about 100 ms after `Hide()` | 0 |
| Flyout, full | sweeps in top to bottom (74 to 208) | 141 / 208 / 208 | sweeps out top to bottom (117 to 275) | 200 / 291 / 334 |
| MenuFlyout | the template's storyboard (below) | 483 / 516 / 516 | the menu goes in one frame; the full-window layer under it about 300 ms later | 0 / 0 / 317 |
| ListPickerFlyout | the header, then each item, swing in one after another, then the background fills downwards (75 to 367) | 209 / 325 / 367 | top to bottom (100 to 375) | 242 / 375 / 425 at the lag typed in, 29.15 s; 284 / 392 / 434 at the knob's own, 29.158 s (`popups.py`): an 8 ms shift moves this row, so it is less certain than ±25 ms |
| DatePickerFlyout | the three columns and header (83 to 200); the last 1%, the app bar, by 400 | 117 / 200 / 400 | 117 to 325 | 150 / 242 / 358 |
| TimePickerFlyout | as the date picker (100 to 183; 408) | 125 / 183 / 408 | 125 to 333 | 167 / 234 / 358 |
| ComboBox, 6 items (the full-screen picker) | as the ListPickerFlyout (109 to 342) | 200 / 333 / 342 | top to bottom (116 to 408) | 291 / 408 / 458 |
| ComboBox, 5 items (inline) | opens in one frame, about 140 ms after the call; in the app, the items' `TranslateY` eases from −8.5 to 0 over 4 frames | 142 / 142 / 208 | closes in one frame | 0 |
| ContentDialog | about 200 ms | 34 / 192 / 209 | gone in one frame, about 125 ms after `Hide()` | 0 |

**MenuFlyout, from the app's values** (`20261110-0745-dev11` and `-0753`): `OuterBorder`'s `ScaleX` goes
0 to 1 linearly over 380 ms, origin (0.5, 0); then `InnerBorder`'s `ScaleY` 0 to 1 linearly over
120 ms. That is the template's `TopPortrait` storyboard, run as written. Its close has no storyboard.

### Page transitions

`20261110-0748-dev11` and `20261110-0810-dev11` (`-Probe pages -Record`): a Frame with a
`NavigationThemeTransition` (what the root frame has) navigates between two pages, a white and a blue
sheet inset 19, 60. The second run did each transition eight times. The sheet's outline in each frame
is solved for the page's rotation with the camera above.

- **The default transition is the turnstile**, and `CommonNavigationTransitionInfo` is the same. Only
  the page coming in moves: the page going out disappears at once. It turns about a vertical axis
  44 px left of the page's left edge and 99 px behind the screen (a PlaneProjection with
  `CenterOfRotationX` about −0.1 and `CenterOfRotationZ` −100): with that one axis, every frame of
  nine forward turns fits within 0.24 px. Forward it comes in from a negative `RotationY` (the right
  side away), back from a positive one (the right side nearer).
- **Its angle falls by a factor of 0.803 every 15.625 ms step** (a time constant of 71 ms), both
  ways: an exponential ease-out. Back, an `ExponentialEase` (exponent 6, ease-out) from +42.0° over
  430 ms, starting on the first frame shown, fits the 16 distinct angles within 0.07°. Forward, the
  first frame shown is at −67.0° (in 4 of 9 runs; else −53.8° or −43.1°), and an exponential
  ease-out from there ending 391 ms later (exponent 5.5) fits the 19 distinct angles within 0.05°;
  exponent 6 over 430 ms fits only with a start far off the screen. It is within 1° of rest after
  about 300 ms. *Settled 2026-11-15 (B10, #114, [below](#the-turnstiles-forward-start-2026-11-15-device-11-114)):
  forward starts at −67.0°, on the same `ExponentialEase` (exponent 6) as back, over 425 to 430 ms.*
- **`SlideNavigationTransitionInfo`**: forward, the new page rises from below, first seen 200 px down;
  its offset falls by 0.765 a step (58 ms): an `ExponentialEase` (exponent 6, ease-out) from 200 px
  over 350 ms, starting on the first frame shown, fits the 16 distinct offsets within 0.05 px. Back, the page going
  out slides down, faster each step (×1.43, an ease-in) for about 190 ms, and then the page under it shows at
  once; the last offset seen is 65 to 137 px. *Corrected 2026-11-12* (`slide.py`): the eased fit to the offsets of
  2 px or more ends 12 steps, 188 ms, after the first; "about 220 ms" came from offsets picked by hand on
  2026-11-10, and "140 to 200 px" isn't reproduced.
- **`ContinuumNavigationTransitionInfo`**: forward, the old page fades out in one or two recorded frames,
  the screen stays black for at most 94 to 125 ms, then the new page grows about the screen's centre from a
  scale of 0.94 to 0.96 at its first frame to 1 in 281 to 312 ms. These are times on the recording, its frames
  about 31 ms apart here. *Corrected 2026-11-12* (`slide.py`): the black is from the first black frame to the
  first of the new page, so an upper bound; 2026-11-10's 60 to 100 ms isn't reproduced. Back, the page going out slides down as in the slide's back,
  then the page under it fades in: what is left of its opacity falls by about 0.84 a step, over
  about 340 ms.

### The turnstile's forward start (2026-11-15, device 11, #114)

B10. `20261115-2053-dev11` (`-Probe turnstile -Record`): the default transition alone, forward and back 30 times. The
recording can't go faster: gdigrab gets 40 to 43 frames a second on the VM whatever the rate asked for or the area
grabbed (60, 120 and 240 tried, the whole display and 200 × 100 px), and `ddagrab` finds no Direct3D 11 device there. So
the turns are many instead. `tools/motion/turnstile.py` prints every number below; on `20261110-0810-dev11` it gives the
same.

- **The first frame shown was −53.8° in 25 turns, −67.0° in 4 and −43.1° in 1** (−67.0 in 4 of 9 before). They are
  ticks 0, 1 and 2 of one ladder: 67.0 × 0.803 = 53.8, × 0.803 = 43.1.
- **No step comes before −67.0°.** The back turn's first step, +42.0°, is its start (above). Each turn's tick 0 is
  timed as the first frame's time after its step in the log, less 15.625 ms for each tick it is past tick 0. Forward
  tick 0 reaches the screen 3.7 ms *earlier* than back's (standard error 3.4 ms; on the 2026-11-10 run 0.4 ms later,
  ± 7.2). A step drawn before −67.0° would put forward's a tick, 15.6 ms, later. So −67.0° is the turn's first value,
  shown on the same frame as the back turn's +42.0°.
- **Forward is the back turn's curve**: an `ExponentialEase` (exponent 6, ease-out) starting on tick 0 fits the 19
  distinct forward angles from −67.10° over 425 ms within 0.072° rms, and the back ones from +41.97° over 430 ms within
  0.070°. A free fit still prefers a start before the screen (−147° over 430 ms, 3.6 ticks in, 0.063°), but by 0.01°,
  which the timing above rules out.
- Two frames were left out: one torn (grabbed while the emulator drew it, so half one angle and half another; no
  rotation fits it within 123 px), and one value, −6.5°, between two steps a tick apart (a frame caught between them).
- Clock: the angles are on frames (one a 15.625 ms step); the tick-0 times are the recording's clock against the log's,
  paired by the turns' median lag, so only the forward-against-back difference is used.

### Pivot's header strip (2026-11-15, device 11, #114)

`20261115-2056-dev11` (`-Probe pivot -Record`, `tools/motion/pivot.py`; again without the recording after review,
`20261115-2136-dev11`, every number the same or in the ranges given): a Pivot of four items, headers "one" to "four",
below the header band. Swipes left (200 px in 150 ms) from each item, twice round (the swipe from "four" wraps to "one");
a swipe right from "one" (it wraps to "four"); a tap on the header "two" and on "three" from "one"; a drag of 100 px
held and let go. The Pivot's ScrollViewer reads DirectManipulation's offset, and the header items' and the
ItemsPresenter's places follow it frame by frame in the app. The recording shows the same places about a frame (30 ms)
earlier than the log has them (checked on the first swipe, header and content).

- **The header strip moves with the content's offset, at one header's pitch a page.** A header's left edge moves
  against the offset, in a straight line (worst 0.015 px over every frame): 0.23706 px a px for "one", 0.23056 for
  "two", 0.30462 for "three", 0.24909 for "four". Each is that header's pitch over the page's 450 px: the next header's
  left edge less its own (106.67, 103.75, 137.08), which is its width plus 15.83. So when the content has moved a page,
  the next header has come to where this one was. Going right, from "one", it moves at "four"'s 0.24907: the pitch of
  the header coming in. On the wrap the strip goes on the same way: "four" moves 0.24909 px a px, a pitch of 112.1 (0.24909 × 450) to
  the "one" after it.
- **Once the new item is chosen, the header that left goes to the end.** On the frame the new item jumps in, the
  header that was selected, by then partly off the left edge (its left edge at −52), moves to after the last (402); the
  others keep their places. The header panel's translate is reset when the offset settles, with no move on the screen.
- **The selection changes during the swipe**, 188 to 241 ms after the press (stopwatch; 168 and 174 ms swiping right),
  once the inertia is under way, and at the release of a header tap (49 to 74 ms after its press).
- **The content isn't the offset's.** The old item goes with the finger and the offset. When the selection changes,
  the ItemsPresenter's translate jumps to put the new item 450 px beyond it, and the new item then slides in on its own:
  one frame at 450, then an `ExponentialEase` (exponent 4) over 400 ms to 0, the same fractions a frame on every swipe,
  wrap and tap to four places (0.8526, 0.7266, 0.6188, 0.5266, …; exponent 4 over 400 ms fits all 25 distinct
  fractions to rms 0.003, 1.4 px of 450). A tap on "three", two pages on, does the same, from 450 px.
- **The offset travels exactly 450 px a swipe and settles** 609 to 672 ms (rt) after the first frame after the press,
  and 516 to 531 ms after a tap (900 px for "three", two pages).
  A drag of 100 px held 300 ms and let go goes back: the offset returns to where it was.
- Clocks: the per-frame values are on `RenderingTime` (15.625 ms a step); the selection times are the stopwatch's.

### Pivot: wide headers, two items, and a tap on a header cut by the edge (2026-11-15, device 11, #136)

For #121. `20261115-2339-dev11` (`-Probe pivotwide -Record`): four items, headers "favourites", "everything", "recent" and
"supercalifragilisticexpialidocious", which come to 1,346 px against the 450 px view, the last one 728.75 wide on its
own. Swipes left from each item (the last wraps), a swipe right from the first, and taps on the headers the right edge
cuts. `20261115-2343-dev11` (`-Probe pivottwo -Record`): two items, "one" and "two", swiped left and right from each, and
each header tapped from the other item. Before the touches, the probe selects each item from code and logs every
header's place at rest (the `rest` lines). `tools/motion/pivot.py` prints the numbers below.

- **Wide headers follow the same rule.** Each header is laid out at its full width; nothing is trimmed or wrapped. At
  rest the selected header is at 15.84, and the rest follow it in item order, 15.83 apart, wrapping round (with
  "recent" selected: recent 15.84, supercal… 177.09, favourites 921.67, everything 1162.5). A header the edge cuts is
  simply cut, as is one wider than the screen (the recording shows "supercalifragilisticex" running off the right).
- **The strip still moves at the selected header's pitch over 450.** Pitch is the next header's left edge less this
  one's. "favourites" 0.53518 px a px (pitch 240.83), "everything" 0.58428 (262.92), "recent" 0.35835 (161.25). The
  wide header moves 1.65462 px a px (pitch 744.58): faster than the content, about 42 px a frame. Going right from
  "favourites", the strip moves at 1.65463, the pitch of the wide header coming in. Worst residual 0.011 px.
- **The header that leaves still goes to the end** once the new item is chosen, wide or not (the wide one jumps from
  −424 to 949).
- **A tap on a header the edge cuts selects it**, as a tap on any other header does: "everything" (on screen from 256.67
  to 450, tapped at 352.8) from item 0, and the wide header (tapped at 313) from "recent". The selection changes at
  the release, 68 to 72 ms after the press. The offset then travels 450 px and settles 531 ms (rt) after the first
  frame after the press. That is the same as a tap on a header fully on screen.
- **A tap on the selected header does nothing**, even when the header runs off the screen. It raises `Tapped`, but the
  selection, the offset and the strip don't change.
- **A two-item Pivot still wraps, both ways.** A swipe left from "two" goes to "one", and a swipe right from "one" to
  "two". Each moves the offset 450 px and slides the new item in, as with four items. The strip holds only the two
  headers, with no copies. Going left, the header that leaves goes after the other one, as with four. Going right
  from "one", "one" moves right at "two"'s pitch (0.23056 px a px). "two" stays to the right of it until the selection
  changes, then jumps to the left (−41.9) and slides in to 15.83. Until then nothing is drawn left of "one".
- **The new item's slide is the same** in both runs, to four places: one frame at 450 px, then `ExponentialEase` 4
  over 400 ms (rms 0.003). The selection changes 200 to 235 ms into a swipe left and 152 to 172 ms into a swipe right
  (stopwatch). The offset settles 625 to 656 ms (rt) after the first frame after the press.
- Clocks: the per-frame places and the settle times are on `RenderingTime` (15.625 ms a step). The selection times are
  the stopwatch's.

### Hub's header (2026-11-15, device 11, #114)

`20261115-2059-dev11` (`-Probe hub -Record`): a Hub with the header "hub" and four sections, swiped left three times,
right once, and dragged 150 px and let go.

- **The header moves at 1/18 of the sections' speed**: its left edge is 15 − offset / 18 view px, in a straight line
  over all 270 frames with the offset past 20 px (offsets 21 to 1,215; worst 0.0005 px). The sections move one for one
  with the offset. The recording agrees within a frame.
- At launch the Hub's entrance animation moves the header in from 19.7 to 15 (and the sections with it) before any
  touch.

### Hub: a background image, and the end of the sections (2026-11-15 and 16, device 11, #136)

For #122. `-Probe hubimage` gives the Hub's `Background` an `ImageBrush` (`Stretch` `Fill`, the default) of a picture the
probe draws: a hue running left to right, a black line every 50 px and a white one every 250 px. Runs:
`20261116-0009-dev11` (1600 × 800 px) and `20261116-0001-dev11` (`-Probe hubimage900`, 900 × 400). Each run reads, every
frame, the template's `Background` grid and its two borders, `MainBackground` and `WrapBackground`. Since #136 every Hub
probe also sets the offset to the end and swipes left there, and again and swipes right. `20261116-0007-dev11`
(`-Probe hub`) is the Hub with no image. `20261115-2350-dev11` was a first 1600 px run, whose layout lines were read
during the entrance. `tools/motion/pivot.py` prints the numbers below; the recordings show the same.

- **The image is drawn at its own width and the screen's height.** `Background` and both borders are the image's
  pixel width in view px (1600, 900) by 800, the window's height (the Hub is 740 tall here), whatever the image's
  height. `Fill` stretches the 400 px picture to 800 tall and leaves its width alone.
- **The image moves, slower than the sections, at a rate set by its width.** Let L be the sections' width in all (4 ×
  405 = 1,620) and V the view's (450). Until the offset reaches L − V (1,170), the image's left edge is −offset × (W −
  V) / (L − V), W the image's width. That is 0.982906 for 1600 and 0.384615 for 900, within 0.001 px over every frame.
  So the image's right edge reaches the screen's right edge just as the last section's does. Past L − V, to the end of
  the scroll range (1,215.4), the image moves nearly one for one with the sections (0.99778).
- **`WrapBackground` is a second copy of the image, 1 px less than its width away** (−1599 or −899 while the offset is
  low, +1599 or +899 near the end). At the end it fills the screen past the image's right edge, so the picture repeats.
- **The header still moves at 1/18 of the offset** (15 − offset / 18, worst 0.0005 px), image or not.
- **The Hub loops.** A swipe left at the end carries on past the last section, with or without an image. The
  sections, the image and its copy go on moving left, and the first section comes in from the right, drawn after the
  last. The header speeds up to the left (from −52.5 to about −110). Then, 206 to 238 ms after the press
  (stopwatch), it jumps to the right, to 433 to 459. Then it eases in to 15: the same fractions a frame in every run, an
  `ExponentialEase` (exponent 7) over 420 ms (rms 0.0002 to 0.0007). About 200 ms (rt) after the jump the offset goes
  back to 0: the first section to its place and the image to 0, its copy to −W + 1. On the screen that is a step of
  1.4 px.
- **No `BackgroundImageLoadStates` change was logged.** The image had loaded before the probe watched the states.
- Clocks: the per-frame places and the ease are on `RenderingTime`. The jump's time after the press is the stopwatch's.

### ProgressRing: Active

The suite's forced `Active` state captures no dots: they show only while the storyboard runs. Read
every frame instead (`20261110-0829-dev11`, `-Probe ring`, 6 s, with a recording): a ring at its
default size, 57.5 view px, shows five dots, and one 120 px wide shows six. Each dot's `Canvas`
rotates and its `Ellipse` hides and shows as the template's storyboard says, the dots 150 to 165 ms
apart in the first cycle (the template's `BeginTime`s are 0.167 s apart). One cycle takes 4.13 s by the app's
clock (nine cycles), and a dot hides 3.09 s after it shows again (*corrected 2026-11-12*: 3.10 s was
published, `ring.py` reads 3.090). The template's keyframes give 4.31 s (the last dot's
`BeginTime` and its 3.47 s) and 3.22 s: the same shape, 4% faster. **That is the emulator's clock**
(below), as far as the cycle goes: at that run's frame rate, 4.305 s and 3.22 s of animation time come
to 4.13 s and 3.09 s on the stopwatch, which [`tools/motion`](../motion/README.md)'s `ring.py` reads as
4.132 s and 3.090 s. The dots' spacing, 0.167 s of animation time, would be about 160 ms on the
stopwatch; `ring.py` reads 164 ms (the median), 150 to 165 ms in the first cycle and 129 to 167 ms over
all nine, so within a frame or two of it but not explained to the millisecond: no probe has read a dot's
own clock. At the run's factor, 1.0425 (`clock.py`), the cycle, the hide and the spacing are 4,307.6 ms, 3,221.3 ms, 171 ms, 156 to 172 ms and 134 to 174 ms of animation time, against the keyframes' 4,305 ms, 3,220 ms and 167 ms;
the spacing is read on frames, so each is ±15 ms. The keyframes are what to build.

### ProgressRing: the size threshold and the clock

`20261111-2316-dev11` (`-Probe ringsize`, device 11, B6 for #28; `tools/motion/ringsize.py` prints every
number below from it). One active ring, its `Width` and `Height` stepped by 1 from 40 to 130 and back,
then by 0.05 from 60 to 61, then 200 wide and 60 or 61 tall and the other way round; each step held
120 ms before it was read.

- **`SizeStates` is `Large` when the ring's smaller laid-out side is over 60 view px, else `Small`.**
  The sizes snap to raw pixels (0.417 view px at 2.4), so: 60 gives `Small`, the next step, 60.417,
  `Large`. The same going down, at the same place (no hysteresis). Not square, it is the smaller side
  that counts: 200 × 60 and 60 × 200 are `Small`, 200 × 61 and 61 × 200 `Large`. All 208 steps agree.
  The side is the one `MaxSideLength` reports, the smaller of `ActualWidth` and `ActualHeight`; below
  the 57.5 `MinWidth` and `MinHeight` it is 57.5. `Large` shows `SixthCircle` and `Small` collapses it,
  at every step. How many frames after the size the state changes isn't read: each step is read once,
  120 ms after it is set.
- **The `Active` storyboard's own time moves 15.625 ms every frame.** Read each frame from
  `Storyboard.GetCurrentTime()` on the `Active` state's storyboard, from `IsActive` going true, for 10 s:
  all 663 moves are 15.625 ms. The frames were 15.0 ms apart by the stopwatch (the median; the mean
  15.017), so animation time ran 1.0405 times the stopwatch (the step over the mean frame), or 1.0416 by a
  least-squares fit. It wraps after 4,296.9 ms of its own time: the keyframes' 4.305 s, wrapping at the
  frame that passes it.
- **A storyboard of known length agrees**: a 4 s `DoubleAnimation` of our own, three runs. Its own time
  runs 1.0416, 1.0415 and 1.0416 times the stopwatch by the fit. Its `Completed` fires 3,856.5, 3,862.6
  and 3,861.1 ms after `Begin()` (ratios 1.036 to 1.037), the event coming a frame or so after the end.

A second run after the probe's review fixes, `20261111-2327-dev11`, gives the same threshold at every step
and the same clock to 0.001 (frames 15.0 ms apart, the mean 15.000, counting as two the one gap over which
the clock moved two steps; the fit 1.0417).

So on this run the emulator's animations ran about 1.04 times as fast as its stopwatch, by every measure,
because each frame moves animation time 1/64 s and the frames came at 66.7 a second. The ring's
template needs no correction: its 4.13 s cycle is the keyframes' 4.305 s on that clock.

## The page's app bar (2026-11-09, device 11, #23)

`20261109-2124-dev11` and `20261109-2130-dev11` (`-Popups only -Only BottomAppBar -Grab`), the
same to the pixel row: a `CommandBar` set as the page's
`BottomAppBar`, with three primary commands (add; favorite, a checked toggle; delete, disabled)
and two secondary ones, in four variants, dark and light, each with a screenshot.

- **The shell draws it, not the page.** The `CommandBar` sits in a popup whose tree is dumped,
  but the dump is the same open or closed and doesn't match the pixels (its bar starts at 742.5,
  the drawn one at 737.08). Only the screenshots are the reference.
- **`IsOpen` set as the bar is attached is ignored.** The specimen opens it 400 ms after the
  bar's `Loaded`, records an error if `Opened` never fires, and on close detaches the bar only
  once it has `Closed`, since taking a popup away mid-animation has killed the app before.

Read from the screenshots, in view pixels (raw ÷ 2.4):

| Variant | Bar's top | Bar's height | Shows |
|---|---|---|---|
| Compact, closed | 737.08 | 62.92 | the three buttons' circles, no labels; the ellipsis at the top right |
| Minimal, closed | 773.75 | 26.25 | the ellipsis only |
| Compact or minimal, open | 514.58 | 285.42 | the circles with labels below, then the secondary commands as a list in large text |

The bar is `#FF1F1F1F` dark and `#FFDDDDDD` light, as the pickers' bars are. The ellipsis's dots
start 25 raw px (10.4 view px) below the bar's top in every variant. The checked toggle is a
filled circle; the disabled one is grey, its label too.

**Its size, read through the API** (B2, #114; `20261115-2050-dev11`, `-Native`, which now reads the app bar after the
`TemplateSettings`: a `CommandBar` with two primary and two secondary commands as the page's `BottomAppBar`, in portrait,
under each bounds mode with the tray hidden and shown, closed and then open, and the page with none before and after).
In view pixels, the same in every reading of each row:

| `ClosedDisplayMode` | `ActualHeight` | The bar in the window | `VisibleBounds`' height, tray hidden / shown | What the screenshot draws |
|---|---|---|---|---|
| (none) | | | 800 / 772.08 | |
| `Compact`, closed or open | 57.5 (138 raw px) | 0, 742.5, 450 × 57.5 | 737.08 / 709.17 | 62.92 tall from 737.08 closed; 285.42 from 514.58 open |
| `Minimal`, closed or open | 26.25 (63 raw px) | 0, 773.75, 450 × 26.25 | 773.75 / 745.83 | 26.25 from 773.75 closed; 285.42 from 514.58 open |

- **The compact bar's control is 57.5 tall, but the view gives it 62.92**: `VisibleBounds` ends 62.92 above the
  screen's bottom (151 raw px, the tray's width in landscape), and the shell draws the bar that tall. The control sits
  in the bottom 57.5 of it. The minimal bar is 26.25 in all three.
- **Opening changes nothing the API reports**: `ActualHeight`, the bar's place and `VisibleBounds` stay as closed
  (`IsOpen` true, `Opened` raised). The open bar is the shell's drawing over the page.
- **`VisibleBounds` takes the bar off the bottom, and the tray off the top**: tray shown, compact, it is 0, 27.92,
  450 × 709.17. Under `UseVisible` (the default) the page's content is laid out in it; under `UseCoreWindow` the
  content fills the window, 450 × 800, and `VisibleBounds` still says what the bar and the tray cover. The page itself
  fills the window in both.

### The app bar under touches, and with other numbers of secondary commands (2026-11-15 and 16, device 11, #136)

For #133. Two runs:

- `20261115-2354-dev11` (`-Probe appbar -Record`): the bar above as the page's `BottomAppBar`, compact, with favorite
  unchecked and delete enabled. The touches go where the screenshots draw things, planned with no target, since the
  shell's bar is outside the app's tree. The app logs the bar's `Opened` and `Closed` and each command's `Click`. The
  recording shows the bar. `tools/motion/appbar.py` reads the bar's top edge in each recorded frame from a strip at the
  screen's left, paired with the log by the header's flash.
- `20261116-0005-dev11` (`-Popups only -Only BottomAppBar -Grab`): the bar open with 0, 1, 3, 4, 5 and 10 secondary
  commands, dark and light, each with a screenshot. The screenshots and the recording's stills are in
  [`ref/wp81/CommandBar/`](../../ref/wp81/CommandBar/README.md).

What a touch does:

- **Invoking a command closes the open bar.** That holds for a primary command, a secondary one (a menu line) and the
  toggle. `Closed` comes first, as the finger lifts, and `Click` 14 to 21 ms after it. The toggle is checked by then.
  On the closed bar a command's `Click` comes and the bar stays closed.
- **A tap on the ellipsis opens the closed bar and closes the open one.** A tap on the page above the open bar closes it.
- **A tap on the minimal bar away from the ellipsis opens it**, as a tap on its ellipsis does.
- **Opening from code** (`IsOpen` set) raises `Opened` in the same call and moves the bar as a tap does.

What a press looks like (stills from the recording, 540 × 960, 1.2 recording px a view px):

- **A pressed command fills its circle with the accent** (#3E65FE recorded; Cobalt is #FF3E65FF). The glyph stays white
  and the label doesn't change. The circle sinks: its ink is 46 recording px across where it is 49 at rest, and its
  bottom rises 4 px, as a tilt's depth does. This is the same on the closed and the open bar.
- **A pressed menu line tilts and keeps its colours.** The text's ink moves from x 26–239, y 743–768 to x 29–239,
  y 740–766, with no highlight behind it.

The motion, from the recording (`appbar.py`). The bar's top moves; the bar doesn't fade.

- **Opening**: the top rises from 737.08 (compact) or 773.75 (minimal) to 514.58. The rest of the way falls about
  exponentially, by e every 76 to 90 ms (seven openings). It is 90% there 145 to 255 ms after the last frame at rest,
  and at rest 400 to 455 ms after it.
- **Closing** is faster: by e every 27 to 44 ms, 90% there by 72 to 127 ms, at rest by 163 to 218 ms (seven closings).
- **Changing `ClosedDisplayMode`** while closed moves the top in 36 ms (compact to minimal) or 91 ms (back).
- **Taking the bar off the page** slides it down off the screen in about 145 ms.
- Clocks: these are the recording's (the VM's), its frames 18 to 36 ms apart, and good to about a frame (35 ms).
  Whether the VM's clock runs with the emulator's is open, as for the popups' times ([which clock](#motion-2026-11-10-device-11-24)).
  The pairing is good to about 35 ms: one opening's first move came 35 ms before the setup that caused it.

**The open bar's height with other numbers of secondary commands** (the screenshots; view px; the same dark and light):

| Secondary commands | Bar's top | Bar's height |
|---|---|---|
| 0 | 710.83 | 89.17: the circles and labels, then 18.3 below the labels |
| 1 | 632.50 | 167.50 |
| 2 (the 2026-11-09 screenshots) and 3 | 514.58 | 285.42 |
| 4, 5 and 10 | 381.25 | 418.75 |

- From the bar's top, everything sits where it does with two: the circles from 10.83, the labels from 62.08, the first
  menu line's ink from 104.58, and each line 59.58 below the last.
- **The height goes up in steps, not a line at a time**: 2 and 3 lines are drawn the same height, and so are 4 and 5.
  With 3, 41.7 is left under the last line, where 2 leave 100.8.
- **It stops at 418.75.** With 10 lines the sixth runs off the bottom of the screen. Whether that list scrolls isn't
  read.

## Popup pixels

`RenderTargetBitmap` can't see popups, so `-Grab` takes the phone's display from the emulator.
What was tried on 2026-11-08, on device 11 (1080 × 1920) with the VM's screen at 2560 × 1440:

- **A grab of the emulator's window** (`OPWindowClass`, "Output Painter DX Child Window", with
  `PrintWindow`) is only as sharp as the emulator's zoom. At the zoom that fits the screen the
  window is 551 × 979, so the grab is scaled. At 100% it is 1080 × 1920, but the emulator doesn't
  draw the part of the window below the screen: every row past about 1406 came out blank, which
  is where the pickers' app bars are. The zoom also goes back to fitting when the emulator restarts.
  A hidden "Output Painter Child Window" is 1080 × 1920 too, but holds no frame of its own.
- **The emulator's own Screenshot tool** (Additional Tools > Screenshot > Capture, then Save...)
  reads the whole display at 1080 × 1920 whatever the zoom. `-Grab` uses it:
  `vm\grab-loop.ps1` watches the window, and each time the picture has changed and then held
  still for 1.2 s, presses Capture and Save... through UI Automation and fills the save dialog
  with Win32 messages (`vm\XdeWin32.cs`). Walking the modal file dialog through UI Automation
  was slow enough to hang. If Additional Tools is closed, as after a restart, it presses the
  toolbar's `>>` with posted mouse messages: the toolbar draws its own buttons, so UI Automation
  doesn't see them. `vm\xde-tree.ps1` lists the emulator's UI Automation tree, to find controls.

Two runs of `-Popups only -Grab`, the second from a cold emulator (`20261108-2248-dev11`,
`20261108-2254-dev11`), gave 24 shots each, all 1080 × 1920. Compared, three shots were identical
to the pixel and one differed in 7 pixels by 1 level, in the anti-aliasing of the app bar's icons.
The ComboBox with 3 items, which opens inline, gets a shot too: the hold doesn't depend on a popup.

## The probe's first numbers (2026-11-08, Update 1 720p 4.7", dark)

| | Value |
|---|---|
| `DisplayInformation.ResolutionScale` | 180 (`RawPixelsPerViewPixel` 1.8, `LogicalDpi` 172.8, raw DPI 309 × 312) |
| `Window.Current.Bounds` | 400 × 711.11 logical px |
| Default `Button`, content "button" | 108.89 × 57.22, `MinWidth` 109, `MinHeight` 57.5, `Padding` 9.5,0,9.5,3.5, `BorderThickness` 2.5, `FontSize` 18.14 |
| `TitleTextBlockStyle` | 18.14 px, weight 600, height 23.63, Segoe WP |
| `BodyTextBlockStyle` | 18.14 px, Segoe WP |

Nothing in this folder is Microsoft's: the project, manifest and placeholder logos were written
here. The captures are ours, but they stay in `R:\cornflower-reference\` until the repo's
`ref/wp81/` exists.
