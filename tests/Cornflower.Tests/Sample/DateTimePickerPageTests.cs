using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's date and time picker page: in the catalogue by #156, the pickers headed in the theme, a pick shown on the
/// page, and the switch that makes the page light.
/// </summary>
public class DateTimePickerPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_LoopingSelector_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(DateTimePickerPage));
        Assert.Equal("datetimepicker", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(LoopingSelectorPage)));
    }

    [AvaloniaFact]
    public void The_Pickers_Are_Headed_And_A_Pick_Shows_On_The_Page()
    {
        using var clock = new TestClock();
        var page = new DateTimePickerPage();
        using var scene = new Scene(page);
        var date = page.FindControl<DatePicker>("Date")!;
        var time24 = page.FindControl<TimePicker>("Time24")!;
        Assert.Equal("date picker", Cornflower.Controls.ControlHeader.GetHeader(date));
        Assert.Equal(15, time24.MinuteIncrement);
        Assert.Equal(DateTimeOffset.Now.Date, date.SelectedDate!.Value.Date);

        var button = date.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton");
        var at = button.TranslatePoint(new Avalonia.Point(20, button.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        clock.At(500);
        var presenter = date.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>()
            .First(p => p.Name == "PickerPopup").Child!;
        presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().First(p => p.Name == "PART_YearSelector").SelectedValue =
            DateTime.Today.Year + 1;
        var accept = presenter.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_AcceptButton");
        accept.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.StartsWith($"{DateTime.Today.Year + 1}-", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Switch_Makes_The_Page_Light()
    {
        var page = new DateTimePickerPage();
        using var scene = new Scene(page);
        page.FindControl<ToggleSwitch>("Light")!.IsChecked = true;
        Assert.Equal(ThemeVariant.Light, page.FindControl<ThemeVariantScope>("Scope")!.ActualThemeVariant);
    }
}
