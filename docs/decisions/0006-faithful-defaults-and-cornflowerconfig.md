# ADR 0006 — Faithful by default; departures through `CornflowerConfig`

**Date:** 2026-11-17 · **Status:** Proposed in [#190](https://github.com/wdcossey/CornflowerUI/pull/190), for [#189](https://github.com/wdcossey/CornflowerUI/issues/189). The
maintainer chose the shape on 2026-11-17 (the issue); the details below were decided on the maintainer's behalf, and
are accepted when the PR is merged.

## Context

On 2026-11-17 the maintainer tried the sample and found the MenuFlyout slow to open. It is the phone's own measured
motion (gap register A8: `ScaleX` over 380 ms, then `ScaleY` until 500 ms), so the theme is right to run it, but apps
will want their motion quicker, and more small departures from the phone will come up. [ADR 0001](0001-what-faithful-means.md)
says what "faithful" means; nothing said where a departure an app asks for should live. Avonic, the sibling library,
has `AvonicConfig`: a public static class of process-wide switches, read when a behaviour runs.

## Decision

**The defaults stay the phone's. Every departure from the phone goes through `CornflowerConfig`, by name, off by
default, and is recorded in its control's README.**

| Question | Decision | Why |
|---|---|---|
| Where the switches live | **`CornflowerConfig`**, public static, in `Cornflower.Controls`: `Animated`, `MotionSpeed`, a speed per area, and `Deviations` | One place an app looks; Avonic's shape. The theme reads it, so it has no visual opinion of its own |
| When a setting is read | **When a motion starts**, and kept to its end | A change at runtime reaches the next motion, never jerks one under way; the sample's theme page changes them live |
| What a speed is | **A multiplier on the motion's time**: 1 is the phone, 2 twice as fast. An area's own speed, when set, replaces `MotionSpeed` (not multiplied by it) | One number an app understands. A replacement, not a product, so "the tilt at the phone's speed while the rest is quicker" is `TiltSpeed = 1` |
| The areas | **Popups** (Flyout, MenuFlyout, the pickers' flyouts, ContentDialog, the suggestion list: the popup host), **pages** (the page transitions), **the tilt** (its hold and return), **Pivot and Hub** (their moves, the Hub's loop, entrance turn and background fade, the Pivot's header transitions), **controls** (the AppBar and CommandBar, the LoopingSelector's move and wheel expansion, the ToggleSwitch's knob, the ListView's check boxes, the inline ComboBox's open, the scroll indicators' fade) | The issue's four, and one for the rest on `CornflowerClock`, so every motion has an area. Finer areas can split from these later without breaking an app |
| How a speed reaches a motion | **`CornflowerClock` scales the motion's time** (`Since`, `AtSpeed`, `RealTime`, a `Schedule` that takes a speed), with the speed read from `CornflowerConfig.SpeedOf` as the motion starts: each motion keeps its numbers and reads its time through it. The theme's Avalonia transitions (the ToggleSwitch's knob, the ListView's check boxes, the Pivot's headers), which run on Avalonia's own clock and start inside a property change, have their `Duration` set instead (`PhoneTransitions`), when they are placed and again whenever a setting changes | One place does the scaling; the motions' numbers, and every test of them at the defaults, are untouched. At 1 the time is the real time exactly |
| `Animated` off | **Each motion is at its end at once**: a popup opens at rest and closes at once; a page transition ends at once; the Hub doesn't turn and its background shows at once; the MenuFlyout opens at rest; the tilt doesn't tilt; the AppBar is there at once; the rest run at a speed of a million, so they end within a frame for each of their phases; Avalonia's transitions take 0 | "Off" should mean no motion, and a motion that would show its first frame (a popup's start angles) for a frame would flash. The tilt is the phone's press feedback, as the ripple is Ionic's, which Ionic's `animated` turns off |
| What keeps moving with `Animated` off, and at any speed | **The ProgressRing's and ProgressBar's indeterminate loops**, the user's only sign something is happening (as `AvonicConfig` keeps its spinner); **the scroll indicators' timers** (how long they stay after a scroll), as Avalonia's own scrolling reads nothing; **input timings** (the ToggleSwitch's hold, a swipe's speed, the LoopingSelector's flick velocity), which measure the finger, not a motion | They aren't transitions, or aren't Cornflower's to change |
| A departure | **`CornflowerConfig.Deviations.<Name>`**, a named setting off by default, recorded under "Departures" in its control's README, with a test that it does what it says and one that the default is the phone's. The first is `MenuFlyoutSingleStepOpen` | A departure an app can see by name and turn off, never a silent change to the default |
| Back to the phone's | **`CornflowerConfig.Reset()`**, public | One call an app (and the sample's "the phone's" button) can make that stays right as deviations are added |
| The sample | **Starts at `MotionSpeed` 2** (`App.StartMotion`, from each head), so trying it feels quick; its theme page shows the settings and changes them at runtime. `tools/android/run.ps1` (`--es motion phone`) and `tools/real-window/drive.ps1` (`--phone-motion`) start it at the phone's, and the tests, frames and `PreviewGallery` never start the sample's motion, so they see the defaults | The sample is for trying; the runs and the tests are the reference's |

## Consequences

- Every existing test, at the defaults, is the spec for "faithful by default"; none changed.
- A new motion on `CornflowerClock` reads its area's speed when it starts and its time through `CornflowerClock.Since`
  (or `AtSpeed`, or `Schedule` with the speed), and says what `Animated` off does to it.
- A new departure is a new `Deviations` setting, never a changed default; changing a default needs an ADR.
