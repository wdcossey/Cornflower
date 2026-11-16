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

        // Mid-motion, at the ticks nearest #149's stills (ref/wp81/Flyout/motion/): the top band at 52.1° (the still's
        // 51.9°), the full one's bands 6 ticks in (4.3, 22.6, 34.5 and 40.6°; the still's 4.4, 22.2, 33.4 and 41.4) and
        // 8 ticks into its closing (−65.7, −43.8, −21.9°; the still's −66.3, −45.6, −24.5).
        Motion(folder, "top-opening", full: false, closing: false, ticks: 3);
        Motion(folder, "full-opening", full: true, closing: false, ticks: 6);
        Motion(folder, "full-closing", full: true, closing: true, ticks: 8);
    }

    private static void Motion(string folder, string name, bool full, bool closing, int ticks)
    {
        using var stage = new FlyoutTests.Stage(ThemeVariant.Dark, full, page: new SolidColorBrush(FlyoutReadings.Page(dark: true)));
        if (closing)
        {
            stage.OpenAtRest();
            var first = stage.FirstFrameAfter(stage.Clock.Now.TotalMilliseconds);
            stage.Flyout.Hide();
            stage.At(first + ticks * FlyoutReadings.Tick);
        }
        else
        {
            stage.Open();
            stage.At(stage.FirstFrameAfter(0) + ticks * FlyoutReadings.Tick);
        }

        using var frame = stage.Scene.Capture();
        frame.Save(Path.Combine(folder, $"dark__motion__{name}__ours.png"), PngBitmapEncoderOptions.Default);
    }
}
