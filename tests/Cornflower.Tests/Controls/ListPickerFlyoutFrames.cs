using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ListPickerFlyout/COMPARISON.md: the reference app's pickers (a ListPickerFlyout of "one" to
/// "five" titled "LIST PICKER", Single with "two" selected and Multiple with "two" and "four" checked; a PickerFlyout of
/// "picker flyout content", with and without its buttons), headless at the reference profile, dark and light, at rest,
/// named as the reference app names its screenshots. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a
/// folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ListPickerFlyoutFrames
/// </code>
/// </summary>
public class ListPickerFlyoutFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var single = ListPickerFlyoutTests.ListPicker();
            single.SelectedIndex = 1;
            One(folder, variant, single, "ListPickerFlyout", "single");
            var multiple = ListPickerFlyoutTests.ListPicker(ListPickerFlyoutSelectionMode.Multiple);
            multiple.SelectedItems.Add("two");
            multiple.SelectedItems.Add("four");
            One(folder, variant, multiple, "ListPickerFlyout", "multiple-two-checked");
            One(folder, variant, ListPickerFlyoutTests.Picker(), "PickerFlyout", "confirmation-buttons");
            One(folder, variant, ListPickerFlyoutTests.Picker(buttons: false), "PickerFlyout", "no-confirmation-buttons");
        }
    }

    private static void One(string folder, ThemeVariant variant, PickerFlyoutBase picker, string name, string state)
    {
        using var stage = new ListPickerFlyoutTests.Stage(picker, variant);
        stage.OpenAtRest();
        using var frame = stage.Scene.Capture();
        var file = $"{variant.ToString().ToLowerInvariant()}__{name}__{state}__ours.png";
        frame.Save(Path.Combine(folder, file), PngBitmapEncoderOptions.Default);
    }
}
