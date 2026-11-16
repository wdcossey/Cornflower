using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.CommandBarReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The reference app's page with an app bar: a page of its background, 450 × 800, with the bar at its foot: add,
/// favorite (a checked toggle) and delete (disabled), and two secondary commands. Its own <see cref="TestClock"/>, so
/// the bar's motion runs only when a test moves the clock; <see cref="Settle"/> moves it past the motion.
/// </summary>
internal sealed class AppBarPage : IDisposable
{
    private readonly Scene _scene;

    public AppBarPage(ThemeVariant? variant = null, Action<CommandBar>? configure = null)
    {
        Clock = new TestClock();
        Bar = new CommandBar();
        Add = new CommandBarButton { Label = "add", Icon = new LucideIcon { Kind = LucideIconKind.Plus } };
        Favorite = new CommandBarToggleButton { Label = "favorite", IsChecked = true, Icon = new LucideIcon { Kind = LucideIconKind.Star } };
        Delete = new CommandBarButton { Label = "delete", IsEnabled = false, Icon = new LucideIcon { Kind = LucideIconKind.Trash } };
        First = new CommandBarButton { Label = "first menu item" };
        Second = new CommandBarButton { Label = "second menu item" };
        Bar.PrimaryCommands.Add(Add);
        Bar.PrimaryCommands.Add(Favorite);
        Bar.PrimaryCommands.Add(Delete);
        Bar.SecondaryCommands.Add(First);
        Bar.SecondaryCommands.Add(Second);
        configure?.Invoke(Bar);
        Page = new Panel { Children = { Bar } };
        Page.Bind(Panel.BackgroundProperty, Page.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
        try
        {
            _scene = new Scene(Page, variant);
        }
        catch
        {
            Clock.Dispose();
            throw;
        }
    }

    public TestClock Clock { get; }
    public Panel Page { get; }
    public CommandBar Bar { get; }
    public CommandBarButton Add { get; }
    public CommandBarToggleButton Favorite { get; }
    public CommandBarButton Delete { get; }
    public CommandBarButton First { get; }
    public CommandBarButton Second { get; }
    public Window Window => _scene.Window;

    /// <summary>The bar's top, in view pixels: where its layout puts it.</summary>
    public double BarTop => Bar.TranslatePoint(default, Page)!.Value.Y;

    /// <summary>The bar's top as drawn, in view pixels: lower than <see cref="BarTop"/> while the bar moves.</summary>
    public double DrawnTop => Bar.GetVisualDescendants().OfType<Grid>().First(g => g.Name == "LayoutRoot").TranslatePoint(default, Page)!.Value.Y;

    /// <summary>A point given in raw pixels, in the window.</summary>
    public static Point Raw(double x, double y) => new(x / Scale, y / Scale);

    /// <summary>The centre of <paramref name="control"/>, in the window.</summary>
    public Point CentreOf(Visual control) => CentreOf(control, Window);

    public static Point CentreOf(Visual control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    public void Tap(Point at) => Tap(Window, at);

    /// <summary>A mouse press and release at <paramref name="at"/>, the jobs run after each.</summary>
    public static void Tap(Window window, Point at)
    {
        window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The jobs and the layout run, and the clock moved a second on, past any motion.</summary>
    public void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Clock.At(Clock.Now.TotalMilliseconds + 1000);
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The clock moved to <paramref name="milliseconds"/> after its start, the jobs and the layout run.</summary>
    public void At(double milliseconds)
    {
        Clock.At(milliseconds);
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
    }

    public WriteableBitmap Capture() => _scene.Capture();

    public void Dispose()
    {
        _scene.Dispose();
        Clock.Dispose();
    }
}

/// <summary>
/// The Windows Phone 8.1 page's app bar: Avalonia's <c>CommandBar</c> and its commands, themed as the shell draws a
/// page's <c>BottomAppBar</c>, against the shell's screenshots (<see cref="CommandBarReadings"/>).
/// </summary>
public class CommandBarTests
{
    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == control);

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static bool Dark(string variant) => variant == "Dark";

    private static ThemeVariant VariantOf(string variant) => Dark(variant) ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>Each channel within <paramref name="tolerance"/> of <paramref name="expected"/>, or a line saying how far.</summary>
    private static void Expect(List<string> misses, WriteableBitmap frame, int x, int y, Color expected, int tolerance, string what)
    {
        var got = Frames.PixelAt(frame, x, y);
        if (Math.Abs(got.R - expected.R) > tolerance || Math.Abs(got.G - expected.G) > tolerance || Math.Abs(got.B - expected.B) > tolerance)
            misses.Add($"{what} ({x}, {y}): #{got.R:X2}{got.G:X2}{got.B:X2}, the phone's #{expected.R:X2}{expected.G:X2}{expected.B:X2}");
    }

    [AvaloniaFact]
    public void Every_Type_Has_The_Phones_Template()
    {
        var separator = new CommandBarSeparator();
        using var page = new AppBarPage(configure: bar => bar.PrimaryCommands.Add(separator));

        Assert.NotNull(Part<Grid>(page.Bar, "LayoutRoot"));
        Assert.NotNull(Part<Ellipse>(page.Add, "PART_Ellipse"));
        Assert.NotNull(Part<Ellipse>(page.Favorite, "PART_Ellipse"));
        // The phone has no style for AppBarSeparator, so it has no template and takes no room.
        Assert.Null(separator.Template);
        Assert.Empty(separator.GetVisualChildren());
        Assert.Equal(0, separator.Bounds.Width);
    }

    [AvaloniaFact]
    public void ClosedDisplayMode_Is_Compact_Unless_Set()
    {
        var bar = new CommandBar();
        Assert.Equal(AppBarClosedDisplayMode.Compact, CommandBarOptions.GetClosedDisplayMode(bar));
        CommandBarOptions.SetClosedDisplayMode(bar, AppBarClosedDisplayMode.Minimal);
        Assert.Equal(AppBarClosedDisplayMode.Minimal, bar.GetValue(CommandBarOptions.ClosedDisplayModeProperty));
        Assert.Equal(1, (int)AppBarClosedDisplayMode.Minimal);
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_Bar_Takes_The_Phones_Keys(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        var dark = Dark(variant);

        Assert.Equal(Bar(dark), ColourOf(page.Bar.Background));
        Assert.Equal(Text(dark), Blend(ColourOf(page.Bar.Foreground), Bar(dark)));
        Assert.Equal(Text(dark), Blend(ColourOf(page.Add.Foreground), Bar(dark)));
        Assert.Equal(Ring(dark), ColourOf(Part<Ellipse>(page.Add, "PART_Ellipse").Stroke));
        Assert.False(page.Bar.IsTabStop);
    }

    /// <summary><paramref name="ink"/> drawn over <paramref name="under"/>, as the screenshots read it.</summary>
    private static Color Blend(Color ink, Color under)
    {
        byte Mix(byte i, byte u) => (byte)Math.Round(u + (i - u) * ink.A / 255.0);
        return Color.FromRgb(Mix(ink.R, under.R), Mix(ink.G, under.G), Mix(ink.B, under.B));
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_Compact_Bar_Is_The_Shells(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        var dark = Dark(variant);

        Assert.Equal(CompactTop / Scale, page.BarTop, 0.001);
        Assert.Equal(CompactHeight, page.Bar.Bounds.Height, 0.001);

        using var frame = page.Capture();
        var misses = new List<string>();
        Expect(misses, frame, 5, CompactTop - 1, Page(dark), 0, "the page above the bar");
        Expect(misses, frame, 5, CompactTop, Bar(dark), 0, "the bar's top row");
        Expect(misses, frame, 5, 1919, Bar(dark), 0, "the bar's foot");
        AssertCircles(misses, frame, CompactTop, dark);
        AssertDots(misses, frame, CompactTop, dark);
        Assert.True(misses.Count == 0, string.Join("\n", misses));

        // Closed, the labels don't show.
        Assert.All([page.Add, page.Delete], b => Assert.False(Part<TextBlock>(b, "PART_TextLabel").IsVisible));
        Assert.False(Part<TextBlock>(page.Favorite, "PART_TextLabel").IsVisible);
    }

    /// <summary>The three circles where the shell draws them: their rings, the ring of the disabled one dimmed, the checked one filled.</summary>
    private static void AssertCircles(List<string> misses, WriteableBitmap frame, int barTop, bool dark)
    {
        var centreY = barTop + CircleTopBelowBar + CircleRadius;
        for (var i = 0; i < CircleCentres.Length; i++)
        {
            var x = CircleCentres[i];
            var ring = i == 2 ? DisabledRing(dark) : Ring(dark);
            var inside = i == 1 ? Ring(dark) : Bar(dark);
            var tolerance = i == 2 ? 3 : 0;
            Expect(misses, frame, x - CircleRadius + 3, centreY, ring, tolerance, $"circle {i}'s ring, left");
            Expect(misses, frame, x + CircleRadius - 3, centreY, ring, tolerance, $"circle {i}'s ring, right");
            Expect(misses, frame, x, barTop + CircleTopBelowBar + 3, ring, tolerance, $"circle {i}'s ring, top");
            Expect(misses, frame, x, barTop + CircleTopBelowBar + 2 * CircleRadius - 4, ring, tolerance, $"circle {i}'s ring, bottom");
            Expect(misses, frame, x - CircleRadius + 11, centreY, inside, 0, $"circle {i}, inside its ring");
            // The ring's outer edges on its middle row, to the raw pixel: ink from the box's first column to its last.
            ExpectEdge(misses, frame, x - CircleRadius, x - CircleRadius - 1, centreY, Bar(dark), $"circle {i}'s left edge");
            ExpectEdge(misses, frame, x + CircleRadius - 1, x + CircleRadius, centreY, Bar(dark), $"circle {i}'s right edge");
            Expect(misses, frame, x - CircleRadius - 3, centreY, Bar(dark), 0, $"circle {i}, outside it");
            Expect(misses, frame, x + CircleRadius + 3, centreY, Bar(dark), 0, $"circle {i}, outside it");
        }
    }

    /// <summary>Ink (more than 12 levels off the bar) at <paramref name="inked"/> and the bar at <paramref name="clear"/>.</summary>
    private static void ExpectEdge(List<string> misses, WriteableBitmap frame, int inked, int clear, int y, Color bar, string what)
    {
        static int Off(Color a, Color b) => Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
        if (Off(Frames.PixelAt(frame, inked, y), bar) <= 12)
            misses.Add($"{what}: no ink at ({inked}, {y}), where the phone's is");
        if (Off(Frames.PixelAt(frame, clear, y), bar) > 12)
            misses.Add($"{what}: ink at ({clear}, {y}), where the phone has the bar");
    }

    /// <summary>The ellipsis's three dots, 25 raw px below the bar's top, in the words' colour.</summary>
    private static void AssertDots(List<string> misses, WriteableBitmap frame, int barTop, bool dark)
    {
        var middle = barTop + DotTopBelowBar + 6;
        foreach (var (left, right) in Dots)
        {
            Expect(misses, frame, (left + right) / 2, middle, Text(dark), 1, "a dot's centre");
            Expect(misses, frame, left - 1, middle, Bar(dark), 0, "left of a dot");
            Expect(misses, frame, right + 1, middle, Bar(dark), 0, "right of a dot");
            Expect(misses, frame, (left + right) / 2, barTop + DotTopBelowBar - 1, Bar(dark), 0, "above a dot");
        }
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_Minimal_Bar_Is_The_Shells(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant), bar => CommandBarOptions.SetClosedDisplayMode(bar, AppBarClosedDisplayMode.Minimal));
        var dark = Dark(variant);

        Assert.Equal(MinimalTop / Scale, page.BarTop, 0.001);
        Assert.Equal(MinimalHeight, page.Bar.Bounds.Height, 0.001);

        using var frame = page.Capture();
        var misses = new List<string>();
        Expect(misses, frame, 5, MinimalTop - 1, Page(dark), 0, "the page above the bar");
        Expect(misses, frame, 5, MinimalTop, Bar(dark), 0, "the bar's top row");
        foreach (var x in CircleCentres)
            Expect(misses, frame, x - CircleRadius + 3, 1900, Bar(dark), 0, "no circle");
        AssertDots(misses, frame, MinimalTop, dark);
        Assert.True(misses.Count == 0, string.Join("\n", misses));
        Assert.False(Part<ItemsControl>(page.Bar, "PART_PrimaryCommands").IsVisible);
    }

    [AvaloniaTheory]
    [InlineData("Dark", AppBarClosedDisplayMode.Compact)]
    [InlineData("Light", AppBarClosedDisplayMode.Compact)]
    [InlineData("Dark", AppBarClosedDisplayMode.Minimal)]
    public void The_Open_Bar_Is_The_Shells(string variant, AppBarClosedDisplayMode mode)
    {
        using var page = new AppBarPage(VariantOf(variant), bar => CommandBarOptions.SetClosedDisplayMode(bar, mode));
        var dark = Dark(variant);
        page.Bar.IsOpen = true;
        page.Settle();

        Assert.Equal(OpenTop / Scale, page.BarTop, 0.001);
        Assert.Equal(OpenHeight, page.Bar.Bounds.Height, 0.001);

        using var frame = page.Capture();
        var misses = new List<string>();
        Expect(misses, frame, 5, OpenTop - 1, Page(dark), 0, "the page above the bar");
        Expect(misses, frame, 5, OpenTop, Bar(dark), 0, "the bar's top row");
        AssertCircles(misses, frame, OpenTop, dark);
        AssertDots(misses, frame, OpenTop, dark);
        Assert.True(misses.Count == 0, string.Join("\n", misses));

        // The labels, their baseline where the shell's is; Selawik's, not Segoe WP's, so within half a raw pixel.
        foreach (var command in new Control[] { page.Add, page.Favorite, page.Delete })
        {
            var label = Part<TextBlock>(command, "PART_TextLabel");
            Assert.True(label.IsEffectivelyVisible);
            Assert.Equal(LabelBaselineBelowBar, BaselineBelowBar(label, page), 0.25);
        }

        // The secondary commands: a list of words, a line each, the words' baselines and left edge the shell's.
        Assert.True(Part<ItemsControl>(page.Bar, "PART_OverflowPresenter").IsEffectivelyVisible);
        var lines = new[] { page.First, page.Second };
        for (var i = 0; i < lines.Length; i++)
        {
            var text = Part<TextBlock>(lines[i], "PART_MenuText");
            Assert.True(text.IsEffectivelyVisible);
            Assert.False(Part<StackPanel>(lines[i], "PART_LayoutRoot").IsVisible);
            Assert.Equal(MenuPitch, lines[i].Bounds.Height, 0.001);
            Assert.Equal(page.Bar.Bounds.Width, lines[i].Bounds.Width, 0.001);
            Assert.Equal(MenuBaselinesBelowBar[i], BaselineBelowBar(text, page), 0.25);
        }
    }

    private static double BaselineBelowBar(TextBlock text, AppBarPage page) =>
        text.TranslatePoint(default, page.Bar)!.Value.Y + text.Padding.Top + text.TextLayout.TextLines[0].Baseline;

    [AvaloniaFact]
    public void The_Secondary_Commands_Ink_Starts_Where_The_Shells_Does()
    {
        using var page = new AppBarPage();
        page.Bar.IsOpen = true;
        page.Settle();
        using var frame = page.Capture();

        // The first line's ink: the shell's "f" from raw x 51; Selawik's within a raw pixel of it.
        var baseline = (int)Math.Round((page.BarTop + MenuBaselinesBelowBar[0]) * Scale);
        var first = Enumerable.Range(baseline - 60, 60)
            .Min(y => Enumerable.Range(0, 120).FirstOrDefault(x => Frames.PixelAt(frame, x, y).R > 0x80, 120));
        Assert.InRange(first, (int)(MenuInkLeft * Scale) - 1, (int)(MenuInkLeft * Scale) + 1);
    }

    [AvaloniaFact]
    public void The_Ellipsis_Opens_The_Bar_And_Closes_It()
    {
        using var page = new AppBarPage();
        var dots = AppBarPage.Raw(1003, CompactTop + 31);

        page.Tap(dots);
        page.Settle();
        Assert.True(page.Bar.IsOpen);
        Assert.Equal(OpenTop / Scale, page.BarTop, 0.001);

        page.Tap(AppBarPage.Raw(1003, OpenTop + 31));
        page.Settle();
        Assert.False(page.Bar.IsOpen);
        Assert.Equal(CompactTop / Scale, page.BarTop, 0.001);
        // Closed again, the labels go again.
        Assert.False(Part<TextBlock>(page.Add, "PART_TextLabel").IsVisible);
        Assert.False(Part<ScrollViewer>(page.Bar, "OverflowScrollViewer").IsVisible);
    }

    [AvaloniaFact]
    public void The_Ellipsis_Shows_Without_Secondary_Commands()
    {
        using var page = new AppBarPage(configure: bar => bar.SecondaryCommands.Clear());
        var button = Part<Button>(page.Bar, "PART_OverflowButton");
        Assert.True(page.Bar.IsOverflowButtonVisible);
        Assert.True(button.IsEffectivelyVisible);

        page.Tap(page.CentreOf(button));
        page.Settle();
        Assert.True(page.Bar.IsOpen);
        Assert.True(Part<TextBlock>(page.Add, "PART_TextLabel").IsEffectivelyVisible);
        // No list: the shell's bar with none, the commands' row and 18.3 under the labels.
        Assert.False(Part<ScrollViewer>(page.Bar, "OverflowScrollViewer").IsVisible);
        Assert.Equal(OpenHeights[0], page.Bar.Bounds.Height, 0.001);
    }

    [AvaloniaFact]
    public void The_Closed_Heights_Are_Keys_A_Page_Can_Read()
    {
        using var page = new AppBarPage();
        Assert.Equal(CompactHeight, (double)page.Window.FindResource("Wp81CommandBarCompactHeight")!, 0.0001);
        Assert.Equal(MinimalHeight, (double)page.Window.FindResource("Wp81CommandBarMinimalHeight")!, 0.0001);
    }

    [AvaloniaFact]
    public void A_Long_List_Scrolls_Inside_The_Page()
    {
        using var page = new AppBarPage(configure: bar =>
        {
            for (var i = 0; i < 12; i++)
                bar.SecondaryCommands.Add(new CommandBarButton { Label = $"item {i}" });
        });
        page.Bar.IsOpen = true;
        page.Settle();

        // The shell's bar stops at 418.75; ours scrolls what doesn't fit (the shell's isn't read).
        Assert.Equal(800 - OpenHeights[4], page.BarTop, 0.001);
        Assert.Equal(OpenHeights[4], page.Bar.Bounds.Height, 0.001);
        var viewer = Part<ScrollViewer>(page.Bar, "OverflowScrollViewer");
        Assert.True(viewer.Extent.Height > viewer.Viewport.Height);

        // The ellipsis closes the bar.
        page.Tap(AppBarPage.Raw(1003, 1920 - OpenHeights[4] * Scale + 31));
        page.Settle();
        Assert.False(page.Bar.IsOpen);
    }

    [AvaloniaFact]
    public void A_Press_Outside_The_Open_Bar_Closes_It()
    {
        using var page = new AppBarPage();
        var pagePresses = 0;
        page.Page.PointerPressed += (_, _) => pagePresses++;
        page.Bar.IsOpen = true;
        page.Settle();

        page.Tap(new Point(225, 200));
        page.Settle();

        Assert.False(page.Bar.IsOpen);
        Assert.Equal(0, pagePresses);
        Assert.Equal(CompactTop / Scale, page.BarTop, 0.001);
    }

    [AvaloniaFact]
    public void A_Sticky_Bar_Stays_Open()
    {
        using var page = new AppBarPage(configure: bar => bar.IsSticky = true);
        page.Bar.IsOpen = true;
        page.Settle();

        page.Tap(new Point(225, 200));
        page.Settle();

        Assert.True(page.Bar.IsOpen);
    }

    [AvaloniaFact]
    public void Invoking_A_Command_Closes_The_Open_Bar()
    {
        using var page = new AppBarPage();
        var clicks = 0;
        page.Second.Click += (_, _) => clicks++;
        var adds = 0;
        page.Add.Click += (_, _) => adds++;

        page.Bar.IsOpen = true;
        page.Settle();
        page.Tap(page.CentreOf(page.Second));
        Assert.Equal(1, clicks);
        Assert.False(page.Bar.IsOpen);

        page.Bar.IsOpen = true;
        page.Settle();
        page.Tap(page.CentreOf(page.Add));
        Assert.Equal(1, adds);
        Assert.False(page.Bar.IsOpen);

        page.Bar.IsOpen = true;
        page.Settle();
        page.Tap(page.CentreOf(page.Favorite));
        Assert.False(page.Favorite.IsChecked);
        Assert.False(page.Bar.IsOpen);
    }

    [AvaloniaFact]
    public void A_Sticky_Bar_Stays_Open_After_A_Command()
    {
        using var page = new AppBarPage(configure: bar => bar.IsSticky = true);
        var clicks = 0;
        page.Second.Click += (_, _) => clicks++;
        page.Bar.IsOpen = true;
        page.Settle();

        page.Tap(page.CentreOf(page.Second));

        Assert.Equal(1, clicks);
        Assert.True(page.Bar.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void Pressed_Fills_The_Circle_With_The_Accent_And_Sinks_It(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        var dark = Dark(variant);
        var ellipse = Part<Ellipse>(page.Add, "PART_Ellipse");
        var content = Part<ContentPresenter>(page.Add, "PART_Content");
        var circle = Part<Panel>(page.Add, "PART_ContentRoot");
        var label = Part<TextBlock>(page.Add, "PART_TextLabel");
        var labelColour = ColourOf(label.Foreground);

        Assert.Equal(Colors.Transparent, ColourOf(ellipse.Fill));
        page.Window.MouseDown(page.CentreOf(page.Add), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        // The shell's press (the recording): the circle, ring and all, the accent; the icon white, in both themes.
        Assert.True(page.Add.IsPressed);
        Assert.Equal(Accent, ColourOf(ellipse.Fill));
        Assert.Equal(Accent, ColourOf(ellipse.Stroke));
        Assert.Equal(Colors.White, ColourOf(content.Foreground));
        Assert.Equal(labelColour, ColourOf(label.Foreground));
        // It sinks: the circle drawn smaller, and the circle and the icon higher. A command on the bar doesn't tilt.
        Assert.Equal(PressedCircleScale, ellipse.RenderTransform!.Value.M11, 0.0001);
        Assert.Equal(PressedCircleScale, ellipse.RenderTransform!.Value.M22, 0.0001);
        Assert.Equal(-PressedRise / Scale, circle.RenderTransform!.Value.M32, 0.0001);
        Assert.Null(content.RenderTransform);
        Assert.Equal(default, Cornflower.Controls.Tilt.Current(page.Add));

        // In the frame, the disc's ink where the recording's is, within a recording pixel (2 raw px) each way.
        using var frame = page.Capture();
        var x = CircleCentres[0];
        var centreY = CompactTop + CircleTopBelowBar + CircleRadius - PressedRise;
        bool Ink(int px, int py)
        {
            var c = Frames.PixelAt(frame, px, py);
            var bar = Bar(dark);
            return Math.Max(Math.Abs(c.R - bar.R), Math.Max(Math.Abs(c.G - bar.G), Math.Abs(c.B - bar.B))) > 12;
        }

        var misses = new List<string>();
        // Its bounding box, as the recording's was read.
        var rows = Enumerable.Range(CompactTop, 151).Where(y => Enumerable.Range(x - 70, 140).Any(c => Ink(c, y))).ToList();
        var columns = Enumerable.Range(x - 70, 140).Where(c => Enumerable.Range(CompactTop, 151).Any(y => Ink(c, y))).ToList();
        if (Math.Abs(rows.First() - (CompactTop + PressedDiscTopBelowBar)) > 2 || Math.Abs(rows.Last() - (CompactTop + PressedDiscBottomBelowBar)) > 2)
            misses.Add($"the disc's rows: {rows.First()}..{rows.Last()}, the recording's {CompactTop + PressedDiscTopBelowBar}..{CompactTop + PressedDiscBottomBelowBar}");
        if (Math.Abs(columns.First() - PressedDiscColumns.Left) > 2 || Math.Abs(columns.Last() - PressedDiscColumns.Right) > 2)
            misses.Add($"the disc's columns: {columns.First()}..{columns.Last()}, the recording's {PressedDiscColumns.Left}..{PressedDiscColumns.Right}");
        Expect(misses, frame, x - 32, centreY, Accent, 0, "the disc, beside the icon");
        Expect(misses, frame, x + 32, centreY, Accent, 0, "the disc, beside the icon");
        Assert.True(misses.Count == 0, string.Join("\n", misses));
        page.Window.MouseUp(page.CentreOf(page.Add), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        // Let go, it is back where it was.
        Assert.Equal(Colors.Transparent, ColourOf(ellipse.Fill));
        Assert.Null(ellipse.RenderTransform);
        Assert.Null(circle.RenderTransform);
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void Checked_Fills_The_Circle_And_Pressed_Shows_The_Accent(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        var dark = Dark(variant);
        var ellipse = Part<Ellipse>(page.Favorite, "PART_Ellipse");
        var content = Part<ContentPresenter>(page.Favorite, "PART_Content");

        Assert.Equal(Ring(dark), ColourOf(ellipse.Fill));
        Assert.Equal(Bar(dark), ColourOf(content.Foreground));

        // Pressed, checked or not, the shell's press.
        foreach (var check in new[] { false, true })
        {
            page.Window.MouseDown(page.CentreOf(page.Favorite), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Accent, ColourOf(ellipse.Fill));
            Assert.Equal(Accent, ColourOf(ellipse.Stroke));
            Assert.Equal(Colors.White, ColourOf(content.Foreground));
            Assert.Equal(PressedCircleScale, ellipse.RenderTransform!.Value.M11, 0.0001);

            page.Window.MouseUp(page.CentreOf(page.Favorite), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(check, page.Favorite.IsChecked);
            Assert.Equal(check ? Ring(dark) : Colors.Transparent, ColourOf(ellipse.Fill));
            if (!check)
                Assert.Equal(Text(dark), Blend(ColourOf(content.Foreground), Bar(dark)));
        }
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void A_Pressed_Menu_Line_Tilts_And_Keeps_Its_Colours(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        page.Bar.IsOpen = true;
        page.Settle();
        var text = Part<TextBlock>(page.First, "PART_MenuText");
        var colour = ColourOf(text.Foreground);
        var at = text.TranslatePoint(new Point(40, text.Bounds.Height / 2), page.Window)!.Value;

        page.Window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(page.First.IsPressed);
        Assert.NotEqual(default, Cornflower.Controls.Tilt.Current(page.First));
        Assert.Same(Part<Panel>(page.First, "PART_Ink"), Cornflower.Controls.Tilt.TargetOf(page.First));
        Assert.Equal(colour, ColourOf(text.Foreground));
        Assert.Equal(Colors.Transparent, ColourOf(Part<Panel>(page.First, "PART_Root").Background));
        // A command on the bar doesn't tilt.
        Assert.False(Cornflower.Controls.Tilt.GetIsEnabled(page.Add));
        page.Window.MouseUp(new Point(at.X, 10), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Disabled_Dims_The_Whole_Command_As_One_Layer()
    {
        using var page = new AppBarPage(configure: bar => ((CommandBarToggleButton)bar.PrimaryCommands[1]).IsEnabled = false);
        foreach (var command in new Control[] { page.Delete, page.Favorite })
        {
            var ink = Part<Panel>(command, "PART_Ink");
            Assert.Equal(DisabledOpacity, ink.Opacity, 0.0001);
            Assert.True(RenderOptions.GetRequiresFullOpacityHandling(Part<Panel>(command, "PART_Root")));
        }

        Assert.Equal(1, Part<Panel>(page.Add, "PART_Ink").Opacity);
    }

    [AvaloniaFact]
    public void The_Labels_Follow_DefaultLabelPosition_When_An_App_Sets_It()
    {
        using var page = new AppBarPage(configure: bar => bar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom);
        Assert.False(page.Add.IsCompact);
        var label = Part<TextBlock>(page.Add, "PART_TextLabel");
        Assert.True(label.IsVisible);
        // Closed, the bar is as tall as its commands, the labels inside it.
        Assert.Equal(page.Add.Bounds.Height, page.Bar.Bounds.Height, 0.001);
        Assert.True(label.TranslatePoint(new Point(0, label.Bounds.Height), page.Bar)!.Value.Y <= page.Bar.Bounds.Height);
    }

    [AvaloniaFact]
    public void Dynamic_Overflow_Keeps_The_Row_Clear_Of_The_Ellipsis()
    {
        var fifth = new CommandBarButton { Label = "fifth" };
        using var page = new AppBarPage(configure: bar =>
        {
            bar.IsDynamicOverflowEnabled = true;
            bar.PrimaryCommands.Add(new CommandBarButton { Label = "fourth" });
            bar.PrimaryCommands.Add(fifth);
        });
        page.Settle();

        Assert.Equal(4, page.Bar.VisiblePrimaryCommands.Count);
        Assert.True(fifth.IsInOverflow);
        var row = Part<ItemsControl>(page.Bar, "PART_PrimaryCommands");
        var ellipsis = Part<Button>(page.Bar, "PART_OverflowButton");
        Assert.True(row.TranslatePoint(new Point(row.Bounds.Width, 0), page.Bar)!.Value.X <= ellipsis.TranslatePoint(default, page.Bar)!.Value.X);
    }

    [AvaloniaFact]
    public void A_Command_With_A_Flyout_Or_On_Another_Bar_Leaves_The_Bar_Open()
    {
        var withFlyout = new CommandBarButton { Label = "more", Flyout = new Flyout { Content = new TextBlock { Text = "flyout" } } };
        var inner = new CommandBar();
        var innerCommand = new CommandBarButton { Label = "inner" };
        inner.PrimaryCommands.Add(innerCommand);
        using var page = new AppBarPage(configure: bar =>
        {
            bar.SecondaryCommands.Add(withFlyout);
            bar.Content = inner;
        });
        page.Bar.IsOpen = true;
        page.Settle();

        withFlyout.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        innerCommand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(page.Bar.IsOpen);
    }

    [AvaloniaFact]
    public void A_Commands_Bound_Command_Runs_With_The_Bar_Still_Open()
    {
        bool? openWhenRun = null;
        using var page = new AppBarPage();
        page.Second.Command = new RunCommand(() => openWhenRun = page.Bar.IsOpen);
        page.Bar.IsOpen = true;
        page.Settle();

        page.Tap(page.CentreOf(page.Second));

        Assert.True(openWhenRun);
        Assert.False(page.Bar.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 4)]
    [InlineData(10, 4)]
    public void The_Open_Bars_Height_Steps_With_Its_Lines(int lines, int step)
    {
        using var page = new AppBarPage(configure: bar =>
        {
            bar.SecondaryCommands.Clear();
            for (var i = 0; i < lines; i++)
                bar.SecondaryCommands.Add(new CommandBarButton { Label = $"menu item {i + 1}" });
        });
        page.Bar.IsOpen = true;
        page.Settle();

        // The shell's height for the count, the top where its screenshot draws it.
        Assert.Equal(OpenHeights[step], page.Bar.Bounds.Height, 0.001);
        using var frame = page.Capture();
        var top = (int)Math.Round(1920 - OpenHeights[step] * Scale);
        var misses = new List<string>();
        Expect(misses, frame, 5, top - 1, Page(true), 0, "the page above the bar");
        Expect(misses, frame, 5, top, Bar(true), 0, "the bar's top row");
        AssertCircles(misses, frame, top, true);
        Assert.True(misses.Count == 0, string.Join("\n", misses));

        // Everything sits where it does with two, from the bar's top: the labels, and each line 142 raw px on.
        Assert.Equal(LabelBaselineBelowBar, BaselineBelowBar(Part<TextBlock>(page.Add, "PART_TextLabel"), page), 0.25);
        foreach (var (line, i) in page.Bar.SecondaryCommands.OfType<CommandBarButton>().Select((c, i) => (c, i)).Take(5))
            Assert.Equal(MenuBaselinesBelowBar[0] + i * MenuPitch, BaselineBelowBar(Part<TextBlock>(line, "PART_MenuText"), page), 0.25);
    }

    [AvaloniaFact]
    public void Separators_And_Hidden_Commands_Are_Not_Lines()
    {
        var hidden = new CommandBarButton { Label = "hidden", IsVisible = false };
        using var page = new AppBarPage(configure: bar =>
        {
            bar.SecondaryCommands.Add(new CommandBarSeparator());
            bar.SecondaryCommands.Add(hidden);
        });
        page.Bar.IsOpen = true;
        page.Settle();
        Assert.Equal(2, Cornflower.Themes.Wp81.PhoneAppBar.GetMenuLines(page.Bar));
        Assert.Equal(OpenHeights[2], page.Bar.Bounds.Height, 0.001);

        // A line shown, or taken away, moves the bar to its new step.
        hidden.IsVisible = true;
        page.Settle();
        Assert.Equal(OpenHeights[3], page.Bar.Bounds.Height, 0.001);
        page.Bar.SecondaryCommands.Add(new CommandBarButton { Label = "fourth" });
        page.Settle();
        Assert.Equal(OpenHeights[4], page.Bar.Bounds.Height, 0.001);
        page.Bar.SecondaryCommands.Clear();
        page.Settle();
        Assert.Equal(OpenHeights[0], page.Bar.Bounds.Height, 0.001);
    }

    /// <summary>The bar's drawn top each frame from <paramref name="from"/> ms until it rests, as (ms after it was set, top).</summary>
    private static List<(double Ms, double Top)> Frames16(AppBarPage page, double from)
    {
        var tops = new List<(double, double)> { (0, page.DrawnTop) };
        for (var ms = from + 16; ms <= from + 1000; ms += 16)
        {
            page.At(ms);
            tops.Add((ms - from, page.DrawnTop));
        }

        return tops;
    }

    /// <summary>
    /// As tools/motion/appbar.py reads a motion: the time constant of a straight line through the log of the fraction
    /// to go, from 90% to 1.5% to go; when it is 90% there; when it is at rest (within half a recording px, 0.4167 view
    /// px, of its end).
    /// </summary>
    private static (double TimeConstant, double Ninety, double AtRest) Read(List<(double Ms, double Top)> tops)
    {
        var start = tops[0].Top;
        var end = tops[^1].Top;
        var toGo = tops.Select(t => (t.Ms, Fraction: (t.Top - end) / (start - end))).ToList();
        var fit = toGo.Where(t => t.Fraction is < 0.9 and > 0.015).ToList();
        var meanX = fit.Average(t => t.Ms);
        var meanY = fit.Average(t => Math.Log(t.Fraction));
        var slope = fit.Sum(t => (t.Ms - meanX) * (Math.Log(t.Fraction) - meanY)) / fit.Sum(t => Math.Pow(t.Ms - meanX, 2));
        var ninety = toGo.First(t => t.Fraction <= 0.1).Ms;
        var atRest = tops.First(t => Math.Abs(t.Top - end) <= 0.5 / 1.2).Ms;
        return (-1 / slope, ninety, atRest);
    }

    [AvaloniaTheory]
    [InlineData(AppBarClosedDisplayMode.Compact)]
    [InlineData(AppBarClosedDisplayMode.Minimal)]
    public void The_Bar_Opens_And_Closes_As_The_Shells_Does(AppBarClosedDisplayMode mode)
    {
        using var page = new AppBarPage(configure: bar => CommandBarOptions.SetClosedDisplayMode(bar, mode));
        var closedTop = (mode == AppBarClosedDisplayMode.Compact ? CompactTop : MinimalTop) / Scale;
        Assert.Equal(closedTop, page.DrawnTop, 0.001);

        // Opening: the layout is open at once and the drawing rises into it.
        page.Bar.IsOpen = true;
        page.Window.UpdateLayout();
        Assert.Equal(OpenTop / Scale, page.BarTop, 0.001);
        Assert.Equal(closedTop, page.DrawnTop, 0.001);
        var opening = Frames16(page, 0);
        Assert.Equal(OpenTop / Scale, opening[^1].Top, 0.001);
        Assert.True(opening.Zip(opening.Skip(1)).All(p => p.Second.Top <= p.First.Top), "the top only rises");
        var (tau, ninety, atRest) = Read(opening);
        Assert.InRange(tau, OpenTimeConstant.Low, OpenTimeConstant.High);
        Assert.InRange(ninety, OpenNinety.Low, OpenNinety.High);
        Assert.InRange(atRest, OpenAtRest.Low, OpenAtRest.High);

        // Closing: the layout is closed at once, the bar keeps its open look, and the drawing falls to it.
        page.Bar.IsOpen = false;
        page.Window.UpdateLayout();
        Assert.Equal(closedTop, page.BarTop, 0.001);
        Assert.Equal(OpenTop / Scale, page.DrawnTop, 0.001);
        var closing = Frames16(page, 1000);
        Assert.Equal(closedTop, closing[^1].Top, 0.001);
        Assert.True(closing.Zip(closing.Skip(1)).All(p => p.Second.Top >= p.First.Top), "the top only falls");
        (tau, ninety, atRest) = Read(closing);
        Assert.InRange(tau, CloseTimeConstant.Low, CloseTimeConstant.High);
        Assert.InRange(ninety, CloseNinety.Low, CloseNinety.High);
        Assert.InRange(atRest, CloseAtRest.Low, CloseAtRest.High);
    }

    [AvaloniaFact]
    public void Closing_Keeps_The_Open_Look_Until_The_Bar_Is_Down()
    {
        using var page = new AppBarPage(configure: bar => CommandBarOptions.SetClosedDisplayMode(bar, AppBarClosedDisplayMode.Minimal));
        page.Bar.IsOpen = true;
        page.Settle();
        var label = Part<TextBlock>(page.Add, "PART_TextLabel");
        var list = Part<ScrollViewer>(page.Bar, "OverflowScrollViewer");
        var start = page.Clock.Now.TotalMilliseconds;

        page.Bar.IsOpen = false;
        page.Window.UpdateLayout();
        // The minimal bar's height at once, for the page; the labels, the list and the circles while it falls.
        Assert.Equal(MinimalHeight, page.Bar.Bounds.Height, 0.001);
        page.At(start + 150);
        Assert.True(label.IsEffectivelyVisible);
        Assert.True(list.IsEffectivelyVisible);
        Assert.True(Part<ItemsControl>(page.Bar, "PART_PrimaryCommands").IsEffectivelyVisible);
        Assert.True(page.DrawnTop < MinimalTop / Scale);

        page.At(start + 220);
        Assert.False(label.IsVisible);
        Assert.False(list.IsVisible);
        Assert.False(Part<ItemsControl>(page.Bar, "PART_PrimaryCommands").IsVisible);
        Assert.Equal(MinimalTop / Scale, page.DrawnTop, 0.001);
        Assert.Equal(default, Part<Grid>(page.Bar, "LayoutRoot").Margin);
    }

    [AvaloniaFact]
    public void While_It_Moves_Nothing_Is_Drawn_Below_The_Bar()
    {
        // A bar 100 above the window's foot: as it starts to open, its labels are below the height shown, and cut.
        using var page = new AppBarPage(configure: bar => bar.Margin = new Thickness(0, 0, 0, 100));
        page.Bar.IsOpen = true;
        page.Window.UpdateLayout();
        using var frame = page.Capture();
        var foot = (int)Math.Round((800 - 100) * Scale);
        var inked = Enumerable.Range(foot + 1, 120).SelectMany(y => Enumerable.Range(0, 1080).Where(x => Frames.PixelAt(frame, x, y) != Page(true)).Select(x => (x, y))).ToList();
        Assert.True(inked.Count == 0, $"{inked.Count} pixels drawn below the bar, the first at {inked.FirstOrDefault()}");
        page.Settle();
    }

    [AvaloniaFact]
    public void A_Motion_Turned_Back_Goes_On_From_Where_The_Bar_Is()
    {
        using var page = new AppBarPage();
        page.Bar.IsOpen = true;
        page.Window.UpdateLayout();
        page.At(96);
        var top = page.DrawnTop;
        Assert.InRange(top, OpenTop / Scale + 1, CompactTop / Scale - 1);

        page.Bar.IsOpen = false;
        page.Window.UpdateLayout();
        Assert.Equal(top, page.DrawnTop, 0.01);
        page.At(112);
        Assert.InRange(page.DrawnTop, top, CompactTop / Scale);

        page.Bar.IsOpen = true;
        page.Window.UpdateLayout();
        var turned = page.DrawnTop;
        page.At(128);
        Assert.InRange(page.DrawnTop, OpenTop / Scale, turned);
        page.Settle();
        Assert.Equal(OpenTop / Scale, page.DrawnTop, 0.001);
    }

    [AvaloniaFact]
    public void Out_Of_A_Window_Or_With_The_Apps_Height_The_Bar_Goes_Straight_There()
    {
        // With the app's Height, the heights are the app's: nothing moves.
        using var page = new AppBarPage(configure: b => b.Height = 100);
        page.Bar.IsOpen = true;
        page.Window.UpdateLayout();
        Assert.True(Cornflower.Themes.Wp81.PhoneAppBar.GetShowsOpen(page.Bar));
        Assert.Equal(page.BarTop, page.DrawnTop, 0.001);
        page.Bar.IsOpen = false;
        Assert.False(Cornflower.Themes.Wp81.PhoneAppBar.GetShowsOpen(page.Bar));
        page.Window.UpdateLayout();
        Assert.Equal(page.BarTop, page.DrawnTop, 0.001);

        // Taken out of the window while it moves, it is where it was going.
        page.Bar.ClearValue(Layoutable.HeightProperty);
        page.Settle();
        page.Bar.IsOpen = true;
        page.Window.UpdateLayout();
        page.At(page.Clock.Now.TotalMilliseconds + 32);
        var root = Part<Grid>(page.Bar, "LayoutRoot");
        Assert.NotEqual(default, root.Margin);
        page.Page.Children.Remove(page.Bar);
        Assert.Equal(default, root.Margin);
        Assert.Equal(0, page.Clock.Pending);
    }

    [AvaloniaFact]
    public void A_Tap_On_The_Minimal_Bar_Away_From_The_Ellipsis_Opens_It()
    {
        using var page = new AppBarPage(configure: bar => CommandBarOptions.SetClosedDisplayMode(bar, AppBarClosedDisplayMode.Minimal));
        var opened = 0;
        page.Bar.Opened += (_, _) => opened++;

        page.Tap(AppBarPage.Raw(200, MinimalTop + 31));
        Assert.True(page.Bar.IsOpen);
        Assert.Equal(1, opened);
        page.Settle();

        // Open, a tap on the bar away from its commands leaves it open; the ellipsis closes it.
        page.Tap(AppBarPage.Raw(200, OpenTop + 10));
        Assert.True(page.Bar.IsOpen);
        page.Tap(AppBarPage.Raw(1003, OpenTop + 31));
        Assert.False(page.Bar.IsOpen);
        page.Settle();

        // The ellipsis opens it once, not twice over.
        page.Tap(AppBarPage.Raw(1003, MinimalTop + 31));
        Assert.True(page.Bar.IsOpen);
        Assert.Equal(2, opened);
    }

    private sealed class RunCommand(Action run) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
