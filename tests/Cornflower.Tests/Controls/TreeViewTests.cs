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
/// The plain TreeView and TreeViewItem (ADR 0007, #239), and the row they set for batch 9's tables
/// (Wp81PlainRowTheme): no box, the rule under each row at the phone's touch height, the body's type, a chevron column
/// that is the indent, the tilt on the row, the accent for a selected row's text. Simple's draws a bordered box, a
/// triangle in a 16 column and a filled selection, so these fail without the themes.
/// </summary>
public class TreeViewTests
{
    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static (TreeView Tree, TreeViewItem Parent, TreeViewItem Child, TreeViewItem Leaf) Tree(bool expanded = true)
    {
        var child = new TreeViewItem { Header = "child" };
        var parent = new TreeViewItem { Header = "parent", IsExpanded = expanded, Items = { child } };
        var leaf = new TreeViewItem { Header = "leaf" };
        return (new TreeView { Width = 300, Items = { parent, leaf } }, parent, child, leaf);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void The_Tree_Draws_No_Box_Over_The_Page(string variant)
    {
        var (tree, _, _, _) = Tree();
        using var scene = new Scene(new StackPanel { Children = { tree } }, Variant(variant));

        var border = Part<Border>(tree, "PART_Border");
        Assert.Null(border.BorderBrush);
        Assert.Equal(new Thickness(0), border.BorderThickness);
        Assert.Equal(new Thickness(0), tree.Padding);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(tree.Background).Color);
        Assert.Same(Resource<IBrush>(tree, "PhoneForegroundBrush"), tree.Foreground);
        Assert.Null(tree.FocusAdorner);
    }

    // The row: Wp81PlainRowTheme's rule (0,0,0,1.5 in PhoneLowBrush) at the phone's 57.5, round the header in the body's
    // type, centred, with the tilt on PART_Header inside the untilted row.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Row_Is_The_Plain_Row(string variant)
    {
        var (tree, parent, _, _) = Tree();
        using var scene = new Scene(new StackPanel { Children = { tree } }, Variant(variant));

        var row = Part<Border>(parent, "PART_Row");
        Assert.Same(Resource<ControlTheme>(parent, "Wp81PlainRowTheme"), row.Theme);
        Assert.Equal(new Thickness(0, 0, 0, 1.5), Resource<Thickness>(parent, "CornflowerPlainRuleThickness"));
        Assert.Equal(new Thickness(0, 0, 0, 1.5), row.BorderThickness);
        Assert.Same(Resource<IBrush>(parent, "PhoneLowBrush"), row.BorderBrush);
        Near(57.5, row.Bounds.Height);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(row.Background).Color);

        var header = Part<ContentPresenter>(parent, "PART_HeaderPresenter");
        Assert.Equal(18.14, header.FontSize);
        Assert.Equal(Resource<FontFamily>(parent, "PhoneFontFamilyNormal"), header.FontFamily);
        Assert.Same(Resource<IBrush>(parent, "PhoneForegroundBrush"), header.Foreground);
        Assert.Equal(VerticalAlignment.Center, header.VerticalAlignment);
        Assert.True(PhoneHeaderLine.GetIsEnabled(header));
        Assert.True(Assert.IsAssignableFrom<TextBlock>(header.Child).IsSet(PhoneTextLine.MaxHeightLineHeightProperty));

        Assert.True(Tilt.GetIsEnabled(parent));
        Assert.Same(Part<Grid>(parent, "PART_Header"), Tilt.TargetOf(parent));
        Assert.Null(parent.FocusAdorner);
    }

    // The row part on its own, with nothing bound over it (a table's row may bind less): it takes the finger across the row.
    [AvaloniaFact]
    public void The_Row_Part_Alone_Is_Transparent_With_The_Rule()
    {
        var row = new Border { Child = new TextBlock { Text = "row" } };
        using var scene = new Scene(new StackPanel { Children = { row } });
        row.Theme = Resource<ControlTheme>(row, "Wp81PlainRowTheme");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(row.Background).Color);
        Assert.Equal(new Thickness(0, 0, 0, 1.5), row.BorderThickness);
        Assert.Equal(57.5, row.MinHeight);
    }

    // Each level indents by the chevron's column, 38.5, so a child's chevron sits under its parent's header; a leaf
    // hides its chevron and keeps the column, so its header lines up with its siblings'.
    [AvaloniaFact]
    public void The_Chevron_Column_Is_The_Indent_And_A_Leaf_Keeps_It()
    {
        var (tree, parent, child, leaf) = Tree();
        using var scene = new Scene(new StackPanel { Children = { tree } });

        Assert.Equal(38.5, Resource<double>(parent, "CornflowerPlainTreeViewItemIndent"));
        var parentHeader = Part<ContentPresenter>(parent, "PART_HeaderPresenter");
        var childChevron = Part<ToggleButton>(child, "PART_ExpandCollapseChevron");
        Near(38.5, Part<Panel>(parent, "PART_ChevronColumn").Bounds.Width);
        Near(38.5, X(parentHeader, tree));
        Near(38.5, X(Part<Panel>(child, "PART_ChevronColumn"), tree));
        Near(77, X(Part<ContentPresenter>(child, "PART_HeaderPresenter"), tree));

        Assert.False(Part<ToggleButton>(leaf, "PART_ExpandCollapseChevron").IsVisible);
        Assert.False(childChevron.IsVisible);
        Near(38.5, X(Part<ContentPresenter>(leaf, "PART_HeaderPresenter"), tree));
    }

    // Layout rounds to the device pixel.
    private static void Near(double expected, double actual) =>
        Assert.InRange(actual, expected - 1 / Scene.ReferenceScaling, expected + 1 / Scene.ReferenceScaling);

    private static double X(Visual part, Visual to) => part.TranslatePoint(default, to)!.Value.X;

    // The chevron is a header toggle filling the row's height: ChevronRight shut, ChevronDown open; a tap opens the item
    // at once and shows its children.
    [AvaloniaFact]
    public void A_Tap_On_The_Chevron_Opens_The_Item()
    {
        var (tree, parent, child, _) = Tree(expanded: false);
        using var scene = new Scene(new StackPanel { Children = { tree } });

        var chevron = Part<ToggleButton>(parent, "PART_ExpandCollapseChevron");
        Assert.Same(Resource<ControlTheme>(parent, "Wp81PlainHeaderToggleButtonTheme"), chevron.Theme);
        Near(Part<Border>(parent, "PART_Row").Bounds.Height - 1.5, chevron.Bounds.Height);
        Assert.Equal(LucideIconKind.ChevronRight, Part<LucideIcon>(parent, "PART_Chevron").Kind);
        Assert.False(Part<ItemsPresenter>(parent, "PART_ItemsPresenter").IsVisible);

        Release(scene, Press(scene, chevron));

        Assert.True(parent.IsExpanded);
        Assert.Equal(LucideIconKind.ChevronDown, Part<LucideIcon>(parent, "PART_Chevron").Kind);
        Assert.True(Part<ItemsPresenter>(parent, "PART_ItemsPresenter").IsVisible);
        Assert.True(child.IsVisible);
    }

    // Selected, the row's text and chevron are the accent, with no fill; disabled, PhoneDisabledBrush. A child of a
    // selected item keeps its own foreground.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Selected_Its_Text_Is_The_Accent(string variant)
    {
        var (tree, parent, child, leaf) = Tree();
        leaf.IsEnabled = false;
        using var scene = new Scene(new StackPanel { Children = { tree } }, Variant(variant));

        Release(scene, Press(scene, Part<ContentPresenter>(parent, "PART_HeaderPresenter")));

        Assert.True(parent.IsSelected);
        var accent = Resource<IBrush>(parent, "PhoneAccentBrush");
        Assert.Same(accent, Part<ContentPresenter>(parent, "PART_HeaderPresenter").Foreground);
        Assert.Same(accent, Part<LucideIcon>(parent, "PART_Chevron").Foreground);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(Part<Border>(parent, "PART_Row").Background).Color);
        Assert.Same(Resource<IBrush>(child, "PhoneForegroundBrush"), Part<ContentPresenter>(child, "PART_HeaderPresenter").Foreground);
        Assert.Same(Resource<IBrush>(leaf, "PhoneDisabledBrush"), Part<ContentPresenter>(leaf, "PART_HeaderPresenter").Foreground);
    }

    // Only the nearest item tilts: a press on a child tilts the child's row and leaves its parent still.
    [AvaloniaFact]
    public void A_Press_On_A_Child_Tilts_The_Child_Alone()
    {
        var (tree, parent, child, _) = Tree();
        using var scene = new Scene(new StackPanel { Children = { tree } });

        var at = Press(scene, Part<ContentPresenter>(child, "PART_HeaderPresenter"));

        Assert.NotEqual(TiltState.Rest, Tilt.Current(child));
        Assert.Equal(TiltState.Rest, Tilt.Current(parent));
        Release(scene, at);
    }
}
