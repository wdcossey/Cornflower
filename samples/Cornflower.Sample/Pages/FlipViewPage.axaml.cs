using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 FlipView, on Avalonia's Carousel: five coloured items to flip through sideways, with buttons that set
/// SelectedIndex from code, and the same five turned up and down.
/// </summary>
[SamplePage("flipview", Order = 214)]
public partial class FlipViewPage : UserControl
{
    private static readonly FlipViewSample[] Samples =
    [
        new("one", Brushes.SteelBlue),
        new("two", Brushes.SeaGreen),
        new("three", Brushes.IndianRed),
        new("four", Brushes.DarkOrange),
        new("five", Brushes.MediumPurple),
    ];

    public FlipViewPage()
    {
        InitializeComponent();
        Across.ItemsSource = Samples;
        Down.ItemsSource = Samples;
        Across.SelectionChanged += (_, _) => ShowState();
        ShowState();
    }

    private void ShowState() => State.Text = $"item {Across.SelectedIndex + 1} of {Samples.Length}";

    private void OnNext(object? sender, RoutedEventArgs e) => Across.SelectedIndex = Math.Min(Across.SelectedIndex + 1, Samples.Length - 1);

    // Two along, on or, near the end, back: always a jump.
    private void OnTwoOn(object? sender, RoutedEventArgs e) =>
        Across.SelectedIndex = Across.SelectedIndex + 2 < Samples.Length ? Across.SelectedIndex + 2 : Across.SelectedIndex - 2;

    private void OnFirst(object? sender, RoutedEventArgs e) => Across.SelectedIndex = 0;
}

/// <summary>An item of the FlipView page: its name and colour.</summary>
public sealed record FlipViewSample(string Name, IBrush Brush);
