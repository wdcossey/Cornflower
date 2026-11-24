using System.Text.RegularExpressions;

namespace Cornflower.Tests.Completeness;

/// <summary>
/// The plain themes' label and names (ADR 0007): each plain theme's README opens with the label, its theme file's
/// header comment says it, and every key a plain theme or the plain tokens declare is plain: a type (the theme by type,
/// which fills the hole), a part theme <c>Wp81Plain&lt;Part&gt;Theme</c>, or a <c>CornflowerPlain</c> resource, never a
/// WinRT-shaped name (<c>...ThemeBrush</c>), which in this repo means the phone's value. A batch adds its controls to
/// <see cref="Plain"/>.
/// </summary>
public class PlainThemesTests
{
    /// <summary>The line each plain theme's README opens with, and its theme file's header says.</summary>
    public const string Label =
        "Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.";

    /// <summary>Every control with a plain theme: its theme is <c>Controls/&lt;Name&gt;.axaml</c>, its README <c>docs/controls/&lt;Name&gt;/README.md</c>.</summary>
    public static readonly string[] Plain =
    [
        "EmbeddableControlRoot", "ItemsControl", "OverlayPopupHost", "PopupRoot", "Separator", "Window",    // batch 1, #234
        "ButtonSpinner", "DropDownButton", "GridSplitter", "NumericUpDown", "SplitButton",                  // batch 2, #235
    ];

    /// <summary>
    /// Every plain part theme, the ones later batches reuse (plan section 3): its theme is
    /// <c>Controls/Plain&amp;lt;Part&amp;gt;.axaml</c>, keyed <c>Wp81Plain&amp;lt;Part&amp;gt;Theme</c>. Its README is its first host's.
    /// </summary>
    public static readonly string[] Parts =
    [
        "GlyphButton", "SpinnerButton",     // batch 2, #235
    ];

    public static TheoryData<string> Controls => new(Plain);

    public static TheoryData<string> PartThemes => new(Parts);

    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Cornflower.sln")))
                return dir.FullName;
        throw new InvalidOperationException("Cornflower.sln not found above " + AppContext.BaseDirectory);
    }

    private static string ThemeFile(string control) =>
        Path.Combine(Root, "src", "Cornflower.Themes.Wp81", "Controls", control + ".axaml");

    /// <summary>Every <c>x:Key</c> a XAML file declares.</summary>
    internal static string[] Keys(string xaml) =>
        Regex.Matches(xaml, "x:Key=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();

    /// <summary>A plain token: <c>CornflowerPlain</c>, and nothing WinRT-shaped after it.</summary>
    internal static bool IsToken(string key) =>
        key.StartsWith("CornflowerPlain", StringComparison.Ordinal) && !key.Contains("Theme", StringComparison.Ordinal);

    /// <summary>
    /// A key the plain theme of <paramref name="control"/> may declare: its own type (its theme by type, never another
    /// control's), a <c>Wp81Plain&lt;Part&gt;Theme</c>, or a token.
    /// </summary>
    internal static bool IsPlainKey(string key, string control) =>
        key == "{x:Type " + control + "}"
        || Regex.IsMatch(key, "^Wp81Plain[A-Z][A-Za-z0-9]*Theme$")
        || IsToken(key);

    [Theory]
    [MemberData(nameof(Controls))]
    public void Its_Readme_Opens_With_The_Label(string control)
    {
        var lines = File.ReadAllLines(Path.Combine(Root, "docs", "controls", control, "README.md"));

        Assert.Equal("# " + control, lines[0]);
        Assert.Equal(Label, lines.Skip(1).First(line => line.Length > 0));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void Its_Theme_File_Says_It_And_Declares_Only_Plain_Keys(string control)
    {
        var xaml = File.ReadAllText(ThemeFile(control));

        var header = Regex.Match(xaml, "<!--(.*?)-->", RegexOptions.Singleline);
        Assert.True(header.Success, $"{control}.axaml has no header comment.");
        Assert.Contains(Label, Regex.Replace(header.Groups[1].Value, "\\s+", " "));
        var keys = Keys(xaml);
        Assert.Contains("{x:Type " + control + "}", keys);
        Assert.All(keys, key => Assert.True(IsPlainKey(key, control), $"{key} in {control}.axaml: a plain theme's keys are its own type, Wp81Plain<Part>Theme or CornflowerPlain..."));
    }

    [Theory]
    [MemberData(nameof(PartThemes))]
    public void A_Part_Themes_File_Says_It_And_Declares_Only_Its_Part_Theme(string part)
    {
        var xaml = File.ReadAllText(ThemeFile("Plain" + part));

        var header = Regex.Match(xaml, "<!--(.*?)-->", RegexOptions.Singleline);
        Assert.True(header.Success, $"Plain{part}.axaml has no header comment.");
        Assert.Contains(Label, Regex.Replace(header.Groups[1].Value, "\\s+", " "));
        Assert.Equal(["Wp81Plain" + part + "Theme"], Keys(xaml));
    }

    [Fact]
    public void The_Plain_Tokens_Are_CornflowerPlain()
    {
        var keys = Keys(File.ReadAllText(Path.Combine(Root, "src", "Cornflower.Themes.Wp81", "Tokens", "Plain.axaml")));

        Assert.Contains("CornflowerPlainLineThickness", keys);
        Assert.All(keys, key => Assert.True(IsToken(key), $"{key} in Tokens/Plain.axaml: a plain token is CornflowerPlain..., not WinRT-shaped."));
    }

    // The rule itself, on keys made here.
    [Theory]
    [InlineData("{x:Type Separator}", true)]
    [InlineData("{x:Type Button}", false)]                      // another control's theme by type: Wp81's Button replaced
    [InlineData("Wp81PlainGlyphButtonTheme", true)]
    [InlineData("CornflowerPlainErrorBrush", true)]
    [InlineData("Wp81SeparatorTheme", false)]                   // Wp81's own name: a reproduction's
    [InlineData("Wp81PlainGlyphButton", false)]                 // a part theme ends in Theme
    [InlineData("SeparatorLineThickness", false)]               // no prefix
    [InlineData("CornflowerPlainLineThemeThickness", false)]    // prefixed, but WinRT-shaped
    public void A_Key_Is_Plain_By_Its_Name(string key, bool plain) =>
        Assert.Equal(plain, IsPlainKey(key, "Separator"));
}
