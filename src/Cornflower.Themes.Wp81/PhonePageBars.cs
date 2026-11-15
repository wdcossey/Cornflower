using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The page's command bars over its content, as the phone's shell draws the app bar over the page: turned on by the
/// ContentPage theme's setter on the panel that holds <c>PART_Content</c>, <c>PART_TopCommandBar</c> and
/// <c>PART_BottomCommandBar</c> in one cell. The content gives up each bar's closed height, at its foot (or head), and
/// keeps giving up that height while the bar is open: the phone's <c>VisibleBounds</c> ends 62.92 above the screen's
/// foot with a compact app bar and 26.25 with a minimal one, open or closed, and the open bar is drawn over the page
/// (<c>tools/refapp/README.md</c>, "The page's app bar"). A bar's closed height is its laid-out height while closed: the
/// CommandBar theme's <c>Wp81CommandBarCompactHeight</c> (62.9166) or <c>Wp81CommandBarMinimalHeight</c> (26.25) by its
/// <c>CommandBarOptions.ClosedDisplayMode</c>, or an app's own. A bar open before it was laid out closed in its
/// mode takes the theme's height for the mode. A bar that isn't a <see cref="CommandBar"/> is always closed.
/// </summary>
internal static class PhonePageBars
{
    /// <summary>Turns the reserve on for a panel of the three parts.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Panel, bool>("IsEnabled", typeof(PhonePageBars));

    /// <summary>The CommandBar theme's closed compact height (62.9166), for a bar open before it was laid out closed in its mode.</summary>
    internal const string CompactHeightKey = "Wp81CommandBarCompactHeight";

    /// <summary>The CommandBar theme's closed minimal height (26.25), the same way.</summary>
    internal const string MinimalHeightKey = "Wp81CommandBarMinimalHeight";

    private static readonly AttachedProperty<Reserve?> ReserveProperty =
        AvaloniaProperty.RegisterAttached<Panel, Reserve?>("Reserve", typeof(PhonePageBars));

    static PhonePageBars()
    {
        IsEnabledProperty.Changed.AddClassHandler<Panel>((panel, e) =>
        {
            panel.GetValue(ReserveProperty)?.Detach();
            panel.SetValue(ReserveProperty, e.GetNewValue<bool>() ? new Reserve(panel) : null);
        });
    }

    public static bool GetIsEnabled(Panel panel) => panel.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Panel panel, bool value) => panel.SetValue(IsEnabledProperty, value);

    /// <summary>One panel's reserve: the content's margin, from each bar's closed height.</summary>
    private sealed class Reserve
    {
        private readonly Panel _panel;
        private readonly Bar _top;
        private readonly Bar _bottom;

        public Reserve(Panel panel)
        {
            _panel = panel;
            _top = new Bar(this);
            _bottom = new Bar(this);
            // A template's parts can be added before their names are set: looked for again once the panel is in a tree.
            _panel.Children.CollectionChanged += OnChildrenChanged;
            _panel.AttachedToVisualTree += OnAttached;
            Find();
        }

        public void Detach()
        {
            _panel.Children.CollectionChanged -= OnChildrenChanged;
            _panel.AttachedToVisualTree -= OnAttached;
            _top.Watch(null);
            _bottom.Watch(null);
        }

        private void OnChildrenChanged(object? sender, EventArgs e) => Find();

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Find();

        private ContentPresenter? Part(string name) =>
            _panel.Children.OfType<ContentPresenter>().FirstOrDefault(p => p.Name == name);

        private void Find()
        {
            _top.Watch(Part("PART_TopCommandBar"));
            _bottom.Watch(Part("PART_BottomCommandBar"));
            Update();
        }

        /// <summary>The content's margin: each bar's closed height, at its edge.</summary>
        public void Update()
        {
            if (Part("PART_Content") is not { } content)
            {
                return;
            }

            var margin = new Thickness(0, _top.Height, 0, _bottom.Height);
            if (content.Margin != margin)
            {
                content.Margin = margin;
            }
        }
    }

    /// <summary>One bar's presenter, and the height it keeps while its bar is open.</summary>
    private sealed class Bar(Reserve reserve)
    {
        private ContentPresenter? _presenter;
        private CommandBar? _commandBar;
        private double? _closed;
        private AppBarClosedDisplayMode _closedMode;

        /// <summary>The height the content gives up for this bar: none while it isn't shown; its closed height.</summary>
        public double Height
        {
            get
            {
                if (_presenter is not { IsVisible: true } presenter || presenter.Child is null)
                {
                    return 0;
                }

                if (_commandBar is { IsOpen: true } open)
                {
                    var mode = CommandBarOptions.GetClosedDisplayMode(open);
                    return _closed is { } closed && _closedMode == mode ? closed : ThemeHeight(presenter, mode);
                }

                _closed = presenter.Bounds.Height;
                if (_commandBar is { } bar)
                {
                    _closedMode = CommandBarOptions.GetClosedDisplayMode(bar);
                }

                return _closed.Value;
            }
        }

        public void Watch(ContentPresenter? presenter)
        {
            if (ReferenceEquals(presenter, _presenter))
            {
                return;
            }

            if (_presenter is not null)
            {
                _presenter.PropertyChanged -= OnPresenterChanged;
            }

            _presenter = presenter;
            if (_presenter is not null)
            {
                _presenter.PropertyChanged += OnPresenterChanged;
            }

            WatchBar(_presenter?.Child as CommandBar);
        }

        private void WatchBar(CommandBar? bar)
        {
            if (ReferenceEquals(bar, _commandBar))
            {
                return;
            }

            if (_commandBar is not null)
            {
                _commandBar.PropertyChanged -= OnBarChanged;
            }

            _commandBar = bar;
            _closed = null;
            if (_commandBar is not null)
            {
                _commandBar.PropertyChanged += OnBarChanged;
            }
        }

        private void OnPresenterChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ContentPresenter.ChildProperty)
            {
                WatchBar(_presenter?.Child as CommandBar);
                reserve.Update();
            }
            else if (e.Property == Visual.BoundsProperty || e.Property == Visual.IsVisibleProperty)
            {
                reserve.Update();
            }
        }

        private void OnBarChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CommandBar.IsOpenProperty || e.Property == CommandBarOptions.ClosedDisplayModeProperty)
            {
                reserve.Update();
            }
        }

        /// <summary>
        /// A bar open before it was laid out closed in its mode: the CommandBar theme's closed height for the mode, or,
        /// without that theme, the bar's height now.
        /// </summary>
        private static double ThemeHeight(ContentPresenter presenter, AppBarClosedDisplayMode mode) =>
            presenter.TryFindResource(mode == AppBarClosedDisplayMode.Minimal ? MinimalHeightKey : CompactHeightKey, out var value) && value is double height
                ? height
                : presenter.Bounds.Height;
    }
}
