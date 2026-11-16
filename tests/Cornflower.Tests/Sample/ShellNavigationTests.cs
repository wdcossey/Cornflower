using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Xunit;

using static Cornflower.Tests.Sample.ShellTurns;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The shell's pages are Wp81's Frame and Page: a <see cref="NavigationPage"/> whose first page is the list, and a
/// <see cref="ContentPage"/> pushed for each sample page, so a page comes in with the turnstile and Back turns back to
/// the list (#120's proposal, the user's decision of 2026-11-15). The navigation page drops a navigation asked for while
/// another runs; the shell asks again once it ends. The turns run on a <see cref="TestClock"/>.
/// </summary>
public class ShellNavigationTests
{
    /// <summary>The shell's rail for Back, beside the page.</summary>
    private const double RailWidth = 40;

    [AvaloniaFact]
    public void A_Page_Comes_In_With_The_Turnstile_And_Back_Turns_Back_To_The_List()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        using var clock = new TestClock();
        var host = Host(shell);
        var index = host.CurrentPage;
        var page = new Border();

        shell.ShowPage(page, "a page");
        Draw();
        var holder = page.FindAncestorOfType<ContentPage>()!;
        Assert.Same(holder, host.CurrentPage);
        Assert.Same(host, holder.FindAncestorOfType<NavigationPage>());
        Assert.True(IsTurned(page));

        Settle(clock);
        Assert.False(IsTurned(page));
        Assert.Equal(new Rect(0, 0, Scene.ReferenceWidth, Scene.ReferenceHeight), page.Bounds);

        shell.FindControl<Button>("BackButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Draw();
        Assert.False(shell.IsShowingPage);
        Assert.True(IsTurned(Index(shell)));

        Settle(clock);
        Assert.Same(index, host.CurrentPage);
        Assert.Equal(1, host.StackDepth);
        Assert.False(IsTurned(Index(shell)));
        Assert.Null(TopLevel.GetTopLevel(page));                  // popped, out of the window
    }

    // The navigation page drops a pop asked for mid-turn; the shell asks again when the turn ends.
    [AvaloniaFact]
    public void Back_In_The_Middle_Of_The_Turn_Goes_Back_When_It_Ends()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        var page = new Border();

        shell.ShowPage(page, "a page");
        Draw();
        clock.At(100);
        Draw();
        shell.ShowIndex();
        Draw();
        Assert.False(shell.IsShowingPage);
        Assert.Same(page.FindAncestorOfType<ContentPage>(), host.CurrentPage);   // still turning in

        Settle(clock);
        Assert.Equal(1, host.StackDepth);
        Assert.Same(Index(shell).FindAncestorOfType<ContentPage>(), host.CurrentPage);
        Assert.False(IsTurned(Index(shell)));
    }

    // A page asked for while the list turns back in opens once it has, and the last asked for wins.
    [AvaloniaFact]
    public void A_Page_Opened_In_The_Middle_Of_A_Turn_Opens_When_It_Ends()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        using var clock = new TestClock();
        var host = Host(shell);
        shell.ShowPage(new Border(), "first");
        Settle(clock);

        shell.ShowIndex();
        Draw();
        clock.At(clock.Now.TotalMilliseconds + 100);
        var second = new Border();
        var third = new Border();
        shell.ShowPage(second, "second");
        shell.ShowPage(third, "third");
        Settle(clock);

        Assert.Same(third, shell.Shown);
        Assert.Equal(2, host.StackDepth);
        Assert.Same(third.FindAncestorOfType<ContentPage>(), host.CurrentPage);
        Assert.Null(TopLevel.GetTopLevel(second));
        Assert.Equal(new Size(Scene.ReferenceWidth, Scene.ReferenceHeight), third.Bounds.Size);
    }

    // Open from a page, as Open(title) can be called: the new page takes the old one's place, so Back is one turn away.
    [AvaloniaFact]
    public void A_Page_Opened_From_Another_Takes_Its_Place()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);

        shell.ShowPage(new Border(), "first");
        shell.ShowPage(new Border(), "second");
        Settle(clock);
        Assert.Equal(2, host.StackDepth);

        shell.ShowPage(new Border(), "third");
        Settle(clock);
        Assert.Equal(2, host.StackDepth);

        shell.ShowIndex();
        Settle(clock);
        Assert.Equal(1, host.StackDepth);
        Assert.Same(Index(shell).FindAncestorOfType<ContentPage>(), host.CurrentPage);
    }

    // A page that refuses to be left (its Navigating cancelled) keeps the navigation page where it is: the shell stops
    // asking rather than asking for ever.
    [AvaloniaFact]
    public void A_Navigation_The_Page_Refuses_Is_Given_Up()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        var list = host.CurrentPage!;
        list.Navigating += e => { e.Cancel = true; return Task.CompletedTask; };

        shell.ShowPage(new Border(), "a page");
        Settle(clock);

        Assert.Same(list, host.CurrentPage);
        Assert.False(host.IsNavigating);
        Assert.False(shell.IsShowingPage);                       // the shell shows what the host does
        Assert.False(shell.FindControl<Button>("BackButton")!.IsVisible);
    }

    // The way back refused: the shell stays on the page it shows, and Back with it.
    [AvaloniaFact]
    public void A_Way_Back_The_Page_Refuses_Leaves_The_Page_Shown()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        var page = new Border();
        shell.ShowPage(page, "a page");
        Settle(clock);
        var holder = (Page)host.CurrentPage!;
        holder.Navigating += e => { e.Cancel = true; return Task.CompletedTask; };

        shell.ShowIndex();
        Settle(clock);

        Assert.Same(holder, host.CurrentPage);
        Assert.True(shell.IsShowingPage);
        Assert.Same(page, shell.Shown);
        Assert.True(shell.FindControl<Button>("BackButton")!.IsVisible);
#if DEBUG
        Assert.True(shell.FindControl<TextBlock>("PerformanceLine")!.IsVisible);   // its cost shown again
#endif
    }

    // A page asked for while the shell's own way back waits on the page being left (an async Navigating handler): the
    // list coming in is the shell's own doing, not the navigation page's, so the page asked for still opens after it.
    [AvaloniaFact]
    public void A_Page_Asked_For_While_The_Way_Back_Waits_On_The_Page_Opens_After_It()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        shell.ShowPage(new Border(), "first");
        Settle(clock);
        var leaving = new TaskCompletionSource();
        ((Page)host.CurrentPage!).Navigating += _ => leaving.Task;

        shell.ShowIndex();
        Dispatcher.UIThread.RunJobs();
        var next = new Border();
        shell.ShowPage(next, "next");
        leaving.SetResult();
        Settle(clock);

        Assert.Same(next.FindAncestorOfType<ContentPage>(), host.CurrentPage);
        Assert.True(shell.IsShowingPage);
        Assert.Same(next, shell.Shown);
    }

    // A shell taken out of its window mid-turn (an Android view made again, say): the turn ends out of the window, on the
    // clock's timer, and the shell navigates again once it is back.
    [AvaloniaFact]
    public void A_Shell_Out_Of_Its_Window_Mid_Turn_Navigates_Again_When_Back()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        shell.ShowPage(new Border(), "first");
        Draw();
        clock.At(100);
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        clock.At(1000);
        Dispatcher.UIThread.RunJobs();
        Assert.False(host.IsNavigating);

        scene.Window.Content = shell;
        var second = new Border();
        shell.ShowPage(second, "second");
        Settle(clock);
        Assert.Same(second.FindAncestorOfType<ContentPage>(), host.CurrentPage);
        Assert.Equal(2, host.StackDepth);
    }

    // With the focus in a page, Alt+Left is the navigation page's own: it pops to the list, and the shell follows.
    [AvaloniaFact]
    public void Alt_Left_Taken_By_The_Navigation_Page_Shows_The_List()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        var focusable = new Button { Content = "in the page" };
        shell.ShowPage(new StackPanel { Children = { focusable } }, "a page");
        Settle(clock);
        focusable.Focus();

        var key = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left, KeyModifiers = KeyModifiers.Alt };
        focusable.RaiseEvent(key);
        Dispatcher.UIThread.RunJobs();
        Assert.True(key.Handled);
        Assert.False(shell.IsShowingPage);                       // as the turn back begins, as drive.ps1 checks
        Assert.False(shell.FindControl<Button>("BackButton")!.IsVisible);
        Settle(clock);

        Assert.Equal(1, host.StackDepth);
        Assert.False(shell.IsShowingPage);
        Assert.False(shell.FindControl<Button>("BackButton")!.IsVisible);
    }

    // A page asked for while the navigation page turns back to the list itself waits for its turn, then opens.
    [AvaloniaFact]
    public void A_Page_Opened_While_The_Navigation_Page_Goes_Back_Itself_Opens_When_It_Ends()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var host = Host(shell);
        var focusable = new Button { Content = "in the page" };
        shell.ShowPage(new StackPanel { Children = { focusable } }, "a page");
        Settle(clock);
        focusable.Focus();
        focusable.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left, KeyModifiers = KeyModifiers.Alt });
        Dispatcher.UIThread.RunJobs();
        Assert.True(host.IsNavigating);

        var next = new Border();
        shell.ShowPage(next, "next");
        Settle(clock);

        Assert.Same(next.FindAncestorOfType<ContentPage>(), host.CurrentPage);
        Assert.Equal(2, host.StackDepth);
    }

    // A finger that presses a RepeatButton on a page and slides sideways off it and back keeps the press, as drive.ps1's
    // "repeatbutton" section holds: the navigation page's own swipe recogniser, whose back swipe the theme turns off
    // (IsGestureEnabled), mustn't take the finger. It did, so the repeats never came back (the real-window run of #147).
    [AvaloniaFact]
    public void A_Finger_Slid_Sideways_On_A_Page_Stays_The_Pages()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        using var clock = new TestClock();
        var button = new RepeatButton { Content = "more", Width = 200, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        shell.ShowPage(new StackPanel { Margin = new Avalonia.Thickness(19, 200, 0, 0), Children = { button } }, "a page");
        Settle(clock);
        var centre = button.TranslatePoint(new Avalonia.Point(100, button.Bounds.Height / 2), scene.Window)!.Value;

        var finger = scene.Window.TouchBegin(centre, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(button.IsPressed);
        for (var i = 1; i <= 10; i++)
            scene.Window.TouchMove(finger, centre + new Avalonia.Point(20 * i, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(button.IsPressed, "the slide sideways took the finger from the button");
        Assert.True(button.GetValue(Cornflower.Themes.Wp81.PhonePress.IsOffProperty));

        for (var i = 9; i >= 0; i--)
            scene.Window.TouchMove(finger, centre + new Avalonia.Point(20 * i, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(button.IsPressed);
        Assert.False(button.GetValue(Cornflower.Themes.Wp81.PhonePress.IsOffProperty));
        scene.Window.TouchEnd(finger, centre, RawInputModifiers.None);
    }

    // drive.ps1 finds the list's buttons by name through UI Automation, and counts every button for "no button without a
    // size": the list, behind a page, isn't there to find.
    [AvaloniaFact]
    public void The_List_Leaves_UI_Automation_While_A_Page_Is_Shown()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        using var clock = new TestClock();
        var titles = SampleShell.Pages.Select(page => page.Title).ToHashSet();
        Assert.Contains(Names(scene.Window), titles.Contains);

        shell.ShowPage(new Border(), "a page");
        Settle(clock);
        Assert.DoesNotContain(Names(scene.Window), titles.Contains);
        Assert.Contains("Back", Names(scene.Window));

        shell.ShowIndex();
        Settle(clock);
        Assert.Equal(titles.Count, Names(scene.Window).Count(titles.Contains));
    }

    public static TheoryData<string> Titles() => new(SampleShell.Pages.Select(page => page.Title));

    // Every sample page, opened from the shell as drive.ps1 and the device run do: in its ContentPage, at rest after the
    // turn, filling the view, its ready line naming it. Pivot's and Hub's pages have no SamplePageHeader (their title and
    // header stand for it); every other page's header shows its own title.
    [AvaloniaTheory]
    [MemberData(nameof(Titles))]
    public void Every_Sample_Page_Opens_At_Rest_Filling_The_View(string title)
    {
        var lines = new List<string>();
        var shell = new SampleShell { Report = lines.Add };
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        using var clock = new TestClock();

        shell.Open(title);
        Settle(clock);

        var page = Assert.IsAssignableFrom<Control>(shell.Shown);
        Assert.Equal(new Rect(0, 0, Scene.ReferenceWidth, Scene.ReferenceHeight), page.Bounds);
        Assert.True(page.IsEffectivelyVisible);
        Assert.False(IsTurned(page));
        Assert.EndsWith($" page=450x800 title={title}", Assert.Single(lines, line => line.StartsWith(SampleShell.ReadyMarker, StringComparison.Ordinal)));
        var header = page.GetVisualDescendants().OfType<SamplePageHeader>().FirstOrDefault();
        if (title is "pivot" or "hub")
            Assert.Null(header);
        else
            Assert.Equal(title, Assert.IsType<SamplePageHeader>(header).Title);
    }

    // #99's second half: the phone's page title doesn't wrap and is clipped at the screen's edge, so a long title is cut,
    // as "hyperlinkbutton" is on the Pixel 8's 411.43-wide view.
    [AvaloniaFact]
    public void A_Long_Title_Is_One_Line_Clipped_At_The_Phones_Edge()
    {
        var shell = new SampleShell { Report = _ => { } };
        shell.HasBackRail = false;
        using var scene = new Scene(shell, width: 411.43, height: 914.29, scaling: 2.625);
        using var clock = new TestClock();

        shell.Open("hyperlinkbutton");
        Settle(clock);

        var page = Assert.IsAssignableFrom<Control>(shell.Shown);
        var title = page.GetVisualDescendants().OfType<SamplePageHeader>().Single()
            .GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "PageTitle");
        Assert.Equal("hyperlinkbutton", title.Text);
        Assert.Single(title.TextLayout.TextLines);
        Assert.True(title.TextLayout.WidthIncludingTrailingWhitespace > title.Bounds.Width,
            $"the title, {title.TextLayout.WidthIncludingTrailingWhitespace} wide, fits in {title.Bounds.Width}");
        Assert.True(title.ClipToBounds);
        Assert.True(title.TranslatePoint(new Point(title.Bounds.Width, 0), page)!.Value.X <= page.Bounds.Width + 0.01);
    }

    // The page page is a NavigationPage of its own inside the shell's: the platform's back request goes back its pages
    // first, then the shell's to the list, and its first page keeps its compact app bar's height free, as on its own.
    [AvaloniaFact]
    public void The_Page_Pages_Own_Pages_Go_Back_First_Then_The_Shell_Shows_The_List()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell, width: Scene.ReferenceWidth + RailWidth);
        using var clock = new TestClock();
        shell.Open("page");
        Settle(clock);
        var pages = Assert.IsType<Cornflower.Sample.Pages.PagePage>(shell.Shown);
        var inner = pages.FindControl<NavigationPage>("Navigation")!;
        var first = (ContentPage)inner.CurrentPage!;
        var content = (Control)first.Content!;
        Assert.Equal(737.08, content.TranslatePoint(new Point(0, content.Bounds.Height), inner)!.Value.Y + content.Margin.Bottom, 0.5);

        _ = pages.GoForward(null);
        Settle(clock);
        Assert.Equal(2, pages.Depth);

        Assert.True(Back(scene.Window));
        Settle(clock);
        Assert.Equal(1, pages.Depth);
        Assert.True(shell.IsShowingPage);

        Assert.True(Back(scene.Window));
        Settle(clock);
        Assert.False(shell.IsShowingPage);
        Assert.Equal(1, Host(shell).StackDepth);
    }

    /// <summary>What Avalonia does for the platform's back: raises the request, and reads <c>Handled</c>.</summary>
    private static bool Back(Interactive from)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        from.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e.Handled;
    }

    private static NavigationPage Host(SampleShell shell) => shell.FindControl<NavigationPage>("Host")!;

    private static ScrollViewer Index(SampleShell shell) => shell.FindControl<ScrollViewer>("Index")!;

    /// <summary>Whether a turn is drawn on the visual or a visual above it.</summary>
    private static bool IsTurned(Visual visual) =>
        visual.GetSelfAndVisualAncestors().Any(v => v.RenderTransform is not null);

    /// <summary>The names UI Automation gives every element in the window.</summary>
    private static List<string> Names(Control root)
    {
        var names = new List<string>();
        Walk(ControlAutomationPeer.CreatePeerForElement(root));
        return names;

        void Walk(AutomationPeer peer)
        {
            if (peer.GetName() is { Length: > 0 } name)
                names.Add(name);
            foreach (var child in peer.GetChildren())
                Walk(child);
        }
    }

}
