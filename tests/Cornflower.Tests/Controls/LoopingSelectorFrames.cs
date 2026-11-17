using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using static Cornflower.Tests.Controls.LoopingSelectorReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/LoopingSelector/COMPARISON.md: the DatePickerFlyout's three wheels (15 July 2014) at the
/// phone's places, headless at the reference profile, on the flyout's background: at rest, dark and light, and with the
/// month wheel active (expanded), dark. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~LoopingSelectorFrames
/// </code>
/// </summary>
public class LoopingSelectorFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var (name, variant, expanded) in new[]
                 {
                     ("dark__LoopingSelector__date__rest", ThemeVariant.Dark, false),
                     ("light__LoopingSelector__date__rest", ThemeVariant.Light, false),
                     ("dark__LoopingSelector__date__month-expanded", ThemeVariant.Dark, true),
                 })
        {
            using var clock = new TestClock();
            using var stage = new LoopingSelectorTests.Stage(variant);
            stage.Canvas.Bind(Panel.BackgroundProperty, stage.Canvas.GetResourceObservable("FlyoutBackgroundThemeBrush"));
            if (expanded)
            {
                var touch = stage.Window.TouchBegin(stage.At(stage.Month, ColumnHeight / 2), RawInputModifiers.None);
                clock.At(78);
                stage.Window.TouchEnd(touch, stage.At(stage.Month, ColumnHeight / 2), RawInputModifiers.None);
            }

            stage.Settle();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var frame = stage.Window.CaptureRenderedFrame()!;
            frame.Save(Path.Combine(folder, name + "__ours.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
