using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain Separator (ADR 0007, #234): themed by type in <c>Wp81Theme</c>, a line across its slot in the line
/// thickness (<c>CornflowerPlainLineThickness</c>, 1.5) and <c>PhoneLowBrush</c>, with no margin of its own.
/// Simple's is 1 tall, indented 29, in its own brush, so these fail without the theme. Inside a MenuFlyout the phone's
/// keyed <c>Wp81MenuFlyoutSeparatorTheme</c> still applies (<c>MenuFlyoutTests</c>).
/// </summary>
public class SeparatorTests
{
    private static T Resource<T>(StyledElement element, string key) =>
        Assert.IsAssignableFrom<T>(element.FindResource(element.ActualThemeVariant, key));

    /// <summary>A length <paramref name="expected"/> long, as layout rounds it to the raw pixel grid (ADR 0003).</summary>
    private static void AssertRounded(double expected, double actual) =>
        Assert.InRange(actual, expected - 1 / Scene.ReferenceScaling, expected + 1 / Scene.ReferenceScaling);

    // The line: across the slot, 1.5 tall, the token's thickness and PhoneLowBrush, no margin, not focusable; drawn in
    // PhoneLowBrush over the page, dark and light.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Separator_Is_A_Line_In_The_Line_Thickness(string variant)
    {
        var separator = new Separator();
        var panel = new StackPanel { Width = 200, Margin = new Thickness(100), Children = { separator } };
        using var scene = new Scene(new Canvas { Children = { panel } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var thickness = Resource<Thickness>(separator, "CornflowerPlainLineThickness");
        Assert.Equal(new Thickness(1.5), thickness);
        Assert.Equal(thickness, separator.BorderThickness);
        var brush = Resource<ISolidColorBrush>(separator, "PhoneLowBrush");
        Assert.Same(brush, separator.BorderBrush);
        Assert.Equal(default, separator.Margin);
        Assert.False(separator.Focusable);
        Assert.Equal(200, separator.Bounds.Width);
        AssertRounded(1.5, separator.Bounds.Height);

        using var frame = scene.Capture();
        var page = Resource<ISolidColorBrush>(separator, "ApplicationPageBackgroundThemeBrush").Color;
        var line = brush.Color;
        byte Over(byte fore, byte back) => (byte)Math.Round((fore * line.A + back * (255 - line.A)) / 255.0);
        var expected = Color.FromRgb(Over(line.R, page.R), Over(line.G, page.G), Over(line.B, page.B));
        var drawn = Frames.PixelAt(frame, (int)(200 * Scene.ReferenceScaling), (int)((100 + 0.75) * Scene.ReferenceScaling));
        Assert.InRange(drawn.R, expected.R - 2, expected.R + 2);
        Assert.InRange(drawn.G, expected.G - 2, expected.G + 2);
        Assert.InRange(drawn.B, expected.B - 2, expected.B + 2);
        Assert.Equal(page, Frames.PixelAt(frame, (int)(200 * Scene.ReferenceScaling), (int)(104 * Scene.ReferenceScaling)));
    }

    // The line is the separator's BorderBrush, as tall as its BorderThickness's top: an app's change of either draws.
    [AvaloniaFact]
    public void An_Apps_BorderThickness_And_BorderBrush_Change_The_Line()
    {
        var mine = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var separator = new Separator { BorderThickness = new Thickness(0, 6, 0, 0), BorderBrush = mine };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { separator } });

        AssertRounded(6, separator.Bounds.Height);
        var line = Assert.IsType<Border>(Assert.Single(Avalonia.VisualTree.VisualExtensions.GetVisualChildren(separator)));
        Assert.Equal("PART_Line", line.Name);
        Assert.Same(mine, line.Background);
    }
}
