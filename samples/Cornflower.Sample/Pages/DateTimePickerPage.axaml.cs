using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 DatePicker and TimePicker: Avalonia's in the Wp81 theme, today's date and half past twelve, a 24-hour picker in
/// quarter hours, a DatePickerFlyout and a TimePickerFlyout shown from buttons, what they hold, and a switch for the light
/// theme.
/// </summary>
[SamplePage("datetimepicker", Order = 156)]
public partial class DateTimePickerPage : UserControl
{
    /// <summary>The flyouts, kept so each opens at what it last picked.</summary>
    private readonly DatePickerFlyout _dateFlyout = new();

    private readonly TimePickerFlyout _timeFlyout = new() { Time = new System.TimeSpan(7, 0, 0) };

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

    private async void OnShowDate(object? sender, RoutedEventArgs e)
    {
        await _dateFlyout.ShowAtAsync(ShowDate);
        ShowState();
    }

    private async void OnShowTime(object? sender, RoutedEventArgs e)
    {
        await _timeFlyout.ShowAtAsync(ShowTime);
        ShowState();
    }

    private void ShowState()
    {
        if (State is not null && Date is not null && Time is not null && Time24 is not null)
            State.Text = $@"{Date.SelectedDate:yyyy-MM-dd}, {Time.SelectedTime:hh\:mm}, {Time24.SelectedTime:hh\:mm}; "
                + $@"flyouts {_dateFlyout.Date:yyyy-MM-dd}, {_timeFlyout.Time:hh\:mm}";
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
