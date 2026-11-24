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
/// The plain TabbedPage and CarouselPage (ADR 0007, #238). A TabbedPage is the plain TabControl, the Pivot's headers at
/// the top, over the page's background; a CarouselPage is Wp81's FlipView of pages. Simple's TabbedPage is a keyed
/// uniform tab bar (Auto: at the bottom on a phone) and its CarouselPage a Carousel on Avalonia's own panel, so these
/// fail without the themes.
/// </summary>
public class TabbedPageTests
{
    public static TheoryData<string> Variants => new("dark", "light");

    private static ThemeVariant VariantOf(string name) => name == "light" ? ThemeVariant.Light : ThemeVariant.Dark;

    private static ContentPage Page(string header) => new() { Header = header, Content = new TextBlock { Text = header + " page" } };

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_TabbedPage_Is_The_Plain_TabControl(string variant)
    {
        var mail = Page("mail");
        var calendar = Page("calendar");
        var page = new TabbedPage { Pages = new Avalonia.Collections.AvaloniaList<Page> { mail, calendar } };
        using var scene = new Scene(page, VariantOf(variant));

        Assert.Same(Resource<IBrush>(page, "ApplicationPageBackgroundThemeBrush"), page.Background);
        Assert.Same(Resource<IBrush>(page, "PhoneForegroundBrush"), page.Foreground);
        Assert.Same(Resource<FontFamily>(page, "PhoneFontFamilyNormal"), page.FontFamily);
        Assert.Same(page.Background, Assert.IsType<Panel>(page.GetVisualChildren().Single()).Background);
        Assert.Equal(TabPlacement.Top, page.TabPlacement);

        var tabs = Part<TabControl>(page, "PART_TabControl");
        Assert.Null(tabs.Theme);
        Assert.Null(tabs.ItemContainerTheme);
        Assert.Equal(Dock.Top, tabs.TabStripPlacement);
        Assert.Same(Resource<ControlTheme>(tabs, "Wp81ScrollViewerScrollBarlessTheme"), Part<ScrollViewer>(tabs, "PART_ScrollViewer").Theme);
        var first = (TabItem)tabs.ContainerFromIndex(0)!;
        var second = (TabItem)tabs.ContainerFromIndex(1)!;
        Assert.Null(first.Theme);
        Assert.Equal("calendar", second.Header);
        TabControlTests.AssertPivotHeader(first, selected: true);
        TabControlTests.AssertPivotHeader(second, selected: false);
        Assert.Same(mail, page.CurrentPage);
    }

    // A tap on a header shows its page; no swipe between pages (answer 8).
    [AvaloniaFact]
    public void A_Tap_On_A_Header_Shows_Its_Page()
    {
        var calendar = Page("calendar");
        var page = new TabbedPage { Pages = new Avalonia.Collections.AvaloniaList<Page> { Page("mail"), calendar } };
        using var scene = new Scene(page);
        var tabs = Part<TabControl>(page, "PART_TabControl");
        var second = (TabItem)tabs.ContainerFromIndex(1)!;

        Release(scene, Press(scene, second));
        Dispatcher.UIThread.RunJobs();

        Assert.Same(calendar, page.CurrentPage);
        TabControlTests.AssertPivotHeader(second, selected: true);
        Assert.False(page.IsGestureEnabled);
    }

    // An app's own TabPlacement wins over the theme's Top.
    [AvaloniaFact]
    public void An_Apps_TabPlacement_Wins()
    {
        var page = new TabbedPage { TabPlacement = TabPlacement.Bottom, Pages = new Avalonia.Collections.AvaloniaList<Page> { Page("mail") } };
        using var scene = new Scene(page);

        Assert.Equal(Dock.Bottom, Part<TabControl>(page, "PART_TabControl").TabStripPlacement);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_CarouselPage_Is_A_FlipView_Of_Pages(string variant)
    {
        var one = Page("one");
        var page = new CarouselPage { Pages = new Avalonia.Collections.AvaloniaList<Page> { one, Page("two") } };
        using var scene = new Scene(page, VariantOf(variant));

        Assert.Same(Resource<IBrush>(page, "ApplicationPageBackgroundThemeBrush"), page.Background);
        Assert.Same(Resource<IBrush>(page, "PhoneForegroundBrush"), page.Foreground);
        Assert.Same(Resource<FontFamily>(page, "PhoneFontFamilyNormal"), page.FontFamily);
        Assert.Same(page.Background, Assert.IsType<Panel>(page.GetVisualChildren().Single()).Background);

        var carousel = Part<Carousel>(page, "PART_Carousel");
        Assert.Null(carousel.Theme);
        Assert.True(PhoneFlipView.GetIsEnabled(carousel));
        Assert.IsType<PhoneFlipViewPanel>(carousel.ItemsPanelRoot);
        Assert.True(carousel.IsSwipeEnabled);
        Assert.Same(one, page.CurrentPage);
        Assert.True(one.IsVisible);
        Assert.Equal(carousel.Bounds.Width - 5, one.Bounds.Width, 3);  // the FlipView's 2.5 inset each side
    }
}
