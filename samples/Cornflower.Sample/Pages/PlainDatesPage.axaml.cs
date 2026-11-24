using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>The seventh plain themes (#240): Calendar, CalendarItem, CalendarDayButton, CalendarButton and CalendarDatePicker, and the light theme.</summary>
[SamplePage("plain dates", Order = 240)]
public partial class PlainDatesPage : UserControl
{
    public PlainDatesPage()
    {
        InitializeComponent();

        // A day this month selected, today outlined beside it; the pickers hold today's date.
        var today = System.DateTime.Today;
        var day = today.Day > 20 ? today.AddDays(-3) : today.AddDays(3);
        Month.SelectedDate = day;
        Light.SelectedDate = day;
        Picker.SelectedDate = today;
        LightPicker.SelectedDate = today;
    }
}
