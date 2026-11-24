# ADR 0007 — Plain themes

**Date:** 2026-11-24 · **Status:** Proposed in [#246](https://github.com/wdcossey/Cornflower/pull/246), for [#216](https://github.com/wdcossey/Cornflower/issues/216). The maintainer took every recommendation of the
plan ([`docs/spikes/plain-themes-plan.md`](../spikes/plain-themes-plan.md)) on 2026-11-24 (the issue's last comment); the
details the plan left open were decided on the maintainer's behalf (the PR lists them), and are accepted when the PR is
merged.

## Context

Avalonia has templated controls the phone never had. Wp81 doesn't theme them, so today they fall back to Simple (spike 2,
`docs/spikes/README.md`), and `tests/Cornflower.Tests/Completeness/Holes.txt` lists them: 49 at Avalonia 12.1.4, held by
`HolesTests`. With the two free first-party packages, `Avalonia.Controls.DataGrid` (6 controls) and
`Avalonia.Controls.ColorPicker` (5), that is 60 controls. [ADR 0002](0002-decisions-of-2026-11-08.md) said each gets a
plain theme in the era's style, labelled as ours and not a reproduction, in a late tier.
[ADR 0004](0004-larger-screens-as-an-extension-pack.md) keeps them apart from the Adaptive pack.

A plain theme has no reference to measure. Rule 4 of CLAUDE.md ("Tests assert the reference's behaviour") and
[ADR 0001](0001-what-faithful-means.md) items 2 (every number from the templates or a capture) and 6 (names follow WinRT)
assume one, so plain themes need rules of their own, settled before the first batch. The plan (#216, PR #233) has the
list, the 8.1 equivalents, the sizes and the options; this ADR is what was chosen.

## Decision

**A plain theme is Cornflower's design in the era's style, built from the phone's existing tokens, labelled as ours, and
done without measurement.**

### The rules

1. **Labelled as ours.** Each plain theme's README (`docs/controls/<Name>/README.md`) opens with the line "Plain theme:
   Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone." The theme file
   says the same in its header comment. A new resource key a plain theme needs is prefixed **`CornflowerPlain`**, never a
   WinRT-shaped name (`...ThemeBrush`, `...ThemeThickness`), which in this repo means "the phone's value". A keyed theme
   for a part is **`Wp81Plain<Part>Theme`**. The theme by type stays keyed by type, so it fills the hole and `HolesTests`
   sees it.
2. **Type ramp: existing keys only.** Content in `BodyTextBlockStyle`'s size and family; a control's own header in
   `ControlHeaderTextBlockStyle`; a group's heading in `GroupHeaderTextBlockStyle`; small print in
   `ControlContextualInfoTextBlockStyle`; a tab's header in the Pivot header's keys. Sizes by `DynamicResource`.
3. **Accent, as the phone uses it**: for what is pressed, selected, checked, on, in progress or a link, never as
   decoration at rest. Always `PhoneAccentBrush`.
4. **Borders: the phone's two thicknesses, square.** `PhoneBorderThickness` (2.5) round a box a finger presses or types
   in; 1.5 for a line that divides or edges a surface. Corner radius 0, no shadows, no gradients.
5. **Backgrounds.** Transparent at rest over the page's `ApplicationPageBackgroundThemeBrush`; a menu on the MenuFlyout's
   `MenuFlyoutBackgroundThemeBrush`; another floating surface on `FlyoutBackgroundThemeBrush`; disabled is
   `PhoneDisabledBrush`. Nothing translucent.
6. **Touch sizes stay the phone's** (`PhoneButtonMinHeight`, `PhoneTouchTargetOverhang`) unless it is a part inside a
   larger control. No phone width written into a template (ADR 0004).
7. **The tilt on presses of item-sized things** (a button-like part, a tree or menu item, a tab header, a day in a
   calendar); none on large surfaces or on what is dragged.
8. **No motion of their own.** Simple's transitions are left out; a popup does what Wp81's popup host already does. Any
   motion added later reads its time through `CornflowerClock` under an [ADR 0006](0006-faithful-defaults-and-cornflowerconfig.md)
   area, so `CornflowerConfig.Animated` stops it.
9. **Dark and light.** Every brush from the `Dark` and `Light` dictionaries in `Tokens/Colors.axaml`; no colour in a
   theme.
10. **Glyphs** from the theme's `Glyphs` (our own drawings) or from `LucideIcon` (ISC, already in the repo). Lucide in a
    plain theme is a new use, so each README records it. No new font.
11. **Done, for a plain theme**: the theme's tests (templated, both variants, the key prefix, `HoleParts.cs`' list where
    it changes), a frame in the preview, and a look on the Pixel 8, with no measurement and no timings. Its line leaves
    `Holes.txt`, as for any control. When the Pixel is away the local emulator stands in and the Pixel look is owed, as for
    Tier 4's runs.

### The answers

| Question | Decision |
|---|---|
| Which controls | **57**: the 60, less the file chooser and its overwrite prompt (`ManagedFileChooser`, `ManagedFileChooserOverwritePrompt`), dropped: apps use the platform's chooser, and the two stay holes in `Holes.txt`; and less `TextSelectionHandle`, the phone's own grippers, a reproduction of its own ([#245](https://github.com/wdcossey/Cornflower/issues/245)) |
| The order | **Batches 1 to 11** of the plan's section 4, batch 1 first: [#234](https://github.com/wdcossey/Cornflower/issues/234) hosts and small pieces, [#235](https://github.com/wdcossey/Cornflower/issues/235) buttons and the shared parts, [#236](https://github.com/wdcossey/Cornflower/issues/236) text and content, [#237](https://github.com/wdcossey/Cornflower/issues/237) menus, [#238](https://github.com/wdcossey/Cornflower/issues/238) tabs and pages, [#239](https://github.com/wdcossey/Cornflower/issues/239) disclosure and navigation, [#240](https://github.com/wdcossey/Cornflower/issues/240) dates, [#241](https://github.com/wdcossey/Cornflower/issues/241) feedback, [#242](https://github.com/wdcossey/Cornflower/issues/242) tables, [#243](https://github.com/wdcossey/Cornflower/issues/243) DataGrid, [#244](https://github.com/wdcossey/Cornflower/issues/244) ColorPicker |
| Where they live | **In `Wp81Theme`**, labelled per control. The DataGrid and ColorPicker themes go in an assembly each (`Cornflower.Themes.Wp81.DataGrid`, `Cornflower.Themes.Wp81.ColorPicker`, working names settled in batches 10 and 11), so a phone app doesn't take those packages |
| Density | **The phone's type and touch sizes everywhere**, the dense controls included, with the sizes as `DynamicResource` hooks so the Adaptive pack can make them denser on wide windows. Tweaks for mobile after the first look are expected; each is a `CornflowerPlain` key with its reason in its PR |
| The tilt | **On item-sized presses only** (rule 7) |
| Motion | **None of their own** (rule 8). `TransitioningContentControl` has no transition unless the app sets one (Wp81's `TurnstileTransition`, `SlideTransition` and `ContinuumTransition` are there to choose) |
| `ContextMenu` | **The MenuFlyout's look and motion**, what an 8.1 app showed on hold, labelled as plain since `ContextMenu` isn't the phone's. The maintainer reviews it once batch 4 has it; a `CornflowerConfig` setting for a plain menu may follow from that review, off by default |
| `TabControl` | **The Pivot's headers, without its swipe** |
| New tokens | Two: **`CornflowerPlainErrorBrush`**, from `CornflowerAccentRedColor` in both variants, for `DataValidationErrors` (added in batch 8); and **a 1.5 line thickness on all sides** (`CornflowerPlainLineThickness`), since the existing 1.5 keys are one-sided (added in batch 1, with `Separator`). Both go under `Tokens/`; this ADR approves adding them |
| The label | **The README line and the `CornflowerPlain` and `Wp81Plain` prefixes** (rule 1); ADR 0001 item 6 amended to match |
| Done | **Tests, a preview frame and a look on the Pixel**, no measurement (rule 11) |
| The refresh sign | **`RefreshVisualizer` shows Wp81's `ProgressRing`**, which a pull can turn as it grows |
| PRs | **One PR per batch for batches 1 and 3**, where each control is a few lines; **one per family elsewhere**. Batch 8 splits into validation, notifications, refresh and `PipsPager` |
| The plan | **Stays in `docs/spikes/` as the record of the options**; what was decided is here and in ROADMAP's Tier 4 rows |

## Consequences

- **An exception to CLAUDE.md rule 4 and ADR 0001 item 2, for plain themes only.** A plain theme's tests assert the
  rules above, not a reference; nothing in it claims a measurement, and ADR 0001's "nothing is done without a capture"
  doesn't apply to it. A control the phone had is never a plain theme: `TextSelectionHandle` (#245) is measured as
  any reproduction.
- **ADR 0001 item 6 is amended**: a plain theme's new keys take the `CornflowerPlain` prefix, as there is no WinRT name
  to follow.
- **An exception to CLAUDE.md rule 2** for batches 1 and 3, each one PR of mixed small controls.
- **Shared files stay rule 7's.** A batch that changes `HoleParts.cs` (batches 2, 4, 6, 7 and 8, as their themes replace
  Simple's templates and its inner parts with Wp81's) says so in its PR, and the maintainer approves it case by case.
  The two tokens above are the only shared edit this ADR approves in advance.
- The file chooser's two lines stay in `Holes.txt`, and `TextSelectionHandle`'s until #245 fills it.
- Batch 1 sets the label, the key prefix and their test on cheap themes, so the later batches copy a pattern.
