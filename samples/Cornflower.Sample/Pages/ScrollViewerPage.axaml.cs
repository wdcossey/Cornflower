using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 ScrollViewer and its thin indicators: the page itself, with how far it has scrolled shown, a strip that
/// scrolls sideways, boxes whose indicator stays shown (enabled, disabled and in the light theme), and more of the page.
/// </summary>
[SamplePage("scrollviewer", Order = 116)]
public partial class ScrollViewerPage : UserControl
{
    public ScrollViewerPage()
    {
        InitializeComponent();
        Fill(PageLines, "line", 30);
        Fill(ShownLines, "item", 12);
        Fill(DisabledLines, "item", 12);
        Fill(LightLines, "item", 12);
        for (var i = 1; i <= 12; i++)
        {
            var tile = new TextBlock { Text = $"tile {i}" };
            tile.Bind(ThemeProperty, tile.GetResourceObservable("SubheaderTextBlockStyle"));
            Strip.Children.Add(tile);
        }
        ShowState();
    }

    private static void Fill(Panel panel, string what, int count)
    {
        for (var i = 1; i <= count; i++)
            panel.Children.Add(new TextBlock { Text = $"{what} {i}" });
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, Page))
            ShowState();
    }

    /// <summary>How far the page has scrolled, read from it.</summary>
    private void ShowState()
    {
        if (State is null || Page is null) return;
        State.Text = $"scrolled {Page.Offset.Y:0} of {Page.ScrollBarMaximum.Y:0}";
    }
}
