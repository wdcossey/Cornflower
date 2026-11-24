using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;
using Avalonia.Themes.Simple;

/// <summary>
/// The plain PathIcon (ADR 0007, #236): its Data in its Foreground, inherited, with no size of its own (Simple's fixes
/// one), scaled to a size the app gives it; in a template written for the theme under Wp81, that theme's (HoleParts).
/// </summary>
public class PathIconTests
{
    private static readonly Color Red = Color.FromUInt32(0xFFFF0000u);

    // A 10 by 20 bar, 2 in from the origin: drawn at its own size, the bar's bounds, without the room round it.
    private static PathIcon Bar() => new() { Data = Geometry.Parse("M2,2 H12 V22 H2 Z") };

    private static Color At(Scene scene, double x, double y)
    {
        using var frame = scene.Capture();
        return Frames.PixelAt(frame, (int)(x * Scene.ReferenceScaling), (int)(y * Scene.ReferenceScaling));
    }

    // Its geometry's size, in the foreground it inherits.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_Its_Datas_Size_In_Its_Foreground(string variant)
    {
        var icon = Bar();
        var host = new StackPanel { Children = { icon } };
        host.SetValue(TextElement.ForegroundProperty, new SolidColorBrush(Red));
        using var scene = new Scene(new Canvas { Children = { new Border { Margin = new Thickness(100), Child = host } } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Equal(new Size(10, 20), icon.Bounds.Size);
        Assert.Equal(Red, At(scene, 105, 110));
        Assert.Equal(Resource<ISolidColorBrush>(icon, "ApplicationPageBackgroundThemeBrush").Color, At(scene, 112, 110));
    }

    // Given a size, the geometry is scaled to fit it, its shape kept, in the middle; the app's Background behind it.
    [AvaloniaFact]
    public void An_Apps_Size_Scales_It_And_Its_Background_Is_Drawn()
    {
        var blue = Color.FromUInt32(0xFF0000FFu);
        var icon = Bar();
        icon.Width = 40;
        icon.Height = 40;
        icon.Foreground = new SolidColorBrush(Red);
        icon.Background = new SolidColorBrush(blue);
        using var scene = new Scene(new Canvas { Children = { new Border { Margin = new Thickness(100), Child = icon } } });

        Assert.Equal(blue, At(scene, 105, 120));
        Assert.Equal(Red, At(scene, 112, 120));
        Assert.Equal(Red, At(scene, 128, 120));
        Assert.Equal(blue, At(scene, 135, 120));
    }

    // Simple's DrawerPage holds a PathIcon with no size of its own: it keeps Simple's (HoleParts).
    [AvaloniaFact]
    public void In_A_Drawer_Page_It_Keeps_Simples()
    {
        var simple = new SimpleTheme();
        var page = new DrawerPage();
        using var scene = new Scene(page, themes: [simple, new Wp81Theme()]);

        Assert.True(simple.TryGetResource(typeof(PathIcon), ThemeVariant.Dark, out var simples));
        var icons = page.GetVisualDescendants().OfType<PathIcon>().ToList();
        Assert.NotEmpty(icons);
        Assert.All(icons, icon => Assert.Same(simples, icon.Theme));
    }
}
