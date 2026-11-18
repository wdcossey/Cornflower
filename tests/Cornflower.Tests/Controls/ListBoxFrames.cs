using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ListBox/COMPARISON.md: a ListBoxItem in the item theme headless on the reference app's stage
/// (450 × 100 at 2.4, so 1080 × 240 like the capture), at rest, pressed at its centre and settled, disabled, selected and
/// selected disabled (<c>IsSelected</c> on its own, for the capture's forced states); and the Catalog's three-item list
/// with the second selected (450 × 260), at rest and disabled. Dark and light, named as the reference app names its
/// captures. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ListBoxFrames
/// </code>
/// </summary>
public class ListBoxFrames
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
            foreach (var state in new[] { "base", "CommonStates.Pressed", "CommonStates.Disabled", "SelectionStates.Selected", "SelectionStates.SelectedDisabled" })
            {
                using var clock = new TestClock();
                var item = new ListBoxItem
                {
                    Content = "list box item",
                    IsSelected = state.StartsWith("SelectionStates", StringComparison.Ordinal),
                    IsEnabled = !state.EndsWith("Disabled", StringComparison.Ordinal),
                };
                using var stage = new ListBoxTests.ItemStage(variant, item);
                if (state == "CommonStates.Pressed")
                    stage.Window.TouchBegin(stage.At(stage.Item.Bounds.Width / 2, stage.Item.Bounds.Height / 2));
                clock.At(160);
                Dispatcher.UIThread.RunJobs();
                using var frame = stage.Capture();
                frame.Save(Path.Combine(folder, $"{theme}__ListBoxItem__default__{state}.png"), PngBitmapEncoderOptions.Default);
            }

            foreach (var (state, enabled) in new[] { ("base", true), ("CommonStates.Disabled", false) })
            {
                using var list = new ListBoxTests.ListStage(variant, selected: 1, enabled: enabled);
                using var listFrame = list.Capture();
                listFrame.Save(Path.Combine(folder, $"{theme}__ListBox__three-items__{state}.png"), PngBitmapEncoderOptions.Default);
            }
        }
    }
}
