using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/FlipView/COMPARISON.md: the reference app's <c>FlipView</c> <c>three-items</c> specimen, a
/// Carousel of three grey panels ("one", "two", "three", 40 px text) with the second selected, full bleed on its 450 × 300
/// stage at 2.4 (1080 × 720 like the capture), at rest, dark and light, named as the reference app names its captures. By
/// hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~FlipViewFrames
/// </code>
/// </summary>
public class FlipViewFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var carousel = new Carousel();
            foreach (var name in new[] { "one", "two", "three" })
                carousel.Items.Add(new Grid { Background = new SolidColorBrush(Color.FromRgb(128, 128, 128)), Children = { new TextBlock { Text = name, FontSize = 40 } } });
            carousel.SelectedIndex = 1;
            var stage = new Grid { Children = { carousel } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            using var scene = new Scene(stage, variant, height: 300);
            Dispatcher.UIThread.RunJobs();
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__FlipView__three-items__base.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
