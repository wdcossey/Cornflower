using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Cornflower.Sample;
using Cornflower.Sample.Pages;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The sample's ContentControl page: in the catalogue by #117, every specimen in the phone's theme with its content at
/// the top left, but where the page centres it, and the background not drawn.
/// </summary>
public class ContentControlPageTests
{
    private static ContentControl Named(Control page, string name) => page.FindControl<ContentControl>(name)!;

    [AvaloniaFact]
    public void The_Page_Is_In_The_Catalogue_After_The_PasswordBox_Page()
    {
        var pages = SampleShell.Pages.ToList();
        var entry = Assert.Single(pages, page => page.Type == typeof(ContentControlPage));
        Assert.Equal("contentcontrol", entry.Title);
        Assert.Equal(117, Assert.Single(typeof(ContentControlPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>()).Order);   // the issue
        Assert.True(pages.IndexOf(entry) > pages.FindIndex(page => page.Type == typeof(PasswordBoxPage)));
    }

    [AvaloniaFact]
    public void The_Header_Shows_The_Pages_Title()
    {
        var page = new ContentControlPage();
        using var scene = new Scene(page);

        Assert.Equal("contentcontrol", page.FindControl<SamplePageHeader>("Header")!.Title);
    }

    // Every specimen wears the theme; each one's content is at its top left, but the centred one's.
    [AvaloniaFact]
    public void Every_Specimen_Has_The_Phones_Theme_And_Its_Content_At_The_Top_Left()
    {
        var page = new ContentControlPage();
        using var scene = new Scene(page);
        var theme = scene.Window.FindResource("Wp81ContentControlTheme");

        var specimens = page.GetVisualDescendants().OfType<ContentControl>().Where(c => c.Classes.Contains("specimen")).ToList();
        Assert.Equal(6, specimens.Count);
        foreach (var specimen in specimens)
        {
            Assert.Same(theme, specimen.Theme);
            var presenter = Assert.IsType<ContentPresenter>(specimen.Presenter);
            var at = presenter.TranslatePoint(default, specimen)!.Value;
            if (specimen.Name == "Centred")
                Assert.True(at.X > 100 && at.Y > 20, $"{specimen.Name}'s content at {at}");
            else
            {
                Assert.InRange(at.X, specimen.Padding.Left, specimen.Padding.Left + 1 / Scene.ReferenceScaling);   // the raw pixel grid
                Assert.InRange(at.Y, specimen.Padding.Top, specimen.Padding.Top + 1 / Scene.ReferenceScaling);
            }
        }
    }

    // The specimen with a background draws only its text: the slot's inside is the page's colour.
    [AvaloniaFact]
    public void The_Background_Isnt_Drawn()
    {
        var page = new ContentControlPage();
        using var scene = new Scene(page);
        var specimen = Named(page, "Unpainted");
        Assert.NotNull(specimen.Background);

        using var frame = scene.Capture();
        var inside = specimen.TranslatePoint(new Point(specimen.Bounds.Width - 10, specimen.Bounds.Height - 10), scene.Window)!.Value;
        var pixel = Frames.PixelAt(frame, (int)(inside.X * Scene.ReferenceScaling), (int)(inside.Y * Scene.ReferenceScaling));
        Assert.True(inside.Y < Scene.ReferenceHeight, $"{inside}");
        Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(page.Background).Color, pixel);     // ApplicationPageBackgroundThemeBrush
    }
}
