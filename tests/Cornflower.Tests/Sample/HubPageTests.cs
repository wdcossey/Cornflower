using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's Hub page: in the catalogue by #122, the Hub the theme's and full width, which sections are in view and
/// the last header clicked shown, and the switches for the background image and the light theme.
/// </summary>
public class HubPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Pivot_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(HubPage));
        Assert.Equal("hub", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(PivotPage)));
    }

    [AvaloniaFact]
    public void The_Hub_Is_The_Themes_And_Full_Width_With_What_Is_In_View_Shown()
    {
        var page = new HubPage();
        using var scene = new Scene(page);
        var hub = page.FindControl<Hub>("Hub")!;
        Assert.Equal(4, hub.Sections.Count);
        Assert.Equal(0, hub.TranslatePoint(default, page)!.Value.X);
        Assert.Equal(Scene.ReferenceWidth, hub.Bounds.Width);
        Assert.Equal(405, hub.Sections[0].Bounds.Width, 0.01);
        Assert.Equal("in view: about, lines", page.State.InView);
        Assert.Contains(hub.Sections[0].GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "in view: about, lines");
    }

    [AvaloniaFact]
    public void The_Interactive_Header_Shows_Its_Click()
    {
        var page = new HubPage();
        using var scene = new Scene(page);
        var hub = page.FindControl<Hub>("Hub")!;
        hub.ScrollToSection(hub.Sections[2]);
        scene.Window.UpdateLayout();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();                   // Loaded: the entrance begins
        hub.Entrance.Finish();                                              // at rest, after its entrance
        _ = scene.Capture();
        var header = hub.Sections[2].GetTemplateDescendants().OfType<Control>().Single(c => c.Name == "PART_Header");
        var at = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        Assert.Equal("clicked: see all", page.State.LastClick);
    }

    [AvaloniaFact]
    public void The_Switches_Set_The_Background_Image_And_The_Light_Theme()
    {
        var page = new HubPage();
        using var scene = new Scene(page);
        var hub = page.FindControl<Hub>("Hub")!;
        var switches = hub.Sections[0].GetVisualDescendants().OfType<ToggleSwitch>().ToList();
        switches[0].IsChecked = true;
        var brush = Assert.IsType<ImageBrush>(hub.Background);
        Assert.Equal(Stretch.Fill, brush.Stretch);                           // WinRT's default, which Avalonia's isn't
        scene.Window.UpdateLayout();
        var background = hub.GetTemplateDescendants().OfType<Control>().Single(c => c.Name == "PART_Background");
        Assert.Equal(1600, background.Bounds.Width);
        switches[1].IsChecked = true;
        Assert.Equal(ThemeVariant.Light, page.FindControl<ThemeVariantScope>("Scope")!.ActualThemeVariant);
        switches[0].IsChecked = false;
        Assert.Null(hub.Background);
    }
}
