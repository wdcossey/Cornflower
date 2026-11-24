# PathIcon

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#236](https://github.com/wdcossey/Cornflower/issues/236) · a look on the Pixel is owed (no timings)

Avalonia's `PathIcon`, themed by type in `src/Cornflower.Themes.Wp81/Controls/PathIcon.axaml`. The phone's `PathIcon`
was an untemplated icon element; Cornflower's own icons are `LucideIcon`. Here a `PathIcon` draws its `Data` in its
`Foreground` and nothing else, at its geometry's size unless the app sizes it.

## Usage

```xml
<Button>
  <PathIcon Data="M4,0 H6 V4 H10 V6 H6 V10 H4 V6 H0 V4 H4 Z" Width="20" Height="20" />
</Button>
```

## What it sets

| Property | Value |
|---|---|
| `Template` | the `Data` in the `Foreground`, scaled to fit (below) |
| `Background` | `Transparent`, as Simple's, so the whole box takes the pointer |
| `HoleParts.FallBack` | true: in a template written for the theme under Wp81, that theme's `PathIcon` |

## Resource keys

None. It sets no foreground, so it inherits its host's (a pressed button's), and no size, where Simple's fixes one.

## The template's parts

- `PART_Border`: the app's `Background`, unset.
- `PART_Path`, in `PART_Viewbox`: the `Data`, filled with the `Foreground`. Unsized (no `Width` and no `Height`), it is
  the geometry's own size, shrunk to fit a smaller slot but never grown to fill a larger one. Given a `Width` or a
  `Height`, it is scaled to fit, its shape kept, in the middle.

## Known gaps

- With no size, the icon is its shape's bounds, not its grid's: a geometry drawn on a 24 grid with room round it comes
  out smaller, without that room. Give it a `Width` and `Height`.
- Simple's `DrawerPage` holds a `PathIcon` with no size; it keeps Simple's (`HoleParts`) until the drawer's batch.
