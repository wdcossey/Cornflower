using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain SplitView (ADR 0007, #239): the pane on the flyout's surface, edged on the content's side by the 1.5 line
/// in PhoneLowBrush; it opens and shuts at once, and an open overlay pane dims nothing. Simple's pane is its own grey
/// with a transparent 1 edge, slides over 0.2 s and dims the content, so these fail without the theme.
/// </summary>
public class SplitViewTests
{
    private static SplitView View(SplitViewPanePlacement placement = SplitViewPanePlacement.Left) => new()
    {
        Width = 400, Height = 400, PanePlacement = placement,
        Pane = new TextBlock { Text = "pane" }, Content = new TextBlock { Text = "content" },
    };

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Its_Pane_Is_The_Flyouts_Surface_With_The_Line_At_Its_Edge(string variant)
    {
        var view = View();
        view.DisplayMode = SplitViewDisplayMode.Inline;
        view.IsPaneOpen = true;
        using var scene = new Scene(view, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var pane = Part<Panel>(view, "PART_PaneRoot");
        Assert.Same(Resource<IBrush>(view, "FlyoutBackgroundThemeBrush"), view.PaneBackground);
        Assert.Same(view.PaneBackground, pane.Background);
        var edge = Part<Rectangle>(view, "PART_PaneEdge");
        Assert.Same(Resource<IBrush>(view, "PhoneLowBrush"), edge.Fill);
        Assert.Equal(1.5, edge.Width);
        Assert.InRange(edge.Bounds.Width, 1.5 - 1 / Scene.ReferenceScaling, 1.5 + 1 / Scene.ReferenceScaling);
        Assert.Equal(pane.Bounds.Width, edge.Bounds.Right, 6);
        Assert.InRange(pane.Bounds.Width, 320 - 1 / Scene.ReferenceScaling, 320 + 1 / Scene.ReferenceScaling);
        Assert.Null(view.Background);
    }

    // The line is on the content's side for every placement.
    [AvaloniaTheory]
    [InlineData(SplitViewPanePlacement.Left, HorizontalAlignment.Right, VerticalAlignment.Stretch)]
    [InlineData(SplitViewPanePlacement.Right, HorizontalAlignment.Left, VerticalAlignment.Stretch)]
    [InlineData(SplitViewPanePlacement.Top, HorizontalAlignment.Stretch, VerticalAlignment.Bottom)]
    [InlineData(SplitViewPanePlacement.Bottom, HorizontalAlignment.Stretch, VerticalAlignment.Top)]
    public void The_Line_Faces_The_Content(SplitViewPanePlacement placement, HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        var view = View(placement);
        using var scene = new Scene(view);

        var edge = Part<Rectangle>(view, "PART_PaneEdge");
        Assert.Equal(horizontal, edge.HorizontalAlignment);
        Assert.Equal(vertical, edge.VerticalAlignment);
        Assert.Equal(1.5, horizontal == HorizontalAlignment.Stretch ? edge.Height : edge.Width);
    }

    // No motion (rule 8): the pane is its open length the moment it opens, and its closed one the moment it shuts.
    [AvaloniaTheory]
    [InlineData(SplitViewPanePlacement.Left)]
    [InlineData(SplitViewPanePlacement.Top)]
    public void It_Opens_And_Shuts_At_Once(SplitViewPanePlacement placement)
    {
        var view = View(placement);
        view.DisplayMode = SplitViewDisplayMode.CompactOverlay;
        using var scene = new Scene(view);
        var pane = Part<Panel>(view, "PART_PaneRoot");
        double Length() => placement == SplitViewPanePlacement.Left ? pane.Width : pane.Height;

        Assert.Equal(48, Length());
        view.IsPaneOpen = true;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(320, Length());
        Assert.True(pane.Transitions is null || pane.Transitions.Count == 0);
        view.IsPaneOpen = false;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(48, Length());
    }

    // An open overlay pane dims nothing, light dismiss or not (rule 5), and its layer still takes the finger.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_Open_Overlay_Pane_Dims_Nothing(bool lightDismiss)
    {
        var view = View();
        view.UseLightDismissOverlayMode = lightDismiss;
        view.IsPaneOpen = true;
        using var scene = new Scene(view);

        var layer = Part<Rectangle>(view, "LightDismissLayer");
        Assert.True(layer.IsVisible);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(layer.Fill).Color);
    }
}
