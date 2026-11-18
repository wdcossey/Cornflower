using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ListBox page: in the catalogue by #212, every list on it in the keyed theme, its header, opened at its top.</summary>
public class ListBoxPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_AutoSuggestBox_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ListBoxPage));
        Assert.Equal("listbox", entry.Title);
        Assert.Equal(212, Assert.Single(typeof(ListBoxPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);   // the issue
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(AutoSuggestBoxPage)));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ListBoxPage();
        using var scene = new Scene(page);

        Assert.Equal("listbox", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    // Each list wears Wp81ListBoxTheme and its items Wp81ListBoxItemTheme, the second selected; the disabled one is.
    [AvaloniaFact]
    public void Every_List_On_The_Page_Is_The_Phones_ListBox()
    {
        var page = new ListBoxPage();
        using var scene = new Scene(page);
        var listTheme = Assert.IsType<ControlTheme>(scene.Window.FindResource("Wp81ListBoxTheme"));
        var itemTheme = Assert.IsType<ControlTheme>(scene.Window.FindResource("Wp81ListBoxItemTheme"));

        var lists = page.GetVisualDescendants().OfType<ListBox>().ToList();
        Assert.Equal(3, lists.Count);
        foreach (var list in lists)
        {
            Assert.Same(listTheme, list.Theme);
            Assert.Equal(1, list.SelectedIndex);
            Assert.All(list.GetRealizedContainers(), item => Assert.Same(itemTheme, item.Theme));
        }
        Assert.False(page.FindControl<ListBox>("Disabled")!.IsEnabled);
    }

    [AvaloniaFact]
    public void The_Page_Opens_At_Its_Top()
    {
        var page = new ListBoxPage();
        using var scene = new Scene(page);

        Assert.Equal(0, page.GetVisualDescendants().OfType<ScrollViewer>().First().Offset.Y);
    }
}
