using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        // Wider than the reference, the whole turn is the reference's scaled: the axis's depth and the eye too.
        var wider = Math.Max(1, width / 450);
        double ax = Turnstile.AxisX * width / 450, u = point.X - ax, axisZ = Turnstile.AxisZ * wider, depth = -axisZ, eye = Turnstile.Eye * wider;
        var x = ax + u * Math.Cos(r) - depth * Math.Sin(r);
        var z = axisZ + u * Math.Sin(r) + depth * Math.Cos(r);
        var k = eye / (eye - z);
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

    [AvaloniaTheory]
    [InlineData("turnstile")]
    [InlineData("slide")]
    [InlineData("continuum")]
    public void At_Its_End_A_Transition_Hides_The_Page_Going_Out_And_Leaves_Both_At_Rest(string which)
    {
        // As Avalonia's own transitions do, for any host: NavigationPage hides the page going out again, but a
        // TransitioningContentControl, say, leaves it to the transition.
        IPageTransition transition = which switch
        {
            "turnstile" => new TurnstileTransition(),
            "slide" => new SlideTransition(),
            _ => new ContinuumTransition(),
        };
        var from = new Border { Background = Brushes.White };
        var to = new Border { Background = Brushes.Blue, IsVisible = false };
        using var scene = new Scene(new Grid { Children = { from, to } });
        using var clock = EmulatorClock();

        foreach (var forward in new[] { true, false })
        {
            from.IsVisible = true;
            to.IsVisible = false;
            var task = transition.Start(from, to, forward, default);
            Assert.True(to.IsVisible);
            clock.At(clock.Now.TotalMilliseconds + 1000);

            Assert.True(task.IsCompletedSuccessfully);
            Assert.False(from.IsVisible);
            Assert.True(to.IsVisible);
            AssertAtRest(from, to);
        }
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
        // Hidden on top of the page coming in, the page going out takes no input.
        Assert.False(from.IsHitTestVisible);
        Assert.True(to.IsHitTestVisible);
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

        // Narrower than the reference (the Pixel's 411.43), only the axis's place moves with the width.
        var narrow = new Size(411.43, 800);
        AssertNear(Turned(30, new Point(411.43, 0), narrow), TurnstileTransition.ToMatrix(30, narrow).Transform(new Point(411.43, 0)), 0.001, "narrow");
    }

    [AvaloniaFact]
    public void On_A_Wide_Host_The_Back_Turn_Is_The_References_Scaled_And_Never_Reaches_The_Eye()
    {
        // With the eye fixed at 1,000 px a page 1,400 px wide would bring its near edge to the eye at +42°.
        var size = new Size(1800, 900);
        var matrix = TurnstileTransition.ToMatrix(Turnstile.BackFrom, size);
        var reference = TurnstileTransition.ToMatrix(Turnstile.BackFrom, new Size(450, 225));
        foreach (var point in new[] { new Point(0, 0), new Point(1800, 0), new Point(1800, 900), new Point(0, 900) })
        {
            var w = point.X * matrix.M13 + point.Y * matrix.M23 + matrix.M33;
            Assert.True(w > 0, $"{point}: the perspective divides by {w}");
            var scaled = reference.Transform(new Point(point.X / 4, point.Y / 4));
            AssertNear(new Point(scaled.X * 4, scaled.Y * 4), matrix.Transform(point), 0.001, $"{point}");
        }
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

        // The fade: the old page going, the new one not yet shown, within the recording's two frames. The page going
        // out takes no input though it still shows.
        Assert.Equal(1, from.Opacity);
        Assert.False(from.IsHitTestVisible);
        AtTick(clock, 2);
        Assert.InRange(from.Opacity, 0.01, 0.99);
        Assert.False(from.IsHitTestVisible);
        Assert.Equal(0, to.Opacity);
        // Hidden on top, the new page takes no input either.
        Assert.False(to.IsHitTestVisible);
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

    [AvaloniaTheory]
    [InlineData("PopAsync")]
    [InlineData("Alt+Left")]
    [InlineData("back request")]
    public void The_Default_Going_Back_Plays_The_Transition_The_Page_Came_With(string how)
    {
        // As the phone's Frame.GoBack plays the reverse of the page's own transition: a page that came with the slide
        // drops away, however NavigationPage is asked to go back.
        var a = Sheet("A", Brushes.White);
        var nav = new NavigationPage();
        using var scene = new Scene(new PageNavigationHost { Page = nav });
        _ = nav.PushAsync(a);
        Dispatcher.UIThread.RunJobs();
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);
        _ = nav.PushAsync(b, new SlideTransition());
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 40);
        Assert.Same(b, nav.CurrentPage);

        switch (how)
        {
            case "PopAsync":
                _ = nav.PopAsync();
                break;
            case "Alt+Left":
                nav.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left, KeyModifiers = KeyModifiers.Alt });
                break;
            default:
                scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
                break;
        }

        Dispatcher.UIThread.RunJobs();
        var from = PresenterOf(b);
        var to = PresenterOf(a);
        AtTick(clock, 41 + 6);
        Assert.InRange(from.TranslatePoint(new Point(0, 0), nav)!.Value.Y, 1, 136);
        Assert.Null(to.RenderTransform);
        Assert.Equal(0, to.Opacity);
        AtTick(clock, 80);
        Assert.Same(a, nav.CurrentPage);
        AssertAtRest(from, to);
    }

    [AvaloniaFact]
    public void A_Transition_Asked_For_Going_Back_Is_Played_As_Asked()
    {
        var a = Sheet("A", Brushes.White);
        var nav = Host(a, out var scene);
        using var shown = scene;
        using var clock = EmulatorClock();
        var b = Sheet("B", Brushes.Blue);
        _ = nav.PushAsync(b, new SlideTransition());
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 40);

        _ = nav.PopAsync(new ContinuumTransition());
        Dispatcher.UIThread.RunJobs();
        AtTick(clock, 41 + 14);
        // The continuum's back: past the slide, the page under it fading in.
        Assert.InRange(PresenterOf(a).Opacity, 0.3, 0.99);
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
    public void A_NavigationPage_Leaving_Its_Window_Mid_Turn_Leaves_No_Page_Turned_Or_Faded_And_The_Old_One_Hidden()
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
        Assert.False(from.IsVisible);
        Assert.Equal(0, clock.Pending);
    }

    [AvaloniaFact]
    public void A_Pop_Given_Up_Mid_Turn_Leaves_The_Page_Going_Out_Hidden_Not_Over_The_Current_Page()
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
        AtTick(clock, 5);
        var from = PresenterOf(b);
        var to = PresenterOf(a);
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();

        Assert.False(from.IsVisible);
        Assert.True(to.IsVisible);
        AssertAtRest(from, to);
        Assert.True(from.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void A_Transition_Given_Up_From_Another_Thread_Stops_On_The_UI_Thread()
    {
        var from = new Border { Background = Brushes.White };
        var to = new Border { Background = Brushes.Blue };
        using var scene = new Scene(new Grid { Children = { from, to } });
        using var clock = EmulatorClock();
        using var cancel = new CancellationTokenSource();

        var task = new TurnstileTransition().Start(from, to, true, cancel.Token);
        AtTick(clock, 3);
        // A thread of its own: Task.Wait could run a task inline on this one.
        Exception? error = null;
        var other = new Thread(() =>
        {
            try { cancel.Cancel(); }
            catch (Exception e) { error = e; }
        });
        other.Start();
        Assert.True(other.Join(TimeSpan.FromSeconds(5)), "the cancel didn't return");
        Assert.Null(error);
        // Nothing is done on the other thread: the stop waits for the UI thread.
        Assert.False(task.IsCompleted);
        Assert.NotNull(to.RenderTransform);
        Dispatcher.UIThread.RunJobs();

        Assert.True(task.IsCanceled);
        Assert.False(from.IsVisible);
        AssertAtRest(from, to);
        Assert.True(from.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void A_Modal_Drop_Given_Up_Leaves_The_Page_Going_Out_Hidden()
    {
        // Start(page, null): a modal pop, as NavigationPage calls it; any host is left with the page hidden.
        var page = new Border { Background = Brushes.Green };
        using var scene = new Scene(page);
        using var clock = EmulatorClock();
        using var cancel = new CancellationTokenSource();

        _ = new SlideTransition().Start(page, null, false, cancel.Token);
        AtTick(clock, 4);
        Assert.True(page.TranslatePoint(new Point(0, 0), scene.Window)!.Value.Y > 1);
        cancel.Cancel();

        Assert.False(page.IsVisible);
        AssertAtRest(page);
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
