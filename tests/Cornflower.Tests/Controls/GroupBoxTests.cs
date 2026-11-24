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
/// The plain GroupBox (ADR 0007, #236): a heading in GroupHeaderTextBlockStyle's look (<c>Wp81PlainGroupHeaderTheme</c>)
/// 9.5 over the content, no box; collapsed with no Header, PhoneDisabledBrush disabled. Simple's draws a rounded box
/// round the content with the heading in its edge, so these fail without the theme.
/// </summary>
public class GroupBoxTests
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Its_Header_Is_Over_The_Content(string variant)
    {
        var control = new GroupBox { Header = "header", Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var header = Part<ContentPresenter>(control, "PART_HeaderPresenter");
        var content = Part<ContentPresenter>(control, "PART_ContentPresenter");
        Assert.Same(Resource<ControlTheme>(control, "Wp81PlainGroupHeaderTheme"), header.Theme);
        Assert.Same(Resource<IBrush>(control, "PhoneMidBrush"), header.Foreground);
        Assert.Equal(Resource<FontFamily>(control, "PhoneFontFamilySemiLight"), header.FontFamily);
        Assert.Equal(23.73, header.FontSize);
        Assert.Equal(PhoneCharacterSpacing.LetterSpacingOf(-3, 23.73), header.LetterSpacing, 6);
        Assert.True(PhoneHeaderLine.GetIsEnabled(header));
        Assert.Equal(-3 * 23.73 / 1000, Assert.IsAssignableFrom<TextBlock>(header.Child).LetterSpacing, 6);
        var margin = Resource<Thickness>(control, "CornflowerPlainGroupHeaderMargin");
        Assert.Equal(new Thickness(0, 0, 0, 9.5), margin);
        Assert.Equal(margin, header.Margin);
        Assert.True(header.IsVisible);
        Assert.InRange(content.Bounds.Top - header.Bounds.Bottom, 9.5 - 1 / Scene.ReferenceScaling, 9.5 + 1 / Scene.ReferenceScaling);
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
        var control = new GroupBox { Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        Assert.False(Part<ContentPresenter>(control, "PART_HeaderPresenter").IsVisible);
        Assert.Equal(0, Part<ContentPresenter>(control, "PART_ContentPresenter").Bounds.Top);
    }

    [AvaloniaFact]
    public void Disabled_Its_Header_Is_PhoneDisabledBrush()
    {
        var control = new GroupBox { Header = "header", Content = "content", IsEnabled = false };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        Assert.Same(Resource<IBrush>(control, "PhoneDisabledBrush"), Part<ContentPresenter>(control, "PART_HeaderPresenter").Foreground);
    }

    // The header part's name is the one Avalonia looks for, so a header that is a control is the control's logical child.
    [AvaloniaFact]
    public void A_Header_Control_Is_Its_Logical_Child()
    {
        var heading = new TextBlock { Text = "header" };
        var control = new GroupBox { Header = heading, Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { control } });

        Assert.Same(control, heading.Parent);
        Assert.Same(Part<ContentPresenter>(control, "PART_HeaderPresenter"), heading.GetVisualParent());
    }

    // What an app sets reaches the template.
    [AvaloniaFact]
    public void An_Apps_Values_Reach_The_Template()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var control = new GroupBox
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
        Assert.Equal(new Thickness(4), content.Margin);
        Assert.Equal(HorizontalAlignment.Right, content.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, content.VerticalContentAlignment);
        Assert.Equal("my content", Assert.IsType<TextBlock>(content.Child).Text);
        Assert.Equal("my header", Assert.IsType<TextBlock>(Part<ContentPresenter>(control, "PART_HeaderPresenter").Child).Text);
    }

    // No box: the page shows at the control's edges, where Simple draws its border, dark and light.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Draws_No_Box(string variant)
    {
        var control = new GroupBox { Header = "header", Content = new Border { Height = 60 }, Width = 200 };
        using var scene = new Scene(new Canvas { Children = { new StackPanel { Margin = new Thickness(100), Children = { control } } } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        using var frame = scene.Capture();
        var page = Resource<ISolidColorBrush>(control, "ApplicationPageBackgroundThemeBrush").Color;
        var middle = 100 + control.Bounds.Height / 2;
        Assert.Equal(page, Frames.PixelAt(frame, (int)(100.5 * Scene.ReferenceScaling), (int)(middle * Scene.ReferenceScaling)));
        Assert.Equal(page, Frames.PixelAt(frame, (int)(299.5 * Scene.ReferenceScaling), (int)(middle * Scene.ReferenceScaling)));
        Assert.Equal(page, Frames.PixelAt(frame, (int)(200 * Scene.ReferenceScaling), (int)((100 + control.Bounds.Height - 0.5) * Scene.ReferenceScaling)));
    }
}
