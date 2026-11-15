using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ScrollViewer/COMPARISON.md. The reference app's specimens on their stages, named as it
/// names its captures (run <c>20261115-1921-dev11</c>): the ScrollBars <c>vertical</c>, <c>horizontal</c> and
/// <c>no-indicator</c> at rest and disabled, the first two in <c>NoIndicator</c> too, and the ScrollViewer <c>rest</c>
/// and <c>offset-200</c> once its indicator has gone (the phone's captures can't draw it). And a ScrollViewer filling the
/// 450 × 800 view over a page 2000 tall: at rest, scrolled with its touch indicator shown, at the end, disabled with the
/// indicator kept (AllowAutoHide off), and 900 wide scrolled sideways. Dark and light. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~ScrollViewerFrames
/// </code>
/// </summary>
public class ScrollViewerFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var theme = variant.ToString().ToLowerInvariant();
            void Save(WriteableBitmap frame, string name) => frame.Save(Path.Combine(folder, $"{theme}__{name}.png"), PngBitmapEncoderOptions.Default);

            foreach (var specimen in new[] { "vertical", "horizontal", "no-indicator" })
            foreach (var state in new[] { "base", "CommonStates.Disabled", "ScrollingIndicatorStates.NoIndicator" })
            {
                if (specimen == "no-indicator" && state == "ScrollingIndicatorStates.NoIndicator")
                    continue;
                using var clock = new TestClock();
                using var stage = new ScrollViewerTests.BarStage(specimen, variant, disabled: state == "CommonStates.Disabled");
                if (state == "ScrollingIndicatorStates.NoIndicator")
                {
                    PhoneScrollIndicators.SetIndicatorMode(stage.Bar, PhoneIndicatorMode.None);
                    clock.At(1000);
                }
                using var frame = stage.Capture();
                Save(frame, $"ScrollBar__{specimen}__{state}");
            }

            foreach (var (specimen, offset) in new[] { ("rest", 0.0), ("offset-200", 200.0) })
            {
                using var clock = new TestClock();
                using var stage = ScrollViewerTests.Lines(offset, variant);
                clock.At(1000);
                Dispatcher.UIThread.RunJobs();
                using var frame = stage.Capture();
                Save(frame, $"ScrollViewer__{specimen}__base");
            }

            foreach (var state in new[] { "base", "TouchIndicator", "TouchIndicator-end", "CommonStates.Disabled", "sideways" })
            {
                using var clock = new TestClock();
                var viewer = new ScrollViewer();
                if (state == "CommonStates.Disabled")
                {
                    viewer.AllowAutoHide = false;
                    viewer.IsEnabled = false;
                }
                if (state == "sideways")
                    viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                using var stage = new ScrollViewerTests.Stage(contentHeight: 2000, contentWidth: state == "sideways" ? 900 : double.NaN, variant: variant, viewer: viewer);

                // Scrolled a step every 40 ms for 400 ms, so the touch's indicator has faded in whole.
                var to = state switch { "TouchIndicator" => 600.0, "TouchIndicator-end" => 1200.0, "sideways" => 600.0, _ => 0.0 };
                if (to > 0)
                {
                    for (var t = 0; t <= 400; t += 40)
                    {
                        clock.At(t);
                        stage.Viewer.Offset = new Avalonia.Vector(state == "sideways" ? 225 * t / 400.0 : 0, to - 400 + t);
                        Dispatcher.UIThread.RunJobs();
                    }
                }
                using var frame = stage.Capture();
                Save(frame, $"ScrollViewer__page__{state}");
            }
        }
    }
}
