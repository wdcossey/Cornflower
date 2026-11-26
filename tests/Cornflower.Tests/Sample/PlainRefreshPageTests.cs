using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's plain refresh page (#241): it opens at its top, and its button refreshes, which ends by itself.</summary>
public class PlainRefreshPageTests
{
    private static RefreshVisualizerState State(PlainRefreshPage page) =>
        page.FindControl<RefreshContainer>("Container")!.Visualizer!.GetValue(RefreshVisualizer.RefreshVisualizerStateProperty);

    [AvaloniaFact]
    public void The_Page_Opens_At_Its_Top_With_Nothing_Focused()
    {
        var page = new PlainRefreshPage();
        using var scene = new Scene(page);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("plain refresh", page.FindControl<SamplePageHeader>("Header")!.Title);
        Assert.Equal(default, page.FindControl<ScrollViewer>("List")!.Offset);
        Assert.Null(scene.Window.FocusManager?.GetFocusedElement());
        Assert.Equal(RefreshVisualizerState.Idle, State(page));
    }

    [AvaloniaFact]
    public void Its_Button_Refreshes_And_The_Refresh_Ends_By_Itself()
    {
        var page = new PlainRefreshPage();
        using var scene = new Scene(page);
        var ring = page.FindControl<RefreshContainer>("Container")!.Visualizer!.GetVisualDescendants()
            .OfType<ProgressRing>().Single();

        page.FindControl<Button>("Refresh")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(RefreshVisualizerState.Refreshing, State(page));
        Assert.True(ring.IsActive);

        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, PlainRefreshPage.RefreshTime + TimeSpan.FromMilliseconds(500));
        Dispatcher.UIThread.PushFrame(frame);

        Assert.Equal(RefreshVisualizerState.Idle, State(page));
        Assert.False(ring.IsActive);
        Assert.Equal("refreshed once", page.FindControl<TextBlock>("Status")!.Text);
        Assert.Equal(default, page.FindControl<ScrollViewer>("List")!.Offset);
    }
}
