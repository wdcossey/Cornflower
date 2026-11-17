using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's AutoSuggestBox page: in the catalogue by #158, every box the theme's, and the page giving the
/// suggestions for what is typed, a tap choosing one.
/// </summary>
public class AutoSuggestBoxPageTests
{
    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_Flyout_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(AutoSuggestBoxPage));
        Assert.Equal("autosuggestbox", entry.Title);
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(FlyoutPage)));
    }

    [AvaloniaFact]
    public void Every_Box_Is_The_Themes_And_The_Header_Shows_The_Title()
    {
        var page = new AutoSuggestBoxPage();
        using var scene = new Scene(page);

        var boxes = page.GetVisualDescendants().OfType<AutoCompleteBox>().ToList();
        Assert.Equal(3, boxes.Count);
        Assert.All(boxes, box => Assert.True(PhoneSuggestionList.GetIsEnabled(box)));
        Assert.Single(boxes, b => !b.IsEnabled);
        Assert.Equal("autosuggestbox", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    [AvaloniaFact]
    public void Typing_Shows_The_Fruits_That_Start_With_It_And_A_Tap_Chooses_One()
    {
        var page = new AutoSuggestBoxPage();
        using var scene = new Scene(page);
        var box = page.FindControl<AutoCompleteBox>("Fruit")!;
        box.MinimumPopulateDelay = TimeSpan.Zero;
        var text = box.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
        Assert.True(text.Focus());

        scene.Window.KeyTextInput("ap");
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        Assert.True(box.IsDropDownOpen);
        var rows = box.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().Single().Child!
            .GetVisualDescendants().OfType<ListBoxItem>().ToList();
        Assert.Equal(["apple", "apricot"], rows.Select(r => r.Content as string));

        var row = rows[1];
        var at = row.TranslatePoint(new Avalonia.Point(row.Bounds.Width / 2, row.Bounds.Height / 2), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at);
        scene.Window.TouchEnd(touch, at);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("apricot", box.Text);
        Assert.Equal("selected apricot", page.FindControl<TextBlock>("State")!.Text);
        Assert.False(box.IsDropDownOpen);
    }
}
