using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's LoopingSelector page: in the catalogue by #154, three wheels in the theme at the DatePickerFlyout's
/// places and today's date, what they show, and the switch that makes the page light.
/// </summary>
public class LoopingSelectorPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Flyout_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(LoopingSelectorPage));
        Assert.Equal("loopingselector", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(FlyoutPage)));
    }

    [AvaloniaFact]
    public void The_Wheels_Are_The_Themes_At_The_Flyouts_Places_And_Today()
    {
        var page = new LoopingSelectorPage();
        using var scene = new Scene(page);
        var today = DateTime.Today;
        var wheels = new[] { "Month", "Day", "Year" }.Select(name => page.FindControl<DateTimePickerPanel>(name)!).ToArray();
        Assert.Equal([today.Month, today.Day, today.Year], wheels.Select(w => w.SelectedValue));
        for (var i = 0; i < 3; i++)
        {
            Assert.Same(page.FindResource("Wp81LoopingSelectorTheme"), wheels[i].Theme);
            Assert.Equal(138.375, wheels[i].ItemHeight);
            Assert.Equal(17.5 + i * 138.333, wheels[i].TranslatePoint(default, page)!.Value.X, 2);
            Assert.Equal(138.333, wheels[i].Bounds.Width, 2);
        }

        Assert.Equal($"{today.Day} / {today.Month} / {today.Year}", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void A_Drag_Turns_A_Wheel_And_The_Page_Shows_It()
    {
        using var clock = new TestClock();
        var page = new LoopingSelectorPage();
        using var scene = new Scene(page);
        var year = page.FindControl<DateTimePickerPanel>("Year")!;
        var at = year.TranslatePoint(new Point(69, year.Bounds.Height / 2), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        for (var i = 1; i <= 40; i++)
        {
            clock.At(i * 10);
            scene.Window.TouchMove(touch, at - new Point(0, i * 4), RawInputModifiers.None);
        }

        clock.At(800);
        scene.Window.TouchEnd(touch, at - new Point(0, 160), RawInputModifiers.None);
        clock.At(2000);
        Assert.Equal(DateTime.Today.Year + 1, year.SelectedValue);
        Assert.EndsWith($"/ {DateTime.Today.Year + 1}", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Switch_Makes_The_Page_Light()
    {
        var page = new LoopingSelectorPage();
        using var scene = new Scene(page);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Assert.Equal(ThemeVariant.Light, page.FindControl<ThemeVariantScope>("Scope")!.ActualThemeVariant);
    }
}
