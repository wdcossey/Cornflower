using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain hosts (ADR 0007, #234): <c>Window</c>, <c>EmbeddableControlRoot</c>, <c>PopupRoot</c> and
/// <c>OverlayPopupHost</c>, each themed by type in <c>Wp81Theme</c>, dark and light. Nothing here is the phone's
/// measurement: the values are the plan's (docs/spikes/plain-themes-plan.md, "Hosts and small pieces"). Under Simple
/// alone each host takes Simple's brushes and sizes, so every test here fails without its theme.
/// </summary>
public class PlainHostTests
{
    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    /// <summary>
    /// A headless platform window, for a host the headless platform won't make itself: it has no embeddable top level, and
    /// opens every popup in the overlay layer (<c>OverlayPopups</c>, true by default), so a <c>PopupRoot</c> is made here
    /// round one, as a platform with popup windows makes it.
    /// </summary>
    private static T HeadlessImpl<T>() where T : class =>
        Assert.IsAssignableFrom<T>(Activator.CreateInstance(
            typeof(AvaloniaHeadlessPlatformOptions).Assembly.GetType("Avalonia.Headless.HeadlessWindowImpl", throwOnError: true)!,
            new AvaloniaHeadlessPlatformOptions()));

    private static T Resource<T>(StyledElement element, string key) =>
        Assert.IsAssignableFrom<T>(element.FindResource(element.ActualThemeVariant, key));

    /// <summary>The template part of <paramref name="host"/> named <paramref name="name"/>.</summary>
    private static T Part<T>(TemplatedControl host, string name) where T : Control =>
        Assert.IsType<T>(host.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name && c.TemplatedParent == host));

    /// <summary>A window's or a view's root: the page's background, the phone's foreground, the body text's family and size.</summary>
    private static void AssertPage(TemplatedControl host)
    {
        Assert.Same(Resource<IBrush>(host, "ApplicationPageBackgroundThemeBrush"), host.Background);
        Assert.Same(Resource<IBrush>(host, "PhoneForegroundBrush"), host.Foreground);
        Assert.Equal(Resource<FontFamily>(host, "PhoneFontFamilyNormal"), host.FontFamily);
        Assert.Equal(Resource<double>(host, "TextStyleLargeFontSize"), host.FontSize);
        Assert.Equal(18.14, host.FontSize);
    }

    /// <summary>A popup's host: transparent, the phone's foreground and family, the control content's size, the rest reset.</summary>
    private static void AssertPopupHost(TemplatedControl host, TextBlock text)
    {
        Assert.Null(host.Background);
        Assert.Same(Resource<IBrush>(host, "PhoneForegroundBrush"), host.Foreground);
        Assert.Equal(Resource<FontFamily>(host, "PhoneFontFamilyNormal"), host.FontFamily);
        Assert.Equal(Resource<double>(host, "ControlContentThemeFontSize"), host.FontSize);
        Assert.Equal(FontWeight.Normal, host.FontWeight);
        Assert.Equal(FontStyle.Normal, host.FontStyle);
        Assert.Same(host.Foreground, text.Foreground);                 // what it holds inherits them
        Assert.Equal(host.FontSize, text.FontSize);
        var presenter = Part<ContentPresenter>(host, "PART_ContentPresenter");
        Assert.IsType<VisualLayerManager>(presenter.GetVisualParent());
        Assert.Equal("PART_VisualLayerManager", ((Control)presenter.GetVisualParent()!).Name);
    }

    // The window is the page: its background (black dark, white light) drawn behind the content, the phone's foreground
    // and the body text's family and size, which content with none of its own inherits. The layer manager holds the
    // presenter, so the overlay and adorner layers are there.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Window_Is_The_Page_In_The_Phones_Font(string variant)
    {
        var text = new TextBlock { Text = "content" };
        using var scene = new Scene(new Panel { Children = { text } }, Variant(variant));
        var window = scene.Window;

        AssertPage(window);
        Assert.Same(window.Background, window.TransparencyBackgroundFallback);
        Assert.Equal(window.FontSize, text.FontSize);
        Assert.Equal(window.FontFamily, text.FontFamily);
        var presenter = Part<ContentPresenter>(window, "PART_ContentPresenter");
        Assert.Equal("PART_VisualLayerManager", ((Control)presenter.GetVisualParent()!).Name);
        Assert.Same(window.Background, Part<Border>(window, "PART_Background").Background);
        Assert.NotNull(OverlayLayer.GetOverlayLayer(text));

        using var frame = scene.Capture();
        var page = Assert.IsAssignableFrom<ISolidColorBrush>(window.Background).Color;
        Assert.Equal(variant == "dark" ? Colors.Black : Colors.White, page);
        Assert.Equal(page, Frames.PixelAt(frame, frame.PixelSize.Width - 2, frame.PixelSize.Height - 2));
    }

    // Avalonia.Android's view is an EmbeddableControlRoot (AvaloniaView, 12.1.4), so on Android it is every page's host:
    // the same page and font as the window, its background drawn round the layer manager.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void An_EmbeddableControlRoot_Is_The_Page_In_The_Phones_Font(string variant)
    {
        var text = new TextBlock { Text = "content" };
        var root = new EmbeddableControlRoot(HeadlessImpl<ITopLevelImpl>()) { RequestedThemeVariant = Variant(variant) };
        root.Styles.AddRange([new SimpleTheme(), new Wp81Theme()]);
        root.Content = text;
        try
        {
            root.Prepare();
            Dispatcher.UIThread.RunJobs();

            AssertPage(root);
            Assert.Equal(root.FontSize, text.FontSize);
            var background = Part<Border>(root, "PART_Background");
            Assert.Same(root.Background, background.Background);
            var layers = Part<VisualLayerManager>(root, "PART_VisualLayerManager");
            Assert.Same(background, layers.GetVisualParent());
            Assert.Same(text, Part<ContentPresenter>(root, "PART_ContentPresenter").Child);
        }
        finally
        {
            root.Dispose();
        }
    }

    // A popup in a window of its own: its host is transparent, with the phone's foreground and family and the control
    // content's size, which the popup's text takes, not the page's.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_PopupRoot_Is_Transparent_In_The_Phones_Font(string variant)
    {
        var text = new TextBlock { Text = "in a popup" };
        using var scene = new Scene(variant: Variant(variant));
        var host = new PopupRoot(scene.Window, HeadlessImpl<IPopupImpl>()) { Content = text, RequestedThemeVariant = Variant(variant) };
        host.Styles.AddRange([new SimpleTheme(), new Wp81Theme()]);     // no Popup made it, so it has no styling parent
        try
        {
            host.Show();
            Dispatcher.UIThread.RunJobs();

            AssertPopupHost(host, text);
            Assert.Equal(WindowTransparencyLevel.Transparent, Assert.Single(host.TransparencyLevelHint));
        }
        finally
        {
            host.Dispose();
        }
    }

    // A popup in the window's overlay layer, as on Android and as Wp81's own popups: the same.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void An_OverlayPopupHost_Is_Transparent_In_The_Phones_Font(string variant)
    {
        var text = new TextBlock { Text = "in a popup" };
        var popup = new Popup { ShouldUseOverlayLayer = true, Child = text };
        using var scene = new Scene(new Panel { Children = { popup } }, Variant(variant));

        popup.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        var host = text.GetVisualAncestors().OfType<OverlayPopupHost>().Single();
        AssertPopupHost(host, text);
        popup.IsOpen = false;
    }
}
