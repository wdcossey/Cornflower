using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The second plain themes (#235): DropDownButton, SplitButton and ToggleSplitButton, ButtonSpinner, NumericUpDown,
/// GridSplitter, and the light theme.
/// </summary>
[SamplePage("plain buttons", Order = 235)]
public partial class PlainButtonsPage : UserControl
{
    private int _copies = 3;
    private int _replies;

    public PlainButtonsPage()
    {
        InitializeComponent();
    }

    private void OnSpin(object? sender, SpinEventArgs e)
    {
        _copies = Math.Max(0, _copies + (e.Direction == SpinDirection.Increase ? 1 : -1));
        Spinner.Content = _copies == 1 ? "1 copy" : $"{_copies} copies";
    }

    private void OnReply(object? sender, RoutedEventArgs e) =>
        Replies.Text = ++_replies == 1 ? "replied once" : $"replied {_replies} times";
}
