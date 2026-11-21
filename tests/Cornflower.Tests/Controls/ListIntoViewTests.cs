using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// A list's scroll to an item stays in the list (<see cref="ListBoxOptions.KeepsScrollInListProperty"/>, #220), in each
/// Wp81 list theme: the ListView's by type, <c>Wp81ListBoxTheme</c> and <c>Wp81GridViewTheme</c>. The page is the
/// probe's (<c>-Probe listintoview</c>, tools/refapp/README.md): a ScrollViewer holding a 900-tall spacer, the list of
/// 30 items with item 20 selected before it loads, and 400 more.
/// </summary>
public class ListIntoViewTests
{
    /// <summary>The themes: "ListView" for the ListView's, by type, or a keyed one.</summary>
    public static TheoryData<string> Themes => ["ListView", "Wp81ListBoxTheme", "Wp81GridViewTheme"];

    /// <summary>Each theme with its selection in the list's own view (item 1) and below it (item 20).</summary>
    public static TheoryData<string, int> ThemesAndSelections => new()
    {
        { "ListView", 1 }, { "ListView", 20 },
        { "Wp81ListBoxTheme", 1 }, { "Wp81ListBoxTheme", 20 },
        { "Wp81GridViewTheme", 1 }, { "Wp81GridViewTheme", 20 },
    };

    private sealed class Page : IDisposable
    {
        private readonly Scene _scene;

        /// <param name="theme">One of <see cref="Themes"/>.</param>
        /// <param name="free">The list left unconstrained in the page's ScrollViewer, not 300 tall.</param>
        /// <param name="firstHeight">The window's height at the first layout: short, as Android's first is.</param>
        /// <param name="selected">The item selected before the list loads.</param>
        public Page(string theme, bool free = false, double firstHeight = 60, int selected = 20)
        {
            List = new ListBox
            {
                ItemsSource = Enumerable.Range(0, 30).Select(i => $"item {i}").ToList(),
                SelectedIndex = selected,
            };
            if (!free)
                List.Height = 300;
            if (theme != "ListView")
                List.Bind(StyledElement.ThemeProperty, List.GetResourceObservable(theme));
            if (theme == "Wp81GridViewTheme")
                List.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((s, _) =>
                    new Border { Width = 120, Height = 120, Child = new TextBlock { Text = s } });
            Viewer = new ScrollViewer { Content = new StackPanel { Children = { new Border { Height = 900 }, List, new Border { Height = 400 } } } };
            _scene = new Scene(Viewer, height: firstHeight);
            Settle();
            Window.Height = Scene.ReferenceHeight;
            Settle();
        }

        public ListBox List { get; }

        public ScrollViewer Viewer { get; }

        public Window Window => _scene.Window;

        public ScrollViewer Inner => List.GetVisualDescendants().OfType<ScrollViewer>().First(v => v.TemplatedParent == List);

        public ListBoxItem Item(int i) => Assert.IsType<ListBoxItem>(List.ContainerFromIndex(i));

        public void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>The page and the list back at the top.</summary>
        public void Reset()
        {
            Viewer.Offset = default;
            Inner.Offset = default;
            Settle();
        }

        public void Dispose() => _scene.Dispose();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void The_Themes_Keep_The_Scroll_In_The_List(string theme)
    {
        using var page = new Page(theme, firstHeight: Scene.ReferenceHeight);

        Assert.True(ListBoxOptions.GetKeepsScrollInList(page.List));
    }

    // Android's first layout of a page is short (60 here), then the full height. Avalonia's scroll to the selection,
    // when the list's own ScrollViewer needn't move (item 1) or when the page is too short for the list (item 20), went on
    // to the page's ScrollViewer and left it there (142.9 in the gridview sample, #219). The phone's page stays at 0.
    [AvaloniaTheory]
    [MemberData(nameof(ThemesAndSelections))]
    public void A_Selected_List_Low_On_A_Page_Leaves_The_Page_At_Its_Top_After_A_Short_First_Layout(string theme, int selected)
    {
        using var page = new Page(theme, selected: selected);

        Assert.Equal(0, page.Viewer.Offset.Y);
    }

    // Unconstrained in the page's ScrollViewer, only the page could bring the selection into view: on the phone it stays.
    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void An_Unconstrained_Selected_List_Leaves_The_Page_At_Its_Top(string theme)
    {
        using var page = new Page(theme, free: true);

        Assert.Equal(0, page.Viewer.Offset.Y);
    }

    // On the phone ScrollIntoView, and a selection set in code with the focus in the list (#136), scroll the list, never
    // the page. Avalonia's selection scroll stays Avalonia's in the list: it scrolls with no focus too (a departure).
    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void A_Selection_Or_ScrollIntoView_From_Code_Scrolls_The_List_Not_The_Page(string theme)
    {
        using var page = new Page(theme, firstHeight: Scene.ReferenceHeight);
        page.Reset();

        page.List.SelectedIndex = 1;
        page.Settle();
        Assert.Equal(0, page.Viewer.Offset.Y);
        Assert.Equal(0, page.Inner.Offset.Y);

        page.List.SelectedIndex = 25;
        page.Settle();
        Assert.Equal(0, page.Viewer.Offset.Y);
        Assert.True(page.Inner.Offset.Y > 0, "the list didn't scroll to its selection");

        page.Reset();
        page.List.ScrollIntoView(28);
        page.Settle();
        Assert.Equal(0, page.Viewer.Offset.Y);
        Assert.True(page.Inner.Offset.Y > 0, "ScrollIntoView didn't scroll the list");
    }

    // The focus still goes through: an item focused by the keyboard is brought into view in the page as well.
    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void A_Focused_Item_Is_Brought_Into_View_In_The_Page_Too(string theme)
    {
        using var page = new Page(theme, firstHeight: Scene.ReferenceHeight);
        page.Reset();

        Assert.True(page.Item(0).Focus(NavigationMethod.Tab));
        page.Settle();

        Assert.True(page.Viewer.Offset.Y > 0, "the page didn't scroll to the focused item");
        Assert.InRange(page.Item(0).TranslatePoint(default, page.Window)!.Value.Y, 0, Scene.ReferenceHeight - page.Item(0).Bounds.Height);
    }

    // Arrow keys still move the list to the item they focus, and the page as far as it takes to show it.
    [AvaloniaTheory]
    [MemberData(nameof(Themes))]
    public void Arrow_Keys_Scroll_The_List_To_The_Item_They_Focus(string theme)
    {
        using var page = new Page(theme, firstHeight: Scene.ReferenceHeight);
        page.List.SelectedIndex = 0;
        page.Reset();
        Assert.True(page.Item(0).Focus(NavigationMethod.Tab));
        page.Settle();
        var pageOffset = page.Viewer.Offset.Y;
        Assert.True(pageOffset > 0, "the page didn't scroll to the focused item");

        // Fifteen on: item 15, below the list's 300 (down a stack; right along the grid's rows, three a row).
        var (key, physical) = theme == "Wp81GridViewTheme" ? (Key.Right, PhysicalKey.ArrowRight) : (Key.Down, PhysicalKey.ArrowDown);
        for (var i = 0; i < 15; i++)
        {
            page.Window.KeyPress(key, RawInputModifiers.None, physical, null);
            page.Window.KeyRelease(key, RawInputModifiers.None, physical, null);
            page.Settle();
        }

        var focused = Assert.IsType<ListBoxItem>(page.Window.FocusManager!.GetFocusedElement());
        Assert.True(page.List.IndexFromContainer(focused) >= 15, $"focus reached item {page.List.IndexFromContainer(focused)}");
        Assert.True(page.Inner.Offset.Y > 0, "the arrow keys didn't scroll the list");
        Assert.InRange(focused.TranslatePoint(default, page.Inner)!.Value.Y, -0.01, page.Inner.Viewport.Height - focused.Bounds.Height + 0.01);
        Assert.InRange(focused.TranslatePoint(default, page.Window)!.Value.Y, -0.01, Scene.ReferenceHeight - focused.Bounds.Height + 0.01);
    }
}
