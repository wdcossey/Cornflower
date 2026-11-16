using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.ListViewReadings;
using CfTilt = Cornflower.Controls.Tilt;
using Path = Avalonia.Controls.Shapes.Path;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ListView and ListViewItem (<c>Controls/ListView.axaml</c>, with <c>PhoneListMultiSelect</c>) on Avalonia's
/// ListBox and ListBoxItem, against the phone's styles, the reference app's captures and the press probes
/// (<see cref="ListViewReadings"/>). docs/controls/ListView has the plan and the comparison.
/// </summary>
public class ListViewTests
{
    /// <summary>Long enough for a press to settle: four or five frames.</summary>
    private const double SettleMs = 160;

    /// <summary>The tilt's hold after a release, and its return (TiltModel; the probes read the Button's, to four places).</summary>
    private const double HoldMs = 182.6;

    private const double ReturnMs = 167.0;

    /// <summary>
    /// A ListViewItem on its own on the reference app's stage, 450 × 100, the page's background, in a slot 19 in and 12
    /// down, as the reference app forced its states. <see cref="ListViewFrames"/> saves the comparison's frames on it.
    /// </summary>
    internal sealed class ItemStage : IDisposable
    {
        private readonly Scene _scene;

        public ItemStage(ThemeVariant? variant = null, ListBoxItem? item = null)
        {
            Item = item ?? new ListBoxItem { Content = "list view item" };
            var slot = new Grid { Margin = Slot, Children = { Item } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: ItemStageHeight);
        }

        public ListBoxItem Item { get; }

        public Window Window => _scene.Window;

        /// <summary>A point of the item, in the window.</summary>
        public Point At(double x, double y) => Item.TranslatePoint(new Point(x, y), Window)!.Value;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>A ListView of "one", "two" and "three" on the reference app's 450 × 260 stage, as the Catalog's.</summary>
    internal sealed class ListStage : IDisposable
    {
        private readonly Scene _scene;

        public ListStage(ThemeVariant? variant = null, int selected = -1)
        {
            List = new ListBox { ItemsSource = new[] { "one", "two", "three" }, SelectedIndex = selected };
            var slot = new Grid { Margin = Slot, Children = { List } };
            var stage = new Grid { Children = { slot } };
            stage.Bind(Panel.BackgroundProperty, stage.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(stage, variant, width: StageWidth, height: ListStageHeight);
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public ListBoxItem Item(int index) => (ListBoxItem)List.ContainerFromIndex(index)!;

        public WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    /// <summary>The probes' page: a ListView 19 in and 80 down the 450 × 800 view, its items TextBlocks.</summary>
    private sealed class ProbePage : IDisposable
    {
        private readonly Scene _scene;

        public ProbePage(int count)
        {
            List = new ListBox
            {
                Margin = ProbeListMargin,
                ItemsSource = Enumerable.Range(0, count).Select(i => $"item {i}").ToList(),
            };
            _scene = new Scene(List);
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public ListBoxItem Item(int index) => (ListBoxItem)List.ContainerFromIndex(index)!;

        public void Dispose() => _scene.Dispose();
    }

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name && c.TemplatedParent == control);

    private static Rectangle BoxOf(ListBoxItem item) => Part<Rectangle>(item, "NormalRectangle");

    private static Path TickOf(ListBoxItem item) => Part<Path>(item, "CheckGlyph");

    private static ContentPresenter PresenterOf(ListBoxItem item) => Part<ContentPresenter>(item, "PART_ContentPresenter");

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static string Hex(Color c) => Tokens.Token.Hex(c);

    public static TheoryData<string> Variants => ["Dark", "Light"];

    private static ThemeVariant VariantOf(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Point In(Visual part, Visual of) => part.TranslatePoint(default, of)!.Value;

    /// <summary>How far a part's own transform moves it across: its translation.</summary>
    private static double ShiftOf(Control part) => part.RenderTransform?.Value.M31 ?? 0;

    private static bool IsOn(ListBoxItem item) => PhoneListMultiSelect.GetIsOn(item);

    /// <summary>
    /// Runs Avalonia's frames until the check mode's slide has come to rest on every item given: its transitions move on
    /// the render ticks, by real time, so this ticks until each part is where it runs to, or two seconds pass.
    /// </summary>
    private static void Settle(params ListBoxItem[] items)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        bool Moving() => items.Any(i =>
            Math.Abs(ShiftOf(Part<Grid>(i, "CheckboxContainer")) - (IsOn(i) ? 0 : ContentOffsetX)) > 1e-9 ||
            Math.Abs(ShiftOf(Part<Border>(i, "ContentBorder")) - (IsOn(i) ? ContentTranslateX : 0)) > 1e-9);
        do
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        while (watch.ElapsedMilliseconds < 2000 && Moving());

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static void AssertNear(TiltState expected, TiltState actual, string because)
    {
        Assert.True(
            Math.Abs(expected.RotationX - actual.RotationX) <= AngleTolerance &&
            Math.Abs(expected.RotationY - actual.RotationY) <= AngleTolerance &&
            Math.Abs(expected.OffsetZ - actual.OffsetZ) <= DepthTolerance,
            $"{because}: read {expected}, ours {actual}");
    }

    // ----- The ListView's style -----

    // listview.json's setters, mapped to Avalonia's: IsTabStop, TabNavigation Once, the ScrollViewer's attached values;
    // the phone's ItemsStackPanel is a VirtualizingStackPanel. Its Foreground is what the phone's inherits from the page.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Lists_Style_Sets_What_The_Phone_Read(string variant)
    {
        using var stage = new ListStage(VariantOf(variant));
        var list = stage.List;

        Assert.False(list.IsTabStop);
        Assert.Equal(KeyboardNavigationMode.Once, KeyboardNavigation.GetTabNavigation(list));
        Assert.Equal(ScrollBarVisibility.Disabled, ScrollViewer.GetHorizontalScrollBarVisibility(list));
        Assert.Equal(ScrollBarVisibility.Auto, ScrollViewer.GetVerticalScrollBarVisibility(list));
        Assert.False(ScrollViewer.GetIsDeferredScrollingEnabled(list));
        Assert.True(ScrollViewer.GetBringIntoViewOnFocusChange(list));
        Assert.Equal(Foreground(variant == "Dark"), ColourOf(list.Foreground));
        Assert.Null(list.FocusAdorner);
        Assert.IsType<VirtualizingStackPanel>(list.ItemsPanelRoot);
        list.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Assert.True(PhoneListMultiSelect.GetIsOn(list));
    }

    // The phone's template holds a plain ScrollViewer, with the thin indicators, its values bound from the ListView's
    // attached ones (docs/controls/ScrollViewer, "How a later control holds it"), named as Avalonia's ListBox looks for it.
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
    }

    // ----- The item's style and template -----

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void The_Items_Style_Sets_What_The_Phone_Read(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant));
        var item = stage.Item;

        Assert.Equal(stage.Window.FindResource("ContentControlThemeFontFamily"), item.FontFamily);
        Assert.Equal(FontSize, item.FontSize);
        Assert.Equal(Transparent, ColourOf(item.Background));
        Assert.Equal(KeyboardNavigationMode.Local, KeyboardNavigation.GetTabNavigation(item));
        Assert.Equal(ItemMargin, item.Margin);
        Assert.Equal(HorizontalAlignment.Left, item.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, item.VerticalContentAlignment);
        Assert.Null(item.FocusAdorner);
        Assert.False(item.ClipToBounds);

        Assert.Equal(CheckBoxMargin, Part<Border>(item, "CheckboxTiltContainer").Margin);
        Assert.Equal(Transparent, ColourOf(BoxOf(item).Fill));
        Assert.Equal(Foreground(dark), ColourOf(BoxOf(item).Stroke));
        Assert.Equal(BoxStroke, BoxOf(item).StrokeThickness);
        Assert.Equal(Foreground(dark), ColourOf(TickOf(item).Fill));
        Assert.Same(stage.Window.FindResource("Wp81.Glyph.CheckMark"), TickOf(item).Data);
        Assert.Equal(0, TickOf(item).Opacity);
        var selected = Part<Border>(item, "SelectedBorder");
        Assert.Equal(0, selected.Opacity);
        Assert.Equal(SelectedBorderThickness, selected.BorderThickness);
        Assert.Equal(Accent, ColourOf(selected.BorderBrush));
    }

    // The dump's bounds: three items 39.1667 tall, one under the other; the text's line Segoe WP's, 1.33 em at 12; the
    // check box clipped away to the left, at 9.5833 down; the selected check mark at the right inside the 2.5 border.
    [AvaloniaFact]
    public void The_Bounds_Are_The_Phones_Within_Avalonias_Rounding()
    {
        using var stage = new ListStage(selected: 1);
        var list = stage.List;

        Assert.Equal(InStage.X, In(list, stage.Window).X, 0.001);
        Assert.Equal(InStage.Y, In(list, stage.Window).Y, 0.001);
        Assert.Equal(OurWidth, list.Bounds.Width, 0.001);
        for (var i = 0; i < 3; i++)
        {
            var item = stage.Item(i);
            Assert.Equal(new Rect(0, i * ItemHeight, OurWidth, ItemHeight), item.Bounds.WithY(In(item, list).Y), new RectNear());
            Assert.Equal(default, In(PresenterOf(item), item));
            Assert.Equal(ContentHeight, PresenterOf(item).Bounds.Height, 0.001);
        }

        var two = stage.Item(1);
        var box = Part<Border>(two, "CheckboxTiltContainer");
        Assert.Equal(BoxAt, In(box, two), new PointNear());
        Assert.Equal(OurBox, box.Bounds.Width, 0.001);
        Assert.Equal(OurBox, box.Bounds.Height, 0.001);
        Assert.Equal(ContentOffsetX, ShiftOf(Part<Grid>(two, "CheckboxContainer")), 0.001);
        Assert.Equal(GlyphInBox, In(TickOf(two), BoxOf(two)), new PointNear());
        Assert.Equal(Glyph.Width, TickOf(two).Bounds.Width, 0.001);
        Assert.Equal(Glyph.Height, TickOf(two).Bounds.Height, 0.001);
        var mark = Part<Panel>(two, "SelectedCheckMark");
        Assert.Equal(new Point(OurCheckMarkLeft, CheckMarkTop), In(mark, two), new PointNear());
        Assert.Equal(CheckMark, mark.Bounds.Width, 0.001);
    }

    private sealed class PointNear : IEqualityComparer<Point>
    {
        public bool Equals(Point a, Point b) => Math.Abs(a.X - b.X) < 0.001 && Math.Abs(a.Y - b.Y) < 0.001;

        public int GetHashCode(Point p) => 0;
    }

    private sealed class RectNear : IEqualityComparer<Rect>
    {
        public bool Equals(Rect a, Rect b) =>
            Math.Abs(a.X - b.X) < 0.001 && Math.Abs(a.Y - b.Y) < 0.001 && Math.Abs(a.Width - b.Width) < 0.001 && Math.Abs(a.Height - b.Height) < 0.001;

        public int GetHashCode(Rect r) => 0;
    }

    // The phone's text for a string is Segoe WP's line, 1.33 em at 12 (ADR 0003, through PhoneHeaderLine); the presenter
    // doesn't recognise access keys, so an underscore is drawn, as WinRT's draws it.
    [AvaloniaFact]
    public void A_Strings_Line_Is_The_Phones_And_An_Underscore_Is_Drawn()
    {
        using var stage = new ItemStage(item: new ListBoxItem { Content = "list_view item" });
        var text = PresenterOf(stage.Item).GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.IsNotType<AccessText>(text);
        Assert.Equal("list_view item", text.Text);
        Assert.Equal(1.33 * FontSize, text.LineHeight, 0.01);
        Assert.Equal(ContentHeight, PresenterOf(stage.Item).Bounds.Height, 0.001);
    }

    // ----- Selection -----

    // The Catalog's list has its second item selected, and draws it as the others: a ListView in Single mode shows no
    // selection. The tick turns on (Selected), but the check box is clipped away to the left.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Single_Selection_Draws_Nothing(string variant)
    {
        using var none = new ListStage(VariantOf(variant));
        using var two = new ListStage(VariantOf(variant), selected: 1);
        using var a = none.Capture();
        using var b = two.Capture();

        Assert.True(two.Item(1).IsSelected);
        Assert.Equal(1, TickOf(two.Item(1)).Opacity);
        Assert.True(Frames.SameAs(a, b), "the selected item is drawn differently");
    }

    // WinRT's Multiple is Avalonia's Multiple and Toggle together. Multiple alone, WinRT's Extended, the phone shows with
    // no boxes (#136's probe: leaving Multiple for Extended took every item to NoMultiSelect). Toggle alone is a toggled
    // single selection, with none.
    [AvaloniaTheory]
    [InlineData(SelectionMode.Single, false)]
    [InlineData(SelectionMode.Multiple, false)]
    [InlineData(SelectionMode.Multiple | SelectionMode.AlwaysSelected, false)]
    [InlineData(SelectionMode.Toggle, false)]
    [InlineData(SelectionMode.Single | SelectionMode.AlwaysSelected, false)]
    [InlineData(SelectionMode.Multiple | SelectionMode.Toggle, true)]
    [InlineData(SelectionMode.Multiple | SelectionMode.Toggle | SelectionMode.AlwaysSelected, true)]
    public void The_Check_Mode_Is_Multiple_With_Toggle(SelectionMode mode, bool on)
    {
        using var stage = new ListStage();
        stage.List.SelectionMode = mode;
        Dispatcher.UIThread.RunJobs();

        Assert.All(Enumerable.Range(0, 3), i => Assert.Equal(on, IsOn(stage.Item(i))));
    }

    // Every item is marked: those already there when the mode changes, those made as the list scrolls (recycled or
    // new), an item that is its own container, and none once the mode goes.
    [AvaloniaFact]
    public void Every_Item_Follows_The_Lists_Mode()
    {
        using var page = new ProbePage(60);
        var list = page.List;
        list.Height = 400;
        Dispatcher.UIThread.RunJobs();

        list.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        Assert.All(list.GetRealizedContainers().Cast<ListBoxItem>(), i => Assert.True(IsOn(i)));

        list.ScrollIntoView(55);
        Dispatcher.UIThread.RunJobs();
        Assert.True(IsOn((ListBoxItem)list.ContainerFromIndex(55)!));
        Assert.All(list.GetRealizedContainers().Cast<ListBoxItem>(), i => Assert.True(IsOn(i)));

        list.SelectionMode = SelectionMode.Single;
        Dispatcher.UIThread.RunJobs();
        Assert.All(list.GetRealizedContainers().Cast<ListBoxItem>(), i => Assert.False(IsOn(i)));

        var own = new ListBox { SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle, Items = { new ListBoxItem { Content = "own" } } };
        using var scene = new Scene(own);
        Assert.True(IsOn((ListBoxItem)own.Items[0]!));
    }

    // A container the list recycled while the mode changed comes back in the new mode at once, with no slide: a list
    // that left the check mode while scrolled doesn't show boxes sliding out of the items it brings back.
    [AvaloniaFact]
    public void A_Recycled_Item_Comes_Back_In_The_Lists_Mode_Without_A_Slide()
    {
        using var page = new ProbePage(60);
        var list = page.List;
        list.Height = 400;
        list.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        list.ScrollIntoView(55);
        Dispatcher.UIThread.RunJobs();

        list.SelectionMode = SelectionMode.Single;
        Dispatcher.UIThread.RunJobs();
        list.ScrollIntoView(0);
        Dispatcher.UIThread.RunJobs();

        var first = (ListBoxItem)list.ContainerFromIndex(0)!;
        Assert.False(IsOn(first));
        Assert.Equal(ContentOffsetX, ShiftOf(Part<Grid>(first, "CheckboxContainer")), 0.001);
        Assert.Equal(0, ShiftOf(Part<Border>(first, "ContentBorder")), 0.001);
    }

    // A virtualising panel keeps the focused container when it scrolls out of view, outside the realised ones, and hands
    // it back without preparing it again: it follows a change of mode made meanwhile.
    [AvaloniaFact]
    public void A_Focused_Item_Scrolled_Away_Follows_The_Lists_Mode()
    {
        using var page = new ProbePage(60);
        var list = page.List;
        list.Height = 400;
        Dispatcher.UIThread.RunJobs();
        var first = (ListBoxItem)list.ContainerFromIndex(0)!;
        first.Focus();
        list.ScrollIntoView(55);
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(first, list.GetRealizedContainers());

        list.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        Assert.True(IsOn(first));

        list.ScrollIntoView(0);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(first, list.ContainerFromIndex(0));
        Assert.True(IsOn(first));
    }

    // The mode is the list's, inherited by its items, and a list in an item of a list in the check mode has its own.
    [AvaloniaFact]
    public void A_List_In_An_Item_Has_Its_Own_Mode()
    {
        var inner = new ListBox { ItemsSource = new[] { "a", "b" } };
        var outer = new ListBox { SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle, Items = { new ListBoxItem { Content = inner } } };
        using var scene = new Scene(outer);

        Assert.True(IsOn((ListBoxItem)outer.ContainerFromIndex(0)!));
        Assert.All(inner.GetRealizedContainers().Cast<ListBoxItem>(), i => Assert.False(IsOn(i)));
    }

    // An item outside a list is never in the check mode, and a list in a theme of the app's own lets its items go.
    [AvaloniaFact]
    public void An_Item_Outside_A_List_Or_In_Another_Theme_Isnt_In_The_Check_Mode()
    {
        using var stage = new ItemStage();
        Assert.False(IsOn(stage.Item));

        using var list = new ListStage();
        list.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        Assert.True(IsOn(list.Item(0)));

        list.List.Theme = new SimpleTheme().TryGetResource(typeof(ListBox), ThemeVariant.Dark, out var simple)
            ? (ControlTheme)simple!
            : throw new InvalidOperationException("No ListBox theme in Simple.");
        Dispatcher.UIThread.RunJobs();
        Assert.False(PhoneListMultiSelect.GetIsOn(list.List));
        Assert.All(list.List.GetRealizedContainers().Cast<ListBoxItem>(), i => Assert.False(IsOn(i)));
    }

    // Items that are their own containers are the same objects under the app's theme: they are unmarked as it comes.
    [AvaloniaFact]
    public void Own_Items_Lose_The_Check_Mode_With_The_Lists_Theme()
    {
        var mine = new ListBoxItem { Content = "own" };
        var list = new ListBox { SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle, Items = { mine } };
        using var scene = new Scene(list);
        Assert.True(IsOn(mine));

        list.Theme = new SimpleTheme().TryGetResource(typeof(ListBox), ThemeVariant.Dark, out var simple)
            ? (ControlTheme)simple!
            : throw new InvalidOperationException("No ListBox theme in Simple.");
        Dispatcher.UIThread.RunJobs();

        Assert.False(IsOn(mine));
    }

    // ListMultiSelect: the CheckboxContainer comes from -40.5 to 0 and the ContentBorder goes 40.5 to the right, both over
    // the phone's 0.15 s (GeneratedDuration, linear), and back. The offsets are the tokens' values.
    [AvaloniaFact]
    public void In_The_Check_Mode_The_Box_Comes_In_And_The_Content_Goes_Over()
    {
        using var stage = new ListStage();
        var item = stage.Item(0);
        var box = Part<Grid>(item, "CheckboxContainer");
        var content = Part<Border>(item, "ContentBorder");
        Assert.Equal(ContentOffsetX, (double)stage.Window.FindResource("ListViewItemContentOffsetX")!);
        Assert.Equal(ContentTranslateX, (double)stage.Window.FindResource("ListViewItemContentTranslateX")!);

        foreach (var part in new Control[] { box, content })
        {
            var slide = Assert.IsType<TransformOperationsTransition>(Assert.Single(part.Transitions!));
            Assert.Equal(Visual.RenderTransformProperty, slide.Property);
            Assert.Equal(MultiSelectDuration, slide.Duration);
            Assert.IsType<LinearEasing>(slide.Easing);
        }

        stage.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Settle(item);
        Assert.Equal(0, ShiftOf(box), 0.001);
        Assert.Equal(ContentTranslateX, ShiftOf(content), 0.001);
        Assert.Equal(new Point(ContentTranslateX, 0), PresenterOf(item).TranslatePoint(default, item)!.Value, new PointNear());

        stage.List.SelectionMode = SelectionMode.Single;
        Settle(item);
        Assert.Equal(ContentOffsetX, ShiftOf(box), 0.001);
        Assert.Equal(0, ShiftOf(content), 0.001);
    }

    // The forced ListMultiSelect captures: the box's border, 6 raw pixels, its left side in columns 46-51 and its top in
    // rows 52-57, the foreground on the page; ours is 62 raw pixels wide, the phone's 61. Unchecked, the box is empty;
    // selected, the tick is drawn in it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void In_The_Check_Mode_The_Box_Is_Drawn_Where_The_Phone_Draws_It(string variant)
    {
        var dark = variant == "Dark";
        foreach (var isSelected in new[] { false, true })
        {
            var item = new ListBoxItem { Content = "list view item", IsSelected = isSelected };
            item.SetValue(PhoneListMultiSelect.IsOnProperty, true);         // before it is shown, as a forced state: no slide
            using var stage = new ItemStage(VariantOf(variant), item);
            using var frame = stage.Capture();

            foreach (var x in LeftColumns.Concat(RightColumns))
                Assert.True(Foreground(dark) == Frames.PixelAt(frame, x, 80), $"column {x}: {Hex(Frames.PixelAt(frame, x, 80))}");
            foreach (var y in TopRows)
                Assert.True(Foreground(dark) == Frames.PixelAt(frame, 76, y), $"row {y}: {Hex(Frames.PixelAt(frame, 76, y))}");
            Assert.Equal(Page(dark), Frames.PixelAt(frame, 45, 80));
            // Inside the tick's long arm, 12.2 across and 8 down the glyph: raw (84, 81).
            Assert.Equal(isSelected ? Foreground(dark) : Page(dark), Frames.PixelAt(frame, 84, 81));
        }
    }

    // At rest the box is 40.5 to the left of its own 25.5 clip: the base capture draws nothing left of the item. The
    // item doesn't clip to its bounds, as the phone's doesn't, and this one, on its own, has no list round it: the box's
    // own clip is all that hides it.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void At_Rest_The_Box_Is_Clipped_Away(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant));
        using var frame = stage.Capture();

        for (var y = TopRows[0]; y < TopRows[0] + 62; y++)
        for (var x = 0; x < LeftColumns[0]; x++)
            Assert.True(Page(dark) == Frames.PixelAt(frame, x, y), $"raw ({x}, {y}): {Hex(Frames.PixelAt(frame, x, y))}");
    }

    // A tap checks an item and another unchecks it, the tick following: WinRT's Multiple, on Avalonia's Toggle.
    [AvaloniaFact]
    public void In_The_Check_Mode_A_Tap_Checks_And_Unchecks()
    {
        using var stage = new ListStage();
        stage.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var two = stage.Item(1);
        var at = two.TranslatePoint(new Point(200, 20), stage.Window)!.Value;

        var touch = stage.Window.TouchBegin(at);
        stage.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();
        Assert.True(two.IsSelected);
        Assert.Equal(1, TickOf(two).Opacity);

        var three = stage.Item(2);
        var at3 = three.TranslatePoint(new Point(200, 20), stage.Window)!.Value;
        touch = stage.Window.TouchBegin(at3);
        stage.Window.TouchEnd(touch, at3);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, stage.List.SelectedItems!.Count);

        touch = stage.Window.TouchBegin(at);
        stage.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();
        Assert.False(two.IsSelected);
        Assert.Equal(0, TickOf(two).Opacity);
    }

    // ----- Press -----

    // ReorderHintContent, the template's Grid, has a transparent background, so the whole item takes a tap even with no
    // Background of its own.
    [AvaloniaFact]
    public void A_Tap_Anywhere_On_The_Item_Selects_It_Even_With_No_Background()
    {
        using var stage = new ListStage();
        var two = stage.Item(1);
        two.Background = null;
        Dispatcher.UIThread.RunJobs();
        var at = two.TranslatePoint(new Point(300, 30), stage.Window)!.Value;

        var touch = stage.Window.TouchBegin(at);
        stage.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, stage.List.SelectedIndex);
    }

    // Pressed runs the tilt on TiltContainer, the whole content, inside a root that stays untilted and takes the hits.
    [AvaloniaFact]
    public void A_Press_Tilts_The_Content_Inside_An_Untilted_Root()
    {
        using var clock = new TestClock();
        using var stage = new ItemStage();
        var item = stage.Item;
        var tilt = Part<Border>(item, "TiltContainer");

        stage.Window.TouchBegin(stage.At(200, 20));
        clock.At(SettleMs);

        Assert.True(CfTilt.GetIsEnabled(item));
        Assert.Same(tilt, CfTilt.TargetOf(item));
        Assert.IsType<MatrixTransform>(tilt.RenderTransform);
        Assert.Null(Part<Border>(item, "OuterContainer").RenderTransform);
        Assert.Null(Part<Border>(item, "CheckboxTiltContainer").RenderTransform);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(item));
    }

    // CheckboxPressed, a press on the box in the check mode: the box fills with the accent, the tick is drawn over it in
    // PhoneTextOverAccentColor, and the box tilts, the content doesn't.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void A_Press_On_The_Box_Fills_It_And_Tilts_The_Box_Alone(string variant)
    {
        using var clock = new TestClock();
        using var stage = new ItemStage(VariantOf(variant), new ListBoxItem { Content = "list view item", IsSelected = true });
        var item = stage.Item;
        item.SetValue(PhoneListMultiSelect.IsOnProperty, true);
        Dispatcher.UIThread.RunJobs();
        var box = Part<Border>(item, "CheckboxTiltContainer");

        stage.Window.TouchBegin(box.TranslatePoint(new Point(12, 12), stage.Window)!.Value);
        clock.At(SettleMs);

        Assert.True(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        Assert.Equal(Accent, ColourOf(BoxOf(item).Fill));
        Assert.Equal(OverAccent, ColourOf(TickOf(item).Fill));
        Assert.IsType<MatrixTransform>(box.RenderTransform);
        Assert.NotEqual(TiltState.Rest, PhoneListMultiSelect.BoxTilt(item));
        Assert.Null(Part<Border>(item, "TiltContainer").RenderTransform);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
    }

    // The probe's press held at item 1's box's centre: the box settles at the tilt the phone's held, about its own centre
    // with the item's eye, the first frame the depth with almost no angle.
    [AvaloniaFact]
    public void A_Press_On_The_Box_Settles_At_The_Probes_Reading()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);

        page.Window.TouchBegin(BoxPress);
        var first = PhoneListMultiSelect.BoxTilt(item);
        clock.At(SettleMs);
        var held = PhoneListMultiSelect.BoxTilt(item);

        Assert.True(
            Math.Abs(BoxHeld.RotationX - held.RotationX) <= BoxAngleTolerance &&
            Math.Abs(BoxHeld.RotationY - held.RotationY) <= BoxAngleTolerance &&
            Math.Abs(BoxHeld.OffsetZ - held.OffsetZ) <= BoxDepthTolerance,
            $"held: read {BoxHeld}, ours {held}");
        Assert.True(
            Math.Abs(first.RotationX) < 0.5 && Math.Abs(first.RotationY) < 0.5 &&
            Math.Abs(BoxFirstFrame.OffsetZ - first.OffsetZ) <= BoxDepthTolerance,
            $"first frame: read {BoxFirstFrame}, ours {first}");
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
    }

    // The probe: on release the box is back at rest at once, its colours too, with no hold and no return, and the
    // content never tilts.
    [AvaloniaFact]
    public void On_Release_The_Box_Is_At_Rest_At_Once()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var box = Part<Border>(item, "CheckboxTiltContainer");

        var touch = page.Window.TouchBegin(BoxPress);
        clock.At(600);
        Assert.NotEqual(TiltState.Rest, PhoneListMultiSelect.BoxTilt(item));
        page.Window.TouchEnd(touch, BoxPress);
        var released = clock.Now.TotalMilliseconds;

        Assert.Equal(TiltState.Rest, PhoneListMultiSelect.BoxTilt(item));
        Assert.Null(box.RenderTransform);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        Assert.Equal(Transparent, ColourOf(BoxOf(item).Fill));
        for (var t = 16.0; t <= HoldMs + ReturnMs + 40; t += 16)
        {
            clock.At(released + t);
            Assert.Null(box.RenderTransform);
            Assert.Equal(TiltState.Rest, CfTilt.Current(item));
        }

        // The mouse's left button, the same.
        page.Window.MouseDown(BoxPress, MouseButton.Left);
        clock.At(clock.Now.TotalMilliseconds + SettleMs);
        Assert.NotNull(box.RenderTransform);
        page.Window.MouseUp(BoxPress, MouseButton.Left);
        Assert.Null(box.RenderTransform);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
    }

    // A pan from a press on the box takes the pointer from the item: the box is at rest at once, and nothing is checked.
    [AvaloniaFact]
    public void A_Pan_From_The_Box_Puts_It_At_Rest_At_Once()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var box = Part<Border>(item, "CheckboxTiltContainer");

        var touch = page.Window.TouchBegin(BoxPress);
        clock.At(300);
        Assert.NotNull(box.RenderTransform);
        for (var step = 1; step <= 8; step++)
        {
            page.Window.TouchMove(touch, BoxPress - new Point(0, 10 * step));
            clock.At(300 + 20 * step);
        }

        Assert.True(page.List.Scroll!.Offset.Y > 0, "the drag didn't pan the list");
        Assert.Null(box.RenderTransform);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        page.Window.TouchEnd(touch, BoxPress - new Point(0, 80));
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(page.List.SelectedItems!);
    }

    // The list leaving the check mode while its box is pressed takes the box's press with it: at rest, unfilled.
    [AvaloniaFact]
    public void Leaving_The_Check_Mode_Puts_A_Pressed_Box_At_Rest()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var box = Part<Border>(item, "CheckboxTiltContainer");

        page.Window.TouchBegin(BoxPress);
        clock.At(300);
        Assert.NotNull(box.RenderTransform);
        page.List.SelectionMode = SelectionMode.Single;
        Dispatcher.UIThread.RunJobs();

        Assert.Null(box.RenderTransform);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        Assert.Equal(Transparent, ColourOf(BoxOf(item).Fill));
    }

    // The item disabled while its box is pressed: the box at rest, unfilled.
    [AvaloniaFact]
    public void Disabling_The_Item_Puts_A_Pressed_Box_At_Rest()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var box = Part<Border>(item, "CheckboxTiltContainer");

        page.Window.TouchBegin(BoxPress);
        clock.At(300);
        Assert.NotNull(box.RenderTransform);
        page.List.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Null(box.RenderTransform);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
    }

    // One pointer at a time, as Tilt's: a second finger on the box while the first holds the item's text changes nothing
    // (the content keeps its tilt, the box stays flat); and while the box is pressed, a second finger on the text doesn't
    // tilt the content. Once the box is let go, the item tilts again.
    [AvaloniaFact]
    public void A_Second_Finger_Neither_Takes_Nor_Adds_A_Press()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var box = Part<Border>(item, "CheckboxTiltContainer");
        var text = CheckModePresses[0].Press;

        var first = page.Window.TouchBegin(text);
        clock.At(SettleMs);
        var held = CfTilt.Current(item);
        Assert.NotEqual(TiltState.Rest, held);
        var second = page.Window.TouchBegin(BoxPress);
        clock.At(2 * SettleMs);
        Assert.Equal(held, CfTilt.Current(item));
        Assert.Null(box.RenderTransform);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        page.Window.TouchEnd(second, BoxPress);
        page.Window.TouchEnd(first, text);
        clock.At(2 * SettleMs + HoldMs + ReturnMs + 40);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));

        first = page.Window.TouchBegin(BoxPress);
        clock.At(clock.Now.TotalMilliseconds + SettleMs);
        Assert.NotNull(box.RenderTransform);
        second = page.Window.TouchBegin(text);
        clock.At(clock.Now.TotalMilliseconds + SettleMs);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
        Assert.Null(Part<Border>(item, "TiltContainer").RenderTransform);
        page.Window.TouchEnd(second, text);
        page.Window.TouchEnd(first, BoxPress);
        Assert.Null(box.RenderTransform);

        page.Window.TouchBegin(text);
        clock.At(clock.Now.TotalMilliseconds + SettleMs);
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(item));
    }

    // The probe: in the check mode a press anywhere off the box is the Single mode's, at the same tilts: the content
    // tilts, the box stays flat and unfilled; on release the tilt holds, then eases back.
    public static TheoryData<int> CheckModePressIndexes => new(Enumerable.Range(0, CheckModePresses.Length));

    [AvaloniaTheory]
    [MemberData(nameof(CheckModePressIndexes))]
    public void In_The_Check_Mode_A_Press_Off_The_Box_Is_The_Single_Modes(int index)
    {
        var (name, press, held) = CheckModePresses[index];
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        page.List.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item = page.Item(1);
        var box = Part<Border>(item, "CheckboxTiltContainer");

        var touch = page.Window.TouchBegin(press);
        clock.At(600);
        var ours = CfTilt.Current(item);

        AssertNear(held, ours, name);
        Assert.Same(Part<Border>(item, "TiltContainer"), CfTilt.TargetOf(item));
        Assert.Null(box.RenderTransform);
        Assert.Equal(TiltState.Rest, PhoneListMultiSelect.BoxTilt(item));
        Assert.False(item.GetValue(PhoneListMultiSelect.IsBoxPressedProperty));
        Assert.Equal(Transparent, ColourOf(BoxOf(item).Fill));

        page.Window.TouchEnd(touch, press);
        var released = clock.Now.TotalMilliseconds;
        clock.At(released + HoldMs - 20);
        Assert.Equal(ours, CfTilt.Current(item));
        clock.At(released + HoldMs + ReturnMs + 40);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
    }

    // The probe's taps in the check mode, in its order: every tap toggles its item, on the box or off it, at the release.
    [AvaloniaFact]
    public void Every_Tap_In_The_Check_Mode_Toggles_Its_Item()
    {
        using var page = new ProbePage(40);
        var list = page.List;
        list.SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle;
        Dispatcher.UIThread.RunJobs();
        var item4Box = BoxPress + new Point(0, 3 * ItemHeight);

        (Point At, int Item, bool Selected, int Count)[] taps =
        [
            (BoxPress, 1, true, 1),
            (CheckModePresses[0].Press, 1, false, 0),
            (CheckModePresses[1].Press, 1, true, 1),
            (CheckModePresses[2].Press, 1, false, 0),
            (CheckModePresses[3].Press, 1, true, 1),
            (item4Box, 4, true, 2),
            (new Point(225, 256.656), 4, false, 1),
            (new Point(225, 295.803), 5, true, 2),
        ];
        foreach (var (at, index, selected, count) in taps)
        {
            var touch = page.Window.TouchBegin(at);
            Assert.Equal(!selected, page.Item(index).IsSelected);
            page.Window.TouchEnd(touch, at);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(selected, page.Item(index).IsSelected);
            Assert.Equal(count, list.SelectedItems!.Count);
        }
    }

    // The probe: Extended, Avalonia's Multiple, under a tap is Single, with no boxes: with items 1 and 5 selected, a tap
    // on item 1 keeps it alone; a tap on another replaces the selection; a tap on the selected item changes nothing.
    // Each press tilts the content as in Single.
    [AvaloniaFact]
    public void Extended_Under_A_Tap_Is_Single()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        var list = page.List;
        list.SelectionMode = SelectionMode.Multiple;
        list.SelectedItems!.Add("item 1");
        list.SelectedItems.Add("item 5");
        Dispatcher.UIThread.RunJobs();
        Assert.False(IsOn(page.Item(1)));

        (Point At, int Item)[] taps = [(CheckModePresses[0].Press, 1), (Presses[1].Press, 2), (Presses[1].Press, 2)];
        foreach (var (at, index) in taps)
        {
            var touch = page.Window.TouchBegin(at);
            clock.At(clock.Now.TotalMilliseconds + 70);
            Assert.NotEqual(TiltState.Rest, CfTilt.Current(page.Item(index)));
            page.Window.TouchEnd(touch, at);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new object[] { $"item {index}" }, list.SelectedItems.Cast<object>());
            clock.At(clock.Now.TotalMilliseconds + 600);
        }
    }

    // The probe: a SelectedIndex set from code scrolls the list at once, by the least that brings the item into view (its
    // bottom on the list's, or its top on the list's), with no animation: the offset doesn't move on later frames.
    [AvaloniaFact]
    public void A_SelectedIndex_Set_From_Code_Scrolls_At_Once_By_The_Least()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        var list = page.List;
        var scroll = list.Scroll!;
        var height = page.Item(0).Bounds.Height;
        var view = scroll.Viewport.Height;
        Assert.Equal(720, view, 0.5);

        foreach (var (index, offset) in SelectedFromCode)
        {
            var before = scroll.Offset.Y;
            var top = index * height;
            var expected = top < before ? top : top + height > before + view ? top + height - view : before;

            list.SelectedIndex = index;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(expected, scroll.Offset.Y, 0.5);
            Assert.Equal(offset, scroll.Offset.Y, 1.0);

            for (var frame = 1; frame <= 20; frame++)
            {
                clock.At(clock.Now.TotalMilliseconds + 16);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(expected, scroll.Offset.Y, 0.5);
            }
        }
    }

    // Outside the check mode a press changes no colour: the phone's Pressed state runs only the tilt.
    [AvaloniaFact]
    public void Outside_The_Check_Mode_A_Press_Changes_No_Colour()
    {
        using var clock = new TestClock();
        using var stage = new ItemStage();
        stage.Window.TouchBegin(stage.At(200, 20));
        clock.At(SettleMs);

        Assert.Equal(Transparent, ColourOf(BoxOf(stage.Item).Fill));
        Assert.Equal(Foreground(true), ColourOf(TickOf(stage.Item).Fill));
    }

    // Each held press of the probes settles at the tilt the phone held, within the model's 0.1° and 0.15 of depth.
    public static TheoryData<int> PressIndexes => new(Enumerable.Range(0, Presses.Length));

    [AvaloniaTheory]
    [MemberData(nameof(PressIndexes))]
    public void A_Held_Press_Settles_At_The_Probes_Reading(int index)
    {
        var (name, press, itemIndex, held) = Presses[index];
        using var clock = new TestClock();
        using var page = new ProbePage(12);
        var item = page.Item(itemIndex);
        Assert.Equal(ProbeListMargin.Top + itemIndex * ItemHeight, In(item, page.Window).Y, 0.001);

        page.Window.TouchBegin(press);
        clock.At(SettleMs);

        Assert.Contains(":pressed", item.Classes);
        AssertNear(held, CfTilt.Current(item), name);
    }

    // The probes: Pressed comes in the same input dispatch as the press, with no delay to tell a tap from a pan; a tap
    // selects with the release, as ItemClick and Tapped came with it.
    [AvaloniaFact]
    public void The_Press_Is_At_Once_And_A_Tap_Selects_On_Its_Release()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(12);
        var item = page.Item(4);
        var at = Presses[^1].Press;

        var touch = page.Window.TouchBegin(at);
        Assert.Contains(":pressed", item.Classes);
        Assert.Equal(-1, page.List.SelectedIndex);
        clock.At(80);
        page.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain(":pressed", item.Classes);
        Assert.Equal(4, page.List.SelectedIndex);
    }

    // The probes: the release holds the tilt 182.6 ms, then eases it back over 167.0 ms: the Button's.
    [AvaloniaFact]
    public void A_Release_Holds_Then_Eases_Back_As_The_Buttons()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(12);
        var item = page.Item(2);
        var at = Presses[1].Press;

        var touch = page.Window.TouchBegin(at);
        clock.At(600);
        var held = CfTilt.Current(item);
        page.Window.TouchEnd(touch, at);
        var released = clock.Now.TotalMilliseconds;

        clock.At(released + HoldMs - 20);
        Assert.Equal(held, CfTilt.Current(item));
        clock.At(released + HoldMs + ReturnMs / 2);
        Assert.NotEqual(held, CfTilt.Current(item));
        Assert.NotEqual(TiltState.Rest, CfTilt.Current(item));
        clock.At(released + HoldMs + ReturnMs + 40);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));
    }

    // The 40-item probe: a press held and then dragged up pans the list; the pan takes the pointer from the item, which
    // holds its tilt and returns as after a release, timed from the pointer lost, and nothing is selected.
    [AvaloniaFact]
    public void A_Pan_Takes_The_Press_The_Tilt_Returns_And_Nothing_Is_Selected()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        var item = page.Item(3);
        var at = Presses[2].Press;
        double? lost = null;
        item.AddHandler(InputElement.PointerCaptureLostEvent, (_, _) => lost ??= clock.Now.TotalMilliseconds, RoutingStrategies.Direct, handledEventsToo: true);

        var touch = page.Window.TouchBegin(at);
        clock.At(300);
        var held = CfTilt.Current(item);
        Assert.NotEqual(TiltState.Rest, held);
        for (var step = 1; step <= 8 && lost is null; step++)
        {
            page.Window.TouchMove(touch, at - new Point(0, 10 * step));
            clock.At(300 + 20 * step);
        }

        Assert.NotNull(lost);
        Assert.DoesNotContain(":pressed", item.Classes);
        Assert.True(page.List.Scroll!.Offset.Y > 0, "the drag didn't pan the list");
        var atLoss = CfTilt.Current(item);
        Assert.NotEqual(TiltState.Rest, atLoss);
        clock.At(lost.Value + HoldMs - 20);
        Assert.Equal(atLoss, CfTilt.Current(item));
        clock.At(lost.Value + HoldMs + ReturnMs + 40);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));

        page.Window.TouchEnd(touch, at - new Point(0, 80));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(-1, page.List.SelectedIndex);
    }

    // The 12-item probe: where the list can't pan, the tilt follows the finger while it is on the item, holds and returns
    // once it leaves, and a release off the item selects nothing.
    [AvaloniaFact]
    public void Dragged_Off_The_Item_The_Tilt_Returns_And_Nothing_Is_Selected()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(12);
        var item = page.Item(2);
        var at = Presses[1].Press;

        var touch = page.Window.TouchBegin(at);
        clock.At(300);
        var centre = CfTilt.Current(item);
        page.Window.TouchMove(touch, at + new Point(0, 12));            // still on item 2, 90% down
        clock.At(460);
        var lower = CfTilt.Current(item);
        Assert.True(lower.RotationX > centre.RotationX + 4, $"the tilt didn't follow the finger: {centre} then {lower}");

        page.Window.TouchMove(touch, at + new Point(0, 60));            // on item 3
        var left = clock.Now.TotalMilliseconds;
        clock.At(left + HoldMs - 20);
        Assert.Equal(lower, CfTilt.Current(item));
        clock.At(left + HoldMs + ReturnMs + 40);
        Assert.Equal(TiltState.Rest, CfTilt.Current(item));

        page.Window.TouchEnd(touch, at + new Point(0, 60));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(-1, page.List.SelectedIndex);
        Assert.Equal(0, page.List.Scroll!.Offset.Y);
    }

    // The probes: a press moved 10 px sideways is still a tap, and the tilt follows it.
    [AvaloniaFact]
    public void A_Press_Moved_Ten_Pixels_Sideways_Is_Still_A_Tap()
    {
        using var clock = new TestClock();
        using var page = new ProbePage(40);
        var item = page.Item(2);
        var at = Presses[1].Press;

        var touch = page.Window.TouchBegin(at);
        clock.At(100);
        for (var step = 1; step <= 10; step++)
        {
            page.Window.TouchMove(touch, at + new Point(step, 0));
            clock.At(100 + 30 * step);
        }

        Assert.True(CfTilt.Current(item).RotationY < -0.3, $"the tilt didn't follow: {CfTilt.Current(item)}");
        page.Window.TouchEnd(touch, at + new Point(10, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, page.List.SelectedIndex);
    }

    // ----- Disabled -----

    // Disabled dims the content to ListViewItemDisabledThemeOpacity, Update 1's 0.4: the text's stems #666666 on black,
    // as the capture draws them, and the box's border and tick to PhoneBaseLowColor.
    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void Disabled_Dims_The_Content_And_The_Box(string variant)
    {
        var dark = variant == "Dark";
        using var stage = new ItemStage(VariantOf(variant), new ListBoxItem { Content = "list view item", IsEnabled = false });
        var item = stage.Item;

        Assert.Equal(DisabledOpacity, PresenterOf(item).Opacity);
        Assert.Equal(Disabled(dark), ColourOf(BoxOf(item).Stroke));
        Assert.Equal(Disabled(dark), ColourOf(TickOf(item).Fill));

        // Text isn't compared by pixel (the fonts differ), but its fullest ink is: the enabled text's at 0.4 on the page,
        // as the capture's #666666 stems are white at 0.4 on black.
        using var frame = stage.Capture();
        using var enabled = new ItemStage(VariantOf(variant));
        using var enabledFrame = enabled.Capture();
        var page = dark ? 0 : 255;
        var full = FullestInk(enabledFrame, PresenterOf(enabled.Item), enabled.Window, dark) - page;
        var dimmed = FullestInk(frame, PresenterOf(item), stage.Window, dark) - page;
        // Within 8 of 255: Skia's text coverage under an opacity isn't exactly linear (83 where 0.4 × 222 is 88.8).
        Assert.True(Math.Abs(dimmed - DisabledOpacity * full) <= 8, $"the text's fullest ink is {dimmed} from the page, enabled {full}");
    }

    /// <summary>The red channel furthest from the page inside <paramref name="part"/>: the brightest on black, the darkest on white.</summary>
    private static int FullestInk(WriteableBitmap frame, Visual part, Visual window, bool dark)
    {
        var at = part.TranslatePoint(default, window)!.Value * Scene.ReferenceScaling;
        var size = part.Bounds.Size * Scene.ReferenceScaling;
        var reds = new List<int>();
        for (var y = (int)at.Y; y < (int)(at.Y + size.Height); y++)
        for (var x = (int)at.X; x < (int)(at.X + size.Width); x++)
            reds.Add(Frames.PixelAt(frame, x, y).R);
        return dark ? reds.Max() : reds.Min();
    }

    // ----- Variant and focus -----

    [AvaloniaFact]
    public void The_Colours_Follow_A_Change_Of_Variant()
    {
        using var stage = new ItemStage();
        Assert.Equal(Foreground(true), ColourOf(BoxOf(stage.Item).Stroke));

        stage.Window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Foreground(false), ColourOf(BoxOf(stage.Item).Stroke));
    }

    // The phone's template has no FocusStates, and its PointerOver sets nothing: keyboard focus and a mouse over an item
    // draw nothing.
    [AvaloniaFact]
    public void Keyboard_Focus_And_The_Pointer_Over_Draw_Nothing()
    {
        using var stage = new ListStage();
        using var before = stage.Capture();

        stage.Item(1).Focus(NavigationMethod.Tab);
        stage.Window.MouseMove(stage.Item(2).TranslatePoint(new Point(100, 20), stage.Window)!.Value);
        Dispatcher.UIThread.RunJobs();
        using var after = stage.Capture();

        Assert.True(stage.Item(1).IsFocused);
        Assert.Contains(":pointerover", stage.Item(2).Classes);
        Assert.True(Frames.SameAs(before, after), "focus or the pointer drew something");
    }
}
