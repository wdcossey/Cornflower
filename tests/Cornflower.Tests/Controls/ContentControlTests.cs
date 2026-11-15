using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The phone's ContentControl, <c>Wp81ContentControlTheme</c> (<c>Controls/ContentControl.axaml</c>), against its style
/// (<c>contentcontrol.json</c>) and the dumps of the ContentControls the phone's own templates hold; and the bare
/// ContentPresenter, which keeps Avalonia's own, against the dumps of the phone's template presenters.
/// docs/controls/ContentControl/PLAN.md has each reading.
/// </summary>
public class ContentControlTests
{
    private const string Key = "Wp81ContentControlTheme";

    /// <summary>
    /// Pivot's <c>TitleContentControl</c>, run <c>20261108-1732-dev11</c>, <c>Pivot__two-items__base</c>, dark and light
    /// alike: a ContentControl whose own style (<c>PivotTitleContentControlStyle</c>) sets only its font and margin, so
    /// its alignment and template are the default style's.
    /// </summary>
    private static class TitleReading
    {
        public const double ControlWidth = 430.8333;      // the stage's 450 less the 19 margin, rounded to the raw grid
        public const double ControlHeight = 24.1667;      // 58 raw pixels: the line
        public const double PresenterHeight = 24.1667;    // the presenter at the control's top left, its content's height
        public const double Left = 19.1667;               // 46 raw pixels: the 19 margin
        public const double Top = 37.9167;                // 91 raw pixels: the 38 margin
        public const double TextHeight = 24.1267;         // Segoe WP's line, 1.33 em (ADR 0003): 1.33 × 18.14 is 24.1262
        public const double PresenterWidth = 119.5833;    // "CORNFLOWER" in Segoe WP Semibold: compared within 10% (Selawik is wider)
    }

    /// <summary>A ContentControl given the theme by its key, as an app or a template gives it: from the scene's Wp81Theme.</summary>
    private static ContentControl Themed(object? content = null)
    {
        var control = new ContentControl { Content = content };
        control.Bind(StyledElement.ThemeProperty, control.GetResourceObservable(Key));
        return control;
    }

    /// <summary>
    /// Pivot's title as the dump has it: "CORNFLOWER" in <c>PivotTitleContentControlStyle</c>'s font, 19,38 from the
    /// stage's corner, in the theme. <c>ContentControlFrames</c> draws the same.
    /// </summary>
    internal static ContentControl PivotTitle()
    {
        var control = Themed("CORNFLOWER");
        control.Margin = new Thickness(19, 38, 0, 0);
        control.FontWeight = FontWeight.SemiBold;
        control.Bind(TemplatedControl.FontFamilyProperty, control.GetResourceObservable("PhoneFontFamilyNormal"));
        control.Bind(TemplatedControl.FontSizeProperty, control.GetResourceObservable("TextStyleLargeFontSize"));
        return control;
    }

    /// <summary>Whether the scene's themes have a theme for <paramref name="type"/> by type, dark or light.</summary>
    private static bool ThemedByType(Scene scene, Type type) =>
        new[] { ThemeVariant.Dark, ThemeVariant.Light }.Any(variant => scene.Window.TryFindResource(type, variant, out var found) && found is ControlTheme);

    private static ContentPresenter PresenterOf(ContentControl control) =>
        Assert.IsType<ContentPresenter>(control.Presenter);

    /// <summary>A line <paramref name="expected"/> tall, as layout rounds it up to the raw pixel grid (ADR 0003).</summary>
    private static void AssertLine(double expected, double actual) =>
        Assert.InRange(actual, expected - 0.001, expected + 1 / Scene.ReferenceScaling);

    private static Rect BoundsIn(Visual visual, Visual relativeTo)
    {
        var origin = visual.TranslatePoint(default, relativeTo) ?? throw new InvalidOperationException("Not in the same tree.");
        return new Rect(origin, visual.Bounds.Size);
    }

    // contentcontrol.json's two setters, and its template's one part, the presenter Avalonia's ContentControl looks for.
    [AvaloniaFact]
    public void The_Theme_Sets_The_Phones_Two_Values_And_One_Presenter()
    {
        var control = Themed("content");
        using var scene = new Scene(control);

        var theme = Assert.IsType<ControlTheme>(scene.Window.FindResource(Key));
        Assert.Same(theme, control.Theme);
        Assert.Equal(typeof(ContentControl), theme.TargetType);
        Assert.Equal(HorizontalAlignment.Left, control.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Top, control.VerticalContentAlignment);
        var presenter = PresenterOf(control);
        Assert.Equal("PART_ContentPresenter", presenter.Name);
        Assert.Null(control.FocusAdorner);      // none, as the phone (no focus states): Simple and Avalonia set none for a ContentControl
        Assert.Single(control.GetVisualChildren());
    }

    // The template places the presenter by the content alignments, inside the Padding as its margin: at Left and Top, the
    // content sits at the top left at its own size. Avalonia's own ContentControl stretches the presenter and centres a
    // fixed-size element in it.
    [AvaloniaTheory]
    [InlineData(null, null, 0, 0, 60, 40)]                                   // the phone's: Left, Top
    [InlineData(HorizontalAlignment.Right, VerticalAlignment.Bottom, 140, 60, 60, 40)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Center, 70, 30, 60, 40)]
    [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, 200, 100)]     // a fixed size centred in it
    public void The_Content_Is_Placed_By_The_Content_Alignments(
        HorizontalAlignment? horizontal, VerticalAlignment? vertical, double x, double y, double width, double height)
    {
        var content = new Border { Width = 60, Height = 40 };
        var control = Themed(content);
        control.Width = 200;
        control.Height = 100;
        if (horizontal is { } h) control.HorizontalContentAlignment = h;
        if (vertical is { } v) control.VerticalContentAlignment = v;
        using var scene = new Scene(new Canvas { Children = { control } });

        var presenter = PresenterOf(control);
        Assert.Equal(new Rect(x, y, width, height), BoundsIn(presenter, control));
        Assert.Equal(new Rect(x + (width - 60) / 2, y + (height - 40) / 2, 60, 40), BoundsIn(content, control));
    }

    // The Padding is the presenter's margin.
    [AvaloniaFact]
    public void The_Padding_Is_The_Presenters_Margin()
    {
        var content = new Border { Width = 60, Height = 40 };
        var control = Themed(content);
        control.Width = 200;
        control.Height = 100;
        control.Padding = new Thickness(10, 5, 0, 0);
        using var scene = new Scene(new Canvas { Children = { control } });

        Assert.Equal(new Thickness(10, 5, 0, 0), PresenterOf(control).Margin);
        Assert.Equal(new Rect(10, 5, 60, 40), BoundsIn(content, control));
    }

    // Pivot's title, a ContentControl the phone's default style lays out: stretched across the stage, its presenter at
    // its top left, as tall as the text's line, 1.33 em of 18.14, and only as wide as the text.
    // The light dump reads the same: the theme has no brush, so dark alone is laid out here.
    [AvaloniaFact]
    public void Pivots_Title_Is_Laid_Out_As_The_Dump()
    {
        var control = PivotTitle();
        using var scene = new Scene(new StackPanel { Width = 450, Children = { control } });

        Assert.Equal(18.14, control.FontSize);
        Assert.Equal(TitleReading.Left, control.Bounds.X, 3);
        Assert.Equal(TitleReading.Top, control.Bounds.Y, 3);
        Assert.Equal(TitleReading.ControlWidth, control.Bounds.Width, 3);
        Assert.Equal(TitleReading.ControlHeight, control.Bounds.Height, 3);
        var presenter = PresenterOf(control);
        var bounds = BoundsIn(presenter, control);
        Assert.Equal(new Point(0, 0), bounds.Position);
        Assert.Equal(TitleReading.PresenterHeight, bounds.Height, 3);
        Assert.InRange(bounds.Width, TitleReading.PresenterWidth * 0.9, TitleReading.PresenterWidth * 1.1);  // Selawik, not Segoe WP
        var text = Assert.IsType<TextBlock>(presenter.Child);
        AssertLine(TitleReading.TextHeight, text.Bounds.Height);
    }

    // A string's text has the phone's line, and no access key: the phone's ContentControl draws "_open" as it is.
    [AvaloniaFact]
    public void A_Strings_Text_Has_The_Phones_Line_And_No_Access_Key()
    {
        var control = Themed("_open");
        control.FontSize = 30;
        using var scene = new Scene(control);

        var text = Assert.IsType<TextBlock>(PresenterOf(control).Child);     // exactly a TextBlock: not an AccessText
        Assert.Equal("_open", text.Text);
        Assert.True(PhoneHeaderLine.GetIsEnabled(PresenterOf(control)));
        AssertLine(30 * 1.33, text.Bounds.Height);
    }

    // An app's ContentTemplate is the content's template, and keeps its own line.
    [AvaloniaFact]
    public void An_Apps_ContentTemplate_Is_Used_And_Keeps_Its_Own_Line()
    {
        var control = Themed("data");
        control.FontSize = 30;
        control.ContentTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock { Name = "Mine", Text = value, LineHeight = 50 });
        using var scene = new Scene(control);

        var text = Assert.IsType<TextBlock>(PresenterOf(control).Child);
        Assert.Equal("Mine", text.Name);
        Assert.Equal(50, text.Bounds.Height, 1);
    }

    // The phone's template draws no Background, BorderBrush or BorderThickness: a ContentControl given them draws only
    // its content, and neither its padding nor the space behind a transparent content takes hits. Avalonia's own draws
    // them, and is hit there. The points: the padding's corners and sides, and inside the 20-square content at 140, 140,
    // where a presenter drawing them would put its border (140 to 145) and its background.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Background_And_Border_Draw_Nothing_And_The_Padding_Takes_No_Hits(string variant)
    {
        var content = new Border { Width = 20, Height = 20 };
        var control = Themed(content);
        control.Width = 200;
        control.Height = 100;
        control.Padding = new Thickness(40);
        control.Background = new SolidColorBrush(Color.FromUInt32(0xFFFF0000u));
        control.BorderBrush = new SolidColorBrush(Color.FromUInt32(0xFF00FF00u));
        control.BorderThickness = new Thickness(5);
        var canvas = new Canvas { Children = { control } };
        Canvas.SetLeft(control, 100);
        Canvas.SetTop(control, 100);
        using var scene = new Scene(canvas, variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        using var frame = scene.Capture();
        var page = Frames.PixelAt(frame, 50, 50);                              // outside the control
        foreach (var (x, y) in new[] { (102.0, 102.0), (110.0, 150.0), (250.0, 120.0), (298.0, 198.0), (142.0, 142.0), (150.0, 150.0) })
            Assert.Equal(page, Frames.PixelAt(frame, (int)(x * Scene.ReferenceScaling), (int)(y * Scene.ReferenceScaling)));

        foreach (var point in new[] { new Point(110, 110), new Point(150, 150) })
        {
            var hit = scene.Window.InputHitTest(point);
            Assert.True(hit is null || !ReferenceEquals(hit, control) && !control.IsVisualAncestorOf((Visual)hit), $"{hit} took the hit at {point}");
        }
    }

    // The theme is keyed: a bare ContentControl keeps Avalonia's own template under Wp81, which binds the Background, so
    // the controls Wp81 doesn't theme yet keep their ContentControls as Simple lays them out, and an app's page host
    // stretches its page. docs/controls/ContentControl/README.md says what turning it on by type takes.
    [AvaloniaFact]
    public void A_Bare_ContentControl_Keeps_Avalonias_Own()
    {
        var control = new ContentControl { Content = new Border { Width = 60, Height = 40 }, Width = 200, Height = 100 };
        using var scene = new Scene(new Canvas { Children = { control } });

        Assert.Null(control.Theme);
        Assert.False(ThemedByType(scene, typeof(ContentControl)));
        Assert.Equal(HorizontalAlignment.Stretch, control.HorizontalContentAlignment);
        Assert.Equal(new Rect(0, 0, 200, 100), BoundsIn(PresenterOf(control), control));
    }

    // The phone's ContentPresenter style (Left, Top) isn't applied to a presenter in a template: the ToggleSwitch's
    // SwitchCurtainBounds and SwitchCurtainClip, two presenters with nothing set round a 64-wide Rectangle, fill the
    // InnerBorder's 66.25 × 21.6667 inside its 2.5 border, 61.25 × 16.6667, and read Stretch (run 20261108-1732-dev11,
    // ToggleSwitch__on__base). Wp81 has no theme for the bare type, so Avalonia's own presenters fill their slots too.
    [AvaloniaFact]
    public void A_Template_Presenter_With_Nothing_Set_Fills_Its_Slot_As_The_Phones_Do()
    {
        var presenters = new List<ContentPresenter>();
        var host = new ContentControl
        {
            Theme = new ControlTheme(typeof(ContentControl))
            {
                Setters =
                {
                    new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>((_, _) =>
                    {
                        presenters.Clear();
                        var clip = new ContentPresenter { Name = "SwitchCurtainClip", Content = new Rectangle { Width = 64 } };
                        var bounds = new ContentPresenter { Name = "SwitchCurtainBounds", Content = clip };
                        presenters.AddRange([bounds, clip]);
                        return new Border { Name = "InnerBorder", Width = 159 / Scene.ReferenceScaling, Height = 52 / Scene.ReferenceScaling, BorderThickness = new Thickness(2.5), Child = bounds };
                    })),
                },
            },
        };
        using var scene = new Scene(new Canvas { Children = { host } });

        Assert.False(ThemedByType(scene, typeof(ContentPresenter)));
        Assert.Equal(2, presenters.Count);
        foreach (var presenter in presenters)
        {
            Assert.Equal(HorizontalAlignment.Stretch, presenter.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Stretch, presenter.VerticalAlignment);
            Assert.Equal(61.25, presenter.Bounds.Width, 3);
            Assert.Equal(16.6667, presenter.Bounds.Height, 3);
        }
    }

    // A ContentTemplate set after a string, that doesn't match it, leaves the presenter's TextBlock in place (Avalonia's
    // default template recycles it): the line comes off that same text, as an app's template now decides it. This is
    // PhoneHeaderLine's rule (#111), held here on this presenter; the phone has no such case, as a WinRT ContentTemplate
    // applies to any content.
    [AvaloniaFact]
    public void A_ContentTemplate_Set_Later_Takes_The_Line_Off_The_Same_Text()
    {
        var control = Themed("data");
        control.FontSize = 30;
        using var scene = new Scene(new StackPanel { Children = { control } });
        var text = Assert.IsType<TextBlock>(PresenterOf(control).Child);
        Assert.Equal(30 * 1.33, text.LineHeight, 0.01);

        control.ContentTemplate = new FuncDataTemplate<int>((value, _) => new TextBlock { Text = value.ToString() });
        Dispatcher.UIThread.RunJobs();

        Assert.Same(text, PresenterOf(control).Child);
        Assert.True(double.IsNaN(text.LineHeight), $"the phone's line stayed: {text.LineHeight}");
    }
}
