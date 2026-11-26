using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>The eighth plain themes, part d (#241): PipsPagers, horizontal, overflowing and vertical, and their indexes.</summary>
[SamplePage("plain pips", Order = 244)]
public partial class PlainPipsPage : UserControl
{
    public PlainPipsPage()
    {
        InitializeComponent();

        Show(Five, FiveIndex);
        Show(Twelve, TwelveIndex);
        Show(Vertical, VerticalIndex);
    }

    /// <summary>The pager's selected page, one-based, in its line, now and at each change.</summary>
    private static void Show(PipsPager pager, TextBlock line)
    {
        void Update() => line.Text = $"page {pager.SelectedPageIndex + 1} of {pager.NumberOfPages}";
        Update();
        pager.SelectedIndexChanged += (_, _) => Update();
    }
}
