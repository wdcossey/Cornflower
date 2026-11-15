# ItemClick and IsItemClickEnabled: comparison

Item click has no look, so there are no frames: the comparison is of events. The phone's are #113's probe logs, runs
`20261115-1941-dev11` (12 items) and `20261115-1948-dev11` (40 items), a ListView with `IsItemClickEnabled` in Single
mode on the reference profile ([PLAN](PLAN.md#where-the-behaviour-comes-from)). Ours are the tests in
`tests/Cornflower.Tests/Controls/ItemClickTests.cs`, headless, on the same page: a ListBox 19 in and 80 down a
450 × 800 view at 2.4, with the probes' press points (`ListViewReadings.Presses`).

| The finger | Phone: `ItemClick` | Phone: selection | Ours: `ItemClick` | Ours: selection |
|---|---|---|---|---|
| Held 600 ms at each of the ten probed points | one, the item under the finger, with the release (20 of 20) | none | one, that item, with the release | none |
| Tapped 80 ms on item 4 | one, with the release (6 of 6) | none | one, with the release | none |
| Tapped 150 ms on item 4 | one, with the release (4 of 4) | none | one (the 600 ms presses cover the length) | none |
| Held 300 ms, dragged 80 px up, the list pans | none (3 of 3, and the flicks) | none | none: the pan took the pointer | none |
| Dragged off the item, the list can't pan | none | none | none | none |
| Moved 10 px sideways over 300 ms | one | none | one | none |
| The press tilts | the Button's tilt (#113) | | the Button's tilt (#119), held while the finger is down, back at rest after the release | |

The order: on the phone `ItemClick` comes before the list's own `PointerReleased` handler sees the release and before
`Tapped`. Ours is raised as the release reaches the item, so before a handler on the list, and before Avalonia's
`Tapped`, which its gestures raise from the same release.

## Not compared

- The check mode, Extended and None with item click on: not probed (owed, PLAN).
- The keyboard: the phone has none.
- The device run on the Pixel 8: owed.
