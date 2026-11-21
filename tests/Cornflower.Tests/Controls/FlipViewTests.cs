using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using Xunit;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 FlipView, on Avalonia's Carousel in the Wp81 theme (#214), against #211's reads: runs
/// <c>20261118-2213-dev11</c> (<c>-Probe flipview</c>: five items, full width, 400 tall, 80 from the top, item 1
/// selected) and <c>20261118-2216-dev11</c> (<c>-Probe flipvertical</c>), <c>tools/refapp/README.md</c>, "Tier 4's
/// reads", FlipView, and <c>tools/refapp/flip.py</c>'s frames. P is where the strip is, in view pixels from item 0: item k
/// at rest is k pages along, a page 445 px across (395 down).
/// </summary>
public class FlipViewTests
{
    private const double Page = 445;
    private const double Frame = 15.625;

    // Layout rounds to the device pixel, 1/2.4 of a view pixel at the reference profile: a place is read to half that.
    private const double Rounding = 0.21;

    private static TestClock Clock() => new() { FrameInterval = TimeSpan.FromMilliseconds(Frame), FramesOnGrid = true };

    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(int selected = 1, bool vertical = false, Func<int, Control>? item = null, int count = 5)
        {
            Carousel = new Carousel { Height = 400, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 80, 0, 0) };
            if (vertical)
                Carousel.PageTransition = new PageSlide { Orientation = PageSlide.SlideAxis.Vertical };
            for (var i = 0; i < count; i++)
                Carousel.Items.Add(item?.Invoke(i) ?? new Border { Child = new TextBlock { Text = $"item {i}" } });
            Carousel.SelectedIndex = selected;
            Carousel.SelectionChanged += (_, _) => Changes.Add(Carousel.SelectedIndex);
            _scene = new Scene(Carousel);
            Vertical = vertical;
        }

        public Carousel Carousel { get; }

        public Window Window => _scene.Window;

        public bool Vertical { get; }

        /// <summary>SelectedIndex at each SelectionChanged.</summary>
        public List<int> Changes { get; } = [];

        public Control Item(int index) => (Control)Carousel.ContainerFromIndex(index)!;

        /// <summary>Item <paramref name="index"/>'s top left, from the Carousel's.</summary>
        public Point ItemAt(int index)
        {
            Window.UpdateLayout();
            return Item(index).TranslatePoint(default, Carousel)!.Value;
        }

        /// <summary>P: the strip's place in view pixels from item 0, as <c>flip.py</c> reads it from item 0's place.</summary>
        public double P
        {
            get
            {
                var at = ItemAt(0);
                return 2.5 - (Vertical ? at.Y : at.X);
            }
        }

        /// <summary>
        /// A point in the items, <paramref name="along"/> forward (left, or up) from a press near the far side, or the near
        /// side for a drag back (<paramref name="back"/>).
        /// </summary>
        public Point Point(double along, bool back = false) =>
            Vertical ? new Point(225, (back ? 100 : 450) - along) : new Point((back ? 30 : 420) - along, 280);

        /// <summary>
        /// A finger pressed at <paramref name="start"/> ms, moved <paramref name="travel"/> forward (left, or up) in
        /// <paramref name="steps"/> moves a frame apart, then, if <paramref name="hold"/>, held still that long, and
        /// lifted on a frame. Returns the release's time.
        /// </summary>
        public double Swipe(TestClock clock, double travel, int steps, double start = 0, double hold = 0)
        {
            var t = start;
            clock.At(t);
            var back = travel < 0;
            var touch = Window.TouchBegin(Point(0, back), RawInputModifiers.None);
            for (var i = 1; i <= steps; i++)
            {
                clock.At(t += Frame);
                Window.TouchMove(touch, Point(travel * i / steps, back), RawInputModifiers.None);
            }

            if (hold > 0)
                clock.At(t += Math.Ceiling(hold / Frame) * Frame);
            Window.TouchEnd(touch, Point(travel, back), RawInputModifiers.None);
            return t;
        }

        public void Dispose() => _scene.Dispose();
    }

    // The template: a Border 2.5 in from each side round the items, each the Border's inside, abutting (item 0 at -442.5,
    // item 1 at 2.5, item 2 at 447.5 with item 1 shown; 445 x 395).
    [AvaloniaFact]
    public void An_Item_Is_The_FlipView_Less_2_5_A_Side_And_The_Items_Abut()
    {
        using var stage = new Stage();
        Assert.Equal(new Point(-442.5, 2.5), stage.ItemAt(0));
        Assert.Equal(new Point(2.5, 2.5), stage.ItemAt(1));
        Assert.Equal(new Point(447.5, 2.5), stage.ItemAt(2));
        Assert.Equal(new Size(445, 395), stage.Item(1).Bounds.Size);
    }

    // No previous and next buttons and no indicator: the template is the Border round the items, and nothing else.
    [AvaloniaFact]
    public void The_Template_Has_No_Buttons_Or_Indicator()
    {
        using var stage = new Stage();
        stage.Window.UpdateLayout();
        var panel = stage.Carousel.ItemsPanelRoot!;
        var parts = stage.Carousel.GetVisualDescendants().Where(v => !panel.IsVisualAncestorOf(v) || v == panel).ToList();
        Assert.Collection(parts,
            v => Assert.IsType<Border>(v),
            v => Assert.IsType<ItemsPresenter>(v),
            v => Assert.IsType<PhoneFlipViewPanel>(v));
    }

    // Held still, the phone's went back at 221.0 px and on from 233.8 (of 445): the Pivot's rule, its reach 0.52 a page.
    [AvaloniaTheory]
    [InlineData(221.0, 1)]
    [InlineData(233.8, 2)]
    public void Held_Still_Past_Half_A_Page_It_Goes_On(double travel, int ends)
    {
        using var clock = Clock();
        using var stage = new Stage();
        var release = stage.Swipe(clock, travel, steps: 12, hold: 100);
        Assert.Equal(445 + travel, stage.P, Rounding);

        clock.At(release + 1000);
        Assert.Equal(ends, stage.Carousel.SelectedIndex);
        Assert.Equal(ends * Page, stage.P, Rounding);
    }

    // Moving, the speed decides: 109.0 px at 0.20 px a ms went back, 107.2 at 0.33 on.
    [AvaloniaTheory]
    [InlineData(109.0, 0.20, 1)]
    [InlineData(107.2, 0.33, 2)]
    public void Moving_The_Speed_Decides(double travel, double speed, int ends)
    {
        using var clock = Clock();
        using var stage = new Stage();
        var steps = (int)Math.Round(travel / speed / Frame);
        var release = stage.Swipe(clock, travel, steps);
        clock.At(release + 1000);
        Assert.Equal(ends, stage.Carousel.SelectedIndex);
    }

    // The snap on, frame by frame: the README's first swipe, 174.7 px along at the release, P less its start a frame apart.
    // The Pivot's swipe curve over 33 frames is within 2.62 px of every frame.
    private static readonly double[] SnapOn =
    [
        174.7, 197.6, 220.2, 243.4, 267.0, 290.7, 311.0, 328.7, 345.4, 359.7, 371.5, 381.5, 390.5, 398.0, 404.6, 410.7,
        415.2, 419.5, 423.6, 426.9, 429.8, 432.6, 434.6, 436.5, 438.3, 439.7, 440.9, 442.0, 442.9, 443.6, 444.1, 444.5,
        444.8, 445.0,
    ];

    [AvaloniaFact]
    public void The_Snap_On_Follows_The_Phones_Frames_And_Selects_On_Its_Last()
    {
        using var clock = Clock();
        using var stage = new Stage();
        var release = stage.Swipe(clock, 174.7, steps: 10);   // 1.12 px a ms; the phone's, 1.14
        for (var k = 0; k < SnapOn.Length; k++)
        {
            clock.At(release + k * Frame);
            Assert.True(Math.Abs(445 + SnapOn[k] - stage.P) < 2.62 + Rounding, $"frame {k}: {stage.P - 445:F1}, the phone's {SnapOn[k]}");
            if (k < SnapOn.Length - 1)
                Assert.Equal(1, stage.Carousel.SelectedIndex);   // SelectionChanged comes at the end of the snap
        }

        Assert.Equal(2, stage.Carousel.SelectedIndex);
        Assert.Equal([2], stage.Changes);
        Assert.Equal(890, stage.P, Rounding);
    }

    // The snap back, frame by frame: 120 px let go at 0.20 px a ms, 112.1 along at the release. It carries on the way it was
    // dragged (to 125.0) and comes back by 640 ms; BackCurve is within 13.2 px of every frame.
    private static readonly double[] SnapBack =
    [
        112.1, 114.6, 117.1, 119.6, 122.0, 124.3, 125.0, 123.7, 121.0, 116.6, 110.2, 103.0, 94.2, 85.2, 76.4, 67.9, 60.7,
        53.3, 47.4, 41.5, 36.3, 31.9, 27.8, 24.1, 20.5, 17.6, 15.2, 12.7, 10.7, 8.8, 7.2, 5.7, 4.5, 3.4, 2.5, 1.7, 1.1, 0.7,
        0.3, 0.1, 0.0, 0.0,
    ];

    [AvaloniaFact]
    public void The_Snap_Back_Follows_The_Phones_Frames()
    {
        using var clock = Clock();
        using var stage = new Stage();
        var release = stage.Swipe(clock, 112.1, steps: 36);    // 0.20 px a ms
        var farthest = 0.0;
        for (var k = 0; k < SnapBack.Length; k++)
        {
            clock.At(release + k * Frame);
            Assert.True(Math.Abs(445 + SnapBack[k] - stage.P) < 13.22 + Rounding, $"frame {k}: {stage.P - 445:F1}, the phone's {SnapBack[k]}");
            farthest = Math.Max(farthest, stage.P - 445);
        }

        Assert.True(farthest > 112.1 + 2, $"it carries on first: {farthest:F1}");
        Assert.Equal(1, stage.Carousel.SelectedIndex);
        Assert.Empty(stage.Changes);
    }

    [AvaloniaFact]
    public void The_Snap_Back_Comes_To_Rest_After_640_Ms()
    {
        using var clock = Clock();
        using var stage = new Stage();
        var release = stage.Swipe(clock, 112.1, steps: 36);
        var motion = PhoneFlipView.MotionOf(stage.Carousel)!;
        clock.At(release + 625);
        Assert.True(motion.IsMoving);
        clock.At(release + 640.625);
        Assert.False(motion.IsMoving);
        Assert.Equal(445, stage.P, Rounding);
    }

    // Past the first item the items follow the finger, 139.7 px for 150, then ease back; the selection doesn't change and
    // the last item doesn't come round.
    [AvaloniaFact]
    public void Past_The_First_Item_It_Follows_Damped_And_Comes_Back()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 0);
        var release = stage.Swipe(clock, -150, steps: 10, hold: 100);
        Assert.Equal(-139.7, stage.P, Rounding);
        Assert.Equal(4 * Page + 139.7 + 2.5, stage.ItemAt(4).X, Rounding);

        clock.At(release + 700);
        Assert.Equal(0, stage.P, Rounding);
        Assert.Equal(0, stage.Carousel.SelectedIndex);
        Assert.Empty(stage.Changes);
    }

    // A swipe right at item 0 went back, as a drag held did: it never goes round to the last.
    [AvaloniaFact]
    public void A_Flick_Past_The_First_Doesnt_Go_Round()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 0);
        var release = stage.Swipe(clock, -136.9, steps: 8);  // 1.1 px a ms
        clock.At(release + 1000);
        Assert.Equal(0, stage.Carousel.SelectedIndex);
        Assert.Equal(0, stage.P, Rounding);
        Assert.Empty(stage.Changes);
    }

    // Past the last, 98.1 and 100.6 px for 150.
    [AvaloniaFact]
    public void Past_The_Last_Item_It_Follows_About_Two_Thirds()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 4);
        var release = stage.Swipe(clock, 150, steps: 10, hold: 100);
        Assert.Equal(4 * Page + 99.35, stage.P, Rounding);

        clock.At(release + 700);
        Assert.Equal(4 * Page, stage.P, Rounding);
        Assert.Equal(4, stage.Carousel.SelectedIndex);
    }

    // SelectedIndex 2 to 1 from code: 34 frames over 516 ms, P 890 to 445. CodeCurve is within 4.9 px of every frame.
    private static readonly double[] FromCode =
    [
        890.0, 882.8, 865.5, 838.4, 792.0, 735.9, 692.7, 655.8, 624.7, 601.1, 580.1, 562.2, 545.0, 532.5, 521.1, 510.7,
        501.0, 492.8, 485.5, 479.5, 473.1, 468.9, 464.3, 460.5, 457.2, 454.3, 451.8, 449.9, 448.2, 447.0, 446.0, 445.4,
        445.1, 445.0,
    ];

    [AvaloniaFact]
    public void A_Change_Of_One_From_Code_Animates_As_The_Phones()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 2);
        clock.At(Frame * 4);
        stage.Carousel.SelectedIndex = 1;
        for (var k = 0; k < FromCode.Length; k++)
        {
            clock.At(Frame * (4 + k));
            Assert.True(Math.Abs(FromCode[k] - stage.P) < 4.88 + Rounding, $"frame {k}: {stage.P:F1}, the phone's {FromCode[k]}");
        }

        Assert.Equal(445, stage.P, Rounding);
    }

    // 1 to 3, and 3 to 1: the next frame has the new place.
    [AvaloniaFact]
    public void A_Change_Of_Two_From_Code_Jumps()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 1);
        stage.Carousel.SelectedIndex = 3;
        Assert.Equal(3 * Page, stage.P, Rounding);
        stage.Carousel.SelectedIndex = 1;
        Assert.Equal(Page, stage.P, Rounding);
        Assert.False(PhoneFlipView.MotionOf(stage.Carousel)!.IsMoving);
    }

    // A press 94 ms after a swipe's release, held 600 ms: P didn't move. Let go, it went on (it was past half), the
    // selection with the end of the snap.
    [AvaloniaFact]
    public void A_Press_While_It_Snaps_Stops_It_Dead_And_Let_Go_It_Goes_On()
    {
        using var clock = Clock();
        using var stage = new Stage();
        var release = stage.Swipe(clock, 174.7, steps: 10);
        clock.At(release + 6 * Frame);
        var held = stage.P;
        Assert.True(held > 445 + 0.52 * Page);
        var touch = stage.Window.TouchBegin(stage.Point(0), RawInputModifiers.None);
        clock.At(release + 6 * Frame + 600);
        Assert.Equal(held, stage.P, Rounding);
        Assert.Equal(1, stage.Carousel.SelectedIndex);

        stage.Window.TouchEnd(touch, stage.Point(0), RawInputModifiers.None);
        clock.At(release + 6 * Frame + 1200);
        Assert.Equal(2, stage.Carousel.SelectedIndex);
        Assert.Equal(890, stage.P, Rounding);
    }

    // A drag after the press carries on from where it stopped.
    [AvaloniaFact]
    public void A_Drag_After_A_Press_Mid_Snap_Goes_On_From_There()
    {
        using var clock = Clock();
        using var stage = new Stage();
        var release = stage.Swipe(clock, 174.7, steps: 10);
        clock.At(release + 6 * Frame);
        var held = stage.P;
        var touch = stage.Window.TouchBegin(stage.Point(0), RawInputModifiers.None);
        for (var i = 1; i <= 5; i++)
        {
            clock.At(release + (6 + i) * Frame);
            stage.Window.TouchMove(touch, stage.Point(-10 * i), RawInputModifiers.None);
        }

        Assert.Equal(held - 50, stage.P, Rounding);
        stage.Window.TouchEnd(touch, stage.Point(-50), RawInputModifiers.None);
    }

    // A tap while SelectedIndex's move from code runs doesn't hold it, and doesn't undo the selection (as the Pivot's).
    [AvaloniaFact]
    public void A_Tap_During_A_Move_From_Code_Lets_It_Run()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 2);
        clock.At(Frame * 4);
        stage.Carousel.SelectedIndex = 3;
        clock.At(Frame * 10);
        var during = stage.P;
        var touch = stage.Window.TouchBegin(stage.Point(0), RawInputModifiers.None);
        clock.At(Frame * 20);
        Assert.True(stage.P > during + 10, "it moves on under the finger");
        stage.Window.TouchEnd(touch, stage.Point(0), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(3, stage.Carousel.SelectedIndex);
        Assert.Equal(3 * Page, stage.P, Rounding);
    }

    // An item removed before the selection shifts SelectedIndex by one: the same item, so nothing moves.
    [AvaloniaFact]
    public void An_Index_Shifted_By_The_Items_Doesnt_Animate()
    {
        using var clock = Clock();
        using var stage = new Stage(selected: 2);
        var shown = stage.Carousel.SelectedItem;
        stage.Carousel.Items.RemoveAt(0);
        Assert.Equal(1, stage.Carousel.SelectedIndex);
        Assert.Same(shown, stage.Carousel.SelectedItem);
        Assert.False(PhoneFlipView.MotionOf(stage.Carousel)!.IsMoving);
        Assert.Equal(Page, stage.P, Rounding);
    }

    // The selected item is laid out a page along before it slides in: brought into view there, it would scroll the page.
    [AvaloniaFact]
    public void A_Move_From_Code_Doesnt_Scroll_The_Page_Round_It()
    {
        using var clock = Clock();
        var carousel = new Carousel { Height = 240, PageTransition = new PageSlide { Orientation = PageSlide.SlideAxis.Vertical } };
        for (var i = 0; i < 3; i++) carousel.Items.Add(new Border());
        var page = new ScrollViewer { Content = new StackPanel { Children = { carousel, new Border { Height = 2000 } } } };
        using var scene = new Scene(page);
        carousel.SelectedIndex = 1;
        clock.At(1000);
        scene.Window.UpdateLayout();
        Assert.Equal(0, page.Offset.Y);
        Assert.Equal(1, carousel.SelectedIndex);
    }

    // With the motion off, the theme's panel still shows the selection at once.
    [AvaloniaFact]
    public void With_The_Motion_Off_The_Panel_Follows_The_Selection()
    {
        using var clock = Clock();
        using var stage = new Stage();
        PhoneFlipView.SetIsEnabled(stage.Carousel, false);
        stage.Carousel.SelectedIndex = 2;
        Assert.Equal(2 * Page, stage.P, Rounding);
    }

    // Under another panel (Avalonia's own, put back), the motion leaves the finger alone.
    [AvaloniaFact]
    public void Under_Another_Panel_The_Motion_Doesnt_Swipe()
    {
        using var clock = Clock();
        using var stage = new Stage();
        stage.Carousel.ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel());
        stage.Window.UpdateLayout();
        var touch = stage.Window.TouchBegin(stage.Point(0), RawInputModifiers.None);
        clock.At(Frame);
        stage.Window.TouchMove(touch, stage.Point(100), RawInputModifiers.None);
        Assert.False(PhoneFlipView.MotionOf(stage.Carousel)!.IsDragging);
        stage.Window.TouchEnd(touch, stage.Point(100), RawInputModifiers.None);
    }

    // A PageSlide turned vertical in place is read at the next press.
    [AvaloniaFact]
    public void A_PageSlide_Turned_In_Place_Is_Read_At_The_Next_Press()
    {
        using var clock = Clock();
        using var stage = new Stage(vertical: true);
        var slide = new PageSlide();
        stage.Carousel.PageTransition = slide;           // sideways
        slide.Orientation = PageSlide.SlideAxis.Vertical;
        var release = stage.Swipe(clock, 247.3, steps: 12, hold: 100);
        clock.At(release + 1000);
        Assert.Equal(2, stage.Carousel.SelectedIndex);
        Assert.Equal(new Point(2.5, 2.5), stage.ItemAt(2));
    }

    // Vertical: a page is 395 px; held, 247.3 px up went on; a drag across doesn't move it.
    [AvaloniaFact]
    public void A_Vertical_FlipView_Flips_Up_And_Down()
    {
        using var clock = Clock();
        using var stage = new Stage(vertical: true);
        Assert.Equal(new Point(2.5, 397.5), stage.ItemAt(2));

        var touch = stage.Window.TouchBegin(new Point(400, 280), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(i * Frame);
            stage.Window.TouchMove(touch, new Point(400 - 20 * i, 280), RawInputModifiers.None);
        }

        Assert.Equal(395, stage.P, Rounding);
        stage.Window.TouchEnd(touch, new Point(200, 280), RawInputModifiers.None);

        var release = stage.Swipe(clock, 247.3, steps: 12, start: 400, hold: 100);
        clock.At(release + 1000);
        Assert.Equal(2, stage.Carousel.SelectedIndex);
        Assert.Equal(790, stage.P, Rounding);
    }

    // As in a Pivot, a ScrollViewer in an item keeps a drag up and down, and a part that asks for no gesture keeps its own.
    [AvaloniaFact]
    public void A_ScrollViewer_In_An_Item_Keeps_Its_Drag()
    {
        using var clock = Clock();
        ScrollViewer? scroll = null;
        using var stage = new Stage(item: i =>
        {
            var lines = new StackPanel();
            for (var j = 0; j < 40; j++) lines.Children.Add(new TextBlock { Text = $"line {j}", Height = 40 });
            var viewer = new ScrollViewer { Content = lines };
            if (i == 1) scroll = viewer;
            return viewer;
        });

        var touch = stage.Window.TouchBegin(new Point(225, 400), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(i * Frame);
            stage.Window.TouchMove(touch, new Point(225 + i, 400 - 20 * i), RawInputModifiers.None);
        }

        stage.Window.TouchEnd(touch, new Point(235, 200), RawInputModifiers.None);
        Assert.True(scroll!.Offset.Y > 100);
        Assert.Equal(445, stage.P, Rounding);
    }

    // A drag more up and down than across isn't a swipe, though it passes the 5 px sideways.
    [AvaloniaFact]
    public void A_Drag_More_Across_The_Axis_Than_Along_It_Isnt_A_Swipe()
    {
        using var clock = Clock();
        using var stage = new Stage();
        var touch = stage.Window.TouchBegin(new Point(225, 400), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            clock.At(i * Frame);
            stage.Window.TouchMove(touch, new Point(225 - 4 * i, 400 - 10 * i), RawInputModifiers.None);
        }

        Assert.Equal(445, stage.P, Rounding);
        stage.Window.TouchEnd(touch, new Point(185, 300), RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void A_Part_With_No_Manipulation_Keeps_Its_Drag()
    {
        using var clock = Clock();
        using var stage = new Stage(item: _ =>
        {
            var part = new Border { Background = Avalonia.Media.Brushes.Transparent };
            PhoneNoManipulation.SetIsEnabled(part, true);
            return part;
        });

        stage.Swipe(clock, 300, steps: 10);
        clock.At(2000);
        Assert.Equal(445, stage.P, Rounding);
        Assert.Equal(1, stage.Carousel.SelectedIndex);
    }

    // Avalonia's IsSwipeEnabled, which the theme turns on: off, a finger drags nothing.
    [AvaloniaFact]
    public void IsSwipeEnabled_Off_Drags_Nothing()
    {
        using var clock = Clock();
        using var stage = new Stage();
        Assert.True(stage.Carousel.IsSwipeEnabled);
        stage.Carousel.IsSwipeEnabled = false;
        stage.Swipe(clock, 300, steps: 10);
        Assert.Equal(445, stage.P, Rounding);
        clock.At(2000);
        Assert.Equal(1, stage.Carousel.SelectedIndex);
    }

    // The motion's area is CornflowerConfig.ControlSpeed: at 2, the snap on takes half its 515.625 ms; the Pivot's and
    // Hub's speed doesn't touch it.
    [AvaloniaFact]
    public void The_Snap_Runs_At_The_Control_Speed()
    {
        using var config = new ConfigScope();
        using var clock = Clock();
        using var stage = new Stage();
        CornflowerConfig.PivotAndHubSpeed = 4;
        CornflowerConfig.ControlSpeed = 2;
        var release = stage.Swipe(clock, 174.7, steps: 10);
        clock.At(release + 16 * Frame);
        Assert.Equal(1, stage.Carousel.SelectedIndex);
        clock.At(release + 17 * Frame);
        Assert.Equal(2, stage.Carousel.SelectedIndex);
        Assert.Equal(890, stage.P, Rounding);
    }
}
