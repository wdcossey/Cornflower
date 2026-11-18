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
/// page, the flyouts shown from its buttons (#184), and the switch that makes the page light.
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
        var year = Assert.NotNull(date.SelectedDate).Year;

        var button = date.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton");
        var at = button.TranslatePoint(new Avalonia.Point(20, button.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        clock.At(500);
        var presenter = date.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>()
            .First(p => p.Name == "PickerPopup").Child!;
        presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().First(p => p.Name == "PART_YearSelector").SelectedValue =
            year + 1;
        var accept = presenter.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_AcceptButton");
        accept.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.StartsWith($"{year + 1}-", page.FindControl<TextBlock>("State")!.Text);
    }

    [AvaloniaFact]
    public void The_Buttons_Show_The_Flyouts_And_A_Pick_Shows_On_The_Page()
    {
        using var clock = new TestClock();
        var page = new DateTimePickerPage();
        using var scene = new Scene(page);
        var show = page.FindControl<Button>("ShowTime")!;
        show.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        clock.At(500);
        var presenter = scene.Window.GetVisualDescendants().OfType<Cornflower.Controls.TimePickerFlyoutPresenter>().Single();
        presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().First(p => p.Name == "PART_MinuteSelector").SelectedValue = 45;
        var accept = presenter.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_AcceptButton");
        accept.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.EndsWith(", 07:45", page.FindControl<TextBlock>("State")!.Text);
        clock.At(1500);

        page.FindControl<Button>("ShowDate")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        clock.At(2000);
        var dates = scene.Window.GetVisualDescendants().OfType<Cornflower.Controls.DatePickerFlyoutPresenter>().Single();
        var year = dates.GetVisualDescendants().OfType<DateTimePickerPanel>().First(p => p.Name == "PART_YearSelector");
        var next = year.SelectedValue + 1;
        year.SelectedValue = next;
        dates.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_AcceptButton")
            .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Contains($"flyouts {next}-", page.FindControl<TextBlock>("State")!.Text);
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
