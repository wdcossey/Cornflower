using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// What <see cref="PhonePopupHost"/> takes from a popup's owner, beyond the Flyout's own (FlyoutTests holds those): a
/// motion of its own (<see cref="PhonePopupHost.Moves"/>), its rows (<see cref="PhonePopupHost.Rows"/>) or swing
/// (<see cref="PhonePopupHost.Swing"/>), its say on Back (<see cref="PhonePopupHost.Back"/>), its window's area and its
/// resizes, and Back left to a focused element first (<see cref="PhonePopupDismissal.BackTakesFocusFirst"/>).
/// </summary>
public class PhonePopupHostTests
{
    private const double Tick = 15.625;

    /// <summary>A bare popup on a host, its owner's close counted.</summary>
    private sealed class Stage : IDisposable
    {
        public Stage(double? height = 100, bool closes = true)
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Content = new Border { Background = Brushes.White };
            if (height is { } h)
                Content.Height = h;
            Popup = new Popup { Child = Content };
            Scene = new Scene(new Panel { Children = { Popup } });
            Host = new PhonePopupHost(Popup, Content, () =>
            {
                Closes++;
                if (closes)
                    Popup.Close();
            });
        }

        public TestClock Clock { get; }

        public Border Content { get; }

        public Popup Popup { get; }

        public Scene Scene { get; }

        public PhonePopupHost Host { get; }

        public int Closes { get; private set; }

        public double Now => Clock.Now.TotalMilliseconds;

        public double FirstFrameAfter(double call) => (Math.Floor(call / Tick) + 1) * Tick;

        public void Open()
        {
            Popup.Open();
            Dispatcher.UIThread.RunJobs();
        }

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
        }

        public RoutedEventArgs Back()
        {
            var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            Scene.Window.RaiseEvent(request);
            Dispatcher.UIThread.RunJobs();
            return request;
        }

        public void Dispose()
        {
            Host.Dispose();
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    [AvaloniaFact]
    public void A_Popup_That_Doesnt_Move_Opens_At_Rest_And_Closes_At_Once()
    {
        using var stage = new Stage();
        stage.Host.Moves = false;
        stage.Open();

        Assert.True(stage.Host.IsOpen);
        Assert.Null(stage.Host.Bands);
        Assert.Equal(1, stage.Content.Opacity);
        stage.At(stage.FirstFrameAfter(0));
        Assert.Null(stage.Host.Bands);
        // Still placed and sized by the host.
        Assert.Equal(450, stage.Popup.Width);

        stage.Host.Dismiss();
        Assert.Equal(1, stage.Closes);
        Assert.False(stage.Popup.IsOpen);
        Assert.False(stage.Host.IsOpen);
    }

    [AvaloniaFact]
    public void A_Popup_That_Doesnt_Move_Kept_Open_By_Its_Owner_Is_Shown_And_Not_Closed_Again()
    {
        using var stage = new Stage(closes: false);
        stage.Host.Moves = false;
        stage.Open();

        stage.Host.Dismiss();
        Assert.Equal(1, stage.Closes);
        Assert.True(stage.Host.IsOpen);
        Assert.False(stage.Host.IsClosing);
        stage.At(stage.Now + 500);
        Assert.Equal(1, stage.Closes);
    }

    [AvaloniaFact]
    public void A_Full_Popup_Turns_In_Its_Own_Rows()
    {
        using var stage = new Stage(height: null);
        stage.Host.Placement = PhonePopupPlacement.Full;
        stage.Host.Rows = new PhonePopupRows(200, [10, 5], []);
        stage.Open();
        var first = stage.FirstFrameAfter(0);

        stage.At(first);
        var bands = Assert.IsType<PhonePopupBands>(stage.Host.Bands);
        Assert.Equal(4, bands.Bands.Count);
        Assert.All(bands.Bands, b => Assert.Equal(200, b.Height));
        Assert.Equal([80, 80, 0, 0], bands.Angles);
        // No reveal: below the turning rows it is drawn at once.
        Assert.True(double.IsPositiveInfinity(bands.RevealedTo));
        stage.At(first + 4 * Tick);
        Assert.Equal([40, 60, 0, 0], stage.Host.Bands!.Angles);

        // The slowest row is at rest after 16 ticks.
        stage.At(first + 15 * Tick);
        Assert.NotNull(stage.Host.Bands);
        stage.At(first + 16 * Tick);
        Assert.Null(stage.Host.Bands);

        // Closing, its four bands turn away two ticks apart: closed at 14 ticks (2 × 3 + 8.2).
        var hidden = stage.Now;
        stage.Host.Dismiss();
        stage.At(stage.FirstFrameAfter(hidden) + 13 * Tick);
        Assert.Equal(4, stage.Host.Bands!.Bands.Count);
        Assert.Equal(0, stage.Closes);
        stage.At(stage.FirstFrameAfter(hidden) + 14 * Tick);
        Assert.Equal(1, stage.Closes);
    }

    [AvaloniaFact]
    public void Rows_Given_Mid_Opening_Cut_The_Popup_Again_In_Them()
    {
        // An owner told its flyout only after the popup has opened (Avalonia's IsOpen) gives its rows then.
        using var stage = new Stage(height: null);
        stage.Host.Placement = PhonePopupPlacement.Full;
        stage.Open();
        Assert.Equal(8, stage.Host.Bands!.Bands.Count);

        stage.Host.Rows = PhonePopupMotion.ListPickerRows;
        // Before the first frame, cut again at once: no frame shows the popup hidden with no bands.
        Assert.Equal(12, stage.Host.Bands!.Bands.Count);
        stage.At(stage.FirstFrameAfter(0));
        Assert.Equal(12, stage.Host.Bands!.Bands.Count);
        Assert.All(stage.Host.Bands.Angles.Take(5), a => Assert.Equal(80, a, 1e-9));
    }

    [AvaloniaFact]
    public void Rows_That_Dont_Turn_And_Reveal_Nothing_Open_At_Once()
    {
        using var stage = new Stage(height: null);
        stage.Host.Placement = PhonePopupPlacement.Full;
        stage.Host.Rows = new PhonePopupRows(200, [], []);
        stage.Open();

        Assert.Equal([0, 0, 0, 0], stage.Host.Bands!.Angles);
        stage.At(stage.FirstFrameAfter(0));
        Assert.Null(stage.Host.Bands);
    }

    [AvaloniaFact]
    public void A_Top_Popup_Swings_Its_Owners_Way_And_Closes_When_It_Is_Gone()
    {
        using var stage = new Stage();
        var length = TimeSpan.FromMilliseconds(2 * Tick);
        stage.Host.Swing = new PhonePopupSwing(t => 30 * Math.Max(0, 1 - t / length), length,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
        stage.Open();
        var first = stage.FirstFrameAfter(0);

        Assert.Equal(30, stage.Host.Bands!.Angles[0]);
        stage.At(first + Tick);
        Assert.Equal(15, stage.Host.Bands!.Angles[0], 1e-9);
        stage.At(first + 2 * Tick);
        Assert.Null(stage.Host.Bands);

        // Drawn as it is until it goes, in one frame, closed as it goes.
        var hidden = stage.Now;
        stage.Host.Dismiss();
        stage.At(hidden + 40);
        Assert.Equal(1, stage.Content.Opacity);
        Assert.Equal(0, stage.Closes);
        stage.At(hidden + 50 + Tick);
        Assert.Equal(1, stage.Closes);
        Assert.False(stage.Popup.IsOpen);
    }

    [AvaloniaFact]
    public void Back_Goes_To_An_Owner_That_Takes_It_And_Is_Taken()
    {
        using var stage = new Stage();
        var backs = 0;
        stage.Host.Back = () => backs++;
        stage.Open();
        stage.At(stage.Now + 500);

        Assert.True(stage.Back().Handled);
        Assert.Equal(1, backs);
        Assert.False(stage.Host.IsClosing);
        Assert.True(stage.Popup.IsOpen);

        // While it closes, Back is taken and does nothing.
        stage.Host.Dismiss();
        Assert.True(stage.Back().Handled);
        Assert.Equal(1, backs);
    }

    [AvaloniaFact]
    public void The_Host_Gives_Its_Window_And_Area_While_Open_And_Says_When_It_Has_Resized()
    {
        using var stage = new Stage();
        var widths = new List<double>();
        stage.Host.Resized += (_, _) => widths.Add(stage.Popup.Width);
        Assert.Null(stage.Host.Window);
        Assert.Equal(default, stage.Host.Area);

        stage.Open();
        Assert.Same(stage.Scene.Window, stage.Host.Window);
        Assert.Equal(new Rect(0, 0, 450, 800), stage.Host.Area);
        Assert.Equal(new Rect(0, 0, 450, 800), PhonePopupHost.AreaOf(stage.Scene.Window));
        Assert.Equal([450], widths);

        // Raised after the popup is sized to the window again.
        stage.Scene.Window.Width = 400;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal([450, 400], widths);
        Assert.Equal(new Rect(0, 0, 400, 800), stage.Host.Area);

        stage.Popup.Close();
        Dispatcher.UIThread.RunJobs();
        stage.Scene.Window.Width = 300;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal([450, 400], widths);
        Assert.Null(stage.Host.Window);
    }

    // One Back hides the keyboard, closes an AutoSuggestBox's list and takes the focus, before any popup; the next closes
    // the popup the box is in (#150).
    [AvaloniaFact]
    public void Back_Leaves_A_Focused_Box_In_A_Flyout_First()
    {
        var box = new AutoCompleteBox();
        using var stage = new FlyoutTests.Stage();
        stage.Flyout.Content = box;
        stage.OpenAtRest();
        var text = box.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
        Assert.True(text.Focus());
        Dispatcher.UIThread.RunJobs();
        var host = PhoneFlyout.HostOf(stage.Presenter)!;

        var back = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(back);
        Dispatcher.UIThread.RunJobs();
        Assert.True(back.Handled);
        Assert.False(box.IsKeyboardFocusWithin);
        Assert.False(host.IsClosing);
        Assert.True(stage.Flyout.IsOpen);

        var next = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(next);
        Assert.True(next.Handled);
        Assert.True(host.IsClosing);
    }

    // A box on the page under an open popup isn't the popup's: Back closes the popup first.
    [AvaloniaFact]
    public void Back_Closes_A_Flyout_Over_A_Focused_Box_First()
    {
        var box = new AutoCompleteBox { Width = 200 };
        using var stage = new FlyoutTests.Stage(root: new StackPanel { Children = { box } });
        var text = box.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
        Assert.True(text.Focus());
        Dispatcher.UIThread.RunJobs();
        stage.Flyout.ShowAt(box);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(box.IsKeyboardFocusWithin, "the flyout took the focus: nothing to show");
        var host = PhoneFlyout.HostOf(stage.Presenter)!;

        var back = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(back);
        Assert.True(back.Handled);
        Assert.True(host.IsClosing);
    }
}
