using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/SemanticZoom/COMPARISON.md: the probe's SemanticZoom headless on the 450 × 800 view at 2.4,
/// zoomed in, and zoomed out at rest after a tap on header a, dark, and on a light page under the dark app. Wp81 in the
/// app's styles too, for the converters' brushes. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~SemanticZoomFrames
/// </code>
/// </summary>
public class SemanticZoomFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        using var app = new SemanticZoomTests.AppTheme(ThemeVariant.Dark);
        using (var stage = new SemanticZoomTests.Stage())
        {
            using (var zoomedIn = stage.Capture())
                zoomedIn.Save(Path.Combine(folder, "dark__zoomed-in.png"), PngBitmapEncoderOptions.Default);
            stage.OpenAtRest();
            using var zoomedOut = stage.Capture();
            zoomedOut.Save(Path.Combine(folder, "dark__zoomed-out.png"), PngBitmapEncoderOptions.Default);
        }

        using (var stage = new SemanticZoomTests.Stage(page: ThemeVariant.Light))
        {
            stage.OpenAtRest();
            using var lightPage = stage.Capture();
            lightPage.Save(Path.Combine(folder, "dark__zoomed-out-light-page.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
