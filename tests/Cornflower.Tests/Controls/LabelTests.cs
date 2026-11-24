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
/// The plain Label (ADR 0007, #236): the control header's look, ControlHeaderTextBlockStyle's (the phone's family at
/// TextStyleMediumFontSize, PhoneMidBrush), set on the Label so its content inherits it; access keys recognised, with
/// the phone's line; PhoneDisabledBrush disabled. Simple's sets none of these and pads 3, so these fail without the theme.
/// </summary>
public class LabelTests
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Label_Is_A_Control_Header(string variant)
    {
        // In an app's own font, so the family is the Label's, not the window's.
        var label = new Label { Content = "_name" };
        var host = new StackPanel { Children = { label } };
        host.SetValue(TextElement.FontFamilyProperty, new FontFamily("Courier New"));
        using var scene = new Scene(host, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Equal(Resource<FontFamily>(label, "PhoneFontFamilyNormal"), label.FontFamily);
        Assert.Equal(16, Resource<double>(label, "TextStyleMediumFontSize"));
        Assert.Equal(16, label.FontSize);
        var mid = Resource<IBrush>(label, "PhoneMidBrush");
        Assert.Same(mid, label.Foreground);
        Assert.Equal(default, label.Padding);

        var presenter = Part<ContentPresenter>(label, "PART_ContentPresenter");
        Assert.True(presenter.RecognizesAccessKey);
        var text = Assert.IsType<AccessText>(presenter.Child);
        Assert.Same(mid, text.Foreground);
        Assert.Equal(16, text.FontSize);
        Assert.True(PhoneContentLine.GetIsEnabled(presenter));
        Assert.Null(presenter.Background);
        Assert.Null(presenter.BorderBrush);
    }

    [AvaloniaFact]
    public void Disabled_It_Is_PhoneDisabledBrush()
    {
        var label = new Label { Content = "name", IsEnabled = false };
        using var scene = new Scene(new StackPanel { Children = { label } });

        Assert.Same(Resource<IBrush>(label, "PhoneDisabledBrush"), label.Foreground);
    }

    // What an app sets reaches the presenter.
    [AvaloniaFact]
    public void An_Apps_Values_Reach_The_Presenter()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var template = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "mine" });
        var label = new Label
        {
            Content = "name", Background = brush, BorderBrush = brush, BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(3), Padding = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Right, VerticalContentAlignment = VerticalAlignment.Bottom,
            ContentTemplate = template,
        };
        using var scene = new Scene(new StackPanel { Children = { label } });

        var presenter = Part<ContentPresenter>(label, "PART_ContentPresenter");
        Assert.Same(brush, presenter.Background);
        Assert.Same(brush, presenter.BorderBrush);
        Assert.Equal(new Thickness(2), presenter.BorderThickness);
        Assert.Equal(new CornerRadius(3), presenter.CornerRadius);
        Assert.Equal(new Thickness(4), presenter.Padding);
        Assert.Equal(HorizontalAlignment.Right, presenter.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, presenter.VerticalContentAlignment);
        Assert.Equal("mine", Assert.IsType<TextBlock>(presenter.Child).Text);
    }
}
