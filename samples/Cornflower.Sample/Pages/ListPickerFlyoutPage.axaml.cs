using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 full-screen pickers: a ListPickerFlyout in Single and in Multiple and a PickerFlyout with its buttons, each
/// opened with ShowAtAsync and what it gave back shown, and a switch for the light theme.
/// </summary>
[SamplePage("listpicker", Order = 153)]
public partial class ListPickerFlyoutPage : UserControl
{
    private static readonly string[] Colours = ["red", "orange", "yellow", "green", "blue", "indigo", "violet"];

    public ListPickerFlyoutPage()
    {
        InitializeComponent();
        Single = new ListPickerFlyout { ItemsSource = Colours, SelectedIndex = 4 };
        PickerFlyoutBase.SetTitle(Single, "COLOUR");
        Multiple = new ListPickerFlyout { ItemsSource = Colours, SelectionMode = ListPickerFlyoutSelectionMode.Multiple };
        PickerFlyoutBase.SetTitle(Multiple, "COLOURS");
        Picker = new PickerFlyout
        {
            ConfirmationButtonsVisible = true,
            Content = new TextBlock { Text = "Any content goes here.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 19) },
        };
        PickerFlyoutBase.SetTitle(Picker, "PICKER");
    }

    /// <summary>The list picker in Single.</summary>
    public ListPickerFlyout Single { get; }

    /// <summary>The list picker in Multiple.</summary>
    public ListPickerFlyout Multiple { get; }

    /// <summary>The picker flyout.</summary>
    public PickerFlyout Picker { get; }

    private async void OnOpenSingle(object? sender, RoutedEventArgs e) =>
        State.Text = Shown("list picker", await Single.ShowAtAsync(OpenSingle));

    private async void OnOpenMultiple(object? sender, RoutedEventArgs e) =>
        State.Text = Shown("multiple", await Multiple.ShowAtAsync(OpenMultiple));

    private async void OnOpenPicker(object? sender, RoutedEventArgs e) =>
        State.Text = await Picker.ShowAtAsync(OpenPicker) ? "picker accepted" : "picker cancelled";

    private static string Shown(string name, IReadOnlyList<object>? picked) =>
        picked is null ? $"{name}: nothing picked" : $"{name}: {string.Join(", ", picked)}";

    private void OnLightChanged(object? sender, RoutedEventArgs e) =>
        Scope.RequestedThemeVariant = Light.IsChecked == true ? ThemeVariant.Light : null;
}
