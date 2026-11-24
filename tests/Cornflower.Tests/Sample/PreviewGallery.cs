using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Sample;

namespace Cornflower.Tests.Sample;

/// <summary>
/// The pictures for <c>PREVIEW.md</c>: every page of the sample, in the sample's shell at the reference view (450 ×
/// 800, without the desktop's Back rail, as on a phone), dark and light, and the pages that show the accent in two
/// more of the phone's accents, and the pages that open a popup again with it open (<see cref="PopupShots"/>), at rest
/// after its motion; <c>PREVIEW.md</c> and <c>README.md</c> show a chosen set of them, committed under
/// <c>docs/preview/</c>. Drawn headless with Skia, as the comparison frames are, so they are the theme's own pixels
/// on any OS; at a render scaling of 2, not the reference profile's 2.4, so a picture may differ from a comparison
/// frame by a pixel of rounding. By hand, as the <c>*Frames</c> savers are: it does nothing unless
/// <c>CORNFLOWER_PREVIEW</c> names a new or empty folder.
/// <code>
/// CORNFLOWER_PREVIEW=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~PreviewGallery
/// </code>
/// Each picture is <c>&lt;variant&gt;__&lt;page&gt;[__&lt;accent&gt;|__&lt;popup&gt;].png</c>, 900 × 1600.
/// </summary>
public class PreviewGallery
{
    /// <summary>
    /// The render scaling of the pictures, so 900 × 1600: sharp on a high-density screen at the 250 to 270 wide the
    /// Markdown shows them, and small enough to commit (about 55 KB each).
    /// </summary>
    private const double Scaling = 2;

    /// <summary>Other accents, of the phone's twenty, far from the default Cobalt, for the pages that show one.</summary>
    private static readonly string[] OtherAccents = ["Lime", "Magenta"];

    /// <summary>The pages whose accent shows at rest, drawn again in each of <see cref="OtherAccents"/>.</summary>
    private static readonly string[] AccentPages = ["toggleswitch", "slider", "progressbar"];

    /// <summary>The header of the switch some pages have that turns the page light; set to match the picture.</summary>
    private const string LightSwitchHeader = "light theme";

    /// <summary>The pages known to have that switch: the gallery stops if one has lost it, rather than draw it Off.</summary>
    private static readonly string[] LightSwitchPages =
        ["commandbar", "pivot", "hub", "flyout", "menu flyout", "listpicker", "contentdialog", "loopingselector", "datetimepicker"];

    /// <summary>
    /// The pages drawn again with a popup open: the page's title, the popup's name in the file, and how it is opened
    /// (a page's control touched by its name, or text typed in a named box), as a page at rest shows them closed;
    /// the LoopingSelector's page with a wheel tapped, the active wheel showing its items; and the ComboBox's page with
    /// its five items open inline, the box grown in place (its only popup Avalonia's empty <c>PART_Popup</c>).
    /// </summary>
    private static readonly PopupShot[] PopupShots =
    [
        new("flyout", "top", Tap<Button>("OpenTop")),
        new("flyout", "full", Tap<Button>("OpenFull")),
        new("menu flyout", "menu", Tap<Button>("OpenMenu")),
        new("listpicker", "single", Tap<Button>("OpenSingle")),
        new("listpicker", "multiple", Tap<Button>("OpenMultiple")),
        new("listpicker", "picker", Tap<Button>("OpenPicker")),
        new("contentdialog", "dialog", Tap<Button>("ShowTwo")),
        new("contentdialog", "full", Tap<Button>("ShowFull")),
        new("autosuggestbox", "list", Type("Fruit", "a")),
        new("loopingselector", "active", Tap<Control>("Month"), Popups: 0),
        new("combobox", "inline", Tap<ComboBox>("Inline"), Popups: 1),
        new("combobox", "full", Tap<ComboBox>("Full"), Popups: 2),
        new("datetimepicker", "date", Tap<DatePicker>("Date")),
        new("plain hosts", "popup", Tap<ToggleButton>("OpenPopup")),
        new("plain buttons", "menu", Tap<DropDownButton>("DropDown")),
        new("plain menus", "file", (_, shell) => Named<Menu>(shell, "Bar").Items.OfType<MenuItem>().First().Open()),
        new("plain menus", "context", (_, shell) =>
        {
            var held = Named<Border>(shell, "Held");
            held.ContextMenu!.Open(held);
        }),
    ];

    /// <summary>
    /// Clock time a popup's motion is given to end, in ms: three times the longest, the MenuFlyout's 500 ms opening
    /// (#149's full popups are at rest by 250 ms, the ContentDialog by 141 ms, a full popup's close by 469 ms).
    /// </summary>
    private const double PopupMotion = 1500;

    /// <summary>
    /// Forced render ticks drawn before each picture, so Avalonia's own transitions are at rest: the longest in the theme,
    /// the ToggleSwitch knob's 370 ms (<c>KnobTransitions</c>), was at rest after 49 to 60 of them in this
    /// gallery, not the 23 frames of 1/60 s its length gives (measured: 48 left the knob short), so twice that.
    /// A longer transition needs this raised.
    /// </summary>
    private const int TransitionTicks = 120;

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable("CORNFLOWER_PREVIEW") is not { Length: > 0 } folder)
            return;
        if (!Path.IsPathFullyQualified(folder))
            throw new ArgumentException($"CORNFLOWER_PREVIEW must be an absolute folder; it is '{folder}'.");

        var missing = AccentPages.Concat(LightSwitchPages).Concat(PopupShots.Select(shot => shot.Page)).Distinct(StringComparer.OrdinalIgnoreCase).Where(title =>
            !SampleShell.Pages.Any(page => string.Equals(page.Title, title, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"No sample page is titled {string.Join(", ", missing)}: update AccentPages, LightSwitchPages or PopupShots.");

        // A fresh folder, so a picture of a page since renamed or removed can't sit beside the new ones.
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new InvalidOperationException($"CORNFLOWER_PREVIEW must be a new or empty folder; '{folder}' isn't.");

        Directory.CreateDirectory(folder);
        // The phone's culture, en-US, whatever the machine's (without its user overrides): the dates, times and wheels
        // read as the phone's do.
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                var name = variant.ToString().ToLowerInvariant();
                Shoot(folder, $"{name}__catalogue", variant, accent: null, open: null);
                foreach (var page in SampleShell.Pages)
                {
                    Shoot(folder, $"{name}__{FileName(page.Title)}", variant, accent: null, page);
                    foreach (var shot in PopupShots.Where(shot => string.Equals(shot.Page, page.Title, StringComparison.OrdinalIgnoreCase)))
                        Shoot(folder, $"{name}__{FileName(page.Title)}__{shot.Name}", variant, accent: null, page, shot);
                    if (!AccentPages.Contains(page.Title, StringComparer.OrdinalIgnoreCase))
                        continue;
                    foreach (var accent in OtherAccents)
                        Shoot(folder, $"{name}__{FileName(page.Title)}__{accent.ToLowerInvariant()}", variant, accent, page);
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    /// <summary>
    /// The shell, showing <paramref name="open"/> (the catalogue if null) with <paramref name="popup"/> open if one is
    /// given, at rest, saved as <paramref name="file"/>.
    /// </summary>
    private static void Shoot(string folder, string file, ThemeVariant variant, string? accent, SamplePageEntry? open, PopupShot? popup = null)
    {
        using var clock = new TestClock();
        var shell = new SampleShell { HasBackRail = false, Report = _ => { } };
        using var scene = new Scene(shell, variant, scaling: Scaling);
        if (accent is not null)
        {
            if (!scene.Window.TryFindResource($"CornflowerAccent{accent}Color", variant, out var color) || color is not Color named)
                throw new InvalidOperationException($"The theme has no accent named {accent}.");
            scene.Window.Resources["SystemColorControlAccentColor"] = named;
        }

        if (open is not null)
            shell.Open(open);

        ShellTurns.Settle(clock);
        // A page with its own light switch says what the picture shows, rather than "Off" on a light page. Set as a
        // tap sets it (SetCurrentValue), so a binding on it, as the Hub page's, carries the change to its model.
        var switches = shell.GetVisualDescendants().OfType<ToggleSwitch>()
            .Where(toggle => Equals(ControlHeader.GetHeader(toggle), LightSwitchHeader)).ToList();
        if (open is not null && LightSwitchPages.Contains(open.Title, StringComparer.OrdinalIgnoreCase) && switches.Count == 0)
            throw new InvalidOperationException($"The {open.Title} page has no '{LightSwitchHeader}' switch: update LightSwitchPages.");
        if (variant == ThemeVariant.Light)
        {
            foreach (var toggle in switches)
                toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, true);
        }

        SettleTransitions();
        HubsAtRest(file, shell);
        if (popup is not null)
            OpenAtRest(file, scene.Window, shell, clock, popup);

        // At rest, every picture: another TransitionTicks of Avalonia's transitions draw the same pixels. (The clock
        // stands still meanwhile; a ring or bar that never rests runs on it, and a popup's motion was checked ended.)
        using var frame = scene.Capture();
        SettleTransitions();
        using (var later = scene.Capture())
        {
            if (!Frames.SameAs(frame, later))
                throw new InvalidOperationException($"{file}: the page still moves.");
        }

        frame.Save(Path.Combine(folder, file + ".png"), PngBitmapEncoderOptions.Default);
    }

    /// <summary>
    /// Opens <paramref name="popup"/> and runs its motion to its end; the gallery stops if the popups shown then aren't
    /// as many as the shot says, or a timeline still runs on the clock.
    /// </summary>
    private static void OpenAtRest(string file, Window window, Control shell, TestClock clock, PopupShot popup)
    {
        if (OpenPopups(window) != 0)
            throw new InvalidOperationException($"{file}: a popup is shown before the gallery opens one.");
        popup.Act(window, shell);
        // The popup's motion on the TestClock, in steps of 100 ms each drawn (its frames in a step run one after
        // another: only the end is pictured), then Avalonia's own transitions in it.
        var end = clock.Now.TotalMilliseconds + PopupMotion;
        while (clock.Now.TotalMilliseconds < end)
        {
            clock.At(clock.Now.TotalMilliseconds + 100);
            ShellTurns.Draw();
        }

        SettleTransitions();
        if (OpenPopups(window) is var shown && shown != popup.Popups)
            throw new InvalidOperationException($"{file}: {shown} popups are shown, not {popup.Popups}.");
        if (clock.Pending != 0)
            throw new InvalidOperationException($"{file}: {clock.Pending} timelines still run on the clock: the popup isn't at rest.");
    }

    /// <summary>
    /// A Hub turns in when it is placed (1,000 ms, #166) and fades its background in (90 ms, #167), on the clock: the
    /// gallery stops rather than picture one still turning or fading in.
    /// </summary>
    private static void HubsAtRest(string file, Control shell)
    {
        foreach (var hub in shell.GetVisualDescendants().OfType<Hub>())
        {
            if (hub.Entrance.IsRunning)
                throw new InvalidOperationException($"{file}: the Hub is still turning in (its last frame hasn't run).");
            if (hub.Classes.Contains(":backgroundimagefadingin"))
                throw new InvalidOperationException($"{file}: the Hub's background is still fading in.");
        }
    }

    /// <summary>
    /// Avalonia's own transitions (the ToggleSwitch knob's 370 ms, the ListView check box's) run on the render loop's
    /// clock, not the TestClock's, and move only on a forced render tick, whatever the wall clock says
    /// (<c>ManualRenderTimer</c>): draw <see cref="TransitionTicks"/> of them, on every picture, so each is at rest.
    /// </summary>
    private static void SettleTransitions()
    {
        for (var tick = 0; tick < TransitionTicks; tick++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>The popups shown in the window, in its overlay layer, where every Wp81 popup is drawn.</summary>
    private static int OpenPopups(Window window) =>
        window.GetVisualDescendants().OfType<OverlayPopupHost>().Count(host => host.IsEffectivelyVisible);

    /// <summary>The page's one control named <paramref name="name"/>, shown and enabled; the gallery stops if there isn't one.</summary>
    private static T Named<T>(Control shell, string name) where T : Control
    {
        var found = shell.GetVisualDescendants().OfType<T>().Where(control => control.Name == name).ToList();
        if (found is not [{ IsEffectivelyVisible: true, IsEffectivelyEnabled: true } one])
            throw new InvalidOperationException($"The gallery found {found.Count} {typeof(T).Name} named {name}, not one shown and enabled: update PopupShots.");
        return one;
    }

    /// <summary>
    /// A touch tap on the page's <typeparamref name="T"/> named <paramref name="name"/>, at its middle: a touch, as on
    /// the phone, so no pointer is left over the popup it opens to show a pointer-over look.
    /// </summary>
    private static Action<Window, Control> Tap<T>(string name) where T : Control => (window, shell) =>
    {
        var control = Named<T>(shell, name);
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException($"The {typeof(T).Name} {name} isn't in the window.");
        // The touch must land on the control itself, not on something over it or a control scrolled there in its place,
        // so the popup that opens is the one it opens.
        if (window.InputHitTest(at) is not Visual hit || (hit != control && !control.IsVisualAncestorOf(hit)))
            throw new InvalidOperationException($"A touch at the middle of the {typeof(T).Name} {name} doesn't reach it.");
        var touch = window.TouchBegin(at, RawInputModifiers.None);
        window.TouchEnd(touch, at, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    };

    /// <summary>
    /// <paramref name="text"/> typed in the page's AutoCompleteBox named <paramref name="name"/>, its suggestions given at
    /// once (its populate delay is a real-time timer) and its caret not drawn (it blinks in real time).
    /// </summary>
    private static Action<Window, Control> Type(string name, string text) => (window, shell) =>
    {
        var box = Named<AutoCompleteBox>(shell, name);
        box.MinimumPopulateDelay = TimeSpan.Zero;
        var input = box.GetVisualDescendants().OfType<TextBox>().SingleOrDefault(t => t.Name == "PART_TextBox")
            ?? throw new InvalidOperationException($"The box {name} has no PART_TextBox.");
        input.CaretBrush = Brushes.Transparent;
        if (!input.Focus())
            throw new InvalidOperationException($"The box {name} takes no focus.");
        window.KeyTextInput(text);
        Dispatcher.UIThread.RunJobs();
        if (!box.IsDropDownOpen)
            throw new InvalidOperationException($"The box {name} shows no suggestions for '{text}'.");
    };

    /// <summary>
    /// A page drawn again after <see cref="Act"/> in it, with <see cref="Popups"/> popups shown in the overlay layer.
    /// </summary>
    private sealed record PopupShot(string Page, string Name, Action<Window, Control> Act, int Popups = 1);

    /// <summary>A page's title as a file name: lower case, a hyphen for anything but a letter or digit.</summary>
    private static string FileName(string title) =>
        string.Concat(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
}
