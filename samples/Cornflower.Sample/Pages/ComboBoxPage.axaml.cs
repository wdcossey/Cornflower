using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 ComboBox: five items open inline and seven full screen, with what is picked shown; one with nothing picked,
/// one disabled, and one in the light theme.
/// </summary>
[SamplePage("combobox", Order = 155)]
public partial class ComboBoxPage : UserControl
{
    public ComboBoxPage()
    {
        InitializeComponent();
        ShowState();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => ShowState();

    /// <summary>What the first two have picked, read from them. The XAML selects during <c>InitializeComponent</c>, before the fields are set.</summary>
    private void ShowState()
    {
        if (State is null || Inline is null || Full is null) return;
        State.Text = $"{Inline.SelectedItem}, {Full.SelectedItem}";
    }
}
