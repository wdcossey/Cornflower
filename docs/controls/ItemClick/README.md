# ItemClick and IsItemClickEnabled

Status: done · [#132](https://github.com/wdcossey/CornflowerUI/issues/132) · compared against
[#113](https://github.com/wdcossey/CornflowerUI/issues/113)'s probes (runs `20261115-1941-dev11`, `-1948-`) · owed:
a probe of item click in the check mode, Extended and None · owed: the device run on the Pixel 8 · owed: the
real-window run

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
- **Selection.** With item click on, a tap, click or key selects nothing, whatever the `SelectionMode`, unless the
  list is in the check mode: `SelectionMode="Multiple,Toggle"`, WinRT's Multiple, where a tap checks the item and
  clicks nothing. `Multiple` alone, WinRT's Extended, isn't the check mode (#142): a tap clicks, as in Single. A selection set in code stands. WinRT's `SelectionMode="None"` with
  `IsItemClickEnabled` is Single here: no tap selects.
- `ClickedItem` is the list's item, the data, not its `ListBoxItem`.
- `ItemClick` is a direct routed event raised on the list, as WinRT's is the list's own; its sender and source are
  the list. A control in an item that takes its own press, a Button or a TextBox, takes the tap; the item isn't
  clicked.
- It works under any theme: it decides what a tap does, not how anything looks.

## Known gaps

- Arrow keys move the selection, as Avalonia's ListBox does.
- A pointer an app captures to the `ListBoxItem` itself selects, as before, and doesn't click; the item template
  root's own handlers don't see a press or release the list takes; a `ListBox` subclass's own selection rule isn't
  followed (PLAN, "How a click is told").
- Not read on the phone: item click in the check mode, with Extended and with None; the keyboard. What we do for
  each is in the plan, "Decided on the user's behalf".
