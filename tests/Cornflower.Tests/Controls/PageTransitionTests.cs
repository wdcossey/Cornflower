using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PageReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's three page transitions, <see cref="TurnstileTransition"/>, <see cref="SlideTransition"/> and
/// <see cref="ContinuumTransition"/>, run by Wp81's NavigationPage frame by frame on a <see cref="TestClock"/> whose frames
/// are the emulator's, 15.625 ms apart on its grid, against the recordings (<see cref="PageReadings"/>). Tick n of a
/// transition is the clock's frame n + 1: the transition starts on the first frame after the navigation.
/// </summary>
public class PageTransitionTests
{
    /// <summary>A page: a plain sheet of colour, as the probe's pages were.</summary>
    private static ContentPage Sheet(string name, IBrush brush) =>
        new() { Name = name, Content = new Border { Background = brush } };

    /// <summary>A NavigationPage in the scene with <paramref name="first"/> on it, at rest.</summary>
    private static NavigationPage Host(ContentPage first, out Scene scene)
    {
        var nav = new NavigationPage();
        scene = new Scene(nav);
        _ = nav.PushAsync(first);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(first, nav.CurrentPage);
        return nav;
    }

    private static TestClock EmulatorClock() => new() { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };

    /// <summary>The clock at tick <paramref name="tick"/> of a transition started at time 0.</summary>
    private static void AtTick(TestClock clock, double tick)
    {
        clock.At((tick + 1) * Tick);
        Dispatcher.UIThread.RunJobs();
    }

    private static ContentPresenter PresenterOf(Page page) => Assert.IsType<ContentPresenter>(page.GetVisualParent());

    /// <summary>
    /// Where the emulator drew a point of a 450 × 800 page turned by <paramref name="degrees"/>: about the axis, seen from
    /// the camera (as <c>tools/motion/motionlib.py</c>'s <c>project_y</c>; a positive angle brings the right side nearer).
    /// </summary>
    private static Point Turned(double degrees, Point point, Size? size = null)
    {
        var (width, height) = size is { } s ? (s.Width, s.Height) : (450, 800);
        var r = degrees * Math.PI / 180;
        double ax = Turnstile.AxisX * width / 450, u = point.X - ax, depth = -Turnstile.AxisZ;
        var x = ax + u * Math.Cos(r) - depth * Math.Sin(r);
        var z = Turnstile.AxisZ + u * Math.Sin(r) + depth * Math.Cos(r);
        var k = Turnstile.Eye / (Turnstile.Eye - z);
        return new Point(width / 2 + (x - width / 2) * k, height / 2 + (point.Y - height / 2) * k);
    }

    private static double ExpoOut(double exponent, double u) => (1 - Math.Exp(-exponent * u)) / (1 - Math.Exp(-exponent));

    private static double TurnstileAngle(bool forward, double ms) => forward
        ? Turnstile.ForwardFrom * (1 - ExpoOut(Turnstile.Exponent, Math.Min(1, ms / Turnstile.ForwardMs)))
        : Turnstile.BackFrom * (1 - ExpoOut(Turnstile.Exponent, Math.Min(1, ms / Turnstile.BackMs)));

    private static void AssertNear(Point expected, Point actual, double tolerance, string what)
    {
        Assert.True(Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance,
            $"{what}: expected {expected}, drawn at {actual}");
    }

    private static void AssertAtRest(params Visual[] presenters)
    {
        foreach (var presenter in presenters)
        {
            Assert.Null(presenter.RenderTransform);
            Assert.Equal(1, presenter.Opacity);
        }
    }

    [AvaloniaFact]
    public void The_Theme_Turns_Every_Navigation_And_Slides_A_Modal_Page()
    {
        var nav = new NavigationPage();
        using var scene = new Scene(nav);

        Assert.IsType<TurnstileTransition>(nav.PageTransition);
        Assert.IsType<SlideTransition>(nav.ModalTransition);
    }

    [AvaloniaFact]
    public void The_Turnstile_Forward_Turns_The_New_Page_In_From_Minus_67_Degrees_Over_425_ms()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);

        _ = nav.PushAsync(b);
        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(a);
        var to = PresenterOf(b);

        // The page going out goes at once.
        Assert.Equal(0, from.Opacity);
        for (var tick = 0; tick * Tick < Turnstile.ForwardMs; tick++)
        {
            AtTick(clock, tick);
            var angle = TurnstileAngle(true, tick * Tick);
            AssertNear(Turned(angle, new Point(450, 0)), to.TranslatePoint(new Point(450, 0), nav)!.Value, 0.05, $"top right at tick {tick}");
            AssertNear(Turned(angle, new Point(0, 800)), to.TranslatePoint(new Point(0, 800), nav)!.Value, 0.05, $"bottom left at tick {tick}");
            Assert.Equal(0, from.Opacity);
        }

        AtTick(clock, Turnstile.ForwardMs / Tick + 1);
        Assert.Same(b, nav.CurrentPage);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
        Assert.Equal(new Point(450, 0), to.TranslatePoint(new Point(450, 0), nav));
    }

    [AvaloniaFact]
    public void The_Turnstiles_First_Ticks_Are_The_Angles_The_Emulator_Showed_First()
    {
        for (var tick = 0; tick < Turnstile.ForwardFirstTicks.Length; tick++)
            Assert.Equal(Turnstile.ForwardFirstTicks[tick], TurnstileTransition.AngleAt(TimeSpan.FromMilliseconds(tick * Tick), true), 0.15);
        Assert.Equal(Turnstile.BackFirst, TurnstileTransition.AngleAt(TimeSpan.Zero, false), 0.05);
        Assert.InRange(Math.Abs(TurnstileTransition.AngleAt(TimeSpan.FromMilliseconds(Turnstile.WithinADegreeMs), true)), 0.5, 1.0);
        Assert.InRange(Math.Abs(TurnstileTransition.AngleAt(TimeSpan.FromMilliseconds(Turnstile.WithinADegreeMs), false)), 0.3, 1.0);
    }

    [AvaloniaFact]
    public void The_Turnstile_Back_Turns_The_Page_Under_It_In_From_42_Degrees_Over_430_ms()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        var b = Sheet("B", Brushes.Blue);
        _ = nav.PushAsync(b, null);
        Dispatcher.UIThread.RunJobs();
        using var clock = EmulatorClock();

        _ = nav.PopAsync();
        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(b);
        var to = PresenterOf(a);

        Assert.Equal(0, from.Opacity);
        for (var tick = 0; tick * Tick < Turnstile.BackMs; tick++)
        {
            AtTick(clock, tick);
            var angle = TurnstileAngle(false, tick * Tick);
            AssertNear(Turned(angle, new Point(450, 0)), to.TranslatePoint(new Point(450, 0), nav)!.Value, 0.05, $"top right at tick {tick}");
            AssertNear(Turned(angle, new Point(0, 800)), to.TranslatePoint(new Point(0, 800), nav)!.Value, 0.05, $"bottom left at tick {tick}");
        }

        AtTick(clock, Turnstile.BackMs / Tick + 1);
        Assert.Same(a, nav.CurrentPage);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
    }

    [AvaloniaFact]
    public void The_Turnstiles_Axis_Is_A_Fraction_Of_The_Pages_Width_And_The_Camera_Is_Over_Its_Centre()
    {
        // The axis is PlaneProjection's CenterOfRotationX, a fraction of the width: 44 of 450 on the reference, so 88 of
        // 900; its depth and the camera's distance are in view px.
        var size = new Size(900, 600);
        var matrix = TurnstileTransition.ToMatrix(-30, size);
        foreach (var point in new[] { new Point(0, 0), new Point(900, 0), new Point(450, 600) })
            AssertNear(Turned(-30, point, size), matrix.Transform(point), 0.001, $"{point}");
        Assert.Equal(new Point(900, 600), TurnstileTransition.ToMatrix(0, size).Transform(new Point(900, 600)));
    }

    [AvaloniaFact]
    public void The_Slide_Forward_Raises_The_New_Page_From_200_px_Over_350_ms()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);

        _ = nav.PushAsync(b, new SlideTransition());
        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(a);
        var to = PresenterOf(b);

        Assert.Equal(0, from.Opacity);
        foreach (var (tick, offset) in Slide.Forward)
        {
            AtTick(clock, tick);
            Assert.Equal(offset, to.TranslatePoint(new Point(0, 0), nav)!.Value.Y, 0.5);
            Assert.Equal(0, to.TranslatePoint(new Point(0, 0), nav)!.Value.X);
            Assert.Equal(0, from.Opacity);
        }

        AtTick(clock, 350 / Tick + 1);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
    }

    [AvaloniaFact]
    public void The_Slide_Back_Drops_The_Page_Going_Out_Faster_Each_Frame_Then_Shows_The_Page_Under_It()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        var b = Sheet("B", Brushes.Blue);
        _ = nav.PushAsync(b, null);
        Dispatcher.UIThread.RunJobs();
        using var clock = EmulatorClock();

        _ = nav.PopAsync(new SlideTransition());
        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(b);
        var to = PresenterOf(a);

        for (var tick = 0; tick <= Slide.BackLastTick; tick++)
        {
            AtTick(clock, tick);
            Assert.Equal(0, to.Opacity);
            var back = Slide.BackLastTick - tick;
            if (back < Slide.BackFromLast.Length)
                Assert.Equal(Slide.BackFromLast[back], from.TranslatePoint(new Point(0, 0), nav)!.Value.Y, 2.0);
        }

        AtTick(clock, Slide.BackLastTick + 1);
        Assert.Same(a, nav.CurrentPage);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
    }

    [AvaloniaFact]
    public void The_Continuum_Forward_Fades_The_Old_Page_Then_Grows_The_New_One_About_Its_Centre()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);

        _ = nav.PushAsync(b, new ContinuumTransition());
        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(a);
        var to = PresenterOf(b);

        // The fade: the old page going, the new one not yet shown, within the recording's two frames.
        Assert.Equal(1, from.Opacity);
        AtTick(clock, 2);
        Assert.InRange(from.Opacity, 0.01, 0.99);
        Assert.Equal(0, to.Opacity);
        AtTick(clock, Continuum.FadeFramesMs / Tick);
        Assert.Equal(0, from.Opacity);

        // The growth: from the recording's first scale, about the centre, at rest within its 281 to 312 ms.
        var first = to.TranslatePoint(new Point(450, 0), nav)!.Value;
        var scale = (first.X - 225) / 225;
        Assert.InRange(scale, Continuum.FirstScaleLow - 0.003, Continuum.FirstScaleHigh);
        Assert.Equal(400 - 400 * scale, first.Y, 0.01);
        Assert.Equal(1, to.Opacity);
        var grownAt = Continuum.FadeFramesMs + Continuum.GrownByMsHigh;
        clock.At(grownAt);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(450, to.TranslatePoint(new Point(450, 0), nav)!.Value.X, 0.25);

        AtTick(clock, 30);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
    }

    [AvaloniaFact]
    public void The_Continuum_Back_Drops_The_Page_Going_Out_Then_Fades_The_Page_Under_It_In()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        var b = Sheet("B", Brushes.Blue);
        _ = nav.PushAsync(b, null);
        Dispatcher.UIThread.RunJobs();
        using var clock = EmulatorClock();

        _ = nav.PopAsync(new ContinuumTransition());
        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(b);
        var to = PresenterOf(a);

        AtTick(clock, Slide.BackLastTick);
        Assert.Equal(Slide.BackFromLast[0], from.TranslatePoint(new Point(0, 0), nav)!.Value.Y, 2.0);
        Assert.Equal(0, to.Opacity);

        AtTick(clock, Slide.BackLastTick + 1);
        Assert.Equal(0, from.Opacity);
        Assert.Equal(Continuum.BackFirstOpacity, to.Opacity, 0.02);
        Assert.Null(to.RenderTransform);

        clock.At(Tick + (Slide.BackLastTick + 1) * Tick + Continuum.BackFadeMs - 1);
        Dispatcher.UIThread.RunJobs();
        Assert.InRange(to.Opacity, 0.99, 1);
        AtTick(clock, 40);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
    }

    [AvaloniaFact]
    public void A_Navigation_In_The_Middle_Of_A_Turn_Is_Ignored_And_The_Turn_Ends_At_Rest()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);
        var c = Sheet("C", Brushes.Red);

        _ = nav.PushAsync(b);
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 5);
        Assert.NotNull(PresenterOf(b).RenderTransform);

        // Avalonia's NavigationPage runs one navigation at a time, and drops one asked for while another runs.
        _ = nav.PushAsync(c, new SlideTransition());
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 40);

        Assert.Same(b, nav.CurrentPage);
        Assert.Equal(2, nav.StackDepth);
        var presenters = Presenters(nav);
        AssertAtRest([.. presenters]);
        Assert.Equal(1, presenters.Count(p => p.IsVisible));
        Assert.Equal(new Point(0, 0), PresenterOf(b).TranslatePoint(new Point(0, 0), nav));
    }

    [AvaloniaFact]
    public void A_NavigationPage_Leaving_Its_Window_Mid_Turn_Leaves_No_Page_Turned_Or_Hidden()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);

        _ = nav.PushAsync(b);
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 5);
        var from = PresenterOf(a);
        var to = PresenterOf(b);
        Assert.NotNull(to.RenderTransform);
        Assert.Equal(0, from.Opacity);

        // Avalonia cancels the transition when the NavigationPage leaves the visual tree.
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        AssertAtRest(from, to);
        Assert.Equal(0, clock.Pending);
    }

    private static List<ContentPresenter> Presenters(NavigationPage nav) => nav.GetVisualDescendants().OfType<ContentPresenter>()
        .Where(p => p.Name is "PART_PagePresenter" or "PART_PageBackPresenter").ToList();

    [AvaloniaFact]
    public void A_Modal_Page_Rises_From_200_px_And_Drops_Away_Again()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var modal = Sheet("M", Brushes.Green);

        _ = nav.PushModalAsync(modal);
        Dispatcher.UIThread.RunJobs();
        var presenter = PresenterOf(modal);
        AtTick(clock, 0);
        Assert.Equal(200, presenter.TranslatePoint(new Point(0, 0), nav)!.Value.Y, 0.05);
        AtTick(clock, 30);
        AssertAtRest(presenter);
        Assert.True(presenter.IsVisible);

        _ = nav.PopModalAsync();
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 30 + 6);
        Assert.InRange(presenter.TranslatePoint(new Point(0, 0), nav)!.Value.Y, 1, 136);
        AtTick(clock, 60);
        Assert.False(presenter.IsVisible);
        AssertAtRest(presenter);
    }

    [AvaloniaFact]
    public void A_Transition_With_No_Page_Either_Side_Or_Already_Cancelled_Does_Nothing()
    {
        var page = new Border();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.True(new TurnstileTransition().Start(null, null, true, default).IsCompletedSuccessfully);
        Assert.True(new SlideTransition().Start(null, page, true, cancelled.Token).IsCanceled);
        AssertAtRest(page);
    }
}
