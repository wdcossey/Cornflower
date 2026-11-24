using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>The fourth plain themes (#237): MenuItem, Menu and ContextMenu, and the light theme.</summary>
[SamplePage("plain menus", Order = 237)]
public partial class PlainMenusPage : UserControl
{
    public PlainMenusPage()
    {
        InitializeComponent();
    }

    private void OnPicked(object? sender, RoutedEventArgs e) =>
        Picked.Text = sender is MenuItem { Header: string header } ? "picked: " + header.Replace("_", "") : "picked";
}
