using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/Hub/COMPARISON.md: the probes' Hub (the header "hub", four sections of 30 px text, below a
/// 60 px band) headless at the reference profile, at rest at the first section and at the last, dark, and at the first
/// in the light theme; and with a 1600 x 800 background image part way. It also writes <c>ink.txt</c>, the bounds of the
/// light pixels in the regions COMPARISON.md measures on the recording's frames, in view pixels. By hand: it does
/// nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~HubFrames
/// </code>
/// </summary>
public class HubFrames
{
    /// <summary>The regions, in view pixels of the window: the Hub's header, the left section's header and content, and the right edge's.</summary>
    private static readonly (string Name, Rect Region)[] Regions =
    [
        ("hub header", new Rect(0, 50, 450, 116.67)),
        ("section header, left", new Rect(0, 166.67, 250, 33.33)),
        ("content, left", new Rect(0, 208.33, 333.33, 66.67)),
        ("section header, right edge", new Rect(375, 166.67, 75, 33.33)),
        ("content, right edge", new Rect(375, 208.33, 75, 66.67)),
    ];

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        var ink = new List<string>();
        foreach (var (name, variant, section, image) in new[]
                 {
                     ("dark__Hub__four-sections__first", ThemeVariant.Dark, 0, false),
                     ("dark__Hub__four-sections__last", ThemeVariant.Dark, 3, false),
                     ("light__Hub__four-sections__first", ThemeVariant.Light, 0, false),
                     ("dark__Hub__four-sections__image-at-603", ThemeVariant.Dark, -1, true),
                 })
        {
            var hub = HubTests.Stage.Make(4);
            if (image)
                hub.Background = new ImageBrush(Stripes(1600, 800)) { Stretch = Stretch.Fill };
            using var stage = new HubTests.Stage(variant: variant, hub: hub);
            var root = (Panel)stage.Hub.Parent!;
            root.Bind(Panel.BackgroundProperty, root.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            if (section >= 0)
            {
                stage.Hub.ScrollToSection(stage.Section(section));
            }
            else
            {
                stage.Hub.Motion.BeginDrag();
                stage.Hub.Motion.Drag(603.562);
            }

            stage.Window.UpdateLayout();
            using var frame = new HubFramesScene(stage).Capture();
            frame.Save(Path.Combine(folder, name + "__ours.png"), PngBitmapEncoderOptions.Default);
            if (!image)
            {
                foreach (var (label, region) in Regions)
                    ink.Add(string.Create(CultureInfo.InvariantCulture, $"{name} {label}: {Ink(frame, region, variant == ThemeVariant.Dark)}"));
            }

            if (section < 0)
                stage.Hub.Motion.Release(0);
        }

        File.WriteAllLines(Path.Combine(folder, "ink.txt"), ink);
    }

    /// <summary>The bounds of the pixels lighter (dark theme) or darker (light) than mid grey in a region, in view pixels.</summary>
    private static string Ink(WriteableBitmap frame, Rect region, bool light)
    {
        using var buffer = frame.Lock();
        var scale = Scene.ReferenceScaling;
        int x0 = (int)(region.X * scale), x1 = (int)(region.Right * scale), y0 = (int)(region.Y * scale), y1 = (int)(region.Bottom * scale);
        var row = new int[buffer.Size.Width];
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        for (var y = y0; y < Math.Min(y1, buffer.Size.Height); y++)
        {
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
            for (var x = x0; x < Math.Min(x1, buffer.Size.Width); x++)
            {
                var p = row[x];
                var luma = ((p >> 16) & 0xFF) * 0.299 + ((p >> 8) & 0xFF) * 0.587 + (p & 0xFF) * 0.114;
                if (light ? luma <= 128 : luma >= 128) continue;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x + 1);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y + 1);
            }
        }

        return minX > maxX ? "none" : string.Create(CultureInfo.InvariantCulture,
            $"{minX / scale:F2}, {minY / scale:F2} to {maxX / scale:F2}, {maxY / scale:F2}");
    }

    /// <summary>The probe's picture: a hue left to right, a darker line every 50 px, a lighter one every 250.</summary>
    private static WriteableBitmap Stripes(int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
        var row = new int[width];
        for (var x = 0; x < width; x++)
        {
            var h = x * 6.0 / width;
            double C(double n) => 255 * (1 - Math.Max(0, Math.Min(Math.Min((n + h) % 6, 4 - (n + h) % 6), 1)));
            var scale = x % 250 == 0 ? 1.0 : x % 50 == 0 ? 0.15 : 0.45;
            row[x] = unchecked((int)(0xFF000000u | (uint)(C(5) * scale) << 16 | (uint)(C(3) * scale) << 8 | (uint)(C(1) * scale)));
        }

        using var buffer = bitmap.Lock();
        for (var y = 0; y < height; y++)
            Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, width);
        return bitmap;
    }

    /// <summary>The stage's window, captured as the scene captures it.</summary>
    private sealed class HubFramesScene(HubTests.Stage stage)
    {
        public WriteableBitmap Capture()
        {
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            return Avalonia.Headless.HeadlessWindowExtensions.CaptureRenderedFrame(stage.Window) ?? throw new InvalidOperationException("no frame");
        }
    }
}
