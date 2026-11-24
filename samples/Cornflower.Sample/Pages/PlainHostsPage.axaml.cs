using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The first plain themes (#234): the page's host (Window or EmbeddableControlRoot), an ItemsControl, Separators, a
/// Popup's host, and the light theme.
/// </summary>
[SamplePage("plain hosts", Order = 234)]
public partial class PlainHostsPage : UserControl
{
    public PlainHostsPage()
    {
        InitializeComponent();
    }
}
