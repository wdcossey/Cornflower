using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 DatePicker and TimePicker: Avalonia's in the Wp81 theme, today's date and half past twelve, a 24-hour picker in
/// quarter hours, what they hold, and a switch for the light theme.
/// </summary>
[SamplePage("datetimepicker", Order = 156)]
public partial class DateTimePickerPage : UserControl
{
    public DateTimePickerPage()
    {
        InitializeComponent();
        Date.SelectedDate = System.DateTimeOffset.Now.Date;
        Time.SelectedTime = new System.TimeSpan(12, 30, 0);
        Time24.SelectedTime = new System.TimeSpan(18, 15, 0);
        ShowState();
    }

    private void OnDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e) => ShowState();

    private void OnTimeChanged(object? sender, TimePickerSelectedValueChangedEventArgs e) => ShowState();

    private void ShowState()
    {
        if (State is not null && Date is not null && Time is not null && Time24 is not null)
            State.Text = $@"{Date.SelectedDate:yyyy-MM-dd}, {Time.SelectedTime:hh\:mm}, {Time24.SelectedTime:hh\:mm}";
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
