using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's GridView page: in the catalogue by #213, every grid on it in the keyed theme, its header, opened at its top.</summary>
public class GridViewPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ListBox_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(GridViewPage));
        Assert.Equal("gridview", entry.Title);
        Assert.Equal(213, Assert.Single(typeof(GridViewPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);   // the issue
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ListBoxPage)));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new GridViewPage();
        using var scene = new Scene(page);

        Assert.Equal("gridview", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    // Each grid wears Wp81GridViewTheme and its tiles Wp81GridViewItemTheme; the two below are in the check mode with
    // tiles 0, 1 and 3 selected.
    [AvaloniaFact]
    public void Every_Grid_On_The_Page_Is_The_Phones_GridView()
    {
        var page = new GridViewPage();
        using var scene = new Scene(page);
        var listTheme = Assert.IsType<ControlTheme>(scene.Window.FindResource("Wp81GridViewTheme"));
        var itemTheme = Assert.IsType<ControlTheme>(scene.Window.FindResource("Wp81GridViewItemTheme"));

        var lists = page.GetVisualDescendants().OfType<ListBox>().ToList();
        Assert.Equal(3, lists.Count);
        foreach (var list in lists)
        {
            Assert.Same(listTheme, list.Theme);
            Assert.All(list.GetRealizedContainers(), item => Assert.Same(itemTheme, item.Theme));
        }

        Assert.False(PhoneListMultiSelect.GetIsOn(page.FindControl<ListBox>("Single")!));
        foreach (var name in new[] { "Check", "Light" })
        {
            var list = page.FindControl<ListBox>(name)!;
            Assert.True(PhoneListMultiSelect.GetIsOn(list));
            Assert.Equal([0, 1, 3], list.Selection.SelectedIndexes.Order());
        }
    }

    [AvaloniaFact]
    public void The_Page_Opens_At_Its_Top()
    {
        var page = new GridViewPage();
        using var scene = new Scene(page);

        Assert.Equal(0, page.GetVisualDescendants().OfType<ScrollViewer>().First().Offset.Y);
    }

    // On the Pixel the page is first laid out short, then at its size (#219's device run): a grid bringing its selection
    // into view in that short first layout would scroll the page down to it, where it stayed. The page opens at its top
    // all the same.
    [AvaloniaFact]
    public void The_Page_Opens_At_Its_Top_After_A_Short_First_Layout()
    {
        var page = new GridViewPage();
        using var scene = new Scene(page, height: 60);
        Dispatcher.UIThread.RunJobs();
        scene.Window.Height = Scene.ReferenceHeight;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, page.GetVisualDescendants().OfType<ScrollViewer>().First().Offset.Y);
    }
}
