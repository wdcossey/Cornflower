using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>The sample's ComboBox page: in the catalogue by #155, every ComboBox on it the theme's, each in its form, and what is picked shown.</summary>
public class ComboBoxPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_List_Picker_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ComboBoxPage));
        Assert.Equal("combobox", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(ListPickerFlyoutPage)));
    }

    [AvaloniaFact]
    public void Every_ComboBox_Is_The_Phones_In_Its_Form()
    {
        var page = new ComboBoxPage();
        using var scene = new Scene(page);

        var boxes = page.GetVisualDescendants().OfType<ComboBox>().ToList();
        Assert.Equal(5, boxes.Count);
        Assert.All(boxes, box => Assert.Contains(box.GetVisualDescendants(), v => v is Control { Name: "ItemsPresenterHost" }));
        Assert.Equal(["Full"], boxes.Where(PhoneComboBox.GetIsFull).Select(b => b.Name));
        Assert.Equal("size", ControlHeader.GetHeader(page.FindControl<ComboBox>("Inline")!));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title_And_The_State_Follows_The_Picks()
    {
        var page = new ComboBoxPage();
        using var scene = new Scene(page);
        var state = page.FindControl<TextBlock>("State")!;

        Assert.Equal("combobox", page.FindControl<SamplePageHeader>("Header")!.Title);
        Assert.Equal("medium, blue", state.Text);

        page.FindControl<ComboBox>("Full")!.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("medium, red", state.Text);
    }
}
