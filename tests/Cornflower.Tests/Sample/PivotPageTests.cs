using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's Pivot page: in the catalogue by #121, the Pivot the theme's and full width, the selection shown, and
/// the switches that lock it and make the page light.
/// </summary>
public class PivotPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Page_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(PivotPage));
        Assert.Equal("pivot", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(PagePage)));
    }

    [AvaloniaFact]
    public void The_Pivot_Is_The_Themes_And_Full_Width()
    {
        var page = new PivotPage();
        using var scene = new Scene(page);
        var pivot = page.FindControl<Pivot>("Pivot")!;
        Assert.NotNull(pivot.HeaderPanel);
        Assert.Equal(4, pivot.HeaderPanel!.Children.Count);
        Assert.Equal(0, pivot.TranslatePoint(default, page)!.Value.X);
        Assert.Equal(Scene.ReferenceWidth, pivot.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_Header_Tap_Shows_The_Selection()
    {
        using var clock = new TestClock();
        var page = new PivotPage();
        using var scene = new Scene(page);
        var pivot = page.FindControl<Pivot>("Pivot")!;
        var header = pivot.HeaderPanel!.Children[2];
        var at = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        clock.At(2000);
        Assert.Equal(2, pivot.SelectedIndex);
        Assert.Equal("three, the third of four", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Switches_Lock_The_Pivot_And_Make_The_Page_Light()
    {
        var page = new PivotPage();
        using var scene = new Scene(page);
        var pivot = page.FindControl<Pivot>("Pivot")!;
        page.FindControl<ToggleSwitch>("Locked")!.IsChecked = true;
        Assert.True(pivot.IsLocked);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Assert.Equal(ThemeVariant.Light, page.FindControl<ThemeVariantScope>("Scope")!.ActualThemeVariant);
        page.FindControl<ToggleSwitch>("Locked")!.IsChecked = false;
        Assert.False(pivot.IsLocked);
    }
}
