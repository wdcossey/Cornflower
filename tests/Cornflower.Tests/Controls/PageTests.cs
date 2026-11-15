using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PageReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's page and Frame: Wp81's ContentPage (<c>Controls/ContentPage.axaml</c>) and NavigationPage
/// (<c>Controls/NavigationPage.axaml</c>), against where the emulator lays out a page and its content
/// (<see cref="PageReadings.Bounds"/>), and the phone's Frame and page styles (<c>frame.json</c>, <c>page.json</c>).
/// docs/controls/Page/PLAN.md has each reading.
/// </summary>
public class PageTests
{
    private static readonly Thickness Tray = new(0, Bounds.TrayHeight, 0, 0);

    /// <summary>A page whose content is a plain sheet, named so its bounds can be read.</summary>
    private static ContentPage Page(out Border content, IBrush? brush = null)
    {
        content = new Border { Background = brush ?? Brushes.Blue };
        return new ContentPage { Content = content };
    }

    private static Rect BoundsIn(Visual visual, Visual root) =>
        new(visual.TranslatePoint(new Point(0, 0), root)!.Value, visual.Bounds.Size);

    private static void AssertRect(Rect expected, Rect actual, string what)
    {
        // Layout rounds to the raw pixel grid, 1/2.4 view px (ADR 0003).
        const double grid = 1 / Scene.ReferenceScaling;
        Assert.True(Math.Abs(expected.X - actual.X) <= grid && Math.Abs(expected.Y - actual.Y) <= grid &&
                    Math.Abs(expected.Width - actual.Width) <= grid && Math.Abs(expected.Height - actual.Height) <= grid,
            $"{what}: expected {expected}, laid out at {actual}");
    }

    private static Color ColourOf(string key, ThemeVariant variant, Visual anywhere)
    {
        Assert.True(anywhere.TryFindResource(key, variant, out var value));
        return Assert.IsAssignableFrom<ISolidColorBrush>(value).Color;
    }

    [AvaloniaFact]
    public void A_ContentPage_And_A_NavigationPage_Are_Templated_By_The_Theme()
    {
        var page = Page(out var content);
        var nav = new NavigationPage();
        using var scene = new Scene(new Grid { Children = { page, nav } });

        // The bars and the content are inside PART_ContentPresenter, which the safe area pads: in the page, not its template.
        var parts = page.GetVisualDescendants().OfType<Control>().Select(c => c.Name).ToList();
        Assert.Contains("PART_ContentPresenter", parts);
        Assert.Contains("PART_Content", parts);
        Assert.Contains("PART_BottomCommandBar", parts);
        Assert.Contains("PART_TopCommandBar", parts);
        Assert.Same(page, content.GetVisualAncestors().OfType<ContentPage>().First());
        var navParts = nav.GetVisualDescendants().OfType<Control>().Where(c => c.TemplatedParent == nav).Select(c => c.Name).ToList();
        Assert.Contains("PART_ContentHost", navParts);
        Assert.Contains("PART_NavigationBar", navParts);
        Assert.Contains("PART_BackButton", navParts);
        Assert.Contains("PART_ModalPresenter", navParts);
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void The_Page_Fills_The_Window_In_The_Phones_Page_Background_And_Foreground(string variantName)
    {
        var variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var page = new ContentPage { Content = new TextBlock { Text = "page" } };
        using var scene = new Scene(page, variant);

        AssertRect(new Rect(0, 0, 450, 800), page.Bounds, "the page");
        var background = ColourOf("ApplicationPageBackgroundThemeBrush", variant, page);
        Assert.Equal(background, Assert.IsAssignableFrom<ISolidColorBrush>(page.Background).Color);
        Assert.Equal(ColourOf("PhoneForegroundBrush", variant, page), Assert.IsAssignableFrom<ISolidColorBrush>(((TextBlock)page.Content!).Foreground).Color);
        using var frame = scene.Capture();
        Assert.Equal(background, Frames.PixelAt(frame, 1080 - 5, 1920 - 5));
        Assert.Equal(background, Frames.PixelAt(frame, 5, 5));
    }

    [AvaloniaFact]
    public void The_Content_Is_Laid_Out_Below_The_Tray_And_The_Page_Background_Reaches_Under_It()
    {
        var page = Page(out var content);
        using var scene = new Scene(page);

        page.SafeAreaPadding = Tray;
        Dispatcher.UIThread.RunJobs();

        AssertRect(new Rect(0, 0, 450, 800), page.Bounds, "the page");
        AssertRect(new Rect(0, Bounds.TrayHeight, 450, 800 - Bounds.TrayHeight), BoundsIn(content, page), "the content");
        using var frame = scene.Capture();
        Assert.Equal(ColourOf("ApplicationPageBackgroundThemeBrush", ThemeVariant.Dark, page), Frames.PixelAt(frame, 540, 10));
        Assert.Equal(Colors.Blue, Frames.PixelAt(frame, 540, 80));
    }

    [AvaloniaFact]
    public void With_The_Safe_Area_Not_Applied_The_Content_Fills_The_Window_As_Under_UseCoreWindow()
    {
        var page = Page(out var content);
        page.AutomaticallyApplySafeAreaPadding = false;
        using var scene = new Scene(page);

        page.SafeAreaPadding = Tray;
        Dispatcher.UIThread.RunJobs();

        AssertRect(new Rect(0, 0, 450, 800), BoundsIn(content, page), "the content");
    }

    [AvaloniaTheory]
    [InlineData(Bounds.CompactBarTop)]
    [InlineData(Bounds.MinimalBarTop)]
    public void A_Bottom_Bar_Takes_Its_Height_Off_The_Content_As_The_Phones_App_Bar_Takes_It_Off_VisibleBounds(double barTop)
    {
        var bar = new Border { Height = 800 - barTop, Background = Brushes.Red };
        var page = Page(out var content);
        page.BottomCommandBar = bar;
        using var scene = new Scene(page);

        AssertRect(new Rect(0, barTop, 450, 800 - barTop), BoundsIn(bar, page), "the bar");
        AssertRect(new Rect(0, 0, 450, barTop), BoundsIn(content, page), "the content");

        // With the tray shown too, the content is VisibleBounds: 0, 27.92 and down to the bar.
        page.SafeAreaPadding = Tray;
        Dispatcher.UIThread.RunJobs();
        AssertRect(new Rect(0, barTop, 450, 800 - barTop), BoundsIn(bar, page), "the bar, tray shown");
        AssertRect(new Rect(0, Bounds.TrayHeight, 450, barTop - Bounds.TrayHeight), BoundsIn(content, page), "the content, tray shown");
    }

    [AvaloniaFact]
    public void A_Bar_Drawn_Past_Its_Place_Draws_Over_The_Content()
    {
        // An open app bar reaches up over the page without the page moving (the phone's VisibleBounds stays).
        var bar = new Border { Height = 62.92, Background = Brushes.Red, RenderTransform = new TranslateTransform(0, -50) };
        var top = new Border { Height = 40, Background = Brushes.Lime, RenderTransform = new TranslateTransform(0, 30) };
        var page = Page(out var content);
        page.BottomCommandBar = bar;
        page.TopCommandBar = top;
        using var scene = new Scene(page);

        AssertRect(new Rect(0, 40, 450, Bounds.CompactBarTop - 40), BoundsIn(content, page), "the content");
        using var frame = scene.Capture();
        Assert.Equal(Colors.Red, Frames.PixelAt(frame, 540, (int)((Bounds.CompactBarTop - 25) * 2.4)));
        Assert.Equal(Colors.Lime, Frames.PixelAt(frame, 540, (int)(55 * 2.4)));
    }

    [AvaloniaFact]
    public void A_Top_Bar_Sits_Below_The_Tray()
    {
        var bar = new Border { Height = 40 };
        var page = Page(out var content);
        page.TopCommandBar = bar;
        using var scene = new Scene(page);

        page.SafeAreaPadding = Tray;
        Dispatcher.UIThread.RunJobs();
        AssertRect(new Rect(0, Bounds.TrayHeight, 450, 40), BoundsIn(bar, page), "the bar");
        AssertRect(new Rect(0, Bounds.TrayHeight + 40, 450, 800 - Bounds.TrayHeight - 40), BoundsIn(content, page), "the content");
    }

    [AvaloniaFact]
    public void In_A_NavigationPage_A_ContentPage_Has_No_Navigation_Bar_And_Gets_The_Whole_Safe_Area()
    {
        var page = Page(out var content);
        var nav = new NavigationPage();
        using var scene = new Scene(nav);
        _ = nav.PushAsync(page);
        Dispatcher.UIThread.RunJobs();

        nav.SafeAreaPadding = Tray;
        Dispatcher.UIThread.RunJobs();

        Assert.False(NavigationPage.GetHasNavigationBar(page));
        // Nor an edge swipe: the phone's back is the hardware button.
        Assert.False(nav.IsGestureEnabled);
        Assert.False(nav.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_NavigationBar").IsEffectivelyVisible);
        AssertRect(new Rect(0, 0, 450, 800), BoundsIn(page, nav), "the page");
        AssertRect(new Rect(0, Bounds.TrayHeight, 450, 800 - Bounds.TrayHeight), BoundsIn(content, nav), "the content");
        Assert.Equal(ColourOf("ApplicationPageBackgroundThemeBrush", ThemeVariant.Dark, nav), Assert.IsAssignableFrom<ISolidColorBrush>(nav.Background).Color);
    }

    [AvaloniaFact]
    public void A_Page_That_Turns_The_Bar_Back_On_Gets_A_Header_And_A_Back_Arrow_In_The_Phones_Chrome()
    {
        var first = new ContentPage { Header = "first" };
        var second = new ContentPage { Header = "second" };
        NavigationPage.SetHasNavigationBar(second, true);
        var nav = new NavigationPage();
        using var scene = new Scene(nav);
        _ = nav.PushAsync(first);
        _ = nav.PushAsync(second, null);
        Dispatcher.UIThread.RunJobs();

        var bar = nav.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_NavigationBar");
        Assert.True(bar.IsEffectivelyVisible);
        Assert.Equal(ColourOf("PhoneChromeBrush", ThemeVariant.Dark, nav), Assert.IsAssignableFrom<ISolidColorBrush>(bar.Background).Color);
        Assert.Contains(bar.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "second");
        var back = bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_BackButton");
        Assert.True(back.IsEffectivelyVisible);
        // Its own theme, not the phone's Button: no tilt, no border, no minimum size.
        Assert.Same(back.FindResource("Wp81NavigationBackButtonTheme"), back.Theme);
        Assert.False(Tilt.GetIsEnabled(back));
        Assert.Contains(back.GetVisualDescendants(), d => d is LucideIcon { Kind: LucideIconKind.ArrowLeft });

        back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Same(first, nav.CurrentPage);
    }

    [AvaloniaFact]
    public void A_NavigationPage_In_A_PageNavigationHost_Goes_Back_On_The_Platforms_Back_Request()
    {
        // The hardware back button is the app's: hosted so, Avalonia forwards the window's back request to the page.
        var first = new ContentPage();
        var second = new ContentPage();
        var nav = new NavigationPage();
        using var scene = new Scene(new PageNavigationHost { Page = nav });
        _ = nav.PushAsync(first);
        _ = nav.PushAsync(second, null);
        Dispatcher.UIThread.RunJobs();

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        scene.Window.RaiseEvent(request);
        Dispatcher.UIThread.RunJobs();

        Assert.True(request.Handled);
        Assert.Same(first, nav.CurrentPage);
        var last = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        scene.Window.RaiseEvent(last);
        Assert.False(last.Handled);
    }

    [AvaloniaFact]
    public void Simple_Alone_Keeps_Its_Own_Pages()
    {
        var page = new ContentPage();
        var nav = new NavigationPage();
        using var scene = new Scene(new Grid { Children = { page, nav } }, themes: [new SimpleTheme()]);

        Assert.True(NavigationPage.GetHasNavigationBar(page));
        Assert.IsNotType<TurnstileTransition>(nav.PageTransition);
        Assert.DoesNotContain(page.GetVisualDescendants().OfType<ContentPresenter>(), p => p.Name == "PART_Content");
    }
}
