using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Cornflower.Controls;

/// <summary>
/// A list's scroll to an item stays in the list: WinRT's <c>ScrollIntoView</c>, and the scroll to the selection, move
/// the list's own ScrollViewer alone, never the page's round it (#220).
/// </summary>
public static partial class ListBoxOptions
{
    /// <summary>
    /// Whether the list's bring-into-view of one of its items stops at the list, as on Windows Phone 8.1, where
    /// Avalonia's goes on through every ScrollViewer round it. Off by default; the Wp81 list themes (ListView, ListBox,
    /// GridView) turn it on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia's <see cref="ListBox"/> scrolls to its selection (<c>AutoScrollToSelectedItem</c>, at load and on each
    /// change) and to an item asked for (<see cref="ItemsControl.ScrollIntoView(int)"/>) by the item's
    /// <c>BringIntoView</c>. Its request bubbles: the list's own ScrollViewer takes it if it has to move, and if not, it
    /// goes on to the page's. So a selected list low on a page in a ScrollViewer scrolls the page down to it, at load,
    /// and on Android, whose first layout of a page is short, the page stays there. On the phone (the reference app,
    /// #220) neither the selection, at load or set in code, nor <c>ScrollIntoView</c> moves the page, a short first
    /// layout or not; with this on, a request for one of the list's items that its own ScrollViewer didn't take stops
    /// at the list. A request from a control inside an item (a TextBox's caret), and the list's own, go on.
    /// </para>
    /// <para>
    /// The keyboard's focus still goes through: an item given the focus by the keyboard (an arrow key, Tab) is brought
    /// into view in every ScrollViewer round it, as an item given <c>FocusState.Keyboard</c> is on the phone. An item
    /// focused from code (<c>Focus()</c>) or by a press is brought into view in the list alone: the phone's
    /// <c>FocusState.Programmatic</c> brings nothing into view. What stays Avalonia's, in the list itself: it scrolls to
    /// a selection at load, and to one set in code with no focus in it, where the phone's list doesn't move.
    /// </para>
    /// </remarks>
    public static readonly AttachedProperty<bool> KeepsScrollInListProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("KeepsScrollInList", typeof(ListBoxOptions));

    public static bool GetKeepsScrollInList(ListBox list) => list.GetValue(KeepsScrollInListProperty);

    public static void SetKeepsScrollInList(ListBox list, bool value) => list.SetValue(KeepsScrollInListProperty, value);

    /// <summary>
    /// The item the keyboard has just focused, while its <c>GotFocus</c> is routed: each ScrollViewer round it brings it
    /// into view on the way (<c>BringIntoViewOnFocusChange</c>), and those requests go on.
    /// </summary>
    private static ListBoxItem? _keyboardFocused;

    /// <summary>The list a key is on its way to, while it is routed and handled.</summary>
    private static ListBox? _keyInList;

    private static void HookIntoView()
    {
        Control.RequestBringIntoViewEvent.AddClassHandler<ListBox>(OnRequestBringIntoView);
        // On the item, before the event bubbles to the ScrollViewers round it.
        InputElement.GotFocusEvent.AddClassHandler<ListBoxItem>(OnItemGotFocus, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<ListBox>(OnListKeyDownForFocus, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private static void OnItemGotFocus(ListBoxItem item, FocusChangedEventArgs e)
    {
        // Tab, or a key the list is handling: Avalonia's ListBox moves the focus on an arrow key with no method.
        if (!ReferenceEquals(e.Source, item) ||
            e.NavigationMethod is not (NavigationMethod.Tab or NavigationMethod.Directional) &&
            (_keyInList is null || !ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(item), _keyInList)))
            return;

        _keyboardFocused = item;
        // Cleared once this GotFocus has been routed and handled, all of it synchronous.
        Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(_keyboardFocused, item))
                _keyboardFocused = null;
        }, DispatcherPriority.Send);
    }

    /// <summary>A key on its way to a list that keeps its scroll: whatever it focuses, the keyboard focused.</summary>
    private static void OnListKeyDownForFocus(ListBox list, KeyEventArgs e)
    {
        if (!GetKeepsScrollInList(list))
            return;

        _keyInList = list;
        // Cleared once the key has been handled, synchronously.
        Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(_keyInList, list))
                _keyInList = null;
        }, DispatcherPriority.Send);
    }

    /// <summary>
    /// A request for one of the list's own items that its own ScrollViewer didn't take (one that moved it is handled
    /// there): stopped, unless the keyboard has just focused that item.
    /// </summary>
    private static void OnRequestBringIntoView(ListBox list, RequestBringIntoViewEventArgs e)
    {
        if (!GetKeepsScrollInList(list) || e.TargetObject is not ListBoxItem item ||
            !ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(item), list) ||
            ReferenceEquals(item, _keyboardFocused))
            return;

        e.Handled = true;
    }
}
