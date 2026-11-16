using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
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

        // The open bar with other numbers of secondary commands, named as the reference app names them (#141).
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (var lines in new[] { 0, 1, 3, 4, 5, 10 })
        {
            using var page = new AppBarPage(variant, bar =>
            {
                bar.SecondaryCommands.Clear();
                for (var i = 0; i < lines; i++)
                    bar.SecondaryCommands.Add(new CommandBarButton { Label = i switch { 0 => "first menu item", 1 => "second menu item", _ => $"menu item {i + 1}" } });
            });
            page.Bar.IsOpen = true;
            page.Settle();
            using var frame = page.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__BottomAppBar__compact-open-{lines}-secondary__ours.png"), PngBitmapEncoderOptions.Default);
        }

        // A finger held on add, closed and open, as the recording's stills (#141).
        foreach (var open in new[] { false, true })
        {
            using var page = new AppBarPage();
            page.Bar.IsOpen = open;
            page.Settle();
            page.Window.MouseDown(page.CentreOf(page.Add), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            using var frame = page.Capture();
            frame.Save(Path.Combine(folder, $"dark__AppBar__add-pressed-{(open ? "open" : "closed")}__ours.png"), PngBitmapEncoderOptions.Default);
            page.Window.MouseUp(page.CentreOf(page.Add), MouseButton.Left);
        }
    }
}
