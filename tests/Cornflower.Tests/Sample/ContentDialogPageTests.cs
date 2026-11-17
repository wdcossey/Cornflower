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
/// The sample's ContentDialog page: in the catalogue by #157, each dialog the theme's, placed and closed the phone's way,
/// what it returned shown, the MessageDialog text styles, and the light theme's switch reaching the dialog.
/// </summary>
public class ContentDialogPageTests
{
    private static void Click(Window window, Control control)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static ContentDialog Open(TestClock clock, Scene scene, ContentDialogPage page, string button)
    {
        Click(scene.Window, page.FindControl<Button>(button)!);
        clock.At(clock.Now.TotalMilliseconds + 500);
        Dispatcher.UIThread.RunJobs();
        return page.Shown!;
    }

    private static string State(ContentDialogPage page) => page.FindControl<TextBlock>("State")!.Text!;

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Flyout_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ContentDialogPage));
        Assert.Equal("contentdialog", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(FlyoutPage)));
    }

    [AvaloniaFact]
    public void The_Two_Button_Dialog_Opens_At_The_Top_And_Its_Ok_Returns_Primary()
    {
        using var clock = new TestClock();
        var page = new ContentDialogPage();
        using var scene = new Scene(page);

        var dialog = Open(clock, scene, page, "ShowTwo");
        Assert.Equal("two buttons opened", State(page));
        Assert.Equal(new Point(0, 0), dialog.TranslatePoint(default, scene.Window));
        Assert.Equal(ContentDialogReadings.Height, dialog.Bounds.Height, ContentDialogReadings.RawPixel + 0.001);

        Click(scene.Window, dialog.GetVisualDescendants().OfType<Button>().First());
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("two buttons: primary", State(page));
        clock.At(clock.Now.TotalMilliseconds + 500);
        Assert.False(dialog.Popup!.IsOpen);
    }

    [AvaloniaTheory]
    [InlineData("ShowOne", false)]
    [InlineData("ShowNone", false)]
    [InlineData("ShowFull", true)]
    [InlineData("ShowLong", true)]
    [InlineData("ShowTextBox", false)]
    public void Each_Dialog_Opens_And_Back_Closes_It_With_None(string button, bool fills)
    {
        using var clock = new TestClock();
        var page = new ContentDialogPage();
        using var scene = new Scene(page);

        var dialog = Open(clock, scene, page, button);
        Assert.Equal(fills, dialog.Bounds.Height == 800);
        scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.EndsWith(": none", State(page));
        clock.At(clock.Now.TotalMilliseconds + 500);
        Assert.False(dialog.Popup!.IsOpen);
    }

    [AvaloniaFact]
    public void The_MessageDialog_Styles_Are_On_The_Page()
    {
        var page = new ContentDialogPage();
        using var scene = new Scene(page);

        Assert.Same(page.FindResource("MessageDialogTitleStyle"), page.FindControl<TextBlock>("MessageTitle")!.Theme);
        Assert.Same(page.FindResource("MessageDialogContentStyle"), page.FindControl<TextBlock>("MessageContent")!.Theme);
    }

    [AvaloniaFact]
    public void The_Light_Switch_Reaches_The_Dialog_And_Goes_With_The_Page()
    {
        using var clock = new TestClock();
        var page = new ContentDialogPage();
        using var scene = new Scene(page);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        var dialog = Open(clock, scene, page, "ShowTwo");
        using (var frame = scene.Capture())
            Assert.Equal(ContentDialogReadings.Chrome(dark: false), Frames.PixelAt(frame, 1070, 5));
        Assert.Equal(ContentDialogReadings.Chrome(dark: false), ((ISolidColorBrush)dialog.Background!).Color);

        dialog.Hide();
        clock.At(clock.Now.TotalMilliseconds + 500);
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Avalonia.Styling.ThemeVariant.Dark, scene.Window.RequestedThemeVariant);
    }
}
