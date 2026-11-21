using Avalonia;
using Avalonia.Controls;

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
    /// layout or not; with this on, a request the list's own ScrollViewer didn't take stops at the list.
    /// </para>
    /// <para>
    /// The focus still goes through: a request for the focused element goes on to the page, so an item focused by the
    /// keyboard (an arrow key, Tab) is brought into view in every ScrollViewer round it, as an item given the keyboard's
    /// focus is on the phone. What stays Avalonia's, in the list itself: it scrolls to a selection at load, and to one set
    /// in code with no focus in it, where the phone's list doesn't move.
    /// </para>
    /// </remarks>
    public static readonly AttachedProperty<bool> KeepsScrollInListProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("KeepsScrollInList", typeof(ListBoxOptions));

    public static bool GetKeepsScrollInList(ListBox list) => list.GetValue(KeepsScrollInListProperty);

    public static void SetKeepsScrollInList(ListBox list, bool value) => list.SetValue(KeepsScrollInListProperty, value);

    private static void HookIntoView() =>
        Control.RequestBringIntoViewEvent.AddClassHandler<ListBox>(OnRequestBringIntoView);

    /// <summary>
    /// A request from inside the list (a bubbling request's target is on its route) that its own ScrollViewer didn't take
    /// (one that moved it is handled there): stopped, unless it is the focus's.
    /// </summary>
    private static void OnRequestBringIntoView(ListBox list, RequestBringIntoViewEventArgs e)
    {
        // The list's own request (list.BringIntoView()) is the page's to take.
        if (!GetKeepsScrollInList(list) || e.TargetObject is not { } target || ReferenceEquals(target, list))
            return;

        // The focus's own request: ScrollViewer brings the focused element into view on a focus change.
        if (ReferenceEquals(TopLevel.GetTopLevel(list)?.FocusManager?.GetFocusedElement(), target))
            return;

        e.Handled = true;
    }
}
