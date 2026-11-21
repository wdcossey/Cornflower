using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Cornflower.Controls;

/// <summary>
/// A group's header in a grouped list: WinRT's <c>ListViewHeaderItem</c>, and its <c>GridViewHeaderItem</c>, which adds
/// nothing to it. The list makes one for each group of <see cref="ListBoxOptions.GroupedItemsSource"/>, its
/// <see cref="ContentControl.Content"/> the group and its template <see cref="ListBoxOptions.GroupHeaderTemplate"/>.
/// </summary>
/// <remarks>
/// A <see cref="ListBoxItem"/>, so the list takes it as its own container, but never a selected one: it doesn't select
/// on a press or a key, isn't focusable, so the arrow keys pass it, and the list lets it go from any selection that
/// takes it in. It never raises <see cref="ListBoxOptions.ItemClickEvent"/>. In a panel that gives it a width, it asks
/// for all of it, so a header fills its row in a wrapping panel as in a stack. Behaviour only: a theme draws it.
/// </remarks>
public class ListViewHeaderItem : ListBoxItem
{
    static ListViewHeaderItem()
    {
        FocusableProperty.OverrideDefaultValue<ListViewHeaderItem>(false);
    }

    /// <summary>The list's width, where it has one: the phone's header spans the list.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return double.IsInfinity(availableSize.Width) ? size : size.WithWidth(availableSize.Width);
    }

    // ListBoxItem selects in these, and on a key, which a header, never focused, doesn't take.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
    }
}
