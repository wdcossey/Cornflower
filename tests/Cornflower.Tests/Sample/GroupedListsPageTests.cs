using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's grouped lists page (#131): in the catalogue, its header, a grouped ListView and a grouped GridView.</summary>
public class GroupedListsPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_By_Its_Issue()
    {
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(GroupedListsPage));
        Assert.Equal("grouped lists", entry.Title);
        Assert.Equal(131, Assert.Single(typeof(GroupedListsPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);
    }

    // Both lists grouped as the probe's: six headers (b and e empty), "a" first, in the letter's template; the lower in
    // the GridView's theme.
    [AvaloniaFact]
    public void Both_Lists_Are_Grouped_As_The_Probes()
    {
        var page = new GroupedListsPage();
        using var scene = new Scene(page);

        Assert.Equal("grouped lists", page.FindControl<SamplePageHeader>("Header")!.Title);
        foreach (var name in new[] { "List", "Grid" })
        {
            var list = page.FindControl<ListBox>(name)!;
            Assert.Equal(6, list.Items.OfType<ListViewHeaderItem>().Count());
            var a = Assert.IsType<ListViewHeaderItem>(list.ContainerFromIndex(0));
            Assert.Equal("a", Assert.IsType<TextBlock>(a.GetVisualDescendants().OfType<ContentPresenter>().First().Child).Text);
        }

        Assert.Same(scene.Window.FindResource("Wp81GridViewTheme"), page.FindControl<ListBox>("Grid")!.Theme);
    }
}
