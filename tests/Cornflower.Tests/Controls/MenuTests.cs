using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain Menu, a desktop menu bar (ADR 0007; <c>Controls/Menu.axaml</c>): headers in the body's type, each in
/// <c>Wp81PlainTopLevelMenuItemTheme</c>, whose drop-down is the plain MenuItem's on the MenuFlyout's surface. No
/// reference: these hold the plain rules.
/// </summary>
public class MenuTests
{
    public static TheoryData<string> Variants => new("dark", "light");

    private static ThemeVariant VariantOf(string name) => name == "light" ? ThemeVariant.Light : ThemeVariant.Dark;

    private sealed class Stage : IDisposable
    {
        public Stage(ThemeVariant? variant = null)
        {
            Clock = new TestClock();
            Open = new MenuItem { Header = "open" };
            Recent = new MenuItem { Header = "recent", Items = { new MenuItem { Header = "notes" } } };
            File = new MenuItem { Header = "_file", Items = { Open, Recent, new Separator(), new MenuItem { Header = "exit" } } };
            Edit = new MenuItem { Header = "edit", Items = { new MenuItem { Header = "copy" } } };
            Menu = new Menu { Items = { File, Edit }, VerticalAlignment = VerticalAlignment.Top };
            try
            {
                // An app's own type round the bar, so the bar's own is what the headers show.
                var page = new Panel { Children = { Menu } };
                page.SetValue(TextElement.FontFamilyProperty, new FontFamily("Courier New"));
                page.SetValue(TextElement.FontSizeProperty, 11d);
                page.SetValue(TextElement.ForegroundProperty, Brushes.Orange);
                Scene = new Scene(page, variant);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public Menu Menu { get; }

        public MenuItem File { get; }

        public MenuItem Open { get; }

        public MenuItem Recent { get; }

        public MenuItem Edit { get; }

        public Scene Scene { get; }

        public void Dispose()
        {
            Menu.Close();
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    private static IBrush? ForegroundOf(MenuItem item) =>
        PlainParts.Part<Border>(item, "ContentBorder").GetValue(TextElement.ForegroundProperty);

    // The bar: the body's type (rule 2), nothing drawn of its own (rule 5); its headers in the keyed top-level theme,
    // the tilt on ContentBorder, in both variants.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Its_Headers_Are_In_The_Bodys_Type_In_The_Top_Level_Theme(string variant)
    {
        using var stage = new Stage(VariantOf(variant));
        var menu = stage.Menu;

        Assert.Same(PlainParts.Resource<FontFamily>(menu, "PhoneFontFamilyNormal"), menu.FontFamily);
        Assert.Equal(PlainParts.Resource<double>(menu, "TextStyleLargeFontSize"), menu.FontSize);
        Assert.Null(menu.Background);
        var theme = PlainParts.Resource<ControlTheme>(menu, "Wp81PlainTopLevelMenuItemTheme");
        Assert.Same(theme, menu.ItemContainerTheme);
        Assert.Same(theme, stage.File.Theme);
        Assert.Equal(18.14, stage.File.FontSize);
        Assert.Equal(PlainParts.Resource<Thickness>(menu, "CornflowerPlainTopLevelMenuItemPadding"), stage.File.Padding);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)stage.File.Background!).Color);
        Assert.True(Tilt.GetIsEnabled(stage.File));
        var content = PlainParts.Part<Border>(stage.File, "ContentBorder");
        Assert.True(Tilt.GetIsTarget(content));
        Assert.Equal(stage.File.Padding, content.Padding);
        Assert.True(PlainParts.Part<ContentPresenter>(stage.File, "PART_HeaderPresenter").RecognizesAccessKey);
        // A row: the second header after the first.
        Assert.True(stage.Edit.TranslatePoint(default, menu)!.Value.X >= stage.File.Bounds.Width);
        Assert.NotSame(PlainParts.Resource<IBrush>(menu, "PhoneAccentBrush"), ForegroundOf(stage.File));
    }

    // A header's drop-down: below it, on the MenuFlyout's surface, with the plain MenuItem's items in the MenuFlyout's type.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Headers_Drop_Down_Opens_Below_It_On_The_MenuFlyouts_Surface(string variant)
    {
        using var stage = new Stage(VariantOf(variant));
        var at = PlainParts.Press(stage.Scene, stage.File);
        PlainParts.Release(stage.Scene, at);

        Assert.True(stage.File.IsSubMenuOpen);
        var surface = (Border)PlainParts.Part<Popup>(stage.File, "PART_Popup").Child!;
        Assert.Same(PlainParts.Resource<IBrush>(surface, "MenuFlyoutBackgroundThemeBrush"), surface.Background);
        Assert.Same(PlainParts.Resource<IBrush>(surface, "MenuFlyoutBorderThemeBrush"), surface.BorderBrush);
        Assert.Equal(new Thickness(1.5), surface.BorderThickness);
        var below = stage.File.TranslatePoint(new Point(0, stage.File.Bounds.Height), stage.Scene.Window)!.Value;
        var top = surface.TranslatePoint(default, stage.Scene.Window)!.Value;
        Assert.Equal(below.Y, top.Y, 1);
        Assert.Equal(below.X, top.X, 1);
        // Avalonia clears the bar's item theme from the items below: they are the plain MenuItem's, by type.
        Assert.Null(stage.Open.Theme);
        Assert.Equal(25.5, stage.Open.FontSize);
        // On the MenuFlyout's surface, the phone's foreground, not the bar's (the page's orange, which a header keeps).
        Assert.Same(PlainParts.Resource<IBrush>(stage.Open, "PhoneForegroundBrush"), stage.Open.Foreground);
        Assert.Same(Brushes.Orange, stage.Edit.Foreground);
        Assert.NotNull(PlainParts.Part<Border>(stage.Open, "ContentBorder"));
        // Open, the header is the accent (rule 3).
        Assert.Same(PlainParts.Resource<IBrush>(stage.File, "PhoneAccentBrush"), ForegroundOf(stage.File));
    }

    // A sub-menu of the drop-down opens to the right of its item, its top at the item's.
    [AvaloniaFact]
    public void A_Sub_Menu_Opens_To_The_Right_Of_Its_Item()
    {
        using var stage = new Stage();
        var at = PlainParts.Press(stage.Scene, stage.File);
        PlainParts.Release(stage.Scene, at);
        stage.Recent.IsSubMenuOpen = true;
        Dispatcher.UIThread.RunJobs();

        var surface = (Border)PlainParts.Part<Popup>(stage.Recent, "PART_Popup").Child!;
        var item = stage.Recent.TranslatePoint(new Point(stage.Recent.Bounds.Width, 0), stage.Scene.Window)!.Value;
        var top = surface.TranslatePoint(default, stage.Scene.Window)!.Value;
        Assert.Equal(item.X, top.X, 1);
        Assert.Equal(item.Y, top.Y, 1);
    }

    // Selected and not pressed (a pointer over it, the keyboard on it): the accent. Disabled: the disabled brush.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Selected_A_Header_Is_The_Accent_And_Disabled_The_Disabled_Brush(string variant)
    {
        using var stage = new Stage(VariantOf(variant));

        stage.Edit.IsSelected = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(PlainParts.Resource<IBrush>(stage.Edit, "PhoneAccentBrush"), ForegroundOf(stage.Edit));

        stage.File.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(PlainParts.Resource<IBrush>(stage.File, "MenuFlyoutItemDisabledForegroundThemeBrush"), ForegroundOf(stage.File));
    }
}
