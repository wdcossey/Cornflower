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
    /// <summary>What a list's line shows before its first click.</summary>
    public const string NothingClicked = "nothing clicked";

    private string _clicked = NothingClicked;

    public ItemClickPage()
    {
        InitializeComponent();
        Clickable.ItemsSource = Items(20);
        Light.ItemsSource = Items(6);
        LightState.Text = NothingClicked;
        ShowState();
    }

    private static List<string> Items(int count) => Enumerable.Range(1, count).Select(i => $"item {i}").ToList();

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

    /// <summary>
    /// Into the check mode and out. Out of it, the checks are cleared: Avalonia keeps one selected when a list goes back
    /// to Single, and with item click on no tap could clear it.
    /// </summary>
    private void OnSelectChanged(object? sender, RoutedEventArgs e)
    {
        var on = Select.IsChecked == true;
        if (!on)
        {
            Clickable.UnselectAll();
        }

        Clickable.SelectionMode = on ? ListViewPage.CheckMode : SelectionMode.Single;
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
