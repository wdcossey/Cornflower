using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.HubReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Hub (Cornflower's <c>Hub</c> and <c>HubSection</c>, themed by <c>Controls/Hub.axaml</c>) against the phone's
/// styles, the probes' layout and #128's and #137's motion readings (<see cref="HubReadings"/>). docs/controls/Hub has
/// the plan and the comparison.
/// </summary>
public class HubTests
{
    /// <summary>
    /// The probes' Hub: the header "hub" and four sections, below a 60 px band in the reference view; after its
    /// entrance, as the probes read it, unless <c>entrance</c>.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(int sections = 4, ThemeVariant? variant = null, double width = Scene.ReferenceWidth, Hub? hub = null,
            double band = ProbeTop, double below = 0, bool entrance = false)
        {
            Hub = hub ?? Make(sections);
            var grid = new Grid { RowDefinitions = new RowDefinitions($"{band},*,{below}") };
            Grid.SetRow(Hub, 1);
            grid.Children.Add(Hub);
            _scene = new Scene(grid, variant, width: width);
            Place();
            if (!entrance)
                Hub.Entrance.Finish();
        }

        /// <summary>Lays the window out and lets the Hub's <c>Loaded</c> come, which starts its entrance.</summary>
        public void Place()
        {
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Places the Hub and ends its entrance, so that it is drawn at rest.</summary>
        public void EndEntrance()
        {
            Place();
            Hub.Entrance.Finish();
        }

        public Hub Hub { get; }

        public Window Window => _scene.Window;

        public HubSection Section(int index) => Hub.Sections[index];

        /// <summary>A section's left edge, as drawn, from the Hub's left.</summary>
        public double SectionX(int index) => Section(index).TranslatePoint(default, Hub)!.Value.X;

        /// <summary>The offset, as the sections are drawn: how far the second section is left of its place.</summary>
        public double Offset => Hub.SectionStarts[1] - SectionX(1);

        public Control Part(string name) => Hub.GetTemplateDescendants().OfType<Control>().Single(c => c.Name == name);

        public static Control Part(TemplatedControl control, string name) =>
            control.GetTemplateDescendants().OfType<Control>().Single(c => c.Name == name);

        /// <summary>The header's left edge, as drawn, from the Hub's left.</summary>
        public double HeaderX => Part("PART_HeaderHost").TranslatePoint(default, Hub)!.Value.X;

        public double BackgroundX => Part("PART_Background").TranslatePoint(default, Hub)!.Value.X;

        public double WrapX => Part("PART_WrapBackground").TranslatePoint(default, Hub)!.Value.X;

        /// <summary>A point in the window, <paramref name="y"/> down the Hub.</summary>
        public Point At(double x, double y) => Hub.TranslatePoint(new Point(x, y), Window)!.Value;

        public void Render()
        {
            Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => _scene.Dispose();

        public static Hub Make(int sections, string header = "hub")
        {
            var hub = new Hub { Header = header };
            for (var i = 1; i <= sections; i++)
                hub.Sections.Add(MakeSection("section " + i));
            return hub;
        }

        public static HubSection MakeSection(string header) => new()
        {
            Header = header,
            ContentTemplate = new FuncDataTemplate<object?>((_, _) => new TextBlock { Text = header + " content", FontSize = 30 }),
        };
    }

    /// <summary>A clock on the emulator's frames, so each frame is one of the phone's.</summary>
    private static TestClock PhoneClock() => new() { FrameInterval = TimeSpan.FromMilliseconds(Frame), FramesOnGrid = true };

    /// <summary>
    /// The probes' swipe, let go at <paramref name="release"/> ms (on a frame): 15 moves of 13.333 px 10 ms apart, left
    /// from x 380 or right from 70, at y 440 down the Hub (500 in the view). With <paramref name="holdMs"/>, held still
    /// that long before it lifts.
    /// </summary>
    private static void Swipe(Stage stage, TestClock clock, double release, bool left = true, int moves = SwipeMoves,
        double step = SwipeStep, double holdMs = 0, double from = double.NaN)
    {
        var x = double.IsNaN(from) ? (left ? 380 : 70) : from;
        var sign = left ? -1 : 1;
        var at = release - moves * SwipeMoveMs - holdMs;
        clock.At(at);
        var touch = stage.Window.TouchBegin(stage.At(x, 440), RawInputModifiers.None);
        for (var i = 1; i <= moves; i++)
        {
            clock.At(at + i * SwipeMoveMs);
            stage.Window.TouchMove(touch, stage.At(x + sign * i * step, 440), RawInputModifiers.None);
        }

        clock.At(release);
        stage.Window.TouchEnd(touch, stage.At(x + sign * moves * step, 440), RawInputModifiers.None);
    }

    /// <summary>The probes' slow drag: 150 px left over 500 ms (50 moves of 3 px), held 300 ms, let go.</summary>
    private static void SlowDrag(Stage stage, TestClock clock, double release, double px = 150) =>
        Swipe(stage, clock, release, moves: (int)(px / 3), step: 3, holdMs: 300, from: 300);

    // ---- The style, the template and the layout ----

    [AvaloniaFact]
    public void Each_Control_Has_The_Wp81_Template()
    {
        using var stage = new Stage();
        var scroll = (ScrollViewer)stage.Part("PART_ScrollViewer");
        Assert.Same(stage.Hub.FindResource("Wp81ScrollViewerScrollBarlessTheme"), scroll.Theme);
        Assert.Equal(ScrollBarVisibility.Disabled, scroll.HorizontalScrollBarVisibility);
        Assert.IsType<StackPanel>(stage.Part("PART_Panel"));
        Assert.IsType<ContentControl>(stage.Part("PART_HeaderHost"));
        Assert.IsType<Border>(stage.Part("PART_MainBackground"));
        Assert.IsType<Border>(stage.Part("PART_WrapBackground"));
        Assert.IsType<Rectangle>(Stage.Part(stage.Section(0), "PART_HubHeaderPlaceholder"));
        Assert.Same(stage.Hub.FindResource("Wp81ContentControlTheme"), ((ContentControl)Stage.Part(stage.Section(0), "PART_Header")).Theme);
        Assert.IsType<ContentPresenter>(Stage.Part(stage.Section(0), "PART_ContentPresenter"));
        // The sections are the Hub's, in its panel.
        Assert.All(stage.Hub.Sections, s => Assert.Same(stage.Hub, ((ILogical)s).LogicalParent));
        Assert.All(stage.Hub.Sections, s => Assert.Same(stage.Part("PART_Panel"), s.GetVisualParent()));
    }

    // The probes' layout (run 20261116-0009-dev11): the header at 15, 1, 103.75 tall; the sections 405 x 740 side by
    // side; each section's placeholder the header's height, its header 5 below and 1 left of its 19 inset.
    [AvaloniaFact]
    public void The_Layout_Is_The_Probes()
    {
        using var stage = new Stage();
        var host = stage.Part("PART_HeaderHost");
        var at = host.TranslatePoint(default, stage.Hub)!.Value;
        Assert.Equal(HeaderLeft, at.X, 0.01);
        Assert.Equal(HeaderTop, at.Y, 0.2);                                  // 0.83: 1 rounds down to the raw grid
        Assert.Equal(HeaderHeight, host.Bounds.Height, 0.02);
        Assert.Equal(ProbeHeight, stage.Hub.Bounds.Height, 0.01);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(i * SectionWidth, stage.SectionX(i), 0.01);
            Assert.Equal(SectionWidth, stage.Section(i).Bounds.Width, 0.01);
            Assert.Equal(ProbeHeight, stage.Section(i).Bounds.Height, 0.01);
        }

        var section = stage.Section(0);
        var placeholder = (Rectangle)Stage.Part(section, "PART_HubHeaderPlaceholder");
        Assert.Equal(host.Bounds.Height, placeholder.Bounds.Height, 0.001);
        var header = Stage.Part(section, "PART_Header").TranslatePoint(default, section)!.Value;
        Assert.Equal(SectionInset + SectionHeaderLeftMargin, header.X, 0.4);  // rounding: 19 is 19.17 on the raw grid
        Assert.Equal(HeaderHeight + SectionHeaderTopMargin, header.Y, 0.02);
        var content = Stage.Part(section, "PART_ContentPresenter");
        Assert.Equal(header.Y + Stage.Part(section, "PART_Header").Bounds.Height + SectionHeaderBottomMargin,
            content.TranslatePoint(default, section)!.Value.Y, 0.4);
        Assert.Equal(SectionInset, content.TranslatePoint(default, section)!.Value.X, 0.2);
    }

    // #137's catalogue dumps on three profiles: a section is 0.9 of the Hub, unless it has its own Width.
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_Section_Is_Nine_Tenths_Of_The_Hub(int profile)
    {
        var (hub, section) = SectionWidths[profile];
        using var stage = new Stage(width: hub);
        Assert.Equal(section, stage.Section(0).Bounds.Width, 0.5);
        Assert.Equal(section, stage.SectionX(1), 0.5);
    }

    [AvaloniaFact]
    public void A_Sections_Own_Width_Wins()
    {
        var hub = Stage.Make(3);
        hub.Sections[1].Width = 600;
        using var stage = new Stage(hub: hub);
        Assert.Equal(600, stage.Section(1).Bounds.Width, 0.01);
        Assert.Equal(1005, stage.Hub.SectionStarts[2], 0.01);
    }

    [AvaloniaFact]
    public void The_Headers_Are_The_Phones_Size_Weight_And_Spacing()
    {
        using var stage = new Stage();
        var host = (ContentControl)stage.Part("PART_HeaderHost");
        Assert.Equal(78, host.FontSize);
        Assert.Equal(FontWeight.Light, host.FontWeight);
        Assert.Equal(-22 * 78 / 1000.0, host.LetterSpacing, 6);
        Assert.Same(stage.Hub.FindResource("PhoneFontFamilyNormal"), host.FontFamily);
        Assert.Equal(new Thickness(15, 1, 0, 0), host.Margin);
        var text = host.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("hub", text.Text);
        Assert.Equal(-22 * 78 / 1000.0, text.LetterSpacing, 6);              // inherited, as WinRT's CharacterSpacing

        var header = (ContentControl)Stage.Part(stage.Section(0), "PART_Header");
        Assert.Equal(19, header.FontSize);
        Assert.Equal(FontWeight.SemiBold, header.FontWeight);
        Assert.Equal(-10 * 19 / 1000.0, header.LetterSpacing, 6);
        Assert.Equal(HorizontalAlignment.Left, header.HorizontalAlignment);
        Assert.Equal(new Thickness(-1, 5, 0, 31.5), header.Margin);
        // The phone's 1.33 em line (Wp81's ContentControl theme), so the 19 px header is 25.27 tall.
        Assert.Equal(19 * 1.33, header.Bounds.Height, 0.2);
        Assert.Equal(HorizontalAlignment.Stretch, stage.Section(0).HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Stretch, stage.Section(0).VerticalContentAlignment);
    }

    [AvaloniaFact]
    public void The_Header_And_Sections_Are_The_Foreground_Dark_And_Light()
    {
        foreach (var (variant, argb) in new[] { (ThemeVariant.Dark, 0xFFFFFFFFu), (ThemeVariant.Light, 0xFF000000u) })
        {
            using var stage = new Stage(variant: variant);
            var text = stage.Part("PART_HeaderHost").GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(argb, ((ISolidColorBrush)text.Foreground!).Color.ToUInt32());
            var content = stage.Section(0).BuiltContent!;
            Assert.Equal(argb, ((ISolidColorBrush)((TextBlock)content).Foreground!).Color.ToUInt32());
        }
    }

    // ---- The content, the templates and the API ----

    [AvaloniaFact]
    public void The_Content_Is_Built_From_The_Template_And_Takes_The_DataContext()
    {
        var section = new HubSection
        {
            Header = "data",
            DataContext = "the data",
            ContentTemplate = new FuncDataTemplate<object?>((_, _) => new TextBlock { [!TextBlock.TextProperty] = new Avalonia.Data.Binding() }),
            HeaderTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s.ToUpperInvariant() }),
        };
        var hub = new Hub { HeaderTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = "[" + s + "]" }), Header = "hub" };
        hub.Sections.Add(section);
        using var stage = new Stage(hub: hub);
        var content = (TextBlock)section.BuiltContent!;
        Assert.Equal("the data", content.Text);
        Assert.Same(Stage.Part(section, "PART_ContentPresenter"), content.GetVisualParent());
        Assert.Contains(Stage.Part(section, "PART_Header").GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "DATA");
        Assert.Contains(stage.Part("PART_HeaderHost").GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "[hub]");

        section.DataContext = "new data";
        stage.Window.UpdateLayout();
        Assert.Same(content, section.BuiltContent);                          // built once; the data flows in
        Assert.Equal("new data", content.Text);

        section.ContentTemplate = new FuncDataTemplate<object?>((_, _) => new Button());
        stage.Window.UpdateLayout();
        Assert.IsType<Button>(section.BuiltContent);
        Assert.Null(content.GetVisualParent());
    }

    [AvaloniaFact]
    public void The_Content_Alignments_Place_The_Content()
    {
        var hub = Stage.Make(2);
        hub.Sections[0].HorizontalContentAlignment = HorizontalAlignment.Right;
        hub.Sections[0].VerticalContentAlignment = VerticalAlignment.Bottom;
        hub.Sections[0].Padding = new Thickness(0, 0, 10, 20);
        using var stage = new Stage(hub: hub);
        var content = stage.Section(0).BuiltContent!;
        var at = content.TranslatePoint(new Point(content.Bounds.Width, content.Bounds.Height), stage.Section(0))!.Value;
        Assert.Equal(SectionWidth - 10, at.X, 0.5);
        Assert.Equal(ProbeHeight - 20, at.Y, 0.5);
    }

    [AvaloniaFact]
    public void SectionHeaders_Are_The_Sections_Headers_As_They_Are()
    {
        using var stage = new Stage(3);
        Assert.Equal(["section 1", "section 2", "section 3"], stage.Hub.SectionHeaders);
        stage.Section(1).Header = "two";
        Assert.Equal("two", stage.Hub.SectionHeaders[1]);
        stage.Hub.Sections.Add(Stage.MakeSection("four"));
        Assert.Equal(4, stage.Hub.SectionHeaders.Count);
    }

    [AvaloniaFact]
    public void Sections_Added_And_Removed_Join_And_Leave_The_Hub()
    {
        using var stage = new Stage(2);
        var panel = (Panel)stage.Part("PART_Panel");
        var added = Stage.MakeSection("new");
        stage.Hub.Sections.Insert(1, added);
        stage.Window.UpdateLayout();
        Assert.Same(stage.Hub, ((ILogical)added).LogicalParent);
        Assert.Same(added, panel.Children[1]);
        Assert.Equal(SectionWidth, stage.SectionX(1), 0.01);
        Assert.Equal([0, 405, 810], stage.Hub.SectionStarts);

        var removed = stage.Section(0);
        stage.Hub.Sections.RemoveAt(0);
        stage.Window.UpdateLayout();
        Assert.Null(((ILogical)removed).LogicalParent);
        Assert.Null(removed.GetVisualParent());
        Assert.Equal(0, stage.SectionX(0), 0.01);

        stage.Hub.Sections.Clear();
        stage.Window.UpdateLayout();
        Assert.Empty(panel.Children);
        Assert.Empty(stage.Hub.SectionsInView);
    }

    [AvaloniaFact]
    public void ScrollToSection_Moves_The_Sections_At_Once()
    {
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(2));
        Assert.Equal(810, stage.Offset, 0.01);
        Assert.Equal(HeaderLeft - 810 * HeaderRatio, stage.HeaderX, 0.01);
        Assert.False(stage.Hub.Motion.IsMoving);
        Assert.Equal([stage.Section(2), stage.Section(3)], stage.Hub.SectionsInView);

        // At a new size the Hub stays on its section.
        stage.Hub.Width = 400;
        stage.Window.UpdateLayout();
        Assert.Equal(720, stage.Offset, 0.5);
    }

    [AvaloniaFact]
    public void DefaultSectionIndex_Is_The_Section_Shown_First()
    {
        var hub = Stage.Make(4);
        hub.DefaultSectionIndex = 2;
        using (var stage = new Stage(hub: hub))
        {
            Assert.Equal(810, stage.Offset, 0.01);
            Assert.Equal(HeaderLeft - 810 * HeaderRatio, stage.HeaderX, 0.01);
        }

        var past = Stage.Make(3);
        past.DefaultSectionIndex = 7;
        using (var stage = new Stage(hub: past))
            Assert.Equal(810, stage.Offset, 0.01);                           // the last
    }

    [AvaloniaFact]
    public void ScrollToSection_Before_The_Hub_Is_Shown_Wins_Over_The_Default()
    {
        var hub = Stage.Make(4);
        hub.DefaultSectionIndex = 1;
        hub.ScrollToSection(hub.Sections[3]);
        using var stage = new Stage(hub: hub);
        Assert.Equal(1215, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void A_Tap_On_An_Interactive_Header_Raises_SectionHeaderClick()
    {
        using var stage = new Stage(3);
        var clicks = new List<HubSection>();
        stage.Hub.SectionHeaderClick += (_, e) => clicks.Add(e.Section);
        stage.Section(0).IsHeaderInteractive = true;
        stage.Render();
        Point Centre(Control c) => c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), stage.Window)!.Value;

        // Each click somewhere else, so none is a double click. The second section's header shows at the right edge.
        var second = Stage.Part(stage.Section(1), "PART_Header");
        var plain = second.TranslatePoint(new Point(4, second.Bounds.Height / 2), stage.Window)!.Value;
        Assert.InRange(plain.X, 400, 449);
        stage.Window.MouseDown(plain, MouseButton.Left);
        stage.Window.MouseUp(plain, MouseButton.Left);
        Assert.Empty(clicks);                                                // not interactive: nothing

        var header = Centre(Stage.Part(stage.Section(0), "PART_Header"));
        stage.Window.MouseDown(header, MouseButton.Left);
        stage.Window.MouseUp(header, MouseButton.Left);
        Assert.Equal([stage.Section(0)], clicks);

        var content = stage.Section(0).BuiltContent!.TranslatePoint(new Point(10, 10), stage.Window)!.Value;
        stage.Window.MouseDown(content, MouseButton.Left);
        stage.Window.MouseUp(content, MouseButton.Left);
        Assert.Single(clicks);                                               // the content isn't the header
    }

    [AvaloniaFact]
    public void A_Section_Put_First_While_Wrapped_Leaves_The_Old_First_At_Its_Place()
    {
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));                        // wrapped: the first past the last
        var oldFirst = stage.Section(0);
        Assert.NotNull(oldFirst.RenderTransform);
        stage.Hub.Sections.Insert(0, Stage.MakeSection("new"));
        stage.Window.UpdateLayout();
        Assert.Equal(1620, stage.Offset, 0.01);                              // still at the old last, now the fifth
        Assert.Null(oldFirst.RenderTransform);
        Assert.Equal(SectionWidth, oldFirst.TranslatePoint(default, stage.Hub)!.Value.X + 1620, 0.01);
    }

    [AvaloniaFact]
    public void A_Hub_Laid_Out_Hidden_Opens_On_Its_Default_Section()
    {
        var hub = Stage.Make(4);
        hub.DefaultSectionIndex = 2;
        hub.IsVisible = false;
        using var stage = new Stage(hub: hub);
        hub.IsVisible = true;
        stage.EndEntrance();
        Assert.Equal(810, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void Sections_Added_After_The_Hub_Is_Shown_Open_On_The_Default()
    {
        var hub = new Hub { Header = "hub", DefaultSectionIndex = 2 };
        using var stage = new Stage(hub: hub);
        for (var i = 1; i <= 4; i++)
            hub.Sections.Add(Stage.MakeSection("section " + i));
        stage.Window.UpdateLayout();
        Assert.Equal(810, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void Shown_Again_The_Hub_Keeps_Its_Place_And_A_Drag_Cut_Short_Ends()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var grid = (Grid)stage.Hub.Parent!;
        stage.Hub.ScrollToSection(stage.Section(3));
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(300, 440), RawInputModifiers.None);
        clock.At(110);
        stage.Window.TouchMove(touch, stage.At(250, 440), RawInputModifiers.None);
        Assert.True(stage.Hub.Motion.IsDragging);
        grid.Children.Remove(stage.Hub);                                     // away mid-drag
        Assert.False(stage.Hub.Motion.IsDragging);
        grid.Children.Add(stage.Hub);
        stage.EndEntrance();
        Assert.Equal(1215, stage.Offset, 0.01);                              // where it was, not the default
        stage.Hub.ScrollToSection(stage.Section(1));
        stage.Window.UpdateLayout();
        Assert.Equal(405, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void A_New_Width_Mid_Swipe_Ends_On_The_Section_It_Was_Going_To()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 250);
        clock.At(250 + 5 * Frame);
        stage.Hub.Width = 400;
        stage.Window.UpdateLayout();
        Assert.False(stage.Hub.Motion.IsMoving);
        Assert.Equal(360, stage.Offset, 0.5);                                // the second section, at its new place
    }

    [AvaloniaFact]
    public void A_Control_Brought_Into_View_Brings_Its_Section()
    {
        var hub = Stage.Make(4);
        var button = new Button { Content = "in the third" };
        hub.Sections[2].ContentTemplate = new FuncDataTemplate<object?>((_, _) => button);
        using var stage = new Stage(hub: hub);
        button.BringIntoView();
        stage.Window.UpdateLayout();
        Assert.Equal(810, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void A_Control_Down_A_Sections_List_Brought_Into_View_Brings_The_Section_Too()
    {
        var hub = Stage.Make(3);
        var lines = new StackPanel();
        for (var i = 0; i < 60; i++) lines.Children.Add(new TextBlock { Text = "line " + i });
        var button = new Button { Content = "at the bottom" };
        lines.Children.Add(button);
        var scroller = new ScrollViewer { Content = lines };
        hub.Sections[1].ContentTemplate = new FuncDataTemplate<object?>((_, _) => scroller);
        using var stage = new Stage(hub: hub);
        button.BringIntoView();                                              // the list scrolls down and handles it
        stage.Window.UpdateLayout();
        Assert.True(scroller.Offset.Y > 100);
        Assert.Equal(SectionWidth, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void A_Control_Already_In_View_Brought_Into_View_Leaves_The_Hub()
    {
        var hub = Stage.Make(3);
        var button = new Border { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
        hub.Sections[1].ContentTemplate = new FuncDataTemplate<object?>((_, _) => button);
        using var stage = new Stage(hub: hub);
        Assert.InRange(button.TranslatePoint(default, stage.Hub)!.Value.X, 405, 430);   // in the second's edge, on screen
        button.BringIntoView();
        stage.Window.UpdateLayout();
        Assert.Equal(0, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void A_Drag_Ended_By_A_New_Layout_Rests_On_The_Section_It_Was_On()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(100, 440), RawInputModifiers.None);
        clock.At(110);
        stage.Window.TouchMove(touch, stage.At(115, 440), RawInputModifiers.None);
        Assert.Equal(1200, stage.Offset, 0.01);
        stage.Hub.Width = 900;
        stage.Hub.HorizontalAlignment = HorizontalAlignment.Left;
        stage.Window.UpdateLayout();
        Assert.False(stage.Hub.Motion.IsDragging);
        Assert.Equal(3 * 810, stage.Offset, 0.5);                            // the fourth, at its new place
        stage.Window.TouchEnd(touch, stage.At(115, 440), RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void A_Drag_Cut_Short_Near_The_Loops_End_Rests_On_The_First_Section()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(420, 440), RawInputModifiers.None);
        clock.At(110);
        stage.Window.TouchMove(touch, stage.At(20, 440), RawInputModifiers.None);
        Assert.Equal(1615, 1215 - stage.SectionX(3), 0.01);                  // the first 5 px from its place
        var grid = (Grid)stage.Hub.Parent!;
        grid.Children.Remove(stage.Hub);
        grid.Children.Add(stage.Hub);
        stage.EndEntrance();
        Assert.Equal(0, stage.SectionX(0), 0.01);
    }

    [AvaloniaFact]
    public void A_Tap_While_Only_The_Header_Comes_Back_Is_The_Contents()
    {
        using var clock = PhoneClock();
        var hub = Stage.Make(4);
        var clicks = 0;
        var button = new Button { Content = "tap me" };
        button.Click += (_, _) => clicks++;
        hub.Sections[0].ContentTemplate = new FuncDataTemplate<object?>((_, _) => button);
        using var stage = new Stage(hub: hub);
        stage.Hub.ScrollToSection(stage.Section(3));
        Swipe(stage, clock, 250);
        clock.At(250 + 22 * Frame);                                          // past the reset, the header still easing
        Assert.True(stage.Hub.Motion.IsLooping);
        stage.Render();
        var at = button.TranslatePoint(new Point(10, 10), stage.Window)!.Value;
        var touch = stage.Window.TouchBegin(at, RawInputModifiers.None);
        stage.Window.TouchEnd(touch, at, RawInputModifiers.None);
        Assert.Equal(1, clicks);
        Assert.True(stage.Hub.Motion.IsLooping);                             // the header's ease goes on
    }

    [AvaloniaFact]
    public void Sections_Changed_Mid_Swipe_End_It_On_The_Section_It_Was_Going_To()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 250);
        clock.At(250 + 5 * Frame);
        stage.Hub.Sections.Add(Stage.MakeSection("section 5"));
        stage.Window.UpdateLayout();
        Assert.False(stage.Hub.Motion.IsMoving);
        Assert.Equal(SectionWidth, stage.Offset, 0.01);
    }

    [AvaloniaFact]
    public void A_Sections_Content_Is_Built_With_The_Data_It_Gets_In_The_Hub()
    {
        var section = new HubSection { ContentTemplate = new FuncDataTemplate<object?>((data, _) => new TextBlock { Text = data as string ?? "no data" }) };
        var hub = new Hub { DataContext = "the hub's data" };
        hub.Sections.Add(section);
        using var stage = new Stage(hub: hub);
        Assert.Equal("the hub's data", ((TextBlock)section.BuiltContent!).Text);
    }

    [AvaloniaFact]
    public void A_Section_Joins_One_Hub_Once()
    {
        var hub = Stage.Make(2);
        var other = new Hub();
        Assert.Throws<ArgumentNullException>(() => hub.Sections.Add(null!));
        Assert.Throws<InvalidOperationException>(() => other.Sections.Add(hub.Sections[0]));
        Assert.Throws<InvalidOperationException>(() => hub.Sections.Add(hub.Sections[0]));
        Assert.Equal(2, hub.Sections.Count);
        Assert.Empty(other.Sections);
        Assert.Same(hub, hub.Sections[0].Owner);
    }

    [AvaloniaFact]
    public void A_Section_Replaced_Is_Read_Again_Before_Anything_Is_Placed()
    {
        using var stage = new Stage();
        stage.Hub.Sections[1] = Stage.MakeSection("new");
        Assert.Empty(stage.Hub.SectionStarts);                               // the old places aren't trusted
        stage.Window.UpdateLayout();
        Assert.Equal([0, 405, 810, 1215], stage.Hub.SectionStarts);
    }

    // ---- The header against the sections (#128) ----

    [AvaloniaFact]
    public void The_Header_Moves_At_An_Eighteenth_Of_The_Sections_Speed()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(400, 440), RawInputModifiers.None);
        foreach (var travel in new[] { 21.0, 100, 250.5, 390 })
        {
            stage.Window.TouchMove(touch, stage.At(400 - travel, 440), RawInputModifiers.None);
            Assert.Equal(travel, stage.Offset, 0.001);
            Assert.Equal(HeaderLeft - travel * HeaderRatio, stage.HeaderX, 0.001);
        }

        stage.Window.TouchEnd(touch, stage.At(10, 440), RawInputModifiers.None);
        stage.Hub.ScrollToSection(stage.Section(3));
        Assert.Equal(HeaderLeft - 1215 * HeaderRatio, stage.HeaderX, 0.001);  // −52.5, the phone's at 1215
    }

    // ---- The swipe (#128, #137) ----

    [AvaloniaFact]
    public void A_Swipe_Left_Goes_On_One_Section_On_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        const double release = 250;                                          // on a frame
        Swipe(stage, clock, release);
        var from = stage.Offset;
        Assert.Equal(200, from, 0.01);                                       // the finger's 200 px
        for (var k = 1; k <= SwipeFractions.Length; k++)
        {
            clock.At(release + k * Frame);
            var fraction = (stage.Offset - from) / (SectionWidth - from);
            Assert.True(Math.Abs(fraction - SwipeFractions[k - 1]) < 0.005, $"frame {k}: {fraction:F4}, the phone's {SwipeFractions[k - 1]:F4}");
            Assert.Equal(HeaderLeft - stage.Offset * HeaderRatio, stage.HeaderX, 0.001);
        }

        clock.At(release + (SwipeFractions.Length + 1) * Frame);            // the curve's 743.75 ms are over
        Assert.False(stage.Hub.Motion.IsMoving);
        Assert.Equal(SectionWidth, stage.Offset, 0.0001);                    // exactly a section
        Assert.Equal(-7.5, stage.HeaderX, 0.001);                            // the phone's at 405
        Assert.Equal([stage.Section(1), stage.Section(2)], stage.Hub.SectionsInView);
    }

    [AvaloniaFact]
    public void Swipes_Go_A_Section_Each_And_Right_Goes_Back_One()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 250);
        Swipe(stage, clock, 1250);
        Swipe(stage, clock, 2250);
        clock.At(3000);
        Assert.Equal(1215, stage.Offset, 0.0001);
        Assert.Equal(-52.5, stage.HeaderX, 0.001);                           // the phone's at 1215
        Swipe(stage, clock, 3250, left: false);
        var from = stage.Offset;
        Assert.Equal(1015, from, 0.01);
        clock.At(3250 + 4 * Frame);
        Assert.Equal(SwipeFractions[3], (stage.Offset - from) / (810 - from), 2);
        clock.At(4000);
        Assert.Equal(810, stage.Offset, 0.0001);
    }

    [AvaloniaFact]
    public void A_Slow_Drag_Let_Go_Goes_Back_On_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        const double release = 1000;
        SlowDrag(stage, clock, release);
        var from = stage.Offset;
        Assert.Equal(150, from, 0.01);
        for (var k = 1; k <= ReturnFractions.Length; k++)
        {
            clock.At(release + k * Frame);
            var fraction = (stage.Offset - from) / (0 - from);
            Assert.True(Math.Abs(fraction - ReturnFractions[k - 1]) < 0.01, $"frame {k}: {fraction:F4}, the phone's {ReturnFractions[k - 1]:F4}");
        }

        clock.At(release + 687.5);
        Assert.Equal(0, stage.Offset, 0.0001);
        Assert.False(stage.Hub.Motion.IsMoving);
    }

    // Not read on the phone: past half a section, a slow release goes on (decided; PLAN.md).
    [AvaloniaFact]
    public void A_Slow_Drag_Past_Half_A_Section_Goes_On()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        SlowDrag(stage, clock, 1500, px: 240);
        Assert.Equal(240, stage.Offset, 0.01);
        clock.At(1500 + 687.5);
        Assert.Equal(SectionWidth, stage.Offset, 0.0001);
    }

    // ---- The loop backwards (#148) ----

    /// <summary>The probes' drag right at the first section: 150 px over 500 ms from x 50, held 400 ms, let go.</summary>
    private static void PullRight(Stage stage, TestClock clock, double release, double px = 150) =>
        Swipe(stage, clock, release, left: false, moves: (int)(px / 3), step: 3, holdMs: 400, from: 50);

    [AvaloniaFact]
    public void A_Drag_Right_At_The_First_Section_Pulls_It_Right_With_Nothing_Left_Of_It()
    {
        var hub = Stage.Make(4);
        hub.Background = new ImageBrush(Image(1600, 800));
        using var clock = PhoneClock();
        using var stage = new Stage(hub: hub);
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(50, 440), RawInputModifiers.None);
        foreach (var travel in new[] { 40.0, 100, 150 })
        {
            stage.Window.TouchMove(touch, stage.At(50 + travel, 440), RawInputModifiers.None);
            Assert.Equal(travel, stage.SectionX(0), 0.001);                  // with the finger
            Assert.Equal(HeaderLeft + travel * HeaderRatio, stage.HeaderX, 0.001);
            Assert.Equal(1215 + travel, stage.SectionX(3), 0.001);          // the last isn't drawn left of the first
            Assert.Equal(travel * Rate1600, stage.BackgroundX, 0.001);       // the image at its rate, its copy filling in
            Assert.Equal(-1599, stage.WrapX - stage.BackgroundX, 0.001);
        }

        Assert.Equal([stage.Section(0)], stage.Hub.SectionsInView);
        stage.Window.TouchEnd(touch, stage.At(200, 440), RawInputModifiers.None);
        hub.Motion.BeginDrag();
        hub.Motion.Drag(-600);
        Assert.Equal(405, stage.SectionX(0), 0.001);                         // no further than where the end shows it
        hub.Motion.Release(0);
    }

    [AvaloniaFact]
    public void Held_And_Let_Go_Right_Of_The_First_Section_It_Comes_Back_On_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        const double release = 1500;
        PullRight(stage, clock, release);
        var from = stage.SectionX(0);
        Assert.Equal(150, from, 0.01);
        for (var k = 2; k < BackReturnFractions.Length + 2; k++)
        {
            clock.At(release + k * Frame);
            var fraction = 1 - stage.SectionX(0) / from;
            Assert.True(Math.Abs(fraction - BackReturnFractions[k - 2]) < 0.008, $"frame {k}: {fraction:F4}, the phone's {BackReturnFractions[k - 2]:F4}");
            Assert.Equal(HeaderLeft + stage.SectionX(0) * HeaderRatio, stage.HeaderX, 0.001);
        }

        clock.At(release + (BackReturnFractions.Length + 2) * Frame);
        Assert.Equal(0, stage.Offset, 0.0001);
        Assert.False(stage.Hub.Motion.IsMoving);
    }

    [AvaloniaFact]
    public void A_Swipe_Right_At_The_First_Section_Loops_Backwards_To_The_Last()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        const double release = 250;
        Swipe(stage, clock, release, left: false);
        var from = stage.SectionX(0);
        Assert.Equal(200, from, 0.01);
        var held = stage.HeaderX;
        Assert.Equal(HeaderLeft + from * HeaderRatio, held, 0.001);
        var carry = stage.Hub.SectionsLength - 1215;                        // where the end shows the first: 405
        var header = new List<double> { held };
        for (var k = 1; k <= HeaderBackJumpFrame + HeaderBackFlyIn.Length; k++)
        {
            clock.At(release + k * Frame);
            header.Add(stage.HeaderX);
            if (k < LoopResetFrame)
            {
                var fraction = (stage.SectionX(0) - from) / (carry - from);
                Assert.True(Math.Abs(fraction - BackLoopFractions[k - 1]) < 0.009, $"frame {k}: {fraction:F4}, the phone's {BackLoopFractions[k - 1]:F4}");
                Assert.True(stage.SectionX(3) > 450);                          // nothing left of the first on the way
                Assert.True(stage.Hub.Motion.AreSectionsMoving);
            }
            else if (k == LoopResetFrame)
            {
                Assert.Equal(1215, stage.Offset, 0.01);                      // the end: the last, the first after it
                Assert.Equal(0, stage.SectionX(3), 0.01);
                Assert.Equal(SectionWidth, stage.SectionX(0), 0.01);
                Assert.False(stage.Hub.Motion.AreSectionsMoving);            // the header still coming back
            }
        }

        // The header: held, flown out right, then a jump to 33.8 px left of its place and a quick ease in.
        var place = HeaderLeft - 1215 * HeaderRatio;
        for (var k = 1; k <= HeaderHoldFrames; k++)
            Assert.Equal(held, header[k], 0.001);
        for (var k = HeaderHoldFrames + 1; k < HeaderBackJumpFrame; k++)
            Assert.Equal(held + (k - HeaderHoldFrames) * HeaderBackFlyOutPerFrame, header[k], 0.01);
        for (var k = 0; k < HeaderBackFlyIn.Length; k++)
            Assert.Equal(place - HeaderBackFlyIn[k], header[HeaderBackJumpFrame + k], 0.1);
        Assert.False(stage.Hub.Motion.IsMoving);
        Assert.Equal(place, stage.HeaderX, 0.0001);
        Assert.Equal([stage.Section(3), stage.Section(0)], stage.Hub.SectionsInView);
    }

    // Not read on the phone: where a release right of the first section goes but for a held one and a swipe (decided; PLAN.md).
    [AvaloniaFact]
    public void A_Release_Right_Of_The_First_Section_Goes_As_The_Loop_Forward_Does()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        PullRight(stage, clock, 1500, px: 210);                              // past half of 405, held: on round
        clock.At(1500 + 600);
        Assert.Equal(1215, stage.Offset, 0.0001);

        stage.Hub.ScrollToSection(stage.Section(0));
        Swipe(stage, clock, 3000, left: false, moves: 6, step: 10, from: 50);   // 60 px right at 1 px/ms: on round
        clock.At(3600);
        Assert.Equal(1215, stage.Offset, 0.0001);

        // 300 px right, then 60 px back left in 60 ms: back to 0, however far it was pulled.
        stage.Hub.ScrollToSection(stage.Section(0));
        clock.At(5000);
        var touch = stage.Window.TouchBegin(stage.At(50, 440), RawInputModifiers.None);
        for (var i = 1; i <= 30; i++)
        {
            clock.At(5000 + i * 10);
            stage.Window.TouchMove(touch, stage.At(50 + i * 10, 440), RawInputModifiers.None);
        }

        for (var i = 1; i <= 6; i++)
        {
            clock.At(5300 + i * 10);
            stage.Window.TouchMove(touch, stage.At(350 - i * 10, 440), RawInputModifiers.None);
        }

        clock.At(5360);
        stage.Window.TouchEnd(touch, stage.At(290, 440), RawInputModifiers.None);
        Assert.Equal(-240, stage.Offset, 0.01);
        clock.At(5360 + 781.25);
        Assert.Equal(0, stage.Offset, 0.0001);
    }

    [AvaloniaFact]
    public void A_Press_During_The_Loop_Backwards_Ends_It_At_The_End()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 250, left: false);
        clock.At(250 + 3 * Frame);
        var touch = stage.Window.TouchBegin(stage.At(200, 440), RawInputModifiers.None);
        clock.At(400);
        stage.Window.TouchMove(touch, stage.At(180, 440), RawInputModifiers.None);
        Assert.False(stage.Hub.Motion.IsLooping);
        Assert.Equal(1235, stage.Offset, 0.01);
        Assert.Equal(HeaderLeft - 1235 * HeaderRatio, stage.HeaderX, 0.001);
        stage.Window.TouchEnd(touch, stage.At(180, 440), RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void A_Drag_Cut_Short_Near_The_Loop_Backwards_End_Rests_On_The_Last_Section()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(20, 440), RawInputModifiers.None);
        clock.At(110);
        stage.Window.TouchMove(touch, stage.At(420, 440), RawInputModifiers.None);
        Assert.Equal(400, stage.SectionX(0), 0.01);
        var grid = (Grid)stage.Hub.Parent!;
        grid.Children.Remove(stage.Hub);
        grid.Children.Add(stage.Hub);
        stage.EndEntrance();
        Assert.Equal(1215, stage.Offset, 0.01);
    }

    // Not read on the phone: a press while the sections move catches them where they are (decided; PLAN.md).
    [AvaloniaFact]
    public void A_Press_While_They_Move_Catches_The_Sections()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 250);
        clock.At(250 + 5 * Frame);
        var caught = stage.Offset;
        var touch = stage.Window.TouchBegin(stage.At(200, 440), RawInputModifiers.None);
        clock.At(500);
        stage.Window.TouchMove(touch, stage.At(190, 440), RawInputModifiers.None);
        Assert.Equal(caught + 10, stage.Offset, 0.01);
        stage.Window.TouchEnd(touch, stage.At(190, 440), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(SectionWidth, stage.Offset, 0.0001);
    }

    // ---- The end and the loop (#137) ----

    [AvaloniaFact]
    public void At_The_End_The_First_Section_Shows_Past_The_Last()
    {
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));
        Assert.Equal(0, stage.SectionX(3), 0.01);
        Assert.Equal(SectionWidth, stage.SectionX(0), 0.01);                 // the phone's peek, at 405 (404.58)
        Assert.Equal([stage.Section(3), stage.Section(0)], stage.Hub.SectionsInView);
        var text = stage.Section(0).BuiltContent!.TranslatePoint(default, stage.Hub)!.Value;
        Assert.Equal(PeekHeaderInkLeft, text.X, 1.5);                       // the recording's peek, its ink
        stage.Render();
        var hit = stage.Window.InputHitTest(stage.At(446, 185)) as Visual;
        Assert.Same(stage.Section(0), hit?.FindAncestorOfType<HubSection>());

        // Back at the start, it is at its place again.
        stage.Hub.ScrollToSection(stage.Section(0));
        Assert.Equal(0, stage.SectionX(0), 0.01);
        Assert.Null(stage.Section(0).RenderTransform);
    }

    [AvaloniaFact]
    public void A_Swipe_Left_At_The_End_Loops_To_The_First_Section()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));
        const double release = 250;
        Swipe(stage, clock, release);
        var from = 1215 - stage.SectionX(3);
        Assert.Equal(1415, from, 0.01);
        var held = stage.HeaderX;
        Assert.Equal(HeaderLeft - from * HeaderRatio, held, 0.001);         // the header kept pace until the release
        var length = stage.Hub.SectionsLength;
        var header = new List<double> { held };
        for (var k = 1; k <= HeaderJumpFrame + FlyInFrames; k++)
        {
            clock.At(release + k * Frame);
            header.Add(stage.HeaderX);
            if (k < LoopResetFrame)
            {
                var offset = 1215 - stage.SectionX(3);
                var fraction = (offset - from) / (length - from);
                Assert.True(Math.Abs(fraction - LoopFractions[k - 1]) < 0.006, $"frame {k}: {fraction:F4}, the phone's {LoopFractions[k - 1]:F4}");
                Assert.Equal(offset - length, -stage.SectionX(0), 0.01);    // the first comes in after the last
            }
            else if (k == LoopResetFrame)
            {
                Assert.Equal(0, stage.SectionX(0), 0.01);                    // the offset is 0 again
                Assert.Equal(SectionWidth, stage.SectionX(1), 0.01);
                Assert.True(stage.Hub.Motion.IsLooping);                     // the header still easing in
            }
        }

        // The header: held, flown out, then a jump right and an ExponentialEase of 7 over 420 ms back to its place.
        for (var k = 1; k <= HeaderHoldFrames; k++)
            Assert.Equal(held, header[k], 0.001);
        for (var k = HeaderHoldFrames + 1; k < HeaderJumpFrame; k++)
            Assert.Equal(held - (k - HeaderHoldFrames) * HeaderFlyOutPerFrame, header[k], 0.05);
        Assert.InRange(header[HeaderJumpFrame], HeaderJumpMin, HeaderJumpMax);
        for (var k = HeaderJumpFrame + 1; k < HeaderJumpFrame + 4; k++)
            Assert.InRange((header[k] - HeaderLeft) / (header[k - 1] - HeaderLeft), FlyInRatioMin, FlyInRatioMax);
        Assert.InRange(header[HeaderJumpFrame + FlyInFrames - 1] - HeaderLeft, 0.01, 0.2);   // the phone's 0.09
        Assert.Equal(HeaderLeft, header[HeaderJumpFrame + FlyInFrames], 0.0001);
        Assert.False(stage.Hub.Motion.IsMoving);
        Assert.Equal([stage.Section(0), stage.Section(1)], stage.Hub.SectionsInView);
    }

    [AvaloniaFact]
    public void A_Swipe_Right_At_The_End_Goes_Back_One_And_A_Short_Drag_Past_It_Comes_Back()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));
        Swipe(stage, clock, 250, left: false);
        clock.At(1100);
        Assert.Equal(810, stage.Offset, 0.0001);

        stage.Hub.ScrollToSection(stage.Section(3));
        SlowDrag(stage, clock, 2000, px: 90);                                // under half the last section, slowly
        clock.At(2800);
        Assert.Equal(1215, stage.Offset, 0.0001);
    }

    [AvaloniaFact]
    public void A_Press_During_The_Loop_Ends_It_At_The_First_Section()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Hub.ScrollToSection(stage.Section(3));
        Swipe(stage, clock, 250);
        clock.At(250 + 3 * Frame);
        var touch = stage.Window.TouchBegin(stage.At(200, 440), RawInputModifiers.None);
        clock.At(400);
        stage.Window.TouchMove(touch, stage.At(180, 440), RawInputModifiers.None);
        Assert.False(stage.Hub.Motion.IsLooping);
        Assert.Equal(20, stage.Offset, 0.01);
        Assert.Equal(HeaderLeft - 20 * HeaderRatio, stage.HeaderX, 0.001);
        stage.Window.TouchEnd(touch, stage.At(180, 440), RawInputModifiers.None);
    }

    // ---- The background (#137) ----

    private static Bitmap Image(int width, int height) => new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96));

    [AvaloniaFact]
    public void The_Background_Image_Is_Its_Pixel_Width_By_The_Windows_Height()
    {
        foreach (var (w, h) in new[] { (1600, 800), (900, 400) })
        {
            var hub = Stage.Make(4);
            hub.Background = new ImageBrush(Image(w, h));
            using var stage = new Stage(hub: hub);
            var background = stage.Part("PART_Background");
            Assert.Equal(w, background.Bounds.Width, 0.001);
            Assert.Equal(ViewHeight, background.Bounds.Height, 0.001);       // the window's, not the Hub's 740
            Assert.Equal(w, stage.Part("PART_MainBackground").Bounds.Width, 0.001);
            Assert.True(stage.Part("PART_WrapBackground").IsVisible);
        }
    }

    [AvaloniaFact]
    public void The_Background_Moves_At_Its_Rate_Then_On_With_The_Sections()
    {
        foreach (var (w, readings, rate) in new[] { (1600, Background1600, Rate1600), (900, Background900, Rate900) })
        {
            var hub = Stage.Make(4);
            hub.Background = new ImageBrush(Image(w, 800));
            using var stage = new Stage(hub: hub);
            Assert.Equal(rate, (w - 450.0) / (1620 - 450), 6);
            hub.Motion.BeginDrag();
            foreach (var (offset, left) in readings)
            {
                hub.Motion.Drag(offset);
                Assert.Equal(left, stage.BackgroundX, 0.005);                // the logs' 3 places
            }

            // At L, where the first section is back at its place, the copy is where the image is at 0.
            hub.Motion.Drag(1620);
            Assert.Equal(0, stage.WrapX, 0.001);
            hub.Motion.Release(0);
        }
    }

    [AvaloniaFact]
    public void The_Backgrounds_Copy_Goes_From_Left_To_Right_Half_Way()
    {
        var hub = Stage.Make(4);
        hub.Background = new ImageBrush(Image(1600, 800));
        using var stage = new Stage(hub: hub);
        Assert.Equal(-1599, stage.WrapX - stage.BackgroundX, 0.001);
        hub.Motion.BeginDrag();
        hub.Motion.Drag(WrapFlipAbove);
        Assert.Equal(-1599, stage.WrapX - stage.BackgroundX, 0.001);
        hub.Motion.Drag(WrapFlipBelow);
        Assert.Equal(1599, stage.WrapX - stage.BackgroundX, 0.001);
        hub.Motion.Release(0);
    }

    [AvaloniaFact]
    public void The_Background_Is_Hidden_Until_The_Image_Is_There()
    {
        var brush = new ImageBrush();
        var hub = Stage.Make(2);
        hub.Background = brush;
        using var stage = new Stage(hub: hub);
        Assert.Contains(":backgroundhidden", hub.Classes);
        Assert.Equal(0, stage.Part("PART_Background").Opacity);
        brush.Source = Image(900, 400);
        Assert.DoesNotContain(":backgroundhidden", hub.Classes);
        Assert.Contains(":backgroundshowing", hub.Classes);             // straight there, as the phone (#137)
        Assert.Equal(1, stage.Part("PART_Background").Opacity);
        stage.Window.UpdateLayout();
        Assert.Equal(900, stage.Part("PART_Background").Bounds.Width, 0.001);
    }

    // The phone clips the background to a rectangle its code sets; ours clips at the Hub's edge (decided; PLAN.md), so an
    // image as tall as the window draws nothing below a shorter Hub.
    [AvaloniaFact]
    public void The_Background_Is_Clipped_At_The_Hubs_Edge()
    {
        var hub = Stage.Make(2);
        var image = new WriteableBitmap(new PixelSize(900, 800), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
        using (var buffer = image.Lock())
        {
            var row = Enumerable.Repeat(unchecked((int)0xFFFFFFFFu), 900).ToArray();
            for (var y = 0; y < 800; y++)
                System.Runtime.InteropServices.Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, 900);
        }

        hub.Background = new ImageBrush(image) { Stretch = Stretch.Fill };
        using var stage = new Stage(hub: hub, below: 340);
        Assert.Equal(400, hub.Bounds.Height, 0.001);
        Assert.Equal(ViewHeight, stage.Part("PART_Background").Bounds.Height, 0.001);
        using var frame = stage.Window.CaptureRenderedFrame()!;
        var inside = stage.At(200, 390);
        var below = stage.At(200, 420);
        Assert.Equal(0xFFFFFFFFu, Frames.PixelAt(frame, (int)(inside.X * 2.4), (int)(inside.Y * 2.4)).ToUInt32());
        Assert.NotEqual(0xFFFFFFFFu, Frames.PixelAt(frame, (int)(below.X * 2.4), (int)(below.Y * 2.4)).ToUInt32());
    }

    [AvaloniaFact]
    public void Any_Image_Brush_Is_Drawn_As_The_Phones_Image()
    {
        var hub = Stage.Make(4);
        hub.Background = (IBrush)new ImageBrush(Image(900, 400)).ToImmutable();               // not an ImageBrush
        using var stage = new Stage(hub: hub);
        Assert.Equal(900, stage.Part("PART_Background").Bounds.Width, 0.001);
        Assert.True(stage.Part("PART_WrapBackground").IsVisible);
    }

    [AvaloniaFact]
    public void The_Hub_Hears_Its_Brush_Only_In_A_Window()
    {
        var brush = new ImageBrush();
        var hub = Stage.Make(2);
        hub.Background = brush;
        using var stage = new Stage(hub: hub);
        var grid = (Grid)hub.Parent!;
        grid.Children.Remove(hub);
        brush.Source = Image(900, 400);
        Assert.Contains(":backgroundhidden", hub.Classes);                   // not listening out of the window
        grid.Children.Add(hub);
        Assert.Contains(":backgroundshowing", hub.Classes);                  // up to date when back
        stage.Place();
        Assert.Contains(":backgroundimagefadingin", hub.Classes);           // and placed again, fading in
        brush.Source = null;
        Assert.Contains(":backgroundhidden", hub.Classes);
        Assert.DoesNotContain(":backgroundimagefadingin", hub.Classes);
    }

    // Not read on the phone: a brush that isn't an image fills the Hub and doesn't move (decided; PLAN.md).
    [AvaloniaFact]
    public void A_Brush_That_Isnt_An_Image_Fills_The_Hub_And_Stays()
    {
        var hub = Stage.Make(4);
        hub.Background = Brushes.Transparent;
        using var stage = new Stage(hub: hub);
        var background = stage.Part("PART_Background");
        Assert.Equal(450, background.Bounds.Width, 0.001);
        Assert.Equal(ProbeHeight, background.Bounds.Height, 0.001);
        Assert.False(stage.Part("PART_WrapBackground").IsVisible);
        stage.Hub.ScrollToSection(stage.Section(2));
        Assert.Equal(0, stage.BackgroundX, 0.001);
        hub.Height = 500;
        stage.Window.UpdateLayout();
        Assert.Equal(500, background.Bounds.Height, 0.001);                  // it follows the Hub's height
    }

    [AvaloniaFact]
    public void An_Image_Narrower_Than_The_View_Fills_It_And_Stays()
    {
        foreach (var stretch in new[] { Stretch.Fill, Stretch.None })
        {
            var hub = Stage.Make(4);
            hub.Background = new ImageBrush(Image(300, 400)) { Stretch = stretch };
            using var stage = new Stage(hub: hub);
            var background = stage.Part("PART_Background");
            Assert.Equal(450, background.Bounds.Width, 0.001);               // the view's width, not the image's 300
            Assert.Equal(ViewHeight, background.Bounds.Height, 0.001);       // by the window's height
            Assert.Equal(450, stage.Part("PART_MainBackground").Bounds.Width, 0.001);
            Assert.False(stage.Part("PART_WrapBackground").IsVisible);       // never on the phone's screen
            hub.Motion.BeginDrag();
            foreach (var offset in new[] { -100.0, 0, 585, 1215.417, 1500 })
            {
                hub.Motion.Drag(offset);
                Assert.Equal(0, stage.BackgroundX, 0.001);                   // at 0 at every offset
            }

            hub.Motion.Release(0);
        }
    }

    [AvaloniaFact]
    public void Placed_With_A_Brush_The_Background_Fades_In_Then_Shows()
    {
        using var clock = PhoneClock();
        foreach (var brush in new IBrush?[] { new ImageBrush(Image(1600, 800)), Brushes.Purple, null })
        {
            var hub = Stage.Make(2);
            hub.Background = brush;
            using var stage = new Stage(hub: hub);
            var start = clock.Now.TotalMilliseconds;
            if (brush is null)
            {
                Assert.Contains(":backgroundshowing", hub.Classes);          // no brush: nothing to fade in
                continue;
            }

            Assert.Contains(":backgroundimagefadingin", hub.Classes);
            Assert.DoesNotContain(":backgroundshowing", hub.Classes);
            Assert.DoesNotContain(":backgroundhidden", hub.Classes);
            Assert.Equal(1, stage.Part("PART_Background").Opacity);          // the phone's reads 1 throughout
            clock.At(start + FadingInMin);
            Assert.Contains(":backgroundimagefadingin", hub.Classes);
            clock.At(start + FadingInMax);
            Assert.Contains(":backgroundshowing", hub.Classes);
            Assert.DoesNotContain(":backgroundimagefadingin", hub.Classes);
        }
    }

    // ---- The entrance (#148) ----

    [AvaloniaFact]
    public void Placed_The_Hub_Turns_In_On_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage(entrance: true);
        var root = stage.Part("PART_HubRoot");
        var entrance = stage.Hub.Entrance;
        Assert.True(entrance.IsRunning);
        var start = clock.Now.TotalMilliseconds;
        for (var k = 0; k < EntranceAngles.Length; k++)
        {
            clock.At(start + k * Frame);
            Assert.Equal(EntranceAngles[k], entrance.Angle, 0.001);
            var matrix = Assert.IsType<MatrixTransform>(root.RenderTransform).Matrix;
            Assert.Equal(HubEntrance.ToMatrix(entrance.Angle, root.Bounds.Size), matrix);
        }

        clock.At(start + (EntranceTicks - 1) * Frame);
        Assert.True(entrance.IsRunning);
        Assert.InRange(entrance.Angle, -0.1, 0);                             // the ease's last tick, -0.09
        clock.At(start + EntranceTicks * Frame);
        Assert.False(entrance.IsRunning);
        Assert.Null(root.RenderTransform);                                    // at rest, nothing left behind
        Assert.Equal(0, stage.SectionX(0), 0.001);
        Assert.Equal(HeaderLeft, stage.HeaderX, 0.001);
    }

    [AvaloniaFact]
    public void The_Turn_Is_PlaneProjections_About_An_Axis_Half_The_Width_To_The_Left()
    {
        var size = new Size(450, 740);
        Assert.True(HubEntrance.ToMatrix(0, size).IsIdentity);
        foreach (var degrees in new[] { -120.0, -53.716, -10, 30 })
        {
            var matrix = HubEntrance.ToMatrix(degrees, size);
            foreach (var point in new[] { new Point(0, 0), new Point(450, 0), new Point(200, 300), new Point(450, 740) })
            {
                // Turned about x = −225 and seen from 999 px in front of the root's centre.
                var r = degrees * Math.PI / 180;
                double u = point.X + 225, x = -225 + u * Math.Cos(r), z = u * Math.Sin(r), scale = 999 / (999 - z);
                var expected = new Point(225 + (x - 225) * scale, 370 + (point.Y - 370) * scale);
                var drawn = point.Transform(matrix);
                Assert.Equal(expected.X, drawn.X, 1e-3);
                Assert.Equal(expected.Y, drawn.Y, 1e-3);
            }
        }

        // On the first frame the Hub is turned away and off the screen to the left; its right side goes back as it turns.
        var first = HubEntrance.ToMatrix(HubEntrance.From, size);
        Assert.True(new Point(0, 0).Transform(first).X < 0 && new Point(450, 740).Transform(first).X < 0);
        var turning = HubEntrance.ToMatrix(-30, size);
        var right = new Point(450, 0).Transform(turning);
        Assert.True(right.Y > 0);                                            // smaller: further away
    }

    [AvaloniaFact]
    public void Placed_Again_It_Turns_In_Again_And_Taken_Away_It_Rests()
    {
        using var clock = PhoneClock();
        using var stage = new Stage(entrance: true);
        var root = stage.Part("PART_HubRoot");
        var grid = (Grid)stage.Hub.Parent!;
        clock.At(clock.Now.TotalMilliseconds + 5 * Frame);
        grid.Children.Remove(stage.Hub);                                     // away mid-turn
        Assert.False(stage.Hub.Entrance.IsRunning);
        Assert.Null(root.RenderTransform);
        grid.Children.Add(stage.Hub);
        stage.Place();
        Assert.True(stage.Hub.Entrance.IsRunning);
        Assert.Equal(HubEntrance.From, stage.Hub.Entrance.Angle, 0.001);

        // The turn is drawn over the root's own transform, which shows again after it.
        var own = new ScaleTransform(0.5, 0.5);
        root.RenderTransform = own;
        Assert.IsType<MatrixTransform>(root.RenderTransform);
        clock.At(clock.Now.TotalMilliseconds + 1000);
        Assert.False(stage.Hub.Entrance.IsRunning);
        Assert.Same(own, root.RenderTransform);
    }

    [AvaloniaFact]
    public void An_Image_Gone_And_Back_While_Fading_In_Goes_Straight_To_Showing()
    {
        using var clock = PhoneClock();
        var brush = new ImageBrush(Image(1600, 800));
        var hub = Stage.Make(2);
        hub.Background = brush;
        using var stage = new Stage(hub: hub);
        Assert.Contains(":backgroundimagefadingin", hub.Classes);
        brush.Source = null;
        Assert.Contains(":backgroundhidden", hub.Classes);
        brush.Source = Image(900, 400);                                       // on the screen now: straight there (#137)
        Assert.Contains(":backgroundshowing", hub.Classes);
        Assert.DoesNotContain(":backgroundimagefadingin", hub.Classes);
    }

    // ---- In view ----

    [AvaloniaFact]
    public void SectionsInView_Follows_The_Sections_And_Says_When_It_Changes()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var changes = new List<(string[] Added, string[] Removed)>();
        stage.Hub.SectionsInViewChanged += (_, e) => changes.Add((e.AddedSections.Select(s => (string)s.Header!).ToArray(), e.RemovedSections.Select(s => (string)s.Header!).ToArray()));
        Assert.Equal([stage.Section(0), stage.Section(1)], stage.Hub.SectionsInView);
        Swipe(stage, clock, 250);
        clock.At(1100);
        Assert.Equal([stage.Section(1), stage.Section(2)], stage.Hub.SectionsInView);
        Assert.Equal(["section 3"], changes[0].Added);                       // the third comes in first
        Assert.Empty(changes[0].Removed);
        Assert.Equal(["section 1"], changes[^1].Removed);
    }

    // ---- Who takes the finger ----

    [AvaloniaFact]
    public void A_Mouse_Drag_Doesnt_Pan()
    {
        using var stage = new Stage();
        stage.Window.MouseDown(stage.At(380, 440), MouseButton.Left);
        stage.Window.MouseMove(stage.At(200, 440));
        stage.Window.MouseUp(stage.At(200, 440), MouseButton.Left);
        Assert.Equal(0, stage.Offset, 0.0001);
    }

    [AvaloniaFact]
    public void A_Drag_Up_A_Sections_ScrollViewer_Scrolls_It_And_A_Swipe_Across_It_Pans()
    {
        using var clock = PhoneClock();
        var hub = Stage.Make(3);
        var lines = new StackPanel();
        for (var i = 0; i < 60; i++) lines.Children.Add(new TextBlock { Text = "line " + i });
        var scroller = new ScrollViewer { Content = lines };
        hub.Sections[0].ContentTemplate = new FuncDataTemplate<object?>((_, _) => scroller);
        using var stage = new Stage(hub: hub);
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(200, 500), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(100 + i * 10);
            stage.Window.TouchMove(touch, stage.At(200, 500 - i * 15), RawInputModifiers.None);
        }

        stage.Window.TouchEnd(touch, stage.At(200, 350), RawInputModifiers.None);
        Assert.True(scroller.Offset.Y > 50, $"the list scrolled {scroller.Offset.Y}");
        Assert.Equal(0, stage.Offset, 0.0001);

        Swipe(stage, clock, 1000, from: 300);
        Assert.True(stage.Offset > 100);

        // A finger on the moving sections holds them, and the Hub hears its release even when it then goes up the list.
        clock.At(1000 + 3 * Frame);
        touch = stage.Window.TouchBegin(stage.At(200, 500), RawInputModifiers.None);
        for (var i = 1; i <= 5; i++)
        {
            clock.At(1100 + i * 10);
            stage.Window.TouchMove(touch, stage.At(200, 500 - i * 15), RawInputModifiers.None);
        }

        stage.Window.TouchEnd(touch, stage.At(200, 425), RawInputModifiers.None);
        Assert.False(stage.Hub.Motion.IsDragging);
        clock.At(3000);
        Assert.Contains(stage.Offset, stage.Hub.SectionStarts);
    }

    [AvaloniaFact]
    public void A_Drag_More_Up_Than_Across_Doesnt_Pan()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        clock.At(100);
        var touch = stage.Window.TouchBegin(stage.At(300, 500), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(100 + i * 10);
            stage.Window.TouchMove(touch, stage.At(300 - i * 2, 500 - i * 15), RawInputModifiers.None);
            Assert.Equal(0, stage.Offset, 0.0001);                           // 20 across, 150 up: not the Hub's
        }

        stage.Window.TouchEnd(touch, stage.At(280, 350), RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void One_Section_Doesnt_Pan()
    {
        using var clock = PhoneClock();
        using var stage = new Stage(1);
        Swipe(stage, clock, 250);
        Assert.Equal(0, stage.SectionX(0), 0.0001);
        Assert.False(stage.Hub.CanPan);
    }
}
