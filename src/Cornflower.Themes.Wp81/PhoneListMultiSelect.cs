using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's list check mode: WinRT's <c>MultiSelectStates</c>, which Avalonia's <see cref="ListBoxItem"/> doesn't
/// raise. A WinRT ListView in <c>SelectionMode="Multiple"</c> puts every item in <c>ListMultiSelect</c>: its check box
/// slides in from the left and its content 40.5 to the right, and a tap checks or unchecks it. Avalonia's ListBox
/// selects that way with <see cref="SelectionMode.Multiple"/> and <see cref="SelectionMode.Toggle"/> together, so a
/// list with both flags is in the check mode, and <see cref="IsOnProperty"/> says so on each of its items, for the
/// ListViewItem theme's selectors. Turned on by the ListView theme's own setter.
/// </summary>
/// <remarks>
/// <para>Only both flags. <see cref="SelectionMode.Multiple"/> alone is WinRT's <c>Extended</c>, chosen with the
/// modifier keys, which shows no check boxes here; <see cref="SelectionMode.Toggle"/> alone toggles one item.</para>
/// <para>The list does the work, not each item: it marks each container as it is prepared, a recycled one again
/// as it is reused, and every realised one when <see cref="ListBox.SelectionMode"/> changes. So it costs a list one
/// handler and a container one property, and no global rule. An item outside a list is never in the mode; an item that
/// is its own container keeps its mark when it is taken out of the list, until a list prepares it again. Turned off (a
/// theme of the app's own on the list), it lets go: the new theme's template makes new containers, which it doesn't
/// mark.</para>
/// </remarks>
internal static class PhoneListMultiSelect
{
    /// <summary>Set by Wp81's ListView theme: whether the list marks its items.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsEnabled", typeof(PhoneListMultiSelect));

    /// <summary>On an item: whether its list is in the check mode. Read by the ListViewItem theme's selectors.</summary>
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsOn", typeof(PhoneListMultiSelect));

    /// <summary>The selection that is the check mode: WinRT's <c>Multiple</c>.</summary>
    private const SelectionMode CheckMode = SelectionMode.Multiple | SelectionMode.Toggle;

    static PhoneListMultiSelect()
    {
        IsEnabledProperty.Changed.AddClassHandler<ListBox>((list, e) =>
        {
            // A theme's setter comes before its template, so every container is prepared after this.
            if (e.GetNewValue<bool>())
                list.ContainerPrepared += OnPrepared;
            else
                list.ContainerPrepared -= OnPrepared;
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

    /// <summary>Whether <paramref name="list"/> is in the check mode: on, with both flags.</summary>
    private static bool IsCheckMode(ListBox list) =>
        list.GetValue(IsEnabledProperty) && (list.SelectionMode & CheckMode) == CheckMode;

    private static void OnPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (sender is ListBox list)
            Mark(e.Container, IsCheckMode(list));
    }

    private static void MarkAll(ListBox list)
    {
        var on = IsCheckMode(list);
        foreach (var container in list.GetRealizedContainers())
            Mark(container, on);
    }

    private static void Mark(Control container, bool on)
    {
        if (on)
            container.SetValue(IsOnProperty, true);
        else
            container.ClearValue(IsOnProperty);
    }
}
