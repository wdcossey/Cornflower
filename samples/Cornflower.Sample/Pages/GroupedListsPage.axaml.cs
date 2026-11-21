using Avalonia.Controls;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>A group of the sample's: its letter, and its items.</summary>
public sealed class LetterGroup(string key, IEnumerable<string> items) : List<string>(items)
{
    public string Key { get; } = key;
}

/// <summary>
/// Grouped lists (#131), as #211's probe: a grouped ListView, whose header sticks, over a grouped GridView, whose
/// headers don't; groups a to h, b and e empty.
/// </summary>
[SamplePage("grouped lists", Order = 131)]
public partial class GroupedListsPage : UserControl
{
    public GroupedListsPage()
    {
        InitializeComponent();
        ListBoxOptions.SetGroupedItemsSource(List, Letters());
        ListBoxOptions.SetGroupedItemsSource(Grid, Letters());
    }

    private static List<LetterGroup> Letters() =>
        "abcdefgh".Select(ch => new LetterGroup(ch.ToString(),
            ch is 'b' or 'e' ? [] : Enumerable.Range(1, 5).Select(i => $"{ch} item {i}"))).ToList();
}
