using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's item click page: in the catalogue by #132, its lists clicking their items, its header, a tap's click shown, and the check mode.</summary>
public class ItemClickPageTests
{
    private static void Tap(Scene scene, Control item)
    {
        var at = item.TranslatePoint(new Point(100, 10), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at);
        scene.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_ListView_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ItemClickPage));
        Assert.Equal("itemclick", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ListViewPage)));
    }

    [AvaloniaFact]
    public void Every_List_On_The_Page_Clicks_Its_Items_In_The_Phones_Look()
    {
        var page = new ItemClickPage();
        using var scene = new Scene(page);

        var lists = page.GetVisualDescendants().OfType<ListBox>().ToList();
        Assert.Equal(2, lists.Count);
        Assert.All(lists, list => Assert.True(ListBoxOptions.GetIsItemClickEnabled(list)));
        Assert.All(lists.SelectMany(l => l.GetRealizedContainers()), item => Assert.True(Tilt.GetIsEnabled(item)));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ItemClickPage();
        using var scene = new Scene(page);

        Assert.Equal("itemclick", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void A_Tap_Clicks_And_Select_Puts_The_List_In_The_Check_Mode()
    {
        var page = new ItemClickPage();
        using var scene = new Scene(page);
        var list = page.FindControl<ListBox>("Clickable")!;
        var state = page.FindControl<TextBlock>("State")!;
        Assert.Equal("nothing clicked · nothing selected", state.Text);

        Tap(scene, list.ContainerFromIndex(1)!);
        Assert.Equal("clicked: item 2 · nothing selected", state.Text);

        page.FindControl<ToggleButton>("Select")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ListViewPage.CheckMode, list.SelectionMode);
        Tap(scene, list.ContainerFromIndex(2)!);
        Assert.Equal("clicked: item 2 · selected: item 3", state.Text);

        // Out of the check mode the checks go, as a tap couldn't clear one left selected.
        page.FindControl<ToggleButton>("Select")!.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(SelectionMode.Single, list.SelectionMode);
        Assert.Equal("clicked: item 2 · nothing selected", state.Text);
    }

    [AvaloniaFact]
    public void The_Light_Lists_Click_Is_Shown_Above_It()
    {
        var page = new ItemClickPage();
        using var scene = new Scene(page);
        var light = page.FindControl<ListBox>("Light")!;
        light.BringIntoView();
        Dispatcher.UIThread.RunJobs();

        Tap(scene, light.ContainerFromIndex(0)!);

        Assert.Equal("clicked: item 1", page.FindControl<TextBlock>("LightState")!.Text);
        Assert.Equal("nothing clicked · nothing selected", page.FindControl<TextBlock>("State")!.Text);
    }
}
