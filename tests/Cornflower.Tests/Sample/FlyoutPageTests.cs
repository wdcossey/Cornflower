using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Tests.Controls;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's Flyout page: in the catalogue by #151, a Button's top flyout and full flyout the theme's, placed and
/// closed the phone's way, what happened last shown, and the light theme's switch reaching the flyout.
/// </summary>
public class FlyoutPageTests
{
    private static void Click(Window window, Button button)
    {
        var at = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static FlyoutPresenter Presenter(Flyout flyout) => (FlyoutPresenter)flyout.Popup.Child!;

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Hub_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(FlyoutPage));
        Assert.Equal("flyout", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(HubPage)));
    }

    [AvaloniaFact]
    public void The_Top_Flyout_Opens_At_The_Top_And_Its_Close_Button_Closes_It()
    {
        using var clock = new TestClock();
        var page = new FlyoutPage();
        using var scene = new Scene(page);
        var flyout = (Flyout)page.FindControl<Button>("OpenTop")!.Flyout!;

        Click(scene.Window, page.FindControl<Button>("OpenTop")!);
        clock.At(500);
        Dispatcher.UIThread.RunJobs();
        Assert.True(flyout.Popup.IsOpen);
        Assert.Equal("top opened", page.FindControl<TextBlock>("State")!.Text);
        Assert.Equal(new Point(0, 0), Presenter(flyout).TranslatePoint(default, scene.Window));
        Assert.Equal(450, Presenter(flyout).Bounds.Width);
        Assert.True(Presenter(flyout).Bounds.Height < 200);

        var close = Presenter(flyout).GetVisualDescendants().OfType<Button>().Single(b => b.Name == "CloseTop");
        Click(scene.Window, close);
        clock.At(700);
        Dispatcher.UIThread.RunJobs();
        Assert.False(flyout.Popup.IsOpen);
        Assert.Equal("top closed", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Full_Flyout_Fills_The_Window_And_Back_Closes_It()
    {
        using var clock = new TestClock();
        var page = new FlyoutPage();
        using var scene = new Scene(page);
        var flyout = (Flyout)page.FindControl<Button>("OpenFull")!.Flyout!;

        Click(scene.Window, page.FindControl<Button>("OpenFull")!);
        clock.At(500);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Size(450, 800), Presenter(flyout).Bounds.Size);

        scene.Window.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(TopLevel.BackRequestedEvent));
        clock.At(1000);
        Dispatcher.UIThread.RunJobs();
        Assert.False(flyout.Popup.IsOpen);
        Assert.Equal("full closed", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Light_Switch_Reaches_The_Flyout()
    {
        using var clock = new TestClock();
        var page = new FlyoutPage();
        using var scene = new Scene(page);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Click(scene.Window, page.FindControl<Button>("OpenTop")!);
        clock.At(500);
        Dispatcher.UIThread.RunJobs();
        using var frame = scene.Capture();
        Assert.Equal(FlyoutReadings.Chrome(dark: false), Frames.PixelAt(frame, 1000, 5));
        Assert.Equal(FlyoutReadings.Chrome(dark: false), ((ISolidColorBrush)Presenter((Flyout)page.FindControl<Button>("OpenTop")!.Flyout!).Background!).Color);
    }
}
