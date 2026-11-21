using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.GridViewReadings;
using CfTilt = Cornflower.Controls.Tilt;
using LV = Cornflower.Tests.Controls.ListViewReadings;
using Path = Avalonia.Controls.Shapes.Path;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 GridView and GridViewItem (the keyed themes at the end of <c>Controls/ListView.axaml</c>), based on the ListView's, against the
/// phone's styles, the reference app's captures and the probe (<see cref="GridViewReadings"/>). docs/controls/GridView
/// has the plan and the comparison.
/// </summary>
public class GridViewTests
{
    private const double SettleMs = 160;

    /// <summary>A grey 120 × 120 tile with its name, as the reference app's <c>Catalog.Tile</c>.</summary>
    internal static Grid TileOf(string name) => new()
    {
        Width = Tile,
        Height = Tile,
        Background = new SolidColorBrush(TileGrey),
        Children = { new TextBlock { Text = name, FontSize = 20, Margin = new Thickness(6) } },
    };

    /// <summary>A ListBoxItem holding a tile, in the item theme, on the reference app's 450 × 160 stage.</summary>
    internal sealed class ItemStage : IDisposable
    {
        private readonly Scene _scene;

        public ItemStage(ThemeVariant? variant = null, ListBoxItem? item = null)
        {
            Item = item ?? new ListBoxItem { Content = TileOf("one") };
            Item.Bind(StyledElement.ThemeProperty, Item.GetResourceObservable("Wp81GridViewItemTheme"));
            _scene = new Scene(Staged(Item), variant, width: LV.StageWidth, height: ItemStageHeight);
        }

        public ListBoxItem Item { get; }

        public Window Window => _scene.Window;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>A GridView of six tiles in the list theme on the reference app's 450 × 300 stage, as the Catalog's.</summary>
    internal sealed class GridStage : IDisposable
    {
        private readonly Scene _scene;

        public GridStage(ThemeVariant? variant = null, int selected = -1, SelectionMode mode = SelectionMode.Single)
        {
            List = new ListBox
            {
                ItemsSource = new[] { "one", "two", "three", "four", "five", "six" }.Select(TileOf).ToList(),
                SelectionMode = mode,
            };
            List.Bind(StyledElement.ThemeProperty, List.GetResourceObservable("Wp81GridViewTheme"));
            _scene = new Scene(Staged(List), variant, width: LV.StageWidth, height: GridStageHeight);
            List.SelectedIndex = selected;
            Dispatcher.UIThread.RunJobs();
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public ListBoxItem Item(int index) => (ListBoxItem)List.ContainerFromIndex(index)!;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>The probe's page: a GridView of 20 tiles 19 in and 80 down the 450 × 800 view.</summary>
    private sealed class ProbePage : IDisposable
    {
        private readonly Scene _scene;

        public ProbePage()
        {
            List = new ListBox
            {
                Margin = ProbeGridMargin,
                ItemsSource = Enumerable.Range(0, ProbeTiles).Select(i => TileOf($"tile {i}")).ToList(),
            };
            List.Bind(StyledElement.ThemeProperty, List.GetResourceObservable("Wp81GridViewTheme"));
            _scene = new Scene(List);
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public ListBoxItem Item(int index) => (ListBoxItem)List.ContainerFromIndex(index)!;

        public void Dispose() => _scene.Dispose();
    }

    private static Grid Staged(Control control)
    {
        var slot = new Grid { Margin = LV.Slot, Children = { control } };
        var stage = new Grid { Children = { slot } };
        stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
        return stage;
    }

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name && c.TemplatedParent == control);

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    private static double ShiftOf(Control part) => part.RenderTransform?.Value.M31 ?? 0;

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static string Hex(Color c) => Tokens.Token.Hex(c);

    /// <summary>The item's edges in the frame, in raw pixels: left and top inclusive, right and bottom exclusive.</summary>
    private static (int Left, int Top, int Right, int Bottom) RawEdges(Visual item, Visual window)
    {
        var at = In(item, window);
        int R(double v) => (int)Math.Round(v * Scene.ReferenceScaling);
        return (R(at.X), R(at.Y), R(at.X + item.Bounds.Width), R(at.Y + item.Bounds.Height));
    }

    private static void AssertPixel(Color expected, WriteableBitmap frame, int x, int y, string what) =>
        Assert.True(expected == Frames.PixelAt(frame, x, y), $"{what}, raw ({x}, {y}): {Hex(Frames.PixelAt(frame, x, y))}, not {Hex(expected)}");

    /// <summary>The grey's extent in a frame, in raw pixels: the frame locked once and read by rows.</summary>
    private static PixelRect GreyOf(WriteableBitmap frame)
    {
        using var buffer = frame.Lock();
        // Opaque grey is the same four bytes in RGBA and BGRA.
        Assert.True(buffer.Format == PixelFormat.Rgba8888 || buffer.Format == PixelFormat.Bgra8888, $"{buffer.Format}");
        var grey = BitConverter.ToInt32([TileGrey.R, TileGrey.G, TileGrey.B, TileGrey.A]);
        var row = new int[buffer.Size.Width];
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
            for (var x = 0; x < row.Length; x++)
            {
                if (row[x] != grey)
                    continue;
                x0 = Math.Min(x0, x);
                y0 = Math.Min(y0, y);
                x1 = Math.Max(x1, x);
                y1 = Math.Max(y1, y);
            }
        }

        return x1 < 0 ? default : new PixelRect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    private static void Tap(Window window, Point at)
    {
        var touch = window.TouchBegin(at);
        window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();
    }

    private static Point CentreOf(ListBoxItem item, Window window) =>
        item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;

    // ----- The keys -----

    // The list's theme gives its items the item's theme; the panel is the phone's ItemsWrapGrid, Horizontal, as a
    // WrapPanel; a ListBox with no Theme stays the ListView, in a VirtualizingStackPanel.
    [AvaloniaFact]
    public void The_Keyed_List_Wraps_And_Gives_Its_Items_The_Keyed_Item_Theme()
    {
        using var stage = new GridStage();
        var theme = stage.Window.FindResource("Wp81GridViewItemTheme");

        Assert.Same(theme, stage.List.ItemContainerTheme);
        Assert.Same(theme, stage.Item(0).Theme);
        var panel = Assert.IsType<WrapPanel>(stage.List.ItemsPanelRoot);
        Assert.Equal(Orientation.Horizontal, panel.Orientation);

        var plain = new ListBox { ItemsSource = new[] { "one" } };
        using var scene = new Scene(plain);
        Assert.IsType<VirtualizingStackPanel>(plain.ItemsPanelRoot);
        Assert.NotSame(theme, ((ListBoxItem)plain.ContainerFromIndex(0)!).Theme);
    }

    // gridview.json's setters, the ListView's: no horizontal scroll, the vertical Auto, IsTabStop off, TabNavigation Once,
    // and the ListView's template (its ScrollViewer and ItemsPresenter).
    [AvaloniaFact]
    public void The_Lists_Style_Is_The_ListViews()
    {
        using var stage = new GridStage();
        var list = stage.List;

        Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, ScrollViewer.GetHorizontalScrollBarVisibility(list));
        Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, ScrollViewer.GetVerticalScrollBarVisibility(list));
        Assert.False(list.IsTabStop);
        Assert.Equal(KeyboardNavigationMode.Once, KeyboardNavigation.GetTabNavigation(list));
        Assert.NotNull(Part<ScrollViewer>(list, "PART_ScrollViewer"));
        Assert.NotNull(Part<ItemsPresenter>(list, "PART_ItemsPresenter"));
    }

    // gridviewitem.json's setters: GridViewItemMargin 0, the ListViewItem's font, background and press, and the content
    // centred both ways, WinRT's default, as the phone's style sets neither.
    [AvaloniaFact]
    public void The_Items_Style_Sets_What_The_Phone_Read()
    {
        using var stage = new ItemStage();
        var item = stage.Item;

        Assert.Equal(ItemMargin, item.Margin);
        Assert.Equal(new Thickness(0), item.Padding);
        Assert.Equal(HorizontalAlignment.Center, item.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, item.VerticalContentAlignment);
        Assert.Equal(LV.FontSize, item.FontSize);
        Assert.Equal(Colors.Transparent, ColourOf(item.Background));
        Assert.True(CfTilt.GetIsEnabled(item));
        Assert.Same(Part<Border>(item, "TiltContainer"), CfTilt.TargetOf(item));
    }

    // ----- Layout -----

    // The capture of the item on its own: the tile centred in the item, raw (396, 62), 288 square.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void An_Items_Tile_Is_Centred_In_It(string variant)
    {
        using var stage = new ItemStage(VariantOf(variant));
        using var frame = stage.Capture();
        var grey = GreyOf(frame);

        // Across, to the pixel; down, within the one raw pixel Avalonia's rounding of the 147.92-tall item gives (ADR 0003).
        Assert.Equal(ItemTile.Size, grey.Size);
        Assert.Equal(ItemTile.X, grey.X);
        Assert.InRange(grey.Y, ItemTile.Y, ItemTile.Y + 1);
    }

    // The six-items capture: three tiles a row from the list's left edge, wrapping, each 120 after the last, abutting:
    // the grey from raw (46, 29), 864 × 576.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Tiles_Fill_A_Row_Left_To_Right_And_Wrap_Abutting(string variant)
    {
        using var stage = new GridStage(VariantOf(variant), selected: 1);
        for (var i = 0; i < 6; i++)
            Assert.Equal(new Rect(i % 3 * Tile, i / 3 * Tile, Tile, Tile), stage.Item(i).Bounds);

        using var frame = stage.Capture();
        Assert.Equal(SixTiles, GreyOf(frame));
    }

    // The probe: 20 tiles, seven rows 840 tall in the 720 view below the header, no horizontal scroll: it scrolls down,
    // to 120 at its end.
    [AvaloniaFact]
    public void The_Grid_Scrolls_Down_And_Not_Across()
    {
        using var page = new ProbePage();
        var viewer = Part<ScrollViewer>(page.List, "PART_ScrollViewer");

        Assert.Equal(ProbeExtent, viewer.Extent.Height, 0.001);
        Assert.Equal(ProbeViewport, viewer.Viewport.Height, 0.001);
        Assert.Equal(viewer.Viewport.Width, viewer.Extent.Width, 0.001);
        Assert.Equal(new Point(0, 0), In(page.Item(0), page.List));
        Assert.Equal(new Point(0, Tile), In(page.Item(3), page.List));

        viewer.Offset = new Vector(0, 1000);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ProbeExtent - ProbeViewport, viewer.Offset.Y, 0.001);
    }

    // ----- Selection -----

    // Selected in Single draws nothing: the probe's item went SelectedUnfocused, its capture the rest's, byte for byte.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Single_Selection_Draws_Nothing(string variant)
    {
        using var selected = new ItemStage(VariantOf(variant), new ListBoxItem { Content = TileOf("one"), IsSelected = true });
        using var rest = new ItemStage(VariantOf(variant));
        using var a = selected.Capture();
        using var b = rest.Capture();

        Assert.Equal(0, Part<Border>(selected.Item, "SelectedBorder").Opacity);
        Assert.True(Frames.SameAs(a, b));
    }

    // Multiple, the check mode: GridMultiSelect on every item, the 2.5 accent border on each, selected or not, drawn
    // where the capture draws it; the content not moved and no check box.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void In_The_Check_Mode_Every_Item_Has_The_Accent_Border(string variant)
    {
        using var stage = new GridStage(VariantOf(variant), selected: 1, mode: SelectionMode.Multiple | SelectionMode.Toggle);
        using var frame = stage.Capture();

        for (var i = 0; i < 6; i++)
        {
            var item = stage.Item(i);
            var border = Part<Border>(item, "SelectedBorder");
            Assert.Equal(1, border.Opacity);
            Assert.Equal(new Thickness(BorderThickness), border.BorderThickness);
            Assert.Equal(LV.Accent, ColourOf(border.BorderBrush));
            Assert.False(Part<Border>(item, "CheckboxTiltContainer").IsEffectivelyVisible);
            Assert.Equal(0, ShiftOf(Part<Border>(item, "ContentBorder")));

            var (l, t, r, b) = RawEdges(item, stage.Window);
            var midX = (l + r) / 2;
            var midY = (t + b) / 2;
            AssertPixel(LV.Accent, frame, l + BorderIn, midY, $"item {i}'s left border");
            AssertPixel(LV.Accent, frame, r - 1 - BorderIn, midY, $"item {i}'s right border");
            AssertPixel(LV.Accent, frame, midX, t + BorderIn, $"item {i}'s top border");
            AssertPixel(LV.Accent, frame, midX, b - 1 - BorderIn, $"item {i}'s bottom border");
            AssertPixel(TileGrey, frame, l + PastBorder, midY, $"item {i}'s tile inside the border");
        }
    }

    // The capture of the item on its own in GridMultiSelect: the border inside the item's edges, the page past it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Border_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant));
        stage.Item.SetValue(PhoneListMultiSelect.IsOnProperty, true);
        Dispatcher.UIThread.RunJobs();
        using var frame = stage.Capture();
        var (l, t, r, b) = RawEdges(stage.Item, stage.Window);
        // Between the item's edges and the tile's: (l + r) / 2 runs through the tile, so the top and bottom are read
        // on the page beside it, and the sides halfway down.
        var midY = (t + b) / 2;
        var besideX = l + (ItemTile.X - l) / 2;

        AssertPixel(LV.Accent, frame, l + BorderIn, midY, "the left border");
        AssertPixel(LV.Page(dark), frame, l + PastBorder, midY, "past the left border");
        AssertPixel(LV.Accent, frame, r - 1 - BorderIn, midY, "the right border");
        AssertPixel(LV.Page(dark), frame, r - PastBorder, midY, "past the right border");
        AssertPixel(LV.Accent, frame, besideX, t + BorderIn, "the top border");
        AssertPixel(LV.Page(dark), frame, besideX, t + PastBorder, "below the top border");
        AssertPixel(LV.Accent, frame, besideX, b - 1 - BorderIn, "the bottom border");
        AssertPixel(LV.Page(dark), frame, besideX, b - PastBorder, "above the bottom border");
        // Unselected: no earmark.
        foreach (var (fromRight, fromTop) in EarmarkPoints.Concat(TickPoints))
            AssertPixel(LV.Page(dark), frame, r - fromRight, t + fromTop, "no earmark on an unselected item");
    }

    // A selected item in GridMultiSelect: the 34 × 34 accent earmark at its top right inside the border, and the
    // 17 × 14.5 tick in ListViewItemCheckThemeBrush, white in both variants, 1 in from the corner.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Selected_Item_In_The_Check_Mode_Has_The_Earmark_And_Its_Tick(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant), new ListBoxItem { Content = TileOf("one"), IsSelected = true });
        stage.Item.SetValue(PhoneListMultiSelect.IsOnProperty, true);
        Dispatcher.UIThread.RunJobs();
        using var frame = stage.Capture();
        var (_, t, r, _) = RawEdges(stage.Item, stage.Window);

        foreach (var (fromRight, fromTop) in TickPoints)
            AssertPixel(LV.OverAccent, frame, r - fromRight, t + fromTop, "the tick");
        foreach (var (fromRight, fromTop) in EarmarkPoints)
            AssertPixel(LV.Accent, frame, r - fromRight, t + fromTop, "the earmark");
        AssertPixel(LV.Page(dark), frame, r - BelowTheCut.FromRight, t + BelowTheCut.FromTop, "below the earmark's cut");

        var mark = Part<Grid>(stage.Item, "SelectedCheckMark");
        Assert.Equal(1, mark.Opacity);
        Assert.Equal(new Point(stage.Item.Bounds.Width - BorderThickness - CheckMark, BorderThickness), In(mark, stage.Item), new PointNear());
        Assert.Equal(CheckMark, mark.Bounds.Width, 0.001);
        var tick = Part<Path>(stage.Item, "SelectedGlyph");
        Assert.Equal(TickMargin, tick.Margin);
        Assert.Equal(TickSize.Width, tick.Width);
        Assert.Equal(TickSize.Height, tick.Height);
        Assert.Same(stage.Window.FindResource("Wp81.Glyph.CheckMark"), tick.Data);
        Assert.Same(stage.Window.FindResource("Wp81.Glyph.SelectedEarmark"), Part<Path>(stage.Item, "SelectedEarmark").Data);
    }

    // A tap in the check mode toggles its item, the earmark following; the item's own press, not the box's, even at
    // the top left, where a ListView item's box would be.
    [AvaloniaFact]
    public void In_The_Check_Mode_A_Tap_Toggles_And_The_Top_Left_Is_No_Box()
    {
        using var clock = new TestClock();
        using var stage = new GridStage(mode: SelectionMode.Multiple | SelectionMode.Toggle);
        var one = stage.Item(0);
        var corner = one.TranslatePoint(new Point(10, 20), stage.Window)!.Value;

        var touch = stage.Window.TouchBegin(corner);
        clock.At(SettleMs);
        Assert.False(one.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(one));
        stage.Window.TouchEnd(touch, corner);
        Dispatcher.UIThread.RunJobs();
        Assert.True(one.IsSelected);
        Assert.Equal(1, Part<Grid>(one, "SelectedCheckMark").Opacity);

        Tap(stage.Window, CentreOf(stage.Item(2), stage.Window));
        Assert.Equal(2, stage.List.SelectedItems!.Count);

        Tap(stage.Window, corner);
        Assert.False(one.IsSelected);
        Assert.Equal(0, Part<Grid>(one, "SelectedCheckMark").Opacity);
    }

    // ----- Press -----

    // The probe: Pressed at once; at tile 1's centre the first frame is depth alone (GlobalOffsetZ -24.95), the
    // ListView item's tilt on TiltContainer inside an untilted root.
    [AvaloniaFact]
    public void A_Press_Tilts_The_Tile_As_A_ListView_Items()
    {
        using var clock = new TestClock();
        using var page = new ProbePage();
        var item = page.Item(PressedTile);

        page.Window.TouchBegin(CentreOf(item, page.Window));

        Assert.Contains(":pressed", item.Classes);
        var first = CfTilt.Current(item);
        Assert.Equal(CentreFirstFrame.OffsetZ, first.OffsetZ, LV.DepthTolerance);
        Assert.Equal(0, first.RotationX, LV.AngleTolerance);
        Assert.Equal(0, first.RotationY, LV.AngleTolerance);
        clock.At(SettleMs);
        Assert.IsType<MatrixTransform>(Part<Border>(item, "TiltContainer").RenderTransform);
        Assert.Null(Part<Border>(item, "OuterContainer").RenderTransform);
    }

    // The probe: 6 px from tile 1's left edge the tilt settles at RotationY +8.05°.
    [AvaloniaFact]
    public void A_Press_Near_The_Left_Edge_Settles_At_The_Probes_Reading()
    {
        using var clock = new TestClock();
        using var page = new ProbePage();
        var item = page.Item(PressedTile);

        page.Window.TouchBegin(item.TranslatePoint(new Point(EdgePressX, Tile / 2), page.Window)!.Value);
        clock.At(SettleMs);

        Assert.Equal(EdgeRotationY, CfTilt.Current(item).RotationY, LV.AngleTolerance);
    }

    // ----- Item click -----

    // ItemClick, unread on the GridView, is #148's rule, on ListBoxOptions as the ListView's: a tap clicks its item and
    // selects nothing, in the check mode too.
    [AvaloniaTheory]
    [InlineData(SelectionMode.Single)]
    [InlineData(SelectionMode.Multiple | SelectionMode.Toggle)]
    public void With_Item_Click_On_A_Tap_Clicks_And_Selects_Nothing(SelectionMode mode)
    {
        using var stage = new GridStage(mode: mode);
        ListBoxOptions.SetIsItemClickEnabled(stage.List, true);
        var clicked = new List<object?>();
        stage.List.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => clicked.Add(e.ClickedItem));

        Tap(stage.Window, CentreOf(stage.Item(4), stage.Window));

        Assert.Equal([stage.List.Items[4]], clicked);
        Assert.Empty(stage.List.SelectedItems!);
    }

    // ----- Disabled -----

    // Disabled: the ListViewItem's content at ListViewItemDisabledThemeOpacity, and the selected parts transparent, so a
    // disabled item in the check mode draws no border and no earmark.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Tile_And_Hides_The_Border_And_The_Earmark(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant), new ListBoxItem { Content = TileOf("one"), IsSelected = true, IsEnabled = false });
        stage.Item.SetValue(PhoneListMultiSelect.IsOnProperty, true);
        Dispatcher.UIThread.RunJobs();
        using var frame = stage.Capture();
        var (l, t, r, b) = RawEdges(stage.Item, stage.Window);

        Assert.Equal(LV.DisabledOpacity, Part<ContentPresenter>(stage.Item, "PART_ContentPresenter").Opacity);
        AssertPixel(LV.Page(dark), frame, l + BorderIn, (t + b) / 2, "no border");
        foreach (var (fromRight, fromTop) in EarmarkPoints.Concat(TickPoints))
            AssertPixel(LV.Page(dark), frame, r - fromRight, t + fromTop, "no earmark");
    }

    // ----- The ListView unchanged -----

    // The earmark is the ListViewItem's template's too, but a ListView in the check mode never shows SelectedBorder: its
    // selected item draws the box and no earmark, and its content still goes over.
    [AvaloniaFact]
    public void A_ListView_In_The_Check_Mode_Shows_No_Earmark()
    {
        var list = new ListBox { ItemsSource = new[] { "one", "two" }, SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle };
        using var scene = new Scene(list);
        list.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        var item = (ListBoxItem)list.ContainerFromIndex(0)!;

        Assert.Equal(0, Part<Border>(item, "SelectedBorder").Opacity);
        Assert.Equal(1, Part<Grid>(item, "SelectedCheckMark").Opacity);
        Assert.True(Part<Border>(item, "CheckboxTiltContainer").IsEffectivelyVisible);
    }

    private sealed class PointNear : IEqualityComparer<Point>
    {
        public bool Equals(Point a, Point b) => Math.Abs(a.X - b.X) < 0.001 && Math.Abs(a.Y - b.Y) < 0.001;

        public int GetHashCode(Point p) => 0;
    }
}
