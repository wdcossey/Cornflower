using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ListView/COMPARISON.md: a ListViewItem headless on the reference app's stage (450 × 100 at
/// 2.4, so 1080 × 240 like the capture), unselected and selected, at rest, pressed at its centre and settled, disabled,
/// in the check mode, and pressed in the check mode; and the Catalog's three-item list with the second selected (450 ×
/// 260). Dark and light, named as the reference app names its captures. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ListViewFrames
/// </code>
/// </summary>
public class ListViewFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var theme = variant.ToString().ToLowerInvariant();
            foreach (var (name, isSelected) in new[] { ("default", false), ("selected", true) })
            foreach (var state in new[] { "base", "CommonStates.Pressed", "CommonStates.Disabled", "MultiSelectStates.ListMultiSelect", "MultiSelectStates.ListMultiSelect+CommonStates.CheckboxPressed" })
            {
                if (name == "selected" && state is "CommonStates.Pressed" or "CommonStates.Disabled")
                    continue;

                using var clock = new TestClock();
                var item = new ListBoxItem { Content = "list view item", IsSelected = isSelected, IsEnabled = state != "CommonStates.Disabled" };
                // In the check mode before it is shown, as a forced state: no slide.
                if (state.StartsWith("MultiSelectStates.ListMultiSelect", StringComparison.Ordinal))
                    item.SetValue(PhoneListMultiSelect.IsOnProperty, true);
                using var stage = new ListViewTests.ItemStage(variant, item);
                if (state.EndsWith("Pressed", StringComparison.Ordinal))
                    stage.Window.TouchBegin(stage.At(stage.Item.Bounds.Width / 2, stage.Item.Bounds.Height / 2));
                clock.At(160);
                Dispatcher.UIThread.RunJobs();
                using var frame = stage.Capture();
                frame.Save(Path.Combine(folder, $"{theme}__ListViewItem__{name}__{state}.png"), PngBitmapEncoderOptions.Default);
            }

            using var list = new ListViewTests.ListStage(variant, selected: 1);
            using var listFrame = list.Capture();
            listFrame.Save(Path.Combine(folder, $"{theme}__ListView__three-items__base.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
