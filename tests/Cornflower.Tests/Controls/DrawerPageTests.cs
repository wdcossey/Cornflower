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
/// The plain DrawerPage (ADR 0007, #239): the page's background and type, the drawer on the flyout's surface (the plain
/// SplitView), Wp81's NavigationPage bar's look at 48, and its three drawer buttons in Wp81PlainHeaderToggleButtonTheme
/// with Lucide's Menu and the tilt, not the theme under Wp81's ToggleButton (HoleParts). Simple's bar is its own
/// background at 16 SemiBold, its buttons fall back to Simple's ToggleButton, and its icon is a PathIcon, so these fail
/// without the theme.
/// </summary>
public class DrawerPageTests
{
    private static readonly string[] Buttons = ["PART_PaneButton", "PART_BottomPaneButton", "PART_CompactPaneToggle"];

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_A_Page_With_The_Drawer_On_The_Flyouts_Surface(string variant)
    {
        var page = new DrawerPage { Header = "title", Drawer = new TextBlock { Text = "drawer" }, Content = new TextBlock { Text = "content" } };
        // In a host with an app's own font, so the page's family is its theme's, not the window's.
        var host = new Border { Child = page };
        host.SetValue(Avalonia.Controls.Documents.TextElement.FontFamilyProperty, new FontFamily("Courier New"));
        using var scene = new Scene(host, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Same(Resource<IBrush>(page, "ApplicationPageBackgroundThemeBrush"), page.Background);
        Assert.Same(Resource<IBrush>(page, "PhoneForegroundBrush"), page.Foreground);
        Assert.Equal(Resource<FontFamily>(page, "PhoneFontFamilyNormal"), page.FontFamily);
        var split = Part<SplitView>(page, "PART_SplitView");
        Assert.Same(Resource<IBrush>(page, "FlyoutBackgroundThemeBrush"), page.DrawerBackground);
        Assert.Same(page.DrawerBackground, split.PaneBackground);
        Assert.Null(split.Theme);   // the plain SplitView, by type
        Assert.NotNull(split.GetVisualDescendants().OfType<Rectangle>().FirstOrDefault(r => r.Name == "PART_PaneEdge"));
    }

    // The bar: PhoneChromeBrush, 48 tall, the Header in PhoneFontFamilySemiBold at 18.14 after a square drawer button.
    [AvaloniaFact]
    public void Its_Bar_Is_The_Navigation_Bars_Look()
    {
        var page = new DrawerPage { Header = "title", Content = new TextBlock { Text = "content" } };
        using var scene = new Scene(page);

        var bar = Part<Border>(page, "PART_TopBar");
        Assert.Same(Resource<IBrush>(page, "PhoneChromeBrush"), bar.Background);
        Assert.Equal(48, Resource<double>(page, "CornflowerPlainDrawerBarHeight"));
        Near(48, bar.Bounds.Height);
        var button = Part<ToggleButton>(page, "PART_PaneButton");
        Near(48, button.Bounds.Width);
        Near(48, button.Bounds.Height);
        var title = Part<ContentPresenter>(page, "PART_TitlePresenter");
        Assert.Equal(18.14, title.FontSize);
        Assert.Equal(Resource<FontFamily>(page, "PhoneFontFamilySemiBold"), title.FontFamily);
        Assert.Same(Resource<IBrush>(page, "PhoneForegroundBrush"), title.Foreground);
        Near(button.Bounds.Width + 12, title.TranslatePoint(default, bar)!.Value.X);
    }

    // With the drawer on the right, the title still stands 12 off the bar's left edge.
    [AvaloniaFact]
    public void With_The_Drawer_On_The_Right_Its_Title_Stands_Off_The_Edge()
    {
        var page = new DrawerPage { Header = "title", DrawerPlacement = DrawerPlacement.Right, Content = new TextBlock { Text = "content" } };
        using var scene = new Scene(page);

        var bar = Part<Border>(page, "PART_TopBar");
        Assert.Equal(new Thickness(12, 0), Resource<Thickness>(page, "CornflowerPlainDrawerTitleMargin"));
        Near(12, Part<ContentPresenter>(page, "PART_TitlePresenter").TranslatePoint(default, bar)!.Value.X);
        Near(bar.Bounds.Width - 48, Part<ToggleButton>(page, "PART_PaneButton").TranslatePoint(default, bar)!.Value.X);
    }

    private static void Near(double expected, double actual) =>
        Assert.InRange(actual, expected - 1 / Scene.ReferenceScaling, expected + 1 / Scene.ReferenceScaling);

    private static LucideIcon Menu(ToggleButton button) => Assert.IsType<Panel>(button.Content).Children.OfType<LucideIcon>().Single();

    // Its three drawer buttons wear the header toggle part, tilt, and draw Lucide's Menu: none takes the theme under
    // Wp81's ToggleButton, and no PathIcon is left.
    [AvaloniaFact]
    public void Its_Drawer_Buttons_Are_The_Header_Toggle_With_Lucides_Menu()
    {
        var page = new DrawerPage { Content = new TextBlock { Text = "content" } };
        using var scene = new Scene(page);

        var theme = Resource<ControlTheme>(page, "Wp81PlainHeaderToggleButtonTheme");
        foreach (var name in Buttons)
        {
            var button = Part<ToggleButton>(page, name);
            Assert.Same(theme, button.Theme);
            Assert.True(Tilt.GetIsEnabled(button));
            Assert.Equal(LucideIconKind.Menu, Menu(button).Kind);
        }

        Assert.Empty(page.GetVisualDescendants().OfType<PathIcon>());
    }

    // A tap on the drawer button opens the drawer, at once; a DrawerIcon takes the Menu's place.
    [AvaloniaFact]
    public void A_Tap_Opens_The_Drawer_And_A_DrawerIcon_Replaces_The_Menu()
    {
        var page = new DrawerPage { Drawer = new TextBlock { Text = "drawer" }, Content = new TextBlock { Text = "content" } };
        using var scene = new Scene(page);
        var button = Part<ToggleButton>(page, "PART_PaneButton");

        Release(scene, Press(scene, button));

        Assert.True(page.IsOpen);
        Assert.Equal(page.DrawerLength, Part<Panel>(Part<SplitView>(page, "PART_SplitView"), "PART_PaneRoot").Width);

        page.DrawerIcon = "icon";
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.False(Menu(button).IsVisible);
        Assert.True(Part<ContentPresenter>(page, "PART_PaneIconPresenter").IsVisible);
    }
}
