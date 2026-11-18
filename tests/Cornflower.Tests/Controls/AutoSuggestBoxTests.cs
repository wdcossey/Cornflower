using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.AutoSuggestBoxReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's AutoSuggestBox as Avalonia's AutoCompleteBox: Wp81's theme (<c>Controls/AutoCompleteBox.axaml</c>) and
/// <see cref="PhoneSuggestionList"/>, against #150's screen grabs and events (<see cref="AutoSuggestBoxReadings"/>).
/// docs/controls/AutoSuggestBox/PLAN.md has each reading.
/// </summary>
public class AutoSuggestBoxTests
{
    /// <summary>
    /// The reference app's popup stage: the specimen in a slot 19 in and 12 down, or, <paramref name="atTop"/>, as the
    /// phone draws it focused, the page moved up so the control's top is at the window's top (its TextBox part at 0).
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Func<TopLevel, PhoneSuggestionList.IPhoneKeyboard?> _keyboardOf;

        public Stage(ThemeVariant? variant = null, bool atTop = false, IEnumerable<IStyle>? themes = null,
            FakeKeyboard? keyboard = null, Func<Control, Control>? wrap = null)
        {
            _keyboardOf = PhoneSuggestionList.KeyboardOf;
            if (keyboard is not null)
                PhoneSuggestionList.KeyboardOf = _ => keyboard;
            Box = new AutoCompleteBox { PlaceholderText = Placeholder };
            ControlHeader.SetHeader(Box, Header);
            Slot = new Grid { Margin = atTop ? new Thickness(AutoSuggestBoxReadings.Slot.Left, -Margin.Top, AutoSuggestBoxReadings.Slot.Right, 0) : AutoSuggestBoxReadings.Slot, Children = { Box } };
            var page = new Grid { Children = { Slot } };
            page.Bind(Panel.BackgroundProperty, page.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            try
            {
                Scene = new Scene(wrap?.Invoke(page) ?? page, variant, themes: themes);
            }
            catch
            {
                PhoneSuggestionList.KeyboardOf = _keyboardOf;
                throw;
            }
        }

        public AutoCompleteBox Box { get; }

        public Grid Slot { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public TextBox TextBox => Part<TextBox>(Box, "PART_TextBox");

        public Popup Popup => Part<Popup>(Box, "PART_Popup");

        public Border List => Assert.IsType<Border>(Popup.Child);

        public ListBox Items => Part<ListBox>(List, "PART_SelectingItemsControl");

        public List<ListBoxItem> Rows => Items.GetVisualDescendants().OfType<ListBoxItem>().ToList();

        /// <summary>Focuses the box's TextBox part, as the reference app does.</summary>
        public void Focus()
        {
            Assert.True(TextBox.Focus(), "the TextBox part took no focus");
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// As the reference app opens it: focused, its text, the first <paramref name="n"/> fruits and the list open. With
        /// no delay before the list asks for them, which has its own test.
        /// </summary>
        public void Open(int n, string text = "a")
        {
            Box.MinimumPopulateDelay = TimeSpan.Zero;
            Focus();
            Box.ItemsSource = Fruits.Take(n).ToList();
            Box.Text = text;
            Box.IsDropDownOpen = true;
            Settle();
        }

        public void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>A part's bounds in the window.</summary>
        public Rect InWindow(Visual part) => new(part.TranslatePoint(default, Window)!.Value, part.Bounds.Size);

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
        }
    }

    /// <summary>A keyboard a test brings up and puts down, as Avalonia's input pane on a phone.</summary>
    internal sealed class FakeKeyboard : PhoneSuggestionList.IPhoneKeyboard
    {
        public bool IsOpen { get; private set; }

        public Rect Occluded { get; private set; }

        public event EventHandler? Changed;

        public void Show(Rect at)
        {
            IsOpen = true;
            Occluded = at;
            Changed?.Invoke(this, EventArgs.Empty);
            Dispatcher.UIThread.RunJobs();
        }

        public void Hide()
        {
            IsOpen = false;
            Occluded = default;
            Changed?.Invoke(this, EventArgs.Empty);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() { }
    }

    private static T Part<T>(Visual of, string name) where T : Control =>
        of.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static string PixelHex(WriteableBitmap frame, int x, int y) => Tokens.Token.Hex(Frames.PixelAt(frame, x, y));

    /// <summary>Real time, in a pushed frame, until <paramref name="done"/> or the limit (the populate delay is Avalonia's own timer).</summary>
    private static void RunFor(double limitMs, Func<bool>? done = null)
    {
        var frame = new DispatcherFrame();
        var watch = Stopwatch.StartNew();
        var poll = new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Background, (_, _) =>
        {
            if (watch.Elapsed.TotalMilliseconds >= limitMs || done?.Invoke() == true)
                frame.Continue = false;
        });
        poll.Start();
        Dispatcher.UIThread.PushFrame(frame);
        poll.Stop();
    }

    // autosuggestbox.json's template: a TextBox part (the phone's TextBox, its header the control's), and a Popup of a
    // Border of a ListView. An AutoCompleteBox gets them from Wp81's theme, its TextBox Wp81's, not Simple's.
    [AvaloniaFact]
    public void The_Box_Is_Templated_By_The_Theme_Its_TextBox_The_Phones()
    {
        using var stage = new Stage();

        var theme = Assert.IsType<ControlTheme>(stage.Window.FindResource(typeof(AutoCompleteBox)));
        var template = theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
        Assert.Same(template, stage.Box.Template);
        Assert.True(PhoneSuggestionList.GetIsEnabled(stage.Box));
        Assert.Null(stage.TextBox.Theme);
        Assert.Contains(stage.TextBox.GetVisualDescendants(), v => v is Border { Name: "BorderElement" });
        Assert.Equal(new Thickness(0), stage.TextBox.Margin);
        Assert.Contains(stage.TextBox.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == Header);
        Assert.Equal(Placeholder, stage.TextBox.PlaceholderText);
        Assert.True(stage.Popup.ShouldUseOverlayLayer);
    }

    // The style's setters (Margin, VerticalAlignment), and what the phone's control does that Avalonia's doesn't by
    // default: it doesn't filter, it asks the app at every change, and it asks after the phone's 150 ms.
    [AvaloniaFact]
    public void The_Style_Sets_What_The_Phone_Does()
    {
        using var stage = new Stage();
        var box = stage.Box;

        Assert.Equal(Margin, box.Margin);
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Top, box.VerticalAlignment);
        Assert.Equal(AutoCompleteFilterMode.None, box.FilterMode);
        Assert.Equal(0, box.MinimumPrefixLength);
        Assert.Equal(CueAfterKey, box.MinimumPopulateDelay.TotalMilliseconds);
        Assert.Same(box.FindResource(box.ActualThemeVariant, "TextBoxPlaceholderTextThemeBrush"), stage.TextBox.PlaceholderForeground);
        Assert.Null(box.FocusAdorner);
    }

    // At rest the box is the phone's: its border from 48.33 to 87.5, 19.17 in (the rest grab's rows 116 to 209).
    [AvaloniaFact]
    public void At_Rest_The_Box_Is_Where_The_Phones_Is()
    {
        using var stage = new Stage();
        var border = stage.InWindow(Part<Border>(stage.TextBox, "BorderElement"));

        Assert.Equal(BoxLeft, border.X, 0.001);
        Assert.Equal(RestBoxTop, border.Y, 0.001);
        Assert.Equal(RestBoxBottom, border.Bottom, 0.001);
    }

    // The phone has one window: the list is drawn in it, in its overlay layer, not in a window of its own.
    [AvaloniaFact]
    public void The_List_Is_In_The_Windows_Own_Overlay_Layer()
    {
        using var stage = new Stage();
        stage.Open(3);

        Assert.True(stage.Popup.IsOpen);
        Assert.Contains(stage.List.GetVisualAncestors(), a => a is OverlayPopupHost);
        Assert.Same(stage.Window, TopLevel.GetTopLevel(stage.List));
    }

    // The list hangs from the box: its top is the box's bottom border (63.33, the two borders on each other), the box's
    // width; one suggestion makes it 53.75 tall, three 151.25; rows 39.17 tall, 48.75 apart, the first 12.08 below its
    // top, the phone's ListViewItems at 20.26 in black.
    [AvaloniaTheory]
    [InlineData(1, OneTall)]
    [InlineData(3, ThreeTall)]
    public void The_List_Hangs_From_The_Boxs_Bottom_Border_As_Tall_As_Its_Rows(int n, double tall)
    {
        using var stage = new Stage(atTop: true);
        stage.Open(n);
        var box = stage.InWindow(Part<Border>(stage.TextBox, "BorderElement"));
        var list = stage.InWindow(stage.List);

        Assert.Equal(FocusedBoxTop, box.Y, 0.001);
        Assert.Equal(FocusedBoxBottom, box.Bottom, 0.001);
        Assert.Equal(ListTop, list.Y, 0.001);
        Assert.Equal(box.X, list.X, 0.001);
        Assert.Equal(box.Width, list.Width, 0.001);
        Assert.Equal(tall, list.Height, 0.001);
        Assert.Equal(ListBorder, stage.List.BorderThickness.Top);

        var rows = stage.Rows;
        Assert.Equal(n, rows.Count);
        for (var i = 0; i < n; i++)
        {
            var row = stage.InWindow(rows[i]);
            Assert.Equal(ListTop + FirstRow + i * RowPitch, row.Y, 0.01);
            Assert.Equal(RowHeight, row.Height, 0.001);
            Assert.Equal(ItemMargin, rows[i].Margin);
            Assert.Equal(ItemFontSize, rows[i].FontSize);
            Assert.Equal(Black, Tokens.Token.Hex(Assert.IsAssignableFrom<ISolidColorBrush>(rows[i].Foreground).Color));
            Assert.True(Tilt.GetIsEnabled(rows[i]), "the phone's ListViewItem tilts");
        }
    }

    // Twelve suggestions run the list down to 2.5 above the keyboard's top (450.83, the keyboard at 453.33): 387.5 tall,
    // the rest scrolled to.
    [AvaloniaFact]
    public void Twelve_Fill_The_List_Down_To_The_Keyboard()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(atTop: true, keyboard: keyboard);
        keyboard.Show(Keyboard);
        stage.Open(12);
        var list = stage.InWindow(stage.List);

        Assert.Equal(ListTop, list.Y, 0.001);
        Assert.Equal(TwelveTall, list.Height, 0.01);
        Assert.Equal(Keyboard.Top - KeyboardGap, list.Bottom, 0.01);
        var scroll = Part<ScrollViewer>(stage.Items, "PART_ScrollViewer");
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height, "the rest scroll");
    }

    // With no keyboard (a desktop), the list runs down to 2.5 above the window's bottom; it follows the keyboard when one
    // comes up after it opened; MaxDropDownHeight (WinRT's MaxSuggestionListHeight) caps it.
    [AvaloniaFact]
    public void The_List_Takes_The_Room_Above_The_Keyboard_Or_The_Windows_Bottom_Up_To_Its_Max()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(atTop: true, keyboard: keyboard);
        stage.Open(12);
        Assert.Equal(Scene.ReferenceHeight - KeyboardGap - ListTop, stage.List.MaxHeight, 0.01);
        Assert.Equal(12 * RowPitch + 2 * ListBorder, stage.InWindow(stage.List).Height, 0.01);

        keyboard.Show(Keyboard);
        stage.Settle();
        Assert.Equal(Keyboard.Top - KeyboardGap, stage.InWindow(stage.List).Bottom, 0.01);

        stage.Box.MaxDropDownHeight = 200;
        stage.Settle();
        Assert.Equal(200, stage.InWindow(stage.List).Height, 0.01);
    }

    // A keyboard that covers nothing, or leaves less than one suggestion's room (a box low on a page that, unlike the phone's, doesn't
    // move up), is passed over: the list runs to the window's bottom rather than shrink to nothing.
    [AvaloniaFact]
    public void A_Keyboard_Covering_Nothing_Or_Over_The_Lists_Top_Is_Passed_Over()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(atTop: true, keyboard: keyboard);
        var room = Scene.ReferenceHeight - KeyboardGap - ListTop;

        keyboard.Show(new Rect(0, 500, 450, 0));
        stage.Open(3);
        Assert.Equal(room, stage.List.MaxHeight, 0.01);

        keyboard.Show(new Rect(0, ListTop + OneTall, 450, 300));
        stage.Settle();
        Assert.True(stage.Box.IsDropDownOpen);
        Assert.Equal(room, stage.List.MaxHeight, 0.01);
    }

    // With less than one suggestion's room below the box, the list closes rather than stay open and unseen.
    [AvaloniaFact]
    public void With_No_Room_Below_The_Box_The_List_Closes()
    {
        using var stage = new Stage();
        // The box's bottom at or past the window's bottom.
        stage.Slot.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        stage.Slot.Height = 200;
        stage.Slot.Margin = new Thickness(19, Scene.ReferenceHeight - FocusedBoxBottom, 19, 0);
        stage.Open(3);
        Assert.True(stage.InWindow(stage.TextBox).Bottom >= Scene.ReferenceHeight);

        Assert.False(stage.Box.IsDropDownOpen);
    }

    // While open, the list follows the box as the page lays it out again, and its room with it.
    [AvaloniaFact]
    public void The_Open_List_Follows_The_Box()
    {
        using var stage = new Stage(atTop: true);
        stage.Open(3);

        stage.Slot.Margin = new Thickness(19, 100 - Margin.Top, 19, 0);
        stage.Settle();
        stage.Settle();

        Assert.Equal(ListTop + 100, stage.InWindow(stage.List).Y, 0.01);
        Assert.Equal(Scene.ReferenceHeight - KeyboardGap - ListTop - 100, stage.List.MaxHeight, 0.01);
    }

    // A tap on the box with the list open goes to the box (its caret); the list stays.
    [AvaloniaFact]
    public void A_Tap_On_The_Box_Keeps_The_List()
    {
        using var stage = new Stage(atTop: true);
        stage.Open(3);
        var at = stage.InWindow(stage.TextBox).Center;

        var touch = stage.Window.TouchBegin(at);
        Dispatcher.UIThread.RunJobs();
        stage.Window.TouchEnd(touch, at);
        stage.Settle();

        Assert.True(stage.Box.IsDropDownOpen);
        Assert.True(stage.TextBox.IsFocused);
    }

    // The grabs' pixels with three suggestions, dark and light: the focused box's accent border and white inside, the
    // borders on each other, the list's white inside and accent sides and bottom, the page under it, the text black.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Its_Pixels_Are_The_Phones(string variant)
    {
        using var stage = new Stage(VariantOf(variant), atTop: true);
        stage.Open(3);
        var frame = stage.Scene.Capture();
        var page = variant == "Dark" ? "#FF000000" : "#FFFFFFFF";
        const int mid = 540;

        for (var k = 0; k < BorderPixels; k++)
        {
            Assert.Equal(Accent, PixelHex(frame, mid, BoxTopRow + k));
            Assert.Equal(Accent, PixelHex(frame, mid, SharedBorderRow + k));
            Assert.Equal(Accent, PixelHex(frame, mid, ThreeBottomBorderRow + k));
            Assert.Equal(Accent, PixelHex(frame, LeftBorderColumn + k, 300));
        }
        Assert.Equal(White, PixelHex(frame, mid, BoxTopRow + BorderPixels));
        Assert.Equal(White, PixelHex(frame, mid, ListInsideRow));
        Assert.Equal(White, PixelHex(frame, mid, 300));
        Assert.Equal(page, PixelHex(frame, mid, ThreeBottomBorderRow + BorderPixels));
        Assert.Equal(page, PixelHex(frame, LeftBorderColumn - 1, 300));

        // "apple"'s ink: the first row's band holds black.
        var first = (int)((ListTop + FirstRow) * Scene.ReferenceScaling);
        var inked = false;
        for (var y = first; y < first + (int)(RowHeight * Scene.ReferenceScaling) && !inked; y++)
            for (var x = LeftBorderColumn + 20; x < 400 && !inked; x++)
                inked = PixelHex(frame, x, y) == Black;
        Assert.True(inked, "no black text in the first row");
    }

    // The phone's control doesn't filter: the app's suggestions are shown as given, whatever is typed.
    [AvaloniaFact]
    public void The_App_Gives_The_Suggestions_And_The_Box_Doesnt_Filter_Them()
    {
        using var stage = new Stage();
        stage.Box.MinimumPopulateDelay = TimeSpan.Zero;
        stage.Box.ItemsSource = new[] { "banana", "cherry" };
        stage.Focus();

        stage.Window.KeyTextInput("a");
        stage.Settle();

        Assert.True(stage.Box.IsDropDownOpen);
        Assert.Equal(["banana", "cherry"], stage.Rows.Select(r => r.Content as string));
    }

    // A key's cue to the app comes 150 ms after it (#150: TextChanged with UserInput 150 to 177 ms after KeyDown), and
    // the list opens with it, with what the app gave. Avalonia's cue is Populating; its TextChanged comes at once.
    [AvaloniaFact]
    public void A_Key_Asks_The_App_150_Ms_Later_And_The_List_Opens_Then()
    {
        using var stage = new Stage();
        stage.Focus();
        var watch = new Stopwatch();
        double? asked = null;
        stage.Box.Populating += (_, e) =>
        {
            asked ??= watch.Elapsed.TotalMilliseconds;
            stage.Box.ItemsSource = Fruits.Where(f => f.StartsWith(e.Parameter ?? "", StringComparison.OrdinalIgnoreCase)).ToList();
        };

        watch.Start();
        stage.Window.KeyTextInput("a");
        Dispatcher.UIThread.RunJobs();
        Assert.Null(asked);
        Assert.False(stage.Box.IsDropDownOpen);

        RunFor(5_000, () => asked is not null);
        Assert.NotNull(asked);
        // Avalonia's timer against a stopwatch started before the key: a few milliseconds' slack for the timer's rounding.
        Assert.InRange(asked.Value, CueAfterKey - 5, CueAfterKey + 500);
        stage.Settle();
        Assert.True(stage.Box.IsDropDownOpen);
        Assert.Equal(3, stage.Rows.Count);
    }

    // The phone tells the app of every change the user makes, the box emptied too.
    [AvaloniaFact]
    public void Emptying_The_Box_Asks_The_App_Again()
    {
        using var stage = new Stage();
        stage.Box.MinimumPopulateDelay = TimeSpan.Zero;
        var asked = new List<string?>();
        stage.Box.Populating += (_, e) => asked.Add(e.Parameter);
        stage.Focus();

        stage.Window.KeyTextInput("a");
        stage.Settle();
        stage.Window.KeyPressQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
        stage.Settle();

        Assert.Equal(["a", ""], asked);
    }

    // A tap on a suggestion puts it in the box and closes the list; the box keeps the focus, as the phone's keyboard
    // stays. (The phone raises SuggestionChosen first, with the typed text, then TextChanged; Avalonia sets the text,
    // then raises SelectionChanged: a recorded difference.)
    [AvaloniaFact]
    public void A_Tap_On_A_Suggestion_Puts_It_In_The_Box_And_Closes_The_List()
    {
        using var stage = new Stage(atTop: true);
        stage.Open(3, "ap");
        object? chosen = null;
        stage.Box.SelectionChanged += (_, e) => chosen = e.AddedItems.Cast<object>().SingleOrDefault();
        var row = stage.InWindow(stage.Rows[1]);

        var at = row.Center;
        var touch = stage.Window.TouchBegin(at);
        Dispatcher.UIThread.RunJobs();
        stage.Window.TouchEnd(touch, at);
        stage.Settle();

        Assert.Equal("apricot", chosen);
        Assert.Equal("apricot", stage.Box.Text);
        Assert.False(stage.Box.IsDropDownOpen);
        Assert.True(stage.TextBox.IsFocused, $"focus on {stage.Window.FocusManager?.GetFocusedElement()}");
    }

    // Text set from code (the phone's ProgrammaticChange) doesn't open the list.
    [AvaloniaFact]
    public void Text_From_Code_Doesnt_Open_The_List()
    {
        using var stage = new Stage();
        stage.Box.MinimumPopulateDelay = TimeSpan.Zero;
        stage.Box.ItemsSource = Fruits;
        stage.Focus();

        stage.Box.Text = "a";
        stage.Settle();

        Assert.False(stage.Box.IsDropDownOpen);
    }

    // One Back closes the list and takes the focus from the box, before the page's NavigationPage sees it; the next goes
    // back (#150: "one Back hid the keyboard, closed the suggestion list and took the focus; the next reached the app").
    [AvaloniaFact]
    public void Back_Closes_The_List_And_Takes_The_Focus_Then_The_Next_Goes_Back()
    {
        var first = new ContentPage();
        var second = new ContentPage();
        var nav = new NavigationPage();
        using var stage = new Stage(atTop: true, wrap: page =>
        {
            second.Content = page;
            return new PageNavigationHost { Page = nav };
        });
        _ = nav.PushAsync(first);
        _ = nav.PushAsync(second, null);
        Dispatcher.UIThread.RunJobs();
        stage.Open(3);
        Assert.True(stage.Box.IsDropDownOpen);

        var back = stage.Back();
        Assert.True(back.Handled);
        Assert.False(stage.Box.IsDropDownOpen);
        Assert.False(stage.Box.IsKeyboardFocusWithin);
        Assert.Same(second, nav.CurrentPage);

        var next = stage.Back();
        Assert.True(next.Handled);
        Assert.Same(first, nav.CurrentPage);
    }

    // Turned on after the template was applied (a theme swapped in), the behaviour takes the parts as they are.
    [AvaloniaFact]
    public void Turned_On_After_The_Template_It_Takes_The_Parts()
    {
        using var stage = new Stage(atTop: true);
        PhoneSuggestionList.SetIsEnabled(stage.Box, false);
        PhoneSuggestionList.SetIsEnabled(stage.Box, true);
        stage.Open(3);

        Assert.Equal(ListTop, stage.InWindow(stage.List).Y, 0.001);
        Assert.Equal(Scene.ReferenceHeight - KeyboardGap - ListTop, stage.List.MaxHeight, 0.01);
    }

    // A right click on a suggestion chooses nothing: the list stays open.
    [AvaloniaFact]
    public void A_Right_Click_On_A_Suggestion_Keeps_The_List()
    {
        using var stage = new Stage(atTop: true);
        stage.Open(3, "ap");
        var at = stage.InWindow(stage.Rows[1]).Center;

        stage.Window.MouseDown(at, MouseButton.Right);
        stage.Window.MouseUp(at, MouseButton.Right);
        stage.Settle();

        Assert.True(stage.Box.IsDropDownOpen);
    }

    // With the focus elsewhere, Back is the page's.
    [AvaloniaFact]
    public void Back_With_The_Box_Unfocused_Is_Left_Alone()
    {
        using var stage = new Stage();
        Assert.False(stage.Back().Handled);
    }

    // On Android the keyboard takes the first Back itself; the keyboard going down does what the phone's Back does.
    [AvaloniaFact]
    public void The_Keyboard_Going_Down_Closes_The_List_And_Takes_The_Focus()
    {
        var keyboard = new FakeKeyboard();
        using var stage = new Stage(atTop: true, keyboard: keyboard);
        keyboard.Show(Keyboard);
        stage.Open(3);

        keyboard.Hide();

        Assert.False(stage.Box.IsDropDownOpen);
        Assert.False(stage.Box.IsKeyboardFocusWithin);
    }

    // The keyboard going down with the focus elsewhere leaves the box alone. The other box is kept out of the pan, which
    // would take its focus (#197), so only the AutoSuggestBox's own handling is tried.
    [AvaloniaFact]
    public void The_Keyboard_Going_Down_With_The_Focus_Elsewhere_Takes_Nothing()
    {
        var keyboard = new FakeKeyboard();
        var other = new TextBox();
        PhoneKeyboardPan.SetIsEnabled(other, false);
        using var stage = new Stage(keyboard: keyboard);
        stage.Slot.Children.Add(new StackPanel { Margin = new Thickness(0, 200, 0, 0), Children = { other } });
        Dispatcher.UIThread.RunJobs();
        keyboard.Show(Keyboard);
        Assert.True(other.Focus());

        keyboard.Hide();

        Assert.True(other.IsFocused);
    }

    // Under Simple alone the box is Avalonia's own, and Back is the page's.
    [AvaloniaFact]
    public void Simple_Alone_Keeps_Avalonias_Own()
    {
        using var stage = new Stage(themes: [new SimpleTheme()]);
        stage.Box.MinimumPopulateDelay = TimeSpan.Zero;
        stage.Focus();

        Assert.False(PhoneSuggestionList.GetIsEnabled(stage.Box));
        Assert.Equal(AutoCompleteFilterMode.StartsWith, stage.Box.FilterMode);
        Assert.False(stage.Back().Handled);
    }
}
