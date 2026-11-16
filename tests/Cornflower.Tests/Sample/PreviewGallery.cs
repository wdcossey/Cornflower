using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The pictures for <c>PREVIEW.md</c>: every page of the sample, in the sample's shell at the reference view (450 ×
/// 800, without the desktop's Back rail, as on a phone), dark and light, and the pages that show the accent in two
/// more of the phone's accents; <c>PREVIEW.md</c> and <c>README.md</c> show a chosen set of them, committed under
/// <c>docs/preview/</c>. Drawn headless with Skia, as every comparison frame is, so they are the theme's own pixels on
/// any OS. By hand, as the <c>*Frames</c> savers are: it does nothing unless <c>CORNFLOWER_PREVIEW</c> names a folder.
/// <code>
/// CORNFLOWER_PREVIEW=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~PreviewGallery
/// </code>
/// Each picture is <c>&lt;variant&gt;__&lt;page&gt;[__&lt;accent&gt;].png</c>, 900 × 1600.
/// </summary>
public class PreviewGallery
{
    /// <summary>
    /// The render scaling of the pictures, so 900 × 1600: sharp on a high-density screen at the 250 to 270 wide the
    /// Markdown shows them, and small enough to commit (about 55 KB each).
    /// </summary>
    private const double Scaling = 2;

    /// <summary>Other accents, of the phone's twenty, far from the default Cobalt, for the pages that show one.</summary>
    private static readonly string[] OtherAccents = ["Lime", "Magenta"];

    /// <summary>The pages whose accent shows at rest, drawn again in each of <see cref="OtherAccents"/>.</summary>
    private static readonly string[] AccentPages = ["toggleswitch", "slider", "progressbar"];

    /// <summary>The header of the switch some pages have that turns the page light; set to match the picture.</summary>
    private const string LightSwitchHeader = "light theme";

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_PREVIEW") is not { Length: > 0 } folder)
            return;
        if (!Path.IsPathFullyQualified(folder))
            throw new ArgumentException($"CORNFLOWER_PREVIEW must be an absolute folder; it is '{folder}'.");

        var missing = AccentPages.Where(title =>
            !SampleShell.Pages.Any(page => string.Equals(page.Title, title, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"No sample page is titled {string.Join(", ", missing)}: update AccentPages.");

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            var name = variant.ToString().ToLowerInvariant();
            Shoot(folder, $"{name}__catalogue", variant, accent: null, open: null);
            foreach (var page in SampleShell.Pages)
            {
                Shoot(folder, $"{name}__{FileName(page.Title)}", variant, accent: null, page);
                if (!AccentPages.Contains(page.Title, StringComparer.OrdinalIgnoreCase))
                    continue;
                foreach (var accent in OtherAccents)
                    Shoot(folder, $"{name}__{FileName(page.Title)}__{accent.ToLowerInvariant()}", variant, accent, page);
            }
        }
    }

    /// <summary>The shell, showing <paramref name="open"/> (the catalogue if null), at rest, saved as <paramref name="file"/>.</summary>
    private static void Shoot(string folder, string file, ThemeVariant variant, string? accent, SamplePageEntry? open)
    {
        using var clock = new TestClock();
        var shell = new SampleShell { HasBackRail = false, Report = _ => { } };
        using var scene = new Scene(shell, variant, scaling: Scaling);
        if (accent is not null)
        {
            if (!scene.Window.TryFindResource($"CornflowerAccent{accent}Color", variant, out var color) || color is not Color named)
                throw new InvalidOperationException($"The theme has no accent named {accent}.");
            scene.Window.Resources["SystemColorControlAccentColor"] = named;
        }

        if (open is not null)
            shell.Open(open);

        ShellTurns.Settle(clock);
        // A page with its own light switch says what the picture shows, rather than "Off" on a light page.
        if (variant == ThemeVariant.Light)
        {
            foreach (var toggle in shell.GetVisualDescendants().OfType<ToggleSwitch>())
            {
                if (Equals(ControlHeader.GetHeader(toggle), LightSwitchHeader))
                    toggle.IsChecked = true;
            }

            // The knob slides on an Avalonia transition (370 ms), timed by the render loop's own clock, not the
            // TestClock: draw frames over half a second of real time so it is at rest.
            for (var tick = 0; tick < 10; tick++)
            {
                Thread.Sleep(50);
                ShellTurns.Draw();
            }
        }

        using var frame = scene.Capture();
        frame.Save(Path.Combine(folder, file + ".png"), PngBitmapEncoderOptions.Default);
    }

    /// <summary>A page's title as a file name: lower case, a hyphen for anything but a letter or digit.</summary>
    private static string FileName(string title) =>
        string.Concat(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
}
