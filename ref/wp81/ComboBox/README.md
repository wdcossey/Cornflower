# ComboBox, from the emulator

The reference app's captures of `ComboBox { Header = "combo box" }`, dark and light, on the reference profile.

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile ([ADR 0001](../../../docs/decisions/0001-what-faithful-means.md)) |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4 |
| Stage | the page's background, the ComboBox in a slot 19 from the left and 12 from the top, stretched: 450 × 140 view pixels closed, 450 × 400 open |

- `closed__<state>`: items "one", "two", "three", "one" selected ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs),
  variant `closed`), at rest (`base`) and each state forced with `VisualStateManager.GoToState` on a fresh instance.
  Run `20261108-1705-dev11`, 2026-11-08. Of the run's 22 closed captures, those the comparison uses, and Highlighted
  and Opened (forced on a closed box, so not the open look). `Pressed` is the state forced with no finger: its tilt is
  no touch's (as the Button's). `PresenterStates.Full` is forced on a 3-item box, so its button shows the placeholder.
- `open-<n>-items__stage`: items "item 1" to "item n", "item 1" selected, opened with `IsDropDownOpen`
  ([`tools/refapp/app/Popups.cs`](../../../tools/refapp/app/Popups.cs), `Combo`). Run `20261109-2039-dev11`,
  2026-11-09. 3 and 5 open inline; 6 to 10 open the full-screen picker, whose look is the ListPickerFlyout's
  (`ref/wp81/ListPickerFlyout/`).

The template-tree dumps stay outside the repository ([ADR 0002](../../../docs/decisions/0002-decisions-of-2026-11-08.md));
the numbers read from them are in [`docs/controls/ComboBox/PLAN.md`](../../../docs/controls/ComboBox/PLAN.md) and
`tests/Cornflower.Tests/Controls/ComboBoxReadings.cs`.
