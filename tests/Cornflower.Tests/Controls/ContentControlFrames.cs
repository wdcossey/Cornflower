using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ContentControl/COMPARISON.md: a ContentControl in the phone's theme laid out as Pivot's
/// title, on the reference app's Pivot stage (450 × 320 at 2.4, so 1080 × 768 like the capture), dark and light. The
/// capture is the whole Pivot; this draws its title alone, so only the title's rows compare. By hand: it does nothing
/// unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ContentControlFrames
/// </code>
/// </summary>
public class ContentControlFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var title = ContentControlTests.PivotTitle();
            title.Bind(TemplatedControl.ForegroundProperty, title.GetResourceObservable("PhoneForegroundBrush"));
            var stage = new Panel { Children = { title } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));

            using var scene = new Scene(stage, variant, height: 320);
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__ContentControl__pivot-title__base.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
