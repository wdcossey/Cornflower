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
    // The closed bar's height, compact and minimal, which the page keeps free at its foot (docs/controls/CommandBar).
    private const double CompactHeight = 62.9166;
    private const double MinimalHeight = 26.25;

    public CommandBarPage()
    {
        InitializeComponent();
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
        Body.Padding = new Thickness(0, 0, 0, minimal ? MinimalHeight : CompactHeight);
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
