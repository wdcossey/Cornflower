# RefreshContainer

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#241](https://github.com/wdcossey/Cornflower/issues/241) · its look on the Pixel done 2026-11-26 (no timings)

Avalonia's `RefreshContainer`, themed by type in `src/Cornflower.Themes.Wp81/Controls/RefreshContainer.axaml`.
8.1 had no pull-to-refresh of its own: apps built one on a `ScrollViewer`. Here a pull down on the list opens a band
at the container's top with Wp81's [`ProgressRing`](../ProgressRing/README.md) in it, Wp81's
[`RefreshVisualizer`](../RefreshVisualizer/README.md).

## Usage

```xml
<RefreshContainer RefreshRequested="OnRefreshRequested">
  <ScrollViewer>
    <ItemsControl ItemsSource="{Binding Items}" />
  </ScrollViewer>
</RefreshContainer>
```

```csharp
private async void OnRefreshRequested(object? sender, RefreshRequestedEventArgs e)
{
    var deferral = e.GetDeferral();
    await LoadAsync();
    deferral.Complete();   // the ring stops and the band goes
}
```

The content must hold a `ScrollViewer` (Avalonia's adapter pulls the first it finds). A pull is touch on the phone;
on a desktop set `IsMouseEnabled="True"` to pull with the mouse, or call `RequestRefresh()` (a button, a key).

## What it sets

| Property | Value |
|---|---|
| `Background` | transparent: the page shows |
| `Template` | Simple's, its root `PART_Root` clipped (`ClipToBounds`) |

The visualizer waits its own height above the container, and the clip keeps it out of sight there, so it never covers
what is above the container on the page; a pull brings it in from the container's top edge, pushing the list down.

## The template's parts

- `PART_Root`: the clipped `Grid`.
- `PART_ContentPresenter`: the content (the list).
- `PART_RefreshVisualizerPresenter`: where Avalonia puts the visualizer, over the content at its top.

## Known gaps

- A `PullDirection` other than `TopToBottom` works as Avalonia's does, untried on the phone.
- The list doesn't ease back when a refresh ends: it goes back at once (rule 8; [RefreshVisualizer](../RefreshVisualizer/README.md)).
