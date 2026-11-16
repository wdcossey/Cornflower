using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.FlyoutReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's Flyout: Wp81's FlyoutPresenter theme (<c>Controls/FlyoutPresenter.axaml</c>) and the popup round it
/// (<see cref="PhoneFlyout"/>, <see cref="PhonePopupHost"/>), against the emulator's screenshots and dumps and the
/// recording's times (<see cref="FlyoutReadings"/>). docs/controls/Flyout/PLAN.md has each reading. The motion's curve
/// and the dismissal rules wait on #149 and #150: what is asserted here is what is known.
/// </summary>
public class FlyoutTests
{
    /// <summary>The reference app's stage: a page with the "open" Button, the flyout shown on it.</summary>
    internal sealed class Stage : IDisposable
    {
        public Stage(ThemeVariant? variant = null, bool full = false, PlacementMode? placement = null,
            Control? root = null, double width = Scene.ReferenceWidth, double height = Scene.ReferenceHeight,
            IEnumerable<IStyle>? themes = null, IBrush? page = null)
        {
            Clock = new TestClock();
            Flyout = new Flyout { Content = new TextBlock { Text = "flyout content", TextWrapping = TextWrapping.Wrap } };
            if (placement is { } p)
                Flyout.Placement = p;
            FlyoutOptions.SetIsFullScreen(Flyout, full);
            Button = new Button
            {
                Content = "open",
                Margin = new Thickness(19, 377.08, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            try
            {
                Scene = new Scene(root ?? new Panel { Background = page, Children = { Button } }, variant, width, height, themes: themes);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public Flyout Flyout { get; }

        public Button Button { get; }

        public Scene Scene { get; }

        public FlyoutPresenter Presenter => (FlyoutPresenter)Flyout.Popup.Child!;

        public void Open()
        {
            Flyout.ShowAt(Button);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Opens the flyout and lets its motion end.</summary>
        public void OpenAtRest()
        {
            Open();
            At(Clock.Now.TotalMilliseconds + 500);
        }

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>The presenter's laid-out bounds in the window, without the motion's transform.</summary>
        public Rect Bounds()
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var host = Presenter.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
            return new Rect(Canvas.GetLeft(host), Canvas.GetTop(host), Presenter.Bounds.Width, Presenter.Bounds.Height);
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    [AvaloniaFact]
    public void The_Presenter_Is_Templated_By_The_Theme()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        var theme = Assert.IsType<ControlTheme>(stage.Scene.Window.FindResource(typeof(FlyoutPresenter)));
        Assert.Equal(typeof(FlyoutPresenter), theme.TargetType);
        var template = theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
        Assert.Same(template, stage.Presenter.Template);
        var background = stage.Presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "Background");
        Assert.Contains(background.GetVisualDescendants(), d => d is ScrollViewer { Name: "ScrollViewer" });
        Assert.True(PhoneFlyout.GetIsEnabled(stage.Presenter));
        Assert.Equal(MinHeight, stage.Presenter.MinHeight);
        Assert.Same(stage.Presenter.FindResource(stage.Presenter.ActualThemeVariant, "PhoneForegroundBrush"), stage.Presenter.Foreground);
    }

    [AvaloniaFact]
    public void The_Popup_Is_In_The_Windows_Own_Overlay_Layer()
    {
        // The phone has one window and draws its popups in it: on desktop too, never a window of their own.
        using var stage = new Stage();
        stage.OpenAtRest();

        Assert.True(stage.Flyout.Popup.IsUsingOverlayLayer);
        Assert.Same(stage.Scene.Window, TopLevel.GetTopLevel(stage.Presenter));
    }

    [AvaloniaFact]
    public void A_Top_Flyout_Is_At_The_Windows_Top_Left_Its_Width()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        var bounds = stage.Bounds();
        Assert.Equal(Top.X, bounds.X);
        Assert.Equal(Top.Y, bounds.Y);
        Assert.Equal(Top.Width, bounds.Width);
        // 189 raw rows to the phone's 188: layout rounds the ScrollBar's 53 up to the raw grid, where the phone rounds to
        // the nearest (ADR 0003's recorded deviation).
        Assert.Equal(Top.Height, bounds.Height, 1 / Scale + 0.001);
        var scroll = stage.Presenter.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "ScrollViewer");
        Assert.Equal(ContentTop, scroll.Bounds.Y, 0.001);
        Assert.Equal(TopContentHeight, scroll.Bounds.Height, 1 / Scale + 0.001);
    }

    [AvaloniaTheory]
    [InlineData(PlacementMode.Top)]
    [InlineData(PlacementMode.Bottom)]
    [InlineData(PlacementMode.Left)]
    [InlineData(PlacementMode.Right)]
    [InlineData(PlacementMode.Pointer)]
    public void Any_Placement_But_Full_Is_At_The_Top(PlacementMode placement)
    {
        // The phone draws WinRT's Top, Bottom, Left and Right all across the top of the window.
        using var stage = new Stage(placement: placement);
        stage.OpenAtRest();

        var bounds = stage.Bounds();
        Assert.Equal(new Point(0, 0), bounds.Position);
        Assert.Equal(Top.Width, bounds.Width);
    }

    [AvaloniaFact]
    public void A_Full_Flyout_Fills_The_Window()
    {
        using var stage = new Stage(full: true);
        stage.OpenAtRest();

        var bounds = stage.Bounds();
        Assert.Equal(new Rect(0, 0, 450, 800), bounds);
        Assert.Equal(Full.Height, bounds.Height, 0.001);
    }

    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void The_Chrome_Is_The_Phones_And_The_Page_Under_A_Top_Flyout_Isnt_Dimmed(bool dark, bool full)
    {
        using var stage = new Stage(dark ? ThemeVariant.Dark : ThemeVariant.Light, full, page: new SolidColorBrush(Page(dark)));
        stage.OpenAtRest();

        using var frame = stage.Scene.Capture();
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1000, 5));
        Assert.Equal(Chrome(dark), Frames.PixelAt(frame, 1000, TopRows - 1));
        Assert.Equal(full ? Chrome(dark) : Page(dark), Frames.PixelAt(frame, 1000, TopRows + 2));
        Assert.Equal(full ? Chrome(dark) : Page(dark), Frames.PixelAt(frame, 1000, 1900));
    }

    [AvaloniaFact]
    public void The_Content_Sits_Below_The_Portrait_Margin_At_The_Left_Edge()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        var text = stage.Presenter.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "flyout content");
        var at = text.TranslatePoint(default, stage.Scene.Window)!.Value;
        Assert.Equal(InkLeftColumn / Scale, at.X, 0.001);
        // The phone's ink starts 66 raw rows down, in its 11 px font; ours in the page's: the line's top is the margin's.
        Assert.Equal(ContentTop, at.Y, 0.001);
        Assert.True(at.Y < InkTopRow / Scale);
    }

    [AvaloniaFact]
    public void In_Landscape_The_Content_Has_No_Top_Margin_And_The_Flyout_The_Windows_Width()
    {
        using var stage = new Stage(width: 800, height: 450);
        stage.OpenAtRest();

        Assert.True(PhonePopupHost.GetIsLandscape(stage.Presenter));
        var scroll = stage.Presenter.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "ScrollViewer");
        Assert.Equal(0, scroll.Bounds.Y);
        Assert.Equal(800, stage.Bounds().Width);
    }

    [AvaloniaFact]
    public void The_Flyout_Follows_The_Windows_Size_While_Open()
    {
        using var stage = new Stage(full: true);
        stage.OpenAtRest();

        stage.Scene.Window.Width = 400;
        stage.Scene.Window.Height = 711;
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Clock.Now.TotalMilliseconds + 50);

        var bounds = stage.Bounds();
        Assert.Equal(new Rect(0, 0, 400, 711), bounds, new RectComparer(1 / Scale));
    }

    [AvaloniaFact]
    public void A_Top_Flyout_Swings_Down_From_Its_Top_Edge_And_Is_At_Rest_By_Its_99_Percent_Time()
    {
        using var stage = new Stage();
        stage.Open();

        // Its first frame isn't at rest: the top edge where it rests, the bottom edge drawn above its own.
        var swung = stage.Presenter.RenderTransform?.Value;
        Assert.NotNull(swung);
        var height = stage.Presenter.Bounds.Height;
        Assert.Equal(new Point(0, 0), Draw(swung.Value, new Point(0, 0)));
        Assert.True(Draw(swung.Value, new Point(225, height)).Y < height - 1);

        // Time 0 is the motion's first frame, 16 ms in.
        stage.At(16 + PhonePopupMotion.TopOpenLength.TotalMilliseconds - 16);
        Assert.NotNull(stage.Presenter.RenderTransform);
        stage.At(16 + PhonePopupMotion.TopOpenLength.TotalMilliseconds + 16);
        Assert.Null(stage.Presenter.RenderTransform);
        Assert.InRange(PhonePopupMotion.TopOpenLength.TotalMilliseconds, TopOpenAt99, TopOpenAt99 + RecordingTolerance);
    }

    [AvaloniaFact]
    public void A_Full_Flyout_Swings_In_And_Is_At_Rest_By_Its_99_Percent_Time()
    {
        using var stage = new Stage(full: true);
        stage.Open();

        Assert.NotNull(stage.Presenter.RenderTransform);
        stage.At(16 + PhonePopupMotion.FullOpenLength.TotalMilliseconds - 16);
        Assert.NotNull(stage.Presenter.RenderTransform);
        stage.At(16 + PhonePopupMotion.FullOpenLength.TotalMilliseconds + 16);
        Assert.Null(stage.Presenter.RenderTransform);
        Assert.InRange(PhonePopupMotion.FullOpenLength.TotalMilliseconds, FullOpenAt99 - RecordingTolerance, FullOpenAt99 + RecordingTolerance);
    }

    [AvaloniaFact]
    public void A_Top_Flyout_Stays_At_Rest_After_Hide_Then_Goes_In_One_Frame()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var hidden = stage.Clock.Now.TotalMilliseconds;
        var closed = 0;
        stage.Flyout.Closed += (_, _) => closed++;

        stage.Flyout.Hide();
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Flyout.IsOpen);
        stage.At(hidden + TopGoneAfterHide - 1);
        Assert.True(stage.Flyout.Popup.IsOpen);
        Assert.Null(stage.Presenter.RenderTransform);
        Assert.Equal(0, closed);

        stage.At(hidden + TopGoneAfterHide);
        Assert.False(stage.Flyout.IsOpen);
        Assert.False(stage.Flyout.Popup.IsOpen);
        Assert.Equal(1, closed);
    }

    [AvaloniaFact]
    public void A_Full_Flyout_Swings_Out_After_Hide_And_Is_Gone_By_Its_99_Percent_Time()
    {
        using var stage = new Stage(full: true);
        stage.OpenAtRest();
        var hidden = stage.Clock.Now.TotalMilliseconds;

        stage.Flyout.Hide();
        stage.At(hidden + 150);
        Assert.True(stage.Flyout.Popup.IsOpen);
        Assert.NotNull(stage.Presenter.RenderTransform);

        stage.At(hidden + 16 + FullCloseAt99 - 16);
        Assert.True(stage.Flyout.Popup.IsOpen);
        // Time 0 is its first frame, 16 ms after Hide().
        stage.At(hidden + 16 + FullCloseAt99 + 16);
        Assert.False(stage.Flyout.Popup.IsOpen);
        Assert.Null(stage.Presenter.RenderTransform);
        Assert.InRange(PhonePopupMotion.FullCloseLength.TotalMilliseconds, FullCloseAt99 - RecordingTolerance, FullCloseAt99 + RecordingTolerance);
    }

    [AvaloniaFact]
    public void Setting_IsOpen_False_Closes_It_The_Same_Way()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var hidden = stage.Clock.Now.TotalMilliseconds;

        stage.Flyout.IsOpen = false;
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Flyout.Popup.IsOpen);
        stage.At(hidden + TopGoneAfterHide);
        Assert.False(stage.Flyout.Popup.IsOpen);
        Assert.False(stage.Flyout.IsOpen);
    }

    [AvaloniaFact]
    public void A_Tap_Below_A_Top_Flyout_Closes_It()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var tapped = stage.Clock.Now.TotalMilliseconds;
        var clicks = 0;
        stage.Button.Click += (_, _) => clicks++;

        var centre = stage.Button.TranslatePoint(new Point(stage.Button.Bounds.Width / 2, stage.Button.Bounds.Height / 2), stage.Scene.Window)!.Value;
        stage.Scene.Window.MouseDown(centre, MouseButton.Left);
        stage.Scene.Window.MouseUp(centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        stage.At(tapped + TopGoneAfterHide);

        Assert.False(stage.Flyout.Popup.IsOpen);
        // PENDING #150: whether the tap reaches the page. Today it doesn't, as Avalonia's flyout has it.
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void It_Opens_Again_Placed_And_Moving()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        stage.Flyout.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 200);
        Assert.False(stage.Flyout.Popup.IsOpen);

        stage.Open();
        Assert.NotNull(stage.Presenter.RenderTransform);
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.Equal(new Point(0, 0), stage.Bounds().Position);
        Assert.Equal(Top.Width, stage.Bounds().Width);
    }

    [AvaloniaFact]
    public void Opened_By_IsOpen_It_Is_Placed_From_Its_Own_Options_Too()
    {
        // Opened by IsOpen, the flyout opens its popup before it says it is open: the popup is placed again then.
        using var stage = new Stage(full: true);
        stage.OpenAtRest();
        stage.Flyout.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);

        FlyoutOptions.SetIsFullScreen(stage.Flyout, false);
        stage.Flyout.IsOpen = true;
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(stage.Flyout.Popup.IsOpen);
        Assert.True(stage.Bounds().Height < 100);

        stage.Flyout.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        FlyoutOptions.SetIsFullScreen(stage.Flyout, true);
        stage.Flyout.IsOpen = true;
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.Equal(new Rect(0, 0, 450, 800), stage.Bounds());
    }

    [AvaloniaFact]
    public void Back_Closes_The_Flyout_Before_The_NavigationPage_Goes_Back()
    {
        // Avalonia's PageNavigationHost passes the window's Back to its NavigationPage from an instance handler; the
        // popup's class handler runs first.
        var first = new ContentPage();
        var second = new ContentPage();
        var nav = new NavigationPage();
        using var stage = new Stage(root: new PageNavigationHost { Page = nav });
        _ = nav.PushAsync(first);
        _ = nav.PushAsync(second, null);
        Dispatcher.UIThread.RunJobs();
        second.Content = stage.Button;
        Dispatcher.UIThread.RunJobs();
        stage.OpenAtRest();
        var back = stage.Clock.Now.TotalMilliseconds;

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(request);
        Dispatcher.UIThread.RunJobs();
        Assert.True(request.Handled);
        Assert.Same(second, nav.CurrentPage);
        stage.At(back + TopGoneAfterHide);
        Assert.False(stage.Flyout.Popup.IsOpen);

        // With no popup open, Back is the page's again.
        var next = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(next);
        Dispatcher.UIThread.RunJobs();
        Assert.True(next.Handled);
        Assert.Same(first, nav.CurrentPage);
    }

    [AvaloniaFact]
    public void Back_With_No_Popup_Open_Is_Left_Alone()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        stage.Flyout.Hide();
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(request);
        Assert.False(request.Handled);
    }

    [AvaloniaFact]
    public void Back_Takes_Only_Its_Own_Windows_Popup()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        using var other = new Scene(new Panel());

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        other.Window.RaiseEvent(request);
        Assert.False(request.Handled);
        stage.At(stage.Clock.Now.TotalMilliseconds + 500);
        Assert.True(stage.Flyout.Popup.IsOpen);
    }

    [AvaloniaFact]
    public void A_Host_With_A_Dim_Brush_Dims_The_Window_Under_Its_Popup_While_Open()
    {
        // No Flyout dims; a ContentDialog and a MenuFlyout will, as the phone's full-window Rectangle does under them.
        using var clock = new TestClock();
        var content = new Border { Height = 100, Background = Brushes.Red };
        var popup = new Popup { Child = content };
        var target = new Panel { Children = { popup } };
        using var scene = new Scene(target);
        using var host = new PhonePopupHost(popup, content, () => popup.Close()) { Dim = Brushes.Black };

        popup.Open();
        Dispatcher.UIThread.RunJobs();
        var layer = content.GetVisualAncestors().OfType<OverlayPopupHost>().Single().GetVisualParent<Panel>()!;
        var dim = layer.Children.OfType<Rectangle>().Single();
        Assert.Same(Brushes.Black, dim.Fill);
        Assert.Equal(new Size(450, 800), new Size(dim.Width, dim.Height));
        Assert.True(layer.Children.IndexOf(dim) < layer.Children.IndexOf(content.GetVisualAncestors().OfType<OverlayPopupHost>().Single()));
        Assert.Equal(!PhonePopupDismissal.DimPassesTaps, dim.IsHitTestVisible);

        host.Dismiss();
        clock.At(500);
        Dispatcher.UIThread.RunJobs();
        Assert.False(popup.IsOpen);
        Assert.Empty(layer.Children.OfType<Rectangle>());
    }

    [AvaloniaFact]
    public void A_Flyout_Doesnt_Dim()
    {
        using var stage = new Stage();
        stage.OpenAtRest();

        var layer = stage.Presenter.GetVisualAncestors().OfType<OverlayPopupHost>().Single().GetVisualParent<Panel>()!;
        Assert.Empty(layer.Children.OfType<Rectangle>());
    }

    [AvaloniaFact]
    public void Simple_Alone_Keeps_Avalonias_Own_Flyout()
    {
        using var stage = new Stage(themes: [new SimpleTheme()]);
        stage.Open();

        Assert.False(PhoneFlyout.GetIsEnabled(stage.Presenter));
        Assert.Null(PhoneFlyout.HostOf(stage.Presenter));
        Assert.Null(stage.Presenter.RenderTransform);
        stage.Flyout.Hide();
        Assert.False(stage.Flyout.Popup.IsOpen);
    }

    private sealed class RectComparer(double tolerance) : IEqualityComparer<Rect>
    {
        public bool Equals(Rect a, Rect b) =>
            Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance
            && Math.Abs(a.Width - b.Width) <= tolerance && Math.Abs(a.Height - b.Height) <= tolerance;

        public int GetHashCode(Rect rect) => 0;
    }

    private static Point Draw(Matrix matrix, Point point) => matrix.Transform(point);
}
