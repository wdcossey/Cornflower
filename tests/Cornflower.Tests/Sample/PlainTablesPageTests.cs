using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's plain tables page (#242): it opens at its top with nothing focused, though its table's selected row
/// is below the fold, and the line shows the selected row, at load and once another is selected, with the page still.
/// </summary>
public class PlainTablesPageTests
{
    private static void Wait(int ms = 400)
    {
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromMilliseconds(ms));
        Dispatcher.UIThread.PushFrame(frame);
    }

    [AvaloniaFact]
    public void The_Page_Opens_At_Its_Top_Though_Its_Selection_Is_Below_The_Fold()
    {
        var page = new PlainTablesPage();
        using var scene = new Scene(page, height: 400);
        Wait();

        Assert.Equal("plain tables", page.FindControl<SamplePageHeader>("Header")!.Title);
        var scroller = page.FindControl<ScrollViewer>("Scroller")!;
        var songs = page.FindControl<TableView>("Songs")!;
        Assert.Equal(20, songs.ItemCount);
        Assert.Equal(14, songs.SelectedIndex);
        // The rows are 57.5 at the least: the fifteenth starts 14 rows down the table.
        Assert.True(songs.TranslatePoint(default, scroller)!.Value.Y + 14 * 57.5 > scroller.Viewport.Height, "the selected row isn't below the fold");
        Assert.Equal(default, scroller.Offset);
        Assert.Null(Assert.IsAssignableFrom<IFocusManager>(scene.Window.FocusManager).GetFocusedElement());
        Assert.Equal(1, page.FindControl<TableView>("LightSongs")!.SelectedIndex);
    }

    [AvaloniaFact]
    public void The_Line_Shows_The_Selected_Row()
    {
        var page = new PlainTablesPage();
        using var scene = new Scene(page);
        Wait();

        var line = page.FindControl<TextBlock>("Selection")!;
        Assert.Equal("selected: blue hour", line.Text);

        page.FindControl<TableView>("Songs")!.SelectedIndex = 18;
        Wait();
        Assert.Equal("selected: last train", line.Text);
        Assert.Equal(default, page.FindControl<ScrollViewer>("Scroller")!.Offset);
    }
}
