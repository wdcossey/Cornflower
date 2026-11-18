# ItemClick and IsItemClickEnabled

Status: done · [#132](https://github.com/wdcossey/Cornflower/issues/132) · compared against
[#113](https://github.com/wdcossey/Cornflower/issues/113)'s probes (runs `20261115-1941-dev11`, `-1948-`) · item click in
each mode read in [#148](https://github.com/wdcossey/Cornflower/issues/148) (`listclick`, `20261116-1717-dev11`, #170):
every mode matches, the check mode since [#169](https://github.com/wdcossey/Cornflower/issues/169) · owed for #169: the
device run on the Pixel 8 and the real-window run · device run passed 2026-11-16 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page itemclick` on `main` at `ae652d3`, 22:48 to 22:50, cold starts 716 to 826 ms over the run's pages: the page opened in 406 ms) · real-window run passed 2026-11-16 (`tools/real-window/drive.ps1`, Debug, on #161's head `d0a9ef6`: 160 of 160, every page opened from the list, ready, and left by Back)

WinRT's `IsItemClickEnabled` and `ItemClick` on the 8.1 ListView, for Avalonia's `ListBox`, which lacks them: an
attached behaviour in `Cornflower.Controls`, `ListBoxOptions`. With item click on, a tap on an item raises
`ItemClick` on the list with the item clicked, and selects nothing, as on the phone. The plan, with what the phone
did and where it was read, is [PLAN.md](PLAN.md); the phone's events beside ours are in
[COMPARISON.md](COMPARISON.md). The list's look is the [ListView](../ListView/README.md)'s.

## Usage

```xml
<ListBox ItemsSource="{Binding Pages}"
         cf:ListBoxOptions.IsItemClickEnabled="True"
         cf:ListBoxOptions.ItemClick="OnItemClick" />
```

```csharp
private void OnItemClick(object? sender, ItemClickEventArgs e) => Open(e.ClickedItem);

// or, in code:
list.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => Open(e.ClickedItem));
```

with `xmlns:cf="using:Cornflower.Controls"`.

- **A click** is a tap let go on the item, the mouse's left button pressed and let go on it, or Space or Enter
  pressed and let go on the focused item (once, however long it is held). A pan, or a press dragged off the item,
  clicks nothing; the press tilts either way.
- **Selection.** With item click on, a tap, click or key clicks and selects nothing, whatever the `SelectionMode`,
  as on the phone (read in #148). That includes the check mode, `SelectionMode="Multiple,Toggle"`, WinRT's Multiple:
  the boxes show, a tap on the box or off it clicks the item and leaves it unchecked (#169), and a press on the box
  still tilts the box alone. An app that lets the user check items turns item click off while the check mode is on,
  as the phone's apps do, and as the sample's "select" does. `Multiple` alone, WinRT's Extended, clicks as Single
  does (#145). Ctrl+A, which Avalonia's ListBox takes in any mode with `Multiple`, selects nothing. A selection set
  in code stands. WinRT's `SelectionMode="None"` with `IsItemClickEnabled` is Single here: no tap selects.
- `ClickedItem` is the list's item, the data, not its `ListBoxItem`.
- `ItemClick` is a direct routed event raised on the list, as WinRT's is the list's own; its sender and source are
  the list. A control in an item that takes its own press, a Button or a TextBox, takes the tap; the item isn't
  clicked.
- It works under any theme: it decides what a tap does, not how anything looks.

## Known gaps

- Arrow keys move the selection, as Avalonia's ListBox does, and with Shift in `Multiple` extend it.
- A pointer an app captures to the `ListBoxItem` itself selects, as before, and doesn't click; the item template
  root's own handlers don't see a press or release the list takes; a `ListBox` subclass's own selection rule isn't
  followed (PLAN, "How a click is told").
- Not read on the phone: the keyboard (the phone has none). What we do is in the plan, "Decided on the user's
  behalf".
