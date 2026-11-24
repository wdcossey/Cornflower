using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The plain ContentControls in the templates of controls Wp81 doesn't theme yet (the holes) keep Avalonia's own, as
/// <see cref="HolePartsTests"/> shows for Buttons. Simple has no ContentControl theme, so such a part falls back to an
/// empty theme for its type (<c>HoleParts.FallBackToOwn</c>), which gives it Avalonia's own template and defaults, as
/// under Simple alone. In Avalonia 12.1.4 Simple's template for the managed file dialog holds them, and
/// Simple's DataValidationErrors (in each of that dialog's Simple TextBoxes) holds them too (ComboBox's, MenuItem's and
/// DataValidationErrors' own did, until Wp81 themed them, #155, #237 and #241: a hole's ComboBox is Wp81's, so its parts
/// aren't counted, and a DataValidationErrors given Simple's theme still holds them). Without the fallback they would take Wp81's, which puts the content at the top left
/// at its own size where Simple's template stretches it.
/// </summary>
public class HoleContentControlPartsTests
{
    /// <summary>The plain ContentControls (not their subclasses) in <paramref name="control"/>'s templates, in tree order.</summary>
    private static List<ContentControl> Plain(Control control) =>
        control.GetVisualDescendants().OfType<ContentControl>().Where(c => c.GetType() == typeof(ContentControl) && !HoleTogglePartsTests.InComboBox(c)).ToList();

    /// <summary>Whether <paramref name="theme"/> is an empty theme for ContentControl: Avalonia's own, as with Simple alone.</summary>
    private static bool IsOwn(ControlTheme? theme) =>
        theme is { TargetType: var target, BasedOn: null, Setters.Count: 0, Children.Count: 0 } && target == typeof(ContentControl);

    // Every hole shown with Simple then Wp81: as many plain ContentControls as with Simple alone, each in an empty theme
    // for its type, none in Wp81's. (Sizes are compared below for one: across all the holes, Wp81's font changes the
    // rows some of them stretch to.)
    [AvaloniaFact]
    public void Every_Holes_ContentControls_Keep_Avalonias_Own()
    {
        var problems = new List<string>();
        var fellBack = new List<string>();

        foreach (var type in Holes.Showable())
        {
            var alone = (Control)Activator.CreateInstance(type)!;
            int expected;
            using (new Scene(alone, themes: [new SimpleTheme()]))
                expected = Plain(alone).Count;

            var control = (Control)Activator.CreateInstance(type)!;
            using var scene = new Scene(control, themes: [new SimpleTheme(), new Wp81Theme()]);
            var parts = Plain(control);

            if (parts.Count != expected)
                problems.Add($"{type.Name}: {parts.Count} plain ContentControls, {expected} with Simple alone");
            foreach (var part in parts.Where(c => !IsOwn(c.Theme)))
                problems.Add($"{type.Name}: {part.Name} has {(part.Theme is null ? "Wp81's theme" : "a theme of its own")}");
            foreach (var part in parts.Where(c => IsOwn(c.Theme) && c.HorizontalContentAlignment == HorizontalAlignment.Stretch))
                fellBack.Add(type.Name);
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        Assert.Contains("ManagedFileChooser", fellBack);
    }

    // The two flags in either order in the theme: whichever comes in last does the check, so the part falls back.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Either_Order_Of_The_Two_Setters_Falls_Back(bool fallBackFirst)
    {
        var wp81 = new Wp81Theme();
        Assert.True(wp81.TryGetResource("Wp81ContentControlTheme", null, out var keyed));
        Setter fallBack = new(HoleParts.FallBackProperty, true), own = new(HoleParts.FallBackToOwnProperty, true);
        var theme = new ControlTheme(typeof(ContentControl)) { BasedOn = (ControlTheme)keyed! };
        theme.Setters.Add(fallBackFirst ? fallBack : own);
        theme.Setters.Add(fallBackFirst ? own : fallBack);
        wp81.Resources[typeof(ContentControl)] = theme;

        // Given Simple's theme, as Wp81 themes DataValidationErrors (#241).
        var simple = new SimpleTheme();
        var errors = new DataValidationErrors { Content = "content", Theme = SimplesErrors(simple) };
        using var scene = new Scene(errors, themes: [simple, wp81]);

        var parts = Plain(errors);
        Assert.NotEmpty(parts);
        Assert.All(parts, c => Assert.True(IsOwn(c.Theme)));
    }

    /// <summary>
    /// A theme to put under Wp81: an <see cref="HolePartsTests.AppControl"/> template holding <paramref name="inner"/>,
    /// and the <paramref name="extra"/> themes beside it.
    /// </summary>
    private static Styles Under(Func<Control> inner, params (Type Key, ControlTheme Theme)[] extra)
    {
        var template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new Border { Child = inner() });
        var under = new Styles
        {
            Resources = { [typeof(HolePartsTests.AppControl)] = new ControlTheme(typeof(HolePartsTests.AppControl)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } } },
        };
        foreach (var (key, theme) in extra)
            under.Resources[key] = theme;
        return under;
    }

    // A ContentControl in a template from under Wp81, where that theme has its own theme for ContentControl: it takes
    // that one, as a Button takes the Button theme under Wp81.
    [AvaloniaFact]
    public void A_ContentControl_Takes_The_Theme_Under_Wp81_Where_There_Is_One()
    {
        var host = new HolePartsTests.AppControl();
        var underContent = new ControlTheme(typeof(ContentControl));
        using var scene = new Scene(host, themes: [Under(() => new ContentControl { Name = "Inner" }, (typeof(ContentControl), underContent)), new Wp81Theme()]);

        Assert.Same(underContent, Plain(host).Single().Theme);
    }

    // The empty theme is for the part's own style key: a control with a key of its own whose Wp81 theme sets the flags
    // gets an empty theme for that key, not ContentControl's.
    [AvaloniaFact]
    public void The_Empty_Theme_Is_For_The_Parts_Own_Style_Key()
    {
        var wp81 = new Wp81Theme();
        wp81.Resources[typeof(Note)] = new ControlTheme(typeof(Note))
        {
            Setters = { new Setter(HoleParts.FallBackToOwnProperty, true), new Setter(HoleParts.FallBackProperty, true) },
        };
        var host = new HolePartsTests.AppControl();
        using var scene = new Scene(host, themes: [Under(() => new Note { Name = "Inner" }), wp81]);

        var note = host.GetVisualDescendants().OfType<Note>().Single();
        Assert.Equal(typeof(Note), note.Theme?.TargetType);
        Assert.Empty(note.Theme!.Setters);
    }

    // A theme an app brings in from a file, with StyleInclude, under Wp81: its template's plain Button takes Simple's
    // Button theme and its plain ContentControl Avalonia's own, as from a theme added in code.
    [AvaloniaFact]
    public void A_Template_From_A_StyleInclude_Under_Wp81_Falls_Back()
    {
        var include = Include("UnderWp81.axaml");
        var simple = new SimpleTheme();
        var pager = new PipsPager();
        using var scene = new Scene(pager, themes: [simple, include, new Wp81Theme()]);

        var button = pager.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "Inner");
        Assert.True(simple.TryGetResource(typeof(Button), ThemeVariant.Dark, out var simpleButton));
        Assert.Same(simpleButton, button.Theme);
        Assert.True(IsOwn(Plain(pager).Single().Theme));
    }

    /// <summary>Simple's own DataValidationErrors theme, which holds plain ContentControls (Wp81's has none of Simple's).</summary>
    private static ControlTheme SimplesErrors(SimpleTheme simple)
    {
        Assert.True(simple.TryGetResource(typeof(DataValidationErrors), ThemeVariant.Dark, out var theme));
        return (ControlTheme)theme!;
    }

    /// <summary>A file of this assembly, brought in with StyleInclude, as an app's App.axaml brings in its themes.</summary>
    private static StyleInclude Include(string file)
    {
        var baseUri = new Uri("avares://Cornflower.Tests/");
        return new StyleInclude(baseUri) { Source = new Uri(baseUri, "Completeness/Fixture/" + file) };
    }

    // An app's themes in an included file, Simple then Wp81: Wp81 is found inside it, and the holes fall back.
    [AvaloniaFact]
    public void Wp81_In_An_Included_File_Finds_The_Theme_Under_It()
    {
        var pager = new PipsPager();
        var include = Include("SimpleThenWp81.axaml");
        var errors = new DataValidationErrors { Content = "content", Theme = SimplesErrors(((Styles)include.Loaded).OfType<SimpleTheme>().Single()) };
        using var scene = new Scene(new StackPanel { Children = { pager, errors } }, themes: [include]);

        var buttons = pager.GetVisualDescendants().OfType<Button>().Where(b => b.GetType() == typeof(Button)).ToList();
        Assert.NotEmpty(buttons);
        Assert.All(buttons, b => Assert.NotNull(b.Theme));
        Assert.NotEmpty(Plain(errors));
        Assert.All(Plain(errors), c => Assert.True(IsOwn(c.Theme)));
    }

    // A file holding another Wp81, under Wp81: its templates aren't a theme under Wp81, so a part in one of them
    // keeps Wp81's (here the PasswordBox template's plain CheckBox, given the included Wp81's PasswordBox theme).
    [AvaloniaFact]
    public void An_Included_Wp81_Under_Wp81_Isnt_A_Theme_Under_It()
    {
        var inner = Include("Wp81Only.axaml");
        var outer = new Wp81Theme();
        var box = new TextBox { Classes = { "revealPasswordButton" } };
        using var scene = new Scene(box, themes: [new SimpleTheme(), inner, outer]);
        Assert.True(inner.TryGetResource("PasswordBox", null, out var innerTheme));
        Assert.True(outer.TryGetResource("PasswordBox", null, out var outerTheme));
        Assert.NotSame(outerTheme, innerTheme);
        box.Theme = (ControlTheme)innerTheme!;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var reveal = box.GetVisualDescendants().OfType<CheckBox>().Single();
        Assert.Null(reveal.Theme);
    }

    /// <summary>A ContentControl with a style key of its own.</summary>
    public class Note : ContentControl;

    // An app's own templated control isn't a hole: a plain ContentControl in its template wears Wp81's.
    [AvaloniaFact]
    public void A_ContentControl_In_An_Apps_Own_Template_Is_Wp81s()
    {
        var mine = new HolePartsTests.AppControl
        {
            Template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new Border { Child = new ContentControl { Name = "Inner" } }),
        };
        using var scene = new Scene(mine);

        var inner = Plain(mine).Single();
        Assert.Null(inner.Theme);
        Assert.Equal(HorizontalAlignment.Left, inner.HorizontalContentAlignment);
    }
}
