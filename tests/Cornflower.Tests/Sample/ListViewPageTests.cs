using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ListView page: in the catalogue by #119, every list on it the theme's, its header, its selection shown, and the check mode.</summary>
public class ListViewPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ScrollViewer_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ListViewPage));
        Assert.Equal("listview", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ScrollViewerPage)));
    }

    [AvaloniaFact]
    public void Every_List_On_The_Page_Is_The_Phones()
    {
        var page = new ListViewPage();
        using var scene = new Scene(page);

        var lists = page.GetVisualDescendants().OfType<ListBox>().ToList();
        Assert.Equal(4, lists.Count);
        Assert.All(lists, list => Assert.True(PhoneListMultiSelect.GetIsEnabled(list)));
        Assert.All(lists.SelectMany(l => l.GetRealizedContainers()), item => Assert.True(Cornflower.Controls.Tilt.GetIsEnabled(item)));
        Assert.False(page.FindControl<ListBox>("Disabled")!.IsEnabled);
        Assert.All(page.FindControl<ListBox>("Light")!.GetRealizedContainers(), item => Assert.True(item.GetValue(PhoneListMultiSelect.IsOnProperty)));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ListViewPage();
        using var scene = new Scene(page);

        Assert.Equal("listview", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void A_Tap_Selects_And_Select_Puts_The_List_In_The_Check_Mode()
    {
        var page = new ListViewPage();
        using var scene = new Scene(page);
        var list = page.FindControl<ListBox>("Default")!;
        var state = page.FindControl<TextBlock>("State")!;
        Assert.Equal("nothing selected", state.Text);

        var item = list.ContainerFromIndex(1)!;
        var at = item.TranslatePoint(new Point(100, 10), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at);
        scene.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("selected: item 2", state.Text);
        Assert.False(item.GetValue(PhoneListMultiSelect.IsOnProperty));

        page.FindControl<ToggleButton>("Select")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ListViewPage.CheckMode, list.SelectionMode);
        Assert.True(item.GetValue(PhoneListMultiSelect.IsOnProperty));
    }
}
