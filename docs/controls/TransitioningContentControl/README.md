# TransitioningContentControl

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#236](https://github.com/wdcossey/Cornflower/issues/236) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `TransitioningContentControl`, themed by type in
`src/Cornflower.Themes.Wp81/Controls/TransitioningContentControl.axaml`. The phone's nearest is a `Frame`'s page
transitions. This has none of its own (ADR 0007's answer 6): new content shows at once, unless the app sets a transition.

## Usage

```xml
<TransitioningContentControl Content="{Binding Current}">
  <TransitioningContentControl.PageTransition>
    <wp81:TurnstileTransition />
  </TransitioningContentControl.PageTransition>
</TransitioningContentControl>
```

## What it sets

| Property | Value |
|---|---|
| `PageTransition` | null (Avalonia's default is a 125 ms cross-fade). An app's own wins |

## Resource keys

None. Wp81's `TurnstileTransition`, `SlideTransition` and `ContinuumTransition` are there to pick (`xmlns:wp81="using:Cornflower.Themes.Wp81"`); they read their time through `CornflowerClock`, so `CornflowerConfig.Animated` stops them.

## The template's parts

`PART_ContentPresenter` and `PART_ContentPresenter2`, the two Avalonia's control swaps the content between, in a `Panel`.
Each is bound to the app's `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`, `Padding`, content alignments and
`ContentTemplate`, all unset, with the phone's 1.33 em line for the text it makes (`PhoneHeaderLine`).

## Known gaps

- An app's `CrossFade` or `PageSlide` (Avalonia's) runs on Avalonia's clock, not `CornflowerClock`: `CornflowerConfig.Animated`
  doesn't stop it.
