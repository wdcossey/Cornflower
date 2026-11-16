using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ContentDialogReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's ContentDialog: Cornflower's <see cref="ContentDialog"/>, Wp81's theme (<c>Controls/ContentDialog.axaml</c>)
/// and the popup round it (<see cref="PhoneContentDialog"/> on <see cref="PhonePopupHost"/>), against the emulator's
/// screen grabs, the dismissal probes and the recording's motion (<see cref="ContentDialogReadings"/>).
/// docs/controls/ContentDialog/PLAN.md has each reading.
/// </summary>
public class ContentDialogTests
{
    /// <summary>The reference app's stage: a page with the "open" Button, the dialog shown over its window.</summary>
    internal sealed class Stage : IDisposable
    {
        public Stage(ThemeVariant? variant = null, string? primary = "ok", string? secondary = "cancel",
            object? title = null, object? content = null, bool fullSize = false, Control? root = null,
            double width = Scene.ReferenceWidth, double height = Scene.ReferenceHeight, IBrush? page = null)
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Dialog = new ContentDialog
            {
                Title = title ?? "content dialog",
                Content = content ?? "Agjy 0123 the dialog's body text, long enough to wrap onto a second line.",
                PrimaryButtonText = primary,
                SecondaryButtonText = secondary,
                FullSizeDesired = fullSize,
            };
            Button = new Button
            {
                Content = "open",
                Margin = new Thickness(19, 377.08, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            Page = new Panel { Background = page ?? Brushes.Transparent, Children = { Button } };
            Page.PointerPressed += (_, _) => PagePresses++;
            try
            {
                Scene = new Scene(root ?? Page, variant, width, height);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public ContentDialog Dialog { get; }

        public Button Button { get; }

        public Panel Page { get; }

        public int PagePresses { get; private set; }

        public Scene Scene { get; }

        public Task<ContentDialogResult>? Result { get; private set; }

        public PhonePopupBands? Bands => PhoneContentDialog.HostOf(Dialog)?.Bands;

        public double FirstFrameAfter(double call) => (Math.Floor(call / Tick) + 1) * Tick;

        public void Show()
        {
            Result = Dialog.ShowAsync(Scene.Window);
            Dispatcher.UIThread.RunJobs();
        }

        public void ShowAtRest()
        {
            Show();
            At(Clock.Now.TotalMilliseconds + 500);
        }

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
        }

        public void Back(RoutedEventArgs? request = null)
        {
            Scene.Window.RaiseEvent(request ?? new RoutedEventArgs(TopLevel.BackRequestedEvent));
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>The dialog's laid-out bounds in the window, without the motion's transform.</summary>
        public Rect Bounds()
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var host = Dialog.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
            return new Rect(Canvas.GetLeft(host), Canvas.GetTop(host), Dialog.Bounds.Width, Dialog.Bounds.Height);
        }

        public T Part<T>(string name) where T : Control =>
            Dialog.GetVisualDescendants().OfType<T>().Single(c => c.Name == name && c.TemplatedParent == Dialog);

        public Button[] Buttons => Dialog.GetVisualDescendants().OfType<Button>().ToArray();

        public Rect InWindow(Visual visual) =>
            new(visual.TranslatePoint(default, Scene.Window)!.Value, visual.Bounds.Size);

        /// <summary>A button's outline, its template's Border, in the window.</summary>
        public Rect Outline(Button button) =>
            InWindow(button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "Border"));

        public void Tap(Point at)
        {
            Scene.Window.MouseDown(at, MouseButton.Left);
            Scene.Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        public void Click(Button button)
        {
            var at = InWindow(button).Center;
            Tap(at);
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    private static string Long(int lines) =>
        string.Join("\n", Enumerable.Range(1, lines).Select(i => $"line {i} of the dialog's long body"));

    private const string LongTitle = "Agjy 0123 a content dialog title long enough to wrap onto a second line";

    private static void AssertRect(Rect expected, Rect actual, double tolerance = RawPixel + 0.001)
    {
        Assert.Equal(expected.X, actual.X, tolerance);
        Assert.Equal(expected.Y, actual.Y, tolerance);
        Assert.Equal(expected.Width, actual.Width, tolerance);
        Assert.Equal(expected.Height, actual.Height, tolerance);
    }

    /// <summary>A task's result, once it has one: never waited for, which would block the UI thread.</summary>
    private static ContentDialogResult Done(Task<ContentDialogResult>? task)
    {
        Assert.NotNull(task);
        Assert.True(task.IsCompletedSuccessfully);
        return task.Result;
    }

    private static double Eased(int tick) =>
        OpenFrom * (1 - (1 - Math.Exp(-OpenExponent * tick / (double)OpenTicks)) / (1 - Math.Exp(-OpenExponent)));

    [AvaloniaFact]
    public void The_Dialog_Is_Templated_By_The_Theme()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        var theme = Assert.IsType<ControlTheme>(stage.Scene.Window.FindResource(typeof(ContentDialog)));
        var template = theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
        Assert.Same(template, stage.Dialog.Template);
        Assert.True(PhoneContentDialog.GetIsEnabled(stage.Dialog));
        foreach (var name in new[] { "Container", "BackgroundElement", "PART_Button1Host", "PART_Button2Host" })
            stage.Part<Border>(name);
        stage.Part<Panel>("LayoutRoot");
        stage.Part<PhoneContentDialogPanel>("ContentPanel");
        stage.Part<ContentPresenter>("Title");
        Assert.Same(stage.Dialog.Presenter, stage.Part<ContentPresenter>("PART_ContentPresenter"));
        Assert.Same(stage.Dialog.FindResource(stage.Dialog.ActualThemeVariant, "ContentDialogBackgroundThemeBrush"), stage.Dialog.Background);
        Assert.Same(stage.Dialog.FindResource(stage.Dialog.ActualThemeVariant, "PhoneForegroundBrush"), stage.Dialog.Foreground);
    }

    [AvaloniaFact]
    public void It_Is_At_The_Windows_Top_In_Its_Overlay_Layer_Over_A_Dim_Layer()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        var popup = stage.Dialog.Popup!;
        Assert.True(popup.IsUsingOverlayLayer);
        Assert.Same(stage.Scene.Window, TopLevel.GetTopLevel(stage.Dialog));
        var bounds = stage.Bounds();
        Assert.Equal(new Point(0, 0), bounds.Position);
        Assert.Equal(450, bounds.Width);
        Assert.Equal(Height, bounds.Height, RawPixel + 0.001);

        var host = stage.Dialog.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
        var layer = (Panel)host.GetVisualParent()!;
        var dim = layer.Children.OfType<Rectangle>().Single();
        Assert.Equal(Dim(dark: true), ((ISolidColorBrush)dim.Fill!).Color);
        Assert.Equal(new Size(450, 800), new Size(dim.Width, dim.Height));
        Assert.True(layer.Children.IndexOf(dim) < layer.Children.IndexOf(host));
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_Chrome_Is_The_Phones_And_The_Page_Is_Dimmed_Under_It(bool dark)
    {
        // A page of the other colour, so the dim shows: #99000000 over white is 102 grey; #99FFFFFF over black, 153.
        var page = dark ? Colors.White : Colors.Black;
        using var stage = new Stage(dark ? ThemeVariant.Dark : ThemeVariant.Light, page: new SolidColorBrush(page));
        stage.ShowAtRest();

        using var frame = stage.Scene.Capture();
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1070, 5));
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1070, (int)(Height * Scale) - 1));
        var dimmed = Frames.PixelAt(frame, 1070, (int)(Height * Scale) + 2);
        var expected = dark ? (byte)102 : (byte)153;
        Assert.Equal(expected, dimmed.R, 1.0);
        Assert.Equal(expected, Frames.PixelAt(frame, 1070, 1900).G, 1.0);
    }

    [AvaloniaFact]
    public void The_Title_The_Body_And_Two_Buttons_Are_Where_The_Phones_Are()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        var title = stage.Part<ContentPresenter>("Title").Child!;
        var at = title.TranslatePoint(default, stage.Scene.Window)!.Value;
        Assert.Equal(TitleAt.X, at.X, RawPixel + 0.001);
        Assert.Equal(TitleAt.Y, at.Y, RawPixel + 0.001);
        var body = stage.Part<ContentPresenter>("PART_ContentPresenter").Child!;
        Assert.Equal(BodyTop, body.TranslatePoint(default, stage.Scene.Window)!.Value.Y, RawPixel + 0.001);
        Assert.Equal(HostsTop, stage.InWindow(stage.Part<Border>("PART_Button1Host")).Y, RawPixel + 0.001);

        var buttons = stage.Buttons;
        Assert.Equal(2, buttons.Length);
        Assert.Equal("ok", buttons[0].Content);
        Assert.Equal("cancel", buttons[1].Content);
        AssertRect(Outline1, stage.Outline(buttons[0]));
        AssertRect(Outline2, stage.Outline(buttons[1]));
    }

    [AvaloniaFact]
    public void A_String_Body_And_Title_Dont_Wrap()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        var body = (TextBlock)stage.Part<ContentPresenter>("PART_ContentPresenter").Child!;
        Assert.Equal(TextWrapping.NoWrap, body.TextWrapping);
        Assert.Single(body.TextLayout.TextLines);
        Assert.True(body.TextLayout.Width > body.Bounds.Width);
        Assert.Equal(18.14 * PhoneTextLineHeight, body.Bounds.Height, RawPixel);
    }

    private const double PhoneTextLineHeight = (2210 + 514) / 2048.0;

    [AvaloniaTheory]
    [InlineData("ok", null)]
    [InlineData(null, "cancel")]
    public void A_Lone_Button_Takes_The_Left_Half(string? primary, string? secondary)
    {
        using var stage = new Stage(primary: primary, secondary: secondary);
        stage.ShowAtRest();

        var button = Assert.Single(stage.Buttons);
        Assert.Equal(primary ?? secondary, button.Content);
        AssertRect(Outline1, stage.Outline(button));
        Assert.False(stage.Part<Border>("PART_Button2Host").IsVisible);
        Assert.Equal(Height, stage.Bounds().Height, RawPixel + 0.001);
    }

    [AvaloniaFact]
    public void With_No_Button_The_Hosts_Collapse_And_The_Row_Keeps_Its_45()
    {
        using var stage = new Stage(primary: null, secondary: "");
        stage.ShowAtRest();

        Assert.Empty(stage.Buttons);
        Assert.False(stage.Part<Border>("PART_Button1Host").IsVisible);
        Assert.Equal(NoButtonsHeight, stage.Bounds().Height, RawPixel + 0.001);
    }

    [AvaloniaFact]
    public void Button_Text_Set_While_Shown_Moves_The_Buttons()
    {
        using var stage = new Stage(primary: null, secondary: "cancel");
        stage.ShowAtRest();
        var cancel = Assert.Single(stage.Buttons);

        stage.Dialog.PrimaryButtonText = "ok";
        stage.At(stage.Clock.Now.TotalMilliseconds + 50);
        var buttons = stage.Buttons;
        Assert.Equal(["ok", "cancel"], buttons.Select(b => (string)b.Content!));
        Assert.Same(cancel, buttons[1]);
        AssertRect(Outline2, stage.Outline(cancel));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullSizeDesired_Fills_The_Window_And_Puts_The_Buttons_In_The_Shells_Bar(bool dark)
    {
        using var stage = new Stage(dark ? ThemeVariant.Dark : ThemeVariant.Light, fullSize: true);
        stage.ShowAtRest();

        Assert.Equal(new Rect(0, 0, 450, 800), stage.Bounds());
        Assert.True(stage.Part<PhoneContentDialogPanel>("ContentPanel").IsFull);
        var buttons = stage.Buttons;
        AssertRect(BarOutline1, stage.Outline(buttons[0]));
        AssertRect(BarOutline2, stage.Outline(buttons[1]));
        // The bar is the dialog's colour, to its foot.
        using var frame = stage.Scene.Capture();
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1070, 1919));
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 540, 1000));
    }

    [AvaloniaFact]
    public void A_Long_Title_And_Body_Fill_The_Window_The_Title_On_One_Line_The_Body_Clipped_At_The_Bar()
    {
        using var stage = new Stage(title: LongTitle, content: Long(40));
        stage.ShowAtRest();

        Assert.Equal(new Rect(0, 0, 450, 800), stage.Bounds());
        Assert.True(stage.Part<PhoneContentDialogPanel>("ContentPanel").IsFull);
        var title = (TextBlock)stage.Part<ContentPresenter>("Title").Child!;
        Assert.Equal(25.5 * PhoneTextLineHeight, title.Bounds.Height, RawPixel);
        Assert.Single(title.TextLayout.TextLines);
        Assert.True(title.TextLayout.Width > title.Bounds.Width);
        // No ScrollViewer: the body is clipped where the bar starts.
        var body = stage.Part<ContentPresenter>("PART_ContentPresenter");
        Assert.True(body.ClipToBounds);
        Assert.Equal(BarTop, stage.InWindow(body).Bottom, RawPixel + 0.001);
        Assert.Empty(stage.Dialog.GetVisualDescendants().OfType<ScrollViewer>());
        AssertRect(BarOutline1, stage.Outline(stage.Buttons[0]));
        AssertRect(BarOutline2, stage.Outline(stage.Buttons[1]));
    }

    [AvaloniaFact]
    public void A_Body_That_Fits_Is_Not_Full()
    {
        using var stage = new Stage(content: Long(10));
        stage.ShowAtRest();

        Assert.False(stage.Part<PhoneContentDialogPanel>("ContentPanel").IsFull);
        Assert.True(stage.Bounds().Height < 800);
    }

    [AvaloniaFact]
    public void In_Landscape_The_Content_Panel_Is_400_Wide_At_The_Left()
    {
        using var stage = new Stage(width: 800, height: 450);
        stage.ShowAtRest();

        Assert.True(PhonePopupHost.GetIsLandscape(stage.Dialog));
        var panel = stage.Part<PhoneContentDialogPanel>("ContentPanel");
        Assert.Equal(new Rect(0, 0, 400, panel.Bounds.Height), stage.InWindow(panel));
        Assert.Equal(800, stage.Bounds().Width);
    }

    [AvaloniaFact]
    public void It_Opens_As_One_Band_From_45_Degrees_Easing_Out_To_Rest_At_Tick_9()
    {
        using var stage = new Stage();
        stage.Show();

        // No frame shows the popup at rest, or at the Flyout's 80°, before its band.
        Assert.Equal(OpenFrom, stage.Bands!.Angles[0]);
        Assert.Equal(0, stage.Dialog.Opacity);
        var first = stage.FirstFrameAfter(0);
        stage.At(first);
        var band = Assert.Single(stage.Bands!.Bands);
        Assert.Equal(new Rect(0, 0, 450, stage.Dialog.Bounds.Height), band);
        for (var tick = 0; tick < OpenTicks; tick++)
        {
            stage.At(first + tick * Tick);
            Assert.Equal(Eased(tick), stage.Bands!.Angles[0], 1e-9);
        }

        stage.At(first + OpenTicks * Tick);
        Assert.Null(stage.Bands);
        Assert.Equal(1, stage.Dialog.Opacity);
        Assert.Equal(OpenTicks * Tick, PhoneContentDialog.OpenLength.TotalMilliseconds);
    }

    [AvaloniaFact]
    public void Hide_Raises_Closing_And_Closed_And_Completes_At_Once_And_It_Goes_In_One_Frame_About_107_Ms_Later()
    {
        using var stage = new Stage(page: Brushes.White);
        stage.ShowAtRest();
        var log = new List<string>();

        // An app's await: its continuation comes after the events, from the dispatcher, not inside Hide().
        async Task Await() => log.Add($"result {await stage.Result!}");
        stage.Dialog.Closing += (_, e) => log.Add($"Closing {e.Result}");
        stage.Dialog.Closed += (_, e) => log.Add($"Closed {e.Result}");
        _ = Await();
        var hidden = stage.Clock.Now.TotalMilliseconds;

        stage.Dialog.Hide();
        Assert.Equal(["Closing None", "Closed None"], log);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["Closing None", "Closed None", "result None"], log);

        // Drawn as it is, the dim layer too, until it goes in one frame.
        stage.At(hidden + GoneFrom - 1);
        Assert.True(stage.Dialog.Popup!.IsOpen);
        Assert.Equal(1, stage.Dialog.Opacity);
        Assert.Null(stage.Bands);
        stage.At(hidden + GoneBy);
        Assert.False(stage.Dialog.Popup.IsOpen);
        using var frame = stage.Scene.Capture();
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 1070, 5));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 1070, 1900));
        Assert.InRange(PhoneContentDialog.GoneAfter.TotalMilliseconds, GoneFrom, GoneBy);
    }

    [AvaloniaFact]
    public void A_Click_On_A_Button_Raises_Its_Click_Closing_Closed_And_The_Result_In_That_Order()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        var log = new List<string>();
        stage.Dialog.PrimaryButtonClick += (_, _) => log.Add("PrimaryButtonClick");
        stage.Dialog.SecondaryButtonClick += (_, _) => log.Add("SecondaryButtonClick");
        stage.Dialog.Closing += (_, e) => log.Add($"Closing {e.Result}");
        stage.Dialog.Closed += (_, e) => log.Add($"Closed {e.Result}");
        _ = stage.Result!.ContinueWith(t => log.Add($"result {t.Result}"), TaskScheduler.FromCurrentSynchronizationContext());

        stage.Click(stage.Buttons[1]);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["SecondaryButtonClick", "Closing Secondary", "Closed Secondary", "result Secondary"], log);
        Assert.Equal(ContentDialogResult.Secondary, Done(stage.Result));
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Button_Runs_Its_Own_Command_Once_With_Its_Parameter_And_Closes_With_Its_Result(bool primary)
    {
        using var stage = new Stage();
        Command first = new(), second = new();
        stage.Dialog.PrimaryButtonCommand = first;
        stage.Dialog.PrimaryButtonCommandParameter = "p";
        stage.Dialog.SecondaryButtonCommand = second;
        stage.Dialog.SecondaryButtonCommandParameter = "s";
        stage.ShowAtRest();

        stage.Click(stage.Buttons[primary ? 0 : 1]);
        Assert.Equal(primary ? ["p"] : [], first.Ran);
        Assert.Equal(primary ? [] : ["s"], second.Ran);
        Assert.Equal(primary ? ContentDialogResult.Primary : ContentDialogResult.Secondary, Done(stage.Result));
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Command_That_Cant_Run_And_A_Button_Not_Enabled_Disable_Their_Buttons(bool primaryDisabled)
    {
        using var stage = new Stage();
        // One button disabled by its IsPrimaryButtonEnabled or IsSecondaryButtonEnabled, the other by its command.
        if (primaryDisabled)
        {
            stage.Dialog.IsPrimaryButtonEnabled = false;
            stage.Dialog.SecondaryButtonCommand = new Command { CanRun = false };
        }
        else
        {
            stage.Dialog.IsSecondaryButtonEnabled = false;
            stage.Dialog.PrimaryButtonCommand = new Command { CanRun = false };
        }

        stage.ShowAtRest();

        Assert.False(stage.Buttons[0].IsEffectivelyEnabled);
        Assert.False(stage.Buttons[1].IsEffectivelyEnabled);
        stage.Click(stage.Buttons[0]);
        stage.Click(stage.Buttons[1]);
        Assert.False(stage.Result!.IsCompleted);

        stage.Dialog.IsPrimaryButtonEnabled = true;
        stage.Dialog.IsSecondaryButtonEnabled = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(primaryDisabled, stage.Buttons[0].IsEffectivelyEnabled);
        Assert.Equal(!primaryDisabled, stage.Buttons[1].IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void A_Cancelled_Click_Runs_No_Command_And_Keeps_It_Open()
    {
        using var stage = new Stage();
        var command = new Command();
        stage.Dialog.PrimaryButtonCommand = command;
        stage.Dialog.PrimaryButtonClick += (_, e) => e.Cancel = true;
        stage.ShowAtRest();

        stage.Click(stage.Buttons[0]);
        Assert.Empty(command.Ran);
        Assert.False(stage.Result!.IsCompleted);
        Assert.True(stage.Dialog.Popup!.IsOpen);
    }

    [AvaloniaFact]
    public void A_Deferred_Click_Waits_For_Its_Deferral()
    {
        using var stage = new Stage();
        ContentDialogButtonClickDeferral? deferral = null;
        stage.Dialog.PrimaryButtonClick += (_, e) => deferral = e.GetDeferral();
        stage.ShowAtRest();

        stage.Click(stage.Buttons[0]);
        Assert.False(stage.Result!.IsCompleted);
        // A second click while it waits is ignored.
        stage.Click(stage.Buttons[0]);
        deferral!.Complete();
        deferral.Complete();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ContentDialogResult.Primary, Done(stage.Result));
    }

    [AvaloniaFact]
    public void Closing_Can_Keep_It_Open_At_Once_Or_After_A_Deferral()
    {
        using var stage = new Stage();
        var defer = false;
        var raised = 0;
        ContentDialogClosingDeferral? deferral = null;
        ContentDialogClosingEventArgs? last = null;
        stage.Dialog.Closing += (_, e) =>
        {
            raised++;
            last = e;
            if (defer)
                deferral = e.GetDeferral();
            else
                e.Cancel = true;
        };
        stage.ShowAtRest();

        stage.Dialog.Hide();
        Assert.False(stage.Result!.IsCompleted);
        Assert.True(stage.Dialog.Popup!.IsOpen);

        // Deferred, and cancelled before the deferral is completed: still open. Another Hide while it waits does nothing.
        defer = true;
        stage.Dialog.Hide();
        stage.Dialog.Hide();
        Assert.Equal(2, raised);
        last!.Cancel = true;
        deferral!.Complete();
        Dispatcher.UIThread.RunJobs();
        Assert.False(stage.Result.IsCompleted);

        // Deferred and let go: closed when the deferral is completed.
        stage.Dialog.Hide();
        Assert.Equal(3, raised);
        Assert.False(stage.Result.IsCompleted);
        deferral.Complete();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));
    }

    [AvaloniaFact]
    public void A_Tap_On_The_Dim_Layer_Does_Nothing_And_Reaches_Nothing()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        var pressed = 0;
        stage.Button.Click += (_, _) => pressed++;

        stage.Tap(stage.InWindow(stage.Button).Center);
        stage.Tap(new Point(225, 600));
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(stage.Dialog.Popup!.IsOpen);
        Assert.False(stage.Result!.IsCompleted);
        Assert.Equal(0, pressed);
        Assert.Equal(0, stage.PagePresses);
    }

    [AvaloniaFact]
    public void A_Tap_On_The_Body_Does_Nothing()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        stage.Tap(new Point(225, 90));
        Assert.False(stage.Result!.IsCompleted);
    }

    [AvaloniaFact]
    public void Back_Hides_It_With_None_Before_The_NavigationPage_Goes_Back()
    {
        var first = new ContentPage();
        var second = new ContentPage();
        var nav = new NavigationPage();
        using var stage = new Stage(root: new PageNavigationHost { Page = nav });
        _ = nav.PushAsync(first);
        _ = nav.PushAsync(second, null);
        Dispatcher.UIThread.RunJobs();
        stage.ShowAtRest();
        var log = new List<string>();
        stage.Dialog.Closing += (_, e) => log.Add($"Closing {e.Result}");
        stage.Dialog.Closed += (_, e) => log.Add($"Closed {e.Result}");
        var back = stage.Clock.Now.TotalMilliseconds;

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Back(request);
        Assert.True(request.Handled);
        Assert.Same(second, nav.CurrentPage);
        Assert.Equal(["Closing None", "Closed None"], log);
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));

        // A second Back while it is still drawn is taken and does nothing.
        var again = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Back(again);
        Assert.True(again.Handled);
        Assert.Same(second, nav.CurrentPage);
        stage.At(back + GoneBy);
        Assert.False(stage.Dialog.Popup!.IsOpen);

        var next = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Back(next);
        Assert.Same(first, nav.CurrentPage);
    }

    [AvaloniaFact]
    public void Back_Kept_Open_By_Closing_Is_Still_Taken()
    {
        // Even with a Flyout shown first, whose host takes Back for every popup: the dialog's handler comes before it.
        var flyout = new FlyoutTests.Stage();
        try
        {
            flyout.OpenAtRest();
            flyout.Flyout.Hide();
            flyout.At(flyout.Clock.Now.TotalMilliseconds + 500);
        }
        finally
        {
            flyout.Dispose();
        }

        // Which comes first can't be shown in one test process, where whichever was made first stays: so the handler is
        // added as the module loads, before any host can be made.
        var take = typeof(PhoneContentDialog).GetMethod(nameof(PhoneContentDialog.TakeBack), BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(take!.GetCustomAttribute<ModuleInitializerAttribute>());

        using var stage = new Stage();
        stage.Dialog.Closing += (_, e) => e.Cancel = true;
        stage.ShowAtRest();

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Back(request);
        Assert.True(request.Handled);
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(stage.Dialog.Popup!.IsOpen);
        Assert.Equal(1, stage.Dialog.Opacity);
        Assert.False(stage.Result!.IsCompleted);
    }

    [AvaloniaFact]
    public void Back_Closes_A_Flyout_Over_The_Dialog_First()
    {
        var flyout = new Flyout { Content = new TextBlock { Text = "over the dialog" } };
        var opener = new Button { Content = "more", Flyout = flyout };
        using var stage = new Stage(content: opener);
        stage.ShowAtRest();
        flyout.ShowAt(opener);
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(flyout.IsOpen);

        stage.Back();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.False(flyout.IsOpen);
        Assert.True(stage.Dialog.Popup!.IsOpen);
        Assert.False(stage.Result!.IsCompleted);

        stage.Back();
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));
    }

    [AvaloniaFact]
    public void Back_Takes_Only_Its_Own_Windows_Dialog()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        using var other = new Scene(new Panel());

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        other.Window.RaiseEvent(request);
        Assert.False(request.Handled);
        Assert.False(stage.Result!.IsCompleted);
    }

    [AvaloniaFact]
    public void Hidden_Mid_Opening_It_Rests_Until_It_Goes()
    {
        using var stage = new Stage();
        stage.Show();
        stage.At(stage.FirstFrameAfter(0) + 3 * Tick);
        Assert.NotNull(stage.Bands);
        var hidden = stage.Clock.Now.TotalMilliseconds;

        stage.Dialog.Hide();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(stage.Bands);
        Assert.Equal(1, stage.Dialog.Opacity);
        stage.At(hidden + GoneBy);
        Assert.False(stage.Dialog.Popup!.IsOpen);
    }

    [AvaloniaFact]
    public void It_Shows_Again_Placed_And_Moving_Even_Before_It_Has_Gone()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        stage.Dialog.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 50);
        Assert.True(stage.Dialog.Popup!.IsOpen);

        var opened = 0;
        stage.Dialog.Opened += (_, _) => opened++;
        stage.Show();
        Assert.Equal(1, opened);
        Assert.Equal(OpenFrom, stage.Bands!.Angles[0]);
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(stage.Dialog.Popup.IsOpen);
        Assert.Equal(new Point(0, 0), stage.Bounds().Position);
        Assert.False(stage.Result!.IsCompleted);
    }

    [AvaloniaFact]
    public void ShowAsync_While_Shown_Throws()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        Assert.Throws<InvalidOperationException>(() => { _ = stage.Dialog.ShowAsync(stage.Scene.Window); });
    }

    [AvaloniaFact]
    public void ShowAsync_Without_A_Window_Throws_And_On_Desktop_Shows_Over_The_Main_Window()
    {
        using var stage = new Stage();

        // The tests' application has no lifetime.
        Assert.Null(Application.Current!.ApplicationLifetime);
        Assert.Throws<InvalidOperationException>(() => { _ = stage.Dialog.ShowAsync(); });
        Assert.False(stage.Dialog.IsShown);

        using var desktop = new ClassicDesktopStyleApplicationLifetime { MainWindow = stage.Scene.Window };
        Assert.Same(stage.Scene.Window, ContentDialog.AppTopLevel(desktop));
        Assert.Null(ContentDialog.AppTopLevel(null));
    }

    [AvaloniaFact]
    public void Its_Window_Closing_Closes_It_With_None()
    {
        var stage = new Stage();
        stage.ShowAtRest();
        var closed = new List<ContentDialogResult>();
        stage.Dialog.Closed += (_, e) => closed.Add(e.Result);

        stage.Dispose();
        Assert.Equal([ContentDialogResult.None], closed);
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));
    }

    [AvaloniaFact]
    public void The_Title_And_Content_Templates_Are_Used()
    {
        using var stage = new Stage(title: 7, content: 8);
        stage.Dialog.TitleTemplate = new FuncDataTemplate<int>((n, _) => new TextBlock { Text = $"title {n}" });
        stage.Dialog.ContentTemplate = new FuncDataTemplate<int>((n, _) => new TextBlock { Text = $"body {n}" });
        stage.ShowAtRest();

        Assert.Equal("title 7", ((TextBlock)stage.Part<ContentPresenter>("Title").Child!).Text);
        Assert.Equal("body 8", ((TextBlock)stage.Part<ContentPresenter>("PART_ContentPresenter").Child!).Text);
    }

    [AvaloniaFact]
    public void The_Light_Theme_Dims_With_White_Also_After_The_Window_Changes_To_It()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        stage.Dialog.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);

        stage.Scene.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        stage.ShowAtRest();
        var layer = (Panel)stage.Dialog.GetVisualAncestors().OfType<OverlayPopupHost>().Single().GetVisualParent()!;
        Assert.Equal(Dim(dark: false), ((ISolidColorBrush)layer.Children.OfType<Rectangle>().Single().Fill!).Color);
    }

    [AvaloniaFact]
    public void A_Closed_Handler_Can_Show_It_Again()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        Task<ContentDialogResult>? again = null;
        stage.Dialog.Closed += (_, _) => again ??= stage.Dialog.ShowAsync(stage.Scene.Window);

        stage.Dialog.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));
        Assert.NotNull(again);
        Assert.False(again.IsCompleted);
        Assert.True(stage.Dialog.Popup!.IsOpen);
        Assert.Equal(1, stage.Dialog.Opacity);
    }

    [AvaloniaFact]
    public void A_Click_Deferred_In_One_Show_Doesnt_Reach_The_Next()
    {
        using var stage = new Stage();
        ContentDialogButtonClickDeferral? deferral = null;
        stage.Dialog.PrimaryButtonClick += (_, e) => deferral ??= e.GetDeferral();
        stage.ShowAtRest();
        var first = stage.Result!;
        stage.Click(stage.Buttons[0]);
        stage.Dialog.Hide();
        Assert.Equal(ContentDialogResult.None, Done(first));
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);

        stage.ShowAtRest();
        deferral!.Complete();
        Dispatcher.UIThread.RunJobs();
        Assert.False(stage.Result!.IsCompleted);
        // The new show's buttons click.
        stage.Click(stage.Buttons[1]);
        Assert.Equal(ContentDialogResult.Secondary, Done(stage.Result));
    }

    [AvaloniaFact]
    public void A_Deferral_Taken_After_The_Handler_Has_Returned_Throws()
    {
        using var stage = new Stage();
        ContentDialogClosingEventArgs? late = null;
        stage.Dialog.Closing += (_, e) => late = e;
        stage.ShowAtRest();

        stage.Dialog.Hide();
        Assert.Throws<InvalidOperationException>(() => late!.GetDeferral());
    }

    [AvaloniaFact]
    public void It_Takes_The_Focus_Keeps_Tab_In_It_And_Gives_The_Focus_Back()
    {
        using var stage = new Stage();
        stage.Button.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.ShowAtRest();

        var focus = stage.Scene.Window.FocusManager!;
        Assert.True(stage.Dialog.IsKeyboardFocusWithin);
        Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(stage.Dialog));
        for (var i = 0; i < 3; i++)
            stage.Scene.Window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Dialog.IsKeyboardFocusWithin);

        // Back to the page once the dialog has gone from the screen, not while it is still drawn over it.
        stage.Dialog.Hide();
        Assert.True(stage.Dialog.IsKeyboardFocusWithin);
        stage.At(stage.Clock.Now.TotalMilliseconds + GoneBy);
        Assert.Same(stage.Button, focus.GetFocusedElement());
    }

    [AvaloniaFact]
    public void A_Dialog_In_A_Tree_Of_Its_Own_Cant_Be_Shown()
    {
        using var stage = new Stage();
        stage.Page.Children.Add(stage.Dialog);
        Dispatcher.UIThread.RunJobs();

        var error = Assert.Throws<InvalidOperationException>(() => { _ = stage.Dialog.ShowAsync(stage.Scene.Window); });
        Assert.Contains("declare it in resources", error.Message);
        Assert.False(stage.Dialog.IsShown);
    }

    [AvaloniaFact]
    public void Back_Hides_It_Under_A_Popup_No_Phone_Host_Has()
    {
        var plain = new Popup { Child = new Border { Width = 50, Height = 50, Background = Brushes.Red }, ShouldUseOverlayLayer = true };
        var holder = new Panel { Children = { plain } };
        using var stage = new Stage(content: holder);
        var closing = new List<ContentDialogResult>();
        stage.Dialog.Closing += (_, e) => closing.Add(e.Result);
        stage.ShowAtRest();
        plain.Open();
        Dispatcher.UIThread.RunJobs();
        Assert.True(plain.IsOpen);

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Back(request);
        Assert.True(request.Handled);
        Assert.Equal([ContentDialogResult.None], closing);
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));
        // Gone, with the popup in it.
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.False(plain.IsOpen);
    }

    [AvaloniaFact]
    public void Its_Theme_Gone_And_Back_While_Shown_Keeps_One_Host()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        var host = PhoneContentDialog.HostOf(stage.Dialog);

        stage.Dialog.Theme = new ControlTheme(typeof(ContentDialog));
        stage.Dialog.ClearValue(StyledElement.ThemeProperty);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(host, PhoneContentDialog.HostOf(stage.Dialog));
        Assert.True(stage.Dialog.Popup!.ShouldUseOverlayLayer);
        Assert.Same(stage.Scene.Window, stage.Dialog.Popup.PlacementTarget);

        stage.Back();
        Assert.Equal(ContentDialogResult.None, Done(stage.Result));
    }

    [AvaloniaFact]
    public void The_Dialog_Goes_Before_The_Host_Would_Hide_It()
    {
        // The binder closes the popup itself; the host's own close, which it pre-empts, comes later.
        Assert.True(PhoneContentDialog.GoneAfter < PhonePopupMotion.TopGoneAfter);
    }

    [AvaloniaFact]
    public void Unbounded_The_Panel_Is_As_Wide_As_Both_Hosts()
    {
        var panel = new PhoneContentDialogPanel
        {
            Children =
            {
                new Border { Width = 10, Height = 10 },
                new Border { Width = 20, Height = 10 },
                new Border { Width = 100, Height = 10 },
                new Border { Width = 80, Height = 10 },
            },
        };
        panel.Measure(Size.Infinity);
        Assert.Equal(200, panel.DesiredSize.Width);
        Assert.False(panel.IsFull);
    }


    [AvaloniaFact]
    public void The_Title_Is_SemiBold_Extra_Large_And_The_Body_Large_And_An_Underscore_Shows()
    {
        using var stage = new Stage(title: "my_title", content: "my_photo.jpg");
        stage.ShowAtRest();

        var title = (TextBlock)stage.Part<ContentPresenter>("Title").Child!;
        var body = (TextBlock)stage.Part<ContentPresenter>("PART_ContentPresenter").Child!;
        Assert.Equal(FontWeight.SemiBold, title.FontWeight);
        Assert.Equal(25.5, title.FontSize);
        Assert.Equal(FontWeight.Normal, body.FontWeight);
        Assert.Equal(18.14, body.FontSize);
        Assert.IsNotType<AccessText>(title);
        Assert.IsNotType<AccessText>(body);
        Assert.Equal("my_title", title.Text);
        Assert.Equal("my_photo.jpg", body.Text);
        Assert.Equal(25.5 * PhoneTextLineHeight, title.Bounds.Height, RawPixel);
    }

    [AvaloniaFact]
    public void The_Dim_Follows_The_Windows_Theme_While_Shown()
    {
        using var stage = new Stage();
        stage.ShowAtRest();
        var layer = (Panel)stage.Dialog.GetVisualAncestors().OfType<OverlayPopupHost>().Single().GetVisualParent()!;
        var dim = layer.Children.OfType<Rectangle>().Single();

        stage.Scene.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Dim(dark: false), ((ISolidColorBrush)dim.Fill!).Color);
    }

    [AvaloniaFact]
    public void Resized_Mid_Opening_It_Keeps_Its_Own_Curve()
    {
        using var stage = new Stage();
        stage.Show();
        var first = stage.FirstFrameAfter(0);
        stage.At(first + 2 * Tick);

        stage.Scene.Window.Height = 700;
        Dispatcher.UIThread.RunJobs();
        for (var tick = 3; tick < OpenTicks; tick++)
        {
            stage.At(first + tick * Tick);
            Assert.Equal(Eased(tick), stage.Bands!.Angles[0], 1e-9);
        }
    }

    [AvaloniaFact]
    public void Closed_It_Takes_No_Taps_While_Still_Drawn_And_Then_Leaves_Its_Window()
    {
        using var stage = new Stage();
        stage.ShowAtRest();

        stage.Dialog.Hide();
        Assert.False(stage.Dialog.IsHitTestVisible);
        stage.At(stage.Clock.Now.TotalMilliseconds + GoneBy);
        Assert.Null(stage.Dialog.Popup!.Parent);
        Assert.Null(stage.Dialog.Popup.PlacementTarget);
        Assert.True(stage.Dialog.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void A_Closing_Deferral_Of_An_Earlier_Show_Decides_Nothing()
    {
        var stage = new Stage();
        try
        {
            var deferrals = new List<ContentDialogClosingDeferral>();
            stage.Dialog.Closing += (_, e) => deferrals.Add(e.GetDeferral());
            stage.ShowAtRest();
            stage.Dialog.Hide();
            // The popup closed under it, by its window: the show ends with no result.
            stage.Dialog.Popup!.IsOpen = false;
            Dispatcher.UIThread.RunJobs();
            var first = stage.Result;
            Assert.Equal(ContentDialogResult.None, Done(first));

            stage.ShowAtRest();
            stage.Dialog.Hide();
            deferrals[0].Complete();
            Dispatcher.UIThread.RunJobs();
            Assert.False(stage.Result!.IsCompleted);
            deferrals[1].Complete();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ContentDialogResult.None, Done(stage.Result));
        }
        finally
        {
            stage.Dispose();
        }
    }

    private sealed class Command : ICommand
    {
        public List<object?> Ran { get; } = [];

        public bool CanRun { get; init; } = true;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => CanRun;

        public void Execute(object? parameter) => Ran.Add(parameter);
    }

}
