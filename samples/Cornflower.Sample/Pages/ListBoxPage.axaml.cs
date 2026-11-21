using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 ListBox, in the keyed theme: a list with an item selected, to press and tap; a disabled one; the light theme.
/// </summary>
[SamplePage("listbox", Order = 212)]
public partial class ListBoxPage : UserControl
{
    public ListBoxPage()
    {
        InitializeComponent();
        Default.ItemsSource = Enumerable.Range(1, 8).Select(i => $"list box item {i}").ToList();
        Disabled.ItemsSource = new[] { "one", "two", "three" };
        Light.ItemsSource = new[] { "one", "two", "three" };
        Default.SelectedIndex = Disabled.SelectedIndex = Light.SelectedIndex = 1;
    }
}
