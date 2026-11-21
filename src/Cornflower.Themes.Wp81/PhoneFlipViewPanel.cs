using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The Wp81 FlipView's items panel (Avalonia's <c>Carousel</c>, <c>Controls/Carousel.axaml</c>): every item the panel's
/// size, edge to edge along the FlipView's axis, a page apart, placed where <see cref="PhoneFlipView"/>'s motion has
/// the strip; at rest, with the selected item filling it. It stands in for Avalonia's <c>VirtualizingCarouselPanel</c>,
/// which shows one item at a time and runs the <c>PageTransition</c> between them, where the phone's FlipView drags
/// the next item in beside the one going (#211).
/// </summary>
/// <remarks>
/// The items are laid out once, item k k pages along, and the strip is moved by the panel's own translate
/// (<see cref="Place"/>), so a frame of the motion costs no layout, however many items there are. Not virtualising: every item is made and laid out, as an <c>ItemsControl</c>'s plain panel's are. The phone's
/// FlipView virtualises (a <c>VirtualizingStackPanel</c>); <c>docs/controls/FlipView/README.md</c> has it as a gap.
/// </remarks>
internal sealed class PhoneFlipViewPanel : Panel
{
    private readonly TranslateTransform _shift = new();
    private Carousel? _owner;

    public PhoneFlipViewPanel() => RenderTransform = _shift;

    /// <summary>Moves the strip to where the motion has it, without a layout pass.</summary>
    internal void Place() => Place(Bounds.Size);

    private void Place(Size size)
    {
        var vertical = _owner is { } owner && PhoneFlipView.IsVertical(owner);
        var along = -(_owner is null ? 0 : PhoneFlipView.PositionOf(_owner)) * (vertical ? size.Height : size.Width);
        _shift.X = vertical ? 0 : along;
        _shift.Y = vertical ? along : 0;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<Carousel>();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _owner = null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }

        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var vertical = _owner is { } owner && PhoneFlipView.IsVertical(owner);
        var page = vertical ? finalSize.Height : finalSize.Width;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(vertical
                ? new Rect(0, i * page, finalSize.Width, finalSize.Height)
                : new Rect(i * page, 0, finalSize.Width, finalSize.Height));
        }

        Place(finalSize);
        return finalSize;
    }
}
