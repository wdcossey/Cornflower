# PipsPager

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#241](https://github.com/wdcossey/Cornflower/issues/241) · a look on the Pixel is owed (no timings)

Avalonia's `PipsPager`, themed by type in `src/Cornflower.Themes.Wp81/Controls/PipsPager.axaml`. 8.1 had nothing like
it (a FlipView showed no marks), so this is Metro's flat mark in its place: a row of small squares, the selected one in
the accent, with an arrow either side.

## Usage

```xml
<PipsPager NumberOfPages="5" SelectedPageIndex="{Binding Page}" />
<PipsPager NumberOfPages="12" MaxVisiblePips="5" />
<PipsPager NumberOfPages="4" Orientation="Vertical" IsPreviousButtonVisible="False" IsNextButtonVisible="False" />
```

## What it looks like

| Part | Look |
|---|---|
| A mark | a square `CornflowerPlainPipSize` (9.5) across, centred in a square `CornflowerPlainPipTargetSize` (38.5), which takes the finger; the squares touch |
| Unselected | `PhoneLowBrush`, the foreground at the phone's low opacity |
| Selected | `PhoneAccentBrush`: a selection is state (rule 3); disabled, `PhoneDisabledBrush` |
| Pressed | `PhoneForegroundBrush`, the full foreground, on an unselected mark |
| The arrows | plain `Button`s in `Wp81PlainGlyphButtonTheme` (the plain glyph part), each a 38.5 square, Lucide's `ChevronLeft` and `ChevronRight` (`ChevronUp` and `ChevronDown` when vertical); pressed, the accent fill and the tilt (rule 7: a button-like part); at the first or last page, the arrow that can't go on is disabled and dimmed, not hidden |
| `Orientation` | `Vertical` stacks the arrows and the marks in a column |

No circles, no shadows, no tilt on a mark (smaller than an item) and no motion of its own (rule 8): Simple's colour
transitions on the marks and the arrows are left out. With more pages than `MaxVisiblePips`, the marks pan to keep the
selected one in view: that slide is Avalonia's `PipsPager` code, not the theme's, and is kept.

## The template's parts

- `PART_RootPanel`: a `StackPanel` in the pager's `Orientation`.
- `PART_PreviousButton` and `PART_NextButton`: plain `Button`s, their themes the pager's `PreviousButtonTheme` and
  `NextButtonTheme` (an app can give its own).
- `PART_PipsPagerList`: a `ListBox` in `Wp81PlainPipsListTheme` (this file's): Wp81's bar-less `ScrollViewer`,
  `AutoScrollToSelectedItem` off and `ListBoxOptions.KeepsScrollInList` on, so neither a selection at load, a
  `SelectedPageIndex` set in code nor a tap on a mark scrolls the page round the pager (#220). Its items wear
  `Wp81PlainPipTheme`.

A `PipsPager` given Simple's theme keeps Simple's Buttons (`HoleParts`).

## Known gaps

- The arrows have no labels of their own beyond Avalonia's automation names ("Previous page", "Next page").
- A keyboard's arrow keys move the selection as Avalonia's do; untried on the phone.
