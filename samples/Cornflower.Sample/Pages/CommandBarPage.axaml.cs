using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 page's app bar: a CommandBar at the page's foot with three commands and two secondary ones, the last command
/// shown, and switches for the minimal bar and the light theme.
/// </summary>
[SamplePage("commandbar", Order = 118)]
public partial class CommandBarPage : UserControl
{
    public CommandBarPage()
    {
        InitializeComponent();
        KeepTheBarsHeightFree();
        Bar.PropertyChanged += (_, e) =>
        {
            if (e.Property == CommandBarOptions.ClosedDisplayModeProperty)
                KeepTheBarsHeightFree();
        };
    }

    /// <summary>The closed bar's height, compact or minimal, kept free at the content's foot, from the theme's keys.</summary>
    private void KeepTheBarsHeightFree()
    {
        var minimal = CommandBarOptions.GetClosedDisplayMode(Bar) == AppBarClosedDisplayMode.Minimal;
        Body.Bind(ScrollViewer.PaddingProperty, Body.GetResourceObservable(
            minimal ? "Wp81CommandBarMinimalHeight" : "Wp81CommandBarCompactHeight",
            height => new Thickness(0, 0, 0, height is double h ? h : 0)));
    }

    private void OnCommand(object? sender, RoutedEventArgs e)
    {
        if (sender is CommandBarButton button)
            State.Text = button.Label;
        else if (sender is CommandBarToggleButton toggle)
            State.Text = $"{toggle.Label}: {(toggle.IsChecked == true ? "on" : "off")}";
    }

    private void OnMinimalChanged(object? sender, RoutedEventArgs e)
    {
        var minimal = Minimal.IsChecked == true;
        CommandBarOptions.SetClosedDisplayMode(Bar, minimal ? AppBarClosedDisplayMode.Minimal : AppBarClosedDisplayMode.Compact);
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
