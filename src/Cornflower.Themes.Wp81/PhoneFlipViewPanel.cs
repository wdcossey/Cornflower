using Avalonia;
using Avalonia.Controls;
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
/// Not virtualising: every item is made and laid out, as an <c>ItemsControl</c>'s plain panel's are. The phone's
/// FlipView virtualises (a <c>VirtualizingStackPanel</c>); <c>docs/controls/FlipView/README.md</c> has it as a gap.
/// </remarks>
internal sealed class PhoneFlipViewPanel : Panel
{
    private Carousel? _owner;

    /// <summary>The Carousel whose items these are, or null outside one.</summary>
    internal Carousel? Owner => _owner;

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
        var position = _owner is null ? 0 : PhoneFlipView.PositionOf(_owner);
        var page = vertical ? finalSize.Height : finalSize.Width;
        for (var i = 0; i < Children.Count; i++)
        {
            var along = (i - position) * page;
            Children[i].Arrange(vertical
                ? new Rect(0, along, finalSize.Width, finalSize.Height)
                : new Rect(along, 0, finalSize.Width, finalSize.Height));
        }

        return finalSize;
    }
}
