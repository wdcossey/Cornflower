using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornflower.Controls;
using Xunit;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The Pivot's swipe and the Hub's pan are one recogniser, <c>SidewaysSwipeGestureRecognizer</c> (since the Tier 2
/// roll-up), held here on a Pivot: where a swipe may start, how far a finger moves before it is one, and that once it
/// is one the finger is the Pivot's until it lifts or is taken. Its other rules (sideways only, touch and pen only, a
/// press on a moving Hub holding it) are held by <see cref="PivotTests"/> and <see cref="HubTests"/>.
/// </summary>
public class SidewaysSwipeTests
{
    private static TestClock Clock() => new() { FrameInterval = TimeSpan.FromMilliseconds(15.625), FramesOnGrid = true };

    // Avalonia's ScrollGestureRecognizer's distance, 5: a finger that wobbles less while it taps doesn't drag.
    [AvaloniaFact]
    public void A_Move_Within_The_Scroll_Start_Distance_Isnt_A_Swipe()
    {
        using var clock = Clock();
        using var stage = new PivotTests.Stage();
        Assert.Equal(5, SidewaysSwipeGestureRecognizer.StartDistance);

        var touch = stage.Window.TouchBegin(new Point(300, 450), RawInputModifiers.None);
        clock.At(10);
        stage.Window.TouchMove(touch, new Point(296, 450), RawInputModifiers.None);
        clock.At(20);
        Assert.Equal(0, stage.ItemX(0));

        stage.Window.TouchMove(touch, new Point(290, 450), RawInputModifiers.None);
        clock.At(30);
        Assert.Equal(-10, stage.ItemX(0), 0.001);
        stage.Window.TouchEnd(touch, new Point(290, 450), RawInputModifiers.None);
    }

    // The swipe starts in the template's ScrollViewer, the headers and the items; the title above it doesn't drag.
    [AvaloniaFact]
    public void A_Drag_On_The_Title_Isnt_A_Swipe()
    {
        using var clock = Clock();
        using var stage = new PivotTests.Stage();
        var title = new Point(300, 48);
        Assert.False(stage.Pivot.IsInSwipeArea(title));

        var touch = stage.Window.TouchBegin(title, RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(i * 10);
            stage.Window.TouchMove(touch, title - new Point(20 * i, 0), RawInputModifiers.None);
        }

        Assert.Equal(0, stage.ItemX(0));
        stage.Window.TouchEnd(touch, title - new Point(200, 0), RawInputModifiers.None);
        clock.At(1000);
        Assert.Equal(0, stage.Pivot.SelectedIndex);
    }

    // Once the swipe has the finger, a ScrollViewer in the item doesn't take it when it turns up or down.
    [AvaloniaFact]
    public void Once_Swiping_The_Finger_Is_The_Pivots_Up_And_Down_Too()
    {
        using var clock = Clock();
        var list = new StackPanel();
        for (var i = 0; i < 40; i++) list.Children.Add(new TextBlock { Text = $"line {i}", Height = 40 });
        var scroll = new ScrollViewer { Content = list };
        var pivot = new Pivot { Title = "app" };
        pivot.Items.Add(new PivotItem { Header = "one", Content = scroll });
        pivot.Items.Add(new PivotItem { Header = "two", Content = new TextBlock { Text = "two" } });
        using var stage = new PivotTests.Stage(pivot: pivot);

        var touch = stage.Window.TouchBegin(new Point(300, 500), RawInputModifiers.None);
        for (var i = 1; i <= 5; i++)
        {
            clock.At(i * 10);
            stage.Window.TouchMove(touch, new Point(300 - 20 * i, 500), RawInputModifiers.None);
        }

        for (var i = 1; i <= 10; i++)
        {
            clock.At(50 + i * 10);
            stage.Window.TouchMove(touch, new Point(200, 500 - 20 * i), RawInputModifiers.None);
        }

        Assert.Equal(0, scroll.Offset.Y);
        Assert.Equal(-100, stage.ItemX(0), 0.001);
        stage.Window.TouchEnd(touch, new Point(200, 300), RawInputModifiers.None);
    }

    // Something else taking the finger mid-swipe lets the Pivot go on to a page, as a release with no speed does.
    [AvaloniaFact]
    public void A_Swipe_Whose_Finger_Is_Taken_Goes_On_To_A_Page()
    {
        using var clock = Clock();
        using var stage = new PivotTests.Stage();
        IPointer? pointer = null;
        stage.Window.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel, handledEventsToo: true);

        var touch = stage.Window.TouchBegin(new Point(400, 450), RawInputModifiers.None);
        for (var i = 1; i <= 12; i++)
        {
            clock.At(i * 10);
            stage.Window.TouchMove(touch, new Point(400 - 25 * i, 450), RawInputModifiers.None);
        }

        Assert.Equal(-300, stage.ItemX(0), 0.001);
        pointer!.Capture(stage.Window);
        clock.At(2000);

        Assert.Equal(1, stage.Pivot.SelectedIndex);                     // on past half a page
        Assert.Equal(0, stage.ItemX(1), 0.001);
        stage.Window.TouchEnd(touch, new Point(100, 450), RawInputModifiers.None);
    }
}
