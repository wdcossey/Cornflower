using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain TableView, TableViewRow, TableViewCell and TableViewColumnHeader (ADR 0007, #242), on the tree's row
/// (Wp81PlainRowTheme): no box and no grid lines, the rule under each row and under the headers, the phone's touch
/// height and the body's type in the rows, small print in the headers, the cells' text where their headers' starts,
/// the tilt on the row and the accent for a selected row's text, and a selection that never moves the page. Simple's
/// draws a bordered box, a filled header strip, bold headers, a filled selection and 1 lines between headers, so these
/// fail without the themes.
/// </summary>
public class TableViewTests
{
    public sealed record Item(string Name, string Size, string Kind);

    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static TableView Table(int rows = 3) => new()
    {
        Width = 360,
        Columns =
        {
            new TableViewColumn { Header = "name", Width = GridLength.Star, Binding = new Binding(nameof(Item.Name)) },
            new TableViewColumn { Header = "size", Width = new GridLength(90), Binding = new Binding(nameof(Item.Size)) },
            new TableViewColumn { Header = "kind", Width = new GridLength(90), Binding = new Binding(nameof(Item.Kind)) },
        },
        ItemsSource = Enumerable.Range(0, rows).Select(i => new Item("file " + i, i + " KB", "text")).ToList(),
    };

    // An app's own type round the table, so what the themes set can't come from the window's.
    private static Border Host(Control child)
    {
        var host = new Border { Child = child };
        TextElement.SetFontFamily(host, new FontFamily("Arial"));
        TextElement.SetFontSize(host, 12);
        return host;
    }

    private static TableViewRow Row(TableView table, int index) =>
        Assert.IsType<TableViewRow>(table.ContainerFromIndex(index));

    private static TableViewCell[] Cells(TableViewRow row) =>
        row.GetVisualDescendants().OfType<TableViewCell>().ToArray();

    private static TableViewColumnHeader[] Headers(TableView table) =>
        table.GetVisualDescendants().OfType<TableViewColumnHeader>().ToArray();

    private static ContentPresenter Presenter(TemplatedControl control) => Part<ContentPresenter>(control, "PART_ContentPresenter");

    // Layout rounds to the device pixel.
    private static void Near(double expected, double actual) =>
        Assert.InRange(actual, expected - 1 / Scene.ReferenceScaling, expected + 1 / Scene.ReferenceScaling);

    private static double X(Visual part, Visual to) => part.TranslatePoint(default, to)!.Value.X;

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void The_Table_Draws_No_Box_Over_The_Page(string variant)
    {
        var table = Table();
        using var scene = new Scene(Host(table), Variant(variant));

        var border = Part<Border>(table, "PART_Border");
        Assert.Null(border.BorderBrush);
        Assert.Equal(new Thickness(0), border.BorderThickness);
        Assert.Equal(new Thickness(0), table.Padding);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(table.Background).Color);
        Assert.Same(Resource<IBrush>(table, "PhoneForegroundBrush"), table.Foreground);
        Assert.Null(table.FocusAdorner);
        Assert.Equal(TextTrimming.CharacterEllipsis, table.GetValue(TextBlock.TextTrimmingProperty));

        // The headers' strip: no fill, the row's rule under it.
        var scroller = Part<ScrollViewer>(table, "PART_ScrollViewer");
        Assert.Same(Resource<ControlTheme>(table, "Wp81PlainTableScrollViewerTheme"), scroller.Theme);
        var strip = Part<Border>(scroller, "PART_HeaderRow");
        Assert.Null(strip.Background);
        Assert.Equal(new Thickness(0, 0, 0, 1.5), strip.BorderThickness);
        Assert.Same(Resource<IBrush>(table, "PhoneLowBrush"), strip.BorderBrush);
        Assert.All(scroller.GetVisualDescendants().OfType<ScrollBar>(), bar => Assert.False(bar.IsHitTestVisible));
    }

    // The row: Wp81PlainRowTheme's rule (0,0,0,1.5 in PhoneLowBrush) at the phone's 57.5, its cells in the body's type,
    // centred, with the tilt on PART_CellsPresenter inside the untilted row.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Row_Is_The_Plain_Row(string variant)
    {
        var table = Table();
        using var scene = new Scene(Host(table), Variant(variant));

        var item = Row(table, 0);
        var row = Part<Border>(item, "PART_Row");
        Assert.Same(Resource<ControlTheme>(item, "Wp81PlainRowTheme"), row.Theme);
        Assert.Equal(new Thickness(0, 0, 0, 1.5), row.BorderThickness);
        Assert.Same(Resource<IBrush>(item, "PhoneLowBrush"), row.BorderBrush);
        Near(57.5, row.Bounds.Height);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(row.Background).Color);
        Assert.Equal(new Thickness(0), row.Padding);

        var cell = Presenter(Cells(item)[0]);
        Assert.Equal(18.14, cell.FontSize);
        Assert.Equal(Resource<FontFamily>(item, "PhoneFontFamilyNormal"), cell.FontFamily);
        Assert.Same(Resource<IBrush>(item, "PhoneForegroundBrush"), cell.Foreground);
        Assert.True(PhoneHeaderLine.GetIsEnabled(cell));
        var text = Assert.IsAssignableFrom<TextBlock>(cell.Child);
        Assert.True(text.IsSet(PhoneTextLine.MaxHeightLineHeightProperty));
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        // Centred in the row's height.
        Assert.Equal(VerticalAlignment.Center, Cells(item)[0].VerticalContentAlignment);
        Near((row.Bounds.Height - 1.5 - text.Bounds.Height) / 2, text.TranslatePoint(default, row)!.Value.Y);

        Assert.True(Tilt.GetIsEnabled(item));
        Assert.Same(Part<TableViewCellsPresenter>(item, "PART_CellsPresenter"), Tilt.TargetOf(item));
        Assert.Null(item.FocusAdorner);
    }

    // No grid lines and no box round a cell; its text starts at the page's edge in the first column, and where its
    // header's does in each, with 9.5 after it.
    [AvaloniaFact]
    public void A_Cells_Text_Starts_Where_Its_Headers_Does()
    {
        var table = Table();
        using var scene = new Scene(Host(table));

        Assert.Equal(new Thickness(0, 0, 9.5, 0), Resource<Thickness>(table, "CornflowerPlainTableCellPadding"));
        Assert.Equal(new Thickness(0, 4.8, 9.5, 4.8), Resource<Thickness>(table, "CornflowerPlainTableHeaderPadding"));
        var cells = Cells(Row(table, 0));
        var headers = Headers(table);
        Assert.Equal(3, headers.Length);
        for (var i = 0; i < 3; i++)
        {
            var cellText = Assert.IsAssignableFrom<TextBlock>(Presenter(cells[i]).Child);
            var headerText = Assert.IsAssignableFrom<TextBlock>(Presenter(headers[i]).Child);
            Assert.Equal(new Thickness(0, 0, 9.5, 0), cells[i].Padding);
            Assert.Equal(new Thickness(0, 0, 9.5, 0), Presenter(cells[i]).Padding);
            Assert.Equal(new Thickness(0), cells[i].BorderThickness);
            Near(X(headerText, table), X(cellText, table));
        }
        Near(0, X(Presenter(cells[0]).Child!, table));
        Near(270, X(Presenter(cells[2]).Child!, table));
    }

    // A header is small print: 16, PhoneMidBrush, not bold, no fill; its resizer a transparent 9.5 bar over the gap
    // after its text, with nothing drawn.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Header_Is_Small_Print(string variant)
    {
        var table = Table();
        using var scene = new Scene(Host(table), Variant(variant));

        var header = Headers(table)[0];
        var presenter = Presenter(header);
        Assert.Equal(16, presenter.FontSize);
        Assert.Equal(FontWeight.Normal, presenter.FontWeight);
        Assert.Equal(Resource<FontFamily>(header, "PhoneFontFamilyNormal"), presenter.FontFamily);
        Assert.Same(Resource<IBrush>(header, "PhoneMidBrush"), presenter.Foreground);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Background).Color);
        Assert.True(PhoneHeaderLine.GetIsEnabled(presenter));
        Assert.Equal(new Thickness(0, 4.8, 9.5, 4.8), header.Padding);
        Assert.False(Tilt.GetIsEnabled(header));

        var resizer = Part<Thumb>(header, "PART_Resizer");
        Assert.Same(Resource<ControlTheme>(header, "Wp81PlainColumnResizerTheme"), resizer.Theme);
        Near(9.5, resizer.Bounds.Width);
        Near(header.Bounds.Width - 9.5, X(resizer, header));
        Assert.True(resizer.IsEnabled);
        var bar = Assert.IsType<Border>(Assert.Single(resizer.GetVisualChildren()));
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(bar.Background).Color);
        Assert.Empty(bar.GetVisualChildren());

        table.CanUserResizeColumns = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(resizer.IsEnabled);
    }

    // A tap selects the row: its text is the accent, with no fill; disabled, PhoneDisabledBrush; the others keep theirs.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Selected_Its_Text_Is_The_Accent(string variant)
    {
        var table = Table();
        using var scene = new Scene(Host(table), Variant(variant));
        Row(table, 2).IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        Release(scene, Press(scene, Row(table, 1)));

        Assert.Equal(1, table.SelectedIndex);
        var selected = Row(table, 1);
        Assert.Same(Resource<IBrush>(selected, "PhoneAccentBrush"), Presenter(Cells(selected)[0]).Foreground);
        Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(Part<Border>(selected, "PART_Row").Background).Color);
        Assert.Same(Resource<IBrush>(selected, "PhoneForegroundBrush"), Presenter(Cells(Row(table, 0))[0]).Foreground);
        Assert.Same(Resource<IBrush>(selected, "PhoneDisabledBrush"), Presenter(Cells(Row(table, 2))[0]).Foreground);
    }

    [AvaloniaFact]
    public void A_Press_Tilts_The_Row()
    {
        var table = Table();
        using var scene = new Scene(Host(table));

        var at = Press(scene, Row(table, 1));

        Assert.NotEqual(TiltState.Rest, Tilt.Current(Row(table, 1)));
        Assert.Equal(TiltState.Rest, Tilt.Current(Row(table, 0)));
        Release(scene, at);
    }

    private static (ScrollViewer Page, TableView Table) OnAPage(TableView table)
    {
        var page = new ScrollViewer { Height = 200, Content = new StackPanel { Children = { new Border { Height = 600 }, table } } };
        return (page, table);
    }

    // The phone's lists never move the page for a selection (#220, #251, #252): a table with a row selected low on a
    // page doesn't scroll the page to it at load, nor when a row is selected from code (Avalonia's
    // AutoScrollToSelectedItem, off).
    [AvaloniaFact]
    public void A_Selection_Doesnt_Scroll_The_Page()
    {
        var table = Table(20);
        table.SelectedIndex = 15;
        var (page, _) = OnAPage(table);
        using var scene = new Scene(new StackPanel { Children = { page } });
        Dispatcher.UIThread.RunJobs();

        Assert.False(table.AutoScrollToSelectedItem);
        Assert.Equal(0, page.Offset.Y);

        table.SelectedIndex = 18;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, page.Offset.Y);
    }

    // Nor the table's own rows, when it has a height of its own: the phone's list doesn't move for a selection.
    [AvaloniaFact]
    public void A_Selection_Doesnt_Scroll_The_Tables_Own_Rows()
    {
        var table = Table(20);
        table.Height = 200;
        table.SelectedIndex = 15;
        using var scene = new Scene(Host(table));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, Part<ScrollViewer>(table, "PART_ScrollViewer").Offset.Y);
    }

    // A request for a row (ScrollIntoView, a press's focus) stops at the table, as on Wp81's lists (KeepsScrollInList).
    [AvaloniaFact]
    public void A_Request_For_A_Row_Stays_In_The_Table()
    {
        var table = Table(20);
        var (page, _) = OnAPage(table);
        using var scene = new Scene(new StackPanel { Children = { page } });
        Dispatcher.UIThread.RunJobs();

        Assert.True(ListBoxOptions.GetKeepsScrollInList(table));
        table.ScrollIntoView(15);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, page.Offset.Y);
    }

    // A table with a height of its own keeps its headers while its rows scroll; one with none scrolls with the page,
    // headers and all.
    [AvaloniaFact]
    public void A_Tall_Table_Keeps_Its_Headers_Above_Its_Rows()
    {
        var table = Table(20);
        table.Height = 200;
        using var scene = new Scene(Host(table));
        var scroller = Part<ScrollViewer>(table, "PART_ScrollViewer");
        var strip = Part<Border>(scroller, "PART_HeaderRow");
        var rows = Part<ScrollContentPresenter>(scroller, "PART_ContentPresenter");

        scroller.Offset = new Vector(0, 300);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(300, scroller.Offset.Y);
        Near(0, strip.TranslatePoint(default, table)!.Value.Y);
        Near(strip.Bounds.Height, rows.TranslatePoint(default, table)!.Value.Y);
    }
}
