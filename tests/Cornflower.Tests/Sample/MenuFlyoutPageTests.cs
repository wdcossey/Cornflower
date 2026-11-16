using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Tests.Controls;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's MenuFlyout page: in the catalogue by #152, a Button's menu the theme's, placed below or above its Button
/// and closed the phone's way, what happened last shown, and the light theme's switch reaching the menu.
/// </summary>
public class MenuFlyoutPageTests
{
    private static void Tap(Window window, Visual visual)
    {
        var at = visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static MenuFlyoutPresenter Presenter(MenuFlyout flyout) => (MenuFlyoutPresenter)flyout.Popup.Child!;

    private static Rect InWindow(Control control, Window window) =>
        new(control.TranslatePoint(default, window)!.Value, control.Bounds.Size);

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Flyout_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(MenuFlyoutPage));
        Assert.Equal("menu flyout", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(FlyoutPage)));
    }

    [AvaloniaFact]
    public void The_Menu_Opens_Below_Its_Button_And_An_Item_Closes_It()
    {
        using var clock = new TestClock();
        var page = new MenuFlyoutPage();
        using var scene = new Scene(page);
        var button = page.FindControl<Button>("OpenMenu")!;
        var flyout = (MenuFlyout)button.Flyout!;

        Tap(scene.Window, button);
        clock.At(600);
        Dispatcher.UIThread.RunJobs();
        Assert.True(flyout.Popup.IsOpen);
        Assert.Equal("menu opened", page.FindControl<TextBlock>("State")!.Text);
        var menu = InWindow(Presenter(flyout), scene.Window);
        Assert.Equal(InWindow(button, scene.Window).Bottom, menu.Top, 3);
        Assert.Equal(450, menu.Width);

        Tap(scene.Window, page.FindControl<MenuItem>("Toggle")!);
        Assert.False(flyout.Popup.IsOpen);
        Assert.Equal("toggle item: on, closed", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Low_Menu_Opens_Above_Its_Button_And_Back_Closes_It()
    {
        using var clock = new TestClock();
        var page = new MenuFlyoutPage();
        using var scene = new Scene(page);
        var button = page.FindControl<Button>("OpenLow")!;
        var flyout = (MenuFlyout)button.Flyout!;

        Tap(scene.Window, button);
        clock.At(600);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(InWindow(button, scene.Window).Top, InWindow(Presenter(flyout), scene.Window).Bottom, 3);

        scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        Assert.False(flyout.Popup.IsOpen);
        Assert.Equal("menu opened, closed", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Light_Switch_Reaches_The_Menu()
    {
        using var clock = new TestClock();
        var page = new MenuFlyoutPage();
        using var scene = new Scene(page);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        var flyout = (MenuFlyout)page.FindControl<Button>("OpenMenu")!.Flyout!;

        Tap(scene.Window, page.FindControl<Button>("OpenMenu")!);
        clock.At(600);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(MenuFlyoutReadings.Background(dark: false), ((ISolidColorBrush)Presenter(flyout).Background!).Color);
        Assert.Equal(MenuFlyoutReadings.Line(dark: false), ((ISolidColorBrush)Presenter(flyout).BorderBrush!).Color);
    }
}
