using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/GroupedLists/COMPARISON.md: the probe's grouped ListView and grouped GridView headless on the
/// 450 × 800 view at 2.4, each at rest and panned (the ListView 140, as the recording's, its header a pinned; the GridView
/// 100). Dark, as the recording. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~GroupedListFrames
/// </code>
/// </summary>
public class GroupedListFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var (grid, name, panned) in new[] { (false, "list", 140.0), (true, "grid", 100.0) })
        {
            using var page = new GroupedListTests.ProbePage(grid);
            using (var rest = page.Capture())
                rest.Save(Path.Combine(folder, $"dark__grouped-{name}__rest.png"), PngBitmapEncoderOptions.Default);
            page.ScrollTo(panned);
            using var moved = page.Capture();
            moved.Save(Path.Combine(folder, $"dark__grouped-{name}__panned.png"), PngBitmapEncoderOptions.Default);
        }
    }
}
