using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// Keeps Wp81's Button, ToggleButton, RepeatButton, CheckBox, TextBox, ContentControl, GridSplitter and PathIcon out of templates
/// written for the theme under Wp81. Simple's templates for PipsPager and the managed
/// file dialogs hold plain Buttons with no theme of their own, the managed file
/// dialog's plain CheckBox, the plain TextBoxes of the managed file dialog (CalendarItem's and CalendarDatePicker's
/// Buttons and CalendarDatePicker's TextBox were such parts until Wp81 themed them, #240: a CalendarDatePicker given
/// Simple's theme still holds its own, while a Calendar given Simple's theme gets Wp81's CalendarItem by type), the plain
/// ContentControls of DataValidationErrors and the managed file dialog (the menus' RepeatButtons and MenuItem's
/// ContentControls were such parts until Wp81 themed ContextMenu, Menu and MenuItem, #237, and DrawerPage's
/// ToggleButtons and PathIcon until Wp81 themed DrawerPage, #239: a DrawerPage given Simple's theme still holds them), and
/// the managed file dialog's GridSplitters, so they would take Wp81's,
/// keyed by type: the phone's sizes (a Button's 109 × 57.5 minimum), its overhang, the accent and, for a Button or a
/// CheckBox, the tilt, in chrome laid out for Simple's; for a TextBox, the phone's black text and white box over the
/// brushes those templates pass it; for a ContentControl, its content at the top left at its own size where Simple's
/// stretches it; for a GridSplitter, the plain theme's bar (#235); for a PathIcon, the plain theme's, with no size of its own (#236). Such a control takes the theme under Wp81 for its own style key instead: a ToggleButton the
/// ToggleButton theme, not the Button theme. A ContentControl, for which Simple has no theme, takes Avalonia's own
/// (<see cref="FallBackToOwnProperty"/>).
/// </summary>
/// <remarks>
/// <para><b>Which templates.</b> The templated parent's own template decides: a control falls back when that template
/// is one a <c>ControlTheme</c> under Wp81 sets (in its setters or its nested styles) and none of Wp81's does. So it
/// is the theme the parent wears that counts, not its type: an app's subclass with a theme of its own, an app's theme
/// for a control Wp81 doesn't theme, and a control Wp81 themes all keep Wp81's Button (or TextBox, or ContentControl) in their templates, and a
/// control given a theme from under Wp81 explicitly falls back. A new template (a new theme, or the theme stack
/// changing at run time) makes new parts, which are checked as they are styled.</para>
/// <para><b>Under Wp81</b> is Avalonia's lookup order: the styles before <see cref="Wp81Theme"/> in the
/// <see cref="Styles"/> that holds it, a nested group's included, then what holds that group, then each outer host's
/// resources and styles. With nothing under Wp81, nothing falls back. Wp81's own templates are checked first, so a
/// Button in a template Wp81 sets goes no further. The templates each theme (a <see cref="Styles"/> subclass) sets
/// are read once and kept beside it; a plain group of styles and a host's own resources, which an app can change as
/// it runs, are read each time.</para>
/// <para><b>On and off.</b> Only Wp81's themes set <see cref="FallBackProperty"/> (Button's, DropDownButton's,
/// ToggleButton's, RepeatButton's, CheckBox's, TextBox's, ContentControl's, GridSplitter's and PathIcon's) and <see cref="FallBackToOwnProperty"/>
/// (ContentControl's): they are internal, with no accessors. It goes false when that theme leaves
/// the control, which is what falling back does itself, so false undoes nothing; the control keeps the theme it was
/// given. It costs only the controls whose theme sets it, and no global rule (<c>StyleBudgetTests</c>): every Wp81
/// Button, CheckBox, TextBox, plain ContentControl, GridSplitter and PathIcon, in any template, an app's included. One with no templated parent
/// returns at once, and so does one with nothing under Wp81; one in a template of Wp81's, after one walk up its styling
/// parents, and the first in each template newly built also a read of the themes Wp81 has built; one in any other
/// template also reads all of Wp81's templates (once) and those under Wp81 (a theme's once, a plain group's each
/// time).</para>
/// <para><b>Wp81's own, without building them all</b> (#194). Reading every template Wp81 sets builds all of its
/// themes, about 90 ms in Release the first time. A template in use came from a theme already built, so the themes
/// built so far are read first (<see cref="IsWp81s"/>), and read again when a template isn't among them, as more may
/// have been built since: enough for a part in any template of Wp81's. Only a part in a template from elsewhere goes
/// on to read them all, once, and only when something lies under Wp81 to fall back to.</para>
/// <para><b>Included files.</b> A <c>StyleInclude</c> is read as what it loaded: Wp81 found inside one, and a file under
/// Wp81 read as a theme under it.</para>
/// </remarks>
internal static class HoleParts
{
    /// <summary>
    /// Set by Wp81's themes for a Button and its subclasses, a CheckBox, a TextBox, a ContentControl, a GridSplitter and a PathIcon: whether the
    /// control is in a template written for the theme under Wp81.
    /// </summary>
    public static readonly AttachedProperty<bool> FallBackProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, bool>("FallBack", typeof(HoleParts));

    /// <summary>
    /// Set by Wp81's ContentControl theme: where the theme under Wp81 has no theme for the control's style key (Simple
    /// has none for ContentControl), the control falls back to an empty <c>ControlTheme</c> for its key, so Avalonia's
    /// own template and defaults, as under that theme alone. Off, such a control keeps Wp81's, as the other parts do
    /// (<c>HoleTogglePartsTests.With_No_Theme_Under_Wp81_For_Its_Type_It_Keeps_Wp81s</c>).
    /// </summary>
    public static readonly AttachedProperty<bool> FallBackToOwnProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, bool>("FallBackToOwn", typeof(HoleParts));

    /// <summary>The empty theme for each style key, made once: Avalonia's own template and defaults for that type.</summary>
    private static readonly ConditionalWeakTable<Type, ControlTheme> Bare = new();

    /// <summary>Each theme's templates, read once, kept beside the theme.</summary>
    internal static readonly ConditionalWeakTable<object, HashSet<object>> Templates = new();

    /// <summary>The templates of the themes each <see cref="Wp81Theme"/> has built so far, as last read (#194).</summary>
    internal static readonly ConditionalWeakTable<Wp81Theme, HashSet<object>> Built = new();

    static HoleParts()
    {
        FallBackProperty.Changed.AddClassHandler<TemplatedControl>((control, e) =>
        {
            if (e.GetNewValue<bool>()) Check(control);
        });
        // Whichever of a theme's two setters comes in last does the check.
        FallBackToOwnProperty.Changed.AddClassHandler<TemplatedControl>((control, e) =>
        {
            if (e.GetNewValue<bool>() && control.GetValue(FallBackProperty)) Check(control);
        });
    }

    private static void Check(TemplatedControl control)
    {
        if (control.TemplatedParent is not TemplatedControl { Template: { } template } || control.IsSet(StyledElement.ThemeProperty))
            return;

        var (wp81, below) = Under(control);
        // With nothing under Wp81 nothing falls back, so whose template it is doesn't matter (#194, the review).
        if (wp81 is null || below.Count == 0 || IsWp81s(wp81, template))
            return;

        var variant = control.ActualThemeVariant;
        foreach (var node in below)
        {
            if (!TemplatesOf(node).Contains(template))
                continue;

            if (ThemeFor(control.StyleKey, below, variant) is { } fallback)
                control.Theme = fallback;
            else if (control.GetValue(FallBackToOwnProperty))
                control.Theme = Bare.GetValue(control.StyleKey, key => new ControlTheme(key));
            return;
        }
    }

    /// <summary>
    /// Whether <paramref name="template"/> is one <paramref name="wp81"/> sets. The templates of the themes it has built
    /// so far answer yes for a template in use, with nothing built to find out; they're read again when they don't, as
    /// more may have been built since. Only a no from them reads every template, building every theme, once (#194).
    /// </summary>
    private static bool IsWp81s(Wp81Theme wp81, object template)
    {
        if (Templates.TryGetValue(wp81, out var all))
            return all.Contains(template);

        var built = Built.GetValue(wp81, _ => new HashSet<object>(ReferenceEqualityComparer.Instance));
        if (built.Contains(template))
            return true;
        Collect(wp81, built, new HashSet<object>(ReferenceEqualityComparer.Instance), builtOnly: true);
        if (built.Contains(template))
            return true;

        // The full set answers from now on, so the built one goes.
        Built.Remove(wp81);
        return TemplatesOf(wp81).Contains(template);
    }

    /// <summary>
    /// The first <c>ControlTheme</c> under Wp81 keyed by <paramref name="key"/>, the control's style key, and targeting
    /// it: for a ToggleButton the ToggleButton theme, never the Button theme.
    /// </summary>
    private static ControlTheme? ThemeFor(Type key, List<object> below, ThemeVariant variant)
    {
        foreach (var node in below)
        {
            if (node is IResourceNode resources && resources.TryGetResource(key, variant, out var found) &&
                found is ControlTheme { TargetType: var target } theme && target == key)
                return theme;
        }

        return null;
    }

    /// <summary>The <see cref="Wp81Theme"/> <paramref name="element"/> is styled by, and what lies under it, nearest first.</summary>
    private static (Wp81Theme? Wp81, List<object> Below) Under(StyledElement element)
    {
        var below = new List<object>();
        Wp81Theme? wp81 = null;

        for (IStyleHost? host = element; host is not null; host = host.StylingParent)
        {
            if (wp81 is not null)
            {
                // An outer host: its resources, then its styles, as Avalonia looks them up.
                if (host is IResourceNode outer) below.Add(outer);
                if (host.IsStylesInitialized) below.Add(host.Styles);
                continue;
            }

            if (host.IsStylesInitialized)
                wp81 = Find(host.Styles, below);
        }

        return (wp81, below);
    }

    /// <summary>
    /// The last <see cref="Wp81Theme"/> in <paramref name="styles"/>, nested groups included; and into
    /// <paramref name="below"/>, everything before it, nearest first.
    /// </summary>
    private static Wp81Theme? Find(IReadOnlyList<IStyle> styles, List<object> below)
    {
        for (var i = styles.Count - 1; i >= 0; i--)
        {
            var found = styles[i] switch
            {
                Wp81Theme theme => theme,
                Styles group => Find(group, below),
                StyleInclude { Loaded: Wp81Theme theme } => theme,       // a file whose root is Wp81
                StyleInclude { Loaded: Styles group } => Find(group, below),
                _ => null,
            };
            if (found is null)
                continue;

            for (var j = i - 1; j >= 0; j--)
                below.Add(styles[j]);
            return found;
        }

        return null;
    }

    /// <summary>Every template a <c>ControlTheme</c> in <paramref name="node"/> sets, read once per node.</summary>
    private static HashSet<object> TemplatesOf(object node)
    {
        if (Templates.TryGetValue(node, out var known))
            return known;

        var found = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Collect(node, found, new HashSet<object>(ReferenceEqualityComparer.Instance));

        // A theme (Wp81Theme, SimpleTheme, any Styles subclass), or a StyleInclude whose file's root is one, is read once.
        // What an app holds and can change as it runs, a plain group of styles (an included file's too) or a host's
        // own resources, is read each time: it is small.
        if ((node is StyleInclude { Loaded: { } loaded } ? loaded : node) is Styles theme && theme.GetType() != typeof(Styles))
            Templates.AddOrUpdate(node, found);
        return found;
    }

    private static void Collect(object? node, HashSet<object> found, HashSet<object> seen, bool builtOnly = false)
    {
        if (node is null || !seen.Add(node))
            return;

        switch (node)
        {
            case Wp81Theme when seen.Count > 1:
                return;                                          // another Wp81 inside what is under this one
            case ResourceInclude include:
                Collect(include.Loaded, found, seen, builtOnly);
                return;
            case StyleInclude include:                       // a theme an app brings in from a file; a Wp81 root is skipped above
                Collect(include.Loaded, found, seen, builtOnly);
                return;
            case ControlTheme theme:
                CollectTemplates(theme, found);
                return;
            case IResourceDictionary dictionary:
                // Enumerated, an entry not yet built is still its IDeferredContent: read only the built ones, or build it.
                foreach (var entry in dictionary)
                {
                    var value = entry.Value;
                    if (value is IDeferredContent && (builtOnly || !dictionary.TryGetValue(entry.Key, out value)))
                        continue;
                    Collect(value, found, seen, builtOnly);
                }

                foreach (var variant in dictionary.ThemeDictionaries.Values)
                    Collect(variant, found, seen, builtOnly);
                foreach (var merged in dictionary.MergedDictionaries)
                    Collect(merged, found, seen, builtOnly);
                return;
            case Styles styles:
                Collect(styles.Resources, found, seen, builtOnly);
                foreach (var style in styles)
                    Collect(style, found, seen, builtOnly);
                return;
            case StyledElement element:
                Collect(element.Resources, found, seen, builtOnly);
                return;
            case Application application:
                Collect(application.Resources, found, seen, builtOnly);
                return;
            case StyleBase style:
                Collect(style.Resources, found, seen, builtOnly);
                return;
        }
    }

    private static void CollectTemplates(StyleBase style, HashSet<object> found)
    {
        foreach (var setter in style.Setters)
        {
            if (setter is Setter { Value: IControlTemplate template } templateSetter && templateSetter.Property == TemplatedControl.TemplateProperty)
                found.Add(template);
        }

        foreach (var child in style.Children)
        {
            if (child is StyleBase nested) CollectTemplates(nested, found);
        }
    }
}
