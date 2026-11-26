# RefreshVisualizer

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#241](https://github.com/wdcossey/Cornflower/issues/241) · a look on the Pixel is owed (no timings)

Avalonia's `RefreshVisualizer`, themed by type in `src/Cornflower.Themes.Wp81/Controls/RefreshVisualizer.axaml`: the
band a [`RefreshContainer`](../RefreshContainer/README.md)'s pull opens. Its sign is Wp81's own
[`ProgressRing`](../ProgressRing/README.md), the phone's sign of work in one place (ADR 0007, "The refresh sign"): the
control and its theme by type, in the accent.

## What a pull shows

Avalonia's visualizer moves through `RefreshVisualizerState`; the band moves down with the pull, as far as its own
height (100, the container's default), and the list with it.

| State | When | The ring |
|---|---|---|
| `Idle` | at rest, or a pull let go short | off; the band out of sight above the container |
| `Interacting` | pulled, short of 0.8 of the band (letting go does nothing) | running, at `CornflowerPlainRefreshPullOpacity` (0.4) |
| `Pending` | pulled past 0.8 (letting go refreshes) | running, at full strength |
| `Refreshing` | let go there, or `RequestRefresh()` | running, at full strength, until the app's deferral completes |
| `Peeking` | the band shown with no finger on it | running, at full strength |

## What it sets

| Property | Value |
|---|---|
| `Background` | `PhoneBackgroundBrush`: the page's own surface |
| `Foreground` | `ProgressBarIndeterminateForegroundThemeBrush` (the accent), the ring's |
| `Content` | an empty `Panel` (below) |
| `IsHitTestVisible`, `IsTabStop` | false |
| `PlainRefreshMotion.IsEnabled` | true (below) |

## No motion of its own (rule 8)

Avalonia's visualizer turns its `Content` with the pull, fades it, pulses it to 1.5 at `Pending`, spins it every
0.5 s while refreshing, and eases each of those over 100 ms; its container eases the band and the list over 150 ms
after the finger and slides them back when a refresh ends. None of that is the pull's or the ring's, so:

- The `Content` is an empty `Panel`, which takes Avalonia's turn, fade, pulse and spin unseen. The ring is the
  template's own `PART_Ring`, and moves only as the ring does.
- `PlainRefreshMotion` (internal) takes the implicit animations off the band's and the list's composition visuals at
  each change of state, before Avalonia moves them, so they follow the finger as it is and go back at once.

## The template's parts

- `PART_Root`: a `Grid` on the background, which Avalonia finds and puts the `Content` in.
- `PART_Ring`: Wp81's `ProgressRing`, 57.5 square, centred; its `IsActive` and `Opacity` are set by state.

## Known gaps

- An app's own `Content` is turned, faded, pulsed and spun as Avalonia does, over the ring.
- An app's own visualizer (`RefreshContainer.Visualizer`) has no height from the theme: the container sets 100 only
  on its default one, so give an app's a `Height`.
- `Peeking` is shown as Avalonia raises it, untried on the phone.
