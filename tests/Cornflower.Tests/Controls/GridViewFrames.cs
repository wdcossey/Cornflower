using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/GridView/COMPARISON.md: a ListBoxItem holding a grey tile in the GridView item theme headless
/// on the reference app's stage (450 × 160 at 2.4, so 1080 × 384 like the capture), at rest, in the check mode
/// (<c>PhoneListMultiSelect.IsOn</c> set on it, for the capture's forced <c>GridMultiSelect</c>), and selected in it; and
/// the Catalog's six-tile grid with the second selected (450 × 300). Dark and light, named as the reference app names its
/// captures. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~GridViewFrames
/// </code>
/// </summary>
public class GridViewFrames
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
            foreach (var (specimen, state) in new[]
                     {
                         ("default", "base"), ("default", "MultiSelectStates.GridMultiSelect"),
                         ("selected", "MultiSelectStates.GridMultiSelect"),
                     })
            {
                var item = new ListBoxItem { Content = GridViewTests.TileOf("one"), IsSelected = specimen == "selected" };
                using var stage = new GridViewTests.ItemStage(variant, item);
                if (state != "base")
                    item.SetValue(PhoneListMultiSelect.IsOnProperty, true);
                Dispatcher.UIThread.RunJobs();
                using var frame = stage.Capture();
                frame.Save(Path.Combine(folder, $"{theme}__GridViewItem__{specimen}__{state}.png"), PngBitmapEncoderOptions.Default);
            }

            using var grid = new GridViewTests.GridStage(variant, selected: 1);
            using var gridFrame = grid.Capture();
            gridFrame.Save(Path.Combine(folder, $"{theme}__GridView__six-items__base.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
