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
/// The sample's list picker page: in the catalogue by #153, its pickers the theme's, filling the window, what they gave
/// back shown, and the light theme's switch reaching them.
/// </summary>
public class ListPickerFlyoutPageTests
{
    private static void Click(Window window, Control control)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static void At(TestClock clock, double ms)
    {
        clock.At(ms);
        Dispatcher.UIThread.RunJobs();
    }

    private static T Part<T>(PickerFlyoutBase picker, string name) where T : Control =>
        ((Control)picker.Popup.Child!).GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Flyout_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ListPickerFlyoutPage));
        Assert.Equal("listpicker", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(FlyoutPage)));
    }

    [AvaloniaFact]
    public void The_List_Picker_Fills_The_Window_And_A_Tap_Picks_And_Shows_The_Pick()
    {
        using var clock = new TestClock();
        var page = new ListPickerFlyoutPage();
        using var scene = new Scene(page);

        Click(scene.Window, page.FindControl<Button>("OpenSingle")!);
        At(clock, 500);
        var presenter = (Control)page.Single.Popup.Child!;
        Assert.Equal(new Rect(0, 0, 450, 800), new Rect(presenter.TranslatePoint(default, scene.Window)!.Value, presenter.Bounds.Size));
        Assert.Equal("COLOUR", Part<TextBlock>(page.Single, "TitlePresenter").Text);

        var green = Part<ListBox>(page.Single, "PART_ItemsHost").GetVisualDescendants().OfType<ListBoxItem>().ElementAt(3);
        Click(scene.Window, green);
        At(clock, 1500);
        Assert.False(page.Single.Popup.IsOpen);
        Assert.Equal("list picker: green", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Multiple_Picker_Has_The_Bar_And_Back_Picks_Nothing()
    {
        using var clock = new TestClock();
        var page = new ListPickerFlyoutPage();
        using var scene = new Scene(page);

        Click(scene.Window, page.FindControl<Button>("OpenMultiple")!);
        At(clock, 500);
        Assert.True(Part<CommandBar>(page.Multiple, "PickerBar").IsVisible);

        scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        At(clock, 1500);
        Assert.False(page.Multiple.Popup.IsOpen);
        Assert.Equal("multiple: nothing picked", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Picker_Flyouts_Accept_Shows_Accepted()
    {
        using var clock = new TestClock();
        var page = new ListPickerFlyoutPage();
        using var scene = new Scene(page);

        Click(scene.Window, page.FindControl<Button>("OpenPicker")!);
        At(clock, 500);
        Click(scene.Window, Part<CommandBarButton>(page.Picker, "PART_AcceptButton"));
        At(clock, 1500);
        Assert.Equal("picker accepted", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Light_Switch_Reaches_The_Pickers()
    {
        using var clock = new TestClock();
        var page = new ListPickerFlyoutPage();
        using var scene = new Scene(page);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Click(scene.Window, page.FindControl<Button>("OpenPicker")!);
        At(clock, 500);
        using var frame = scene.Capture();
        Assert.Equal(ListPickerFlyoutReadings.Chrome(dark: false), Frames.PixelAt(frame, 1000, 5));
    }
}
