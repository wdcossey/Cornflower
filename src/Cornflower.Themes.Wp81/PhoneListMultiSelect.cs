using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's list check mode: WinRT's <c>MultiSelectStates</c>, which Avalonia's <see cref="ListBoxItem"/> doesn't
/// raise. A WinRT ListView in <c>SelectionMode="Multiple"</c> puts every item in <c>ListMultiSelect</c>: its check box
/// slides in from the left and its content 40.5 to the right, and a tap checks or unchecks it. Avalonia's ListBox
/// selects that way with <see cref="SelectionMode.Multiple"/> and <see cref="SelectionMode.Toggle"/> together. A list
/// with <see cref="SelectionMode.Multiple"/> is in the check mode, and <see cref="IsOnProperty"/> says so on each of its
/// items, for the ListViewItem theme's selectors. Turned on by the ListView theme's own setter.
/// </summary>
/// <remarks>
/// <para><see cref="SelectionMode.Multiple"/> without <see cref="SelectionMode.Toggle"/>, WinRT's <c>Extended</c>
/// (chosen with the modifier keys), shows the check boxes too: the phone's <c>Extended</c> wasn't read, and a ListView
/// item draws its selection only in the check mode, so without them a selection of several would be invisible.
/// <see cref="SelectionMode.Toggle"/> alone toggles one item, with no boxes.</para>
/// <para>The list does the work, not each item: it marks each container as it is prepared, and every one it has when
/// <see cref="ListBox.SelectionMode"/> changes or it is turned on or off: the realised ones, the panel's other children
/// (a virtualising panel keeps the focused one there, out of view), its items that are their own containers, and those
/// the panel recycled, which it keeps out of the tree until it reuses them. Marked while out of the tree, where
/// Avalonia runs no transition, a recycled one comes back already in the mode, so its box doesn't slide as it is
/// shown. So it costs a list two handlers and a weak set, a container one property, and no global rule. An item
/// outside a list is never in the mode; an item that is its own container keeps its mark when it is taken out of the
/// list, until a list prepares it again. Turned off (a theme of the app's own on the list), it unmarks every one and
/// lets go.</para>
/// </remarks>
internal static class PhoneListMultiSelect
{
    /// <summary>Set by Wp81's ListView theme: whether the list marks its items.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsEnabled", typeof(PhoneListMultiSelect));

    /// <summary>On an item: whether its list is in the check mode. Read by the ListViewItem theme's selectors.</summary>
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsOn", typeof(PhoneListMultiSelect));

    /// <summary>Each list's containers cleared and not yet prepared again: the ones its panel keeps to reuse.</summary>
    private static readonly ConditionalWeakTable<ListBox, ConditionalWeakTable<Control, object?>> Recycled = new();

    static PhoneListMultiSelect()
    {
        IsEnabledProperty.Changed.AddClassHandler<ListBox>((list, e) =>
        {
            if (e.GetNewValue<bool>())
            {
                list.ContainerPrepared += OnPrepared;
                list.ContainerClearing += OnClearing;
            }
            else
            {
                list.ContainerPrepared -= OnPrepared;
                list.ContainerClearing -= OnClearing;
            }

            MarkAll(list);
            if (!e.GetNewValue<bool>())
                Recycled.Remove(list);
        });
        ListBox.SelectionModeProperty.Changed.AddClassHandler<ListBox>((list, _) =>
        {
            if (list.GetValue(IsEnabledProperty))
                MarkAll(list);
        });
    }

    public static bool GetIsEnabled(ListBox list) => list.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ListBox list, bool value) => list.SetValue(IsEnabledProperty, value);

    public static bool GetIsOn(Control item) => item.GetValue(IsOnProperty);

    /// <summary>Whether <paramref name="list"/> is in the check mode: on, and selecting several.</summary>
    private static bool IsCheckMode(ListBox list) =>
        list.GetValue(IsEnabledProperty) && list.SelectionMode.HasFlag(SelectionMode.Multiple);

    private static void OnPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (sender is not ListBox list)
            return;

        if (Recycled.TryGetValue(list, out var recycled))
            recycled.Remove(e.Container);
        Mark(e.Container, IsCheckMode(list));
    }

    private static void OnClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (sender is ListBox list)
            Recycled.GetOrCreateValue(list).AddOrUpdate(e.Container, null);
    }

    private static void MarkAll(ListBox list)
    {
        var on = IsCheckMode(list);
        foreach (var container in list.GetRealizedContainers())
            Mark(container, on);

        if (list.ItemsPanelRoot is { } panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is ListBoxItem item)
                    Mark(item, on);
            }
        }

        foreach (var item in list.Items)
        {
            if (item is ListBoxItem own)
                Mark(own, on);
        }

        if (Recycled.TryGetValue(list, out var recycled))
        {
            foreach (var (container, _) in (IEnumerable<KeyValuePair<Control, object?>>)recycled)
                Mark(container, on);
        }
    }

    private static void Mark(Control container, bool on)
    {
        if (on)
            container.SetValue(IsOnProperty, true);
        else
            container.ClearValue(IsOnProperty);
    }
}
