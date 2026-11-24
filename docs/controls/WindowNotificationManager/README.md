# WindowNotificationManager

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#241](https://github.com/wdcossey/Cornflower/issues/241) · a look on the Pixel is owed (no timings)

Avalonia's `WindowNotificationManager`, themed by type in
`src/Cornflower.Themes.Wp81/Controls/WindowNotificationManager.axaml`: the host the plain
[`NotificationCard`](../NotificationCard/README.md)s stack in.

## Usage

```csharp
var notifications = new WindowNotificationManager(TopLevel.GetTopLevel(this)) { MaxItems = 3 };
notifications.Show(new Notification("Saved", "Your changes are in."));
```

Make one per window and keep it: Avalonia's manager puts itself back in the window's adorner layer whenever the
window's template is applied again. And it drops a notification shown before its template is applied, so show the
first after a layout pass, or call `ApplyTemplate()` on it first, as the sample does.

## What it sets

| Property | Value |
|---|---|
| `Template` | `PART_Items`, a `ReversibleStackPanel` inside `Padding`, at the top, `Spacing` `CornflowerPlainLineWidth` (1.5) |
| `PlainNotificationArea.IsEnabled` | true: in a window's adorner layer, `Padding` is the window's safe area |

The banners run across the whole width, at the top, as 8.1's toast did, the first at the top and each new one under
the last, divided by the 1.5 line (rule 4), the page showing through it. A bottom `Position` (`BottomLeft`,
`BottomRight`, `BottomCenter`) stacks them up from the bottom instead, the first at the edge, as Simple's does. Left,
right and centre all take the full width. No background (the page between and below the banners takes its own
presses) and no motion (rule 8).

**The safe area.** Its host's constructor puts it in the window's adorner layer, which covers the whole window, a
phone's status bar too. There `PlainNotificationArea` (internal) sets its `Padding` to the window's
`InsetsManager.SafeAreaPadding`, at style priority so an app's own `Padding` wins, and keeps it up as the safe area
changes, so the banners sit under the status bar. A manager an app places in its own layout is inside its view's
padding already, and takes none.

## Resource keys

- `CornflowerPlainLineWidth` (batch 2): the gap between banners.

## The template's parts

- `PART_Items`: the panel the cards are added to (Avalonia's `TemplatePart`).

## Known gaps

- `Position`'s left, right and centre are the same: the full width.
- On a wide window the banners are as wide as it; a narrower banner there is for the Adaptive pack (ADR 0004).
