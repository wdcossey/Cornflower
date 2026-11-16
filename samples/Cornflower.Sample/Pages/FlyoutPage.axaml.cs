using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 Flyout: a Button's flyout across the top of the window and one that fills it, what happened last shown, and a
/// switch for the light theme.
/// </summary>
[SamplePage("flyout", Order = 151)]
public partial class FlyoutPage : UserControl
{
    public FlyoutPage()
    {
        InitializeComponent();
    }

    private Flyout TopFlyout => (Flyout)OpenTop.Flyout!;

    private Flyout FullFlyout => (Flyout)OpenFull.Flyout!;

    private string NameOf(object? sender) => sender == FullFlyout ? "full" : "top";

    private void OnOpened(object? sender, System.EventArgs e) => State.Text = $"{NameOf(sender)} opened";

    private void OnClosed(object? sender, System.EventArgs e) => State.Text = $"{NameOf(sender)} closed";

    private void OnCloseTop(object? sender, RoutedEventArgs e) => TopFlyout.Hide();

    private void OnCloseFull(object? sender, RoutedEventArgs e) => FullFlyout.Hide();

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
