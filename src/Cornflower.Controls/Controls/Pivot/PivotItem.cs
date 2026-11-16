using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Controls;

/// <summary>
/// One page of a <see cref="Pivot"/>, WinRT's <c>PivotItem</c>: its <see cref="Header"/> goes in the Pivot's header
/// strip, and its content fills the page while it is selected.
/// </summary>
/// <remarks>
/// Behaviour only, as WinRT's is: a <see cref="ContentControl"/> with a header. The Pivot shows its selected item and
/// hides the rest, as the phone's does (an item that isn't selected is drawn at opacity 0 there). The phone's template
/// declares three states, <c>Right</c>, <c>Left</c> and <c>Center</c>, that change nothing, so none is raised here.
/// </remarks>
/// <example><c>&lt;cf:PivotItem Header="one"&gt;&lt;TextBlock Text="first" /&gt;&lt;/cf:PivotItem&gt;</c></example>
public class PivotItem : ContentControl
{
    /// <summary>Defines <see cref="Header"/>.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<PivotItem, object?>(nameof(Header));

    /// <summary>
    /// What the Pivot's header strip shows for this item, through the Pivot's <see cref="Pivot.HeaderTemplate"/>.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HeaderProperty && ItemsControl.ItemsControlFromItemContainer(this) is Pivot pivot)
            pivot.OnItemHeaderChanged(this);
    }
}
