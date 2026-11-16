using System;
using System.Linq;
using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 AutoSuggestBox (Avalonia's AutoCompleteBox): the page gives the fruits that start with what is typed, the
/// last one chosen shown; disabled, and in the light theme.
/// </summary>
[SamplePage("autosuggestbox", Order = 158)]
public partial class AutoSuggestBoxPage : UserControl
{
    /// <summary>The reference app's twelve fruits.</summary>
    public static readonly string[] Fruits =
        ["apple", "apricot", "avocado", "banana", "blackberry", "blueberry", "cherry", "grape", "lemon", "mango", "orange", "peach"];

    public AutoSuggestBoxPage()
    {
        InitializeComponent();
    }

    /// <summary>The app's part, as on the phone: the box doesn't filter, so the page gives the suggestions for the text.</summary>
    private void OnPopulating(object? sender, PopulatingEventArgs e)
    {
        var text = e.Parameter ?? "";
        ((AutoCompleteBox)sender!).ItemsSource = text.Length == 0
            ? Array.Empty<string>()
            : Fruits.Where(f => f.StartsWith(text, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
            State.Text = $"chose {e.AddedItems[0]}";
    }
}
