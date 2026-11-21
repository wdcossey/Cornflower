using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;

namespace Cornflower.Tests.Controls;

/// <summary>
/// SemanticZoom (#215): Cornflower's <see cref="SemanticZoom"/>, Wp81's theme (<c>Controls/SemanticZoom.axaml</c>), the
/// jump list's popup (<see cref="PhoneSemanticZoom"/> on <see cref="PhonePopupHost"/>) and the jump list's converters,
/// against #211's probe and recording (tools/refapp/README.md, "Tier 4's reads", SemanticZoom: runs
/// <c>20261118-2231-dev11-partial</c>, <c>-2237-</c> and <c>-2246-dev11-partial</c>; ref/wp81/SemanticZoom/).
/// docs/controls/SemanticZoom/PLAN.md has each reading.
/// </summary>
public class SemanticZoomTests
{
    // ----- The readings -----

    /// <summary>One tick of the phone's clock, and the frames the test clock gives.</summary>
    private const double Tick = 15.625;

    /// <summary>The probe's SemanticZoom, below its 60 px header band.</summary>
    private const double ZoomTop = 60;

    /// <summary>The tiles show 50 ms after the tap's release, and open to their full height over the next 170 ms.</summary>
    private const double OpenDelay = 50;
    private const double OpenLength = 170;

    /// <summary>Every row turns from the Flyout's 80° over the 170 ms: 7.35° a tick.</summary>
    private const double OpenFrom = 80;
    private const double OpenRate = OpenFrom / (OpenLength / Tick);

    /// <summary>A tile's tap holds the jump list about 100 ms (the tile's tilt), then it goes, top row first, in about 80 ms.</summary>
    private const double TileHold = 100;

    /// <summary>The closing rows: 30° a tick, each 0.3 ticks after the one above; the reference's eight gone in 5 ticks.</summary>
    private const double CloseRate = 30;
    private const double CloseStagger = 0.3;
    private const double ClosedAfter = 5 * Tick;

    /// <summary>The docs' tile, the probe's: 99 × 99 with a margin of 6, a container 111.25 laid out at 2.4.</summary>
    private const double TileSize = 99;
    private const double TileMargin = 6;
    private const double RowHeight = 111.25;

    /// <summary>The jump list's brushes, dark (the file's JumpListDefault*): the accent for a group with items, #FF1F1F1F for an empty one, the letter white.</summary>
    private static readonly Color Accent = Color.Parse("#FF3E65FF");
    private static readonly Color EmptyDark = Color.Parse("#FF1F1F1F");
    private static readonly Color LetterDark = Colors.White;

    /// <summary>Light: the accent, white letters; an empty tile #FFDEDEDE, its letter #FFA5A5A5 (the file's).</summary>
    private static readonly Color EmptyLight = Color.Parse("#FFDEDEDE");
    private static readonly Color EmptyLetterLight = Color.Parse("#FFA5A5A5");

    /// <summary>Layout to a raw pixel at 2.4.</summary>
    private const double Tolerance = 0.5;

    /// <summary>A group of the probe's: a letter and five items, or none for b and e.</summary>
    internal sealed class Letter(string key, IEnumerable<string> items) : ObservableCollection<string>(items)
    {
        public string Key { get; } = key;

        public override string ToString() => Key;
    }

    private static List<Letter> Letters() =>
        "abcdefgh".Select(ch => new Letter(ch.ToString(),
            ch is 'b' or 'e' ? [] : Enumerable.Range(1, 5).Select(i => $"{ch} item {i}"))).ToList();

    /// <summary>The docs' tile: a 99 Border, margin 6, in the converters' brushes, the letter at 48 padded 6 at the bottom left.</summary>
    private static IDataTemplate Tile(double size = TileSize) =>
        new FuncDataTemplate<Letter>((_, _) =>
        {
            var text = new TextBlock
            {
                FontSize = 48,
                Padding = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                [!TextBlock.TextProperty] = new Avalonia.Data.Binding(nameof(Letter.Key)),
                [!TextBlock.ForegroundProperty] = new Avalonia.Data.Binding { Converter = new JumpListItemForegroundConverter() },
            };
            return new Border
            {
                Width = size,
                Height = size,
                Margin = new Thickness(TileMargin),
                [!Border.BackgroundProperty] = new Avalonia.Data.Binding { Converter = new JumpListItemBackgroundConverter() },
                Child = text,
            };
        });

    private static IDataTemplate HeaderTemplate() => new FuncDataTemplate<Letter>((letter, _) =>
    {
        var text = new TextBlock { Text = letter.Key };
        text.Bind(StyledElement.ThemeProperty, text.GetResourceObservable("GroupHeaderTextBlockStyle"));
        return text;
    });

    /// <summary>The probe's page: the SemanticZoom below a 60 px band, a grouped ListView in, a GridView of the groups out.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Scene _scene;

        public Stage(ThemeVariant? page = null, bool canChange = true, IDataTemplate? tile = null, bool zoomedOut = false)
        {
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Groups = Letters();
            List = new ListBox { AutoScrollToSelectedItem = false };
            ListBoxOptions.SetGroupHeaderTemplate(List, HeaderTemplate());
            ListBoxOptions.SetGroupedItemsSource(List, Groups);
            JumpList = new ListBox
            {
                ItemsSource = Groups,
                ItemTemplate = tile ?? Tile(),
                Margin = new Thickness(19, 0, 19, 0),
                AutoScrollToSelectedItem = false,
            };
            JumpList.Bind(StyledElement.ThemeProperty, JumpList.GetResourceObservable("Wp81GridViewTheme"));
            Zoom = new SemanticZoom
            {
                Margin = new Thickness(0, ZoomTop, 0, 0),
                ZoomedInView = List,
                ZoomedOutView = JumpList,
                CanChangeViews = canChange,
                IsZoomedInViewActive = !zoomedOut,
            };
            Zoom.ViewChangeStarted += (_, e) => Events.Add(("started", e, Zoom.IsZoomedInViewActive, Clock.Now.TotalMilliseconds));
            Zoom.ViewChangeCompleted += (_, e) => Events.Add(("completed", e, Zoom.IsZoomedInViewActive, Clock.Now.TotalMilliseconds));
            var background = new Border { Child = Zoom };
            background.Bind(Border.BackgroundProperty, background.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
            Page = new ThemeVariantScope { Child = background };
            if (page is not null)
                Page.RequestedThemeVariant = page;
            try
            {
                _scene = new Scene(Page);
            }
            catch
            {
                Clock.Dispose();
                throw;
            }

            Settle();
        }

        public TestClock Clock { get; }

        public List<Letter> Groups { get; }

        public ListBox List { get; }

        public ListBox JumpList { get; }

        public SemanticZoom Zoom { get; }

        public ThemeVariantScope Page { get; }

        public Window Window => _scene.Window;

        public List<(string Name, SemanticZoomViewChangedEventArgs Args, bool Active, double At)> Events { get; } = [];

        public Popup Popup => Part<Popup>("PART_Popup");

        public PhonePopupBands? Bands => PhoneSemanticZoom.HostOf(Zoom)?.Bands;

        public ScrollViewer Scroll => (ScrollViewer)List.Scroll!;

        public double Now => Clock.Now.TotalMilliseconds;

        public T Part<T>(string name) where T : Control =>
            Zoom.GetVisualDescendants().OfType<T>().SingleOrDefault(c => c.Name == name && c.TemplatedParent == Zoom)
            ?? Popup.Child!.GetSelfAndVisualDescendants().OfType<T>().Single(c => c.Name == name);

        public static double FirstFrameAfter(double call) => (Math.Floor(call / Tick) + 1) * Tick;

        public ListViewHeaderItem Header(string key) =>
            List.GetRealizedContainers().OfType<ListViewHeaderItem>().Single(h => ((Letter)h.Content!).Key == key);

        public Control TileOf(string key) => JumpList.ContainerFromItem(Groups.Single(g => g.Key == key))!;

        public Rect InWindow(Visual visual) => new(visual.TranslatePoint(default, Window)!.Value, visual.Bounds.Size);

        public void Tap(Point at)
        {
            Window.MouseDown(at, MouseButton.Left);
            Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        public void TapHeader(string key) => Tap(InWindow(Header(key)).Center);

        public void TapTile(string key) => Tap(InWindow(TileOf(key)).Center);

        /// <summary>A header tap, and the jump list open and at rest.</summary>
        public void OpenAtRest(string key = "a")
        {
            TapHeader(key);
            At(Now + 1000);
        }

        public RoutedEventArgs Back()
        {
            var request = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            Window.RaiseEvent(request);
            Dispatcher.UIThread.RunJobs();
            return request;
        }

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Settle();
        }

        public void Settle()
        {
            for (var i = 0; i < 3; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
        }

        public Avalonia.Media.Imaging.WriteableBitmap Capture() => _scene.Capture();

        public void Dispose()
        {
            _scene.Dispose();
            Clock.Dispose();
        }
    }

    /// <summary>Wp81's theme in the app's own styles, in a theme of the app's, for the converters' defaults; taken out after.</summary>
    internal sealed class AppTheme : IDisposable
    {
        private readonly Wp81Theme _theme = new();
        private readonly ThemeVariant? _was;

        public AppTheme(ThemeVariant variant)
        {
            var app = Application.Current!;
            _was = app.RequestedThemeVariant;
            app.RequestedThemeVariant = variant;
            app.Styles.Add(_theme);
        }

        public void Dispose()
        {
            var app = Application.Current!;
            app.Styles.Remove(_theme);
            app.RequestedThemeVariant = _was;
        }
    }

    private static Color Pixel(Avalonia.Media.Imaging.WriteableBitmap frame, Point at) =>
        Frames.PixelAt(frame, (int)(at.X * Scene.ReferenceScaling), (int)(at.Y * Scene.ReferenceScaling));

    private static Color ColorOf(object? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // ----- The template -----

    // The file's setters, and its parts: the zoomed-in presenter, the popup, PopupBorder (no background: #211, the list
    // between the tiles) and the zoomed-out presenter, the views where the content alignments put them.
    [AvaloniaFact]
    public void The_Template_Is_The_Phones()
    {
        using var stage = new Stage();
        var zoom = stage.Zoom;

        Assert.Equal(new Thickness(0), zoom.Padding);
        Assert.Equal(Colors.Transparent, ColorOf(zoom.Background));
        Assert.Equal(new Thickness(0), zoom.BorderThickness);
        Assert.Equal(HorizontalAlignment.Stretch, zoom.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Stretch, zoom.VerticalContentAlignment);
        Assert.False(KeyboardNavigation.GetIsTabStop(zoom));
        Assert.Equal(KeyboardNavigationMode.Once, KeyboardNavigation.GetTabNavigation(zoom));

        var zoomedIn = stage.Part<ContentPresenter>("PART_ZoomedInPresenter");
        Assert.Same(stage.List, zoomedIn.Child);
        Assert.False(stage.Popup.IsLightDismissEnabled);
        Assert.Null(stage.Part<Border>("PART_PopupBorder").Background);
        Assert.Same(stage.JumpList, stage.Part<ContentPresenter>("PART_ZoomedOutPresenter").Content);

        zoom.HorizontalContentAlignment = HorizontalAlignment.Left;
        zoom.VerticalContentAlignment = VerticalAlignment.Bottom;
        Assert.Equal(HorizontalAlignment.Left, zoomedIn.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Bottom, zoomedIn.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Left, stage.Part<ContentPresenter>("PART_ZoomedOutPresenter").HorizontalAlignment);
    }

    // ----- Zooming out -----

    // #211: ViewChangeStarted (source the list's first item, destination group a) 1 to 2 ms after the release,
    // ViewChangeCompleted 1 ms later; on screen the tiles show about 50 ms after it. Here header c, the destination its
    // group, the source still the list's first item.
    [AvaloniaFact]
    public void A_Tap_On_A_Header_Raises_Both_Events_At_Once_And_Shows_The_Jump_List_50_ms_Later()
    {
        using var stage = new Stage();
        stage.TapHeader("c");

        Assert.Collection(stage.Events,
            e =>
            {
                Assert.Equal("started", e.Name);
                Assert.True(e.Args.IsSourceZoomedInView);
                Assert.Equal("a item 1", e.Args.SourceItem.Item);
                Assert.Same(stage.Groups[2], e.Args.DestinationItem.Item);
                Assert.True(e.Active);
                Assert.Equal(0, e.At);
            },
            e =>
            {
                Assert.Equal("completed", e.Name);
                Assert.Same(stage.Groups[2], e.Args.DestinationItem.Item);
                Assert.False(e.Active);
                Assert.Equal(0, e.At);
            });
        Assert.False(stage.Zoom.IsZoomedInViewActive);

        stage.At(OpenDelay - 1);
        Assert.False(stage.Popup.IsOpen);
        stage.At(OpenDelay);
        Assert.True(stage.Popup.IsOpen);
    }

    // A pinned header is a header too: panned to group c, a tap on the header pinned at the top opens the jump list for c.
    [AvaloniaFact]
    public void A_Tap_On_The_Pinned_Header_Zooms_Out_To_Its_Group()
    {
        using var stage = new Stage();
        stage.Scroll.Offset = new Vector(0, stage.InWindow(stage.Header("c")).Y - ZoomTop + 40);
        stage.Settle();
        var sticky = stage.List.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_StickyHeader");
        Assert.True(sticky.IsVisible);

        stage.Tap(stage.InWindow(sticky).Center);

        Assert.Same(stage.Groups[2], stage.Events[0].Args.DestinationItem.Item);
        Assert.False(stage.Zoom.IsZoomedInViewActive);
    }

    // #211: the popup covers the window, 450 x 800 from its top left, wherever the SemanticZoom is (here 60 down); no
    // dim and no background, so the list shows between the tiles as it was: here a light page's white, under a dark app.
    [AvaloniaFact]
    public void The_Jump_List_Covers_The_Whole_Window_With_No_Dim_And_The_List_Between_Its_Tiles()
    {
        using var app = new AppTheme(ThemeVariant.Dark);
        using var stage = new Stage(page: ThemeVariant.Light);
        var gap = new Point(19 + RowHeight - TileMargin / 2, 40);
        using (var before = stage.Capture())
        {
            var under = Pixel(before, gap);
            Assert.Equal(Colors.White, under);
            stage.OpenAtRest();

            var content = stage.Popup.Child!;
            Assert.Equal(new Rect(0, 0, Scene.ReferenceWidth, Scene.ReferenceHeight), stage.InWindow(content));
            Assert.Null(PhoneSemanticZoom.HostOf(stage.Zoom)!.Dim);
            Assert.DoesNotContain(OverlayLayer.GetOverlayLayer(stage.Window)!.Children, v => v is Rectangle);
            using var after = stage.Capture();
            Assert.Equal(under, Pixel(after, gap));
            Assert.Equal(Accent, Pixel(after, new Point(19 + TileMargin + 10, TileMargin + 10)));
        }
    }

    // #211: with the page light and the app dark, the jump list stayed dark: the popup takes the app's theme.
    [AvaloniaFact]
    public void The_Jump_List_Takes_The_Windows_Theme_Not_The_Pages()
    {
        using var stage = new Stage(page: ThemeVariant.Light);
        Assert.Equal(ThemeVariant.Light, stage.Zoom.ActualThemeVariant);
        stage.OpenAtRest();

        Assert.Equal(ThemeVariant.Dark, stage.Part<Border>("PART_PopupBorder").ActualThemeVariant);
        Assert.Equal(ThemeVariant.Dark, stage.JumpList.ActualThemeVariant);
    }

    // #211: the tiles open in rows, a tile's height each (111.25), the first row's band across it at its centre line;
    // every row from 80° to rest over 170 ms.
    [AvaloniaFact]
    public void The_Jump_List_Opens_In_Rows_Of_A_Tile_Over_170_ms()
    {
        using (var stage = new Stage())
        {
            stage.TapHeader("a");
            stage.At(OpenDelay);

            var bands = Assert.IsType<PhonePopupBands>(stage.Bands);
            Assert.Equal(8, bands.Bands.Count);
            Assert.Equal(RowHeight, bands.Bands[0].Height, Tolerance);
            Assert.All(bands.Angles, angle => Assert.Equal(OpenFrom, angle, 1e-9));

            var first = Stage.FirstFrameAfter(OpenDelay);
            stage.At(first + 6 * Tick);
            Assert.All(stage.Bands!.Angles, angle => Assert.Equal(OpenFrom - OpenRate * 6, angle, 1e-6));
            stage.At(first + OpenLength - 1);
            Assert.NotNull(stage.Bands);
            stage.At(Stage.FirstFrameAfter(first + OpenLength));
            Assert.Null(stage.Bands);
        }

        // The rows are the app's tile's: 60 with a margin of 6, twelve rows of 72 in the window.
        using var small = new Stage(tile: Tile(60));
        small.TapHeader("a");
        small.At(OpenDelay);
        Assert.Equal(72, small.Bands!.Bands[0].Height, Tolerance);
        Assert.Equal(12, small.Bands.Bands.Count);
        Assert.All(small.Bands.Angles, angle => Assert.Equal(OpenFrom, angle, 1e-9));
    }

    // The motion is the popups': CornflowerConfig's popup speed stretches the wait and the turn; with motion off it opens at once.
    [AvaloniaFact]
    public void The_Jump_Lists_Motion_Follows_The_Popup_Speed()
    {
        using var config = new ConfigScope();
        CornflowerConfig.PopupSpeed = 0.5;
        using (var stage = new Stage())
        {
            stage.TapHeader("a");
            stage.At(OpenDelay * 2 - 1);
            Assert.False(stage.Popup.IsOpen);
            stage.At(OpenDelay * 2);
            Assert.True(stage.Popup.IsOpen);
        }

        CornflowerConfig.Animated = false;
        using (var stage = new Stage())
        {
            stage.TapHeader("a");
            Assert.True(stage.Popup.IsOpen);
            Assert.Null(stage.Bands);
        }
    }

    // ----- The tiles -----

    // #211: the converters' brushes, the file's JumpListDefault* in the app's theme. Dark: the accent for a group with
    // items, #FF1F1F1F for an empty one, the letter white on both. Light: #FFDEDEDE and #FFA5A5A5 for an empty one.
    [AvaloniaFact]
    public void The_Converters_Give_The_Apps_Jump_List_Brushes()
    {
        var background = new JumpListItemBackgroundConverter();
        var foreground = new JumpListItemForegroundConverter();
        var full = new Letter("a", ["a item 1"]);
        var empty = new Letter("b", []);
        object? Back(object? group) => background.Convert(group, typeof(IBrush), null, System.Globalization.CultureInfo.InvariantCulture);
        object? Fore(object? group) => foreground.Convert(group, typeof(IBrush), null, System.Globalization.CultureInfo.InvariantCulture);

        using (new AppTheme(ThemeVariant.Dark))
        {
            Assert.Equal(Accent, ColorOf(Back(full)));
            Assert.Equal(EmptyDark, ColorOf(Back(empty)));
            Assert.Equal(LetterDark, ColorOf(Fore(full)));
            Assert.Equal(LetterDark, ColorOf(Fore(empty)));
            Assert.Equal(EmptyDark, ColorOf(Back(null)));
        }

        using (new AppTheme(ThemeVariant.Light))
        {
            Assert.Equal(Accent, ColorOf(Back(full)));
            Assert.Equal(EmptyLight, ColorOf(Back(empty)));
            Assert.Equal(Colors.White, ColorOf(Fore(full)));
            Assert.Equal(EmptyLetterLight, ColorOf(Fore(empty)));
        }
    }

    // WinRT's Enabled and Disabled: an app's own brushes in place of the theme's.
    [AvaloniaFact]
    public void The_Converters_Take_An_Apps_Own_Brushes()
    {
        IBrush on = new SolidColorBrush(Colors.Red), off = new SolidColorBrush(Colors.Green);
        var background = new JumpListItemBackgroundConverter { Enabled = on, Disabled = off };
        var foreground = new JumpListItemForegroundConverter { Enabled = off, Disabled = on };
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        Assert.Same(on, background.Convert(new Letter("a", ["x"]), typeof(IBrush), null, culture));
        Assert.Same(off, background.Convert(new Letter("b", []), typeof(IBrush), null, culture));
        Assert.Same(off, foreground.Convert(new Letter("a", ["x"]), typeof(IBrush), null, culture));
        Assert.Same(on, foreground.Convert(new Letter("b", []), typeof(IBrush), null, culture));
    }

    // ----- Zooming in -----

    // #211: a tap on tile d: both events (group d to group d) at the release, and in the same frame the list's offset
    // jumps, header d at the SemanticZoom's top (window y 60). The tile isn't selected.
    [AvaloniaFact]
    public void A_Tap_On_A_Tile_Lands_The_List_With_Its_Groups_Header_At_The_Top()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        stage.Events.Clear();
        var at = stage.Now;
        stage.TapTile("d");

        var d = stage.Groups[3];
        Assert.Collection(stage.Events,
            e =>
            {
                Assert.Equal("started", e.Name);
                Assert.False(e.Args.IsSourceZoomedInView);
                Assert.Same(d, e.Args.SourceItem.Item);
                Assert.Same(d, e.Args.DestinationItem.Item);
                Assert.Equal(at, e.At);
            },
            e => Assert.Equal("completed", e.Name));
        Assert.True(stage.Zoom.IsZoomedInViewActive);
        Assert.Equal(ZoomTop, stage.InWindow(stage.Header("d")).Y, Tolerance);
        Assert.Null(stage.JumpList.SelectedItem);
    }

    // The jump list shows the empty groups' tiles, grey (the converters); a tap on one does nothing.
    [AvaloniaFact]
    public void A_Tap_On_An_Empty_Groups_Tile_Does_Nothing()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        stage.Events.Clear();
        var offset = stage.Scroll.Offset;
        stage.TapTile("b");
        stage.At(stage.Now + 1000);

        Assert.Empty(stage.Events);
        Assert.False(stage.Zoom.IsZoomedInViewActive);
        Assert.True(stage.Popup.IsOpen);
        Assert.Equal(offset, stage.Scroll.Offset);
    }

    // #211: the tiles go, the top row first, over about 80 ms (3 frames), from about 100 ms after the release.
    [AvaloniaFact]
    public void A_Tiles_Tap_Closes_The_Jump_List_Top_Row_First_After_The_Tilt()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var tap = stage.Now;
        stage.TapTile("d");

        stage.At(tap + TileHold - 1);
        Assert.Null(stage.Bands);
        Assert.True(stage.Popup.IsOpen);

        stage.At(tap + TileHold);
        var bands = Assert.IsType<PhonePopupBands>(stage.Bands);
        Assert.Equal(8, bands.Bands.Count);
        var first = Stage.FirstFrameAfter(tap + TileHold);
        stage.At(first + 2 * Tick);
        Assert.Equal(-CloseRate * 2, stage.Bands!.Angles[0], 1e-6);
        Assert.Equal(0, stage.Bands.Angles[7], 1e-6);
        stage.At(first + 3 * Tick);
        Assert.Equal(-90, stage.Bands!.Angles[0], 1e-6);
        Assert.Equal(-CloseRate * (3 - CloseStagger * 7), stage.Bands.Angles[7], 1e-6);

        stage.At(first + ClosedAfter - 1);
        Assert.True(stage.Popup.IsOpen);
        stage.At(Stage.FirstFrameAfter(first + ClosedAfter - 1));
        Assert.False(stage.Popup.IsOpen);
    }

    // #211: Back closes it the same way, the tiles gone in about 80 ms; ViewChangeStarted with no source and no
    // destination, the list where it was, and the app's Back handler isn't called.
    [AvaloniaFact]
    public void Back_Closes_The_Jump_List_And_Leaves_The_List_Where_It_Was()
    {
        using var stage = new Stage();
        stage.OpenAtRest("d");
        stage.Events.Clear();
        var offset = stage.Scroll.Offset;
        var back = stage.Now;

        var request = stage.Back();

        Assert.True(request.Handled);
        var started = Assert.Single(stage.Events, e => e.Name == "started");
        Assert.False(started.Args.IsSourceZoomedInView);
        Assert.Null(started.Args.SourceItem.Item);
        Assert.Null(started.Args.DestinationItem.Item);
        Assert.True(stage.Zoom.IsZoomedInViewActive);
        Assert.Equal(offset, stage.Scroll.Offset);
        Assert.NotNull(stage.Bands);

        var first = Stage.FirstFrameAfter(back);
        stage.At(first + ClosedAfter - 1);
        Assert.True(stage.Popup.IsOpen);
        stage.At(Stage.FirstFrameAfter(first + ClosedAfter - 1));
        Assert.False(stage.Popup.IsOpen);
        Assert.False(stage.Back().Handled);
    }

    // ----- In code, and CanChangeViews -----

    // IsZoomedInViewActive and ToggleActiveView change the view as a tap does: zooming out from the top item to its
    // group, zooming in with neither.
    [AvaloniaFact]
    public void The_View_Changes_From_Code()
    {
        using var stage = new Stage();
        stage.Zoom.IsZoomedInViewActive = false;

        Assert.Equal(2, stage.Events.Count);
        Assert.Equal("a item 1", stage.Events[0].Args.SourceItem.Item);
        Assert.Same(stage.Groups[0], stage.Events[0].Args.DestinationItem.Item);
        stage.At(stage.Now + 1000);
        Assert.True(stage.Popup.IsOpen);

        stage.Events.Clear();
        stage.Zoom.ToggleActiveView();
        Assert.True(stage.Zoom.IsZoomedInViewActive);
        Assert.Null(stage.Events[0].Args.SourceItem.Item);
        stage.At(stage.Now + 1000);
        Assert.False(stage.Popup.IsOpen);
    }

    // Zoomed out before it is shown, the jump list opens as it is; taken out of the window and put back, it opens again.
    [AvaloniaFact]
    public void A_Zoomed_Out_View_Opens_As_It_Is_Shown_And_Again_As_It_Comes_Back()
    {
        using var stage = new Stage(zoomedOut: true);
        stage.At(stage.Now + 1000);
        Assert.True(stage.Popup.IsOpen);

        var host = (Border)stage.Zoom.Parent!;
        host.Child = null;
        stage.Settle();
        Assert.False(stage.Popup.IsOpen);
        host.Child = stage.Zoom;
        stage.At(stage.Now + 1000);
        Assert.True(stage.Popup.IsOpen);
    }

    // #211: with CanChangeViews false a tap on a header closed the app. Decided (#215): the view doesn't zoom out.
    [AvaloniaFact]
    public void With_CanChangeViews_False_The_View_Does_Not_Zoom_Out()
    {
        using var stage = new Stage(canChange: false);
        stage.TapHeader("a");
        stage.Zoom.ToggleActiveView();
        stage.Zoom.IsZoomedInViewActive = false;
        stage.At(stage.Now + 1000);

        Assert.Empty(stage.Events);
        Assert.True(stage.Zoom.IsZoomedInViewActive);
        Assert.False(stage.Popup.IsOpen);
        // Turned off with the jump list open, it still closes: Back isn't trapped behind it.
        stage.Zoom.CanChangeViews = true;
        stage.Zoom.IsZoomedInViewActive = false;
        stage.At(stage.Now + 1000);
        stage.Zoom.CanChangeViews = false;
        Assert.True(stage.Back().Handled);
        Assert.True(stage.Zoom.IsZoomedInViewActive);
    }

    // WinRT's arguments are writable: an 8.1 app's ViewChangeStarted handler may move the destination, and the list lands there.
    [AvaloniaFact]
    public void A_Started_Handler_Moves_Where_The_List_Lands()
    {
        using var stage = new Stage();
        stage.OpenAtRest();
        var c = stage.Groups[2];
        stage.Zoom.ViewChangeStarted += (_, e) => e.DestinationItem = new SemanticZoomLocation { Item = c };
        stage.TapTile("d");

        Assert.Equal(ZoomTop, stage.InWindow(stage.Header("c")).Y, Tolerance);
        Assert.Same(c, stage.Events.Last(e => e.Name == "completed").Args.DestinationItem.Item);
    }
}
