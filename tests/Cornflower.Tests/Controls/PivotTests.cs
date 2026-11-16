using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Animation;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Controls.Primitives;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PivotReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 Pivot (Cornflower's <c>Pivot</c>, <c>PivotItem</c> and <c>PivotHeaderItem</c>, themed by
/// <c>Controls/Pivot.axaml</c>) against the phone's style, the catalogue's captures and #128's and #136's motion
/// readings (<see cref="PivotReadings"/>). docs/controls/Pivot has the plan and the comparison.
/// </summary>
public class PivotTests
{
    /// <summary>A Pivot of the given headers, each item a line of text, full width at the top of the reference view.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(string[]? headers = null, ThemeVariant? variant = null, double? height = null, Pivot? pivot = null,
            double width = Scene.ReferenceWidth, double viewHeight = Scene.ReferenceHeight)
        {
            Pivot = pivot ?? Make(headers ?? ["one", "two", "three", "four"]);
            if (height is { } h)
            {
                Pivot.Height = h;
                Pivot.VerticalAlignment = VerticalAlignment.Top;
            }
            _scene = new Scene(Pivot, variant, width: width, height: viewHeight);
        }

        public Pivot Pivot { get; }

        public Window Window => _scene.Window;

        public PivotHeaderPanel Strip => Pivot.HeaderPanel!;

        public PivotHeaderItem Header(int index) => (PivotHeaderItem)Strip.Children[index];

        /// <summary>A header's left edge, as drawn, from the Pivot's left.</summary>
        public double HeaderX(int index) => Header(index).TranslatePoint(default, Pivot)!.Value.X;

        /// <summary>Where item <paramref name="index"/> is drawn, from its place at rest.</summary>
        public double ItemX(int index)
        {
            var container = Pivot.ContainerFromIndex(index)!;
            return container.TranslatePoint(default, Pivot)!.Value.X - container.TranslatePoint(default, Pivot.ItemsPresenterPart!)!.Value.X;
        }

        public bool Shown(int index) => Pivot.ContainerFromIndex(index)!.IsVisible;

        public void Layout() => Window.UpdateLayout();

        public void Dispose() => _scene.Dispose();

        public static Pivot Make(string[] headers, string? title = "CORNFLOWER")
        {
            var pivot = new Pivot { Title = title };
            foreach (var header in headers)
                pivot.Items.Add(new PivotItem { Header = header, Content = new TextBlock { Text = header + " pivot item" } });
            return pivot;
        }
    }

    /// <summary>A clock on the emulator's frames, so each frame is one of the phone's.</summary>
    private static TestClock PhoneClock() => new() { FrameInterval = TimeSpan.FromMilliseconds(Frame), FramesOnGrid = true };

    /// <summary>
    /// The probes' swipe: pressed at <paramref name="at"/> ms, 15 moves of 13.333 px 10 ms apart, let go on the last.
    /// The release is at <paramref name="at"/> + 150 ms. Left from x 380, right from 70, at y 450.
    /// </summary>
    private static void Swipe(Stage stage, TestClock clock, double at, bool left = true, int moves = SwipeMoves, double holdMs = 0)
    {
        var x = left ? 380 : 70;
        var sign = left ? -1 : 1;
        clock.At(at);
        var touch = stage.Window.TouchBegin(new Point(x, 450), RawInputModifiers.None);
        for (var i = 1; i <= moves; i++)
        {
            clock.At(at + i * SwipeMoveMs);
            stage.Window.TouchMove(touch, new Point(x + sign * i * SwipeStep, 450), RawInputModifiers.None);
        }

        clock.At(at + moves * SwipeMoveMs + holdMs);
        stage.Window.TouchEnd(touch, new Point(x + sign * moves * SwipeStep, 450), RawInputModifiers.None);
    }

    private static void Tap(Stage stage, TestClock clock, double at, Point point, double holdMs = 70)
    {
        clock.At(at);
        stage.Window.MouseDown(point, MouseButton.Left);
        clock.At(at + holdMs);
        stage.Window.MouseUp(point, MouseButton.Left);
    }

    private static Point HeaderCentre(Stage stage, int index)
    {
        var header = stage.Header(index);
        return header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), stage.Window)!.Value;
    }

    private static uint Argb(IBrush? brush) => ((ISolidColorBrush)brush!).Color.ToUInt32();

    // ---- The style, the template and the captures ----

    [AvaloniaFact]
    public void Each_Control_Has_The_Wp81_Template()
    {
        using var stage = new Stage();
        Assert.NotNull(stage.Pivot.HeaderPanel);
        Assert.Contains(stage.Pivot.GetTemplateDescendants(), c => c is ScrollViewer { Name: "PART_ScrollViewer" });
        Assert.Contains(((TemplatedControl)stage.Pivot.ContainerFromIndex(0)!).GetTemplateDescendants(), c => c is ContentPresenter { Name: "PART_ContentPresenter" });
        Assert.Contains(stage.Header(0).GetTemplateDescendants(), c => c is Grid { Name: "PART_Grid" });
        Assert.Equal("Wp81ScrollViewerScrollBarlessTheme",
            stage.Pivot.GetTemplateDescendants().OfType<ScrollViewer>().Single().Theme == stage.Pivot.FindResource("Wp81ScrollViewerScrollBarlessTheme") ? "Wp81ScrollViewerScrollBarlessTheme" : "other");
    }

    // The catalogue's capture: the title, the strip, the item, where the phone's dump has them.
    [AvaloniaFact]
    public void The_Bounds_Are_The_Dumps()
    {
        using var stage = new Stage(["one", "two"], height: StageHeight);
        var pivot = stage.Pivot;
        var title = pivot.GetTemplateDescendants().OfType<ContentControl>().Single(c => c.Name == "PART_TitleContentControl");
        var titleText = title.GetVisualDescendants().OfType<TextBlock>().Single();
        var titleAt = titleText.TranslatePoint(default, pivot)!.Value;
        Assert.Equal(TitleLeft, titleAt.X, 0.01);
        Assert.Equal(TitleTop, titleAt.Y, 0.01);
        Assert.Equal(TitleHeight, titleText.Bounds.Height, 0.05);

        var scroll = pivot.GetTemplateDescendants().OfType<ScrollViewer>().Single();
        Assert.Equal(ScrollViewerTop, scroll.TranslatePoint(default, pivot)!.Value.Y, 0.01);
        Assert.Equal(HeaderPanelHeight, stage.Strip.Bounds.Height, 0.2);

        var header = stage.Header(0);
        var headerAt = header.TranslatePoint(default, pivot)!.Value;
        Assert.Equal(HeaderLeft, headerAt.X, 0.02);
        Assert.Equal(HeaderTop, headerAt.Y, 0.01);
        Assert.Equal(HeaderHeight, header.Bounds.Height, 0.01);
        Assert.Equal(HeaderTextHeight, header.GetVisualDescendants().OfType<TextBlock>().Single().Bounds.Height, 0.05);

        var presenter = pivot.ItemsPresenterPart!;
        Assert.Equal(ItemsPresenterTop, presenter.TranslatePoint(default, pivot)!.Value.Y, 0.01);
        var item = pivot.ContainerFromIndex(0)!;
        var itemAt = item.TranslatePoint(default, pivot)!.Value;
        Assert.Equal(ItemLeft, itemAt.X, 0.01);
        Assert.Equal(ItemTop, itemAt.Y, 0.01);
        Assert.Equal(ItemWidth, item.Bounds.Width, 0.5);
    }

    [AvaloniaTheory]
    [InlineData(false, DarkSelected, DarkUnselected)]
    [InlineData(true, LightSelected, LightUnselected)]
    public void The_Selected_Header_Is_The_Foreground_And_The_Others_Dimmed(bool light, uint selected, uint unselected)
    {
        using var stage = new Stage(["one", "two"], light ? ThemeVariant.Light : ThemeVariant.Dark);
        var one = stage.Header(0).GetVisualDescendants().OfType<TextBlock>().Single();
        var two = stage.Header(1).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(selected, Argb(one.Foreground));
        Assert.Equal(unselected, Argb(two.Foreground));
        Assert.Contains(":selected", stage.Header(0).Classes);
        Assert.DoesNotContain(":selected", stage.Header(1).Classes);

        stage.Pivot.SelectedIndex = 1;
        Assert.Equal(unselected, Argb(one.Foreground));
        Assert.Equal(selected, Argb(two.Foreground));
    }

    // PivotHeaderItemFontSize 57, PivotHeaderItemCharacterSpacing -25 (thousandths of an em), the family's key.
    [AvaloniaFact]
    public void A_Header_Is_The_Phones_Size_Spacing_And_Family()
    {
        using var stage = new Stage(["one", "two"]);
        var text = stage.Header(0).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(57, text.FontSize);
        Assert.Equal(-25 * 57 / 1000.0, text.LetterSpacing, 6);
        Assert.Equal(stage.Pivot.FindResource("PivotHeaderItemFontFamily"), text.FontFamily);
    }

    // #128: a header's pitch is its width plus 15.83, the next header's margin on the raw grid.
    [AvaloniaFact]
    public void Each_Header_Follows_The_Last_Its_Width_And_The_Gap_Apart()
    {
        using var stage = new Stage();
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(stage.HeaderX(i) + stage.Header(i).Bounds.Width + HeaderGap, stage.HeaderX(i + 1), 0.01);
            Assert.Equal(stage.Header(i).Bounds.Width + HeaderGap, stage.Strip.Pitch(i), 0.01);
        }

        Assert.Equal(stage.Header(3).Bounds.Width + HeaderGap, stage.Strip.Pitch(3), 0.01);
    }

    // #136: at rest the selected header is first, the rest after it in item order, wrapping round.
    [AvaloniaFact]
    public void At_Rest_The_Strip_Starts_At_The_Selected_Header_And_Wraps_Round()
    {
        using var stage = new Stage();
        stage.Pivot.SelectedIndex = 2;
        stage.Pivot.Motion.Finish();
        stage.Layout();
        var order = Enumerable.Range(0, 4).OrderBy(stage.HeaderX).ToArray();
        Assert.Equal([2, 3, 0, 1], order);
        Assert.Equal(HeaderLeft, stage.HeaderX(2), 0.02);
    }

    // #136: a header wider than the screen keeps its full width; the edge cuts it.
    [AvaloniaFact]
    public void A_Header_Wider_Than_The_View_Keeps_Its_Width()
    {
        using var stage = new Stage(["favourites", "everything", "recent", "supercalifragilisticexpialidocious"]);
        var wide = stage.Header(3);
        var text = wide.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.True(wide.Bounds.Width > Scene.ReferenceWidth, $"{wide.Bounds.Width}");
        Assert.Equal(text.DesiredSize.Width, wide.Bounds.Width, 0.01);
        Assert.Single(text.TextLayout.TextLines);
    }

    [AvaloniaFact]
    public void Only_The_Selected_Item_Is_Shown()
    {
        using var stage = new Stage();
        Assert.True(stage.Shown(0));
        Assert.False(stage.Shown(1));
        Assert.False(stage.Shown(2));
        stage.Pivot.SelectedIndex = 2;
        stage.Pivot.Motion.Finish();
        Assert.False(stage.Shown(0));
        Assert.True(stage.Shown(2));
    }

    // The phone's Orientation states: PivotLandscapeThemePadding (19,25,0,0) while the view is wider than tall.
    [AvaloniaFact]
    public void Landscape_Moves_The_Title_Up()
    {
        using var stage = new Stage(["one", "two"], width: 800, viewHeight: 450);
        Assert.Contains(":landscape", stage.Pivot.Classes);
        var title = stage.Pivot.GetTemplateDescendants().OfType<ContentControl>().Single(c => c.Name == "PART_TitleContentControl");
        Assert.Equal(new Thickness(19, TitleLandscapeTop, 0, 0), title.Margin);

        using var portrait = new Stage(["one", "two"]);
        Assert.Contains(":portrait", portrait.Pivot.Classes);
        var portraitTitle = portrait.Pivot.GetTemplateDescendants().OfType<ContentControl>().Single(c => c.Name == "PART_TitleContentControl");
        Assert.Equal(new Thickness(19, 38, 0, 0), portraitTitle.Margin);
    }

    [AvaloniaFact]
    public void Title_TitleTemplate_HeaderTemplate_And_Data_Items_Are_Shown()
    {
        var pivot = new Pivot
        {
            Title = "app",
            TitleTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s.ToUpperInvariant() }),
            HeaderTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s + "!" }),
            ItemTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = "page " + s }),
            ItemsSource = new[] { "a", "b" },
        };
        using var stage = new Stage(pivot: pivot);
        var texts = pivot.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("APP", texts);
        Assert.Contains("a!", texts);
        Assert.Contains("b!", texts);
        Assert.Contains("page a", texts);
        Assert.Equal("a", pivot.SelectedItem);
        Assert.Equal("a", Assert.IsType<PivotItem>(pivot.ContainerFromIndex(0)).Header);
    }

    [AvaloniaFact]
    public void A_Header_Change_Shows_In_The_Strip()
    {
        using var stage = new Stage(["one", "two"]);
        ((PivotItem)stage.Pivot.Items[1]!).Header = "deux";
        Assert.Equal("deux", stage.Header(1).Content);
    }

    // ---- The strip and the offset (#128, #136) ----

    // #128: a header moves against the offset in a straight line, at its own pitch over a page; #136: a header wider
    // than the view moves faster than the content.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dragging_Left_Moves_The_Strip_At_The_Leaving_Headers_Pitch(bool wide)
    {
        using var clock = PhoneClock();
        using var stage = new Stage(wide ? ["favourites", "everything", "recent", "supercalifragilisticexpialidocious"] : null);
        for (var selected = 0; selected < 4; selected++)
        {
            stage.Pivot.SelectedIndex = selected;
            stage.Pivot.Motion.Finish();
            stage.Layout();
            var start = stage.HeaderX(selected);
            var pitch = stage.Header(selected).Bounds.Width + HeaderGap;
            var touch = stage.Window.TouchBegin(new Point(400, 450), RawInputModifiers.None);
            foreach (var travel in new[] { 40.0, 120, 200 })
            {
                stage.Window.TouchMove(touch, new Point(400 - travel, 450), RawInputModifiers.None);
                Assert.Equal(start - travel * pitch / Scene.ReferenceWidth, stage.HeaderX(selected), 0.01);
                Assert.Equal(-travel, stage.ItemX(selected), 0.01);
            }

            stage.Window.TouchMove(touch, new Point(400, 450), RawInputModifiers.None);
            stage.Window.TouchEnd(touch, new Point(400, 450), RawInputModifiers.None);
        }
    }

    // #128: going right, the strip moves at the pitch of the header coming in, the one before.
    [AvaloniaFact]
    public void Dragging_Right_Moves_The_Strip_At_The_Incoming_Headers_Pitch()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var start = stage.HeaderX(0);
        var incoming = stage.Header(3).Bounds.Width + HeaderGap;
        var touch = stage.Window.TouchBegin(new Point(50, 450), RawInputModifiers.None);
        stage.Window.TouchMove(touch, new Point(200, 450), RawInputModifiers.None);
        Assert.Equal(start + 150 * incoming / Scene.ReferenceWidth, stage.HeaderX(0), 0.01);
        Assert.Equal(150, stage.ItemX(0), 0.01);
        stage.Window.TouchEnd(touch, new Point(50, 450), RawInputModifiers.None);
    }

    // #128: the selection changes during a swipe left, 188 to 241 ms after the press; the offset travels exactly a
    // page and settles 609 to 672 ms after the press; once the new item is chosen, the header that left goes to the end.
    [AvaloniaFact]
    public void A_Swipe_Left_Selects_The_Next_Item_Part_Way_And_Moves_A_Page()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var changedAt = double.NaN;
        stage.Pivot.SelectionChanged += (_, _) => changedAt = clock.Now.TotalMilliseconds;
        const double press = 6.25;                                            // so the release, 150 ms on, is on a frame

        Swipe(stage, clock, press);
        var release = press + SwipeMoves * SwipeMoveMs;
        clock.At(release + 4 * Frame);
        stage.Layout();
        Assert.Equal(0, stage.Pivot.SelectedIndex);
        var leftBefore = stage.HeaderX(0);
        Assert.True(leftBefore < HeaderLeft);

        clock.At(release + 5 * Frame);
        stage.Layout();
        Assert.Equal(1, stage.Pivot.SelectedIndex);
        Assert.InRange(changedAt - press, SelectionLeftMin, SelectionLeftMax);
        Assert.True(stage.HeaderX(0) > stage.HeaderX(3), "the header that left is after the last");
        Assert.True(stage.Shown(1));
        Assert.False(stage.Shown(0));

        clock.At(release + PivotMotion.SwipeDuration.TotalMilliseconds - 1);
        Assert.True(stage.Pivot.Motion.IsMoving);
        clock.At(release + PivotMotion.SwipeDuration.TotalMilliseconds);
        stage.Layout();
        Assert.False(stage.Pivot.Motion.IsMoving);
        Assert.InRange(release + PivotMotion.SwipeDuration.TotalMilliseconds - press, SwipeSettleMin, SwipeSettleMax);
        Assert.Equal(HeaderLeft, stage.HeaderX(1), 0.02);
        Assert.Equal(0, stage.ItemX(1), 0.001);
        Assert.Equal([1, 2, 3, 0], Enumerable.Range(0, 4).OrderBy(stage.HeaderX).ToArray());
    }

    [AvaloniaFact]
    public void A_Header_Tap_After_A_Swipe_Selects()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 6.25);
        clock.At(2000);
        stage.Layout();
        Assert.Equal(1, stage.Pivot.SelectedIndex);
        Tap(stage, clock, 3000, HeaderCentre(stage, 2));
        Assert.Equal(2, stage.Pivot.SelectedIndex);
    }

    [AvaloniaFact]
    public void A_Swipe_That_Starts_On_A_Header_Doesnt_Tap_It()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var start = HeaderCentre(stage, 2);
        clock.At(10);
        var touch = stage.Window.TouchBegin(start, RawInputModifiers.None);
        for (var i = 1; i <= SwipeMoves; i++)
        {
            clock.At(10 + i * SwipeMoveMs);
            stage.Window.TouchMove(touch, start - new Point(i * SwipeStep, 0), RawInputModifiers.None);
        }

        stage.Window.TouchEnd(touch, start - new Point(200, 0), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(1, stage.Pivot.SelectedIndex);
    }

    // #128: the old item slides out ahead of the offset from the frame after the release; the new one comes in a page
    // beyond where it settles and slides in, one frame there, then ExponentialEase 4 over 400 ms.
    [AvaloniaFact]
    public void After_A_Swipe_The_Items_Slide_As_The_Phones()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        const double press = 6.25;
        Swipe(stage, clock, press);
        var release = press + SwipeMoves * SwipeMoveMs;
        var motion = stage.Pivot.Motion;

        for (var k = 1; k < SlideOut.Length; k++)
        {
            clock.At(release + k * Frame);
            Assert.Equal(-motion.Offset - SlideOut[k] * Scene.ReferenceWidth, stage.ItemX(0), 0.1);
        }

        // The jump, at the selection (frame 5), and the slide in after it.
        for (var k = 0; k < SlideIn.Length; k++)
        {
            clock.At(release + (5 + k) * Frame);
            var toFinal = -motion.Offset;                                     // the offset still to go, from the new item
            Assert.Equal(toFinal + SlideIn[Math.Max(k - 1, 0)] * Scene.ReferenceWidth, stage.ItemX(1), 0.1);
        }
    }

    // #128 (20 swipes): the offset after the release follows the phone's frame by frame, within its own spread.
    [AvaloniaFact]
    public void A_Swipes_Offset_Follows_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        const double press = 6.25;
        Swipe(stage, clock, press);
        var release = press + SwipeMoves * SwipeMoveMs;
        var motion = stage.Pivot.Motion;
        var from = motion.Offset;
        for (var k = 0; k < SwipeToGo.Length; k++)
        {
            clock.At(release + (k + 1) * Frame);
            var offset = motion.Lead == 1 ? motion.Offset + Scene.ReferenceWidth : motion.Offset;
            var toGo = (Scene.ReferenceWidth - offset) / (Scene.ReferenceWidth - from);
            Assert.True(Math.Abs(toGo - SwipeToGo[k]) <= 0.006, $"frame {k + 1}: {toGo:F4} against the phone's {SwipeToGo[k]:F4}");
        }
    }

    // #128: a swipe right selects the item before 152 to 174 ms after the press, and the new item comes in at once.
    [AvaloniaFact]
    public void A_Swipe_Right_Selects_The_Item_Before_And_Wraps()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var changedAt = double.NaN;
        stage.Pivot.SelectionChanged += (_, _) => changedAt = clock.Now.TotalMilliseconds;
        const double press = 6.25;
        Swipe(stage, clock, press, left: false);
        var release = press + SwipeMoves * SwipeMoveMs;
        clock.At(release + Frame);
        stage.Layout();
        Assert.Equal(3, stage.Pivot.SelectedIndex);
        Assert.InRange(changedAt - press, SelectionRightMin, SelectionRightMax);
        Assert.True(stage.ItemX(3) < 0, "the item before comes in from the left");
        Assert.True(stage.HeaderX(3) < stage.HeaderX(0), "its header jumps to the left");

        clock.At(release + 1000);
        stage.Layout();
        Assert.Equal(HeaderLeft, stage.HeaderX(3), 0.02);
        Assert.Equal(0, stage.ItemX(3), 0.001);
    }

    // #128: a drag of 100 px held and let go goes back.
    [AvaloniaFact]
    public void A_Short_Drag_Held_And_Let_Go_Goes_Back_On_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        clock.At(10);
        var touch = stage.Window.TouchBegin(new Point(300, 450), RawInputModifiers.None);
        clock.At(20);
        stage.Window.TouchMove(touch, new Point(250, 450), RawInputModifiers.None);
        clock.At(30);
        stage.Window.TouchMove(touch, new Point(200, 450), RawInputModifiers.None);
        clock.At(328.125);
        stage.Window.TouchEnd(touch, new Point(200, 450), RawInputModifiers.None);
        var motion = stage.Pivot.Motion;
        Assert.Equal(100, motion.Offset, 0.001);
        for (var k = 0; k < ReturnToGo.Length; k++)
        {
            clock.At(328.125 + (k + 1) * Frame);
            Assert.True(Math.Abs(motion.Offset / 100 - ReturnToGo[k]) <= 0.004, $"frame {k + 1}: {motion.Offset / 100:F4} against {ReturnToGo[k]:F4}");
        }

        Assert.False(motion.IsMoving);
        Assert.Equal(0, stage.Pivot.SelectedIndex);
        Assert.Equal(0, stage.ItemX(0), 0.001);
    }

    // A slow drag let go past half a page goes on (not read on the phone: PLAN.md, "The motion, as modelled").
    [AvaloniaFact]
    public void A_Slow_Drag_Past_Half_A_Page_Goes_On()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        clock.At(10);
        var touch = stage.Window.TouchBegin(new Point(400, 450), RawInputModifiers.None);
        stage.Window.TouchMove(touch, new Point(300, 450), RawInputModifiers.None);
        clock.At(20);
        stage.Window.TouchMove(touch, new Point(160, 450), RawInputModifiers.None);
        clock.At(400);
        stage.Window.TouchEnd(touch, new Point(160, 450), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(1, stage.Pivot.SelectedIndex);
    }

    // #128: a header tap selects at the release; the offset moves a page a header (two for "three") on the tap's
    // curve, the old item goes out, and the new comes in once it is gone; all settles 516 to 531 ms after the release.
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void A_Header_Tap_Selects_At_The_Release_And_Moves_There(int tapped)
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var changedAt = double.NaN;
        stage.Pivot.SelectionChanged += (_, _) => changedAt = clock.Now.TotalMilliseconds;
        var point = HeaderCentre(stage, tapped);
        Tap(stage, clock, 1000, point, holdMs: 62.5);                         // the release on a frame
        Assert.Equal(tapped, stage.Pivot.SelectedIndex);
        Assert.Equal(1062.5, changedAt);

        var motion = stage.Pivot.Motion;
        var last = 0.0;
        for (var t = 1062.5 + Frame; motion.IsMoving; t += Frame)
        {
            clock.At(t);
            last = t;
        }

        Assert.InRange(last - 1062.5, TapSettleMin, TapSettleMax);
        Assert.Equal(tapped == 1 ? 531.25 : 515.625, last - 1062.5);         // the phone's, run 20261115-2136-dev11
        stage.Layout();
        Assert.Equal(HeaderLeft, stage.HeaderX(tapped), 0.02);
        Assert.True(stage.Shown(tapped));
    }

    [AvaloniaFact]
    public void A_Taps_Offset_Follows_The_Phones_Curve()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Tap(stage, clock, 1000, HeaderCentre(stage, 1), holdMs: 62.5);
        var motion = stage.Pivot.Motion;
        for (var k = 0; k < TapToGo.Length; k++)
        {
            clock.At(1062.5 + (k + 2) * Frame);
            var offset = motion.Lead == 1 ? motion.Offset + Scene.ReferenceWidth : motion.Offset;
            var toGo = 1 - offset / Scene.ReferenceWidth;
            Assert.True(Math.Abs(toGo - TapToGo[k]) <= 0.008, $"frame {k + 2}: {toGo:F4} against {TapToGo[k]:F4}");
        }
    }

    // #136: a header the edge cuts is tapped like any other, and a tap on the selected one changes nothing.
    [AvaloniaFact]
    public void A_Tap_On_A_Header_Cut_By_The_Edge_Selects_It_And_On_The_Selected_Does_Nothing()
    {
        using var clock = PhoneClock();
        using var stage = new Stage(["favourites", "everything", "recent", "supercalifragilisticexpialidocious"]);
        var cut = Enumerable.Range(1, 3).First(i => stage.HeaderX(i) < Scene.ReferenceWidth && stage.HeaderX(i) + stage.Header(i).Bounds.Width > Scene.ReferenceWidth);
        var tapped = false;
        stage.Pivot.AddHandler(InputElement.TappedEvent, (_, _) => tapped = true, handledEventsToo: true);
        Tap(stage, clock, 100, new Point((stage.HeaderX(cut) + Scene.ReferenceWidth) / 2, 100));
        Assert.Equal(cut, stage.Pivot.SelectedIndex);
        clock.At(2000);
        stage.Layout();

        tapped = false;
        var changed = false;
        stage.Pivot.SelectionChanged += (_, _) => changed = true;
        Tap(stage, clock, 3000, new Point(stage.HeaderX(cut) + 20, 100));
        Assert.True(tapped);
        Assert.False(changed);
        Assert.False(stage.Pivot.Motion.IsMoving);
    }

    // #136: a two-item Pivot wraps both ways, its strip only the two headers; going right, "two" stays right of "one"
    // until the selection changes, then jumps to the left.
    [AvaloniaFact]
    public void Two_Items_Wrap_Both_Ways()
    {
        using var clock = PhoneClock();
        using var stage = new Stage(["one", "two"]);
        Assert.Equal(2, stage.Strip.Children.Count);
        const double press = 6.25;
        Swipe(stage, clock, press, left: false, moves: 10);
        stage.Layout();
        Assert.True(stage.HeaderX(1) > stage.HeaderX(0), "two is right of one while the finger drags");
        clock.At(press + 100 + 2 * Frame);
        stage.Layout();
        Assert.Equal(1, stage.Pivot.SelectedIndex);
        Assert.True(stage.HeaderX(1) < stage.HeaderX(0), "then jumps to the left");
        clock.At(2000);

        Swipe(stage, clock, 3006.25);
        clock.At(5000);
        Assert.Equal(0, stage.Pivot.SelectedIndex);
    }

    // #128: a change from code changes the selection at once and moves: back one from the second to the first, forward
    // (wrapping) from the last to the first.
    [AvaloniaTheory]
    [InlineData(1, 0, -1)]
    [InlineData(3, 0, 1)]
    [InlineData(0, 2, 2)]
    public void A_Change_From_Code_Moves_The_Shortest_Way_Back_Or_Forward(int from, int to, int pages)
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Pivot.SelectedIndex = from;
        stage.Pivot.Motion.Finish();
        stage.Pivot.SelectedIndex = to;
        Assert.Equal(to, stage.Pivot.SelectedIndex);
        Assert.True(stage.Pivot.Motion.IsMoving);
        clock.At(Frame * 4);
        stage.Layout();
        var motion = stage.Pivot.Motion;
        var travelled = motion.Lead == to ? motion.Offset + pages * Scene.ReferenceWidth : motion.Offset;
        Assert.Equal(Math.Sign(pages), Math.Sign(travelled));
        clock.At(2000);
        Assert.False(motion.IsMoving);
        Assert.True(stage.Shown(to));
    }

    // ---- Who takes the finger ----

    [AvaloniaFact]
    public void A_Mouse_Drag_Doesnt_Swipe()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Window.MouseDown(new Point(380, 450), MouseButton.Left);
        stage.Window.MouseMove(new Point(100, 450));
        stage.Window.MouseUp(new Point(100, 450), MouseButton.Left);
        clock.At(1000);
        Assert.Equal(0, stage.Pivot.SelectedIndex);
        Assert.Equal(0, stage.ItemX(0));
    }

    [AvaloniaFact]
    public void A_Drag_Up_An_Items_ScrollViewer_Scrolls_It_And_A_Swipe_Across_It_Swipes()
    {
        using var clock = PhoneClock();
        var list = new StackPanel();
        for (var i = 0; i < 40; i++) list.Children.Add(new TextBlock { Text = $"line {i}", Height = 40 });
        var scroll = new ScrollViewer { Content = list };
        var pivot = new Pivot { Title = "app" };
        pivot.Items.Add(new PivotItem { Header = "one", Content = scroll });
        pivot.Items.Add(new PivotItem { Header = "two", Content = new TextBlock { Text = "two" } });
        using var stage = new Stage(pivot: pivot);

        var touch = stage.Window.TouchBegin(new Point(200, 500), RawInputModifiers.None);
        for (var i = 1; i <= 10; i++) stage.Window.TouchMove(touch, new Point(200, 500 - 20 * i), RawInputModifiers.None);
        stage.Window.TouchEnd(touch, new Point(200, 300), RawInputModifiers.None);
        Assert.True(scroll.Offset.Y > 0);
        Assert.Equal(0, stage.ItemX(0));

        Swipe(stage, clock, 100);
        clock.At(2000);
        Assert.Equal(1, pivot.SelectedIndex);
    }

    [AvaloniaFact]
    public void A_Slider_In_An_Item_Keeps_Its_Drag()
    {
        using var clock = PhoneClock();
        var slider = new Slider { Maximum = 100, Width = 400, HorizontalAlignment = HorizontalAlignment.Left };
        var pivot = new Pivot { Title = "app" };
        pivot.Items.Add(new PivotItem { Header = "one", Content = slider });
        pivot.Items.Add(new PivotItem { Header = "two" });
        using var stage = new Stage(pivot: pivot);
        var at = slider.TranslatePoint(new Point(40, slider.Bounds.Height - 10), stage.Window)!.Value;
        var touch = stage.Window.TouchBegin(at, RawInputModifiers.None);
        for (var i = 1; i <= 10; i++) stage.Window.TouchMove(touch, at + new Point(25 * i, 0), RawInputModifiers.None);
        stage.Window.TouchEnd(touch, at + new Point(250, 0), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(0, pivot.SelectedIndex);
        Assert.True(slider.Value > 0);
    }

    // ---- IsLocked ----

    [AvaloniaFact]
    public void Locked_It_Neither_Swipes_Nor_Taps_And_The_Other_Headers_Go()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Pivot.IsLocked = true;
        Assert.Contains(":unselectedlocked", stage.Header(1).Classes);
        Assert.DoesNotContain(":unselectedlocked", stage.Header(0).Classes);

        Swipe(stage, clock, 100);
        clock.At(1000);
        Assert.Equal(0, stage.Pivot.SelectedIndex);
        Assert.Equal(0, stage.ItemX(0));
        Tap(stage, clock, 1100, HeaderCentre(stage, 1));
        clock.At(2000);
        Assert.Equal(0, stage.Pivot.SelectedIndex);

        stage.Pivot.SelectedIndex = 2;                                         // code still selects
        Assert.Equal(2, stage.Pivot.SelectedIndex);
    }

    // UnselectedLocked: the text at opacity 0 and PhonePivotLockedTranslation (40) to the right, over 0.33 s.
    [AvaloniaFact]
    public void A_Locked_Headers_Text_Goes_Right_And_Out_Over_A_Third_Of_A_Second()
    {
        using var stage = new Stage();
        var presenter = stage.Header(1).GetTemplateDescendants().OfType<ContentPresenter>().Single();
        stage.Pivot.IsLocked = true;
        var transitions = presenter.Transitions!;
        Assert.All(transitions, t => Assert.Equal(TimeSpan.FromSeconds(0.33), ((Avalonia.Animation.TransitionBase)t).Duration));
        Assert.Contains(transitions, t => t is DoubleTransition { Property.Name: "Opacity" });
        Assert.Contains(transitions, t => t is DoubleTransition { Property: { } p } && p == PhonePivotHeader.TranslationProperty);
        Assert.Equal(0, presenter.GetBaseValue(Visual.OpacityProperty));
        Assert.Equal(40d, presenter.GetBaseValue(PhonePivotHeader.TranslationProperty));

        stage.Pivot.IsLocked = false;
        Assert.Equal(1d, presenter.GetBaseValue(Visual.OpacityProperty).GetValueOrDefault(1d));
        Assert.DoesNotContain(":unselectedlocked", stage.Header(1).Classes);
    }

    [AvaloniaFact]
    public void The_Translation_Draws_The_Element_That_Far_Right()
    {
        var border = new Border();
        PhonePivotHeader.SetTranslation(border, 40);
        Assert.Equal(40, ((TranslateTransform)border.RenderTransform!).X);
        PhonePivotHeader.SetTranslation(border, 0);
        Assert.Null(border.RenderTransform);

        var apps = new ScaleTransform(2, 2);
        var other = new Border { RenderTransform = apps };
        PhonePivotHeader.SetTranslation(other, 40);
        PhonePivotHeader.SetTranslation(other, 0);
        Assert.Same(apps, other.RenderTransform);
    }

    // ---- The items' events ----

    [AvaloniaFact]
    public void The_Item_Events_Come_As_The_Items_Go_And_Come_In()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var log = new List<string>();
        var pivot = stage.Pivot;
        pivot.PivotItemLoading += (_, e) => log.Add($"loading {e.Item.Header} {clock.Now.TotalMilliseconds}");
        pivot.PivotItemLoaded += (_, e) => log.Add($"loaded {e.Item.Header} {clock.Now.TotalMilliseconds}");
        pivot.PivotItemUnloading += (_, e) => log.Add($"unloading {e.Item.Header} {clock.Now.TotalMilliseconds}");
        pivot.PivotItemUnloaded += (_, e) => log.Add($"unloaded {e.Item.Header} {clock.Now.TotalMilliseconds}");

        Tap(stage, clock, 1000, HeaderCentre(stage, 1), holdMs: 62.5);
        clock.At(3000);
        Assert.Equal(4, log.Count);
        Assert.StartsWith("unloading one 1062.5", log[0]);
        Assert.StartsWith("loading two 1062.5", log[1]);
        Assert.StartsWith("unloaded one", log[2]);
        Assert.StartsWith("loaded two", log[3]);
        Assert.True(double.Parse(log[2].Split(' ')[2], System.Globalization.CultureInfo.InvariantCulture) > 1062.5, "the new item comes in after the old has gone");
    }

    [AvaloniaFact]
    public void The_First_Item_Loads_When_The_Pivot_Is_Shown()
    {
        var pivot = Stage.Make(["one", "two"]);
        var log = new List<string>();
        pivot.PivotItemLoading += (_, e) => log.Add($"loading {e.Item.Header}");
        pivot.PivotItemLoaded += (_, e) => log.Add($"loaded {e.Item.Header}");
        using var stage = new Stage(pivot: pivot);
        Assert.Equal(["loading one", "loaded one"], log);
    }

    // ---- Interruptions (the first review) ----

    private static List<string> LogEvents(Pivot pivot)
    {
        var log = new List<string>();
        pivot.PivotItemLoading += (_, e) => log.Add($"loading {e.Item.Header}");
        pivot.PivotItemLoaded += (_, e) => log.Add($"loaded {e.Item.Header}");
        pivot.PivotItemUnloading += (_, e) => log.Add($"unloading {e.Item.Header}");
        pivot.PivotItemUnloaded += (_, e) => log.Add($"unloaded {e.Item.Header}");
        return log;
    }

    [AvaloniaFact]
    public void A_Change_From_Code_Mid_Move_Keeps_The_Item_Events_With_The_Item_Shown()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var log = LogEvents(stage.Pivot);
        Tap(stage, clock, 1000, HeaderCentre(stage, 1), holdMs: 62.5);
        clock.At(1062.5 + 2 * Frame);                                         // the old item still going out
        stage.Pivot.SelectedIndex = 2;
        clock.At(3000);
        Assert.True(stage.Shown(2));
        Assert.Equal("loaded three", log[^1]);
        Assert.Equal(log.Count(l => l.StartsWith("loaded")), log.Count(l => l.StartsWith("unloaded")));

        stage.Pivot.SelectedIndex = 3;
        clock.At(5000);
        Assert.Equal(["unloading three", "loading four", "unloaded three", "loaded four"], log.TakeLast(4));
    }

    [AvaloniaFact]
    public void An_Item_Inserted_Before_The_Selected_One_Raises_No_Item_Events_And_Keeps_It_Shown()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Pivot.SelectedIndex = 1;
        stage.Pivot.Motion.Finish();
        var log = LogEvents(stage.Pivot);
        var two = stage.Pivot.Items[1];
        stage.Pivot.Items.Insert(0, new PivotItem { Header = "zero", Content = new TextBlock { Text = "zero" } });
        clock.At(1000);
        stage.Layout();
        Assert.Same(two, stage.Pivot.SelectedItem);
        Assert.Equal(2, stage.Pivot.SelectedIndex);
        Assert.True(stage.Shown(2));
        Assert.Empty(log);
        Assert.Equal(5, stage.Strip.Children.Count);
        Assert.Equal(HeaderLeft, stage.HeaderX(2), 0.02);
    }

    [AvaloniaFact]
    public void A_Control_As_A_Header_Survives_The_Strip_Being_Made_Again()
    {
        var pivot = new Pivot();
        pivot.Items.Add(new PivotItem { Header = new TextBlock { Text = "one" } });
        pivot.Items.Add(new PivotItem { Header = "two" });
        using var stage = new Stage(pivot: pivot);
        pivot.Items.Add(new PivotItem { Header = "three" });
        Assert.Equal(3, stage.Strip.Children.Count);
        Assert.IsType<TextBlock>(stage.Header(0).Content);

        var direct = new Pivot();
        direct.Items.Add(new TextBlock { Text = "a page" });
        direct.Items.Add(new TextBlock { Text = "another" });
        using var other = new Stage(pivot: direct);
        Assert.Null(other.Header(0).Content);
    }

    [AvaloniaFact]
    public void A_Flick_Back_After_Half_A_Page_Stays()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        clock.At(10);
        var touch = stage.Window.TouchBegin(new Point(400, 450), RawInputModifiers.None);
        stage.Window.TouchMove(touch, new Point(100, 450), RawInputModifiers.None);  // 300 left
        for (var i = 1; i <= 5; i++)
        {
            clock.At(10 + 10 * i);
            stage.Window.TouchMove(touch, new Point(100 + 10 * i, 450), RawInputModifiers.None);  // back right, 1 px/ms
        }

        stage.Window.TouchEnd(touch, new Point(150, 450), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(0, stage.Pivot.SelectedIndex);
        Assert.Equal(0, stage.ItemX(0), 0.001);
    }

    [AvaloniaFact]
    public void A_Tap_On_The_Header_A_Swipe_Is_About_To_Select_Changes_Nothing_More()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        Swipe(stage, clock, 6.25);
        var at = HeaderCentre(stage, 1);
        clock.At(156.25 + 2 * Frame);                                          // before the selection, 5 frames on
        Assert.Equal(0, stage.Pivot.SelectedIndex);
        stage.Window.MouseDown(at, MouseButton.Left);
        stage.Window.MouseUp(at, MouseButton.Left);
        Assert.Equal(1, stage.Pivot.SelectedIndex);
        Assert.False(stage.Pivot.Motion.IsMoving);
    }

    [AvaloniaFact]
    public void Shown_Again_In_A_Window_The_Item_Loads_Again()
    {
        var pivot = Stage.Make(["one", "two"]);
        var log = LogEvents(pivot);
        using (new Stage(pivot: pivot))
        {
        }

        using var again = new Stage(pivot: pivot);
        Assert.Equal(["loading one", "loaded one", "loading one", "loaded one"], log);
    }

    [AvaloniaFact]
    public void A_Selection_Handler_That_Changes_The_Selection_Leaves_One_Move()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var redirected = false;
        stage.Pivot.SelectionChanged += (_, _) =>
        {
            if (redirected) return;
            redirected = true;
            stage.Pivot.SelectedIndex = 3;
        };
        Swipe(stage, clock, 6.25);
        clock.At(156.25 + 6 * Frame);                                         // the frame after the redirect
        Assert.True(stage.Shown(1), "the new move starts from the item the swipe chose, which goes out first");
        clock.At(3000);
        Assert.Equal(3, stage.Pivot.SelectedIndex);
        Assert.False(stage.Pivot.Motion.IsMoving);
        Assert.Equal(0, clock.Pending);
        Assert.True(stage.Shown(3));
    }

    // ---- The second review ----

    [AvaloniaFact]
    public void A_Control_As_A_Header_Survives_A_New_Template()
    {
        var pivot = new Pivot();
        pivot.Items.Add(new PivotItem { Header = new TextBlock { Text = "one" } });
        pivot.Items.Add(new PivotItem { Header = "two" });
        using var stage = new Stage(pivot: pivot);
        var template = pivot.Template!;
        pivot.Template = null;
        stage.Layout();
        pivot.Template = template;
        stage.Layout();
        Assert.IsType<TextBlock>(stage.Header(0).Content);
    }

    [AvaloniaFact]
    public void A_Tap_Whose_Selection_Handler_Selects_Again_Goes_Where_The_Handler_Said()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        var redirected = false;
        stage.Pivot.SelectionChanged += (_, _) =>
        {
            if (redirected) return;
            redirected = true;
            stage.Pivot.SelectedIndex = 3;
        };
        Tap(stage, clock, 1000, HeaderCentre(stage, 1));
        clock.At(3000);
        Assert.Equal(3, stage.Pivot.SelectedIndex);
        Assert.True(stage.Shown(3));
    }

    [AvaloniaFact]
    public void Removing_The_Selected_Item_Unloads_It_And_Goes_At_Once()
    {
        using var clock = PhoneClock();
        using var stage = new Stage();
        stage.Pivot.SelectedIndex = 1;
        stage.Pivot.Motion.Finish();
        var log = LogEvents(stage.Pivot);
        stage.Pivot.Items.RemoveAt(1);
        Assert.False(stage.Pivot.Motion.IsMoving);
        Assert.DoesNotContain("unloading three", log);
        clock.At(1000);
        stage.Layout();
        Assert.True(stage.Shown(stage.Pivot.SelectedIndex));
    }

    [AvaloniaFact]
    public void Equal_Data_Items_Are_Still_Selected_Anew()
    {
        using var clock = PhoneClock();
        var pivot = new Pivot { ItemsSource = new[] { "one", "two", "one" } };
        using var stage = new Stage(pivot: pivot);
        var log = new List<string>();
        pivot.PivotItemLoading += (_, e) => log.Add("loading");
        pivot.SelectedIndex = 2;
        Assert.True(pivot.Motion.IsMoving);
        Assert.Equal(["loading"], log);
    }

    [AvaloniaFact]
    public void The_First_Item_Loads_Once_With_The_Handler_Set_Before_The_Items()
    {
        var pivot = new Pivot();
        var log = LogEvents(pivot);
        pivot.Items.Add(new PivotItem { Header = "one" });
        pivot.Items.Add(new PivotItem { Header = "two" });
        using var stage = new Stage(pivot: pivot);
        Assert.Equal(["loading one", "loaded one"], log);
    }
}
