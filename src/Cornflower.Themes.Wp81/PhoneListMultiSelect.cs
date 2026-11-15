using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's list check mode: WinRT's <c>MultiSelectStates</c>, which Avalonia's <see cref="ListBoxItem"/> doesn't
/// raise. A WinRT ListView in <c>SelectionMode="Multiple"</c> puts every item in <c>ListMultiSelect</c>: its check box
/// slides in from the left and its content 40.5 to the right, and a tap checks or unchecks it. The ListView theme sets
/// <see cref="IsOnProperty"/> on a ListBox whose <see cref="ListBox.SelectionMode"/> has
/// <see cref="SelectionMode.Multiple"/>, and its items inherit it, for the ListViewItem theme's selectors.
/// </summary>
/// <remarks>
/// <para>With <see cref="SelectionMode.Toggle"/>, a tap checks or unchecks: WinRT's <c>Multiple</c>. Without it,
/// WinRT's <c>Extended</c>, chosen with the modifier keys, it shows the check boxes too: the phone's <c>Extended</c>
/// wasn't read, and a ListView item draws its selection only in the check mode, so without them a selection of several
/// would be invisible. <see cref="SelectionMode.Toggle"/> alone toggles one item, with no boxes.</para>
/// <para>Inherited, so every container the list holds has it, whether it makes it, reuses it, keeps it out of view or
/// the item is its own container, with no bookkeeping; a theme of the app's own on the list takes it away with Wp81's.
/// A nested Wp81 list sets its own. An item outside a list never has it. It costs the ListView theme one style rule,
/// and no global rule.</para>
/// </remarks>
internal static class PhoneListMultiSelect
{
    /// <summary>Whether the list is in the check mode: set on a list by the ListView theme, inherited by its items.</summary>
    public static readonly AttachedProperty<bool> IsOnProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsOn", typeof(PhoneListMultiSelect), inherits: true);

    public static bool GetIsOn(Control control) => control.GetValue(IsOnProperty);
}
