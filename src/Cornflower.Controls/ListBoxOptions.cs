using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
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
/// Each event Avalonia's item would select on (<see cref="ItemSelectionEventTriggers"/>: a touch or a pen let go on
/// the item, a mouse button pressed on it, Space or Enter on the focused item) the list takes instead, and nothing is
/// selected. A click is a touch, or the mouse's left button, pressed on the item and let go on it, the press reaching
/// the item unhandled; or Space or Enter pressed and let go on the focused item. So a pan, which takes the pointer from
/// the item, and a press dragged off it, click nothing, as on the phone (#113); and a control in the item that takes
/// the press itself, a Button or a TextBox, takes the tap as it would.
/// </para>
/// <para>
/// In the check mode, a <see cref="ListBox.SelectionMode"/> with <see cref="SelectionMode.Multiple"/>, a tap checks
/// the item as before and nothing is clicked. A selection set in code stands either way.
/// </para>
/// <para>
/// A pointer's events are taken on the item's template root, by a class handler, so the root's own handlers don't see
/// an event that is taken. A <see cref="ListBox"/> subclass that overrides when it selects isn't followed: the rule is
/// Avalonia's default. A pointer captured to the <see cref="ListBoxItem"/> itself selects, as before, and clicks
/// nothing.
/// </para>
/// </remarks>
/// <example>
/// <code>&lt;ListBox cf:ListBoxOptions.IsItemClickEnabled="True" cf:ListBoxOptions.ItemClick="OnItemClick" /&gt;</code>
/// <code>list.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) =&gt; Open(e.ClickedItem));</code>
/// </example>
public static class ListBoxOptions
{
    /// <summary>Whether a tap on an item clicks it, not selects it: WinRT's <c>IsItemClickEnabled</c>. Off by default.</summary>
    public static readonly AttachedProperty<bool> IsItemClickEnabledProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsItemClickEnabled", typeof(ListBoxOptions));

    /// <summary>
    /// Raised on the list when an item is clicked: WinRT's <c>ItemClick</c>. A direct event, as WinRT's is the list's
    /// own: a list's handler hears its own items alone, not those of a list inside one of them.
    /// </summary>
    public static readonly RoutedEvent<ItemClickEventArgs> ItemClickEvent =
        RoutedEvent.Register<ItemClickEventArgs>("ItemClick", RoutingStrategies.Direct, typeof(ListBoxOptions));

    /// <summary>The pointer whose press reached the item unhandled, until a left button or a touch is let go.</summary>
    private static readonly AttachedProperty<IPointer?> PressedByProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, IPointer?>("PressedBy", typeof(ListBoxOptions));

    /// <summary>The key whose press the item took, until it is let go.</summary>
    private static readonly AttachedProperty<Key?> KeyDownProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItem, Key?>("KeyDown", typeof(ListBoxOptions));

    /// <summary>How many lists have item click on, so that the pointer's class handlers do nothing while none has.</summary>
    private static int _enabled;

    static ListBoxOptions()
    {
        IsItemClickEnabledProperty.Changed.AddClassHandler<ListBox>((_, e) =>
            _enabled += (e.GetNewValue<bool>() ? 1 : 0) - (e.GetOldValue<bool>() ? 1 : 0));

        // The item selects in its own class handler, the first on the item to see an event on its way up. So the list
        // takes a pointer's event a step before, on the item's template root, after everything inside the item has seen
        // it; and a key, whose source is the focused item itself, on its way down to the item. A press or a release
        // something inside the item handled doesn't reach these handlers, so it isn't recorded or clicked, and a press
        // recorded before is forgotten as the pointer's capture goes with its release.
        InputElement.PointerPressedEvent.AddClassHandler<Interactive>(OnPressed);
        InputElement.PointerReleasedEvent.AddClassHandler<Interactive>(OnReleased);
        InputElement.KeyDownEvent.AddClassHandler<ListBoxItem>(OnKeyDown, RoutingStrategies.Tunnel);
        InputElement.KeyUpEvent.AddClassHandler<ListBoxItem>(OnKeyUp, RoutingStrategies.Tunnel);

        // A press is forgotten when its pointer is taken from the item (a pan) or held (a context menu), and a key's
        // when the item loses the focus, so that a release or a key let go later clicks nothing.
        InputElement.PointerCaptureLostEvent.AddClassHandler<Interactive>(OnCaptureLost, handledEventsToo: true);
        InputElement.HoldingEvent.AddClassHandler<ListBoxItem>(OnHolding, handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<ListBoxItem>(OnLostFocus, handledEventsToo: true);
    }

    public static bool GetIsItemClickEnabled(ListBox list) => list.GetValue(IsItemClickEnabledProperty);

    public static void SetIsItemClickEnabled(ListBox list, bool value) => list.SetValue(IsItemClickEnabledProperty, value);

    /// <summary>The item's list, if it clicks its items now: item click on, and out of the check mode.</summary>
    private static ListBox? Clicking(ListBoxItem item) =>
        ItemsControl.ItemsControlFromItemContainer(item) is ListBox list &&
        GetIsItemClickEnabled(list) &&
        !list.SelectionMode.HasFlag(SelectionMode.Multiple)
            ? list
            : null;

    /// <summary>
    /// The item whose template root this is, while some list has item click on, if the event comes from inside the
    /// root: an event from a popup a control in the item owns is routed through the item but isn't on it.
    /// </summary>
    private static ListBoxItem? ItemOf(Interactive root, RoutedEventArgs e) =>
        _enabled > 0 &&
        root.GetVisualParent() is ListBoxItem item &&
        e.Source is Visual hit &&
        (ReferenceEquals(hit, root) || root.IsVisualAncestorOf(hit))
            ? item
            : null;

    private static void OnPressed(Interactive root, PointerPressedEventArgs e)
    {
        if (ItemOf(root, e) is not { } item || Clicking(item) is null)
        {
            return;
        }

        if (e.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
        {
            item.SetValue(PressedByProperty, e.Pointer);
        }

        if (ItemSelectionEventTriggers.ShouldTriggerSelection(item, e))
        {
            e.Handled = true;
        }
    }

    private static void OnReleased(Interactive root, PointerReleasedEventArgs e)
    {
        if (ItemOf(root, e) is not { } item)
        {
            return;
        }

        // The left button, or a touch, let go on the item, by the pointer whose press reached it.
        var left = e.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased;
        var pressedHere = left && ReferenceEquals(item.GetValue(PressedByProperty), e.Pointer);
        if (left)
        {
            item.ClearValue(PressedByProperty);
        }

        if (Clicking(item) is not { } list)
        {
            return;
        }

        var click = pressedHere && new Rect(item.Bounds.Size).Contains(e.GetPosition(item));
        if (click || ItemSelectionEventTriggers.ShouldTriggerSelection(item, e))
        {
            e.Handled = true;
        }

        if (click)
        {
            // As Avalonia's own click and selection do, by the item's PlatformFeedback.FeedbackType.
            item.PerformFeedback(FeedbackAction.Click);
            Raise(list, item);
        }
    }

    /// <summary>The pointer taken from an element: each item round it forgets that pointer's press.</summary>
    private static void OnCaptureLost(Interactive element, PointerCaptureLostEventArgs e)
    {
        if (_enabled == 0)
        {
            return;
        }

        for (Visual? at = element; at is not null; at = at.GetVisualParent())
        {
            if (at is ListBoxItem item && ReferenceEquals(item.GetValue(PressedByProperty), e.Pointer))
            {
                item.ClearValue(PressedByProperty);
            }
        }
    }

    /// <summary>A held press is a hold (a context menu), not a click.</summary>
    private static void OnHolding(ListBoxItem item, HoldingRoutedEventArgs e)
    {
        if (e.HoldingState == HoldingState.Started)
        {
            item.ClearValue(PressedByProperty);
        }
    }

    private static void OnLostFocus(ListBoxItem item, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, item))
        {
            item.ClearValue(KeyDownProperty);
        }
    }

    private static void OnKeyDown(ListBoxItem item, KeyEventArgs e)
    {
        // Avalonia's rule takes a key only from the item itself, so a key on a control inside it is that control's.
        if (Clicking(item) is not null && ItemSelectionEventTriggers.ShouldTriggerSelection(item, e))
        {
            e.Handled = true;
            item.SetValue(KeyDownProperty, e.Key);
        }
    }

    /// <summary>A key clicks as it is let go, so that a held key, whose press repeats, clicks once.</summary>
    private static void OnKeyUp(ListBoxItem item, KeyEventArgs e)
    {
        if (!ReferenceEquals(e.Source, item) || item.GetValue(KeyDownProperty) != e.Key)
        {
            return;
        }

        item.ClearValue(KeyDownProperty);
        if (Clicking(item) is { } list)
        {
            Raise(list, item);
        }
    }

    private static void Raise(ListBox list, ListBoxItem item) =>
        list.RaiseEvent(new ItemClickEventArgs(list.ItemFromContainer(item)));
}
