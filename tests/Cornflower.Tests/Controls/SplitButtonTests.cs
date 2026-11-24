using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain SplitButton and ToggleSplitButton (ADR 0007, #235): Wp81's Button look with two pressable parts in one
/// border, each in the borderless part theme (Wp81PlainGlyphButtonTheme), a 1.5 divider between them, the tilt on the
/// whole. Simple's has none of the phone's sizes, tilt, accent or chevron, so these fail without the theme.
/// </summary>
public class SplitButtonTests
{
    private static SplitButton Specimen(bool toggle = false, bool isChecked = false)
    {
        var flyout = new Flyout { Content = new TextBlock { Text = "more" } };
        return toggle
            ? new ToggleSplitButton { Content = "bold", IsChecked = isChecked, Flyout = flyout }
            : new SplitButton { Content = "reply", Flyout = flyout };
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_The_Phones_Button_In_Two_Parts(string variant)
    {
        var split = Specimen();
        using var scene = new Scene(new StackPanel { Children = { split } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var foreground = Resource<IBrush>(split, "PhoneForegroundBrush");
        Assert.Same(foreground, split.BorderBrush);
        Assert.Same(foreground, split.Foreground);
        Assert.Equal(Brushes.Transparent, split.Background);
        Assert.Equal(new Thickness(2.5), split.BorderThickness);
        Assert.Equal(Resource<FontFamily>(split, "PhoneFontFamilyNormal"), split.FontFamily);
        Assert.Equal(Resource<FontWeight>(split, "PhoneButtonFontWeight"), split.FontWeight);
        Assert.Equal(Resource<double>(split, "TextStyleLargeFontSize"), split.FontSize);
        Assert.Equal(Resource<Thickness>(split, "PhoneButtonContentPadding"), split.Padding);
        Assert.Equal(57.5, split.MinHeight);
        Assert.Equal(109, split.MinWidth);
        Assert.Equal(HorizontalAlignment.Left, split.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, split.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, split.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, split.VerticalContentAlignment);
        Assert.Null(split.FocusAdorner);
        Assert.True(Tilt.GetIsEnabled(split));
        Assert.True(Tilt.GetIsTarget(Part<Panel>(split, "Grid")));
        var border = Part<Border>(split, "Border");
        Assert.Equal(Resource<Thickness>(split, "PhoneTouchTargetOverhang"), border.Margin);

        var glyph = Resource<ControlTheme>(split, "Wp81PlainGlyphButtonTheme");
        var primary = Part<Button>(split, "PART_PrimaryButton");
        var secondary = Part<Button>(split, "PART_SecondaryButton");
        var divider = Part<Border>(split, "Divider");
        Assert.Same(glyph, primary.Theme);
        Assert.Same(glyph, secondary.Theme);
        Assert.Equal("reply", primary.Content);
        Assert.Equal(split.Padding, primary.Padding);
        Assert.Equal(LucideIconKind.ChevronDown, Assert.IsType<LucideIcon>(secondary.Content).Kind);
        Assert.InRange(divider.Bounds.Width, 1.5 - 1 / Scene.ReferenceScaling, 1.5 + 1 / Scene.ReferenceScaling);
        Assert.Equal(Resource<double>(split, "CornflowerPlainLineWidth"), divider.Width);
        Assert.Same(split.BorderBrush, divider.Background);
        // Left to right inside the border: the content, the divider, the chevron, filling it.
        Assert.True(primary.Bounds.Right <= divider.Bounds.X && divider.Bounds.Right <= secondary.Bounds.X);
        Assert.Equal(border.Bounds.Height - 5, primary.Bounds.Height, 0.5);
        Assert.Equal(border.Bounds.Height - 5, secondary.Bounds.Height, 0.5);
        Assert.False(Tilt.GetIsEnabled(primary));
    }

    // The part theme: nothing drawn at rest, the host's foreground, the glyph's padding, filling its slot, no adorner.
    [AvaloniaFact]
    public void Its_Parts_Draw_Nothing_At_Rest()
    {
        var split = Specimen();
        using var scene = new Scene(new StackPanel { Children = { split } });

        foreach (var part in new[] { Part<Button>(split, "PART_PrimaryButton"), Part<Button>(split, "PART_SecondaryButton") })
        {
            Assert.Equal(Brushes.Transparent, part.Background);
            Assert.Equal(Brushes.Transparent, Part<Border>(part, "Border").Background);
            Assert.Equal(default, part.BorderThickness);
            Assert.Same(split.Foreground, part.Foreground);
            Assert.Null(part.FocusAdorner);
            Assert.Equal(HorizontalAlignment.Stretch, part.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, part.VerticalAlignment);
        }
        var secondary = Part<Button>(split, "PART_SecondaryButton");
        Assert.Equal(Resource<Thickness>(split, "CornflowerPlainGlyphButtonPadding"), secondary.Padding);
        Assert.Equal(new Thickness(9.5, 0), secondary.Padding);
        Assert.Equal(HorizontalAlignment.Center, secondary.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, secondary.VerticalContentAlignment);
    }

    // Each part pressed is filled with the accent, its content over it, the other untouched; the primary clicks, the
    // secondary opens the flyout (Avalonia's, through the parts' names).
    [AvaloniaTheory]
    [InlineData("PART_PrimaryButton")]
    [InlineData("PART_SecondaryButton")]
    public void A_Part_Pressed_Is_The_Accent(string name)
    {
        var split = Specimen();
        var clicks = 0;
        split.Click += (_, _) => clicks++;
        using var scene = new Scene(new StackPanel { Children = { split } });
        var pressed = Part<Button>(split, name);
        var other = Part<Button>(split, name == "PART_PrimaryButton" ? "PART_SecondaryButton" : "PART_PrimaryButton");

        var at = Press(scene, pressed);
        Assert.Same(Resource<IBrush>(split, "PhoneAccentBrush"), Part<Border>(pressed, "Border").Background);
        Assert.Same(Resource<IBrush>(split, "PhoneButtonBasePressedForegroundBrush"), Part<ContentPresenter>(pressed, "PART_ContentPresenter").Foreground);
        Assert.Equal(Brushes.Transparent, Part<Border>(other, "Border").Background);
        Release(scene, at);

        Assert.Equal(Brushes.Transparent, Part<Border>(pressed, "Border").Background);
        Assert.Equal(name == "PART_PrimaryButton" ? 1 : 0, clicks);
        Assert.Equal(name == "PART_SecondaryButton", split.Flyout!.IsOpen);
        split.Flyout.Hide();
    }

    // ToggleSplitButton is themed as its base: checked, both parts are the accent with their content over it; the border
    // and divider stay. Unchecked, nothing is drawn.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Checked_Both_Parts_Are_The_Accent(string variant)
    {
        var split = Specimen(toggle: true, isChecked: true);
        using var scene = new Scene(new StackPanel { Children = { split } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var accent = Resource<IBrush>(split, "PhoneAccentBrush");
        var over = Resource<IBrush>(split, "PhoneButtonBasePressedForegroundBrush");
        foreach (var part in new[] { Part<Button>(split, "PART_PrimaryButton"), Part<Button>(split, "PART_SecondaryButton") })
        {
            Assert.Same(accent, Part<Border>(part, "Border").Background);
            Assert.Same(over, Part<ContentPresenter>(part, "PART_ContentPresenter").Foreground);
        }
        Assert.Same(Resource<IBrush>(split, "PhoneForegroundBrush"), Part<Border>(split, "Border").BorderBrush);

        ((ToggleSplitButton)split).IsChecked = false;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(Brushes.Transparent, Part<Border>(Part<Button>(split, "PART_PrimaryButton"), "Border").Background);
    }

    [AvaloniaFact]
    public void Disabled_The_Border_Divider_And_Content_Are_PhoneDisabledBrush()
    {
        var split = Specimen();
        split.IsEnabled = false;
        using var scene = new Scene(new StackPanel { Children = { split } });

        var dim = Resource<IBrush>(split, "PhoneDisabledBrush");
        Assert.Same(dim, Part<Border>(split, "Border").BorderBrush);
        Assert.Same(dim, Part<Border>(split, "Divider").Background);
        foreach (var part in new[] { Part<Button>(split, "PART_PrimaryButton"), Part<Button>(split, "PART_SecondaryButton") })
            Assert.Same(dim, Part<ContentPresenter>(part, "PART_ContentPresenter").Foreground);
    }
}
