using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/Page/COMPARISON.md: the reference app's <c>pages</c> probe rebuilt (run
/// <c>20261110-0810-dev11</c>: a Frame navigating between a white and a blue sheet inset 19, 60, labelled "page A" and
/// "page B"), as a NavigationPage of two ContentPages, caught at the moments of the emulator's frames in
/// <c>ref/wp81/Page/</c>: at rest, the turnstile's first frame forward and back, the slide's second frame forward, and
/// the continuum's first frame of the new page. Dark, as the run was. By hand: it does nothing unless
/// <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~PageFrames
/// </code>
/// </summary>
public class PageFrames
{
    private static ContentPage Sheet(string label, IBrush brush) => new()
    {
        Content = new Border
        {
            Margin = new Thickness(19, 60),
            Background = brush,
            Child = new TextBlock { Text = label, Foreground = Brushes.Black, Margin = new Thickness(6, 4), FontSize = 20 },
        },
    };

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        Shoot(folder, "rest", null, true, -1);
        Shoot(folder, "turnstile-forward-tick0", null, true, 0);
        Shoot(folder, "turnstile-back-tick0", null, false, 0);
        Shoot(folder, "slide-forward-tick1", new SlideTransition(), true, 1);
        Shoot(folder, "continuum-forward-first", new ContinuumTransition(), true, ContinuumTransition.FadeLength.TotalMilliseconds / 15.625);
    }

    /// <summary>A frame <paramref name="tick"/> emulator frames into a navigation from A to B, or back from B to A (−1: before it).</summary>
    private static void Shoot(string folder, string name, IPageTransition? transition, bool forward, double tick)
    {
        var a = Sheet("page A", Brushes.White);
        var b = Sheet("page B", Brushes.Blue);
        var nav = new NavigationPage();
        using var scene = new Scene(nav);
        _ = nav.PushAsync(a);
        Dispatcher.UIThread.RunJobs();
        if (!forward)
        {
            _ = nav.PushAsync(b, null);
            Dispatcher.UIThread.RunJobs();
        }

        using var clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(15.625), FramesOnGrid = true };
        if (tick >= 0)
        {
            _ = forward
                ? transition is null ? nav.PushAsync(b) : nav.PushAsync(b, transition)
                : transition is null ? nav.PopAsync() : nav.PopAsync(transition);
            Dispatcher.UIThread.RunJobs();
            clock.At((tick + 1) * 15.625);
        }

        using WriteableBitmap frame = scene.Capture();
        frame.Save(Path.Combine(folder, $"dark__Page__{name}.png"), PngBitmapEncoderOptions.Default);
    }
}
