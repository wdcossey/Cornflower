using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain tab pieces (ADR 0007, #238): TabControl, TabItem, TabStrip and TabStripItem, the Pivot's headers without
/// its swipe (answer 8). A tab's header is the Pivot header's look, by the Pivot's keys; the row pans in Wp81's
/// scrollbar-less ScrollViewer; the content is inset as a PivotItem. Simple's tabs are 16 in ThemeForegroundLowBrush in a
/// WrapPanel with an accent fill selected, so these fail without the themes.
/// </summary>
public class TabControlTests
{
    public static TheoryData<string> Variants => new("dark", "light");

    private static ThemeVariant VariantOf(string name) => name == "light" ? ThemeVariant.Light : ThemeVariant.Dark;

    private static TabControl Tabs(params string[] headers)
    {
        var control = new TabControl();
        foreach (var header in headers)
            control.Items.Add(new TabItem { Header = header, Content = header + " content" });
        return control;
    }

    /// <summary>The Pivot header's look on a header item: its keys, by DynamicResource, so the Adaptive pack reaches it.</summary>
    internal static void AssertPivotHeader(TemplatedControl item, bool selected)
    {
        Assert.Equal(Resource<double>(item, "PivotHeaderItemFontSize"), item.FontSize);
        Assert.Equal(57, item.FontSize);
        Assert.Same(Resource<FontFamily>(item, "PivotHeaderItemFontFamily"), item.FontFamily);
        Assert.Equal(PhoneCharacterSpacing.LetterSpacingOf(-25, 57), item.LetterSpacing, 6);
        Assert.Equal(Resource<Thickness>(item, "PivotHeaderItemPadding"), item.Padding);
        var margin = Resource<Thickness>(item, "CornflowerPlainTabHeaderMargin");
        Assert.Equal(new Thickness(16, 0, 0, 0), margin);
        Assert.Equal(margin, item.Margin);
        Assert.True(Tilt.GetIsEnabled(item));
        var presenter = Part<ContentPresenter>(item, "PART_ContentPresenter");
        Assert.True(PhoneHeaderLine.GetIsEnabled(presenter));
        Assert.Equal(item.Padding, presenter.Margin);
        Assert.Same(Resource<IBrush>(item, selected ? "PivotHeaderForegroundSelectedBrush" : "PivotHeaderForegroundUnselectedBrush"), presenter.Foreground);
        Assert.Same(Resource<IBrush>(item, selected ? "PivotHeaderBackgroundSelectedBrush" : "PivotHeaderBackgroundUnselectedBrush"), item.Background);
        Assert.Same(item.Background, Part<Border>(item, "PART_LayoutRoot").Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Its_Tabs_Are_The_Pivots_Headers(string variant)
    {
        var control = Tabs("mail", "calendar");
        using var scene = new Scene(control, VariantOf(variant));

        var first = (TabItem)control.ContainerFromIndex(0)!;
        var second = (TabItem)control.ContainerFromIndex(1)!;
        Assert.Null(first.Theme);
        AssertPivotHeader(first, selected: true);
        AssertPivotHeader(second, selected: false);
        Assert.Equal("mail", Assert.IsAssignableFrom<TextBlock>(Part<ContentPresenter>(first, "PART_ContentPresenter").Child).Text);
        Assert.True(second.Bounds.Left >= first.Bounds.Right);
        Assert.Equal(first.Bounds.Top, second.Bounds.Top);
    }

    // A tap selects a tab: its header comes to the foreground and the rest dim, and its content shows.
    [AvaloniaFact]
    public void A_Tap_Selects_A_Tab()
    {
        var control = Tabs("mail", "calendar");
        using var scene = new Scene(control);
        var first = (TabItem)control.ContainerFromIndex(0)!;
        var second = (TabItem)control.ContainerFromIndex(1)!;

        Release(scene, Press(scene, second));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, control.SelectedIndex);
        AssertPivotHeader(first, selected: false);
        AssertPivotHeader(second, selected: true);
        Assert.Equal("calendar content", Part<ContentPresenter>(control, "PART_SelectedContentHost").Content);
    }

    [AvaloniaFact]
    public void Disabled_A_Tab_Is_PhoneDisabledBrush()
    {
        var control = Tabs("mail", "calendar");
        ((TabItem)control.Items[1]!).IsEnabled = false;
        using var scene = new Scene(control);

        var second = (TabItem)control.ContainerFromIndex(1)!;
        Assert.Same(Resource<IBrush>(second, "PhoneDisabledBrush"), Part<ContentPresenter>(second, "PART_ContentPresenter").Foreground);
    }

    // The row pans in Wp81's scrollbar-less ScrollViewer, and the content sits under it inset as a PivotItem.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Content_Is_Under_The_Row_Inset_As_A_PivotItem(string variant)
    {
        var control = Tabs("mail", "calendar");
        using var scene = new Scene(control, VariantOf(variant));

        var row = Part<ScrollViewer>(control, "PART_ScrollViewer");
        Assert.Same(Resource<ControlTheme>(control, "Wp81ScrollViewerScrollBarlessTheme"), row.Theme);
        Assert.Equal(ScrollBarVisibility.Hidden, row.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, row.VerticalScrollBarVisibility);
        Assert.Equal(Dock.Top, DockPanel.GetDock(row));
        var panel = Assert.IsType<StackPanel>(control.ItemsPanelRoot);
        Assert.Equal(Orientation.Horizontal, panel.Orientation);

        var inset = Resource<Thickness>(control, "PivotItemMargin");
        Assert.Equal(inset, control.Padding);
        var host = Part<ContentPresenter>(control, "PART_SelectedContentHost");
        Assert.Equal(inset, host.Margin);
        Assert.Equal(HorizontalAlignment.Stretch, host.HorizontalContentAlignment);
        var at = host.TranslatePoint(default, control)!.Value;
        const double pixel = 1 / Scene.ReferenceScaling;   // layout rounding
        Assert.InRange(at.Y - row.Bounds.Bottom, inset.Top - pixel, inset.Top + pixel);
        Assert.InRange(at.X, inset.Left - pixel, inset.Left + pixel);
        Assert.Equal(inset, Part<ContentPresenter>(control, "PART_SelectedContentHost2").Margin);
        Assert.Null(control.Background);
    }

    // A header past the edge is brought into view when selected, and the row can pan to it.
    [AvaloniaFact]
    public void A_Header_Past_The_Edge_Comes_Into_View()
    {
        var control = Tabs("mail", "calendar", "people", "photos", "music");
        using var scene = new Scene(control);
        var row = Part<ScrollViewer>(control, "PART_ScrollViewer");
        Assert.True(row.Extent.Width > row.Viewport.Width);
        Assert.Equal(0, row.Offset.X);

        control.SelectedIndex = 4;
        Dispatcher.UIThread.RunJobs();

        Assert.True(row.Offset.X > 0);
        var last = control.ContainerFromIndex(4)!;
        var right = last.TranslatePoint(new Point(last.Bounds.Width, 0), row)!.Value.X;
        Assert.InRange(right, 0, row.Viewport.Width + 0.5);
    }

    [AvaloniaTheory]
    [InlineData(Dock.Left)]
    [InlineData(Dock.Right)]
    public void At_The_Side_The_Headers_Stack_And_Pan_Up_And_Down(Dock placement)
    {
        var control = Tabs("mail", "calendar");
        control.TabStripPlacement = placement;
        using var scene = new Scene(control);

        var row = Part<ScrollViewer>(control, "PART_ScrollViewer");
        Assert.Equal(placement, DockPanel.GetDock(row));
        Assert.Equal(ScrollBarVisibility.Disabled, row.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Hidden, row.VerticalScrollBarVisibility);
        Assert.Equal(Orientation.Vertical, Assert.IsType<StackPanel>(control.ItemsPanelRoot).Orientation);
        var first = control.ContainerFromIndex(0)!;
        var second = control.ContainerFromIndex(1)!;
        Assert.True(second.Bounds.Top >= first.Bounds.Bottom);
    }

    [AvaloniaFact]
    public void At_The_Bottom_The_Row_Is_Under_The_Content()
    {
        var control = Tabs("mail", "calendar");
        control.TabStripPlacement = Dock.Bottom;
        using var scene = new Scene(control);

        var row = Part<ScrollViewer>(control, "PART_ScrollViewer");
        Assert.Equal(Dock.Bottom, DockPanel.GetDock(row));
        Assert.Equal(Orientation.Horizontal, Assert.IsType<StackPanel>(control.ItemsPanelRoot).Orientation);
        var host = Part<ContentPresenter>(control, "PART_SelectedContentHost");
        Assert.True(row.Bounds.Top >= host.TranslatePoint(new Point(0, host.Bounds.Height), control)!.Value.Y);
    }

    // What an app sets reaches the template.
    [AvaloniaFact]
    public void An_Apps_Values_Reach_The_Template()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var control = Tabs("mail");
        control.Background = brush;
        control.BorderBrush = brush;
        control.BorderThickness = new Thickness(2);
        control.CornerRadius = new CornerRadius(3);
        control.Padding = new Thickness(4);
        var item = (TabItem)control.Items[0]!;
        item.Background = brush;
        using var scene = new Scene(control);

        var border = Assert.IsType<Border>(control.GetVisualChildren().Single());
        Assert.Same(brush, border.Background);
        Assert.Same(brush, border.BorderBrush);
        Assert.Equal(new Thickness(2), border.BorderThickness);
        Assert.Equal(new CornerRadius(3), border.CornerRadius);
        Assert.Equal(new Thickness(4), Part<ContentPresenter>(control, "PART_SelectedContentHost").Margin);
        Assert.Same(brush, Part<Border>((TabItem)control.ContainerFromIndex(0)!, "PART_LayoutRoot").Background);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_TabStrip_Is_The_Pivots_Header_Row(string variant)
    {
        var strip = new TabStrip { Items = { new TabStripItem { Content = "all" }, new TabStripItem { Content = "unread" } }, SelectedIndex = 0 };
        using var scene = new Scene(new StackPanel { Children = { strip } }, VariantOf(variant));

        var row = Part<ScrollViewer>(strip, "PART_ScrollViewer");
        Assert.Same(Resource<ControlTheme>(strip, "Wp81ScrollViewerScrollBarlessTheme"), row.Theme);
        Assert.Equal(ScrollBarVisibility.Hidden, row.HorizontalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, row.VerticalScrollBarVisibility);
        Assert.Equal(Orientation.Horizontal, Assert.IsType<StackPanel>(strip.ItemsPanelRoot).Orientation);
        var first = (TabStripItem)strip.ContainerFromIndex(0)!;
        var second = (TabStripItem)strip.ContainerFromIndex(1)!;
        Assert.Null(first.Theme);
        AssertPivotHeader(first, selected: true);
        AssertPivotHeader(second, selected: false);
        Assert.Equal("unread", Assert.IsAssignableFrom<TextBlock>(Part<ContentPresenter>(second, "PART_ContentPresenter").Child).Text);

        Release(scene, Press(scene, second));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, strip.SelectedIndex);
        AssertPivotHeader(first, selected: false);
        AssertPivotHeader(second, selected: true);
    }

    [AvaloniaFact]
    public void Disabled_A_TabStripItem_Is_PhoneDisabledBrush()
    {
        var item = new TabStripItem { Content = "unread", IsEnabled = false };
        using var scene = new Scene(new TabStrip { Items = { new TabStripItem { Content = "all" }, item } });

        Assert.Same(Resource<IBrush>(item, "PhoneDisabledBrush"), Part<ContentPresenter>(item, "PART_ContentPresenter").Foreground);
    }

    [AvaloniaFact]
    public void A_TabStrips_Background_And_Padding_Reach_Its_Row()
    {
        var brush = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        var strip = new TabStrip { Background = brush, Padding = new Thickness(4), Items = { new TabStripItem { Content = "all" } } };
        using var scene = new Scene(new StackPanel { Children = { strip } });

        var row = Part<ScrollViewer>(strip, "PART_ScrollViewer");
        Assert.Same(brush, row.Background);
        Assert.Equal(new Thickness(4), row.Padding);
    }
}
