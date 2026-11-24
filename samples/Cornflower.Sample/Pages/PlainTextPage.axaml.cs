using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The third plain themes (#236): Label, HeaderedContentControl, GroupBox, PathIcon, ToolTip and
/// TransitioningContentControl, and the light theme.
/// </summary>
[SamplePage("plain text", Order = 236)]
public partial class PlainTextPage : UserControl
{
    private int _shown;

    public PlainTextPage()
    {
        InitializeComponent();
    }

    private void OnNext(object? sender, RoutedEventArgs e) =>
        Transitioning.Content = ++_shown % 2 == 1 ? "the second page of content" : "the first page of content";
}
