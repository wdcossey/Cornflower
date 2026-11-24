using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain Expander (ADR 0007, #239): a header in Wp81PlainHeaderToggleButtonTheme, at the phone's touch height and
/// tilted, with the heading in Wp81PlainGroupHeaderTheme and Lucide's chevron after it; the content shows at once; no
/// box, and no accent when open. Simple's is a bordered toggle with a triangle and a cross-fade, so these fail without
/// the theme.
/// </summary>
public class ExpanderTests
{
    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Its_Header_Is_A_Plain_Toggle_With_The_Group_Heading_And_A_Chevron(string variant)
    {
        var control = new Expander { Header = "header", Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 300, Children = { control } }, Variant(variant));

        var toggle = Part<ToggleButton>(control, "PART_toggle");
        Assert.Same(Resource<ControlTheme>(control, "Wp81PlainHeaderToggleButtonTheme"), toggle.Theme);
        Assert.Equal(57.5, toggle.MinHeight);
        Assert.Equal(57.5, toggle.Bounds.Height);
        Assert.Equal(300, toggle.Bounds.Width);
        Assert.True(Tilt.GetIsEnabled(toggle));
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(toggle.Background).Color);
        Assert.Equal(new Thickness(0), toggle.BorderThickness);

        var header = Part<ContentPresenter>(control, "PART_HeaderPresenter");
        Assert.Same(Resource<ControlTheme>(control, "Wp81PlainGroupHeaderTheme"), header.Theme);
        Assert.Equal(23.73, header.FontSize);
        Assert.Same(Resource<IBrush>(control, "PhoneMidBrush"), header.Foreground);

        var chevron = Part<LucideIcon>(control, "PART_Chevron");
        Assert.Equal(LucideIconKind.ChevronDown, chevron.Kind);
        Assert.Same(Resource<IBrush>(control, "PhoneMidBrush"), chevron.Foreground);
        Assert.Equal(Resource<Thickness>(control, "CornflowerPlainChevronMargin"), chevron.Margin);
        Assert.True(chevron.Bounds.Left > header.Bounds.Right - 1);
        Assert.False(Part<ContentPresenter>(control, "PART_ContentPresenter").IsVisible);
    }

    // A tap on the header opens it at once (no cross-fade: no ContentTransition), and the chevron turns; open, the header
    // draws no accent (rule 3), and a second tap shuts it.
    [AvaloniaFact]
    public void A_Tap_Opens_It_At_Once_With_No_Accent()
    {
        var control = new Expander { Header = "header", Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 300, Children = { control } });
        var toggle = Part<ToggleButton>(control, "PART_toggle");

        Release(scene, Press(scene, toggle));

        Assert.True(control.IsExpanded);
        Assert.Null(control.ContentTransition);
        var content = Part<ContentPresenter>(control, "PART_ContentPresenter");
        Assert.True(content.IsVisible);
        Assert.Equal("content", Assert.IsAssignableFrom<TextBlock>(content.Child).Text);
        Assert.True(PhoneHeaderLine.GetIsEnabled(content));
        Assert.Equal(LucideIconKind.ChevronUp, Part<LucideIcon>(control, "PART_Chevron").Kind);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(Part<Border>(toggle, "Border").Background).Color);
        Assert.Same(Resource<IBrush>(control, "PhoneMidBrush"), Part<ContentPresenter>(control, "PART_HeaderPresenter").Foreground);

        Release(scene, Press(scene, toggle));
        Assert.False(control.IsExpanded);
        Assert.False(content.IsVisible);
    }

    // The header sits on the side the content comes from, and its chevron points that way, then back once open.
    [AvaloniaTheory]
    [InlineData(ExpandDirection.Down, Dock.Top, LucideIconKind.ChevronDown, LucideIconKind.ChevronUp)]
    [InlineData(ExpandDirection.Up, Dock.Bottom, LucideIconKind.ChevronUp, LucideIconKind.ChevronDown)]
    [InlineData(ExpandDirection.Right, Dock.Left, LucideIconKind.ChevronRight, LucideIconKind.ChevronLeft)]
    [InlineData(ExpandDirection.Left, Dock.Right, LucideIconKind.ChevronLeft, LucideIconKind.ChevronRight)]
    public void Each_Direction_Places_The_Header_And_Points_The_Chevron(ExpandDirection direction, Dock dock, LucideIconKind shut, LucideIconKind open)
    {
        var control = new Expander { Header = "header", Content = "content", ExpandDirection = direction };
        using var scene = new Scene(new StackPanel { Width = 300, Children = { control } });

        Assert.Equal(dock, DockPanel.GetDock(Part<ToggleButton>(control, "PART_toggle")));
        Assert.Equal(shut, Part<LucideIcon>(control, "PART_Chevron").Kind);
        control.IsExpanded = true;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(open, Part<LucideIcon>(control, "PART_Chevron").Kind);
    }

    [AvaloniaFact]
    public void Disabled_Its_Heading_And_Chevron_Are_PhoneDisabledBrush()
    {
        var control = new Expander { Header = "header", Content = "content", IsEnabled = false };
        using var scene = new Scene(new StackPanel { Width = 300, Children = { control } });

        var disabled = Resource<IBrush>(control, "PhoneDisabledBrush");
        Assert.Same(disabled, Part<ContentPresenter>(control, "PART_HeaderPresenter").Foreground);
        Assert.Same(disabled, Part<LucideIcon>(control, "PART_Chevron").Foreground);
    }

    // No box at rest: the border is the app's, unset. The header part is the one Avalonia looks for, so a header
    // control is the Expander's logical child.
    [AvaloniaFact]
    public void It_Draws_No_Box_And_Its_Header_Is_Its_Logical_Child()
    {
        var heading = new TextBlock { Text = "header" };
        var control = new Expander { Header = heading, Content = "content" };
        using var scene = new Scene(new StackPanel { Width = 300, Children = { control } });

        var border = Part<Border>(control, "PART_Border");
        Assert.Null(border.Background);
        Assert.Null(border.BorderBrush);
        Assert.Same(control, heading.Parent);
        Assert.Same(Part<ContentPresenter>(control, "PART_HeaderPresenter"), heading.GetVisualParent());
    }

    // The header toggle part: disabled, its content is PhoneDisabledBrush (the drawer's and the tree's buttons draw
    // their glyph through it).
    [AvaloniaFact]
    public void The_Header_Toggle_Part_Dims_Its_Content_Disabled()
    {
        var toggle = new ToggleButton { Content = "t", IsEnabled = false };
        using var scene = new Scene(new StackPanel { Children = { toggle } });
        toggle.Theme = Resource<ControlTheme>(toggle, "Wp81PlainHeaderToggleButtonTheme");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.Same(Resource<IBrush>(toggle, "PhoneDisabledBrush"), Part<ContentPresenter>(toggle, "PART_ContentPresenter").Foreground);
        Assert.Equal(0, toggle.MinHeight);
    }
}
