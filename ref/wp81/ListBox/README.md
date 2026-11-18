# ListBox and ListBoxItem, from the emulator

The reference app's captures ([`tools/refapp/app/Catalog.cs`](../../../tools/refapp/app/Catalog.cs)) of a
`ListBoxItem { Content = "list box item" }` on its own, `default`, at rest (`base`) and in states of its template forced
with `VisualStateManager.GoToState` on a fresh instance; and of a `ListBox` of "one", "two" and "three" with the second
selected, `three-items`, at rest and `Disabled`. Dark and light. Copied unchanged from refapp run `20261118-2210-dev11`
(#211).

| | |
|---|---|
| Emulator | Emulator 8.1 U1 1080P 5.5 inch (device 11), the reference profile |
| Resolution | 1080 × 1920 raw pixels; `RawPixelsPerViewPixel` 2.4, so the view is 450 × 800 |
| Stage | the page's background; the control 19 from the left and 12 from the top; 450 × 100 for the item, 450 × 260 for the list |
| Date | 2026-11-18, 22:08 to 22:10 UTC |

Copied: the list's `base` and `CommonStates.Disabled`; the item's `base`, `CommonStates.Pressed`, `CommonStates.Disabled`,
`SelectionStates.Selected` and `SelectionStates.SelectedDisabled`. The rest are byte for byte one of these: `Normal`,
`PointerOver` and `Unselected` are `base`; `SelectedUnfocused`, `SelectedPointerOver` and `SelectedPressed` are
`Selected`; the `selected` variant (an item with `IsSelected` outside a list) draws as `default`.

`CommonStates.Pressed` is forced with no finger on the item, so its `PointerDownThemeAnimation` left `InnerGrid` tilted by
−17.19°, 17.19°, which no touch gives (as the ListView's, [`../ListView/README.md`](../ListView/README.md)); it is kept for
the `PressedBackground` fill. The numbers read from the dumps are in
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "ListBox and ListBoxItem".
