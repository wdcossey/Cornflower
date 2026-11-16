using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/Flyout/COMPARISON.md: the reference app's stage, the "open" Button and its flyout, "flyout
/// content", headless at the reference profile (450 × 800 at 2.4, so 1080 × 1920 like the emulator's screenshots), top
/// and full, dark and light, at rest, named as the reference app names its screenshots. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~FlyoutFrames
/// </code>
/// </summary>
public class FlyoutFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var full in new[] { false, true })
        {
            var dark = variant == ThemeVariant.Dark;
            using var stage = new FlyoutTests.Stage(variant, full, page: new SolidColorBrush(FlyoutReadings.Page(dark)));
            stage.OpenAtRest();
            using var frame = stage.Scene.Capture();
            var name = $"{variant.ToString().ToLowerInvariant()}__Flyout__{(full ? "full" : "default")}__ours.png";
            frame.Save(Path.Combine(folder, name), PngBitmapEncoderOptions.Default);
        }
    }
}
