using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 ListView: a list of two-line items with what is selected shown and a button that puts it in the check mode,
/// a list of plain strings, a disabled list in the check mode with two checked, and the light theme's.
/// </summary>
[SamplePage("listview", Order = 119)]
public partial class ListViewPage : UserControl
{
    /// <summary>WinRT's <c>SelectionMode="Multiple"</c>: the check mode.</summary>
    public const SelectionMode CheckMode = SelectionMode.Multiple | SelectionMode.Toggle;

    public ListViewPage()
    {
        InitializeComponent();
        Default.ItemsSource = Names("item", 20);
        Strings.ItemsSource = Names("string", 8);
        Disabled.ItemsSource = Names("item", 4);
        Disabled.SelectedItems!.Add("item 2");
        Disabled.SelectedItems.Add("item 3");
        Light.ItemsSource = Names("item", 6);
        Light.SelectedItems!.Add("item 1");
        ShowState();
    }

    private static List<string> Names(string what, int count) => Enumerable.Range(1, count).Select(i => $"{what} {i}").ToList();

    private void OnSelectChanged(object? sender, RoutedEventArgs e)
    {
        Default.SelectionMode = Select.IsChecked == true ? CheckMode : SelectionMode.Single;
        ShowState();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => ShowState();

    /// <summary>What the first list has selected, read from it.</summary>
    private void ShowState()
    {
        var selected = Default.SelectedItems?.Cast<string>().ToList() ?? [];
        State.Text = selected.Count == 0 ? "nothing selected" : $"selected: {string.Join(", ", selected)}";
    }
}
