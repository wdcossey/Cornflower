using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Cornflower.Controls.Primitives;

/// <summary>
/// The header strip of a <see cref="Pivot"/>, WinRT's <c>Primitives.PivotHeaderPanel</c>: a row of the Pivot's
/// <see cref="PivotHeaderItem"/>s, each at its own width, starting from the selected item's header and wrapping round
/// to the ones before it. A Pivot's template holds one, named <c>PART_Header</c>, and the Pivot fills it.
/// </summary>
/// <remarks>
/// <para>
/// At rest the phone lays its headers out from the selected one, in item order, wrapping round, each at its full
/// width, with nothing trimmed or wrapped; the screen's edge cuts what doesn't fit (#128 and #136, runs
/// <c>20261115-2136-dev11</c> and <c>20261115-2339-dev11</c>, <c>tools/refapp/README.md</c>). This panel does the same,
/// from <see cref="Lead"/>, which the Pivot keeps at the selected item, or at the item it is leaving until the new one
/// comes in.
/// </para>
/// <para>
/// While the Pivot moves, the strip slides as one by its render transform, as the phone's <c>HeaderTranslateTransform</c>
/// does, so the headers' places aren't laid out again each frame.
/// </para>
/// </remarks>
public class PivotHeaderPanel : Panel
{
    private readonly TranslateTransform _translation = new();
    private int _lead;

    /// <summary>Initialises an empty strip.</summary>
    public PivotHeaderPanel()
    {
        RenderTransform = _translation;
    }

    /// <summary>The index of the header laid out first, at the strip's left; the rest follow it, wrapping round.</summary>
    internal int Lead
    {
        get => _lead;
        set
        {
            if (_lead == value) return;
            _lead = value;
            InvalidateArrange();
        }
    }

    /// <summary>How far the strip is slid to the right of its laid-out place, in view pixels (negative: to the left).</summary>
    internal double Translation
    {
        get => _translation.X;
        set => _translation.X = value;
    }

    /// <summary>
    /// The pitch of header <paramref name="index"/>: the next header's left edge less its own, the next being the one
    /// after it in item order, the first after the last. On the phone it is the header's width plus the next one's
    /// left margin (15.83 at the reference profile's raw-pixel grid).
    /// </summary>
    internal double Pitch(int index)
    {
        var count = Children.Count;
        if (count == 0) return 0;
        var header = Children[Wrap(index, count)];
        var next = Children[Wrap(index + 1, count)];
        return header.DesiredSize.Width - header.Margin.Left + next.Margin.Left;
    }

    /// <summary>The left edge of header <paramref name="index"/>'s slot, as laid out from <see cref="Lead"/>.</summary>
    internal double SlotLeft(int index)
    {
        var count = Children.Count;
        var x = 0.0;
        for (var k = 0; k < count; k++)
        {
            var i = Wrap(_lead + k, count);
            if (i == Wrap(index, count)) return x;
            x += Children[i].DesiredSize.Width;
        }

        return x;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = Children.Count;
        var x = 0.0;
        for (var k = 0; k < count; k++)
        {
            var child = Children[Wrap(_lead + k, count)];
            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, Math.Max(finalSize.Height, child.DesiredSize.Height)));
            x += child.DesiredSize.Width;
        }

        return finalSize;
    }

    internal static int Wrap(int index, int count) => count == 0 ? 0 : ((index % count) + count) % count;
}
