using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain DataValidationErrors (ADR 0007, #241): each error a line of small print (ControlContextualInfoTextBlockStyle)
/// under the field, in CornflowerPlainErrorBrush (the Red accent, in dark and light), wrapped, and nothing at all with no
/// errors. Wrapping a field shows the field's errors; in a template, the Owner's. Simple's is an ItemsControl of plain
/// TextBlocks in its own brush, so these fail without the theme.
/// </summary>
public class DataValidationErrorsTests
{
    private static readonly Color Red = Color.Parse("#FFE51400");

    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>A field of the kind named: Wp81's TextBox, PasswordBox (keyed, given in <see cref="Keyed"/>) or ComboBox.</summary>
    private static Control Field(string kind) => kind switch
    {
        "TextBox" => new TextBox { Text = "Ada" },
        "PasswordBox" => new TextBox { Text = "secret", PasswordChar = '*' },
        "ComboBox" => new ComboBox { ItemsSource = new[] { "one", "two" }, SelectedIndex = 0 },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Gives a keyed field its theme once it can find it.</summary>
    private static void Keyed(Control field, string kind)
    {
        if (kind == "PasswordBox")
            field.Theme = Resource<ControlTheme>(field, "PasswordBox");
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The error lines a part shows, in tree order.</summary>
    private static List<TextBlock> Lines(DataValidationErrors errors, string part) =>
        Part<ContentControl>(errors, part).GetVisualDescendants().OfType<TextBlock>().ToList();

    private static double Top(Visual visual, Visual relativeTo) => visual.TranslatePoint(default, relativeTo)!.Value.Y;

    private static double Bottom(Visual visual, Visual relativeTo) => visual.TranslatePoint(new Point(0, visual.Bounds.Height), relativeTo)!.Value.Y;

    [AvaloniaTheory]
    [InlineData("dark", "TextBox")]
    [InlineData("light", "TextBox")]
    [InlineData("dark", "PasswordBox")]
    [InlineData("light", "PasswordBox")]
    [InlineData("dark", "ComboBox")]
    [InlineData("light", "ComboBox")]
    public void An_Error_Is_A_Line_Of_Small_Print_In_Red_Under_The_Box(string variant, string kind)
    {
        var field = Field(kind);
        var errors = new DataValidationErrors { Content = field };
        var bare = Field(kind);
        using var scene = new Scene(new StackPanel { Margin = new Thickness(19, 0), Children = { errors, bare } }, Variant(variant), width: 411);
        Keyed(field, kind);
        Keyed(bare, kind);

        DataValidationErrors.SetErrors(field, ["Enter a name."]);
        Dispatcher.UIThread.RunJobs();

        var line = Assert.Single(Lines(errors, "PART_ContentErrors"));
        Assert.Equal("Enter a name.", line.Text);
        Assert.Same(Resource<ControlTheme>(errors, "ControlContextualInfoTextBlockStyle"), line.Theme);
        Assert.Same(Resource<IBrush>(errors, "CornflowerPlainErrorBrush"), line.Foreground);
        Assert.Equal(Red, Assert.IsAssignableFrom<ISolidColorBrush>(line.Foreground).Color);
        Assert.Equal(TextWrapping.Wrap, line.TextWrapping);
        Assert.Equal(16, line.FontSize, 2);

        // Under the box, outside it, and the box as it is without an error.
        Assert.DoesNotContain(line, field.GetVisualDescendants());
        Assert.True(Top(line, errors) >= Bottom(field, errors) - 0.01, $"the line's top {Top(line, errors)} is above the box's bottom {Bottom(field, errors)}");
        Assert.Equal(bare.Bounds.Size, field.Bounds.Size);
        Assert.Equal(373, line.Bounds.Width, 0);
    }

    [AvaloniaFact]
    public void With_No_Errors_It_Is_Only_The_Field()
    {
        var field = new TextBox { Text = "Ada" };
        var errors = new DataValidationErrors { Content = field };
        using var scene = new Scene(new StackPanel { Children = { errors } });
        var height = errors.Bounds.Height;

        Assert.False(Part<ContentControl>(errors, "PART_ContentErrors").IsVisible);
        Assert.False(Part<ContentControl>(errors, "PART_OwnerErrors").IsVisible);
        Assert.Equal(field.Bounds.Height + field.Margin.Top + field.Margin.Bottom, height, 0.5);

        DataValidationErrors.SetErrors(field, ["Enter a name."]);
        Dispatcher.UIThread.RunJobs();
        Assert.True(errors.Bounds.Height > height);

        DataValidationErrors.ClearErrors(field);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Part<ContentControl>(errors, "PART_ContentErrors").IsVisible);
        Assert.Equal(height, errors.Bounds.Height, 2);
    }

    [AvaloniaFact]
    public void Each_Error_Is_A_Line_Of_Its_Own_And_A_Long_One_Wraps()
    {
        var field = new TextBox();
        var errors = new DataValidationErrors { Width = 200, Content = field };
        using var scene = new Scene(new StackPanel { Children = { errors } });

        DataValidationErrors.SetErrors(field, ["Too short.", "A name has a letter in it, and this one has none at all, so it can't be one."]);
        Dispatcher.UIThread.RunJobs();

        var lines = Lines(errors, "PART_ContentErrors");
        Assert.Equal(2, lines.Count);
        Assert.True(Top(lines[1], errors) >= Bottom(lines[0], errors) - 0.01);
        Assert.True(lines[1].Bounds.Height > lines[0].Bounds.Height * 1.5, "the long error didn't wrap");
    }

    // The real path: a binding's validation error on the field's text.
    [AvaloniaFact]
    public void A_Bindings_Error_Shows()
    {
        var person = new Person();
        var field = new TextBox();
        field.Bind(TextBox.TextProperty, new Binding(nameof(Person.Name)) { Source = person, Mode = BindingMode.TwoWay });
        var errors = new DataValidationErrors { Content = field };
        using var scene = new Scene(new StackPanel { Children = { errors } });

        field.Text = "";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Enter a name.", Assert.Single(Lines(errors, "PART_ContentErrors")).Text);
    }

    public class Person
    {
        private string _name = "Ada";

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrEmpty(value) ? throw new DataValidationException("Enter a name.") : value;
        }
    }

    // In a template Avalonia sets the Owner, the templated parent; given one, it shows the Owner's errors, and not the
    // content's, which a template often passes the Owner's to (Wp81's AutoCompleteBox does): they'd show twice.
    [AvaloniaFact]
    public void It_Shows_Its_Owners_Errors_And_Not_The_Contents_Too()
    {
        var owner = new TextBox();
        var field = new TextBox();
        var errors = new DataValidationErrors { Owner = owner, Content = field };
        using var scene = new Scene(new StackPanel { Children = { owner, errors } });

        DataValidationErrors.SetErrors(owner, ["The owner's."]);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("The owner's.", Assert.Single(Lines(errors, "PART_OwnerErrors")).Text);
        Assert.False(Part<ContentControl>(errors, "PART_ContentErrors").IsVisible);

        DataValidationErrors.SetErrors(field, DataValidationErrors.GetErrors(owner));
        Dispatcher.UIThread.RunJobs();
        Assert.False(Part<ContentControl>(errors, "PART_ContentErrors").IsVisible);
        Assert.Single(errors.GetVisualDescendants().OfType<TextBlock>(), line => line.Text == "The owner's.");
    }

    // Simple's TextBox (a hole's, or one given Simple's theme) holds one inside its box: it keeps Simple's there.
    [AvaloniaFact]
    public void In_Simples_TextBox_It_Keeps_Simples()
    {
        var simple = new SimpleTheme();
        Assert.True(simple.TryGetResource(typeof(TextBox), ThemeVariant.Dark, out var simpleBox));
        Assert.True(simple.TryGetResource(typeof(DataValidationErrors), ThemeVariant.Dark, out var simpleErrors));
        var box = new TextBox { Theme = (ControlTheme)simpleBox! };
        var mine = new DataValidationErrors();
        using var scene = new Scene(new StackPanel { Children = { box, mine } }, themes: [simple, new Cornflower.Themes.Wp81.Wp81Theme()]);

        Assert.Same(simpleErrors, box.GetVisualDescendants().OfType<DataValidationErrors>().Single().Theme);
        Assert.Null(mine.Theme);
    }

    [AvaloniaFact]
    public void Errors_Dont_Scroll_The_Page()
    {
        var field = new TextBox { Text = "Ada" };
        var errors = new DataValidationErrors { Content = field };
        var page = new ScrollViewer { Height = 200, Content = new StackPanel { Children = { new Border { Height = 600 }, errors } } };
        using var scene = new Scene(new StackPanel { Children = { page } });
        Dispatcher.UIThread.RunJobs();

        Assert.False(errors.Focusable);
        DataValidationErrors.SetErrors(field, ["Enter a name."]);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, page.Offset.Y);

        DataValidationErrors.ClearErrors(field);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, page.Offset.Y);
    }
}
