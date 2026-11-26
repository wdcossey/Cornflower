using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain PipsPager (ADR 0007, #241): flat square marks in fingertip squares, the selected one in the accent, the
/// arrows in the plain glyph part with Lucide's chevrons, no motion of its own, and a selection that never scrolls the
/// page round it (#220). Simple's are 12 circles in ThemeControlLowBrush with 24 Buttons in Simple's Button theme, so
/// these fail without the theme.
/// </summary>
public class PipsPagerTests
{
    public static TheoryData<string> Variants => new("dark", "light");

    private static ThemeVariant VariantOf(string name) => name == "light" ? ThemeVariant.Light : ThemeVariant.Dark;

    private const double Pixel = 1 / Scene.ReferenceScaling;   // layout rounding

    private static void Near(double expected, double actual) => Assert.InRange(actual, expected - Pixel, expected + Pixel);

    private static ListBox List(PipsPager pager) => Part<ListBox>(pager, "PART_PipsPagerList");

    private static List<ListBoxItem> Pips(PipsPager pager) =>
        List(pager).GetRealizedContainers().Cast<ListBoxItem>().OrderBy(item => List(pager).IndexFromContainer(item)).ToList();

    private static Rectangle Mark(ListBoxItem pip) => pip.GetVisualDescendants().OfType<Rectangle>().Single(r => r.Name == "Pip");

    private static ScrollViewer ListScroller(PipsPager pager) => Part<ScrollViewer>(List(pager), "PART_ScrollViewer");

    /// <summary>The dispatcher run for &lt;paramref name="ms"/&gt;, so Avalonia's posted and timed scrolls have run.</summary>
    private static void Wait(int ms = 400)
    {
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromMilliseconds(ms));
        Dispatcher.UIThread.PushFrame(frame);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Its_Marks_Are_Flat_Squares_The_Selected_One_In_The_Accent(string variant)
    {
        var pager = new PipsPager { NumberOfPages = 5, SelectedPageIndex = 2 };
        using var scene = new Scene(pager, VariantOf(variant));

        Assert.Same(Resource<IBrush>(pager, "PhoneForegroundBrush"), pager.Foreground);
        Assert.Same(Resource<ControlTheme>(pager, "Wp81PlainPipsListTheme"), List(pager).Theme);
        Assert.Same(Resource<ControlTheme>(pager, "Wp81ScrollViewerScrollBarlessTheme"), ListScroller(pager).Theme);
        var size = Resource<double>(pager, "CornflowerPlainPipSize");
        var target = Resource<double>(pager, "CornflowerPlainPipTargetSize");
        Assert.Equal(9.5, size);
        Assert.Equal(38.5, target);

        var pips = Pips(pager);
        Assert.Equal(5, pips.Count);
        for (var i = 0; i < pips.Count; i++)
        {
            var pip = pips[i];
            Assert.Same(Resource<ControlTheme>(pager, "Wp81PlainPipTheme"), pip.Theme);
            Near(target, pip.Bounds.Width);
            Near(target, pip.Bounds.Height);
            var mark = Mark(pip);
            Near(size, mark.Bounds.Width);
            Near(size, mark.Bounds.Height);
            var at = mark.TranslatePoint(default, pip)!.Value;
            Near((target - size) / 2, at.X);
            Near((target - size) / 2, at.Y);
            Assert.Same(Resource<IBrush>(pager, i == 2 ? "PhoneAccentBrush" : "PhoneLowBrush"), mark.Fill);
            if (i > 0)
                Near(pips[i - 1].Bounds.Right, pip.Bounds.Left);   // the squares touch, in a row
            Assert.Equal(pips[0].Bounds.Top, pip.Bounds.Top);
        }

        // No ellipse anywhere: flat squares.
        Assert.Empty(pager.GetVisualDescendants().OfType<Ellipse>());
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Its_Arrows_Are_The_Plain_Glyph_Part_With_Lucides_Chevrons(string variant)
    {
        var pager = new PipsPager { NumberOfPages = 5 };
        using var scene = new Scene(pager, VariantOf(variant));
        var glyph = Resource<ControlTheme>(pager, "Wp81PlainGlyphButtonTheme");
        var target = Resource<double>(pager, "CornflowerPlainPipTargetSize");

        var previous = Part<Button>(pager, "PART_PreviousButton");
        var next = Part<Button>(pager, "PART_NextButton");
        Assert.Equal([previous, next], pager.GetVisualDescendants().OfType<Button>());
        foreach (var arrow in new[] { previous, next })
        {
            Assert.Same(glyph, arrow.Theme);
            Near(target, arrow.Bounds.Width);
            Near(target, arrow.Bounds.Height);
            Assert.True(arrow.IsVisible);
            Assert.Equal(1, arrow.Opacity);
        }

        Assert.Equal(LucideIconKind.ChevronLeft, previous.GetVisualDescendants().OfType<LucideIcon>().Single().Kind);
        Assert.Equal(LucideIconKind.ChevronRight, next.GetVisualDescendants().OfType<LucideIcon>().Single().Kind);
        // Left, the marks, right, in a row.
        Assert.True(List(pager).TranslatePoint(default, pager)!.Value.X >= previous.Bounds.Right - Pixel);
        Assert.True(next.Bounds.Left >= previous.Bounds.Right + List(pager).Bounds.Width - Pixel);

        // At the first page, previous can't go on: disabled and dimmed, not hidden.
        Assert.False(previous.IsEnabled);
        Assert.Same(Resource<IBrush>(pager, "PhoneDisabledBrush"), Part<ContentPresenter>(previous, "PART_ContentPresenter").Foreground);
        Assert.True(next.IsEnabled);
    }

    [AvaloniaFact]
    public void The_Arrows_And_A_Tap_On_A_Mark_Change_The_Page()
    {
        var pager = new PipsPager { NumberOfPages = 5 };
        using var scene = new Scene(pager);
        var previous = Part<Button>(pager, "PART_PreviousButton");
        var next = Part<Button>(pager, "PART_NextButton");

        next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, pager.SelectedPageIndex);
        Assert.True(previous.IsEnabled);
        previous.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(0, pager.SelectedPageIndex);

        var pips = Pips(pager);
        Release(scene, Press(scene, pips[3]));
        Assert.Equal(3, pager.SelectedPageIndex);
        Assert.Same(Resource<IBrush>(pager, "PhoneAccentBrush"), Mark(pips[3]).Fill);
        Assert.Same(Resource<IBrush>(pager, "PhoneLowBrush"), Mark(pips[0]).Fill);

        pager.SelectedPageIndex = 4;
        Dispatcher.UIThread.RunJobs();
        Assert.False(next.IsEnabled);
        Assert.Same(Resource<IBrush>(pager, "PhoneDisabledBrush"), Part<ContentPresenter>(next, "PART_ContentPresenter").Foreground);
    }

    // A finger down on an unselected mark (a touch selects on release) draws it in the full foreground; the selected
    // mark stays the accent under it. Disabled, the selected mark is PhoneDisabledBrush.
    [AvaloniaFact]
    public void A_Pressed_Mark_Is_The_Foreground_And_A_Disabled_Selection_Dimmed()
    {
        var pager = new PipsPager { NumberOfPages = 3 };
        using var scene = new Scene(pager);
        var pips = Pips(pager);
        var pseudo = typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var pip in pips)
            ((IPseudoClasses)pseudo.GetValue(pip)!).Set(":pressed", true);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(Resource<IBrush>(pager, "PhoneForegroundBrush"), Mark(pips[1]).Fill);
        Assert.Same(Resource<IBrush>(pager, "PhoneAccentBrush"), Mark(pips[0]).Fill);

        foreach (var pip in pips)
            ((IPseudoClasses)pseudo.GetValue(pip)!).Set(":pressed", false);
        pager.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(Resource<IBrush>(pager, "PhoneDisabledBrush"), Mark(pips[0]).Fill);
    }

    [AvaloniaFact]
    public void Vertical_It_Is_A_Column_With_Up_And_Down_Chevrons()
    {
        var pager = new PipsPager { NumberOfPages = 4, Orientation = Orientation.Vertical };
        using var scene = new Scene(pager);
        var previous = Part<Button>(pager, "PART_PreviousButton");
        var next = Part<Button>(pager, "PART_NextButton");

        Assert.Equal(LucideIconKind.ChevronUp, previous.GetVisualDescendants().OfType<LucideIcon>().Single().Kind);
        Assert.Equal(LucideIconKind.ChevronDown, next.GetVisualDescendants().OfType<LucideIcon>().Single().Kind);
        var pips = Pips(pager);
        for (var i = 1; i < pips.Count; i++)
        {
            Near(pips[i - 1].Bounds.Bottom, pips[i].Bounds.Top);
            Assert.Equal(pips[0].Bounds.Left, pips[i].Bounds.Left);
        }

        Assert.True(next.Bounds.Top >= previous.Bounds.Bottom + List(pager).Bounds.Height - Pixel);
        Near(previous.Bounds.Left, next.Bounds.Left);
    }

    // Rule 8: Simple's colour transitions on the marks and the arrows are left out.
    [AvaloniaFact]
    public void It_Has_No_Motion_Of_Its_Own()
    {
        var pager = new PipsPager { NumberOfPages = 3 };
        using var scene = new Scene(pager);

        Assert.All(Pips(pager), pip => Assert.True(Mark(pip).Transitions is null or { Count: 0 }));
        Assert.All(pager.GetVisualDescendants().OfType<Button>(), b => Assert.True(b.Transitions is null or { Count: 0 }));
    }

    // More pages than MaxVisiblePips: the row shows five fingertip squares and pans in its own ScrollViewer to the
    // selection (Avalonia's code, through the template's PART_ScrollViewer).
    [AvaloniaFact]
    public void With_More_Pages_Than_Fit_The_Row_Pans_To_The_Selection()
    {
        var pager = new PipsPager { NumberOfPages = 12, MaxVisiblePips = 5 };
        using var scene = new Scene(pager);
        Wait();

        // Five squares' room (each a target square, as layout rounding draws it).
        Near(5 * Pips(pager)[0].Bounds.Width, List(pager).Bounds.Width);
        Near(Resource<double>(pager, "CornflowerPlainPipTargetSize"), Pips(pager)[0].Bounds.Width);
        Assert.Equal(0, ListScroller(pager).Offset.X);

        pager.SelectedPageIndex = 9;
        Wait();
        Assert.True(ListScroller(pager).Offset.X > 0, $"the row stayed at {ListScroller(pager).Offset.X}");
    }

    // A selection at load, a SelectedPageIndex set in code, and a mark given the focus leave the page round the pager
    // where it is (#220, #251, #252): only the pager's own row moves.
    [AvaloniaFact]
    public void A_Selection_Never_Scrolls_The_Page()
    {
        var five = new PipsPager { NumberOfPages = 5, SelectedPageIndex = 3 };
        var twelve = new PipsPager { NumberOfPages = 12, MaxVisiblePips = 5, SelectedPageIndex = 7 };
        var page = new ScrollViewer
        {
            Content = new StackPanel { Children = { new Border { Height = 900 }, five, new Border { Height = 300 }, twelve, new Border { Height = 2000 } } },
        };
        using var scene = new Scene(page);
        // The list's own scroll to its selection is off: Avalonia's pager pans the row itself.
        Assert.False(List(twelve).AutoScrollToSelectedItem);
        Assert.True(ListBoxOptions.GetKeepsScrollInList(List(twelve)));
        Wait();
        Assert.Equal(0, page.Offset.Y);
        Assert.True(ListScroller(twelve).Offset.X > 0);

        page.Offset = new Vector(0, 2000);
        Dispatcher.UIThread.RunJobs();
        five.SelectedPageIndex = 1;
        twelve.SelectedPageIndex = 11;
        Wait();
        Assert.Equal(2000, page.Offset.Y);

        Pips(five)[4].Focus();
        Wait();
        Assert.Equal(2000, page.Offset.Y);
    }
}
