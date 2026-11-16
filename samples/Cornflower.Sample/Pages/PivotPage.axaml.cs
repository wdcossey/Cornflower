using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 Pivot: four items to swipe round or pick by their headers, the selection shown, and switches that lock the
/// Pivot and make the page light; an item that scrolls up and down, and one with a Slider.
/// </summary>
[SamplePage("pivot", Order = 121)]
public partial class PivotPage : UserControl
{
    private static readonly string[] Ordinals = ["first", "second", "third", "fourth"];

    public PivotPage()
    {
        InitializeComponent();
        for (var i = 1; i <= 30; i++)
            LineList.Children.Add(new TextBlock { Text = $"line {i}" });
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // The first item is selected as it is added, while the page is still being made.
        if (State is not null && Pivot?.SelectedItem is PivotItem item)
            State.Text = $"{item.Header}, the {Ordinals[Pivot.SelectedIndex]} of four";
    }

    private void OnLockedChanged(object? sender, RoutedEventArgs e) => Pivot.IsLocked = Locked.IsChecked == true;

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
