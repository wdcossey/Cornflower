using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ListBoxReadings;
using CfTilt = Cornflower.Controls.Tilt;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ListBox and ListBoxItem (<c>Controls/ListBox.axaml</c>), keyed themes on Avalonia's ListBox and ListBoxItem,
/// against the phone's styles and the reference app's captures (<see cref="ListBoxReadings"/>). docs/controls/ListBox
/// has the plan and the comparison.
/// </summary>
public class ListBoxTests
{
    private const double SettleMs = 160;

    /// <summary>A ListBoxItem on its own in the item theme, on the reference app's 450 × 100 stage, as the Catalog's.</summary>
    internal sealed class ItemStage : IDisposable
    {
        private readonly Scene _scene;

        public ItemStage(ThemeVariant? variant = null, ListBoxItem? item = null)
        {
            Item = item ?? new ListBoxItem { Content = "list box item" };
            Item.Bind(StyledElement.ThemeProperty, Item.GetResourceObservable("Wp81ListBoxItemTheme"));
            _scene = new Scene(Staged(Item), variant, width: ListViewReadings.StageWidth, height: ListViewReadings.ItemStageHeight);
        }

        public ListBoxItem Item { get; }

        public Window Window => _scene.Window;

        public Point At(double x, double y) => Item.TranslatePoint(new Point(x, y), Window)!.Value;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>A ListBox of "one", "two" and "three" in the list theme on the reference app's 450 × 260 stage, as the Catalog's.</summary>
    internal sealed class ListStage : IDisposable
    {
        private readonly Scene _scene;

        public ListStage(ThemeVariant? variant = null, int selected = -1, bool enabled = true)
        {
            List = new ListBox { ItemsSource = new[] { "one", "two", "three" }, SelectedIndex = selected, IsEnabled = enabled };
            List.Bind(StyledElement.ThemeProperty, List.GetResourceObservable("Wp81ListBoxTheme"));
            _scene = new Scene(Staged(List), variant, width: ListViewReadings.StageWidth, height: ListViewReadings.ListStageHeight);
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public ListBoxItem Item(int index) => (ListBoxItem)List.ContainerFromIndex(index)!;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    private static Grid Staged(Control control)
    {
        var slot = new Grid { Margin = ListViewReadings.Slot, Children = { control } };
        var stage = new Grid { Children = { slot } };
        stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
        return stage;
    }

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name && c.TemplatedParent == control);

    private static ContentPresenter PresenterOf(ListBoxItem item) => Part<ContentPresenter>(item, "PART_ContentPresenter");

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Color TextOf(ListBoxItem item) => ColourOf(PresenterOf(item).Foreground);

    private static Color InnerOf(ListBoxItem item) => ColourOf(Part<Grid>(item, "InnerGrid").Background);

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>The frame's pixel at a point of a part, in view pixels from its top left.</summary>
    private static Color PixelOf(WriteableBitmap frame, Visual part, Visual window, double x, double y)
    {
        var at = part.TranslatePoint(new Point(x, y), window)!.Value * Scene.ReferenceScaling;
        return Frames.PixelAt(frame, (int)at.X, (int)at.Y);
    }

    /// <summary>A colour drawn over the page: the frame's pixel for it.</summary>
    private static Color Over(Color colour, bool dark)
    {
        var page = dark ? 0 : 255;
        byte Mix(byte c) => (byte)Math.Round(page + (c - page) * colour.A / 255.0);
        return Color.FromRgb(Mix(colour.R), Mix(colour.G), Mix(colour.B));
    }

    private static void AssertNear(Color expected, Color actual, string because)
    {
        Assert.True(
            Math.Abs(expected.R - actual.R) <= 1 && Math.Abs(expected.G - actual.G) <= 1 && Math.Abs(expected.B - actual.B) <= 1,
            $"{because}: expected {expected}, drawn {actual}");
    }

    // ----- How an app picks it -----

    // The keys, and the list's theme gives each item the item's: a ListBox with no Theme stays the ListView.
    [AvaloniaFact]
    public void The_Keyed_List_Gives_Its_Items_The_Keyed_Item_Theme_And_A_Plain_ListBox_Stays_The_ListView()
    {
        using var stage = new ListStage();
        var listTheme = Assert.IsType<ControlTheme>(stage.Window.FindResource("Wp81ListBoxTheme"));
        var itemTheme = Assert.IsType<ControlTheme>(stage.Window.FindResource("Wp81ListBoxItemTheme"));

        Assert.Same(listTheme, stage.List.Theme);
        Assert.Same(itemTheme, stage.List.ItemContainerTheme);
        for (var i = 0; i < 3; i++)
            Assert.Same(itemTheme, stage.Item(i).Theme);
        Assert.NotNull(Part<Grid>(stage.Item(0), "InnerGrid"));

        var own = new ListBoxItem { Content = "own" };
        var plain = new ListBox { Items = { "plain" } };
        var keyed = new ListBox { Items = { own } };
        keyed.Bind(StyledElement.ThemeProperty, keyed.GetResourceObservable("Wp81ListBoxTheme"));
        using var scene = new Scene(new StackPanel { Children = { plain, keyed } });
        Assert.Same(scene.Window.FindResource("Wp81ListBoxItemTheme"), own.Theme);   // an item of its own too
        var plainItem = (ListBoxItem)plain.ContainerFromIndex(0)!;
        Assert.Null(plainItem.Theme);
        Assert.Contains(plainItem.GetVisualDescendants().OfType<Control>(), c => c.Name == "TiltContainer");
    }

    // ----- The ListBox's style -----

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Lists_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ListStage(VariantOf(variant));
        var list = stage.List;

        Assert.Equal(ListForeground, ColourOf(list.Foreground));
        Assert.Equal(ListBackground(dark), ColourOf(list.Background));
        Assert.Equal(ListBorder, ColourOf(list.BorderBrush));
        Assert.Equal(BorderThickness, list.BorderThickness);
        Assert.Equal(ScrollBarVisibility.Disabled, ScrollViewer.GetHorizontalScrollBarVisibility(list));
        Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetVerticalScrollBarVisibility(list));
        Assert.False(ScrollViewer.GetIsDeferredScrollingEnabled(list));
        Assert.True(ScrollViewer.GetBringIntoViewOnFocusChange(list));
        Assert.False(list.IsTabStop);
        Assert.Equal(KeyboardNavigationMode.Once, KeyboardNavigation.GetTabNavigation(list));
        Assert.Equal(stage.Window.FindResource("ContentControlThemeFontFamily"), list.FontFamily);
        Assert.Equal(FontSize, list.FontSize);
        Assert.Null(list.FocusAdorner);
        Assert.IsType<VirtualizingStackPanel>(list.ItemsPanelRoot);

        var root = Part<Border>(list, "LayoutRoot");
        Assert.Same(list.Background, root.Background);
        Assert.Equal(BorderThickness, root.BorderThickness);
    }

    // The list's own font, not its parent's: ContentControlThemeFontFamily at 12, as the phone's style sets them.
    [AvaloniaFact]
    public void The_List_Sets_Its_Own_Font_Over_Its_Parents()
    {
        var list = new ListBox { ItemsSource = new[] { "one" } };
        list.Bind(StyledElement.ThemeProperty, list.GetResourceObservable("Wp81ListBoxTheme"));
        var parent = new ContentControl { FontFamily = new FontFamily("Courier New"), FontSize = 30, Content = list };
        using var scene = new Scene(parent);

        Assert.Equal(scene.Window.FindResource("ContentControlThemeFontFamily"), list.FontFamily);
        Assert.Equal(FontSize, list.FontSize);
        Assert.Equal(FontSize, ((ListBoxItem)list.ContainerFromIndex(0)!).FontSize);
    }

    // The phone's ScrollViewer, with the thin indicators, its values bound from the list's attached ones, named as
    // Avalonia's ListBox looks for it.
    [AvaloniaFact]
    public void Its_ScrollViewer_Is_The_Phones_With_The_Lists_Values()
    {
        using var stage = new ListStage();
        var list = stage.List;
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Hidden);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        ScrollViewer.SetIsScrollChainingEnabled(list, false);
        ScrollViewer.SetIsDeferredScrollingEnabled(list, true);
        ScrollViewer.SetBringIntoViewOnFocusChange(list, false);
        ScrollViewer.SetAllowAutoHide(list, false);
        ScrollViewer.SetIsScrollInertiaEnabled(list, false);
        ScrollViewer.SetVerticalSnapPointsType(list, SnapPointsType.MandatorySingle);
        ScrollViewer.SetHorizontalSnapPointsType(list, SnapPointsType.Mandatory);
        list.Padding = new Thickness(3, 4, 5, 6);
        Dispatcher.UIThread.RunJobs();

        var viewer = Part<ScrollViewer>(list, "PART_ScrollViewer");
        Assert.Same(viewer, list.Scroll);
        Assert.True(PhoneScrollIndicators.GetIsEnabled(viewer));
        Assert.Equal(ScrollBarVisibility.Hidden, viewer.VerticalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Auto, viewer.HorizontalScrollBarVisibility);
        Assert.False(viewer.IsScrollChainingEnabled);
        Assert.True(viewer.IsDeferredScrollingEnabled);
        Assert.False(viewer.BringIntoViewOnFocusChange);
        Assert.False(viewer.AllowAutoHide);
        Assert.False(viewer.IsScrollInertiaEnabled);
        Assert.Equal(SnapPointsType.MandatorySingle, viewer.VerticalSnapPointsType);
        Assert.Equal(SnapPointsType.Mandatory, viewer.HorizontalSnapPointsType);
        Assert.Equal(new Thickness(3, 4, 5, 6), Part<ItemsPresenter>(list, "PART_ItemsPresenter").Margin);
    }

    // The capture: the pale box on the page (#CCFFFFFF over black, #BF000000 over white) inside the transparent border,
    // whose band, 4 raw pixels, shows the page; the selected item in the accent.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_List_Draws_The_Pale_Box_Inside_Its_Border_And_The_Selection_In_The_Accent(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ListStage(VariantOf(variant), selected: 1);
        using var frame = stage.Capture();

        var page = Over(Colors.Transparent, dark);
        for (var raw = 0; raw < 4; raw++)
        {
            AssertNear(page, PixelOf(frame, stage.List, stage.Window, 300, (raw + 0.5) / Scene.ReferenceScaling), $"the border's band, row {raw}");
            AssertNear(page, PixelOf(frame, stage.List, stage.Window, (raw + 0.5) / Scene.ReferenceScaling, 100), $"the border's band, column {raw}");
        }
        AssertNear(Over(ListBackground(dark), dark), PixelOf(frame, stage.List, stage.Window, 300, 4.5 / Scene.ReferenceScaling), "inside the border");
        AssertNear(Over(ListBackground(dark), dark), PixelOf(frame, stage.Item(0), stage.Window, 300, 16), "an unselected item");
        AssertNear(Over(ListBackground(dark), dark), PixelOf(frame, stage.List, stage.Window, 300, 200), "below the items");
        AssertNear(Accent, PixelOf(frame, stage.Item(1), stage.Window, 300, 16), "the selected item");
    }

    // The dump's bounds: the items inside the 1.5 border, 32.0833 tall, one under the other, each one's content at 6.5, 8
    // and 16.25 tall (Segoe WP's 1.33 em line at 12), at its own width (HorizontalContentAlignment Left).
    [AvaloniaFact]
    public void The_Bounds_Are_The_Phones_Within_Avalonias_Rounding()
    {
        using var stage = new ListStage(selected: 1);
        var list = stage.List;

        Assert.Equal(ListViewReadings.InStage.X, In(list, stage.Window).X, 0.001);
        Assert.Equal(ListViewReadings.OurWidth, list.Bounds.Width, 0.001);
        for (var i = 0; i < 3; i++)
        {
            var item = stage.Item(i);
            Assert.Equal(Inset, In(item, list).X, 0.001);
            Assert.Equal(ItemTops[i], In(item, list).Y, 0.001);
            Assert.Equal(ItemHeight, item.Bounds.Height, 0.001);
            Assert.Equal(ContentInItem.X, In(PresenterOf(item), item).X, 0.001);
            Assert.Equal(ContentInItem.Y, In(PresenterOf(item), item).Y, 0.001);
            Assert.Equal(ContentHeight, PresenterOf(item).Bounds.Height, 0.001);
            Assert.True(PresenterOf(item).Bounds.Width < 40, $"item {i}'s content is {PresenterOf(item).Bounds.Width} wide");
        }
    }

    // ----- The item's style and template -----

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Items_Style_Sets_What_The_Phone_Read(string variant)
    {
        using var stage = new ItemStage(VariantOf(variant));
        var item = stage.Item;

        Assert.Equal(Colors.Transparent, ColourOf(item.Background));
        Assert.Equal(KeyboardNavigationMode.Local, KeyboardNavigation.GetTabNavigation(item));
        Assert.Equal(ItemPadding, item.Padding);
        Assert.Equal(HorizontalAlignment.Left, item.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, item.VerticalContentAlignment);
        Assert.Null(item.FocusAdorner);
        Assert.False(item.ClipToBounds);
        Assert.Equal(0, Part<Rectangle>(item, "PressedBackground").Opacity);
        Assert.Equal(PressedBackground(variant == "Dark"), ColourOf(Part<Rectangle>(item, "PressedBackground").Fill));
        Assert.True(PhoneHeaderLine.GetIsEnabled(PresenterOf(item)));
    }

    // An item on its own, 87.92 tall in its stage, as the dump's: its content centred, as WinRT's default centres it.
    [AvaloniaFact]
    public void An_Items_Content_Is_Centred_Down_Its_Height()
    {
        using var stage = new ItemStage();
        var item = stage.Item;
        var presenter = PresenterOf(item);

        Assert.Equal(StandaloneHeight, item.Bounds.Height, 0.001);
        var gap = item.Bounds.Height - presenter.Bounds.Height - 2 * In(presenter, item).Y;
        Assert.True(Math.Abs(gap) <= 2 / Scene.ReferenceScaling, $"the content is {In(presenter, item).Y} down, {presenter.Bounds.Height} tall");
    }

    // ----- Selection -----

    // Selected: InnerGrid in the accent, the text white, in both themes.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Selected_Fills_The_Item_With_The_Accent_And_Turns_The_Text_White(string variant)
    {
        using var stage = new ListStage(VariantOf(variant), selected: 1);

        Assert.Equal(Accent, InnerOf(stage.Item(1)));
        Assert.Equal(SelectedForeground, TextOf(stage.Item(1)));
        Assert.Equal(Colors.Transparent, InnerOf(stage.Item(0)));
        Assert.Equal(ListForeground, TextOf(stage.Item(0)));
    }

    // A tap selects the item it lands on, at its very edge too: LayoutRoot takes the hits.
    [AvaloniaFact]
    public void A_Tap_Selects_The_Item()
    {
        using var stage = new ListStage();
        var at = stage.Item(2).TranslatePoint(new Point(405, 1), stage.Window)!.Value;

        var touch = stage.Window.TouchBegin(at);
        stage.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, stage.List.SelectedIndex);
        Assert.Equal(Accent, InnerOf(stage.Item(2)));
    }

    // SelectedPointerOver takes its own key (the accent too, by default).
    [AvaloniaFact]
    public void Selected_Under_The_Mouse_Takes_The_SelectedPointerOver_Key()
    {
        using var stage = new ListStage(selected: 1);
        var red = new SolidColorBrush(Color.FromUInt32(0xFFFF0000));
        stage.List.Resources["ListBoxItemSelectedPointerOverBackgroundThemeBrush"] = red;

        stage.Window.MouseMove(stage.Item(1).TranslatePoint(new Point(100, 16), stage.Window)!.Value);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(red.Color, InnerOf(stage.Item(1)));
        Assert.Equal(SelectedForeground, TextOf(stage.Item(1)));
    }

    // ----- The press -----

    // Pressed: PressedBackground at 1, the pale brush; the text black; and the Button's tilt on InnerGrid, inside an
    // untilted LayoutRoot.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Press_Shows_The_Pressed_Brush_And_Tilts_InnerGrid(string variant)
    {
        var dark = variant == "Dark";
        using var clock = new TestClock();
        using var stage = new ItemStage(VariantOf(variant));
        var item = stage.Item;
        var inner = Part<Grid>(item, "InnerGrid");

        stage.Window.TouchBegin(stage.At(200, 40));
        clock.At(SettleMs);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, Part<Rectangle>(item, "PressedBackground").Opacity);
        Assert.Equal(PressedBackground(dark), ColourOf(Part<Rectangle>(item, "PressedBackground").Fill));
        Assert.Equal(PressedForeground, TextOf(item));
        Assert.True(CfTilt.GetIsEnabled(item));
        Assert.Same(inner, CfTilt.TargetOf(item));
        Assert.IsType<MatrixTransform>(inner.RenderTransform);
        Assert.Null(Part<Border>(item, "LayoutRoot").RenderTransform);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(item));
    }

    // A press on a selected item: the pale brush over the accent and the press's black text, legible (its style comes
    // after the selection's; the phone wasn't read for it).
    [AvaloniaFact]
    public void A_Press_On_A_Selected_Item_Draws_The_Presss_Text()
    {
        using var clock = new TestClock();
        using var stage = new ListStage(selected: 1);
        var two = stage.Item(1);

        stage.Window.TouchBegin(two.TranslatePoint(new Point(200, 16), stage.Window)!.Value);
        clock.At(SettleMs);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, Part<Rectangle>(two, "PressedBackground").Opacity);
        Assert.Equal(PressedForeground, TextOf(two));
    }

    // ----- Disabled -----

    // The item: LayoutRoot's background cleared, the text dimmed; selected, InnerGrid and the text in their disabled brushes.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Text_And_Clears_The_Background(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant), new ListBoxItem
        {
            Content = "list box item",
            IsEnabled = false,
            Background = new SolidColorBrush(Color.FromUInt32(0xFFFF0000)),
        });

        Assert.Equal(DisabledForeground(dark), TextOf(stage.Item));
        Assert.Equal(Colors.Transparent, ColourOf(Part<Border>(stage.Item, "LayoutRoot").Background));

        using var list = new ListStage(VariantOf(variant), selected: 1, enabled: false);
        Assert.Equal(SelectedDisabledBackground(dark), InnerOf(list.Item(1)));
        Assert.Equal(SelectedDisabledForeground(dark), TextOf(list.Item(1)));
        Assert.Equal(DisabledForeground(dark), TextOf(list.Item(0)));
    }

    // The list: the background cleared, the border in ListBoxDisabledForegroundThemeBrush.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Disabled_List_Clears_Its_Background_And_Draws_Its_Border(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ListStage(VariantOf(variant), enabled: false);
        var root = Part<Border>(stage.List, "LayoutRoot");

        Assert.Equal(Colors.Transparent, ColourOf(root.Background));
        Assert.Equal(ListDisabledBorder(dark), ColourOf(root.BorderBrush));
        using var frame = stage.Capture();
        AssertNear(Over(ListDisabledBorder(dark), dark), PixelOf(frame, stage.List, stage.Window, 300, 0.5), "the border");
        AssertNear(Over(Colors.Transparent, dark), PixelOf(frame, stage.List, stage.Window, 300, 200), "inside the list");
    }

    // ----- No check mode, no focus -----

    // The phone's ListBox has no check mode: in Multiple with Toggle its items have no box, and a ListBox inside an item
    // of a ListView in the check mode isn't in it.
    [AvaloniaFact]
    public void It_Has_No_Check_Mode_Even_Inside_A_ListView_In_The_Check_Mode()
    {
        var inner = new ListBox { ItemsSource = new[] { "a", "b" }, SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle };
        inner.Bind(StyledElement.ThemeProperty, inner.GetResourceObservable("Wp81ListBoxTheme"));
        var outer = new ListBox { SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle, Items = { new ListBoxItem { Content = inner } } };
        using var scene = new Scene(outer);

        Assert.True(PhoneListMultiSelect.GetIsOn(outer));
        Assert.False(PhoneListMultiSelect.GetIsOn(inner));
        var item = (ListBoxItem)inner.ContainerFromIndex(0)!;
        Assert.False(PhoneListMultiSelect.GetIsOn(item));
        Assert.DoesNotContain(item.GetVisualDescendants().OfType<Control>(), c => c.Name == "CheckboxTiltContainer");
    }

    // An item of the keyed theme as a ListView's own item in the check mode isn't in it either.
    [AvaloniaFact]
    public void A_Keyed_Item_In_A_ListView_In_The_Check_Mode_Isnt_In_It()
    {
        var item = new ListBoxItem { Content = "a" };
        item.Bind(StyledElement.ThemeProperty, item.GetResourceObservable("Wp81ListBoxItemTheme"));
        var outer = new ListBox { SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle, Items = { item } };
        using var scene = new Scene(outer);

        Assert.True(PhoneListMultiSelect.GetIsOn(outer));
        Assert.NotNull(item.Theme);
        Assert.False(PhoneListMultiSelect.GetIsOn(item));
    }

    // The list takes the item theme by its key, so an app's own Wp81ListBoxItemTheme reaches its items.
    [AvaloniaFact]
    public void An_Apps_Own_Item_Theme_By_The_Key_Reaches_The_Lists_Items()
    {
        using var stage = new ListStage();
        var stock = Assert.IsType<ControlTheme>(stage.Window.FindResource("Wp81ListBoxItemTheme"));
        var own = new ControlTheme(typeof(ListBoxItem)) { BasedOn = stock };
        stage.Window.Resources["Wp81ListBoxItemTheme"] = own;
        Dispatcher.UIThread.RunJobs();

        Assert.Same(own, stage.List.ItemContainerTheme);
    }

    // The template has no FocusStates and PointerOver sets nothing: focus and a mouse over an item draw nothing.
    [AvaloniaFact]
    public void Keyboard_Focus_And_The_Pointer_Over_Draw_Nothing()
    {
        using var stage = new ListStage();
        using var before = stage.Capture();

        stage.Item(1).Focus(NavigationMethod.Tab);
        stage.Window.MouseMove(stage.Item(2).TranslatePoint(new Point(100, 16), stage.Window)!.Value);
        Dispatcher.UIThread.RunJobs();
        using var after = stage.Capture();

        Assert.True(stage.Item(1).IsFocused);
        Assert.Contains(":pointerover", stage.Item(2).Classes);
        Assert.True(Frames.SameAs(before, after), "focus or the pointer drew something");
    }

    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new ListStage();
        Assert.Equal(ListBackground(true), ColourOf(stage.List.Background));

        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ListBackground(false), ColourOf(stage.List.Background));
    }
}
