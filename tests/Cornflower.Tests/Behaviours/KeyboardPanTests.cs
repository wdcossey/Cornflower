using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Tests.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.AutoSuggestBoxTests;

namespace Cornflower.Tests.Behaviours;

/// <summary>
/// <see cref="PhoneKeyboardPan"/>: while the keyboard is up, the window's content moves up so the focused box is not
/// under it (#179). The keyboard is #150's (y 453.33, 346.67 tall), through the same seam as the AutoSuggestBox's list.
/// docs/controls/KeyboardPan/PLAN.md has what is read and what is assumed.
/// </summary>
public class KeyboardPanTests
{
    private static readonly Rect Keyboard = AutoSuggestBoxReadings.Keyboard;

    /// <summary>
    /// A page of text boxes at the given tops, with a fake keyboard for the window. The page jumps, unless
    /// <paramref name="slide"/>: where it goes is held here, how it slides there by the slide's own tests.
    /// </summary>
    private sealed class Page : IDisposable
    {
        private readonly Func<TopLevel, PhoneSuggestionList.IPhoneKeyboard?> _keyboardOf = PhoneSuggestionList.KeyboardOf;
        private readonly ConfigScope _config = new();

        public Page(Func<Control, Control>? wrap = null, string? theme = null, bool simpleAlone = false, bool slide = false, params double[] tops)
        {
            if (!slide)
                CornflowerConfig.KeyboardPanSlide = TimeSpan.Zero;
            PhoneSuggestionList.KeyboardOf = _ => KeyboardFake;
            var canvas = new Canvas { Height = 900 };
            foreach (var top in tops)
            {
                var box = new TextBox { Width = 300 };
                Canvas.SetTop(box, top);
                Canvas.SetLeft(box, 19);
                canvas.Children.Add(box);
                Boxes.Add(box);
            }
            try
            {
                Scene = new Scene(wrap?.Invoke(canvas) ?? canvas, themes: simpleAlone ? [new SimpleTheme()] : null);
                if (theme is not null)
                {
                    foreach (var box in Boxes)
                        box.Theme = (ControlTheme)Window.FindResource(theme)!;
                    Dispatcher.UIThread.RunJobs();
                }
            }
            catch
            {
                PhoneSuggestionList.KeyboardOf = _keyboardOf;
                _config.Dispose();
                throw;
            }
        }

        public FakeKeyboard KeyboardFake { get; } = new();

        public List<TextBox> Boxes { get; } = [];

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Rect InWindow(Visual part) => new(part.TranslatePoint(default, Window)!.Value, part.Bounds.Size);

        /// <summary>The bottom of the caret's line in the window: the text presenter's lines down to the caret's.</summary>
        public double CaretBottom(TextBox box) => CaretBottomIn(box, Window);

        /// <summary>As on the phone: the box takes the focus, then the keyboard comes up.</summary>
        public void Tap(TextBox box)
        {
            Assert.True(box.Focus(), "the box took no focus");
            Dispatcher.UIThread.RunJobs();
            if (!KeyboardFake.IsOpen)
                KeyboardFake.Show(Keyboard);
            Dispatcher.UIThread.RunJobs();
        }

        public RoutedEventArgs Back()
        {
            var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            Window.RaiseEvent(request);
            Dispatcher.UIThread.RunJobs();
            return request;
        }

        public void Dispose()
        {
            Scene.Dispose();
            PhoneSuggestionList.KeyboardOf = _keyboardOf;
            _config.Dispose();
        }
    }

    /// <summary>The bottom of the caret's line, from the presenter's lines (the caret on the line its index falls in).</summary>
    internal static double CaretBottomIn(TextBox box, Visual window)
    {
        var presenter = box.GetVisualDescendants().OfType<TextPresenter>().Single(p => p.TemplatedParent == box);
        var lines = presenter.TextLayout.TextLines;
        var height = 0d;
        foreach (var line in lines)
        {
            height += line.Height;
            if (presenter.CaretIndex < line.FirstTextSourceIndex + line.Length || line == lines[^1])
                break;
        }
        return presenter.TranslatePoint(new Point(0, height), window)!.Value.Y;
    }

    // A box the keyboard covers goes up until its caret's line ends at the keyboard's top, its bottom left under the
    // keyboard (#200), a tap on it still reaching it; the keyboard going down brings the page back.
    [AvaloniaFact]
    public void A_TextBox_Under_The_Keyboard_Moves_Up_Until_Its_Caret_Clears_It_And_Back_When_It_Goes()
    {
        using var page = new Page(tops: 600);
        var box = page.Boxes[0];
        var rest = page.InWindow(box);
        var restCaret = page.CaretBottom(box);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        page.Tap(box);

        var moved = page.InWindow(box);
        Assert.Equal(Keyboard.Top, page.CaretBottom(box), 0.01);
        Assert.True(moved.Bottom > Keyboard.Top, "the box's bottom stays under the keyboard");
        Assert.Equal(restCaret - Keyboard.Top, PhoneKeyboardPan.ShiftOf(page.Window), 0.01);
        // The hit test is the compositor's: two frames for the move to reach it.
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.Same(box, (page.Window.InputHitTest(moved.Center) as Visual)?.GetSelfAndVisualAncestors().OfType<TextBox>().FirstOrDefault());

        page.KeyboardFake.Hide();

        Assert.Equal(rest, page.InWindow(box));
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
    }

    // The PasswordBox theme turns it on too.
    [AvaloniaFact]
    public void A_PasswordBox_Under_The_Keyboard_Moves_Up()
    {
        using var page = new Page(theme: "PasswordBox", tops: 600);
        page.Tap(page.Boxes[0]);
        Assert.Equal(Keyboard.Top, page.CaretBottom(page.Boxes[0]), 0.01);
    }

    // #200's read: a box 560 down (its border 598.75 to 637.92) moved 176.25 for the TextBox's keyboard (top 453.33)
    // and 116.25 for the PasswordBox's (513.75), the caret ending within a raw pixel of the keyboard's top and 8.33 and
    // 7.92 of the box left under it. Clearing the whole box would have taken 184.58 and 124.17.
    [AvaloniaTheory]
    [InlineData(null, 453.33, 176.25)]
    [InlineData("PasswordBox", 513.75, 116.25)]
    public void A_Covered_Box_Moves_As_The_Phones_Did(string? theme, double keyboardTop, double shift)
    {
        using var page = new Page(theme: theme, tops: 0);
        var box = page.Boxes[0];
        Canvas.SetTop(box, 598.75 - page.InWindow(box).Y);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(598.75, page.InWindow(box).Y, 0.01);
        page.KeyboardFake.Show(new Rect(0, keyboardTop, Keyboard.Width, 800 - keyboardTop));

        page.Tap(box);

        // Within a view pixel and a half: the phone's caret stopped 0.41 and 0.83 short of the keyboard (one and two raw
        // pixels), and ends 30.41 inside the border against our line's 30.28; ours stops at the keyboard, with no gap,
        // 0.55 and 0.97 less.
        Assert.Equal(shift, PhoneKeyboardPan.ShiftOf(page.Window), 1.5);
        Assert.Equal(keyboardTop, page.CaretBottom(box), 0.01);
    }

    // A box whose bottom the keyboard covers but whose caret it doesn't stays (#200: what moves is the least that shows
    // the caret).
    [AvaloniaFact]
    public void A_Box_Whose_Caret_Is_Above_The_Keyboard_Stays()
    {
        using var page = new Page(tops: 0);
        var box = page.Boxes[0];
        var below = page.InWindow(box).Bottom - page.CaretBottom(box);
        Assert.True(below > 2, "the box has room under its caret");
        Canvas.SetTop(box, Keyboard.Top - page.CaretBottom(box) - below / 2);
        Dispatcher.UIThread.RunJobs();
        Assert.True(page.InWindow(box).Bottom > Keyboard.Top && page.CaretBottom(box) < Keyboard.Top);

        page.Tap(box);

        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
    }

    // In a box of many lines, the caret's line is what clears the keyboard, not the box's last.
    [AvaloniaFact]
    public void A_Box_Of_Many_Lines_Moves_Until_The_Caret_Line_Clears_The_Keyboard()
    {
        using var page = new Page(tops: 600);
        var box = page.Boxes[0];
        box.AcceptsReturn = true;
        box.Text = "one" + Environment.NewLine + "two" + Environment.NewLine + "three" + Environment.NewLine + "four";
        box.CaretIndex = ("one" + Environment.NewLine + "tw").Length;
        Dispatcher.UIThread.RunJobs();
        var lines = page.InWindow(box).Bottom - page.CaretBottom(box);
        Assert.True(lines > 40, "the caret is on the second line of four");

        page.Tap(box);

        Assert.Equal(Keyboard.Top, page.CaretBottom(box), 0.01);
    }

    // A keyboard growing or shrinking (a strip of suggestions coming and going) moves the page afresh: the box sits on
    // the keyboard's top again, with no gap left.
    [AvaloniaFact]
    public void The_Keyboard_Changing_Height_Moves_The_Page_Afresh()
    {
        using var page = new Page(tops: 600);
        var box = page.Boxes[0];
        page.KeyboardFake.Show(new Rect(0, Keyboard.Top - 50, Keyboard.Width, Keyboard.Height + 50));
        page.Tap(box);
        Assert.Equal(Keyboard.Top - 50, page.CaretBottom(box), 0.01);

        page.KeyboardFake.Show(Keyboard);

        Assert.Equal(Keyboard.Top, page.CaretBottom(box), 0.01);
    }

    // The box the page moved for leaving the window, still focused, brings the page back.
    [AvaloniaFact]
    public void The_Box_Leaving_The_Window_Brings_The_Page_Back()
    {
        using var page = new Page(tops: [100, 600]);
        var rest = page.InWindow(page.Boxes[0]);
        page.Tap(page.Boxes[1]);
        Assert.True(PhoneKeyboardPan.ShiftOf(page.Window) > 0);

        ((Canvas)page.Boxes[1].Parent!).Children.Remove(page.Boxes[1]);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
        Assert.Equal(rest, page.InWindow(page.Boxes[0]));
    }

    // The content's own transform (an app's, a page's motion) is left alone: the move is the window's presenter's.
    [AvaloniaFact]
    public void The_Contents_Own_Transform_Is_Left_Alone()
    {
        var own = new TranslateTransform(5, 0);
        using var page = new Page(wrap: c => new Panel { RenderTransform = own, Children = { c } }, tops: 600);
        var rest = page.InWindow(page.Boxes[0]);

        page.Tap(page.Boxes[0]);

        Assert.Same(own, ((Control)page.Window.Content!).RenderTransform);
        Assert.Equal(rest.X, page.InWindow(page.Boxes[0]).X, 0.01);
        Assert.Equal(Keyboard.Top, page.CaretBottom(page.Boxes[0]), 0.01);
    }

    // The moved content is cut at the safe area's top (the window's top here: the headless window has no insets), so it
    // goes under Android's status bar unseen, as the phone's page under its system tray. The cut is taken away with the
    // move.
    [AvaloniaFact]
    public void The_Moved_Page_Is_Cut_At_The_Safe_Areas_Top()
    {
        using var page = new Page(tops: 600);
        var mover = Assert.IsAssignableFrom<Visual>(((Visual)page.Window.Content!).GetVisualParent());

        page.Tap(page.Boxes[0]);

        var cut = Assert.IsType<RectangleGeometry>(mover.Clip).Rect;
        Assert.Equal(0, mover.TranslatePoint(new Point(0, cut.Top), page.Window)!.Value.Y, 0.01);
        Assert.True(cut.Bottom > page.Window.ClientSize.Height + PhoneKeyboardPan.ShiftOf(page.Window));

        page.KeyboardFake.Hide();
        Assert.Null(mover.Clip);
    }

    // What the move leaves bare below the page shows the page's background, not the window's own (Simple's grey, before #234: a band
    // the Pixel showed between the page's end and the keyboard); the window's own comes back with the page.
    [AvaloniaFact]
    public void What_The_Move_Leaves_Bare_Shows_The_Pages_Background()
    {
        using var page = new Page(tops: 600);
        // Wp81's Window takes the page's background (#234), so the window's own is set apart here by a style, as an app's
        // theme may set it.
        page.Window.Styles.Add(new Style(x => x.OfType<Window>())
        {
            Setters = { new Setter(TemplatedControl.BackgroundProperty, page.Window.FindResource(page.Window.ActualThemeVariant, "ThemeBackgroundBrush")) },
        });
        Dispatcher.UIThread.RunJobs();
        var own = page.Window.Background;
        var pageBrush = Assert.IsAssignableFrom<ISolidColorBrush>(page.Window.FindResource(page.Window.ActualThemeVariant, PhoneKeyboardPan.PageBackground));
        Assert.NotEqual(pageBrush, own);

        page.Tap(page.Boxes[0]);

        using (var frame = page.Scene.Capture())
            Assert.Equal(pageBrush.Color, Frames.PixelAt(frame, 2, frame.PixelSize.Height - 2));

        page.KeyboardFake.Hide();
        Assert.Same(own, page.Window.Background);
    }

    // A box the keyboard leaves clear stays where it is.
    [AvaloniaFact]
    public void A_TextBox_Above_The_Keyboard_Stays()
    {
        using var page = new Page(tops: 100);
        var rest = page.InWindow(page.Boxes[0]);
        page.Tap(page.Boxes[0]);
        Assert.Equal(rest, page.InWindow(page.Boxes[0]));
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
    }

    // A pane over none of the window (Windows' touch keyboard beside or below it) moves nothing.
    [AvaloniaFact]
    public void A_Keyboard_Over_None_Of_The_Window_Moves_Nothing()
    {
        using var page = new Page(tops: 600);
        page.KeyboardFake.Show(new Rect(Scene.ReferenceWidth, Keyboard.Top, Keyboard.Width, Keyboard.Height));
        page.Tap(page.Boxes[0]);
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
    }

    // The focus going to another box with the keyboard up moves the page the least that shows it: none for one already
    // in view, and for one above the window's top, until its top is at the window's top. Off the boxes, once the focus
    // has settled, the page comes back.
    [AvaloniaFact]
    public void The_Focus_Moving_To_Another_Box_Moves_The_Page_The_Least()
    {
        using var page = new Page(tops: [100, 400, 700]);
        page.Tap(page.Boxes[2]);
        var shift = PhoneKeyboardPan.ShiftOf(page.Window);
        Assert.True(shift > 200);

        page.Tap(page.Boxes[1]);
        Assert.Equal(shift, PhoneKeyboardPan.ShiftOf(page.Window), 0.01);

        page.Tap(page.Boxes[0]);
        Assert.Equal(0, page.InWindow(page.Boxes[0]).Y, 0.01);

        page.Window.FocusManager!.Focus(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
    }

    // A tap on a box with the keyboard already up (from one box to another): the page moves for the line tapped, where
    // the press puts the caret, not for the line the caret was on before.
    [AvaloniaFact]
    public void A_Tap_Moves_The_Page_For_The_Line_It_Puts_The_Caret_On()
    {
        using var page = new Page(tops: [100, 600]);
        var box = page.Boxes[1];
        box.AcceptsReturn = true;
        box.Text = string.Join(Environment.NewLine, "one", "two", "three", "four");
        box.CaretIndex = box.Text.Length;
        page.Tap(page.Boxes[0]);
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var rest = page.InWindow(box);
        page.Window.MouseDown(new Point(rest.X + 30, rest.Y + 12), MouseButton.Left);
        page.Window.MouseUp(new Point(rest.X + 30, rest.Y + 12), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(box, page.Window.FocusManager!.GetFocusedElement());
        Assert.True(box.CaretIndex <= "one".Length, "the press put the caret on the first line");
        Assert.Equal(Keyboard.Top, page.CaretBottom(box), 0.01);
    }

    // A box at the end of a ScrollViewer, which can scroll no further, moves with the page; the scroll stays.
    [AvaloniaFact]
    public void A_Box_In_A_ScrollViewer_Moves_With_The_Page()
    {
        ScrollViewer? scroll = null;
        using var page = new Page(wrap: c => scroll = new ScrollViewer { Content = c }, tops: 850);
        scroll!.Offset = new Vector(0, 100);
        Dispatcher.UIThread.RunJobs();
        page.Tap(page.Boxes[0]);

        Assert.Equal(Keyboard.Top, page.CaretBottom(page.Boxes[0]), 0.01);
        Assert.Equal(100, scroll.Offset.Y, 0.01);
    }

    // A box in a popup (the phone's are in the overlay layer, a ContentDialog's too) moves nothing: #150 saw a focused
    // TextBox's dialog stay at the top. (A popup that is a window of its own, a desktop's, the headless platform never
    // makes.)
    [AvaloniaFact]
    public void A_Box_In_A_Popup_Moves_Nothing()
    {
        var inPopup = new TextBox { Width = 300, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
        Popup? popup = null;
        using var page = new Page(wrap: c => new Panel
        {
            Children = { c, (popup = new Popup { ShouldUseOverlayLayer = true, Placement = PlacementMode.AnchorAndGravity, Child = new Panel { Height = 780, Children = { inPopup } } }) },
        });
        popup!.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        var rest = inPopup.TranslatePoint(default, page.Window);
        Assert.True(rest!.Value.Y + inPopup.Bounds.Height > Keyboard.Top, "the keyboard covers the box");

        page.Tap(inPopup);

        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(TopLevel.GetTopLevel(inPopup)!));
        Assert.Equal(rest, inPopup.TranslatePoint(default, page.Window));
    }

    // Under Simple alone the box is Avalonia's own: nothing moves, and Back and the keyboard going leave its focus.
    [AvaloniaFact]
    public void Simple_Alone_Moves_Nothing()
    {
        using var page = new Page(simpleAlone: true, tops: 600);
        var rest = page.InWindow(page.Boxes[0]);
        page.Tap(page.Boxes[0]);
        Assert.Equal(rest, page.InWindow(page.Boxes[0]));
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));

        Assert.False(page.Back().Handled);
        page.KeyboardFake.Hide();
        Assert.True(page.Boxes[0].IsFocused);
    }

    // #150: focusing the AutoSuggestBox moved the page up until the control's top (its header's) was at the window's
    // top, the keyboard covering none of it; its list then hangs from the box and runs down to the keyboard.
    [AvaloniaFact]
    public void The_AutoSuggestBox_Goes_Up_Until_Its_Top_Is_At_The_Windows_Top_And_Its_List_Follows()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(keyboard: keyboard);
        var rest = stage.InWindow(stage.Box).Y;
        Assert.True(rest > 0);

        keyboard.Show(Keyboard);
        stage.Open(12);

        Assert.Equal(0, stage.InWindow(stage.Box).Y, 0.01);
        Assert.Equal(rest, PhoneKeyboardPan.ShiftOf(stage.Window), 0.01);
        var list = stage.InWindow(stage.List);
        Assert.Equal(AutoSuggestBoxReadings.ListTop, list.Y, 0.001);
        Assert.Equal(Keyboard.Top - PhoneSuggestionList.KeyboardGap, list.Bottom, 0.01);
    }

    // The list open before the keyboard comes up (a box focused with none, then one shown): as the page moves, the list
    // is hung from the box again and sized down to the keyboard.
    [AvaloniaFact]
    public void The_AutoSuggestBoxs_List_Follows_The_Page_When_The_Keyboard_Comes_Up_After_It_Opened()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(keyboard: keyboard);
        stage.Open(12);

        keyboard.Show(Keyboard);
        stage.Settle();

        var list = stage.InWindow(stage.List);
        Assert.Equal(AutoSuggestBoxReadings.ListTop, list.Y, 0.001);
        Assert.Equal(Keyboard.Top - PhoneSuggestionList.KeyboardGap, list.Bottom, 0.01);

        // Again with a list its max holds to one height, so that nothing is laid out again: the page's move alone tells it.
        keyboard.Hide();
        stage.Box.MaxDropDownHeight = 200;
        stage.Open(12);
        keyboard.Show(Keyboard);
        stage.Settle();
        Assert.Equal(AutoSuggestBoxReadings.ListTop, stage.InWindow(stage.List).Y, 0.001);
    }

    // AutoMaximizeSuggestionArea false (#200): the page doesn't move for an AutoSuggestBox the keyboard doesn't cover, as
    // for a TextBox.
    [AvaloniaFact]
    public void An_AutoSuggestBox_Not_Maximized_And_Not_Covered_Stays()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(keyboard: keyboard);
        AutoCompleteBoxOptions.SetAutoMaximizeSuggestionArea(stage.Box, false);

        keyboard.Show(Keyboard);
        stage.Open(12);

        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(stage.Window));
    }

    // #205's read, the control 572.08 to 637.92 down, the keyboard's top 453.33: focused, the page moves 184.58, the box's
    // bottom border to the keyboard's top (not its caret's line, as a TextBox's: 176.25); with suggestions, the list opens
    // above the box, its bottom border on the box's top border (406.25), 151.25 tall for three and 387.5 for twelve (from
    // 18.75), and the page moves 10.42 more, 195 in all.
    [AvaloniaTheory]
    [InlineData(3, AutoSuggestBoxReadings.ThreeTall)]
    [InlineData(12, AutoSuggestBoxReadings.TwelveTall)]
    public void A_Covered_AutoSuggestBox_Not_Maximized_Moves_Its_Box_Above_The_Keyboard_And_Opens_Its_List_Above(int n, double tall)
    {
        var keyboard = new FakeKeyboard();
        var page = new Border();
        using var stage = new Stage(keyboard: keyboard, wrap: inner => { page.Child = inner; return page; });
        AutoCompleteBoxOptions.SetAutoMaximizeSuggestionArea(stage.Box, false);
        page.Padding = new Thickness(0, 572.08 - stage.InWindow(stage.Box).Y, 0, 0);
        stage.Settle();
        Assert.Equal(637.92, stage.InWindow(stage.TextBox).Bottom, 0.01);

        keyboard.Show(Keyboard);
        stage.Focus();

        Assert.Equal(184.58, PhoneKeyboardPan.ShiftOf(stage.Window), 0.01);
        Assert.Equal(Keyboard.Top, stage.InWindow(stage.TextBox).Bottom, 0.01);

        stage.Open(n);

        Assert.True(stage.Box.IsDropDownOpen, "the list stays open");
        Assert.Equal(195, PhoneKeyboardPan.ShiftOf(stage.Window), 0.01);
        var box = stage.InWindow(stage.TextBox);
        Assert.Equal(Keyboard.Top - PhoneKeyboardPan.ListAboveLift, box.Bottom, 0.01);
        var border = stage.InWindow(stage.TextBox.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "BorderElement"));
        Assert.Equal(403.75, border.Top, 0.01);
        var list = stage.InWindow(stage.List);
        Assert.Equal(border.Top + AutoSuggestBoxReadings.ListBorder, list.Bottom, 0.01);
        Assert.Equal(tall, list.Height, 0.01);
    }

    // A box not maximized in a popup, which the page's move doesn't take: its list doesn't open above it (where the
    // keyboard could still cover it), but runs down past the keyboard as before.
    [AvaloniaFact]
    public void A_Box_Not_Maximized_In_A_Popup_Keeps_Its_List_Below()
    {
        var keyboard = new FakeKeyboard();
        var inPopup = new AutoCompleteBox { Width = 300, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom, MinimumPopulateDelay = TimeSpan.Zero };
        AutoCompleteBoxOptions.SetAutoMaximizeSuggestionArea(inPopup, false);
        Popup? popup = null;
        using var stage = new Stage(keyboard: keyboard, wrap: c => new Panel
        {
            Children = { c, (popup = new Popup { ShouldUseOverlayLayer = true, Placement = PlacementMode.AnchorAndGravity, Child = new Panel { Height = 700, Children = { inPopup } } }) },
        });
        popup!.IsOpen = true;
        stage.Settle();
        keyboard.Show(Keyboard);
        var part = inPopup.GetVisualDescendants().OfType<TextBox>().First();
        Assert.True(stage.InWindow(part).Bottom > Keyboard.Top, "the keyboard covers the box");

        Assert.True(part.Focus());
        inPopup.ItemsSource = AutoSuggestBoxReadings.Fruits.Take(3).ToList();
        inPopup.Text = "a";
        inPopup.IsDropDownOpen = true;
        stage.Settle();

        Assert.True(inPopup.IsDropDownOpen, "the list stays open");
        Assert.Equal(PlacementMode.Bottom, inPopup.GetVisualDescendants().OfType<Popup>().Single(p => p.Name == "PART_Popup").Placement);
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(stage.Window));
    }

    // The slide (#205's recording): up over 300 ms on an ease-out, from the keyboard's change; down over 200 on an
    // ease-in, as it goes; the box where the slide has the page at each frame; at rest, the page given back.
    [AvaloniaFact]
    public void The_Page_Slides_Up_On_An_Ease_Out_And_Down_On_An_Ease_In()
    {
        using var page = new Page(slide: true, tops: 600);
        using var clock = new TestClock();
        var box = page.Boxes[0];
        var rest = page.InWindow(box).Y;
        Assert.Equal(TimeSpan.FromMilliseconds(300), CornflowerConfig.KeyboardPanSlide);

        page.Tap(box);
        var shift = PhoneKeyboardPan.ShiftOf(page.Window);
        Assert.True(shift > 0);
        Assert.Equal(0, PhoneKeyboardPan.ShownShiftOf(page.Window));
        Assert.Equal(rest, page.InWindow(box).Y, 0.01);

        foreach (var at in new[] { 16, 96, 192, 288 })
        {
            clock.At(at);
            var shown = shift * PhoneEase.ExponentialOut(PhoneKeyboardPan.UpExponent, at / 300d);
            Assert.Equal(shown, PhoneKeyboardPan.ShownShiftOf(page.Window), 1e-6);
            Assert.Equal(rest - shown, page.InWindow(box).Y, 0.01);
        }
        clock.At(304);
        Assert.Equal(shift, PhoneKeyboardPan.ShownShiftOf(page.Window));
        Assert.Equal(Keyboard.Top, page.CaretBottom(box), 0.01);

        clock.At(400);
        page.KeyboardFake.Hide();
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
        Assert.Equal(shift, PhoneKeyboardPan.ShownShiftOf(page.Window));
        foreach (var at in new[] { 16, 96, 192 })
        {
            clock.At(400 + at);
            var shown = shift * (1 - PhoneEase.ExponentialIn(PhoneKeyboardPan.DownExponent, at / 200d));
            Assert.Equal(shown, PhoneKeyboardPan.ShownShiftOf(page.Window), 1e-6);
            Assert.Equal(rest - shown, page.InWindow(box).Y, 0.01);
        }
        clock.At(608);
        Assert.Equal(0, PhoneKeyboardPan.ShownShiftOf(page.Window));
        Assert.Equal(rest, page.InWindow(box).Y);
        Assert.Null(page.Scene.Window.Presenter!.RenderTransform);
        Assert.Equal(0, clock.Pending);
    }

    // The config's slide: its time is read as a slide starts; 0 or less, or Animated off, and the page jumps both ways.
    [AvaloniaTheory]
    [InlineData(150, true)]
    [InlineData(0, true)]
    [InlineData(-100, true)]
    [InlineData(300, false)]
    public void The_Slide_Takes_The_Configs_Time_And_Zero_Or_Less_Or_Animated_Off_Jumps(double milliseconds, bool animated)
    {
        using var page = new Page(slide: true, tops: 600);
        using var clock = new TestClock();
        CornflowerConfig.KeyboardPanSlide = TimeSpan.FromMilliseconds(milliseconds);
        CornflowerConfig.Animated = animated;
        var jumps = milliseconds <= 0 || !animated;

        page.Tap(page.Boxes[0]);
        var shift = PhoneKeyboardPan.ShiftOf(page.Window);
        Assert.Equal(jumps ? shift : 0, PhoneKeyboardPan.ShownShiftOf(page.Window));
        if (!jumps)
        {
            clock.At(64);
            Assert.Equal(shift * PhoneEase.ExponentialOut(PhoneKeyboardPan.UpExponent, 64 / milliseconds), PhoneKeyboardPan.ShownShiftOf(page.Window), 1e-6);
            clock.At(milliseconds + 16);
            Assert.Equal(shift, PhoneKeyboardPan.ShownShiftOf(page.Window));
        }

        page.KeyboardFake.Hide();
        Assert.Equal(jumps ? 0 : shift, PhoneKeyboardPan.ShownShiftOf(page.Window));
        if (!jumps)
        {
            clock.At(milliseconds + 16 + milliseconds * PhoneKeyboardPan.DownShare + 16);
            Assert.Equal(0, PhoneKeyboardPan.ShownShiftOf(page.Window));
        }
    }

    // A keyboard that changes midway aims from where the box rests, not where the slide has it; a slide turned midway
    // (the box tapped again as the page slides down) goes back from where the page is.
    [AvaloniaFact]
    public void A_Slide_Changed_Midway_Goes_From_Where_The_Page_Is()
    {
        using var page = new Page(slide: true, tops: 600);
        using var clock = new TestClock();
        var box = page.Boxes[0];
        var restCaret = page.CaretBottom(box);
        page.Tap(box);
        clock.At(96);
        var midway = PhoneKeyboardPan.ShownShiftOf(page.Window);
        Assert.True(midway > 0);

        page.KeyboardFake.Show(new Rect(0, 500, Keyboard.Width, 300));
        Assert.Equal(restCaret - 500, PhoneKeyboardPan.ShiftOf(page.Window), 0.01);
        // Aimed again the same way: it ends when the first slide would have, at 300 ms, not a whole slide later.
        clock.At(304);
        Assert.Equal(500, page.CaretBottom(box), 0.01);

        page.KeyboardFake.Hide();
        clock.At(448);
        midway = PhoneKeyboardPan.ShownShiftOf(page.Window);
        Assert.True(midway > 0 && midway < restCaret - 500);
        page.Tap(box);
        var shift = PhoneKeyboardPan.ShiftOf(page.Window);
        Assert.Equal(restCaret - Keyboard.Top, shift, 0.01);
        clock.At(464);
        Assert.Equal(midway + (shift - midway) * PhoneEase.ExponentialOut(PhoneKeyboardPan.UpExponent, 16 / 300d),
            PhoneKeyboardPan.ShownShiftOf(page.Window), 1e-6);
    }

    // The list opened as the page starts to slide up for a maximized box low on the page: sized for where the slide ends
    // (down to the keyboard), not where the box is now, under the keyboard, where it would have no room and close.
    [AvaloniaFact]
    public void The_List_Is_Sized_For_Where_The_Slide_Ends()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(keyboard: keyboard, slide: true, wrap: page => new Border { Padding = new Thickness(0, 700, 0, 0), Child = page });
        using var clock = new TestClock();
        keyboard.Show(Keyboard);

        stage.Open(12);

        Assert.True(stage.Box.IsDropDownOpen, "the list stays open");
        Assert.True(PhoneKeyboardPan.ShownShiftOf(stage.Window) < PhoneKeyboardPan.ShiftOf(stage.Window));
        Assert.Equal(AutoSuggestBoxReadings.TwelveTall, stage.List.MaxHeight, 0.01);
        clock.At(160);
        stage.Settle();
        Assert.Equal(AutoSuggestBoxReadings.TwelveTall, stage.List.MaxHeight, 0.01);
        clock.At(320);
        stage.Settle();
        Assert.Equal(AutoSuggestBoxReadings.TwelveTall, stage.List.MaxHeight, 0.01);
        Assert.Equal(AutoSuggestBoxReadings.ListTop, stage.InWindow(stage.List).Y, 0.01);
    }

    // A keyboard that leaves the list just one suggestion's room, read back a hair short (arithmetic's error), still
    // counts, rather than pass it over and run the list down under it.
    [AvaloniaFact]
    public void A_Keyboard_A_Hair_Short_Of_One_Suggestions_Room_Still_Holds_The_List()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(keyboard: keyboard);
        keyboard.Show(Keyboard);
        stage.Open(12);
        var listTop = stage.InWindow(stage.List).Y;

        var top = listTop + PhoneSuggestionList.KeyboardGap + PhoneSuggestionList.OneSuggestion - 1e-9;
        keyboard.Show(new Rect(0, top, Keyboard.Width, 800 - top));
        stage.Settle();

        Assert.True(stage.Box.IsDropDownOpen, "the list stays open");
        Assert.Equal(top - PhoneSuggestionList.KeyboardGap, stage.InWindow(stage.List).Bottom, 0.01);
    }

    // #150: the phone's first Back hides the keyboard and takes the focus from the box; the next goes on. On Android
    // the keyboard takes that Back and Avalonia never sees it (#197), so the keyboard going down takes the focus, and a
    // Back with the page's own handler left to go on. The keyboard changing while up takes nothing.
    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("PasswordBox")]
    public void The_Keyboard_Going_Down_Takes_The_Focus_And_The_Next_Back_Goes_On(string? theme)
    {
        using var page = new Page(theme: theme, tops: 600);
        var box = page.Boxes[0];
        page.Tap(box);
        page.KeyboardFake.Show(new Rect(0, 500, Keyboard.Width, 300));
        Assert.True(box.IsFocused, "a keyboard that changes leaves the focus");

        page.KeyboardFake.Hide();

        Assert.False(box.IsKeyboardFocusWithin);
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
        Assert.False(page.Back().Handled, "the next Back is the page's");
    }

    // On Android the keyboard can be up before the window's first box has the focus (the pan made then): its going still
    // takes the focus.
    [AvaloniaFact]
    public void A_Keyboard_Up_Before_The_First_Focus_Takes_It_As_It_Goes()
    {
        using var page = new Page(tops: 600);
        var box = page.Boxes[0];
        page.KeyboardFake.Show(Keyboard);
        Assert.True(box.Focus());
        Dispatcher.UIThread.RunJobs();

        page.KeyboardFake.Hide();

        Assert.False(box.IsKeyboardFocusWithin);
    }

    // A Back Avalonia does see while a box has the focus (no keyboard up) is the phone's first Back: it takes the focus
    // and is taken; the next is the page's.
    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("PasswordBox")]
    public void Back_With_A_Box_Focused_Takes_The_Focus_Then_The_Next_Goes_On(string? theme)
    {
        using var page = new Page(theme: theme, tops: 100);
        var box = page.Boxes[0];
        Assert.True(box.Focus());
        Dispatcher.UIThread.RunJobs();

        Assert.True(page.Back().Handled, "the first Back is the box's");
        Assert.False(box.IsKeyboardFocusWithin);
        Assert.False(page.Back().Handled, "the next Back is the page's");
    }

    // The keyboard going as the focus comes to a read-only box (which asks for none) or to a box not ours, from a box of
    // ours, leaves the focus there.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_Keyboard_Going_With_A_Read_Only_Box_Or_One_Not_Ours_Focused_Takes_Nothing(bool readOnly)
    {
        using var page = new Page(tops: [600, 100]);
        var other = page.Boxes[1];
        if (readOnly)
            other.IsReadOnly = true;
        else
            PhoneKeyboardPan.SetIsEnabled(other, false);
        page.Tap(page.Boxes[0]);
        Assert.True(other.Focus());
        Dispatcher.UIThread.RunJobs();

        page.KeyboardFake.Hide();

        Assert.True(other.IsFocused);
    }

    // A box under an open popup leaves Back to the popup (the focus put back on the page's box while a Flyout is open).
    [AvaloniaFact]
    public void Back_With_A_Box_Under_An_Open_Popup_Is_The_Popups()
    {
        var opener = new Button { Content = "more" };
        var flyout = new Flyout { Content = new TextBlock { Text = "over the page" } };
        using var page = new Page(wrap: c => new StackPanel { Children = { opener, c } }, tops: 100);
        var box = page.Boxes[0];
        flyout.ShowAt(opener);
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.Focus());
        Dispatcher.UIThread.RunJobs();
        Assert.True(flyout.IsOpen);

        Assert.True(page.Back().Handled);

        Assert.True(box.IsFocused, "Back was the popup's");
    }

    // The keyboard going down with the focus on something not a text box leaves it there.
    [AvaloniaFact]
    public void The_Keyboard_Going_Down_With_The_Focus_Elsewhere_Takes_Nothing()
    {
        var other = new Button { Content = "other" };
        using var page = new Page(wrap: c => new StackPanel { Children = { other, c } }, tops: 600);
        page.Tap(page.Boxes[0]);
        Assert.True(other.Focus());
        Dispatcher.UIThread.RunJobs();

        page.KeyboardFake.Hide();

        Assert.True(other.IsFocused);
    }

    // #150: "in a dialog with a focused TextBox, the first Back hid the keyboard and took the focus from the TextBox, the
    // dialog staying; the second closed it". The keyboard going down (Android's) or a Back the app sees, alike.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void In_A_Dialog_The_First_Back_Takes_The_Focus_And_The_Second_Closes_It(bool keyboardTakesBack)
    {
        var keyboardOf = PhoneSuggestionList.KeyboardOf;
        var keyboard = new FakeKeyboard();
        PhoneSuggestionList.KeyboardOf = _ => keyboard;
        try
        {
            var box = new TextBox { Width = 300 };
            using var stage = new ContentDialogTests.Stage(content: box);
            stage.ShowAtRest();
            Assert.True(box.Focus());
            Dispatcher.UIThread.RunJobs();
            keyboard.Show(Keyboard);

            if (keyboardTakesBack)
                keyboard.Hide();
            else
                stage.Back();

            Assert.False(box.IsKeyboardFocusWithin);
            Assert.True(stage.Dialog.Popup!.IsOpen, "the dialog stays");
            stage.Back();
            Assert.True(stage.Result!.IsCompleted, "the second Back closes the dialog");
        }
        finally
        {
            PhoneSuggestionList.KeyboardOf = keyboardOf;
        }
    }
}
