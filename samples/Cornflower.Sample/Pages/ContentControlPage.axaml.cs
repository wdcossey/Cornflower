using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 ContentControl: text, an element, a padding, a background the phone doesn't draw, content centred by the page,
/// and the light theme, each in a slot drawn round it.
/// </summary>
[SamplePage("contentcontrol", Order = 117)]
public partial class ContentControlPage : UserControl
{
    public ContentControlPage()
    {
        InitializeComponent();
    }
}
