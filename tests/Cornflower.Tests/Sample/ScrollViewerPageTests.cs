using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ScrollViewer page: in the catalogue by #116, every ScrollViewer on it the theme's, its header, and its scroll shown.</summary>
public class ScrollViewerPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Tier_1_Pages()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ScrollViewerPage));
        Assert.Equal("scrollviewer", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(PasswordBoxPage)));
    }

    [AvaloniaFact]
    public void Every_ScrollViewer_On_The_Page_Has_The_Phones_Thin_Indicators()
    {
        var page = new ScrollViewerPage();
        using var scene = new Scene(page);

        var viewers = page.GetVisualDescendants().OfType<ScrollViewer>().Prepend(page.FindControl<ScrollViewer>("Page")!).Distinct().ToList();
        Assert.Equal(5, viewers.Count);
        Assert.All(viewers, v => Assert.True(PhoneScrollIndicators.GetIsEnabled(v)));
        var bars = viewers.SelectMany(v => v.GetVisualDescendants().OfType<ScrollBar>().Where(b => b.TemplatedParent == v && b.IsVisible)).ToList();
        Assert.All(bars, bar => Assert.Contains(bar.GetVisualDescendants(), d => d is Thumb { Name: "VerticalPanningThumb" or "HorizontalPanningThumb" }));
        Assert.Contains(bars, bar => bar.Orientation == Avalonia.Layout.Orientation.Horizontal);
        Assert.False(page.FindControl<ScrollViewer>("Disabled")!.IsEnabled);
        Assert.False(page.FindControl<ScrollViewer>("Shown")!.AllowAutoHide);
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ScrollViewerPage();
        using var scene = new Scene(page);

        Assert.Equal("scrollviewer", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void The_Page_Is_Taller_Than_The_Phone_And_Its_Scroll_Is_Shown()
    {
        var page = new ScrollViewerPage();
        using var scene = new Scene(page);
        var viewer = page.FindControl<ScrollViewer>("Page")!;
        var state = page.FindControl<TextBlock>("State")!;

        Assert.True(viewer.ScrollBarMaximum.Y > 0, "the page doesn't scroll at the reference view");
        Assert.Equal($"scrolled 0 of {viewer.ScrollBarMaximum.Y:0}", state.Text);

        viewer.Offset = new Vector(0, 100);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal($"scrolled 100 of {viewer.ScrollBarMaximum.Y:0}", state.Text);
    }
}
