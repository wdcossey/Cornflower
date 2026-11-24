using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Path = Avalonia.Controls.Shapes.Path;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain MenuItem (ADR 0007; <c>Controls/MenuItem.axaml</c>), from Wp81's MenuFlyoutItem, with an icon, a check,
/// gesture text and a sub-menu on the MenuFlyout's surface (<c>Controls/PlainSubMenu.axaml</c>). No reference: these hold
/// the plain rules.
/// </summary>
public class MenuItemTests
{
    public static TheoryData<string> Variants => new("dark", "light");

    private static ThemeVariant VariantOf(string name) => name == "light" ? ThemeVariant.Light : ThemeVariant.Dark;

    /// <summary>A ContextMenu holding <paramref name="items"/>, open at rest over a page.</summary>
    private sealed class Stage : IDisposable
    {
        public Stage(ThemeVariant? variant = null, params object[] items)
        {
            Clock = new TestClock();
            Menu = new ContextMenu();
            foreach (var item in items)
                Menu.Items.Add(item);
            Target = new Button { Content = "target", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, ContextMenu = Menu };
            try
            {
                Scene = new Scene(new Panel { Children = { Target } }, variant);
                Menu.Open(Target);
                Dispatcher.UIThread.RunJobs();
                Clock.At(Clock.Now.TotalMilliseconds + 600);
                Dispatcher.UIThread.RunJobs();
            }
            catch
            {
                Clock.Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public ContextMenu Menu { get; }

        public Button Target { get; }

        public Scene Scene { get; }

        public void Dispose()
        {
            Menu.Close();
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    private static T Part<T>(MenuItem item, string name) where T : Control => PlainParts.Part<T>(item, name);

    /// <summary>Within a raw pixel at the reference scaling: layout rounds to the device's pixels.</summary>
    private static void Near(double expected, double actual) =>
        Assert.InRange(actual, expected - 1 / Scene.ReferenceScaling - 0.001, expected + 1 / Scene.ReferenceScaling + 0.001);

    private static IBrush? ForegroundOf(MenuItem item) => Part<Border>(item, "ContentBorder").GetValue(TextElement.ForegroundProperty);

    private static ControlTheme ThemeOf(Scene scene) => Assert.IsType<ControlTheme>(scene.Window.FindResource(typeof(MenuItem)));

    // By type: the MenuFlyout item's setters and parts, the tilt on ContentBorder inside an untilted root, in both variants.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void It_Is_The_MenuFlyouts_Item_By_Type(string variant)
    {
        var item = new MenuItem { Header = "copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        using var stage = new Stage(VariantOf(variant), item);

        Assert.Null(item.Theme);
        var template = ThemeOf(stage.Scene).Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value;
        Assert.Same(template, item.Template);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)item.Background!).Color);
        Assert.Equal(PlainParts.Resource<Thickness>(item, "MenuFlyoutItemThemePadding"), item.Padding);
        // The MenuFlyout's type, on the item itself.
        Assert.Same(PlainParts.Resource<FontFamily>(item, "PhoneFontFamilySemiLight"), item.FontFamily);
        Assert.Equal(25.5, item.FontSize);
        Assert.Equal(-15 * 25.5 / 1000, item.LetterSpacing, 6);
        Assert.Null(item.FocusAdorner);
        Assert.True(Tilt.GetIsEnabled(item));
        var content = Part<Border>(item, "ContentBorder");
        Assert.True(Tilt.GetIsTarget(content));
        Assert.Equal(item.Padding, content.Padding);
        var header = Part<ContentPresenter>(item, "PART_HeaderPresenter");
        Assert.True(header.RecognizesAccessKey);
        Assert.Equal(HorizontalAlignment.Left, header.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, header.VerticalAlignment);
        // The gesture text in the mid brush, of the item's variant.
        var gesture = Part<TextBlock>(item, "PART_InputGestureText");
        Assert.True(gesture.IsVisible);
        Assert.False(string.IsNullOrEmpty(gesture.Text));
        Assert.Same(PlainParts.Resource<IBrush>(item, "PhoneMidBrush"), gesture.Foreground);
        Assert.Equal(PlainParts.Resource<Thickness>(item, "CornflowerPlainMenuItemGestureMargin"), gesture.Margin);
    }

    [AvaloniaFact]
    public void With_No_Gesture_There_Is_No_Gesture_Text()
    {
        var item = new MenuItem { Header = "copy" };
        using var stage = new Stage(null, item);

        Assert.False(Part<TextBlock>(item, "PART_InputGestureText").IsVisible);
    }

    // The icon column is there only for an icon or a check, 20 square, 9.5 before the header.
    [AvaloniaFact]
    public void The_Icon_Column_Shows_For_An_Icon_Or_A_Check_Only()
    {
        var plain = new MenuItem { Header = "plain" };
        var icon = new MenuItem { Header = "icon", Icon = new LucideIcon { Kind = LucideIconKind.Copy } };
        var check = new MenuItem { Header = "check", ToggleType = MenuItemToggleType.CheckBox };
        var radio = new MenuItem { Header = "radio", ToggleType = MenuItemToggleType.Radio };
        using var stage = new Stage(null, plain, icon, check, radio);

        Assert.False(Part<Panel>(plain, "PART_IconColumn").IsVisible);
        var column = Part<Panel>(icon, "PART_IconColumn");
        Assert.True(column.IsVisible);
        Assert.Equal(new Size(20, 20), column.Bounds.Size);
        Assert.Equal(PlainParts.Resource<Thickness>(icon, "CornflowerPlainMenuItemIconMargin"), column.Margin);
        Assert.True(Part<ContentPresenter>(icon, "PART_IconPresenter").IsVisible);
        Assert.False(Part<Path>(icon, "PART_Check").IsVisible);

        Assert.True(Part<Panel>(check, "PART_IconColumn").IsVisible);
        Assert.False(Part<ContentPresenter>(check, "PART_IconPresenter").IsVisible);
        Assert.False(Part<Path>(check, "PART_Check").IsVisible);
        Assert.True(Part<Panel>(radio, "PART_IconColumn").IsVisible);
        Assert.False(Part<Ellipse>(radio, "PART_Radio").IsVisible);
    }

    // Checked: the theme's check mark, or a dot, in the accent (rule 3).
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Checked_It_Shows_The_Check_Or_The_Dot_In_The_Accent(string variant)
    {
        var check = new MenuItem { Header = "check", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
        var radio = new MenuItem { Header = "radio", ToggleType = MenuItemToggleType.Radio, IsChecked = true };
        using var stage = new Stage(VariantOf(variant), check, radio);

        var mark = Part<Path>(check, "PART_Check");
        Assert.True(mark.IsVisible);
        Assert.Same(PlainParts.Resource<Geometry>(check, "Wp81.Glyph.CheckMark"), mark.Data);
        Assert.Same(PlainParts.Resource<IBrush>(check, "PhoneAccentBrush"), mark.Fill);
        Assert.True(mark.Bounds.Width > 15 && mark.Bounds.Width <= 20);
        var dot = Part<Ellipse>(radio, "PART_Radio");
        Assert.True(dot.IsVisible);
        Assert.Same(PlainParts.Resource<IBrush>(radio, "PhoneAccentBrush"), dot.Fill);
        Assert.Equal(new Size(10, 10), dot.Bounds.Size);
    }

    // One icon column for the whole menu: an item without an icon has its header where one with an icon has.
    [AvaloniaFact]
    public void Every_Item_Of_A_Menu_Has_Its_Header_In_One_Place()
    {
        var plain = new MenuItem { Header = "plain" };
        var icon = new MenuItem { Header = "icon", Icon = new LucideIcon { Kind = LucideIconKind.Copy } };
        using var stage = new Stage(null, plain, icon);

        double X(MenuItem item) => Part<ContentPresenter>(item, "PART_HeaderPresenter").TranslatePoint(default, stage.Menu)!.Value.X;
        Assert.Equal(X(icon), X(plain), 3);
        Assert.True(X(plain) >= 19 + 20 + 9.5 - 0.01);
    }

    // A sub-menu: Lucide's ChevronRight, and a popup on the MenuFlyout's surface in the MenuFlyout's type.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Sub_Menu_Has_A_Chevron_And_Opens_On_The_MenuFlyouts_Surface(string variant)
    {
        var child = new MenuItem { Header = "child" };
        var parent = new MenuItem { Header = "parent", Items = { child } };
        var leaf = new MenuItem { Header = "leaf" };
        using var stage = new Stage(VariantOf(variant), parent, leaf);

        var chevron = Part<LucideIcon>(parent, "PART_Chevron");
        Assert.True(chevron.IsVisible);
        Assert.Equal(LucideIconKind.ChevronRight, chevron.Kind);
        Assert.False(Part<LucideIcon>(leaf, "PART_Chevron").IsVisible);

        parent.IsSubMenuOpen = true;
        Dispatcher.UIThread.RunJobs();
        var surface = (Border)Part<Popup>(parent, "PART_Popup").Child!;
        Assert.Same(PlainParts.Resource<IBrush>(surface, "MenuFlyoutBackgroundThemeBrush"), surface.Background);
        Assert.Same(PlainParts.Resource<IBrush>(surface, "MenuFlyoutBorderThemeBrush"), surface.BorderBrush);
        Assert.Equal(new Thickness(1.5), surface.BorderThickness);
        Assert.Equal(PlainParts.Resource<Thickness>(surface, "CornflowerPlainSubMenuPadding"), surface.Padding);
        Assert.True(child.IsVisible);
        Assert.Null(child.Theme);
        Assert.Equal(25.5, child.FontSize);
        Assert.Equal(-15 * 25.5 / 1000, child.LetterSpacing, 6);
        Assert.Same(PlainParts.Resource<FontFamily>(child, "PhoneFontFamilySemiLight"), child.FontFamily);
        Assert.NotNull(surface.GetVisualDescendants().OfType<ScrollViewer>().SingleOrDefault());
        parent.IsSubMenuOpen = false;
    }

    // In a sub-menu, a Separator is the plain line, indented with the items' text by the sub-menu's 19 sides.
    [AvaloniaFact]
    public void A_Separator_In_A_Sub_Menu_Is_Indented_With_The_Items_Text()
    {
        var separator = new Separator();
        var child = new MenuItem { Header = "child" };
        var parent = new MenuItem { Header = "parent", Items = { child, separator, new MenuItem { Header = "other" } } };
        using var stage = new Stage(null, parent);
        parent.IsSubMenuOpen = true;
        Dispatcher.UIThread.RunJobs();

        var surface = (Border)Part<Popup>(parent, "PART_Popup").Child!;
        var header = Part<ContentPresenter>(child, "PART_HeaderPresenter");
        Near(header.TranslatePoint(default, surface)!.Value.X, separator.TranslatePoint(default, surface)!.Value.X);
        Near(19 + 1.5, separator.TranslatePoint(default, surface)!.Value.X);
        Near(1.5, separator.Bounds.Height);
        Near(child.Bounds.Width, separator.Bounds.Width);
        parent.IsSubMenuOpen = false;
    }

    // Selected (the pointer or the keyboard on it), not pressed: the accent (rule 3). Pressed: the tilt alone.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Selected_It_Is_The_Accent_And_Pressed_Only_Tilts(string variant)
    {
        var item = new MenuItem { Header = "copy" };
        var other = new MenuItem { Header = "paste" };
        using var stage = new Stage(VariantOf(variant), item, other);
        Assert.NotSame(PlainParts.Resource<IBrush>(item, "PhoneAccentBrush"), ForegroundOf(item));

        item.IsSelected = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(PlainParts.Resource<IBrush>(item, "PhoneAccentBrush"), ForegroundOf(item));

        var at = PlainParts.Press(stage.Scene, other);
        stage.Clock.At(stage.Clock.Now.TotalMilliseconds + 100);
        Dispatcher.UIThread.RunJobs();
        Assert.True(other.IsSelected);
        Assert.NotSame(PlainParts.Resource<IBrush>(other, "PhoneAccentBrush"), ForegroundOf(other));
        Assert.NotEqual(TiltState.Rest, Tilt.Current(other));
        PlainParts.Release(stage.Scene, at);
    }

    // Disabled: the MenuFlyout item's disabled brush on the header, the gesture and the check.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_It_Is_The_MenuFlyouts_Disabled_Brush(string variant)
    {
        var item = new MenuItem
        {
            Header = "copy",
            IsEnabled = false,
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = true,
            InputGesture = new KeyGesture(Key.C, KeyModifiers.Control),
        };
        var radio = new MenuItem { Header = "radio", IsEnabled = false, ToggleType = MenuItemToggleType.Radio, IsChecked = true };
        using var stage = new Stage(VariantOf(variant), item, radio);

        var disabled = PlainParts.Resource<IBrush>(item, "MenuFlyoutItemDisabledForegroundThemeBrush");
        Assert.Same(disabled, ForegroundOf(item));
        Assert.Same(disabled, Part<TextBlock>(item, "PART_InputGestureText").Foreground);
        Assert.Same(disabled, Part<Path>(item, "PART_Check").Fill);
        Assert.Same(disabled, Part<Ellipse>(radio, "PART_Radio").Fill);
    }

    // Avalonia's other separator, a Header of "-": the plain Separator's line, not focusable.
    [AvaloniaFact]
    public void A_Dash_Header_Is_The_Plain_Separators_Line()
    {
        var dash = new MenuItem { Header = "-" };
        var parent = new MenuItem { Header = "parent", Items = { new MenuItem { Header = "child" }, dash } };
        using var stage = new Stage(null, parent);
        parent.IsSubMenuOpen = true;
        Dispatcher.UIThread.RunJobs();

        var line = dash.GetVisualDescendants().OfType<Separator>().Single();
        Near(1.5, line.Bounds.Height);
        Assert.False(dash.Focusable);
        parent.IsSubMenuOpen = false;
    }
}
