using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Cornflower.Controls;

/// <summary>The data of <see cref="ListBoxOptions.ItemClickEvent"/>: WinRT's <c>ItemClickEventArgs</c>.</summary>
public sealed class ItemClickEventArgs : RoutedEventArgs
{
    public ItemClickEventArgs(object? clickedItem)
        : base(ListBoxOptions.ItemClickEvent)
    {
        ClickedItem = clickedItem;
    }

    /// <summary>The item clicked: the list's item, the data, not its <see cref="ListBoxItem"/>.</summary>
    public object? ClickedItem { get; }
}

/// <summary>
/// WinRT's <c>IsItemClickEnabled</c> and <c>ItemClick</c>, for Avalonia's <see cref="ListBox"/>, which lacks them: with
/// item click on, a tap on an item raises <see cref="ItemClickEvent"/> on the list, with the item clicked, and selects
/// nothing, as on Windows Phone 8.1. Behaviour only; it works under any theme.
/// </summary>
/// <remarks>
/// <para>
/// What a click is, is Avalonia's own rule for what selects an item (<see cref="ItemSelectionEventTriggers"/>): a
/// touch or a pen let go on the item, a mouse's left button let go on it, or Space or Enter on the focused item. Each
/// event Avalonia's item would select on, the list takes instead, and selects nothing: the mouse's press and its right
/// button included. So a pan, which takes the pointer from the item, and a press dragged off it, click nothing, as on
/// the phone (#113); a control in the item that handles the press itself, a Button, takes it as it would.
/// </para>
/// <para>
/// In the check mode, a <see cref="ListBox.SelectionMode"/> with <see cref="SelectionMode.Multiple"/>, a tap checks
/// the item as before and nothing is clicked. A selection set in code stands either way.
/// </para>
/// </remarks>
/// <example>
/// <code>&lt;ListBox cf:ListBoxOptions.IsItemClickEnabled="True" /&gt;</code>
/// <code>list.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) =&gt; Open(e.ClickedItem));</code>
/// </example>
public static class ListBoxOptions
{
    /// <summary>Whether a tap on an item clicks it, not selects it: WinRT's <c>IsItemClickEnabled</c>. Off by default.</summary>
    public static readonly AttachedProperty<bool> IsItemClickEnabledProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsItemClickEnabled", typeof(ListBoxOptions));

    /// <summary>
    /// Raised on the list when an item is clicked: WinRT's <c>ItemClick</c>. It bubbles, as Avalonia's
    /// <see cref="Button.ClickEvent"/> does, so a list in an item of a list reaches the outer list's handlers too, with
    /// the inner list as its source.
    /// </summary>
    public static readonly RoutedEvent<ItemClickEventArgs> ItemClickEvent =
        RoutedEvent.Register<ItemClickEventArgs>("ItemClick", RoutingStrategies.Bubble, typeof(ListBoxOptions));

    static ListBoxOptions()
    {
        // The item selects in its own class handler, the first on the item to see an event on its way up. So the list
        // takes a pointer's event a step before, on the item's template root, after everything in the item has seen it;
        // and a key, whose source is the focused item itself, on its way down to the item.
        InputElement.PointerPressedEvent.AddClassHandler<Interactive>(OnBelowItem);
        InputElement.PointerReleasedEvent.AddClassHandler<Interactive>(OnBelowItem);
        InputElement.KeyDownEvent.AddClassHandler<ListBoxItem>(Take, RoutingStrategies.Tunnel);
    }

    public static bool GetIsItemClickEnabled(ListBox list) => list.GetValue(IsItemClickEnabledProperty);

    public static void SetIsItemClickEnabled(ListBox list, bool value) => list.SetValue(IsItemClickEnabledProperty, value);

    private static void OnBelowItem(Interactive root, RoutedEventArgs e)
    {
        if (root.GetVisualParent() is ListBoxItem item)
        {
            Take(item, e);
        }
    }

    /// <summary>Takes an event the item would select on, if its list clicks items, and clicks it on a release or a key.</summary>
    private static void Take(ListBoxItem item, RoutedEventArgs e)
    {
        if (e.Handled ||
            ItemsControl.ItemsControlFromItemContainer(item) is not ListBox list ||
            !GetIsItemClickEnabled(list) ||
            list.SelectionMode.HasFlag(SelectionMode.Multiple))
        {
            return;
        }

        bool take, click;
        switch (e)
        {
            case PointerReleasedEventArgs released:
                // A click is the left button, or a touch, let go on the item, whichever pointer it is.
                click = released.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased &&
                    new Rect(item.Bounds.Size).Contains(released.GetPosition(item));
                take = click || ItemSelectionEventTriggers.ShouldTriggerSelection(item, released);
                break;
            case PointerPressedEventArgs pressed:
                click = false;
                take = ItemSelectionEventTriggers.ShouldTriggerSelection(item, pressed);
                break;
            case KeyEventArgs key:
                click = take = ItemSelectionEventTriggers.ShouldTriggerSelection(item, key);
                break;
            default:
                return;
        }

        if (!take)
        {
            return;
        }

        e.Handled = true;
        if (click)
        {
            list.RaiseEvent(new ItemClickEventArgs(list.ItemFromContainer(item)));
        }
    }
}
