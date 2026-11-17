using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The inline ComboBox's <c>ItemsPresenterHost</c>: its items, clipped to the box. Closed, it is <see cref="ClosedHeight"/>
/// tall (the phone's <c>DropDownClosedHeight</c>, 27.17, is under it) and the items are lifted so the selected one sits
/// in it: the phone's <c>DropDownOffset</c>, the item's top padding (8.5) and the items above it. Open, it is as tall
/// as the items (<c>DropDownOpenedHeight</c>, 51.67 a item on the reference) and they are not lifted. tools/refapp/README.md,
/// "TemplateSettings", has the phone's values.
/// </summary>
/// <remarks>
/// The phone's host is a <c>Canvas</c> whose <c>Height</c> and the items' <c>TranslateTransform</c> its storyboards set
/// from the control's <c>TemplateSettings</c>. Here the layout does it, with one child, the <c>ItemsPresenter</c>: measured
/// at the host's width and any height, and arranged at its own height, lifted while closed. The selected item is the
/// templated parent's <c>SelectedIndex</c>, none taken as the first.
/// </remarks>
internal sealed class PhoneComboBoxItemsHost : Panel
{
    /// <summary>Whether the box shows open: the host as tall as the items, the items not lifted.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<PhoneComboBoxItemsHost, bool>(nameof(IsOpen));

    /// <summary>
    /// The closed height: the phone's <c>MinHeight</c>, <c>ComboBoxItemMinHeightThemeSize</c> (33.5). Rounded to the nearest
    /// pixel, as the phone rounds it (33.33 at 2.4), where a <c>MinHeight</c> would be rounded up.
    /// </summary>
    public static readonly StyledProperty<double> ClosedHeightProperty =
        AvaloniaProperty.Register<PhoneComboBoxItemsHost, double>(nameof(ClosedHeight));

    /// <summary>The item to show in the closed box: the templated parent's <c>SelectedIndex</c>.</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<PhoneComboBoxItemsHost, int>(nameof(SelectedIndex), -1);

    static PhoneComboBoxItemsHost()
    {
        ClipToBoundsProperty.OverrideDefaultValue<PhoneComboBoxItemsHost>(true);
        AffectsMeasure<PhoneComboBoxItemsHost>(IsOpenProperty, ClosedHeightProperty);
        AffectsArrange<PhoneComboBoxItemsHost>(SelectedIndexProperty);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public double ClosedHeight
    {
        get => GetValue(ClosedHeightProperty);
        set => SetValue(ClosedHeightProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>How far the items are lifted while closed, in view pixels: the phone's <c>DropDownOffset</c> negated.</summary>
    internal double Lift { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        var width = 0.0;
        foreach (var child in Children)
        {
            child.Measure(availableSize.WithHeight(double.PositiveInfinity));
            // The items' height without their margin: the ItemsPresenter's 0.8 above runs past the host's foot, as on the phone.
            height = Math.Max(height, child.DesiredSize.Height - child.Margin.Top - child.Margin.Bottom);
            width = Math.Max(width, child.DesiredSize.Width);
        }

        var closed = UseLayoutRounding ? LayoutHelper.RoundLayoutValue(ClosedHeight, LayoutHelper.GetLayoutScale(this)) : ClosedHeight;
        // As wide as the items, so a box that isn't stretched fits them (the phone's Canvas is 0 wide, its width set from outside).
        return new Size(width, IsOpen ? Math.Max(height, closed) : closed);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Lift = IsOpen ? 0 : LiftFor(Math.Max(SelectedIndex, 0));
        foreach (var child in Children)
            child.Arrange(new Rect(0, -Lift, finalSize.Width, child.DesiredSize.Height));
        return finalSize;
    }

    /// <summary>The selected item's top padding and the heights of the items above it.</summary>
    private double LiftFor(int index)
    {
        if (TemplatedParent is not ItemsControl items || items.ContainerFromIndex(index) is not { } selected)
            return 0;
        var lift = selected is TemplatedControl templated ? templated.Padding.Top : 0;
        for (var i = 0; i < index; i++)
        {
            if (items.ContainerFromIndex(i) is { } above)
                lift += above.DesiredSize.Height;
        }

        return lift;
    }
}
