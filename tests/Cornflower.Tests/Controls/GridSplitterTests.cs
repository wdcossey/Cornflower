using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain GridSplitter (ADR 0007, #235): a bar the size of its slot, at least 9.5 each way, PhoneLowBrush at rest,
/// the accent while held, PhoneDisabledBrush disabled, no tilt. Simple's is its own brush at 6, so these fail without
/// the theme.
/// </summary>
public class GridSplitterTests
{
    private static (Grid Grid, GridSplitter Splitter) Panes()
    {
        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*"), Width = 300, Height = 100, Children = { splitter } };
        return (grid, splitter);
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void It_Is_A_Quiet_Bar_A_Finger_Can_Find(string variant)
    {
        var (grid, splitter) = Panes();
        using var scene = new Scene(new Canvas { Children = { grid } }, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var low = Resource<IBrush>(splitter, "PhoneLowBrush");
        Assert.Same(low, splitter.Background);
        Assert.Same(low, Part<Border>(splitter, "Bar").Background);
        Assert.Equal(9.5, splitter.MinWidth);
        Assert.Equal(9.5, splitter.MinHeight);
        Assert.Equal(9.5, splitter.Bounds.Width, 0.5);
        Assert.Equal(100, splitter.Bounds.Height, 0.5);
        Assert.Null(splitter.FocusAdorner);
        Assert.False(Tilt.GetIsEnabled(splitter));
    }

    [AvaloniaFact]
    public void Held_It_Is_The_Accent()
    {
        var (grid, splitter) = Panes();
        using var scene = new Scene(new Canvas { Children = { grid } });

        var at = Press(scene, splitter);
        Assert.Same(Resource<IBrush>(splitter, "PhoneAccentBrush"), Part<Border>(splitter, "Bar").Background);
        Release(scene, at);
        Assert.Same(Resource<IBrush>(splitter, "PhoneLowBrush"), Part<Border>(splitter, "Bar").Background);
    }

    [AvaloniaFact]
    public void Disabled_It_Is_PhoneDisabledBrush()
    {
        var (grid, splitter) = Panes();
        splitter.IsEnabled = false;
        using var scene = new Scene(new Canvas { Children = { grid } });

        Assert.Same(Resource<IBrush>(splitter, "PhoneDisabledBrush"), Part<Border>(splitter, "Bar").Background);
    }

    // The managed file dialog, a hole (dropped from the plain themes), keeps Simple's GridSplitters (HoleParts).
    [AvaloniaFact]
    public void In_The_File_Dialog_It_Keeps_Simples()
    {
        var simple = new SimpleTheme();
        var dialog = new ManagedFileChooser();
        using var scene = new Scene(dialog, themes: [simple, new Cornflower.Themes.Wp81.Wp81Theme()]);

        Assert.True(simple.TryGetResource(typeof(GridSplitter), ThemeVariant.Dark, out var simples));
        var splitters = dialog.GetVisualDescendants().OfType<GridSplitter>().ToList();
        Assert.NotEmpty(splitters);
        Assert.All(splitters, s => Assert.Same(simples, s.Theme));
    }
}
