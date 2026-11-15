using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ScrollViewer and ScrollBar (<c>Controls/ScrollViewer.axaml</c>, <c>Controls/ScrollBar.axaml</c>, with
/// <c>PhoneScrollIndicators</c>) against the phone's styles (scrollviewer.json, scrollbar.json), the indicator's sums in
/// WinRT's ScrollBar code, and the template's indicator timeline. docs/controls/ScrollViewer has the plan.
/// </summary>
public class ScrollViewerTests
{
    /// <summary>The ScrollBar's minimum: <c>ScrollBarMinThemeWidth</c> and <c>ScrollBarMinThemeHeight</c>, 7.</summary>
    private const double BarMin = 7;

    /// <summary>
    /// The ScrollBar's laid-out thickness: 7 is 16.8 raw pixels at 2.4, which layout makes 17 (7.0833), as the phone's
    /// rounding to the nearest pixel does too (ADR 0003).
    /// </summary>
    private static readonly double BarThickness = LaidOut(BarMin);

    /// <summary>The panning thumb's laid-out thickness: 2.4 is 5.76 raw pixels, which layout makes 6 (2.5), as the phone's does.</summary>
    private static readonly double ThumbLaidOut = LaidOut(ThumbThickness);

    /// <summary>A length as layout gives it at the reference scale: up to the raw pixel grid.</summary>
    private static double LaidOut(double length) => Math.Ceiling(length * Scene.ReferenceScaling - 1e-9) / Scene.ReferenceScaling;

    private static void Near(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 0.001);
        Assert.Equal(expected.Y, actual.Y, 0.001);
        Assert.Equal(expected.Width, actual.Width, 0.001);
        Assert.Equal(expected.Height, actual.Height, 0.001);
    }

    /// <summary>The panning thumb's thickness: <c>ScrollBarPanningThumbThemeWidth</c> and <c>-Height</c>, 2.4.</summary>
    private const double ThumbThickness = 2.4;

    /// <summary>The panning thumb's shortest: <c>ScrollBarMinThemeHeight</c> on the Rectangle, 7.</summary>
    private const double ThumbMinLength = 7;

    /// <summary>The panning roots' <c>MinHeight</c> and <c>MinWidth</c> in the phone's template: 53.</summary>
    private const double PanningRootMinLength = 53;

    /// <summary>
    /// <c>ScrollBarPanningBackgroundThemeBrush</c>, <c>PhoneBaseLowColor</c>, drawn on the page: #73FFFFFF on black is
    /// #737373; #40000000 on white is #BFBFBF.
    /// </summary>
    private static readonly Color DarkIndicator = Color.FromRgb(0x73, 0x73, 0x73);

    private static readonly Color LightIndicator = Color.FromRgb(0xBF, 0xBF, 0xBF);

    /// <summary>A ScrollViewer filling the 450 × 800 reference view over the page's background, holding a tall page.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(double contentHeight = 2000, double contentWidth = double.NaN, ThemeVariant? variant = null, ScrollViewer? viewer = null, Control? content = null, double height = Scene.ReferenceHeight)
        {
            Content = content ?? new Border { Height = contentHeight, Width = contentWidth, Background = Brushes.Transparent };
            Viewer = viewer ?? new ScrollViewer();
            Viewer.Content = Content;
            var page = new Border { Child = Viewer };
            page.Bind(Border.BackgroundProperty, page.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(page, variant, height: height);
        }

        public ScrollViewer Viewer { get; }

        public Control Content { get; }

        public Window Window => _scene.Window;

        public ScrollBar Vertical => Part<ScrollBar>(Viewer, "PART_VerticalScrollBar");

        public ScrollBar Horizontal => Part<ScrollBar>(Viewer, "PART_HorizontalScrollBar");

        public Thumb VerticalThumb => Part<Thumb>(Vertical, "VerticalPanningThumb");

        public Thumb HorizontalThumb => Part<Thumb>(Horizontal, "HorizontalPanningThumb");

        /// <summary>A part's bounds in the window.</summary>
        public Rect InWindow(Visual part) => new(part.TranslatePoint(default, Window)!.Value, part.Bounds.Size);

        public void ScrollTo(double y)
        {
            Viewer.Offset = new Vector(Viewer.Offset.X, y);
            Dispatcher.UIThread.RunJobs();
        }

        public Avalonia.Media.Imaging.WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>
    /// The reference app's ScrollBar specimens (run <c>20261115-1921-dev11</c>, <c>tools/refapp/app/Catalog.cs</c>): a
    /// ScrollBar 200 long, <c>Maximum</c> 100, <c>ViewportSize</c> 50, <c>Value</c> 25, in a slot 19 from the left and
    /// right and 12 from the top of a 450 wide stage (240 tall, or 60 for the horizontal one), over the page's
    /// background; <c>vertical</c> and <c>horizontal</c> with <c>IndicatorMode</c> <c>TouchIndicator</c>, at the right or
    /// the left, and <c>no-indicator</c> with none set.
    /// </summary>
    internal sealed class BarStage : IDisposable
    {
        private readonly Scene _scene;

        public BarStage(string specimen, ThemeVariant? variant = null, bool disabled = false)
        {
            var horizontal = specimen == "horizontal";
            Bar = new ScrollBar
            {
                Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical,
                Maximum = 100,
                ViewportSize = 50,
                Value = 25,
                IsEnabled = !disabled,
                HorizontalAlignment = horizontal ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };
            if (horizontal)
                Bar.Width = 200;
            else
                Bar.Height = 200;
            if (specimen != "no-indicator")
                PhoneScrollIndicators.SetIndicatorMode(Bar, PhoneIndicatorMode.TouchIndicator);

            var slot = new Grid { Margin = new Thickness(19, 12, 19, 0), Children = { Bar } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, height: horizontal ? 60 : 240);
        }

        public ScrollBar Bar { get; }

        public Window Window => _scene.Window;

        public Avalonia.Media.Imaging.WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>
    /// The reference app's ScrollViewer specimens (run <c>20261115-1921-dev11</c>): a ScrollViewer 280 tall over 14
    /// lines of 20 px text, 40 apart (560 tall), in the same slot of a 450 × 300 stage, scrolled from code to
    /// <paramref name="offset"/>.
    /// </summary>
    internal static Stage Lines(double offset, ThemeVariant? variant = null)
    {
        var lines = new StackPanel();
        for (var i = 1; i <= 14; i++)
            lines.Children.Add(new Border { Height = 40, Child = new TextBlock { Text = "line " + i, FontSize = 20 } });
        var viewer = new ScrollViewer { Height = 280, Margin = new Thickness(19, 12, 19, 0) };
        var stage = new Stage(viewer: viewer, content: lines, variant: variant, height: 300);
        if (offset > 0)
            stage.ScrollTo(offset);
        return stage;
    }

    /// <summary>The raw pixels of <paramref name="frame"/> that aren't the page's colour at its corner: left, right, top, bottom.</summary>
    private static (int Left, int Right, int Top, int Bottom) Drawn(Avalonia.Media.Imaging.WriteableBitmap frame)
    {
        var page = Frames.PixelAt(frame, 0, 0);
        int left = int.MaxValue, right = -1, top = int.MaxValue, bottom = -1;
        for (var y = 0; y < frame.PixelSize.Height; y++)
        for (var x = 0; x < frame.PixelSize.Width; x++)
        {
            if (Frames.PixelAt(frame, x, y) == page)
                continue;
            left = Math.Min(left, x);
            right = Math.Max(right, x);
            top = Math.Min(top, y);
            bottom = Math.Max(bottom, y);
        }
        return (left, right, top, bottom);
    }

    private static T Part<T>(Visual of, string name) where T : Control =>
        of.GetVisualDescendants().OfType<T>().Single(c => c.Name == name && c.TemplatedParent == of);

    private static PhoneIndicatorMode ModeOf(Control control) => PhoneScrollIndicators.GetIndicatorMode(control);

    /// <summary>The fades, as the recording read them: in over 370 ms, out over 340 ms.</summary>
    private const double FadeInMs = 370, FadeOutMs = 340;

    /// <summary>Scrolls a step every 40 ms from <paramref name="from"/> to <paramref name="until"/> ms, so the touch's indicator stays and fades in whole.</summary>
    private static void KeepScrolling(Stage stage, TestClock clock, double from, double until)
    {
        for (var t = from; t <= until; t += 40)
        {
            clock.At(t);
            stage.ScrollTo(stage.Viewer.Offset.Y + 5);
        }
    }

    private static double Shown(ScrollBar bar) =>
        bar.GetVisualDescendants().OfType<Track>().Single(t => t.IsVisible).Opacity;

    /// <summary>The middle of the vertical panning thumb, in raw pixels of a frame.</summary>
    private static (int X, int Y) ThumbPixel(Stage stage)
    {
        var at = stage.InWindow(stage.VerticalThumb).Center;
        return ((int)(at.X * Scene.ReferenceScaling), (int)(at.Y * Scene.ReferenceScaling));
    }

    // scrollviewer.json's setters, those Avalonia has, and IsTabStop False: Avalonia's ScrollViewer takes no focus.
    [AvaloniaFact]
    public void The_ScrollViewers_Style_Sets_What_The_Phone_Sets()
    {
        using var stage = new Stage();
        var v = stage.Viewer;

        Assert.Equal(ScrollBarVisibility.Disabled, v.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Visible, v.VerticalScrollBarVisibility);
        Assert.Equal(new Thickness(0), v.Padding);
        Assert.Equal(new Thickness(0), v.BorderThickness);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)v.BorderBrush!).Color);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)v.Background!).Color);
        Assert.False(v.Focusable);
        Assert.True(PhoneScrollIndicators.GetIsEnabled(v));
    }

    // The phone's template: a Border, then a Grid of two columns (*, Auto) and two rows (*, Auto) with the Background;
    // the presenter over all four cells with the Padding as its Margin; the vertical bar in the second column at the
    // right, the horizontal in the second row.
    [AvaloniaFact]
    public void The_Template_Is_The_Phones_Part_For_Part()
    {
        using var stage = new Stage();
        var v = stage.Viewer;
        var presenter = Part<ScrollContentPresenter>(v, "PART_ContentPresenter");
        var grid = (Grid)presenter.GetVisualParent()!;
        var border = (Border)grid.GetVisualParent()!;

        Assert.Same(v, border.GetVisualParent());
        Assert.Equal([new GridLength(1, GridUnitType.Star), GridLength.Auto], grid.ColumnDefinitions.Select(c => c.Width));
        Assert.Equal([new GridLength(1, GridUnitType.Star), GridLength.Auto], grid.RowDefinitions.Select(r => r.Height));
        Assert.Equal((2, 2), (Grid.GetRowSpan(presenter), Grid.GetColumnSpan(presenter)));
        Assert.Equal((0, 1, HorizontalAlignment.Right), (Grid.GetRow(stage.Vertical), Grid.GetColumn(stage.Vertical), stage.Vertical.HorizontalAlignment));
        Assert.Equal((1, 0), (Grid.GetRow(stage.Horizontal), Grid.GetColumn(stage.Horizontal)));
        Assert.Equal(Orientation.Vertical, stage.Vertical.Orientation);
        Assert.Equal(Orientation.Horizontal, stage.Horizontal.Orientation);

        // The indicator lies over the content: the presenter is the whole view.
        Assert.Equal(new Rect(0, 0, Scene.ReferenceWidth, Scene.ReferenceHeight), stage.InWindow(presenter));
    }

    // The phone's ScrollContentPresenter takes the Padding as its Margin, so what scrolls is clipped inside it. The
    // Border draws the BorderBrush and the Grid the Background.
    [AvaloniaFact]
    public void Padding_Insets_The_Presenter_And_The_Border_And_Background_Are_Drawn()
    {
        var viewer = new ScrollViewer { Padding = new Thickness(10, 20, 30, 40), BorderThickness = new Thickness(5), BorderBrush = Brushes.Red, Background = Brushes.Blue };
        using var stage = new Stage(viewer: viewer);
        var presenter = Part<ScrollContentPresenter>(viewer, "PART_ContentPresenter");

        Assert.Equal(new Rect(15, 25, 450 - 15 - 35, 800 - 25 - 45), stage.InWindow(presenter));
        Assert.True(presenter.ClipToBounds);

        using var frame = stage.Capture();
        Assert.Equal(Colors.Red, Frames.PixelAt(frame, 2 * 12 / 5, 400 * 12 / 5));
        Assert.Equal(Colors.Blue, Frames.PixelAt(frame, 10 * 12 / 5, 400 * 12 / 5));
    }

    // The phone's template binds the presenter's ContentTemplate to the ScrollViewer's; Avalonia's presenter takes only
    // the Content itself. Both themes bind it.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_ContentTemplate_Is_The_ScrollViewers(bool scrollbarless)
    {
        var viewer = new ScrollViewer
        {
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new TextBlock { Text = $"templated {text}" }),
        };
        if (scrollbarless)
            viewer.Bind(StyledElement.ThemeProperty, viewer.GetResourceObservable("Wp81ScrollViewerScrollBarlessTheme"));
        using var stage = new Stage(viewer: viewer, content: null);
        viewer.Content = "data";
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(viewer.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "templated data");
    }

    // scrollbar.json's setters: 7 at least each way, transparent, no tab stop. It takes no pointer: the phone's
    // indicator isn't a bar to drag.
    [AvaloniaFact]
    public void The_ScrollBars_Style_Sets_What_The_Phone_Sets()
    {
        var viewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
        using var stage = new Stage(contentWidth: 900, viewer: viewer);
        foreach (var bar in new[] { stage.Vertical, stage.Horizontal })
        {
            Assert.Equal(BarMin, bar.MinWidth);
            Assert.Equal(BarMin, bar.MinHeight);
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)bar.Background!).Color);
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)bar.Foreground!).Color);
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)bar.BorderBrush!).Color);
            Assert.False(bar.Focusable);
            Assert.False(bar.IsHitTestVisible);
        }

        Assert.Equal(PanningRootMinLength, Part<Track>(stage.Vertical, "VerticalPanningRoot").MinHeight);
        Assert.Equal(PanningRootMinLength, Part<Track>(stage.Horizontal, "HorizontalPanningRoot").MinWidth);
    }

    // The vertical bar is 7 wide at the right edge; its panning thumb 2.4 wide, centred in it; as long as the viewport
    // is to the extent (800 of 2000: 320), and moved down the rest of the track as the offset is to the scrollable
    // height (WinRT's ConvertViewportSizeToDisplayUnits and RefreshTrackLayout).
    [AvaloniaFact]
    public void The_Indicator_Is_Sized_And_Placed_As_WinRTs_Code_Places_It()
    {
        using var stage = new Stage(contentHeight: 2000);

        Near(new Rect(450 - BarThickness, 0, BarThickness, 800), stage.InWindow(stage.Vertical));
        var thumb = stage.InWindow(stage.VerticalThumb);
        Assert.Equal(ThumbLaidOut, thumb.Width, 0.001);
        Assert.Equal(450 - BarThickness / 2, thumb.Center.X, 0.25);
        Assert.Equal(800.0 * 800 / 2000, thumb.Height, 0.5);
        Assert.Equal(0, thumb.Y, 0.5);

        stage.ScrollTo(600);
        thumb = stage.InWindow(stage.VerticalThumb);
        Assert.Equal(600.0 / 1200 * (800 - 320), thumb.Y, 0.5);

        stage.ScrollTo(1200);
        thumb = stage.InWindow(stage.VerticalThumb);
        Assert.Equal(800, thumb.Bottom, 0.5);
    }

    // The thumb is never shorter than ScrollBarMinThemeHeight, 7, and isn't shown when there is nothing to scroll.
    [AvaloniaFact]
    public void The_Thumb_Is_At_Least_Seven_Long_And_Hidden_With_Nothing_To_Scroll()
    {
        using (var stage = new Stage(contentHeight: 800 * 400))
            Assert.Equal(ThumbMinLength, stage.InWindow(stage.VerticalThumb).Height, 0.5);

        using (var stage = new Stage(contentHeight: 500))
            Assert.False(stage.VerticalThumb.IsVisible);
    }

    // The horizontal bar: in the second row, 7 tall; its thumb 2.4 tall, centred in it.
    [AvaloniaFact]
    public void The_Horizontal_Indicator_Is_The_Phones_Too()
    {
        var viewer = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
        using var stage = new Stage(contentHeight: 2000, contentWidth: 900, viewer: viewer);

        Near(new Rect(0, 800 - BarThickness, 450 - BarThickness, BarThickness), stage.InWindow(stage.Horizontal));
        var thumb = stage.InWindow(stage.HorizontalThumb);
        Assert.Equal(ThumbLaidOut, thumb.Height, 0.001);
        Assert.Equal(800 - BarThickness / 2, thumb.Center.Y, 0.25);
        Assert.Equal((450 - BarThickness) * 450 / 900, thumb.Width, 0.5);
        Assert.False(Part<Track>(stage.Horizontal, "VerticalPanningRoot").IsVisible);
        Assert.False(Part<Track>(stage.Vertical, "HorizontalPanningRoot").IsVisible);
    }

    // NoIndicator at rest: nothing is drawn where the thumb is, though there is something to scroll.
    [AvaloniaFact]
    public void At_Rest_No_Indicator_Is_Drawn()
    {
        using var stage = new Stage();

        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
        Assert.Equal(0, Shown(stage.Vertical));
        var (x, y) = ThumbPixel(stage);
        using var frame = stage.Capture();
        Assert.Equal(Colors.Black, Frames.PixelAt(frame, x, y));
    }

    // A scroll shows the touch's indicator, in ScrollBarPanningBackgroundThemeBrush, dark and light: the ScrollViewer
    // goes to TouchIndicator, which sets both ScrollBars' IndicatorMode.
    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void A_Scroll_Shows_The_Indicator_In_The_Phones_Brush(string variant)
    {
        using var clock = new TestClock();
        using var stage = new Stage(variant: variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        KeepScrolling(stage, clock, 0, 400);

        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Viewer));
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Horizontal));
        Assert.Equal(1, Shown(stage.Vertical));
        var (x, y) = ThumbPixel(stage);
        using var frame = stage.Capture();
        Assert.Equal(variant == "Dark" ? DarkIndicator : LightIndicator, Frames.PixelAt(frame, x, y));
        Assert.Equal(variant == "Dark" ? Colors.Black : Colors.White, Frames.PixelAt(frame, x - 6, y));
    }

    // The template's timeline: TouchIndicator's storyboard lasts 0.1 s; when it completes, the transition to
    // NoIndicator sets the ScrollBars' mode to None at 0.05 s. So one scroll's indicator goes 150 ms after it (the phone:
    // about 180 ms for a ChangeView from code). It fades in at the recording's rate until then (a frame behind), and
    // fades out from there at the fade out's rate, as the phone's does from 55 to 65%.
    [AvaloniaFact]
    public void One_Scrolls_Indicator_Fades_In_Then_Out_From_Where_It_Got_To()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        stage.ScrollTo(100);
        Assert.Equal(0, Shown(stage.Vertical));
        clock.At(149);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
        var reached = Shown(stage.Vertical);
        Assert.Equal(149 / FadeInMs, reached, 16 / FadeInMs);

        clock.At(150);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));

        clock.At(150 + reached * FadeOutMs / 2);
        Assert.Equal(reached / 2, Shown(stage.Vertical), 32 / FadeOutMs);
        clock.At(150 + reached * FadeOutMs + 16);
        Assert.Equal(0, Shown(stage.Vertical));
        Assert.Equal(0, clock.Pending);
    }

    // The fades whole, frame by frame: in from 0 to 1 at the touch's 370 ms, and out from 1 to 0 at 340 ms, linear.
    [AvaloniaFact]
    public void The_Touch_Indicator_Fades_In_And_Out_At_The_Recordings_Rates()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        KeepScrolling(stage, clock, 0, 160);
        Assert.Equal(160 / FadeInMs, Shown(stage.Vertical), 16 / FadeInMs);
        KeepScrolling(stage, clock, 200, 400);
        Assert.Equal(1, Shown(stage.Vertical));

        // The state last began at 360, with a scroll in the transition after its 340 completion; the scroll at 400 was
        // inside it. So it completes at 460 and goes at 510.
        clock.At(510);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
        clock.At(510 + FadeOutMs / 2);
        Assert.Equal(0.5, Shown(stage.Vertical), 32 / FadeOutMs);
        clock.At(510 + FadeOutMs + 16);
        Assert.Equal(0, Shown(stage.Vertical));
    }

    // Showing the state it is in changes nothing; a scroll in the 50 ms of the transition shows it again. So a scroll
    // that moves every 40 ms (more often than every 50) keeps the indicator, and it goes within 150 ms of the last move.
    [AvaloniaFact]
    public void A_Scroll_That_Keeps_Moving_Keeps_The_Indicator()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        for (var t = 0; t <= 480; t += 40)
        {
            clock.At(t);
            stage.ScrollTo(10 + t);
            Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
            clock.At(t + 39);
            Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
        }

        // A move inside the state's 100 ms doesn't restart it, and one in the transition's 50 ms does: the state began
        // at 360 and completed at 460, so the move at 480 began it again; it completes at 580, and goes at 630.
        clock.At(629);
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
        clock.At(630);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
    }

    // A scroll inside the state's 100 ms doesn't start it again: scrolls at 0, 40 and 80 ms, and it goes at 150, not
    // 150 ms after the last.
    [AvaloniaFact]
    public void A_Scroll_Inside_The_State_Doesnt_Start_It_Again()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        foreach (var t in new[] { 0, 40, 80 })
        {
            clock.At(t);
            stage.ScrollTo(10 + t);
        }

        clock.At(149);
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
        clock.At(150);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
    }

    // A touch that doesn't scroll shows nothing: only the scroll a touch makes shows the indicator.
    [AvaloniaFact]
    public void A_Touch_That_Doesnt_Scroll_Shows_Nothing()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        var at = new Point(200, 300);
        var touch = stage.Window.TouchBegin(at);
        stage.Window.TouchMove(touch, at + new Point(1, 1));
        Dispatcher.UIThread.RunJobs();
        stage.Window.TouchEnd(touch, at + new Point(1, 1));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, stage.Viewer.Offset.Y);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        Assert.Equal(0, clock.Pending);
    }

    // Taken out of the window mid-scroll, it is at NoIndicator, with no timer left.
    [AvaloniaFact]
    public void Taken_Out_Mid_Scroll_It_Is_At_NoIndicator()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        stage.ScrollTo(100);
        var page = (Border)stage.Viewer.Parent!;

        page.Child = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        Assert.Equal(0, clock.Pending);
    }

    // A touch drag pans the content, and its scroll shows the touch's indicator.
    [AvaloniaFact]
    public void A_Touch_Drag_Shows_The_Touch_Indicator()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        var start = new Point(200, 600);
        var touch = stage.Window.TouchBegin(start);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        for (var step = 1; step <= 10; step++)
        {
            stage.Window.TouchMove(touch, start - new Point(0, 30 * step));
            Dispatcher.UIThread.RunJobs();
        }

        Assert.True(stage.Viewer.Offset.Y > 0, "the drag didn't scroll");
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
        stage.Window.TouchEnd(touch, start - new Point(0, 300));
    }

    // The indicator takes no pointer, so a touch at the very edge, on the indicator, pans the content under it.
    [AvaloniaFact]
    public void A_Drag_On_The_Indicator_Pans_The_Content()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        var start = new Point(448, 600);
        Assert.True(stage.InWindow(stage.Vertical).Contains(start));
        var touch = stage.Window.TouchBegin(start);
        for (var step = 1; step <= 10; step++)
        {
            stage.Window.TouchMove(touch, start - new Point(0, 30 * step));
            Dispatcher.UIThread.RunJobs();
        }
        stage.Window.TouchEnd(touch, start - new Point(0, 300));
        Dispatcher.UIThread.RunJobs();

        Assert.True(stage.Viewer.Offset.Y > 0, "a drag on the indicator didn't scroll");
    }

    // The presenter takes a press anywhere on it, between the content's parts too, so its gesture recogniser sees the
    // drag: in both themes.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_Drag_Between_The_Contents_Parts_Pans_It(bool scrollbarless)
    {
        using var clock = new TestClock();
        var viewer = new ScrollViewer();
        if (scrollbarless)
            viewer.Bind(StyledElement.ThemeProperty, viewer.GetResourceObservable("Wp81ScrollViewerScrollBarlessTheme"));
        var lines = new StackPanel { Spacing = 100 };
        for (var i = 0; i < 20; i++)
            lines.Children.Add(new Border { Height = 20, Background = Brushes.Gray });
        using var stage = new Stage(viewer: viewer, content: lines);

        var start = new Point(200, 360 + 60);
        Assert.DoesNotContain(lines.Children, line => stage.InWindow(line).Contains(start));
        var touch = stage.Window.TouchBegin(start);
        for (var step = 1; step <= 10; step++)
        {
            stage.Window.TouchMove(touch, start - new Point(0, 30 * step));
            Dispatcher.UIThread.RunJobs();
        }
        stage.Window.TouchEnd(touch, start - new Point(0, 300));
        Dispatcher.UIThread.RunJobs();

        Assert.True(viewer.Offset.Y > 0, "a drag between the lines didn't scroll");
    }

    // A move a control in the content handles doesn't reach the ScrollViewer, as in WinRT, whose ScrollViewer sees it in
    // its PointerMoved override only unhandled.
    [AvaloniaFact]
    public void A_Move_The_Content_Handles_Shows_Nothing()
    {
        using var clock = new TestClock();
        var handler = new Border { Height = 2000, Background = Brushes.Transparent };
        handler.PointerMoved += (_, e) => e.Handled = true;
        using var stage = new Stage(content: handler);

        stage.Window.MouseMove(new Point(200, 300));
        Assert.Equal(PhoneIndicatorMode.MouseIndicator, ModeOf(stage.Viewer));
        clock.At(150);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));

        stage.Window.MouseMove(new Point(210, 320));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
    }

    // A theme change mid-scroll goes cleanly. Turning the indicators off takes the state's value away inside the old
    // theme's unsetting, which can stop it (spike 6); IndicatorMode is registered after IsEnabled, so it doesn't, and
    // the new theme's template and values all come in.
    [AvaloniaFact]
    public void A_New_Theme_Mid_Scroll_Comes_In_Whole()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        stage.ScrollTo(100);
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Viewer));

        stage.Viewer.Bind(StyledElement.ThemeProperty, stage.Viewer.GetResourceObservable("Wp81ScrollViewerScrollBarlessTheme"));
        Dispatcher.UIThread.RunJobs();

        Assert.False(PhoneScrollIndicators.GetIsEnabled(stage.Viewer));
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        Assert.Equal(ScrollBarVisibility.Auto, stage.Viewer.VerticalScrollBarVisibility);
        Assert.Empty(stage.Viewer.GetVisualDescendants().OfType<ScrollBar>());
        Assert.Equal(0, clock.Pending);
    }

    // The pointer type picks the indicator, as WinRT's ScrollViewer picks it: a mouse moving over the ScrollViewer
    // shows the mouse's at once, and a scroll after it is the mouse's too; a touch press, then a scroll, is the
    // touch's again. The mouse's goes on the same timeline.
    [AvaloniaFact]
    public void The_Pointer_Type_Picks_The_Indicator()
    {
        using var clock = new TestClock();
        using var stage = new Stage();

        stage.Window.MouseMove(new Point(200, 300));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.MouseIndicator, ModeOf(stage.Viewer));
        Assert.Equal(PhoneIndicatorMode.MouseIndicator, ModeOf(stage.Vertical));
        Assert.Equal(1, Shown(stage.Vertical));

        clock.At(150);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
        clock.At(150 + FadeOutMs + 16);
        Assert.Equal(0, Shown(stage.Vertical));

        stage.ScrollTo(100);
        Assert.Equal(PhoneIndicatorMode.MouseIndicator, ModeOf(stage.Vertical));
        Assert.Equal(1, Shown(stage.Vertical));
        clock.At(700);

        var touch = stage.Window.TouchBegin(new Point(200, 300));
        stage.Window.TouchEnd(touch, new Point(200, 300));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
        stage.ScrollTo(200);
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
    }

    // A key going down on the ScrollViewer also ends the mouse's preference, before the key scrolls (WinRT's OnKeyDown):
    // PageDown, which Avalonia's ScrollViewer handles itself, scrolls with the touch's indicator.
    [AvaloniaFact]
    public void A_Key_Gives_The_Touch_Indicator_Back()
    {
        using var clock = new TestClock();
        var button = new Button { Content = "focus", Focusable = true };
        using var stage = new Stage(content: new StackPanel { Children = { button, new Border { Height = 2000 } } });

        stage.Window.MouseMove(new Point(200, 300));
        clock.At(200);
        stage.ScrollTo(100);
        Assert.Equal(PhoneIndicatorMode.MouseIndicator, ModeOf(stage.Vertical));
        clock.At(400);

        button.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Viewer.Offset.Y > 100, "PageDown didn't scroll");
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));
    }

    // WinRT shows no indicator for the offsets of a ScrollViewer still loading; nor does ours.
    [AvaloniaFact]
    public void An_Offset_Set_Before_It_Loads_Shows_Nothing()
    {
        using var clock = new TestClock();
        var viewer = new ScrollViewer();
        viewer.Offset = new Vector(0, 300);
        using var stage = new Stage(viewer: viewer);

        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Viewer));
        Assert.Equal(0, clock.Pending);
    }

    // Avalonia's AllowAutoHide, which WinRT lacks: off, the indicator stays shown.
    [AvaloniaFact]
    public void AllowAutoHide_Off_Keeps_The_Indicator()
    {
        var viewer = new ScrollViewer { AllowAutoHide = false };
        using var stage = new Stage(viewer: viewer);

        Assert.Equal(1, Shown(stage.Vertical));
        var (x, y) = ThumbPixel(stage);
        using var frame = stage.Capture();
        Assert.Equal(DarkIndicator, Frames.PixelAt(frame, x, y));
    }

    // The ScrollBar's Disabled state draws its root at 0.5.
    [AvaloniaFact]
    public void Disabled_Draws_The_Indicator_At_Half()
    {
        var viewer = new ScrollViewer { AllowAutoHide = false };
        using var stage = new Stage(viewer: viewer);
        var root = Part<Panel>(stage.Vertical, "Root");
        Assert.Equal(1, root.Opacity);

        viewer.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0.5, root.Opacity);
    }

    // Turned off, the ScrollViewer's state no longer follows the scroll; the ScrollBars' modes go with it.
    [AvaloniaFact]
    public void Turning_The_Indicators_Off_Leaves_No_Indicator()
    {
        using var clock = new TestClock();
        using var stage = new Stage();
        stage.ScrollTo(100);
        Assert.Equal(PhoneIndicatorMode.TouchIndicator, ModeOf(stage.Vertical));

        PhoneScrollIndicators.SetIsEnabled(stage.Viewer, false);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
        stage.ScrollTo(200);
        Assert.Equal(PhoneIndicatorMode.None, ModeOf(stage.Vertical));
        clock.At(FadeOutMs + 16);
        Assert.Equal(0, Shown(stage.Vertical));
        Assert.Equal(0, clock.Pending);
    }

    // The colours follow a change of variant.
    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        var viewer = new ScrollViewer { AllowAutoHide = false };
        using var stage = new Stage(viewer: viewer);
        var (x, y) = ThumbPixel(stage);
        using (var dark = stage.Capture())
            Assert.Equal(DarkIndicator, Frames.PixelAt(dark, x, y));

        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        using var light = stage.Capture();
        Assert.Equal(LightIndicator, Frames.PixelAt(light, x, y));
    }

    // A ScrollBar on its own, outside a ScrollViewer: the thin indicator, shown (MouseIndicator unless its mode is set:
    // a deliberate difference, the phone's would draw nothing), and its thumb takes the pointer, so it can be dragged.
    [AvaloniaFact]
    public void A_ScrollBar_On_Its_Own_Shows_And_Its_Thumb_Drags()
    {
        using var clock = new TestClock();
        var bar = new ScrollBar { Height = 300, Maximum = 100, ViewportSize = 100 };
        using var scene = new Scene(new Border { Child = bar, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
        var thumb = Part<Thumb>(bar, "VerticalPanningThumb");

        Assert.Null(bar.Theme);
        Assert.Equal(BarThickness, bar.Bounds.Width);
        Assert.Equal(PhoneIndicatorMode.MouseIndicator, ModeOf(bar));
        Assert.Equal(1, Shown(bar));
        Assert.True(bar.IsHitTestVisible);
        Assert.Equal(150, thumb.Bounds.Height, 0.5);
        Assert.Empty(bar.GetVisualDescendants().OfType<RepeatButton>());

        var from = thumb.TranslatePoint(new Point(1, 75), scene.Window)!.Value;
        scene.Window.MouseDown(from, MouseButton.Left);
        scene.Window.MouseMove(from + new Point(0, 75));
        scene.Window.MouseUp(from + new Point(0, 75), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(50, bar.Value, 0.5);

        PhoneScrollIndicators.SetIndicatorMode(bar, PhoneIndicatorMode.None);
        clock.At(FadeOutMs + 16);
        Assert.Equal(0, Shown(bar));
        PhoneScrollIndicators.SetIndicatorMode(bar, PhoneIndicatorMode.TouchIndicator);
        clock.At(FadeOutMs + FadeInMs + 32);
        Assert.Equal(1, Shown(bar));
    }

    // Against the captures (run 20261115-1921-dev11): the reference app's standalone ScrollBars draw their thumb in raw
    // pixels 1023 to 1028 across and 109 to 269 down (vertical), and 126 to 286 across and 34 to 39 down (horizontal):
    // ours the same but one pixel shorter, 160 for the phone's 161 (66.67 view px: WinRT rounds its thumb's length and
    // place apart, Avalonia's Track lays them out together), and the horizontal thumb one pixel lower, 35 to 40 (the 5.5
    // raw pixels that centre it in the 17-pixel bar rounded the other way). The colours are #737373 and #BFBFBF; disabled, #3A3A3A and
    // #DFDFDF, the root at half.
    [AvaloniaTheory]
    [InlineData("Dark", "vertical", false, 1023, 1028, 109, 268, 0x73)]
    [InlineData("Light", "vertical", false, 1023, 1028, 109, 268, 0xBF)]
    [InlineData("Dark", "vertical", true, 1023, 1028, 109, 268, 0x3A)]
    [InlineData("Light", "vertical", true, 1023, 1028, 109, 268, 0xDF)]
    [InlineData("Dark", "horizontal", false, 126, 285, 35, 40, 0x73)]
    [InlineData("Light", "horizontal", true, 126, 285, 35, 40, 0xDF)]
    public void The_ScrollBars_Thumb_Is_Where_The_Captures_Draw_It(string variant, string specimen, bool disabled, int left, int right, int top, int bottom, int grey)
    {
        using var stage = new BarStage(specimen, variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, disabled);
        using var frame = stage.Capture();

        Assert.Equal((left, right, top, bottom), Drawn(frame));
        var middle = Frames.PixelAt(frame, (left + right) / 2, (top + bottom) / 2);
        Assert.Equal(grey, middle.R, 1.0);
        Assert.Equal((middle.R, middle.R), (middle.G, middle.B));
    }

    // The ScrollViewer specimen as the dump has it: 412.0833 × 280 at 19.1667, 15.8333 on its stage (ours a raw pixel
    // narrower); scrolled to 200 of
    // 280, its thumb 280 × 280 / 560 = 140 long, 100 down (the code's Margin: 200 / 280 × (280 − 140)). The captures
    // can't show it (RenderTargetBitmap draws no ScrollViewer's indicator), so these are the dump's numbers.
    [AvaloniaFact]
    public void The_ScrollViewer_Specimen_Is_The_Dumps()
    {
        using var clock = new TestClock();
        using var stage = Lines(200);

        // 988 raw pixels wide, where the phone's is 989: the slot's rounding, as the ToggleSwitch's (ADR 0003).
        Near(new Rect(19.1667, 15.8333, 411.6667, 280), stage.InWindow(stage.Viewer));
        Assert.Equal(200, stage.Viewer.Offset.Y);
        var thumb = stage.InWindow(stage.VerticalThumb);
        var bar = stage.InWindow(stage.Vertical);
        Assert.Equal(140, thumb.Height, 0.5);
        Assert.Equal(100, thumb.Y - bar.Y, 0.5);
        Assert.Equal(bar.Right, stage.InWindow(stage.Viewer).Right, 0.001);
    }

    // The phone's no-indicator capture, a ScrollBar with no IndicatorMode set, draws nothing; ours shows (a ScrollBar on
    // its own is MouseIndicator unless set: a deliberate difference), and draws nothing once set to None, as the phone's.
    [AvaloniaFact]
    public void A_ScrollBar_Set_To_None_Draws_Nothing_As_The_Phones_Does()
    {
        using var clock = new TestClock();
        using var stage = new BarStage("no-indicator");
        using (var shown = stage.Capture())
            Assert.Equal((1023, 1028, 109, 268), Drawn(shown));

        PhoneScrollIndicators.SetIndicatorMode(stage.Bar, PhoneIndicatorMode.None);
        clock.At(FadeOutMs + 16);
        using var frame = stage.Capture();
        Assert.Equal((int.MaxValue, -1, int.MaxValue, -1), Drawn(frame));
    }

    // The phone's ScrollViewerScrollBarlessTemplate, for Hub and Pivot: the presenter in a Grid with the Background,
    // the Padding as its Margin, no ScrollBars, and it still scrolls.
    [AvaloniaFact]
    public void The_Scrollbarless_Theme_Has_No_Bars_And_Scrolls()
    {
        var viewer = new ScrollViewer { Padding = new Thickness(10), Background = Brushes.Blue };
        viewer.Bind(StyledElement.ThemeProperty, viewer.GetResourceObservable("Wp81ScrollViewerScrollBarlessTheme"));
        using var stage = new Stage(viewer: viewer);
        var presenter = Part<ScrollContentPresenter>(viewer, "PART_ContentPresenter");

        Assert.NotNull(viewer.Theme);
        Assert.Empty(viewer.GetVisualDescendants().OfType<ScrollBar>());
        Assert.IsType<Grid>(presenter.GetVisualParent());
        Assert.Equal(new Rect(10, 10, 430, 780), stage.InWindow(presenter));
        Assert.False(PhoneScrollIndicators.GetIsEnabled(viewer));

        stage.ScrollTo(300);
        Assert.Equal(300, viewer.Offset.Y);
    }

    // The phone's own Thumb style, for a Thumb an app places: ThumbBackgroundThemeBrush in ThumbBorderThemeBrush,
    // 0.8 wide; Pressed swaps in BackgroundPressed.
    [AvaloniaFact]
    public void A_Thumb_Has_The_Phones_Style_And_Pressed_State()
    {
        var thumb = new Thumb { Width = 100, Height = 100 };
        using var scene = new Scene(new Border { Child = thumb, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
        var background = Part<Border>(thumb, "Background");
        var pressed = Part<Border>(thumb, "BackgroundPressed");

        Assert.Equal(new Thickness(0.8), thumb.BorderThickness);
        Assert.Equal(Color.Parse("#CCFFFFFF"), ((ISolidColorBrush)background.Background!).Color);
        Assert.Equal(Color.Parse("#80FFFFFF"), ((ISolidColorBrush)background.BorderBrush!).Color);
        Assert.Equal(Color.Parse("#73FFFFFF"), ((ISolidColorBrush)pressed.Background!).Color);
        Assert.Equal(Color.Parse("#80FFFFFF"), ((ISolidColorBrush)pressed.BorderBrush!).Color);
        Assert.Equal((1.0, 0.0), (background.Opacity, pressed.Opacity));
        Assert.False(thumb.Focusable);

        scene.Window.MouseDown(new Point(50, 50), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal((0.0, 1.0), (background.Opacity, pressed.Opacity));
        scene.Window.MouseUp(new Point(50, 50), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal((1.0, 0.0), (background.Opacity, pressed.Opacity));
    }
}
