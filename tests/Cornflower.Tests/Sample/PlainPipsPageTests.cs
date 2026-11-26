using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's plain pips page (#241): it opens at its top with nothing focused, and each line shows its pager's page.</summary>
public class PlainPipsPageTests
{
    private static void Wait(int ms = 400)
    {
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromMilliseconds(ms));
        Dispatcher.UIThread.PushFrame(frame);
    }

    [AvaloniaFact]
    public void The_Page_Opens_At_Its_Top_With_Nothing_Focused()
    {
        var page = new PlainPipsPage();
        using var scene = new Scene(page, height: 400);
        Wait();

        Assert.Equal("plain pips", page.FindControl<SamplePageHeader>("Header")!.Title);
        var scroller = page.FindControl<ScrollViewer>("Scroller")!;
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height, "the page doesn't scroll at this height");
        Assert.Equal(default, scroller.Offset);
        Assert.Null(Assert.IsAssignableFrom<IFocusManager>(scene.Window.FocusManager).GetFocusedElement());
    }

    [AvaloniaFact]
    public void Each_Line_Shows_Its_Pagers_Page()
    {
        var page = new PlainPipsPage();
        using var scene = new Scene(page);
        Wait();

        Assert.Equal("page 1 of 5", page.FindControl<TextBlock>("FiveIndex")!.Text);
        Assert.Equal("page 8 of 12", page.FindControl<TextBlock>("TwelveIndex")!.Text);
        Assert.Equal("page 3 of 4", page.FindControl<TextBlock>("VerticalIndex")!.Text);

        var twelve = page.FindControl<PipsPager>("Twelve")!;
        var next = twelve.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_NextButton");
        next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Wait();
        Assert.Equal("page 9 of 12", page.FindControl<TextBlock>("TwelveIndex")!.Text);
        Assert.Equal(default, page.FindControl<ScrollViewer>("Scroller")!.Offset);
    }
}
