using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/CommandBar/COMPARISON.md: the reference app's page with an app bar
/// (<see cref="AppBarPage"/>) headless at the reference profile (450 × 800 at 2.4, so 1080 × 1920 like the shell's
/// screenshots), compact and minimal, closed and open, dark and light, named as the reference app names its
/// screenshots. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~CommandBarFrames
/// </code>
/// </summary>
public class CommandBarFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var mode in new[] { AppBarClosedDisplayMode.Compact, AppBarClosedDisplayMode.Minimal })
        foreach (var open in new[] { false, true })
        {
            using var page = new AppBarPage(variant, bar => CommandBarOptions.SetClosedDisplayMode(bar, mode));
            page.Bar.IsOpen = open;
            page.Settle();
            using var frame = page.Capture();
            var name = $"{mode.ToString().ToLowerInvariant()}{(open ? "-open" : "")}";
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__AppBar__{name}__ours.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
