using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The MenuFlyoutPresenter template's root: the menu as tall as its items and padding, the borders round them taking
/// from the bottom padding, as the phone's menu is (#150: four items of 51.25 and the 39.5 padding make 244.58, the
/// borders' 6 not added; the first item 16.67 below the top, the bottom line 23 below the last). A menu longer than the
/// window is cut to it by its popup's <c>MaxHeight</c>, the window's (<see cref="PhonePopupHost"/>).
/// </summary>
/// <remarks>
/// Its child is the template's nest of <see cref="Border"/>s, which draw only their borders, round the part that holds
/// the items; the height is that part's own (its margin in it). The borders' thickness, which the opening changes, so
/// never changes the menu's height.
/// </remarks>
internal sealed class PhoneMenuFlyoutRoot : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
            return default;
        child.Measure(availableSize.WithHeight(double.PositiveInfinity));
        Control inner = child;
        while (inner is Border { Child: { } next })
            inner = next;
        return new Size(child.DesiredSize.Width, inner.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
