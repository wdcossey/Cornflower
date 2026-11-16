using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The ContentDialog's <c>ContentPanel</c>, as the phone lays it out (#150, <c>tools/refapp/README.md</c>, "ContentDialog's
/// layout"): its children are, in order, the title, the content and the two button hosts.
/// </summary>
/// <remarks>
/// <para>As tall as its content asks, the phone's three-row grid: the title, then the content, each at least its
/// <see cref="TitleMinHeight"/> and <see cref="ContentMinHeight"/>, then the hosts side by side in the halves of the
/// width, the first in the left half, the row at least <see cref="ButtonsMinHeight"/>.</para>
/// <para><b>Full</b> (<see cref="IsFull"/>): with <see cref="FullSizeDesired"/>, or when that is taller than the room
/// there is, the panel fills the room, and the buttons go to a bar at its foot, as the phone's shell draws them: the
/// bar is <see cref="BarHeight"/> tall, and each host is placed so its child fills its slot of the bar less
/// <see cref="BarPadding"/>, the two slots <see cref="BarSpacing"/> apart. The content is given what is left between the
/// title and the bar, to clip; the title its own line.</para>
/// </remarks>
internal sealed class PhoneContentDialogPanel : Panel
{
    public static readonly StyledProperty<bool> FullSizeDesiredProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, bool>(nameof(FullSizeDesired));

    public static readonly StyledProperty<double> TitleMinHeightProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, double>(nameof(TitleMinHeight));

    public static readonly StyledProperty<double> ContentMinHeightProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, double>(nameof(ContentMinHeight));

    public static readonly StyledProperty<double> ButtonsMinHeightProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, double>(nameof(ButtonsMinHeight));

    public static readonly StyledProperty<double> BarHeightProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, double>(nameof(BarHeight));

    public static readonly StyledProperty<Thickness> BarPaddingProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, Thickness>(nameof(BarPadding));

    public static readonly StyledProperty<double> BarSpacingProperty =
        AvaloniaProperty.Register<PhoneContentDialogPanel, double>(nameof(BarSpacing));

    public static readonly DirectProperty<PhoneContentDialogPanel, bool> IsFullProperty =
        AvaloniaProperty.RegisterDirect<PhoneContentDialogPanel, bool>(nameof(IsFull), p => p.IsFull);

    private bool _isFull;
    private double _titleHeight;
    private double _contentHeight;
    private double _buttonsHeight;

    static PhoneContentDialogPanel()
    {
        AffectsMeasure<PhoneContentDialogPanel>(FullSizeDesiredProperty, TitleMinHeightProperty, ContentMinHeightProperty,
            ButtonsMinHeightProperty);
        AffectsArrange<PhoneContentDialogPanel>(BarHeightProperty, BarPaddingProperty, BarSpacingProperty);
    }

    /// <summary>The dialog's <c>FullSizeDesired</c>.</summary>
    public bool FullSizeDesired
    {
        get => GetValue(FullSizeDesiredProperty);
        set => SetValue(FullSizeDesiredProperty, value);
    }

    public double TitleMinHeight
    {
        get => GetValue(TitleMinHeightProperty);
        set => SetValue(TitleMinHeightProperty, value);
    }

    public double ContentMinHeight
    {
        get => GetValue(ContentMinHeightProperty);
        set => SetValue(ContentMinHeightProperty, value);
    }

    public double ButtonsMinHeight
    {
        get => GetValue(ButtonsMinHeightProperty);
        set => SetValue(ButtonsMinHeightProperty, value);
    }

    /// <summary>The full dialog's bar, from the foot of the panel.</summary>
    public double BarHeight
    {
        get => GetValue(BarHeightProperty);
        set => SetValue(BarHeightProperty, value);
    }

    /// <summary>Round the bar's two slots, which the hosts' children fill.</summary>
    public Thickness BarPadding
    {
        get => GetValue(BarPaddingProperty);
        set => SetValue(BarPaddingProperty, value);
    }

    /// <summary>Between the bar's two slots.</summary>
    public double BarSpacing
    {
        get => GetValue(BarSpacingProperty);
        set => SetValue(BarSpacingProperty, value);
    }

    /// <summary>Whether the panel fills its room with the buttons in the bar: set by the last measure.</summary>
    public bool IsFull
    {
        get => _isFull;
        private set => SetAndRaise(IsFullProperty, ref _isFull, value);
    }

    private Control? Title => Children.Count > 0 ? Children[0] : null;

    private Control? Body => Children.Count > 1 ? Children[1] : null;

    private Control? Host1 => Children.Count > 2 ? Children[2] : null;

    private Control? Host2 => Children.Count > 3 ? Children[3] : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        var free = new Size(availableSize.Width, double.PositiveInfinity);
        Title?.Measure(free);
        Body?.Measure(free);
        var half = new Size(availableSize.Width / 2, double.PositiveInfinity);
        Host1?.Measure(half);
        Host2?.Measure(half);

        _titleHeight = Math.Max(TitleMinHeight, Title?.DesiredSize.Height ?? 0);
        _contentHeight = Math.Max(ContentMinHeight, Body?.DesiredSize.Height ?? 0);
        _buttonsHeight = Math.Max(ButtonsMinHeight, Math.Max(Shown(Host1)?.DesiredSize.Height ?? 0, Shown(Host2)?.DesiredSize.Height ?? 0));
        var height = _titleHeight + _contentHeight + _buttonsHeight;
        var room = availableSize.Height;
        IsFull = !double.IsInfinity(room) && (FullSizeDesired || height > room);

        // Unbounded, as wide as the widest row: the hosts each have half.
        var width = double.IsInfinity(availableSize.Width)
            ? Math.Max(Math.Max(Title?.DesiredSize.Width ?? 0, Body?.DesiredSize.Width ?? 0),
                2 * Math.Max(Shown(Host1)?.DesiredSize.Width ?? 0, Shown(Host2)?.DesiredSize.Width ?? 0))
            : availableSize.Width;
        return new Size(width, IsFull ? room : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        Title?.Arrange(new Rect(0, 0, width, _titleHeight));
        if (!IsFull)
        {
            Body?.Arrange(new Rect(0, _titleHeight, width, _contentHeight));
            var top = _titleHeight + _contentHeight;
            // The phone's two star columns, a host in each.
            Host1?.Arrange(new Rect(0, top, width / 2, _buttonsHeight));
            Host2?.Arrange(new Rect(width / 2, top, width / 2, _buttonsHeight));
            return finalSize;
        }

        var barTop = finalSize.Height - BarHeight;
        Body?.Arrange(new Rect(0, _titleHeight, width, Math.Max(0, barTop - _titleHeight)));
        var slots = new Rect(0, barTop, width, BarHeight).Deflate(BarPadding);
        var slotWidth = Math.Max(0, (slots.Width - BarSpacing) / 2);
        if (Host1 is { } one)
            InSlot(one, 0);
        if (Host2 is { } two)
            InSlot(two, 1);
        return finalSize;

        void InSlot(Control host, int index)
        {
            var at = new Rect(slots.X + index * (slotWidth + BarSpacing), slots.Y, slotWidth, slots.Height);
            // The host's padding round its slot, so its child fills the slot.
            host.Arrange(at.Inflate(host is Decorator decorator ? decorator.Padding : default));
        }
    }

    private static Control? Shown(Control? host) => host is { IsVisible: true } ? host : null;
}
