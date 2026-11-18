using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Cornflower.Tests.Controls;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// <c>HoleParts</c>' first check (#194). A Button-family part in one of Wp81's templates is answered from the themes
/// Wp81 has built so far, without reading every template Wp81 sets, which builds all of its themes (about 90 ms in
/// Release, once). A part in a template from elsewhere still reads them all, and still falls back.
/// </summary>
public class HolePartsFirstCheckTests
{
    private static object TemplateOf(TemplatedControl control) =>
        control.Template ?? throw new InvalidOperationException($"{control.GetType().Name} has no template.");

    /// <summary>
    /// The entry for <paramref name="key"/> in <paramref name="wp81"/>'s dictionaries, as it is, unbuilt or built:
    /// enumerating a ResourceDictionary gives an entry not yet built as its <see cref="IDeferredContent"/>.
    /// </summary>
    private static object? RawEntry(Wp81Theme wp81, object key)
    {
        var dictionaries = new Stack<IResourceDictionary>([wp81.Resources]);
        while (dictionaries.TryPop(out var dictionary))
        {
            foreach (var entry in dictionary)
            {
                if (Equals(entry.Key, key)) return entry.Value;
            }

            foreach (var merged in dictionary.MergedDictionaries)
            {
                if ((merged is Avalonia.Markup.Xaml.Styling.ResourceInclude include ? include.Loaded : merged) is IResourceDictionary inner)
                    dictionaries.Push(inner);
            }
        }

        throw new InvalidOperationException($"Wp81 has no entry for {key}.");
    }

    // The PasswordBox's RevealButton, a CheckBox in a template of Wp81's, is the part that showed it: the passwordbox
    // page's first open. Each Scene has a Wp81Theme of its own, so nothing has read its templates before.
    [AvaloniaFact]
    public void A_Part_In_A_Wp81_Template_Builds_No_Other_Theme()
    {
        var box = PasswordBoxTests.Specimen(reveal: true);
        using var scene = new Scene(box);
        var wp81 = scene.Window.Styles.OfType<Wp81Theme>().Single();

        var reveal = box.GetVisualDescendants().OfType<CheckBox>().Single();
        Assert.Same(box, reveal.TemplatedParent);
        Assert.False(HoleParts.Templates.TryGetValue(wp81, out _), "every template of Wp81's was read");
        Assert.True(HoleParts.Built.TryGetValue(wp81, out var built), "no built templates were read");
        Assert.Contains(TemplateOf(box), built!);
        // A theme the page doesn't use, Pivot's, is still unbuilt: reading the built ones built nothing.
        Assert.IsAssignableFrom<IDeferredContent>(RawEntry(wp81, typeof(Cornflower.Controls.Pivot)));
        // Wp81's CheckBox theme, kept: the part didn't fall back, which would have set one.
        Assert.False(reveal.IsSet(Avalonia.StyledElement.ThemeProperty));
    }

    // A Button in Simple's CalendarDatePicker template isn't in any theme Wp81 has built, so the check goes on to read
    // all of Wp81's templates, finds it in none, and the Button falls back to Simple's theme, as before #194.
    [AvaloniaFact]
    public void A_Part_In_Another_Template_Still_Reads_Them_All_And_Falls_Back()
    {
        var simple = new SimpleTheme();
        var picker = new CalendarDatePicker();
        using var scene = new Scene(picker, themes: [simple, new Wp81Theme()]);
        var wp81 = scene.Window.Styles.OfType<Wp81Theme>().Single();

        var button = picker.GetVisualDescendants().OfType<Button>().First(b => b.TemplatedParent == picker);
        Assert.True(HoleParts.Templates.TryGetValue(wp81, out var all), "Wp81's templates weren't all read");
        Assert.DoesNotContain(TemplateOf(picker), all!);
        Assert.True(simple.TryGetResource(typeof(Button), ThemeVariant.Dark, out var simplesButton));
        Assert.Same(simplesButton, button.Theme);
    }
}
