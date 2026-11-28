using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using CfTilt = Cornflower.Controls.Tilt;
using LV = Cornflower.Tests.Controls.ListViewReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Grouped lists (#131): <see cref="ListBoxOptions.GroupedItemsSource"/>, <see cref="ListViewHeaderItem"/> and its
/// theme (<c>Controls/ListViewHeaderItem.axaml</c>), and the ListView's pinned header, against #211's probe of a grouped
/// ListView over a grouped GridView (<c>tools/refapp/README.md</c>, "Tier 4's reads", Grouped lists: runs
/// <c>20261118-2221-dev11</c> and, recorded, <c>-2249-</c>). docs/controls/GroupedLists has the plan.
/// </summary>
public class GroupedListTests
{
    // ----- The probe's readings -----

    /// <summary>The probe's page: each list 19 in, 330 tall; the ListView's top at 70 in the window.</summary>
    private static readonly Thickness ProbeListMargin = new(19, 70, 19, 0);
    private const double ProbeListHeight = 330;

    /// <summary>A header in <c>GroupHeaderTextBlockStyle</c>: the list's width (412.08) and its text's height.</summary>
    private const double HeaderHeight = 30;

    /// <summary>The ListView: header a at 70, its first item at 100, header c (b is empty) at 295.8.</summary>
    private const double HeaderATop = 70;
    private const double FirstItemTop = 100;
    private const double HeaderCTop = 295.8;

    /// <summary>The GridView: a group's five items in one row, the headers 69.2 apart.</summary>
    private const double GridHeaderPitch = 69.2;

    /// <summary>
    /// A press on header a, 20 from its left and halfway down, held: <c>RotationX</c> -4.8, <c>RotationY</c> +7.6,
    /// <c>GlobalOffsetZ</c> -6.8. Ours, the Button's tilt on a 412 x 30 header, is -4.79, +8.11, -6.24: within 0.6.
    /// </summary>
    private static readonly Point HeaderPress = new(20, HeaderHeight / 2);
    private static readonly TiltState HeaderHeld = new(-4.8, 7.6, -6.8);
    private const double HeaderAngleTolerance = 0.6;
    private const double HeaderDepthTolerance = 0.6;

    /// <summary>Layout is read to a raw pixel at 2.4, and the probe's numbers to a tenth.</summary>
    private const double Tolerance = 0.5;

    /// <summary>A group of the probe's: its letter, and five items, or none for b and e.</summary>
    internal sealed class Letter(string key, IEnumerable<string> items) : ObservableCollection<string>(items)
    {
        public string Key { get; } = key;
    }

    private static List<Letter> Letters() =>
        "abcdefgh".Select(ch => new Letter(ch.ToString(),
            ch is 'b' or 'e' ? [] : Enumerable.Range(1, 5).Select(i => $"{ch} item {i}"))).ToList();

    /// <summary>The probe's header template: the key, in <c>GroupHeaderTextBlockStyle</c>.</summary>
    private static IDataTemplate HeaderTemplate() => new FuncDataTemplate<Letter>((letter, _) =>
    {
        var text = new TextBlock { Text = letter.Key };
        text.Bind(StyledElement.ThemeProperty, text.GetResourceObservable("GroupHeaderTextBlockStyle"));
        return text;
    });

    /// <summary>The probe's page with one grouped list, a ListView or, by its key, a GridView.</summary>
    internal sealed class ProbePage : IDisposable
    {
        private readonly Scene _scene;

        public ProbePage(bool grid = false, IEnumerable? groups = null, SelectionMode mode = SelectionMode.Single, bool late = false)
        {
            List = new ListBox
            {
                Margin = ProbeListMargin,
                Height = ProbeListHeight,
                VerticalAlignment = VerticalAlignment.Top,
                SelectionMode = mode,
                AutoScrollToSelectedItem = false,
            };
            if (grid)
            {
                List.Bind(StyledElement.ThemeProperty, List.GetResourceObservable("Wp81GridViewTheme"));
            }

            ListBoxOptions.SetGroupHeaderTemplate(List, HeaderTemplate());
            if (!late)
            {
                ListBoxOptions.SetGroupedItemsSource(List, groups ?? Letters());
            }

            // On the page's brush, as the probe's page.
            var page = new Panel { Children = { List } };
            page.Bind(Panel.BackgroundProperty, page.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            _scene = new Scene(page);
            if (late)
            {
                ListBoxOptions.SetGroupedItemsSource(List, groups ?? Letters());
            }

            Settle();
        }

        public ListBox List { get; }

        public Window Window => _scene.Window;

        public ScrollViewer Scroll => (ScrollViewer)List.Scroll!;

        /// <summary>The template's host of the pinned header, and the header in it.</summary>
        public Border StickyHost => List.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_StickyHeader");

        public ListViewHeaderItem Sticky => Assert.IsType<ListViewHeaderItem>(StickyHost.Child);

        public Control Container(int index) => List.ContainerFromIndex(index)!;

        public ListViewHeaderItem Header(int index) => Assert.IsType<ListViewHeaderItem>(Container(index));

        public double Top(Control control) => control.TranslatePoint(default, Window)!.Value.Y;

        public void ScrollTo(double offset)
        {
            Scroll.Offset = new Vector(0, offset);
            Settle();
        }

        public void Settle()
        {
            for (var i = 0; i < 3; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
        }

        public Avalonia.Media.Imaging.WriteableBitmap Capture() => _scene.Capture();

        public void Dispose() => _scene.Dispose();
    }

    // Flattened, groups a, c, d, f, g, h: header a 0, its items 1 to 5, header c 6, its items 7 to 11, ...
    private const int HeaderA = 0;
    private const int HeaderC = 6;

    // ----- The header's style -----

    // The file's setters, and the probe's log: no margin, no padding, the content Left and Top; TiltContainer, the root.
    [AvaloniaFact]
    public void The_Headers_Style_Sets_What_The_Phone_Read()
    {
        using var page = new ProbePage();
        page.List.FontSize = 20;   // a page's text size, which the header's own setter stands against
        page.Settle();
        var header = page.Header(HeaderA);

        Assert.Equal(new Thickness(0), header.Margin);
        Assert.Equal(new Thickness(0), header.Padding);
        Assert.Equal(HorizontalAlignment.Left, header.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, header.VerticalContentAlignment);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(header.Background).Color);
        Assert.Equal(LV.FontSize, header.FontSize);
        Assert.Equal(page.Window.FindResource("ContentControlThemeFontFamily"), header.FontFamily);
        var root = Assert.IsType<Border>(header.GetVisualChildren().Single());
        Assert.Equal("TiltContainer", root.Name);
        Assert.True(CfTilt.GetIsEnabled(header));
        Assert.False(header.Focusable);
    }

    // ----- The layout -----

    // The probe: header a at 70, the list's width and 30 tall; item 1 straight under it at 100; header c, after a's five
    // items (b empty, so no header), at 295.8. Only headers and items: no GroupItem.
    [AvaloniaFact]
    public void A_Grouped_ListView_Lays_Out_As_The_Probe_Read()
    {
        using var page = new ProbePage();
        var a = page.Header(HeaderA);
        var c = page.Header(HeaderC);

        Assert.Equal(HeaderATop, page.Top(a), Tolerance);
        Assert.Equal(LV.OurWidth, a.Bounds.Width, Tolerance);
        Assert.Equal(HeaderHeight, a.Bounds.Height, Tolerance);
        Assert.Equal(FirstItemTop, page.Top(page.Container(HeaderA + 1)), Tolerance);
        Assert.Equal(HeaderCTop, page.Top(c), Tolerance);
        Assert.Equal("c", ((Letter)c.Content!).Key);
        Assert.All(page.List.GetRealizedContainers(), container =>
            Assert.True(container is ListViewHeaderItem || container.GetType() == typeof(ListBoxItem)));
    }

    // The probe: a group's five items in one row under its header, the next header on a row of its own, 69.2 below.
    [AvaloniaFact]
    public void A_Grouped_GridView_Lays_Out_As_The_Probe_Read()
    {
        using var page = new ProbePage(grid: true);
        var a = page.Header(HeaderA);
        var c = page.Header(HeaderC);

        Assert.Equal(LV.OurWidth, a.Bounds.Width, Tolerance);
        Assert.Equal(HeaderHeight, a.Bounds.Height, Tolerance);
        var row = Enumerable.Range(HeaderA + 1, 5).Select(page.Container).ToList();
        Assert.All(row, item => Assert.Equal(page.Top(a) + HeaderHeight, page.Top(item), Tolerance));
        Assert.Equal(GridHeaderPitch, page.Top(c) - page.Top(a), Tolerance);
        Assert.Equal(page.Top(c) + HeaderHeight, page.Top(page.Container(HeaderC + 1)), Tolerance);
    }

    // Avalonia puts the GridView's item theme on every ListBoxItem container, a header included: taken off, so the header
    // is 30 tall, not the item's 39.17, with no check box or earmark.
    [AvaloniaFact]
    public void A_GridViews_Headers_Wear_The_Header_Theme()
    {
        using var page = new ProbePage(grid: true);
        var header = page.Header(HeaderA);

        Assert.Null(header.Theme);
        Assert.Equal("TiltContainer", Assert.IsType<Border>(header.GetVisualChildren().Single()).Name);
        Assert.Same(page.Window.FindResource("Wp81GridViewItemTheme"), page.Container(HeaderA + 1).Theme);
    }

    // ----- The press -----

    // The probe: Pressed runs PointerDownThemeAnimation on TiltContainer; the release returns it.
    [AvaloniaFact]
    public void A_Press_Tilts_A_Header()
    {
        using var clock = new TestClock();
        using var page = new ProbePage();
        var header = page.Header(HeaderA);
        var at = header.TranslatePoint(HeaderPress, page.Window)!.Value;

        var touch = page.Window.TouchBegin(at);
        clock.At(600);
        var held = CfTilt.Current(header);
        page.Window.TouchEnd(touch, at);
        clock.At(1200);

        Assert.Same(header.GetVisualChildren().Single(), CfTilt.TargetOf(header));
        Assert.True(
            Math.Abs(HeaderHeld.RotationX - held.RotationX) <= HeaderAngleTolerance &&
            Math.Abs(HeaderHeld.RotationY - held.RotationY) <= HeaderAngleTolerance &&
            Math.Abs(HeaderHeld.OffsetZ - held.OffsetZ) <= HeaderDepthTolerance,
            $"read {HeaderHeld}, ours {held}");
        Assert.Equal(TiltState.Rest, CfTilt.Current(header));
    }

    // ----- Never selected, never clicked -----

    // A tap and a click on a header leave the selection as it was; select all, and code, take no header in;
    // AlwaysSelected takes the first item.
    [AvaloniaFact]
    public void A_Header_Is_Never_Selected()
    {
        using var page = new ProbePage();
        var list = page.List;
        list.SelectedIndex = HeaderA + 2;
        var at = page.Header(HeaderA).TranslatePoint(new Point(100, 15), page.Window)!.Value;

        var touch = page.Window.TouchBegin(at);
        page.Window.TouchEnd(touch, at);
        page.Window.MouseDown(at, MouseButton.Left);
        page.Window.MouseUp(at, MouseButton.Left);
        page.Settle();
        Assert.Equal("a item 2", list.SelectedItem);

        list.SelectedIndex = HeaderC;
        page.Settle();
        Assert.Equal(-1, list.SelectedIndex);

        list.SelectionMode = SelectionMode.Multiple;
        list.SelectAll();
        page.Settle();
        Assert.Equal(30, list.SelectedItems!.Count);
        Assert.DoesNotContain(list.SelectedItems.Cast<object>(), item => item is ListViewHeaderItem);

        using var always = new ProbePage(mode: SelectionMode.Single | SelectionMode.AlwaysSelected);
        always.Settle();
        Assert.Equal(HeaderA + 1, always.List.SelectedIndex);
    }

    // The arrow keys pass a header: Down from a's last item selects c's first.
    [AvaloniaFact]
    public void The_Arrow_Keys_Pass_A_Header()
    {
        using var page = new ProbePage();
        page.List.SelectedIndex = HeaderC - 1;
        page.Container(HeaderC - 1).Focus();

        page.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        page.Window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);

        Assert.Equal(HeaderC + 1, page.List.SelectedIndex);
    }

    // With item click on, a tap on a header clicks nothing; a tap on an item clicks it. Avalonia's hold is on a real
    // timer that any dispatcher job runs once it is due, so a tap that a slow machine stretches past the hold's wait
    // (300 ms) is a hold, which clicks nothing (#265). Holds are off here; A_Hold_Clicks_Nothing in ItemClickTests has them.
    [AvaloniaFact]
    public void A_Header_Never_Clicks()
    {
        using var page = new ProbePage();
        page.List.Styles.Add(new Style(x => x.Is<InputElement>())
        {
            Setters = { new Setter(InputElement.IsHoldingEnabledProperty, false) },
        });
        ListBoxOptions.SetIsItemClickEnabled(page.List, true);
        var clicked = new List<object?>();
        page.List.AddHandler(ListBoxOptions.ItemClickEvent, (_, e) => clicked.Add(e.ClickedItem));

        foreach (var index in new[] { HeaderA, HeaderA + 1 })
        {
            var at = page.Container(index).TranslatePoint(new Point(100, 15), page.Window)!.Value;
            var touch = page.Window.TouchBegin(at);
            page.Window.TouchEnd(touch, at);
        }

        Assert.Equal(["a item 1"], clicked);
    }

    // ----- The pinned header -----

    // The recording: panned, the ListView's header a stays at its top over its items; header c, arriving, pushes it up;
    // with c at the top, a goes. At rest, none.
    // Grouped before the list is shown, or after.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_ListViews_Header_Sticks_At_The_Top(bool late)
    {
        using var page = new ProbePage(late: late);
        var host = page.StickyHost;
        var sticky = page.Sticky;
        Assert.False(host.IsVisible);
        Assert.True(host.TryFindResource("ApplicationPageBackgroundThemeBrush", host.ActualThemeVariant, out var pageBrush));
        Assert.Same(pageBrush, host.Background);

        page.ScrollTo(140);
        Assert.True(host.IsVisible);
        Assert.Equal("a", ((Letter)sticky.Content!).Key);
        Assert.Equal(HeaderATop, page.Top(host), Tolerance);
        Assert.Equal(HeaderHeight, host.Bounds.Height, Tolerance);

        // Header c, 225.8 down the items, is 15.8 from the top: a is pushed up by 30 less that.
        var cAt = HeaderCTop - HeaderATop;
        page.ScrollTo(cAt - 15.8);
        Assert.True(host.IsVisible);
        Assert.Equal(HeaderATop + 15.8 - HeaderHeight, page.Top(host), Tolerance);

        page.ScrollTo(cAt + 50);
        Assert.Equal("c", ((Letter)sticky.Content!).Key);
        Assert.Equal(HeaderATop, page.Top(host), Tolerance);

        page.ScrollTo(0);
        Assert.False(host.IsVisible);
    }

    // The pinned header is in the list's scrolled content: the viewport clips it as it is pushed, and the wheel on it
    // scrolls the list, as a pan on the phone's does.
    [AvaloniaFact]
    public void The_Pinned_Header_Is_In_The_Viewport()
    {
        using var page = new ProbePage();
        page.ScrollTo(140);
        Assert.True(page.Scroll.IsVisualAncestorOf(page.StickyHost));

        page.Window.MouseWheel(page.Sticky.TranslatePoint(new Point(100, 15), page.Window)!.Value, new Vector(0, -1));
        page.Settle();

        Assert.True(page.Scroll.Offset.Y > 140);
    }

    // The recording: panned, the GridView's top row shows with no header over it.
    [AvaloniaFact]
    public void A_GridViews_Header_Scrolls_Away()
    {
        using var page = new ProbePage(grid: true);

        page.ScrollTo(100);

        Assert.True(page.Scroll.Offset.Y > 0);
        Assert.False(page.StickyHost.IsVisible);
    }

    // ----- Changes, and SemanticZoom's scroll -----

    // An item added to a group shows under its header, and the selection stays; a group emptied takes its header away.
    [AvaloniaFact]
    public void A_Change_To_A_Group_Shows_And_Keeps_The_Selection()
    {
        var letters = Letters();
        using var page = new ProbePage(groups: letters, mode: SelectionMode.Multiple);
        page.List.SelectedItems!.Add("a item 3");
        page.List.SelectedItems.Add("c item 2");

        letters[2].Add("c item 6");
        page.Settle();
        Assert.Equal("c item 6", page.List.Items[HeaderC + 6]);
        Assert.Equal(["a item 3", "c item 2"], page.List.SelectedItems.Cast<object>());

        letters[0].Clear();
        page.Settle();
        Assert.Equal("c", ((Letter)page.Header(0).Content!).Key);
        Assert.Equal(["c item 2"], page.List.SelectedItems.Cast<object>());
    }

    // For #215's jump list: a group's header is put at the top.
    [AvaloniaFact]
    public void A_Group_Scrolls_To_The_Top()
    {
        var letters = Letters();
        using var page = new ProbePage(groups: letters);

        Assert.True(ListBoxOptions.ScrollGroupToTop(page.List, letters[3]));
        page.Settle();

        var d = page.List.GetRealizedContainers().OfType<ListViewHeaderItem>().Single(h => ((Letter)h.Content!).Key == "d");
        Assert.Equal(HeaderATop, page.Top(d), Tolerance);
        Assert.False(ListBoxOptions.ScrollGroupToTop(page.List, letters[1]));   // b, empty: no header
    }

    // A new header template takes effect on every header, and on the pinned one.
    [AvaloniaFact]
    public void A_New_Header_Template_Takes_Effect()
    {
        using var page = new ProbePage();
        page.ScrollTo(140);
        var template = new FuncDataTemplate<Letter>((letter, _) => new TextBlock { Text = letter.Key.ToUpperInvariant() });
        var a = page.Header(HeaderA);

        ListBoxOptions.SetGroupHeaderTemplate(page.List, template);
        page.Settle();

        Assert.Same(a, page.Header(HeaderA));   // the same headers: the list isn't laid out again

        Assert.All(page.List.Items.OfType<ListViewHeaderItem>(), header => Assert.Same(template, header.ContentTemplate));
        Assert.Same(template, page.Sticky.ContentTemplate);
    }

    // A string among the groups is a value, not a group of its characters: no header.
    [AvaloniaFact]
    public void A_String_Is_No_Group()
    {
        using var page = new ProbePage(groups: new object[] { "apple", Letters()[0] });

        Assert.Equal(6, page.List.ItemCount);
        Assert.Equal("a", ((Letter)page.Header(0).Content!).Key);
    }

    // A header's index selected from code, then new groups: the old groups' clean-up, run after, lets go of nothing in
    // the new ones.
    [AvaloniaFact]
    public void New_Groups_Keep_Their_Selection_From_The_Old_Ones_Clean_Up()
    {
        using var page = new ProbePage();
        page.List.SelectedIndex = HeaderC;

        ListBoxOptions.SetGroupedItemsSource(page.List, new[] { new Letter("z", Enumerable.Range(1, 10).Select(i => $"z item {i}")) });
        page.List.SelectedIndex = HeaderC;
        page.Settle();

        Assert.Equal("z item 6", page.List.SelectedItem);
    }

    // Cleared, the list is ungrouped and empty.
    [AvaloniaFact]
    public void Clearing_The_Groups_Clears_The_List()
    {
        using var page = new ProbePage();

        ListBoxOptions.SetGroupedItemsSource(page.List, null);
        page.Settle();

        Assert.Null(page.List.ItemsSource);
        Assert.Equal(0, page.List.ItemCount);
    }
}
