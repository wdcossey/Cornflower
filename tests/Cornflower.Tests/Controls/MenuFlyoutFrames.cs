using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Our side of docs/controls/MenuFlyout/COMPARISON.md: the reference app's stages (<c>tools/refapp/app/Popups.cs</c>,
/// <c>MenuStates</c> and <c>MenuAt</c>), the "open" Button and its menu, headless at the reference profile (450 × 800 at
/// 2.4, so 1080 × 1920 like the emulator's screenshots), dark and light, at rest, named as the reference app names its
/// screenshots. By hand: it does nothing unless <c>CORNFLOWER_FRAMES</c> names a folder.
/// <code>
/// CORNFLOWER_FRAMES=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~MenuFlyoutFrames
/// </code>
/// </summary>
public class MenuFlyoutFrames
{
    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_FRAMES") is not { } folder)
            return;

        Directory.CreateDirectory(folder);
        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            Shot(folder, variant, "states", 377.08, States());
            Shot(folder, variant, "long", 377.08, Enumerable.Range(1, MenuFlyoutReadings.LongItems).Select(i => new MenuItem { Header = $"item {i}" }));
            foreach (var (top, name) in MenuFlyoutReadings.Placements.Select(p => p.TargetTop).Zip(["at-300", "at-500", "at-640", "at-720"]))
                Shot(folder, variant, name, top, Enumerable.Range(1, 4).Select(i => new MenuItem { Header = $"item {i}" }));
        }
    }

    /// <summary>The <c>states</c> menu; the pressed item is shown at rest (the phone's was forced into <c>Pressed</c>).</summary>
    private static IEnumerable<Control> States() =>
    [
        new MenuItem { Header = "pressed item" },
        new MenuItem { Header = "toggle checked", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
        new MenuItem { Header = "toggle unchecked", ToggleType = MenuItemToggleType.CheckBox },
        new Separator(),
        new MenuItem { Header = "disabled item", IsEnabled = false },
        new MenuItem { Header = "last item" },
    ];

    private static void Shot(string folder, ThemeVariant variant, string name, double targetTop, IEnumerable<Control> items)
    {
        var dark = variant == ThemeVariant.Dark;
        using var stage = new MenuFlyoutTests.Stage(targetTop, variant: variant, menu: items,
            page: new SolidColorBrush(MenuFlyoutReadings.Background(dark)));
        stage.OpenAtRest();
        using var frame = stage.Scene.Capture();
        frame.Save(Path.Combine(folder, $"{variant.ToString().ToLowerInvariant()}__MenuFlyout__{name}__ours.png"), PngBitmapEncoderOptions.Default);
    }
}
