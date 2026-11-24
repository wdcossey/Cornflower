using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Cornflower.Controls;
using Cornflower.Tests.Completeness;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain DropDownButton (ADR 0007, #235): Wp81's Button, by the phone's keys, with Lucide's ChevronDown after its
/// content. Simple's has none of the phone's sizes, tilt or chevron, so these fail without the theme.
/// </summary>
public class DropDownButtonTests
{
    private static DropDownButton Specimen() => new() { Content = "sort by", Flyout = new MenuFlyout() };

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_The_Phones_Button_With_A_Chevron(string variant)
    {
        var button = Specimen();
        using var scene = new Scene(new StackPanel { Children = { button } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Same(Resource<IBrush>(button, "PhoneForegroundBrush"), button.BorderBrush);
        Assert.Same(Resource<IBrush>(button, "PhoneForegroundBrush"), button.Foreground);
        Assert.Equal(Brushes.Transparent, button.Background);
        Assert.Equal(new Thickness(2.5), button.BorderThickness);
        Assert.Equal(Resource<FontFamily>(button, "PhoneFontFamilyNormal"), button.FontFamily);
        Assert.Equal(Resource<FontWeight>(button, "PhoneButtonFontWeight"), button.FontWeight);
        Assert.Equal(Resource<double>(button, "TextStyleLargeFontSize"), button.FontSize);
        Assert.Equal(Resource<Thickness>(button, "PhoneButtonContentPadding"), button.Padding);
        Assert.Equal(57.5, button.MinHeight);
        Assert.Equal(109, button.MinWidth);
        Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);
        Assert.Null(button.FocusAdorner);
        Assert.True(Tilt.GetIsEnabled(button));
        Assert.True(Tilt.GetIsTarget(Part<Panel>(button, "Grid")));
        Assert.Equal(Resource<Thickness>(button, "PhoneTouchTargetOverhang"), Part<Border>(button, "Border").Margin);

        var content = Part<ContentPresenter>(button, "PART_ContentPresenter");
        var chevron = Part<LucideIcon>(button, "Chevron");
        Assert.Equal(LucideIconKind.ChevronDown, chevron.Kind);
        Assert.Equal(Resource<Thickness>(button, "CornflowerPlainChevronMargin"), chevron.Margin);
        Assert.Same(button.Foreground, chevron.Foreground);
        var contentRight = content.TranslatePoint(new Point(content.Bounds.Width, 0), button)!.Value.X;
        var chevronLeft = chevron.TranslatePoint(default, button)!.Value.X;
        Assert.InRange(chevronLeft - contentRight, 9.5 - 0.5, double.MaxValue);
    }

    [AvaloniaFact]
    public void Pressed_It_Is_The_Accent_With_The_Content_And_Chevron_Over_It()
    {
        var button = Specimen();
        using var scene = new Scene(new StackPanel { Children = { button } });

        var at = Press(scene, button);
        Assert.True(button.IsPressed);
        Assert.Same(Resource<IBrush>(button, "ButtonPressedBackgroundThemeBrush"), Part<Border>(button, "Border").Background);
        var over = Resource<IBrush>(button, "ButtonPressedForegroundThemeBrush");
        Assert.Same(over, Part<ContentPresenter>(button, "PART_ContentPresenter").Foreground);
        Assert.Same(over, Part<LucideIcon>(button, "Chevron").Foreground);
        Release(scene, at);
    }

    [AvaloniaFact]
    public void Disabled_It_Is_Dimmed_As_The_Button_Is()
    {
        var button = Specimen();
        button.IsEnabled = false;
        using var scene = new Scene(new StackPanel { Children = { button } });

        var border = Part<Border>(button, "Border");
        Assert.Same(Resource<IBrush>(button, "ButtonDisabledBackgroundThemeBrush"), border.Background);
        Assert.Same(Resource<IBrush>(button, "ButtonDisabledBorderThemeBrush"), border.BorderBrush);
        var dim = Resource<IBrush>(button, "ButtonDisabledForegroundThemeBrush");
        Assert.Same(dim, Part<ContentPresenter>(button, "PART_ContentPresenter").Foreground);
        Assert.Same(dim, Part<LucideIcon>(button, "Chevron").Foreground);
    }

    // As a Button does, in a template from under Wp81 it takes the theme under Wp81 for its own type (HoleParts).
    [AvaloniaFact]
    public void In_A_Template_From_Under_Wp81_It_Takes_The_Theme_Under_Wp81()
    {
        var host = new HolePartsTests.AppControl();
        var template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new DropDownButton { Name = "Inner" });
        var underDropDown = new ControlTheme(typeof(DropDownButton));
        var under = new Styles
        {
            Resources =
            {
                [typeof(HolePartsTests.AppControl)] = new ControlTheme(typeof(HolePartsTests.AppControl)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } },
                [typeof(DropDownButton)] = underDropDown,
            },
        };
        using var scene = new Scene(host, themes: [under, new Wp81Theme()]);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(underDropDown, Part<DropDownButton>(host, "Inner").Theme);
    }
}
