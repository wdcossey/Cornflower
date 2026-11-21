using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 GridView, in the keyed theme: tiles in Single, to press and tap; the check mode with tiles 0, 1 and 3
/// selected, as the probe's; the check mode in the light theme.
/// </summary>
[SamplePage("gridview", Order = 213)]
public partial class GridViewPage : UserControl
{
    public GridViewPage()
    {
        InitializeComponent();
        Single.ItemsSource = Enumerable.Range(1, 9).Select(i => $"tile {i}").ToList();
        Check.ItemsSource = Enumerable.Range(1, 6).Select(i => $"tile {i}").ToList();
        Light.ItemsSource = Enumerable.Range(1, 6).Select(i => $"tile {i}").ToList();
        Single.SelectedIndex = 1;
        foreach (var list in new[] { Check, Light })
        {
            foreach (var i in new[] { 0, 1, 3 })
                list.Selection.Select(i);
        }
    }
}
