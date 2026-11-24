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

/// <summary>
/// The plain ToolTip (ADR 0007, #236): the flyout's surface (FlyoutBackgroundThemeBrush), no border, the phone's family at
/// TextStyleMediumFontSize in PhoneForegroundBrush, with the phone's line, inside CornflowerPlainToolTipPadding. Simple's
/// is its own background, bordered, padded 4,2, so these fail without the theme.
/// </summary>
public class ToolTipTests
{
    private static (Scene Scene, Control Target) Open(ToolTip tip, ThemeVariant? variant = null)
    {
        var target = new Border { Width = 100, Height = 40, Background = Brushes.Transparent };
        ToolTip.SetTip(target, tip);
        var scene = new Scene(new Canvas { Children = { target } }, variant);
        ToolTip.SetIsOpen(target, true);
        Dispatcher.UIThread.RunJobs();
        return (scene, target);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_On_The_Flyouts_Surface(string variant)
    {
        var tip = new ToolTip { Content = "hint" };
        var (scene, target) = Open(tip, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        try
        {
            Assert.Equal(variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, tip.ActualThemeVariant);
            var surface = Resource<IBrush>(tip, "FlyoutBackgroundThemeBrush");
            Assert.Same(surface, tip.Background);
            Assert.Same(Resource<IBrush>(tip, "PhoneForegroundBrush"), tip.Foreground);
            Assert.Equal(Resource<FontFamily>(tip, "PhoneFontFamilyNormal"), tip.FontFamily);
            Assert.Equal(16, tip.FontSize);
            var padding = Resource<Thickness>(tip, "CornflowerPlainToolTipPadding");
            Assert.Equal(new Thickness(9.5, 4.8), padding);
            Assert.Equal(padding, tip.Padding);
            Assert.Equal(default, tip.BorderThickness);

            var presenter = Part<ContentPresenter>(tip, "PART_ContentPresenter");
            Assert.Same(surface, presenter.Background);
            Assert.Equal(padding, presenter.Padding);
            Assert.True(PhoneHeaderLine.GetIsEnabled(presenter));
            Assert.Equal("hint", Assert.IsAssignableFrom<TextBlock>(presenter.Child).Text);
        }
        finally
        {
            ToolTip.SetIsOpen(target, false);
            scene.Dispose();
        }
    }

    // What an app sets reaches the presenter.
    [AvaloniaFact]
    public void An_Apps_Border_Reaches_The_Presenter()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var tip = new ToolTip
        {
            Content = "hint", BorderBrush = brush, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(3),
            ContentTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "mine" }),
        };
        var (scene, target) = Open(tip);
        try
        {
            var presenter = Part<ContentPresenter>(tip, "PART_ContentPresenter");
            Assert.Same(brush, presenter.BorderBrush);
            Assert.Equal(new Thickness(2), presenter.BorderThickness);
            Assert.Equal(new CornerRadius(3), presenter.CornerRadius);
            Assert.Equal("mine", Assert.IsType<TextBlock>(presenter.Child).Text);
        }
        finally
        {
            ToolTip.SetIsOpen(target, false);
            scene.Dispose();
        }
    }
}
