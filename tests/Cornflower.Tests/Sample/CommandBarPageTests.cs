using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's CommandBar page: in the catalogue by #118, the bar and its commands the theme's, at the page's foot with
/// its closed height kept free, the last command shown, and the switches for the minimal bar and the light theme.
/// </summary>
public class CommandBarPageTests
{
    private static void Tap(Window window, Point at)
    {
        window.MouseDown(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static Point CentreOf(Visual control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ScrollViewer_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(CommandBarPage));
        Assert.Equal("commandbar", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ScrollViewerPage)));
    }

    [AvaloniaFact]
    public void The_Bar_And_Its_Commands_Are_The_Themes_At_The_Pages_Foot()
    {
        var page = new CommandBarPage();
        using var scene = new Scene(page);

        var bar = page.FindControl<CommandBar>("Bar")!;
        Assert.Contains(bar.GetVisualDescendants(), v => v is Grid { Name: "LayoutRoot" });
        var circles = bar.GetVisualDescendants().OfType<Ellipse>().Where(e => e.Name == "PART_Ellipse" && e.IsEffectivelyVisible).ToList();
        Assert.Equal(3, circles.Count);
        Assert.Equal(800 - 62.9166, bar.TranslatePoint(default, page)!.Value.Y, 0.001);
        Assert.Equal(62.9166, page.FindControl<ScrollViewer>("Body")!.Padding.Bottom, 0.0001);
        Assert.Equal("commandbar", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void A_Command_Shows_Its_Label_And_The_Ellipsis_Opens_The_Bar()
    {
        var page = new CommandBarPage();
        using var scene = new Scene(page);
        var bar = page.FindControl<CommandBar>("Bar")!;

        Tap(scene.Window, CentreOf(page.FindControl<CommandBarButton>("Add")!, scene.Window));
        Assert.Equal("add", page.FindControl<TextBlock>("State")!.Text);

        Tap(scene.Window, CentreOf(bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_OverflowButton"), scene.Window));
        Assert.True(bar.IsOpen);
        scene.Window.UpdateLayout();
        Tap(scene.Window, CentreOf(page.FindControl<CommandBarButton>("Second")!, scene.Window));
        Assert.Equal("second menu item", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Switches_Make_The_Bar_Minimal_And_The_Page_Light()
    {
        var page = new CommandBarPage();
        using var scene = new Scene(page);
        var bar = page.FindControl<CommandBar>("Bar")!;

        page.FindControl<ToggleSwitch>("Minimal")!.IsChecked = true;
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        scene.Window.UpdateLayout();

        Assert.Equal(AppBarClosedDisplayMode.Minimal, CommandBarOptions.GetClosedDisplayMode(bar));
        Assert.Equal(26.25, bar.Bounds.Height, 0.001);
        Assert.Equal(26.25, page.FindControl<ScrollViewer>("Body")!.Padding.Bottom, 0.0001);
        Assert.Equal(ThemeVariant.Light, bar.ActualThemeVariant);
    }
}
