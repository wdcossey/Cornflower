using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
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
/// favorite (a checked toggle) and delete (disabled), and two secondary commands.
/// </summary>
internal sealed class AppBarPage : IDisposable
{
    private readonly Scene _scene;

    public AppBarPage(ThemeVariant? variant = null, Action<CommandBar>? configure = null)
    {
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
        _scene = new Scene(Page, variant);
    }

    public Panel Page { get; }
    public CommandBar Bar { get; }
    public CommandBarButton Add { get; }
    public CommandBarToggleButton Favorite { get; }
    public CommandBarButton Delete { get; }
    public CommandBarButton First { get; }
    public CommandBarButton Second { get; }
    public Window Window => _scene.Window;

    /// <summary>The bar's top, in view pixels.</summary>
    public double BarTop => Bar.TranslatePoint(default, Page)!.Value.Y;

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

    public void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public WriteableBitmap Capture() => _scene.Capture();

    public void Dispose() => _scene.Dispose();
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
        // No list, and no foot under the labels: the bar is the commands' row.
        Assert.Equal(page.Add.Bounds.Height, page.Bar.Bounds.Height, 0.001);
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

        Assert.Equal(0, page.BarTop, 0.001);
        Assert.Equal(800, page.Bar.Bounds.Height, 0.001);
        var viewer = Part<ScrollViewer>(page.Bar, "OverflowScrollViewer");
        Assert.True(viewer.Extent.Height > viewer.Viewport.Height);

        // The ellipsis is still on the screen, and closes the bar.
        page.Tap(AppBarPage.Raw(1003, 31));
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
    public void Pressed_Fills_The_Circle_And_Shows_The_Icon_In_The_Bars_Colour(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        var dark = Dark(variant);
        var ellipse = Part<Ellipse>(page.Add, "PART_Ellipse");
        var content = Part<ContentPresenter>(page.Add, "PART_Content");

        Assert.Equal(Colors.Transparent, ColourOf(ellipse.Fill));
        page.Window.MouseDown(page.CentreOf(page.Add), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(page.Add.IsPressed);
        Assert.Equal(Ring(dark), ColourOf(ellipse.Fill));
        Assert.Equal(Ring(dark), ColourOf(ellipse.Stroke));
        Assert.Equal(Bar(dark), ColourOf(content.Foreground));
        page.Window.MouseUp(page.CentreOf(page.Add), MouseButton.Left);
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void Checked_Fills_The_Circle_And_Checked_Pressed_Shows_The_Ring(string variant)
    {
        using var page = new AppBarPage(VariantOf(variant));
        var dark = Dark(variant);
        var ellipse = Part<Ellipse>(page.Favorite, "PART_Ellipse");
        var content = Part<ContentPresenter>(page.Favorite, "PART_Content");

        Assert.Equal(Ring(dark), ColourOf(ellipse.Fill));
        Assert.Equal(Bar(dark), ColourOf(content.Foreground));

        page.Window.MouseDown(page.CentreOf(page.Favorite), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Transparent, ColourOf(ellipse.Fill));
        Assert.Equal(Ring(dark), ColourOf(ellipse.Stroke));
        Assert.Equal(Ring(dark), ColourOf(content.Foreground));

        page.Window.MouseUp(page.CentreOf(page.Favorite), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(page.Favorite.IsChecked);
        Assert.Equal(Colors.Transparent, ColourOf(ellipse.Fill));
        Assert.Equal(Text(dark), Blend(ColourOf(content.Foreground), Bar(dark)));
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
        Assert.True(Part<TextBlock>(page.Add, "PART_TextLabel").IsVisible);
    }
}
