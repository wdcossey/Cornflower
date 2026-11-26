using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain RefreshContainer and RefreshVisualizer (ADR 0007, #241): a pull opens a band on the page's surface at the
/// container's top edge, with Wp81's ProgressRing in it: off at rest, running dimmed while the pull is short of a
/// refresh, at full strength once letting go refreshes and while refreshing; Avalonia's own turn, pulse, spin and easing
/// left out (rule 8). Simple's draws its arrow icon, unclipped and eased, so these fail without the themes.
/// </summary>
public class RefreshContainerTests
{
    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>A 400-tall container round a list of 30 rows, 40 each, with a 100-tall line above it on the page.</summary>
    private static (Scene Scene, RefreshContainer Container, ScrollViewer List) Pullable(ThemeVariant? variant = null)
    {
        var rows = new StackPanel();
        for (var i = 1; i <= 30; i++)
            rows.Children.Add(new TextBlock { Text = "row " + i, Height = 40 });
        var list = new ScrollViewer { Content = rows };
        var container = new RefreshContainer { Content = list, Height = 400, IsMouseEnabled = true };
        var page = new StackPanel { Children = { new TextBlock { Text = "above", Height = 100 }, container } };
        return (variant is null ? new Scene(page) : new Scene(page, variant), container, list);
    }

    private static RefreshVisualizer Visualizer(RefreshContainer container) => Assert.IsType<RefreshVisualizer>(container.Visualizer);

    private static ProgressRing Ring(RefreshContainer container) => Part<ProgressRing>(Visualizer(container), "PART_Ring");

    private static RefreshVisualizerState State(RefreshContainer container) =>
        Visualizer(container).GetValue(RefreshVisualizer.RefreshVisualizerStateProperty);

    /// <summary>The mouse down on the list's top and drawn down by <paramref name="distance"/>: a pull.</summary>
    private static Point Pull(Scene scene, ScrollViewer list, double distance)
    {
        var at = list.TranslatePoint(new Point(200, 20), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        return To(scene, at, distance);
    }

    /// <summary>The held mouse moved to <paramref name="distance"/> below <paramref name="start"/>, in steps.</summary>
    private static Point To(Scene scene, Point start, double distance)
    {
        for (var step = 5.0; step <= distance; step += 5)
        {
            scene.Window.MouseMove(start + new Point(0, step), RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();
        }
        return start;
    }

    private static void LetGo(Scene scene, Point start, double distance)
    {
        scene.Window.MouseUp(start + new Point(0, distance), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void The_Container_Clips_So_The_Band_Waits_Out_Of_Sight()
    {
        var (scene, container, _) = Pullable();
        using var _scene = scene;

        Assert.True(Part<Grid>(container, "PART_Root").ClipToBounds);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(container.Background).Color);
        // At rest the band sits its own height above the container, so the clip keeps it off the line above.
        Assert.Equal(100, Visualizer(container).Bounds.Height, 0.5);
        Assert.Equal(-100, Visualizer(container).RenderTransform!.Value.M32, 0.5);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void The_Sign_Is_Wp81s_ProgressRing_In_The_Accent_On_The_Pages_Surface(string variant)
    {
        var (scene, container, _) = Pullable(Variant(variant));
        using var _scene = scene;
        var visualizer = Visualizer(container);
        var ring = Ring(container);

        Assert.Same(Resource<IBrush>(visualizer, "PhoneBackgroundBrush"), visualizer.Background);
        Assert.Same(Resource<IBrush>(visualizer, "PhoneBackgroundBrush"), Part<Grid>(visualizer, "PART_Root").Background);
        Assert.Same(Resource<IBrush>(visualizer, "ProgressBarIndeterminateForegroundThemeBrush"), ring.Foreground);
        // The visualizer's Foreground is the ring's, so an app can colour the sign where it sets the visualizer.
        visualizer.Foreground = Brushes.Red;
        Assert.Same(Brushes.Red, ring.Foreground);
        // Wp81's ring, its own theme and parts: the 57.5 ring, centred in the 100-tall band.
        Assert.NotNull(Part<Ellipse>(ring, "PART_E1"));
        Assert.Equal(new Rect(196.25, 21.25, 57.5, 57.5), ring.Bounds);
        // What Avalonia turns, fades, pulses and spins is an empty Panel, not the ring.
        Assert.IsType<Panel>(visualizer.Content);
        Assert.Empty(((Panel)visualizer.Content!).Children);
        Assert.False(visualizer.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void At_Rest_The_Ring_Is_Off()
    {
        var (scene, container, _) = Pullable();
        using var _scene = scene;

        Assert.Equal(RefreshVisualizerState.Idle, State(container));
        Assert.False(Ring(container).IsActive);
    }

    [AvaloniaFact]
    public void A_Pull_Runs_The_Ring_Dimmed_Then_Full_Where_Letting_Go_Refreshes()
    {
        var (scene, container, list) = Pullable();
        using var _scene = scene;
        var ring = Ring(container);

        var start = Pull(scene, list, 40);
        Assert.Equal(RefreshVisualizerState.Interacting, State(container));
        Assert.True(ring.IsActive);
        Assert.Equal(Resource<double>(ring, "CornflowerPlainRefreshPullOpacity"), ring.Opacity);

        To(scene, start, 95);
        Assert.Equal(RefreshVisualizerState.Pending, State(container));
        Assert.True(ring.IsActive);
        Assert.Equal(1, ring.Opacity);

        // Back short of it: dimmed again; let go there, and nothing refreshes.
        var refreshes = 0;
        container.RefreshRequested += (_, _) => refreshes++;
        scene.Window.MouseMove(start + new Point(0, 40), RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(RefreshVisualizerState.Interacting, State(container));
        Assert.Equal(Resource<double>(ring, "CornflowerPlainRefreshPullOpacity"), ring.Opacity);
        LetGo(scene, start, 40);
        Assert.Equal(0, refreshes);
        Assert.False(ring.IsActive);
    }

    [AvaloniaFact]
    public void Letting_Go_Runs_The_Ring_Until_The_Refresh_Completes()
    {
        var (scene, container, list) = Pullable();
        using var _scene = scene;
        var ring = Ring(container);
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) => deferral = e.GetDeferral();

        var start = Pull(scene, list, 95);
        LetGo(scene, start, 95);

        Assert.NotNull(deferral);
        Assert.Equal(RefreshVisualizerState.Refreshing, State(container));
        Assert.True(ring.IsActive);
        Assert.Equal(1, ring.Opacity);

        deferral!.Complete();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(RefreshVisualizerState.Idle, State(container));
        Assert.False(ring.IsActive);
        Assert.Equal(default, list.Offset);
    }

    [AvaloniaFact]
    public void RequestRefresh_Runs_The_Ring_With_No_Pull()
    {
        var (scene, container, list) = Pullable();
        using var _scene = scene;
        RefreshCompletionDeferral? deferral = null;
        container.RefreshRequested += (_, e) => deferral = e.GetDeferral();

        container.RequestRefresh();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(RefreshVisualizerState.Refreshing, State(container));
        Assert.True(Ring(container).IsActive);
        // The band shown at the container's top: moved down its own height, onto the container.
        Assert.Equal(100, ElementComposition.GetElementVisual(Visualizer(container))!.Offset.Y, 0.5);
        deferral!.Complete();
        Dispatcher.UIThread.RunJobs();
        Assert.False(Ring(container).IsActive);
        Assert.Equal(default, list.Offset);
    }

    [AvaloniaFact]
    public void Peeking_Shows_The_Ring_At_Full_Strength()
    {
        var (scene, container, _) = Pullable();
        using var _scene = scene;
        // Peeking is the band partly shown with no finger on it: the provider's ratio above 0 while no pull is held.
        // Avalonia keeps the provider internal, so the test reaches it as the container's adapter does.
        var provider = typeof(RefreshVisualizer).GetProperty("RefreshInfoProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(Visualizer(container))!;
        provider.GetType().GetProperty("InteractionRatio")!.SetValue(provider, 0.5);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(RefreshVisualizerState.Peeking, State(container));
        Assert.True(Ring(container).IsActive);
        Assert.Equal(1, Ring(container).Opacity);
    }

    [AvaloniaFact]
    public void The_Band_And_The_List_Follow_The_Pull_Uneased()
    {
        var (scene, container, list) = Pullable();
        using var _scene = scene;
        // Avalonia's container eases both over 150 ms.
        Assert.NotNull(ElementComposition.GetElementVisual(Visualizer(container))!.ImplicitAnimations);
        Assert.NotNull(ElementComposition.GetElementVisual(list)!.ImplicitAnimations);

        var start = Pull(scene, list, 40);

        Assert.Null(ElementComposition.GetElementVisual(Visualizer(container))!.ImplicitAnimations);
        Assert.Null(ElementComposition.GetElementVisual(list)!.ImplicitAnimations);
        Assert.Equal(40, ElementComposition.GetElementVisual(Visualizer(container))!.Offset.Y, 0.5);
        LetGo(scene, start, 40);
    }
}
