using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ContentDialog/COMPARISON.md: the reference app's dialogs over a stage with the "open"
/// Button, headless at the reference profile (450 × 800 at 2.4, so 1080 × 1920 like the emulator's screenshots), dark
/// and light, at rest, named as the reference app names its screenshots. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ContentDialogFrames
/// </code>
/// </summary>
public class ContentDialogFrames
{
    private const string LongTitle = "Agjy 0123 a content dialog title long enough to wrap onto a second line";

    private static string LongBody() =>
        string.Join("\n", Enumerable.Range(1, 40).Select(i => $"line {i} of the dialog's long body"));

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var dark = variant == ThemeVariant.Dark;
            SaveOne(folder, variant, "default", () => new ContentDialogTests.Stage(variant, page: Page(dark)));
            SaveOne(folder, variant, "no-buttons", () => new ContentDialogTests.Stage(variant, null, null, page: Page(dark)));
            SaveOne(folder, variant, "primary-only", () => new ContentDialogTests.Stage(variant, "ok", null, page: Page(dark)));
            SaveOne(folder, variant, "secondary-only", () => new ContentDialogTests.Stage(variant, null, "cancel", page: Page(dark)));
            SaveOne(folder, variant, "full-size", () => new ContentDialogTests.Stage(variant, fullSize: true, page: Page(dark)));
            SaveOne(folder, variant, "long", () => new ContentDialogTests.Stage(variant, title: LongTitle, content: LongBody(), page: Page(dark)));
        }

        // Mid-opening, at the tick nearest #149's still (ref/wp81/ContentDialog/motion/, turned 21.1°): tick 3, 21.2°.
        using var stage = new ContentDialogTests.Stage(ThemeVariant.Dark, page: Page(dark: true));
        stage.Show();
        stage.At(stage.FirstFrameAfter(0) + 3 * ContentDialogReadings.Tick);
        using var frame = stage.Scene.Capture();
        frame.Save(Path.Combine(folder, "dark__motion__opening__ours.png"), PngBitmapEncoderOptions.Default);
    }

    private static IBrush Page(bool dark) => dark ? Brushes.Black : Brushes.White;

    private static void SaveOne(string folder, ThemeVariant variant, string name, Func<ContentDialogTests.Stage> make)
    {
        using var stage = make();
        stage.ShowAtRest();
        using var frame = stage.Scene.Capture();
        frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__ContentDialog__{name}__ours.png"),
            PngBitmapEncoderOptions.Default);
    }
}
