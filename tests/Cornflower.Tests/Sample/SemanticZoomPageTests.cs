using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's SemanticZoom page (#215): in the catalogue, a contacts list a to z with empty letters, and its jump list.</summary>
public class SemanticZoomPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_By_Its_Issue()
    {
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(SemanticZoomPage));
        Assert.Equal("semantic zoom", entry.Title);
        Assert.Equal(215, Assert.Single(typeof(SemanticZoomPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);
    }

    // The list shows the 18 letters with contacts; the jump list all 26, in the GridView's theme, its tiles 99 with a
    // margin of 6; neither scrolls to its selection (#220). A tap on header a opens it; a tap on tile m lands the list on m.
    [AvaloniaFact]
    public void The_Jump_List_Has_Every_Letter_And_Lands_The_List_On_One()
    {
        using var clock = new TestClock();
        var page = new SemanticZoomPage();
        using var scene = new Scene(page);
        var zoom = page.FindControl<SemanticZoom>("Zoom")!;
        var list = page.FindControl<ListBox>("List")!;
        var jump = page.FindControl<ListBox>("JumpList")!;

        Assert.Equal("semantic zoom", page.FindControl<SamplePageHeader>("Header")!.Title);
        Assert.Equal(18, list.Items.OfType<ListViewHeaderItem>().Count());
        Assert.Equal(26, jump.ItemCount);
        Assert.Same(scene.Window.FindResource("Wp81GridViewTheme"), jump.Theme);
        Assert.False(list.AutoScrollToSelectedItem);
        Assert.False(jump.AutoScrollToSelectedItem);

        Tap(scene, list.ContainerFromIndex(0)!);
        clock.At(1000);
        Dispatcher.UIThread.RunJobs();
        Assert.False(zoom.IsZoomedInViewActive);
        var m = jump.ContainerFromIndex(12)!;
        Assert.Equal(111.25, m.Bounds.Height, 0.5);

        Tap(scene, m);
        Assert.True(zoom.IsZoomedInViewActive);
        var header = list.GetRealizedContainers().OfType<ListViewHeaderItem>().Single(h => ((ContactGroup)h.Content!).Key == "m");
        Assert.Equal(zoom.TranslatePoint(default, scene.Window)!.Value.Y, header.TranslatePoint(default, scene.Window)!.Value.Y, 0.5);
    }

    private static void Tap(Scene scene, Control control)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
