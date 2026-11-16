using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Headless;
using Avalonia;
using static Cornflower.Tests.Controls.PivotReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/Pivot/COMPARISON.md: the catalogue's Pivot (titled "CORNFLOWER", items "one" and "two")
/// full width on its 450 × 320 stage over the page's background, headless at the reference profile, dark and light,
/// named as the reference app names its captures; and the four-item Pivot of #128's probe part way through a swipe.
/// By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~PivotFrames
/// </code>
/// </summary>
public class PivotFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var pivot = PivotTests.Stage.Make(["one", "two"]);
            ((Cornflower.Controls.PivotItem)pivot.Items[0]!).Content = new TextBlock { Text = "first pivot item" };
            ((Cornflower.Controls.PivotItem)pivot.Items[1]!).Content = new TextBlock { Text = "second pivot item" };
            var stage = new Grid { Height = StageHeight, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Children = { pivot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            // In a portrait window, as the phone's stage is on its portrait screen; the stage alone is saved.
            using var scene = new Scene(stage, variant);
            using var frame = new RenderTargetBitmap(new PixelSize(1080, 768), new Vector(96 * Scene.ReferenceScaling, 96 * Scene.ReferenceScaling));
            frame.Render(stage);
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__Pivot__two-items__base__ours.png"), PngBitmapEncoderOptions.Default);
        }

        using (var clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Frame), FramesOnGrid = true })
        {
            var pivot = PivotTests.Stage.Make(["one", "two", "three", "four"]);
            var stage = new Grid { Children = { pivot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            using var scene = new Scene(stage);
            var touch = scene.Window.TouchBegin(new Point(380, 450), RawInputModifiers.None);
            clock.At(10);
            scene.Window.TouchMove(touch, new Point(320, 450), RawInputModifiers.None);
            using var frame = scene.Capture();
            frame.Save(Path.Combine(folder, "dark__Pivot__four-items__dragged-60__ours.png"), PngBitmapEncoderOptions.Default);
            scene.Window.TouchEnd(touch, new Point(320, 450), RawInputModifiers.None);
        }
    }
}
