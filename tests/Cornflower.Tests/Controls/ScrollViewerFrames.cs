using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/ScrollViewer/COMPARISON.md: a ScrollViewer filling the 450 × 800 reference view (1080 ×
/// 1920 at 2.4) over a page 2000 tall, at rest, scrolled with its touch indicator shown, at the end, and disabled with
/// the indicator kept (AllowAutoHide off); and one 900 wide scrolled sideways; dark and light. By hand: it does nothing
/// unless <c>CORNFLOWER_FRAMES</c> names a folder.
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
            if (state == "TouchIndicator")
                stage.ScrollTo(600);
            if (state == "TouchIndicator-end")
                stage.ScrollTo(1200);
            if (state == "sideways")
                stage.Viewer.Offset = new Avalonia.Vector(225, 600);
            Dispatcher.UIThread.RunJobs();
            using var frame = stage.Capture();
            frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__ScrollViewer__page__{state}.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
