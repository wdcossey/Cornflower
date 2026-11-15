using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The plain ContentControls in the templates of controls Wp81 doesn't theme yet (the holes) keep Avalonia's own, as
/// <see cref="HolePartsTests"/> shows for Buttons. Simple has no ContentControl theme, so such a part falls back to an
/// empty theme for its type (<c>HoleParts.FallBackToOwn</c>), which gives it Avalonia's own template and defaults, as
/// under Simple alone. In Avalonia 12.1.4 Simple's templates for ComboBox, DataValidationErrors, MenuItem and the
/// managed file dialog hold them. Without the fallback they would take Wp81's, which puts the content at the top left
/// at its own size where Simple's template stretches it.
/// </summary>
public class HoleContentControlPartsTests
{
    /// <summary>Every hole that can be shown on its own in a window, as <see cref="HolePartsTests"/> shows them.</summary>
    private static IEnumerable<Type> ShowableHoles() =>
        Holes.TemplatedControls(new SimpleTheme())
            .Where(type => Holes.Listed().Contains(type.Name))
            .Where(type => !typeof(TopLevel).IsAssignableFrom(type) && type != typeof(OverlayPopupHost) && type.GetConstructor(Type.EmptyTypes) is not null);

    /// <summary>The plain ContentControls (not their subclasses) in <paramref name="control"/>'s templates, in tree order.</summary>
    private static List<ContentControl> Plain(Control control) =>
        control.GetVisualDescendants().OfType<ContentControl>().Where(c => c.GetType() == typeof(ContentControl)).ToList();

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

        foreach (var type in ShowableHoles())
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
        Assert.Contains(nameof(DataValidationErrors), fellBack);
        Assert.Contains(nameof(MenuItem), fellBack);
    }

    // A MenuItem's icon slot, Simple's ContentControl: as large and as placed as with Simple alone.
    [AvaloniaFact]
    public void A_MenuItems_Icon_Keeps_Simples_Layout()
    {
        var alone = new MenuItem { Header = "item", Icon = new Border { Width = 10, Height = 10 } };
        using var only = new Scene(alone, themes: [new SimpleTheme()]);
        var expected = Plain(alone).Select(c => (c.Name, c.Bounds)).ToList();

        var item = new MenuItem { Header = "item", Icon = new Border { Width = 10, Height = 10 } };
        using var scene = new Scene(item, themes: [new SimpleTheme(), new Wp81Theme()]);
        var parts = Plain(item);

        Assert.NotEmpty(parts);
        Assert.Equal(expected, parts.Select(c => (c.Name, c.Bounds)).ToList());
        Assert.All(parts, c => Assert.True(IsOwn(c.Theme)));
    }

    // A ContentControl in a template from under Wp81, where that theme has its own theme for ContentControl: it takes
    // that one, as a Button takes the Button theme under Wp81.
    [AvaloniaFact]
    public void A_ContentControl_Takes_The_Theme_Under_Wp81_Where_There_Is_One()
    {
        var host = new HolePartsTests.AppControl();
        var underContent = new ControlTheme(typeof(ContentControl));
        var template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new Border { Child = new ContentControl { Name = "Inner" } });
        var under = new Styles
        {
            Resources =
            {
                [typeof(HolePartsTests.AppControl)] = new ControlTheme(typeof(HolePartsTests.AppControl)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } },
                [typeof(ContentControl)] = underContent,
            },
        };
        using var scene = new Scene(host, themes: [under, new Wp81Theme()]);

        Assert.Same(underContent, Plain(host).Single().Theme);
    }

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
