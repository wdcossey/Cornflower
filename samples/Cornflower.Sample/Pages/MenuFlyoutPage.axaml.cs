using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 MenuFlyout: a Button's menu below it and one above, with a toggle item, a separator and a disabled item, what
/// happened last shown, and a switch for the light theme.
/// </summary>
[SamplePage("menu flyout", Order = 152)]
public partial class MenuFlyoutPage : UserControl
{
    public MenuFlyoutPage()
    {
        InitializeComponent();
    }

    private void OnOpened(object? sender, System.EventArgs e) => State.Text = "menu opened";

    private void OnClosed(object? sender, System.EventArgs e) => State.Text += ", closed";

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender!;
        State.Text = item.ToggleType == MenuItemToggleType.CheckBox
            ? $"{item.Header}: {(item.IsChecked ? "on" : "off")}"
            : $"{item.Header}";
    }

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
