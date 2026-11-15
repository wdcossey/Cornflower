using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's page and Frame page: in the catalogue by #120, the theme's pages, its three transitions, and Back.</summary>
public class PagePageTests
{
    private static void Settle(TestClock clock, double ms)
    {
        clock.At(clock.Now.TotalMilliseconds + ms);
        Dispatcher.UIThread.RunJobs();
    }

    private static Button ButtonOn(NavigationPage nav, string name) =>
        ((ContentPage)nav.CurrentPage!).GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == name);

    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ScrollViewer_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(PagePage));
        Assert.Equal("page", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ScrollViewerPage)));
    }

    [AvaloniaFact]
    public void It_Opens_On_Its_First_Page_Headed_By_The_Pages_Title_With_The_Themes_Pages()
    {
        var page = new PagePage();
        using var scene = new Scene(page);
        var nav = page.FindControl<NavigationPage>("Navigation")!;

        Assert.Equal(1, page.Depth);
        Assert.IsType<TurnstileTransition>(nav.PageTransition);
        var first = Assert.IsType<ContentPage>(nav.CurrentPage);
        Assert.Equal("page", first.GetVisualDescendants().OfType<SamplePageHeader>().Single().Title);
        Assert.False(NavigationPage.GetHasNavigationBar(first));
        Assert.DoesNotContain(first.GetVisualDescendants().OfType<Button>(), b => b.Content as string == "back");
    }

    [AvaloniaFact]
    public void Each_Button_Goes_Forward_With_Its_Transition_And_Back_Goes_Back_With_It()
    {
        var page = new PagePage();
        using var scene = new Scene(page);
        using var clock = new TestClock();
        var nav = page.FindControl<NavigationPage>("Navigation")!;

        Click(ButtonOn(nav, "slide"));
        // The slide: the new page rises from below, not turned.
        var presenter = (Visual)nav.CurrentPage!.GetVisualParent()!;
        Assert.Equal(0, presenter.TranslatePoint(new Point(0, 0), nav)!.Value.X, 0.01);
        Assert.True(presenter.TranslatePoint(new Point(0, 0), nav)!.Value.Y > 100);
        Settle(clock, 1000);
        Assert.Equal(2, page.Depth);

        Click(ButtonOn(nav, "turnstile"));
        Settle(clock, 1000);
        Click(ButtonOn(nav, "continuum"));
        Settle(clock, 1000);
        Assert.Equal(4, page.Depth);

        Click(ButtonOn(nav, "back"));
        // Back from the continuum's page drops it first, as the continuum's back does.
        presenter = nav.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
            .Single(p => p.Name is "PART_PagePresenter" or "PART_PageBackPresenter" && p.ZIndex == 1);
        Settle(clock, 100);
        Assert.True(presenter.TranslatePoint(new Point(0, 0), nav)!.Value.Y > 1);
        Settle(clock, 1000);
        Assert.Equal(3, page.Depth);
    }

    [AvaloniaFact]
    public void The_Platforms_Back_Request_Goes_Back_A_Page_And_On_The_First_Is_Left_For_The_Shell()
    {
        var page = new PagePage();
        using var scene = new Scene(page);
        using var clock = new TestClock();
        var nav = page.FindControl<NavigationPage>("Navigation")!;
        Click(ButtonOn(nav, "turnstile"));
        Settle(clock, 1000);

        var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        scene.Window.RaiseEvent(request);
        Settle(clock, 1000);
        Assert.True(request.Handled);
        Assert.Equal(1, page.Depth);

        var last = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        scene.Window.RaiseEvent(last);
        Assert.False(last.Handled);
    }
}
