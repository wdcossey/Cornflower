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

Each mode with item click on, #148's `listclick` (run `20261116-1717-dev11`): the list of `listcheck` with
`IsItemClickEnabled`, against `ItemClickTests`.

| `SelectionMode` (WinRT's) | The taps | Phone | Ours |
|---|---|---|---|
| Multiple (`Multiple, Toggle`), the check mode | item 1's box, its text, item 2's text, 600 ms on item 3 | each clicks its item at the release; nothing checked; no `SelectionChanged` | the same (#169): `In_The_Check_Mode_A_Tap_Clicks_And_Checks_Nothing` |
| Multiple, a press on the box | | the box tilts alone (#136) | the same: `IsBoxPressed`, the box's tilt |
| Extended (`Multiple`) | items 1, 2 and 2 again | each clicks; nothing selected | the same (#145) |
| None (Single here) | item 3 | clicks; nothing selected | the same |
| Single | item 4, twice | each clicks; nothing selected | the same |

The order: on the phone `ItemClick` comes before the list's own `PointerReleased` handler sees the release, and
before `Tapped`. Ours is raised as the release reaches the item's template root, so before a handler on the list.

## Not compared

- The keyboard: the phone has none.
- The device run on the Pixel 8 and the real-window run of #169's change: owed.
