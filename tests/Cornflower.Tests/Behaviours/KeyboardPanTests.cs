using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    /// <summary>A page of text boxes at the given tops, with a fake keyboard for the window.</summary>
    private sealed class Page : IDisposable
    {
        private readonly Func<TopLevel, PhoneSuggestionList.IPhoneKeyboard?> _keyboardOf = PhoneSuggestionList.KeyboardOf;

        public Page(Func<Control, Control>? wrap = null, string? theme = null, bool simpleAlone = false, params double[] tops)
        {
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
                throw;
            }
        }

        public FakeKeyboard KeyboardFake { get; } = new();

        public List<TextBox> Boxes { get; } = [];

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Rect InWindow(Visual part) => new(part.TranslatePoint(default, Window)!.Value, part.Bounds.Size);

        /// <summary>As on the phone: the box takes the focus, then the keyboard comes up.</summary>
        public void Tap(TextBox box)
        {
            Assert.True(box.Focus(), "the box took no focus");
            Dispatcher.UIThread.RunJobs();
            if (!KeyboardFake.IsOpen)
                KeyboardFake.Show(Keyboard);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Scene.Dispose();
            PhoneSuggestionList.KeyboardOf = _keyboardOf;
        }
    }

    // A box the keyboard covers goes up until its bottom is at the keyboard's top, a tap on it still reaching it; the
    // keyboard going down brings the page back.
    [AvaloniaFact]
    public void A_TextBox_Under_The_Keyboard_Moves_Up_Until_It_Clears_It_And_Back_When_It_Goes()
    {
        using var page = new Page(tops: 600);
        var box = page.Boxes[0];
        var rest = page.InWindow(box);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        page.Tap(box);

        var moved = page.InWindow(box);
        Assert.Equal(Keyboard.Top, moved.Bottom, 0.01);
        Assert.Equal(rest.Bottom - Keyboard.Top, PhoneKeyboardPan.ShiftOf(page.Window), 0.01);
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
        Assert.Equal(Keyboard.Top, page.InWindow(page.Boxes[0]).Bottom, 0.01);
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

    // A box at the end of a ScrollViewer, which can scroll no further, moves with the page; the scroll stays.
    [AvaloniaFact]
    public void A_Box_In_A_ScrollViewer_Moves_With_The_Page()
    {
        ScrollViewer? scroll = null;
        using var page = new Page(wrap: c => scroll = new ScrollViewer { Content = c }, tops: 850);
        scroll!.Offset = new Vector(0, 100);
        Dispatcher.UIThread.RunJobs();
        page.Tap(page.Boxes[0]);

        Assert.Equal(Keyboard.Top, page.InWindow(page.Boxes[0]).Bottom, 0.01);
        Assert.Equal(100, scroll.Offset.Y, 0.01);
    }

    // A box in a popup (the phone's are in the overlay layer, a ContentDialog's too) moves nothing: #150 saw a focused
    // TextBox's dialog stay at the top. Nor does one in a popup that is a window of its own (a desktop's).
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Box_In_A_Popup_Moves_Nothing(bool overlay)
    {
        var inPopup = new TextBox { Width = 300, Margin = new Thickness(0, 600, 0, 0) };
        Popup? popup = null;
        using var page = new Page(wrap: c => new Panel
        {
            Children = { c, (popup = new Popup { ShouldUseOverlayLayer = overlay, Placement = PlacementMode.AnchorAndGravity, Child = inPopup }) },
        });
        popup!.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        var rest = inPopup.TranslatePoint(default, page.Window);

        page.Tap(inPopup);

        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(TopLevel.GetTopLevel(inPopup)!));
        Assert.Equal(rest, inPopup.TranslatePoint(default, page.Window));
    }

    // Under Simple alone the box is Avalonia's own: nothing moves.
    [AvaloniaFact]
    public void Simple_Alone_Moves_Nothing()
    {
        using var page = new Page(simpleAlone: true, tops: 600);
        var rest = page.InWindow(page.Boxes[0]);
        page.Tap(page.Boxes[0]);
        Assert.Equal(rest, page.InWindow(page.Boxes[0]));
        Assert.Equal(0, PhoneKeyboardPan.ShiftOf(page.Window));
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
    }
}
