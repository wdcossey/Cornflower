using Cornflower.Controls;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.LoopingSelectorReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 LoopingSelector, the pickers' wheel: Avalonia's <see cref="DateTimePickerPanel"/> in the keyed
/// <c>Wp81LoopingSelectorTheme</c>, driven by <c>PhoneLoopingSelector</c>, against the phone's styles and #149's readings
/// (<see cref="LoopingSelectorReadings"/>). docs/controls/LoopingSelector has the plan and the comparison.
/// </summary>
public class LoopingSelectorTests
{
    /// <summary>The DatePickerFlyout's three wheels as the probe opened them: 15 July 2014, at the phone's places.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? variant = null, bool themed = true, bool hosted = false)
        {
            Month = Make(DateTimePickerPanelType.Month, "MM\nMMMM", 1, 12, 7, loop: true, themed);
            Day = Make(DateTimePickerPanelType.Day, "dd\ndddd", 1, 31, 15, loop: true, themed);
            Year = Make(DateTimePickerPanelType.Year, "yyyy", 1976, 2076, 2014, loop: false, themed);
            Canvas = new Canvas();
            var panels = new[] { Month, Day, Year };
            for (var i = 0; i < 3; i++)
            {
                Control column = panels[i];
                if (hosted)
                    column = new ScrollViewer { Content = panels[i], VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden };
                column.Width = ColumnWidth;
                column.Height = ColumnHeight;
                Canvas.SetLeft(column, ColumnX[i]);
                Canvas.SetTop(column, ColumnTop);
                Canvas.Children.Add(column);
            }

            _scene = new Scene(Canvas, variant);
            Settle();
        }

        public Canvas Canvas { get; }

        public DateTimePickerPanel Month { get; }

        public DateTimePickerPanel Day { get; }

        public DateTimePickerPanel Year { get; }

        public Window Window => _scene.Window;

        public static DateTimePickerPanel Make(DateTimePickerPanelType type, string format, int min, int max, int selected,
            bool loop, bool themed = true)
        {
            var panel = new DateTimePickerPanel { PanelType = type, ItemFormat = format, ShouldLoop = loop };
            panel.MaximumValue = max;
            panel.MinimumValue = min;
            panel.SelectedValue = selected;
            FormatDate.SetValue(panel, new DateTime(2014, 7, 15));
            if (themed)
                panel.Bind(StyledElement.ThemeProperty, panel.GetResourceObservable("Wp81LoopingSelectorTheme"));
            return panel;
        }

        /// <summary>Avalonia's panel formats its days in the month of this date, which its presenter sets: internal.</summary>
        private static readonly PropertyInfo FormatDate =
            typeof(DateTimePickerPanel).GetProperty("FormatDate", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public void Settle()
        {
            Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }

        /// <summary>The panel's items, top to bottom as drawn.</summary>
        public static List<ListBoxItem> Items(DateTimePickerPanel panel) =>
            panel.Children.OfType<ListBoxItem>().OrderBy(item => item.Bounds.Top).ToList();

        /// <summary>The item holding <paramref name="value"/>.</summary>
        public static ListBoxItem Item(DateTimePickerPanel panel, int value) =>
            panel.Children.OfType<ListBoxItem>().Single(item => item.Tag is int tag && tag == value);

        public static Control Part(ListBoxItem item, string name) =>
            item.GetTemplateDescendants().OfType<Control>().Single(c => c.Name == name);

        /// <summary>A point in the window, <paramref name="y"/> down <paramref name="panel"/> and across its middle.</summary>
        public Point At(DateTimePickerPanel panel, double y) =>
            panel.TranslatePoint(new Point(ColumnWidth / 2, y), Window)!.Value;

        /// <summary>Where the item holding <paramref name="value"/> is drawn, from the window's top.</summary>
        public double TopOf(DateTimePickerPanel panel, int value) =>
            Item(panel, value).TranslatePoint(default, Window)!.Value.Y;

        public void Dispose() => _scene.Dispose();
    }

    private static TestClock PhoneClock() => new() { FrameInterval = TimeSpan.FromMilliseconds(Frame), FramesOnGrid = true };

    private static double Expansion(DateTimePickerPanel panel) => PhoneLoopingSelector.GetExpansion(panel);

    /// <summary>
    /// A finger down at <paramref name="y"/> on <paramref name="panel"/>, moved <paramref name="travel"/> up (negative:
    /// down) in steps of <paramref name="step"/> px 10 ms apart from <paramref name="start"/> ms, held still
    /// <paramref name="holdMs"/>, and let go; the clock left at the release.
    /// </summary>
    private static double Drag(Stage stage, TestClock clock, DateTimePickerPanel panel, double travel, double start = 0,
        double y = 350, double step = 2, double holdMs = 400)
    {
        clock.At(start);
        var touch = stage.Window.TouchBegin(stage.At(panel, y), RawInputModifiers.None);
        var moves = (int)Math.Ceiling(Math.Abs(travel) / step);
        var sign = Math.Sign(travel);
        for (var i = 1; i <= moves; i++)
        {
            clock.At(start + i * 10);
            stage.Window.TouchMove(touch, stage.At(panel, y - sign * Math.Min(i * step, Math.Abs(travel))), RawInputModifiers.None);
        }

        var release = start + moves * 10 + holdMs;
        clock.At(release);
        stage.Window.TouchEnd(touch, stage.At(panel, y - travel), RawInputModifiers.None);
        return release;
    }

    private static void Tap(Stage stage, TestClock clock, DateTimePickerPanel panel, double y, double at)
    {
        clock.At(at);
        var touch = stage.Window.TouchBegin(stage.At(panel, y), RawInputModifiers.None);
        clock.At(at + 78);
        stage.Window.TouchEnd(touch, stage.At(panel, y), RawInputModifiers.None);
    }

    // ---- The decision: Avalonia's panel, driven ----

    [AvaloniaFact]
    public void Avalonias_Date_And_Time_Presenters_Need_A_DateTimePickerPanel_For_Each_Column()
    {
        // So a wheel of our own couldn't stand in a column of either presenter: the wheel is the panel, driven.
        var date = typeof(DatePickerPresenter).GetCustomAttributes<TemplatePartAttribute>()
            .Where(part => part.Name.EndsWith("Selector", StringComparison.Ordinal)).ToList();
        var time = typeof(TimePickerPresenter).GetCustomAttributes<TemplatePartAttribute>()
            .Where(part => part.Name.EndsWith("Selector", StringComparison.Ordinal)).ToList();
        Assert.Equal(["PART_DaySelector", "PART_MonthSelector", "PART_YearSelector"], date.Select(part => part.Name).Order());
        Assert.All(date.Concat(time), part => Assert.Equal(typeof(DateTimePickerPanel), part.Type));
        Assert.All(date, part => Assert.True(part.IsRequired));
        Assert.True(time.Single(part => part.Name == "PART_HourSelector").IsRequired);
        Assert.True(time.Single(part => part.Name == "PART_MinuteSelector").IsRequired);
    }

    [AvaloniaFact]
    public void In_A_ScrollViewer_A_Panel_That_Doesnt_Loop_Cant_Reach_Its_Last_Items()
    {
        // The presenter clamps the offset to the extent less the viewport, and the panel's extent is its items alone:
        // so the themed wheel isn't hosted in a ScrollViewer, and moves the panel itself.
        using var stage = new Stage(themed: false, hosted: true);
        stage.Year.SelectedValue = 2076;
        stage.Settle();
        Assert.True(stage.Year.SelectedValue < 2076);
    }

    [AvaloniaFact]
    public void Avalonias_Panel_Selects_On_A_Tap_And_The_Wheel_Doesnt()
    {
        using var clock = PhoneClock();
        using (var plain = new Stage(themed: false))
        {
            Tap(plain, clock, plain.Year, ColumnHeight / 2 + 2 * plain.Year.ItemHeight, 0);
            Assert.Equal(2016, plain.Year.SelectedValue);
        }

        using var stage = new Stage();
        var offset = stage.Year.Offset;
        Tap(stage, clock, stage.Year, SelectedTop - ColumnTop + 2 * Pitch + 60, 1000);
        clock.At(2000);
        Assert.Equal(2014, stage.Year.SelectedValue);
        Assert.Equal(offset, stage.Year.Offset);
    }

    // ---- The layout and the look ----

    [AvaloniaFact]
    public void Each_Item_Has_The_Wheels_Item_Theme_And_The_Items_Are_The_Phones_Pitch_Apart()
    {
        using var stage = new Stage();
        var theme = stage.Month.FindResource("Wp81LoopingSelectorItemTheme");
        Assert.All(stage.Month.Children, child => Assert.Same(theme, child.Theme));
        var tops = Stage.Items(stage.Month).Select(item => item.TranslatePoint(default, stage.Window)!.Value.Y).ToList();
        for (var i = 1; i < tops.Count; i++)
            Assert.Equal(Pitch, tops[i] - tops[i - 1], 3);
        Assert.Equal(SelectedTop, stage.TopOf(stage.Month, 7), 1);
        Assert.Equal(SelectedTop, stage.TopOf(stage.Day, 15), 1);
        Assert.Equal(SelectedTop, stage.TopOf(stage.Year, 2014), 1);
    }

    [AvaloniaFact]
    public void The_Selected_Tile_Is_Where_The_Capture_Draws_It()
    {
        using var stage = new Stage();
        var root = Stage.Part(Stage.Item(stage.Month, 7), "Root");
        var bounds = new Rect(root.Bounds.Size).TransformToAABB(root.TransformToVisual(stage.Window)!.Value);
        Assert.Equal(TileLeft, bounds.Left, 0.5);
        Assert.Equal(TileTop, bounds.Top, 0.5);
        Assert.Equal(TileSize, bounds.Width, 0.5);
        Assert.Equal(TileSize, bounds.Height, 0.5);
    }

    private static IBrush? Background(ListBoxItem item) => ((Panel)Stage.Part(item, "Root")).Background;

    private static double RootOpacity(ListBoxItem item) => Stage.Part(item, "Root").Opacity;

    private static IBrush? BorderBrush(ListBoxItem item) => ((Border)Stage.Part(item, "Border")).BorderBrush;

    private static IBrush? Foreground(ListBoxItem item) =>
        ((Avalonia.Controls.Presenters.ContentPresenter)Stage.Part(item, "PART_ContentPresenter")).Foreground;

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    [AvaloniaTheory]
    [InlineData("dark", "#73FFFFFF")]
    [InlineData("light", "#40000000")]
    public void At_Rest_Only_The_Selected_Item_Shows_In_The_Accent_And_The_Active_Wheel_Shows_Every_Item(string variant, string foreground)
    {
        using var clock = PhoneClock();
        using var stage = new Stage(variant == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
        var selected = Stage.Item(stage.Month, 7);
        var other = Stage.Item(stage.Month, 8);
        Assert.Equal(1, RootOpacity(selected));
        Assert.Equal(Color.Parse("#FF3E65FF"), ColorOf(Background(selected)));
        Assert.Equal(Colors.Transparent, ColorOf(BorderBrush(selected)));
        Assert.Equal(Colors.White, ColorOf(Foreground(selected)));
        Assert.Equal(0, RootOpacity(other));

        // Expanded: every item shows, bordered 2.5 in the wheel's foreground, with no background.
        Tap(stage, clock, stage.Month, 350, 0);
        stage.Settle();
        Assert.Equal(1, RootOpacity(other));
        Assert.Equal(Color.Parse(foreground), ColorOf(BorderBrush(other)));
        Assert.Equal(Color.Parse(foreground), ColorOf(Foreground(other)));
        Assert.Equal(new Thickness(2.5), ((Border)Stage.Part(other, "Border")).BorderThickness);
        Assert.Equal(Colors.Transparent, ColorOf(Background(other)));
        Assert.Equal(Color.Parse("#FF3E65FF"), ColorOf(Background(selected)));
    }

    [AvaloniaFact]
    public void The_Items_Show_Their_Formats_First_Line_Large_And_The_Rest_Small()
    {
        using var stage = new Stage();
        var texts = Stage.Item(stage.Day, 15).GetVisualDescendants().OfType<TextBlock>().ToList();
        Assert.Equal(["15", "Tuesday"], texts.Select(t => t.Text));
        Assert.Equal([45d, 17d], texts.Select(t => t.FontSize));
        Assert.All(texts, t => Assert.Equal(FontWeight.SemiBold, t.FontWeight));
        Assert.Equal(-24 * 45 / 1000d, texts[0].LetterSpacing, 6);
        var year = Stage.Item(stage.Year, 2014).GetVisualDescendants().OfType<TextBlock>().ToList();
        Assert.Equal(["2014", ""], year.Select(t => t.Text));

        // At the left of the tile: the phone's StackPanel margin, 5, and the line's own 0.8, inside the border.
        var primary = texts[0].TranslatePoint(default, Stage.Part(Stage.Item(stage.Day, 15), "Border"))!.Value;
        Assert.Equal(2.5 + 5 + 0.8, primary.X, 3);

        // At the bottom: the second line ends the phone's 5 and 0.8 inside the border, whatever the panel's Center.
        var root = Stage.Part(Stage.Item(stage.Day, 15), "Root");
        var bottom = texts[1].TranslatePoint(new Point(0, texts[1].Bounds.Height), root)!.Value.Y;
        Assert.Equal(113 - 2.5 - 5 + 0.8, bottom, 3);
    }

    // ---- The tap and the active wheel ----

    [AvaloniaFact]
    public void A_Tap_On_An_Item_Selects_Nothing_And_Only_Makes_The_Wheel_Active()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var offset = stage.Year.Offset;
        foreach (var (y, at) in new[] { (350 + 2 * Pitch, 0d), (350 - 2 * Pitch, 1000d), (350d, 2000d) })
        {
            Tap(stage, clock, stage.Year, y, at);
            clock.At(at + 500);
            Assert.Equal(2014, stage.Year.SelectedValue);
            Assert.Equal(offset, stage.Year.Offset);
        }

        // The first time a wheel expands it has no state to come from: it is there at once.
        Assert.Equal(1, Expansion(stage.Year));
        Assert.Equal(0, Expansion(stage.Month));
    }

    [AvaloniaFact]
    public void Another_Wheel_Made_Active_Fades_The_Last_One_Out_In_A_Straight_Line()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Tap(stage, clock, stage.Month, 350, 0);
        Tap(stage, clock, stage.Day, 350, 1000);
        Assert.Equal(1, Expansion(stage.Day));
        Assert.True(PhoneLoopingSelector.Of(stage.Day)!.IsActive);
        Assert.False(PhoneLoopingSelector.Of(stage.Month)!.IsActive);
        // A straight line: the same step each frame, the phone's 0.0469.
        var start = clock.Now.TotalMilliseconds;
        var last = double.NaN;
        for (var n = 1; n <= 10; n++)
        {
            clock.At(start + n * Frame + 1);
            var value = Expansion(stage.Month);
            if (n > 1)
                Assert.Equal(FadePerFrame, last - value, 0.001);
            last = value;
        }

        clock.At(start + 400);
        Assert.Equal(0, Expansion(stage.Month));

        // Back: it fades in from where it is, over the same 0.33 s.
        Tap(stage, clock, stage.Month, 350, 2000);
        start = clock.Now.TotalMilliseconds;
        clock.At(start + 5 * Frame + 1);
        Assert.InRange(Expansion(stage.Month), 4 * FadePerFrame, 6 * FadePerFrame);
        Assert.True(Expansion(stage.Day) < 1);
        clock.At(start + 400);
        Assert.Equal(1, Expansion(stage.Month));
        Assert.Equal(0, Expansion(stage.Day));
    }

    // ---- The drag and the release ----

    [AvaloniaFact]
    public void A_Drag_Moves_The_Items_With_The_Finger_And_Hides_The_Selected_Look_Until_Rest()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var top = stage.TopOf(stage.Day, 15);
        clock.At(0);
        var touch = stage.Window.TouchBegin(stage.At(stage.Day, 350), RawInputModifiers.None);
        clock.At(10);
        stage.Window.TouchMove(touch, stage.At(stage.Day, 320), RawInputModifiers.None);
        stage.Settle();
        var after = stage.TopOf(stage.Day, 15);
        clock.At(20);
        stage.Window.TouchMove(touch, stage.At(stage.Day, 280), RawInputModifiers.None);
        stage.Settle();
        Assert.Equal(-40, stage.TopOf(stage.Day, 15) - after, 3);
        Assert.True(after <= top);
        Assert.Equal(1, Expansion(stage.Day));
        Assert.Equal(1, RootOpacity(Stage.Item(stage.Day, 15)));
        Assert.Equal(Colors.Transparent, ColorOf(Background(Stage.Item(stage.Day, 15))));

        clock.At(500);
        stage.Window.TouchEnd(touch, stage.At(stage.Day, 280), RawInputModifiers.None);
        clock.At(1500);
        stage.Settle();
        Assert.Equal(15, stage.Day.SelectedValue);
        Assert.Equal(Color.Parse("#FF3E65FF"), ColorOf(Background(Stage.Item(stage.Day, 15))));
    }

    [AvaloniaFact]
    public void Let_Go_Short_Of_Half_An_Item_It_Drifts_On_Then_Comes_Back()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var rest = stage.Month.Offset.Y;
        var travel = 0.43 * Pitch + PhoneLoopingSelector.WheelGestureRecognizer.StartDistance;
        var release = Drag(stage, clock, stage.Month, travel);
        var past = stage.Month.Offset.Y - rest;
        Assert.InRange(past / Pitch, 0.4, 0.46);

        var most = past;
        for (var t = Frame; t <= 600; t += Frame)
        {
            clock.At(release + t);
            most = Math.Max(most, stage.Month.Offset.Y - rest);
        }

        Assert.InRange(most - past, DriftMin, DriftMax);
        Assert.Equal(rest, stage.Month.Offset.Y);
        Assert.Equal(7, stage.Month.SelectedValue);
        Assert.False(PhoneLoopingSelector.GetIsMoving(stage.Month));
    }

    [AvaloniaFact]
    public void The_Way_Back_Follows_The_Phones_Frames()
    {
        // From the phone's first inertial frame, 63.4 px past: within 4.5 px of each of its 32 frames (at most 4.3, rms 2.0).
        var motion = PhoneLoopingSelectorMotion.Release(63.4, 0, +1, Pitch)!;
        Assert.True(motion.IsBack);
        Assert.Equal(0, motion.Target);
        foreach (var (ms, offset) in BackFrames)
            Assert.Equal(offset, motion.OffsetAt(TimeSpan.FromMilliseconds(ms)), 4.5);
        Assert.InRange(motion.Duration.TotalMilliseconds, 470, 500);

        // The same either way: dragged down and let go still moving down a little, it drifts on down, then back.
        var down = PhoneLoopingSelectorMotion.Release(-0.3 * Pitch, -0.01, -1, Pitch)!;
        Assert.True(down.IsBack);
        Assert.True(down.OffsetAt(TimeSpan.FromMilliseconds(50)) < -0.3 * Pitch);
        var up = PhoneLoopingSelectorMotion.Release(0.3 * Pitch, 0.01, +1, Pitch)!;
        Assert.True(up.IsBack);
    }

    [AvaloniaFact]
    public void Let_Go_Past_Half_An_Item_It_Goes_On_To_The_Next_At_A_Falling_Speed()
    {
        // The phone's: 61.5 px short of the next item at its first inertial frame, then within 4 px of each frame.
        var motion = PhoneLoopingSelectorMotion.Release(Pitch - 61.5, 0, +1, Pitch)!;
        Assert.False(motion.IsBack);
        Assert.Equal(Pitch, motion.Target);
        foreach (var (ms, offset) in OnFrames)
            Assert.Equal(Pitch + offset, motion.OffsetAt(TimeSpan.FromMilliseconds(ms)), 4d);

        using var clock = PhoneClock();
        using var stage = new Stage();
        var rest = stage.Month.Offset.Y;
        var release = Drag(stage, clock, stage.Month, 0.6 * Pitch + PhoneLoopingSelector.WheelGestureRecognizer.StartDistance);
        clock.At(release + 1000);
        stage.Settle();
        Assert.Equal(8, stage.Month.SelectedValue);
        Assert.Equal(rest + Pitch, stage.Month.Offset.Y);        // in the same loop of the panel's extent
        Assert.Equal(SelectedTop, stage.TopOf(stage.Month, 8), 1);
    }

    [AvaloniaFact]
    public void A_Flick_Coasts_On_The_Law_To_An_Item()
    {
        // The phone's 14-item flick, from its first inertial frame: within 20 px of each frame (at most 19.3, rms 9.5).
        var coast = PhoneLoopingSelectorMotion.CoastTo(0, 1686.6)!;
        foreach (var (ms, offset) in FlickFrames)
            Assert.Equal(1686.6 - offset, coast.OffsetAt(TimeSpan.FromMilliseconds(ms)), 20d);
        Assert.Equal(1844, coast.Duration.TotalMilliseconds, 60d);

        // Where it stops is decided at the release, in whole items.
        var motion = PhoneLoopingSelectorMotion.Release(0, -2.736, -1, Pitch)!;
        Assert.Equal(0, Math.IEEERemainder(motion.Target, Pitch), 9);

        // Flicked up on the day wheel: it ends on an item, several on, which is the selection at rest.
        using var clock = PhoneClock();
        using var stage = new Stage();
        var rest = stage.Day.Offset.Y;
        var release = Drag(stage, clock, stage.Day, 150, y: 500, step: 15, holdMs: 0);
        var wheel = PhoneLoopingSelector.Of(stage.Day)!;
        Assert.True(wheel.IsCoasting);
        var items = (int)Math.Round((wheel.Motion!.Target - rest) / Pitch);
        Assert.InRange(items, 4, 12);
        clock.At(release + 3000);
        stage.Settle();
        Assert.Equal(15 + items, stage.Day.SelectedValue);
        Assert.False(PhoneLoopingSelector.GetIsMoving(stage.Day));
        Assert.Equal(SelectedTop, stage.TopOf(stage.Day, stage.Day.SelectedValue), 1);
    }

    [AvaloniaFact]
    public void Month_And_Day_Wrap_And_The_Year_Doesnt()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var rest = stage.Month.Offset.Y;

        // Down nine months from July: round past January to October.
        var release = Drag(stage, clock, stage.Month, -(9 * Pitch + PhoneLoopingSelector.WheelGestureRecognizer.StartDistance), step: 6);
        clock.At(release + 1500);
        Assert.Equal(10, stage.Month.SelectedValue);
        Assert.Equal(rest + 3 * Pitch, stage.Month.Offset.Y, 6);           // October, back in the middle loop

        // The year stops at its first.
        stage.Year.SelectedValue = 1978;
        stage.Settle();
        release = Drag(stage, clock, stage.Year, -5 * Pitch, start: release + 2000, step: 6);
        clock.At(release + 1500);
        Assert.Equal(1976, stage.Year.SelectedValue);
        Assert.Equal(0, stage.Year.Offset.Y);
    }

    [AvaloniaFact]
    public void A_Press_Catches_A_Coast_Within_A_Frame_And_The_Release_Goes_To_The_Nearer_Item()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var rest = stage.Day.Offset.Y;
        var release = Drag(stage, clock, stage.Day, 150, y: 500, step: 15, holdMs: 0);
        clock.At(release + 300);
        var touch = stage.Window.TouchBegin(stage.At(stage.Day, 350), RawInputModifiers.None);
        var held = stage.Day.Offset.Y;
        Assert.NotEqual(0, Math.IEEERemainder(held, Pitch), 3);
        clock.At(release + 300 + 592);
        Assert.Equal(held, stage.Day.Offset.Y);
        Assert.False(PhoneLoopingSelector.Of(stage.Day)!.IsCoasting);

        stage.Window.TouchEnd(touch, stage.At(stage.Day, 350), RawInputModifiers.None);
        clock.At(release + 300 + 592 + 1000);
        var items = (int)Math.Round((held - rest) / Pitch);
        Assert.Equal((15 - 1 + items) % 31 + 1, stage.Day.SelectedValue);
        Assert.False(PhoneLoopingSelector.GetIsMoving(stage.Day));
    }

    [AvaloniaFact]
    public void A_Notch_Of_The_Mouse_Wheel_Turns_It_One_Item()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Window.MouseWheel(stage.At(stage.Year, 350), new Vector(0, -1));
        Assert.Equal(1, Expansion(stage.Year));
        clock.At(1000);
        Assert.Equal(2015, stage.Year.SelectedValue);
        stage.Window.MouseWheel(stage.At(stage.Year, 350), new Vector(0, 1));
        clock.At(2000);
        Assert.Equal(2014, stage.Year.SelectedValue);

        // A finer wheel's small steps add up to a notch.
        for (var i = 0; i < 3; i++)
            stage.Window.MouseWheel(stage.At(stage.Year, 350), new Vector(0, -0.4));
        clock.At(3000);
        Assert.Equal(2015, stage.Year.SelectedValue);

        // At its end the scroll isn't the wheel's: it goes on to what holds it.
        stage.Year.SelectedValue = 2076;
        var outer = 0;
        stage.Canvas.PointerWheelChanged += (_, _) => outer++;
        for (var i = 0; i < 5; i++)
            stage.Window.MouseWheel(stage.At(stage.Year, 350), new Vector(0, -0.2));
        Assert.Equal(5, outer);
    }

    [AvaloniaFact]
    public void A_Press_Before_A_Move_Has_Gone_Anywhere_Still_Brings_It_To_Rest()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Window.MouseWheel(stage.At(stage.Year, 350), new Vector(0, -1));
        var touch = stage.Window.TouchBegin(stage.At(stage.Year, 350), RawInputModifiers.None);
        Assert.False(PhoneLoopingSelector.Of(stage.Year)!.IsCoasting);
        clock.At(78);
        stage.Window.TouchEnd(touch, stage.At(stage.Year, 350), RawInputModifiers.None);
        clock.At(1000);
        Assert.False(PhoneLoopingSelector.GetIsMoving(stage.Year));
        Assert.Equal(2014, stage.Year.SelectedValue);
    }

    [AvaloniaFact]
    public void Its_Range_Changed_Under_A_Move_It_Rests_Where_The_Panel_Is()
    {
        // As a DatePicker's presenter shortens the days when the month changes, while the day wheel still coasts.
        using var clock = PhoneClock();
        using var stage = new Stage();
        var release = Drag(stage, clock, stage.Day, 150, y: 500, step: 15, holdMs: 0);
        clock.At(release + 100);
        stage.Day.MaximumValue = 30;
        var shown = stage.Day.SelectedValue;
        clock.At(release + 3000);
        Assert.False(PhoneLoopingSelector.GetIsMoving(stage.Day));
        Assert.Equal(shown, stage.Day.SelectedValue);
        stage.Settle();
        Assert.Equal(SelectedTop, stage.TopOf(stage.Day, stage.Day.SelectedValue), 1);
    }

    [AvaloniaFact]
    public void Taken_Off_The_Panel_It_Leaves_Avalonias_Own_Behind()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var itemTheme = stage.Year.FindResource("Wp81LoopingSelectorItemTheme");
        var items = stage.Year.Children.OfType<ListBoxItem>().ToList();
        stage.Year.Theme = null;
        stage.Settle();
        Assert.Null(PhoneLoopingSelector.Of(stage.Year));
        Assert.Equal(40, stage.Year.ItemHeight);
        Assert.All(items, item => Assert.NotSame(itemTheme, item.Theme));

        Tap(stage, clock, stage.Year, ColumnHeight / 2 + 2 * stage.Year.ItemHeight, 0);
        Assert.Equal(2016, stage.Year.SelectedValue);
    }

    [AvaloniaFact]
    public void Turned_Off_Alone_It_Takes_Its_Item_Theme_Back()
    {
        using var stage = new Stage();
        var itemTheme = stage.Year.FindResource("Wp81LoopingSelectorItemTheme");
        PhoneLoopingSelector.SetIsEnabled(stage.Year, false);
        Assert.Null(PhoneLoopingSelector.Of(stage.Year));
        Assert.All(stage.Year.Children, item => Assert.NotSame(itemTheme, item.Theme));
    }

    [AvaloniaFact]
    public void A_Finger_Moving_Sideways_Or_A_Mouse_Drags_Nothing()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var offset = stage.Day.Offset;
        var touch = stage.Window.TouchBegin(stage.At(stage.Day, 350), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(i * 10);
            stage.Window.TouchMove(touch, stage.At(stage.Day, 350) + new Point(i * 6, -i * 3), RawInputModifiers.None);
        }

        Assert.Equal(offset, stage.Day.Offset);
        stage.Window.TouchEnd(touch, stage.At(stage.Day, 350) + new Point(60, -30), RawInputModifiers.None);

        stage.Window.MouseDown(stage.At(stage.Day, 350), MouseButton.Left);
        for (var i = 1; i <= 10; i++)
            stage.Window.MouseMove(stage.At(stage.Day, 350 - i * 10));
        Assert.Equal(offset, stage.Day.Offset);
        stage.Window.MouseUp(stage.At(stage.Day, 250), MouseButton.Left);
        clock.At(1000);
        Assert.Equal(15, stage.Day.SelectedValue);
    }

    [AvaloniaFact]
    public void Out_Of_Its_Window_A_Move_Ends_Where_It_Was_Going()
    {
        // In a popup that closes: out of the visual tree, still in the logical one, so the theme stays.
        using var clock = PhoneClock();
        using var stage = new Stage();
        var panel = Stage.Make(DateTimePickerPanelType.Day, "dd", 1, 31, 15, loop: true);
        panel.Width = ColumnWidth;
        panel.Height = ColumnHeight;
        var popup = new Avalonia.Controls.Primitives.Popup { Child = panel, PlacementTarget = stage.Canvas };
        stage.Canvas.Children.Add(popup);
        popup.IsOpen = true;
        stage.Settle();
        var wheel = PhoneLoopingSelector.Of(panel)!;
        var rest = panel.Offset.Y;
        wheel.BeginDrag();
        wheel.Drag(100);
        wheel.Release(2);
        var items = (int)Math.Round((wheel.Motion!.Target - rest) / Pitch);
        clock.At(100);
        Assert.True(wheel.IsCoasting);
        popup.IsOpen = false;
        Assert.Same(wheel, PhoneLoopingSelector.Of(panel));
        Assert.False(wheel.IsCoasting);
        Assert.False(PhoneLoopingSelector.GetIsMoving(panel));
        Assert.Equal((15 - 1 + items) % 31 + 1, panel.SelectedValue);

        // Shown again, it is as the phone opens its wheels: none active, and its first expansion at once.
        Assert.Equal(0, Expansion(panel));
        popup.IsOpen = true;
        stage.Settle();
        Assert.False(wheel.IsActive);
        wheel.Activate();
        Assert.Equal(1, Expansion(panel));
    }

    // ---- CornflowerConfig (#189) ----

    [AvaloniaFact]
    public void At_ControlSpeed_2_A_Wheel_Fades_Out_Twice_As_Fast()
    {
        using var config = new ConfigScope();
        CornflowerConfig.ControlSpeed = 2;
        using var clock = PhoneClock();
        using var stage = new Stage();
        Tap(stage, clock, stage.Month, 350, 0);
        Tap(stage, clock, stage.Day, 350, 1000);
        var start = clock.Now.TotalMilliseconds;
        clock.At(start + Frame + 1);
        var last = Expansion(stage.Month);
        clock.At(start + 2 * Frame + 1);
        Assert.Equal(2 * FadePerFrame, last - Expansion(stage.Month), 0.001);
        clock.At(start + 200);
        Assert.Equal(0, Expansion(stage.Month));
    }
}
