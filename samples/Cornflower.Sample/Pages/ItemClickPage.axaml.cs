using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// WinRT's item click on the 8.1 ListView: a list whose taps click its items, with the last click and the selection
/// shown and a button that puts it in the check mode, and the light theme's.
/// </summary>
[SamplePage("itemclick", Order = 132)]
public partial class ItemClickPage : UserControl
{
    /// <summary>WinRT's <c>SelectionMode="Multiple"</c>: the check mode.</summary>
    public const SelectionMode CheckMode = SelectionMode.Multiple | SelectionMode.Toggle;

    private string _clicked = "nothing clicked";

    public ItemClickPage()
    {
        InitializeComponent();
        Clickable.ItemsSource = Names("item", 20);
        Light.ItemsSource = Names("item", 6);
        LightState.Text = "nothing clicked";
        ShowState();
    }

    private static List<string> Names(string what, int count) => Enumerable.Range(1, count).Select(i => $"{what} {i}").ToList();

    private void OnItemClick(object? sender, ItemClickEventArgs e)
    {
        var text = $"clicked: {e.ClickedItem}";
        if (sender == Light)
        {
            LightState.Text = text;
            return;
        }

        _clicked = text;
        ShowState();
    }

    private void OnSelectChanged(object? sender, RoutedEventArgs e)
    {
        Clickable.SelectionMode = Select.IsChecked == true ? CheckMode : SelectionMode.Single;
        ShowState();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => ShowState();

    /// <summary>The first list's last click and what it has selected, read from it.</summary>
    private void ShowState()
    {
        var selected = Clickable.SelectedItems?.Cast<string>().ToList() ?? [];
        State.Text = $"{_clicked} · " + (selected.Count == 0 ? "nothing selected" : $"selected: {string.Join(", ", selected)}");
    }
}
