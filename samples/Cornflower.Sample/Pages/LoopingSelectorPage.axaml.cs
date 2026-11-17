using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 LoopingSelector, the pickers' wheel: Avalonia's DateTimePickerPanel in the Wp81 theme, three wheels at today's
/// date as the DatePickerFlyout lays them out, what they show, and a switch for the light theme.
/// </summary>
[SamplePage("loopingselector", Order = 154)]
public partial class LoopingSelectorPage : UserControl
{
    public LoopingSelectorPage()
    {
        InitializeComponent();
        var today = System.DateTime.Today;

        // A bare panel names its days in this month (its presenter would set the month), so it has this month's days.
        Day.MaximumValue = System.DateTime.DaysInMonth(today.Year, today.Month);
        // A hundred years round this one, as Avalonia's DatePicker has.
        Year.MaximumValue = today.Year + 50;
        Year.MinimumValue = today.Year - 50;
        Month.SelectedValue = today.Month;
        Day.SelectedValue = today.Day;
        Year.SelectedValue = today.Year;
        ShowState();
    }

    private void OnSelectionChanged(object? sender, System.EventArgs e) => ShowState();

    private void ShowState()
    {
        if (State is not null)
            State.Text = $"{Day.SelectedValue} / {Month.SelectedValue} / {Year.SelectedValue}";
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
