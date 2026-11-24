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
/// The plain HeaderedContentControl (ADR 0007, #236): the control header reused, its Header in
/// <c>Wp81HeaderContentPresenterTheme</c> 4.8 over the content, collapsed with no Header, PhoneDisabledBrush disabled;
/// nothing drawn at rest. Simple's header is in the control's own font and brush, padded 3, so these fail without it.
/// </summary>
public class HeaderedContentControlTests
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Its_Header_Is_Over_The_Content(string variant)
    {
        var control = new HeaderedContentControl { Header = "header", Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var header = Part<ContentPresenter>(control, "PART_HeaderPresenter");
        var content = Part<ContentPresenter>(control, "PART_ContentPresenter");
        Assert.Same(Resource<ControlTheme>(control, "Wp81HeaderContentPresenterTheme"), header.Theme);
        Assert.Same(Resource<IBrush>(control, "PhoneMidBrush"), header.Foreground);
        Assert.Equal(16, header.FontSize);
        Assert.Equal(Resource<FontFamily>(control, "PhoneFontFamilyNormal"), header.FontFamily);
        var margin = Resource<Thickness>(control, "TextControlHeaderMarginThemeThickness");
        Assert.Equal(new Thickness(0, 0, 0, 4.8), margin);
        Assert.Equal(margin, header.Margin);
        Assert.True(header.IsVisible);
        Assert.InRange(content.Bounds.Top - header.Bounds.Bottom, 4.8 - 1 / Scene.ReferenceScaling, 4.8 + 1 / Scene.ReferenceScaling);
        Assert.Equal("content", Assert.IsAssignableFrom<TextBlock>(content.Child).Text);
        Assert.True(PhoneHeaderLine.GetIsEnabled(content));
        var border = Part<Border>(control, "PART_Border");
        Assert.Null(border.Background);
        Assert.Null(border.BorderBrush);
    }

    // With no Header the header is collapsed, margin and all, and the content starts at the top.
    [AvaloniaFact]
    public void With_No_Header_The_Content_Is_At_The_Top()
    {
        var control = new HeaderedContentControl { Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        Assert.False(Part<ContentPresenter>(control, "PART_HeaderPresenter").IsVisible);
        Assert.Equal(0, Part<ContentPresenter>(control, "PART_ContentPresenter").Bounds.Top);
    }

    [AvaloniaFact]
    public void Disabled_Its_Header_Is_PhoneDisabledBrush()
    {
        var control = new HeaderedContentControl { Header = "header", Content = "content", IsEnabled = false };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        Assert.Same(Resource<IBrush>(control, "PhoneDisabledBrush"), Part<ContentPresenter>(control, "PART_HeaderPresenter").Foreground);
    }

    // The header part's name is the one Avalonia looks for, so a header that is a control is the control's logical child.
    [AvaloniaFact]
    public void A_Header_Control_Is_Its_Logical_Child()
    {
        var heading = new TextBlock { Text = "header" };
        var control = new HeaderedContentControl { Header = heading, Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        Assert.Same(control, heading.Parent);
        Assert.Same(Part<ContentPresenter>(control, "PART_HeaderPresenter"), heading.GetVisualParent());
    }

    // What an app sets reaches the template.
    [AvaloniaFact]
    public void An_Apps_Values_Reach_The_Template()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var control = new HeaderedContentControl
        {
            Header = "header", Content = "content", Background = brush, BorderBrush = brush,
            BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(3), Padding = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Right, VerticalContentAlignment = VerticalAlignment.Bottom,
            HeaderTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "my header" }),
            ContentTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "my content" }),
        };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        var border = Part<Border>(control, "PART_Border");
        Assert.Same(brush, border.Background);
        Assert.Same(brush, border.BorderBrush);
        Assert.Equal(new Thickness(2), border.BorderThickness);
        Assert.Equal(new CornerRadius(3), border.CornerRadius);
        var content = Part<ContentPresenter>(control, "PART_ContentPresenter");
        Assert.Equal(new Thickness(4), border.Padding);
        Assert.Equal(HorizontalAlignment.Right, content.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, content.VerticalContentAlignment);
        Assert.Equal("my content", Assert.IsType<TextBlock>(content.Child).Text);
        Assert.Equal("my header", Assert.IsType<TextBlock>(Part<ContentPresenter>(control, "PART_HeaderPresenter").Child).Text);
    }
}
