using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The ToggleButtons and RepeatButtons in the templates of controls Wp81 doesn't theme yet (the holes) keep the theme
/// under Wp81 for their own type, as <see cref="HolePartsTests"/> shows for Buttons. In Avalonia 12.1.4, Simple's
/// template for DrawerPage (three toggles) holds plain ToggleButtons with no theme of their own: read from Simple's
/// sources at the 12.1.4 tag. Wp81 themes DrawerPage now (#239), with its own part theme for them, so no hole holds one;
/// a DrawerPage given Simple's theme still does, and without HoleParts they would take Wp81's, with the phone's
/// 109 × 57.5 minimum and overhang. The menu ScrollViewer's two plain RepeatButtons were
/// in ContextMenu's, Menu's and MenuItem's until Wp81 themed them (#237): Wp81's ContextMenu has no ScrollViewer, and
/// its sub-menus' is Wp81's. ComboBox was one until Wp81 themed it (#155); a hole's ComboBox (the managed file dialog's) is Wp81's, so its
/// parts aren't counted. Simple's ScrollBar held eight more; Wp81 themes ScrollBar now (#116), and its template has none.
/// </summary>
public class HoleTogglePartsTests
{
    private static readonly Type[] Kinds = [typeof(ToggleButton), typeof(RepeatButton)];

    /// <summary>The plain ToggleButtons and RepeatButtons (not their subclasses) in <paramref name="control"/>'s templates.</summary>
    private static List<Button> Plain(Control control) =>
        control.GetVisualDescendants().OfType<Button>().Where(b => Kinds.Contains(b.GetType()) && !InComboBox(b)).ToList();

    /// <summary>In a ComboBox's template: Wp81 themes ComboBox (#155), so a hole that holds one shows Wp81's.</summary>
    internal static bool InComboBox(Avalonia.Visual part) => part.GetVisualAncestors().OfType<ComboBox>().Any();

    private static ControlTheme ThemeOf(IResourceNode theme, Type type) =>
        theme.TryGetResource(type, ThemeVariant.Dark, out var found) ? (ControlTheme)found! : throw new InvalidOperationException($"No {type.Name} theme.");

    // Every hole shown with Simple then Wp81: the same plain ToggleButtons and RepeatButtons as with Simple alone, none in
    // Wp81's theme, and those that had none of their own in Simple's theme for their own type.
    [AvaloniaFact]
    public void Every_Holes_ToggleButtons_And_RepeatButtons_Keep_The_Theme_Under_Wp81()
    {
        var problems = new List<string>();
        var fellBack = new List<string>();

        foreach (var type in Holes.Showable())
        {
            var alone = (Control)Activator.CreateInstance(type)!;
            List<Type> expected;
            using (new Scene(alone, themes: [new SimpleTheme()]))
                expected = Plain(alone).Select(b => b.GetType()).ToList();

            var simple = new SimpleTheme();
            var control = (Control)Activator.CreateInstance(type)!;
            using var scene = new Scene(control, themes: [simple, new Wp81Theme()]);
            var parts = Plain(control);

            if (!parts.Select(b => b.GetType()).SequenceEqual(expected))
                problems.Add($"{type.Name}: {parts.Count} plain ToggleButtons and RepeatButtons, {expected.Count} with Simple alone");
            foreach (var part in parts.Where(b => b.Theme is null))
                problems.Add($"{type.Name}: {part.GetType().Name} {part.Name} has Wp81's theme");
            foreach (var part in parts.Where(b => b.Theme is { } theme && theme.TargetType != b.GetType() && Kinds.Contains(theme.TargetType)))
                problems.Add($"{type.Name}: {part.GetType().Name} {part.Name} has the {part.Theme!.TargetType!.Name} theme");
            foreach (var part in parts.Where(b => ReferenceEquals(b.Theme, ThemeOf(simple, b.GetType()))))
                fellBack.Add($"{type.Name}.{part.GetType().Name}.{part.Name}");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // A DrawerPage given Simple's own theme (Wp81 themes it, #239) keeps Simple's ToggleButton for its three toggles.
    [AvaloniaFact]
    public void A_DrawerPage_In_Simples_Theme_Keeps_Simples_ToggleButtons()
    {
        var simple = new SimpleTheme();
        var page = new DrawerPage { Theme = ThemeOf(simple, typeof(DrawerPage)) };
        using var scene = new Scene(page, themes: [simple, new Wp81Theme()]);

        var toggles = Plain(page).Where(b => b.GetType() == typeof(ToggleButton)).ToList();
        Assert.Equal(3, toggles.Count);
        Assert.All(toggles, b => Assert.Same(ThemeOf(simple, typeof(ToggleButton)), b.Theme));
    }

    // An app's own templated control isn't a hole: its ToggleButton and RepeatButton are the phone's.
    [AvaloniaFact]
    public void In_An_Apps_Own_Template_They_Are_Wp81s()
    {
        var mine = new HolePartsTests.AppControl
        {
            Template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new StackPanel
            {
                Children = { new ToggleButton { Name = "Toggle", Content = "t" }, new RepeatButton { Name = "Repeat", Content = "r" } },
            }),
        };
        using var scene = new Scene(mine);

        Assert.All(Plain(mine), b =>
        {
            Assert.Null(b.Theme);
            Assert.Equal(109, b.MinWidth);
        });
        Assert.Equal(2, Plain(mine).Count);
    }

    // The theme under Wp81 for the control's own type only: with a Button theme under Wp81 and no ToggleButton theme, a
    // ToggleButton in a template from under Wp81 keeps Wp81's rather than take the Button's.
    [AvaloniaFact]
    public void With_No_Theme_Under_Wp81_For_Its_Type_It_Keeps_Wp81s()
    {
        var host = new HolePartsTests.AppControl();
        var template = new FuncControlTemplate<HolePartsTests.AppControl>((_, _) => new ToggleButton { Name = "Inner" });
        var under = new Styles
        {
            Resources =
            {
                [typeof(HolePartsTests.AppControl)] = new ControlTheme(typeof(HolePartsTests.AppControl)) { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } },
                [typeof(Button)] = new ControlTheme(typeof(Button)),
            },
        };
        using var scene = new Scene(host, themes: [under, new Wp81Theme()]);

        var inner = Plain(host).Single();
        Assert.Null(inner.Theme);
        Assert.Equal(109, inner.MinWidth);
    }
}
