using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Cornflower.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// What the shell's app bar does that the CommandBar theme's XAML can't: the count of the open bar's lines, by which its
/// height steps (<see cref="MenuLinesProperty"/>); its opening and closing motion, frame by frame on
/// <see cref="CornflowerClock"/> (<see cref="ShowsOpenProperty"/>); and a tap on the minimal bar away from its ellipsis,
/// which opens it. Turned on by the CommandBar theme's own setter. As run <c>20261115-2354-dev11</c> and the
/// screenshots of <c>20261116-0005-dev11</c> read them (tools/refapp/README.md, "The app bar under touches, and with
/// other numbers of secondary commands").
/// </summary>
/// <remarks>
/// <para>The motion: the bar's top moves; the bar doesn't fade. Opening, the top rises from the closed bar's to the
/// open bar's on WinRT's <c>ExponentialEase</c>, easing out, exponent <see cref="OpenExponent"/>, over
/// <see cref="OpenLength"/>; closing, exponent <see cref="CloseExponent"/> over <see cref="CloseLength"/>. Each is fitted
/// to the medians of the fourteen openings and fourteen closings recorded, read the same way (tools/motion/appbar.py: the
/// time constant of the rest of the way, from 90% to 1.5% to go; when it is 90% there; when it is at rest). Opening,
/// the recording's medians are 83, 200 and 417 ms (ranges 76 to 92, 145 to 255, 384 to 455); this curve's, on 16 ms
/// frames, 83, 208 and 416. Closing, 35, 79 and 200 ms (27 to 52, 67 to 127, 163 to 233); this one's 34, 96 and 192.
/// The recording's milliseconds are taken as they are, as this clock's (ADR 0005: whether the recording's clock runs
/// with the phone's stopwatch or with its animation time is open, a difference of up to 4%, 17 ms of the opening, under
/// the recording's frame).</para>
/// <para>The bar's own height follows <c>IsOpen</c> at once, as the theme's styles set it, so a page's layout (and the
/// ContentPage's reserve, which keeps the closed height free) sees what it saw before. The drawing moves: the template's
/// <c>LayoutRoot</c> is given a top margin of the bar's height less the height shown, so its top is where the motion has
/// the bar's top and its foot stays at the bar's; what doesn't fit in it is cut there (the labels, as it starts to
/// open), as Avalonia cuts a part arranged smaller than it asks. Opening, the margin is positive and the
/// open layout rises into view; closing, it is negative, so the root reaches above the closed bar, and the bar keeps its
/// open look, <see cref="ShowsOpenProperty"/> still true, until it is down.</para>
/// <para>Out of a window there are no frames: the bar goes straight to where it is going. A bar laid out at no height
/// yet does too, and so does a bar whose app sets its <c>Height</c>: its heights are the app's, not the theme's.</para>
/// </remarks>
internal static class PhoneAppBar
{
    /// <summary>The opening's ease: WinRT's <c>ExponentialEase.Exponent</c>, easing out.</summary>
    internal const double OpenExponent = 4.5;

    /// <summary>The opening's length: the top at rest 420 ms after it starts.</summary>
    internal static readonly TimeSpan OpenLength = TimeSpan.FromMilliseconds(420);

    /// <summary>The closing's ease, the same way.</summary>
    internal const double CloseExponent = 5.5;

    /// <summary>The closing's length: the top at rest 200 ms after it starts.</summary>
    internal static readonly TimeSpan CloseLength = TimeSpan.FromMilliseconds(200);

    /// <summary>The most lines <see cref="MenuLinesProperty"/> counts: the open bar is no taller with more.</summary>
    internal const int MostMenuLines = 4;

    /// <summary>Set by the CommandBar theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<CommandBar, bool>("IsEnabled", typeof(PhoneAppBar));

    /// <summary>
    /// The lines the open bar's list shows, its <c>OverflowItems</c> that are visible and not separators (a separator
    /// draws nothing), at most <see cref="MostMenuLines"/>: the theme's styles step the open bar's height by it.
    /// </summary>
    public static readonly AttachedProperty<int> MenuLinesProperty =
        AvaloniaProperty.RegisterAttached<CommandBar, int>("MenuLines", typeof(PhoneAppBar));

    /// <summary>
    /// Whether the bar looks open: true from the moment it opens until its closing motion ends, so it closes with its
    /// labels and its list. The theme's open look keys on it; its heights key on <c>IsOpen</c>.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowsOpenProperty =
        AvaloniaProperty.RegisterAttached<CommandBar, bool>("ShowsOpen", typeof(PhoneAppBar));

    private static readonly ConditionalWeakTable<CommandBar, Mover> Movers = new();

    static PhoneAppBar()
    {
        IsEnabledProperty.Changed.AddClassHandler<CommandBar>((bar, change) =>
        {
            if (Movers.TryGetValue(bar, out var old))
            {
                Movers.Remove(bar);
                old.Dispose();
            }

            if (change.GetNewValue<bool>())
                Movers.Add(bar, new Mover(bar));
        });
    }

    public static bool GetIsEnabled(CommandBar bar) => bar.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(CommandBar bar, bool value) => bar.SetValue(IsEnabledProperty, value);

    public static int GetMenuLines(CommandBar bar) => bar.GetValue(MenuLinesProperty);

    public static bool GetShowsOpen(CommandBar bar) => bar.GetValue(ShowsOpenProperty);

    /// <summary>One bar's count and motion.</summary>
    private sealed class Mover : IDisposable
    {
        private readonly CommandBar _bar;
        private readonly TemplateParts _parts;
        private Control? _root;
        private IDisposable? _frame;
        private bool _moving;
        private bool _opening;
        private TimeSpan? _start;
        private double _from;

        public Mover(CommandBar bar)
        {
            _bar = bar;
            _bar.PropertyChanged += OnPropertyChanged;
            _bar.SizeChanged += OnSizeChanged;
            _bar.DetachedFromVisualTree += OnDetached;
            _bar.AddHandler(InputElement.TappedEvent, OnTapped);
            if (_bar.OverflowItems is INotifyCollectionChanged items)
                items.CollectionChanged += OnItemsChanged;
            // A new template ends a motion: the bar is where it was going.
            _parts = TemplateParts.Watch(bar, find => _root = find("LayoutRoot"), () =>
            {
                Rest();
                _root = null;
                _bar.SetValue(ShowsOpenProperty, _bar.IsOpen);
            });
            Count();
            _bar.SetValue(ShowsOpenProperty, _bar.IsOpen);
        }

        public void Dispose()
        {
            _bar.PropertyChanged -= OnPropertyChanged;
            _bar.SizeChanged -= OnSizeChanged;
            _bar.DetachedFromVisualTree -= OnDetached;
            _bar.RemoveHandler(InputElement.TappedEvent, OnTapped);
            if (_bar.OverflowItems is INotifyCollectionChanged items)
                items.CollectionChanged -= OnItemsChanged;
            _parts.Dispose();
            Rest();
            _bar.ClearValue(ShowsOpenProperty);
            _bar.ClearValue(MenuLinesProperty);
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Count();

        private void Count()
        {
            var lines = _bar.OverflowItems.Count(item => item is not CommandBarSeparator && item is not Visual { IsVisible: false });
            _bar.SetValue(MenuLinesProperty, Math.Min(lines, MostMenuLines));
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CommandBar.IsOpenProperty)
                Move(e.GetNewValue<bool>());
        }

        /// <summary>
        /// How far the height shown is still short of the bar's height now (negative closing, as it falls to a lower
        /// height): the ease's remainder of the way from the height shown when the motion started. Its time 0 is its
        /// first frame, as the phone's motions are read from the last frame at rest, so a slow first layout doesn't eat
        /// the start of it.
        /// </summary>
        private double Lag()
        {
            if (!_moving)
                return 0;
            var time = _start is { } start ? CornflowerClock.Now - start : TimeSpan.Zero;
            return _opening
                ? PhoneEase.Remaining(_bar.Bounds.Height - _from, OpenExponent, time, OpenLength)
                : PhoneEase.Remaining(_bar.Bounds.Height - _from, CloseExponent, time, CloseLength);
        }

        private void Move(bool open)
        {
            Count();
            // A bar whose app sets its Height has the app's heights, not the theme's steps: it moves not, as it doesn't
            // out of a window or before it is laid out.
            if (_root is null || _bar.Bounds.Height <= 0 || !_bar.IsAttachedToVisualTree() || TopLevel.GetTopLevel(_bar) is null
                || _bar.GetDiagnostic(Layoutable.HeightProperty).Priority == BindingPriority.LocalValue)
            {
                Rest();
                _bar.SetValue(ShowsOpenProperty, open);
                return;
            }

            // From the height shown now: mid-motion, wherever the motion has got to; at rest, the bar's own, which is
            // still the old one, as the bar isn't laid out again until the next layout.
            _from = _bar.Bounds.Height - Lag();
            _opening = open;
            _start = null;
            _moving = true;
            _bar.SetValue(ShowsOpenProperty, true);
            Apply();
            _frame ??= CornflowerClock.RequestFrame(_bar, Step);
        }

        private void Step()
        {
            _frame = null;
            if (!_moving)
                return;
            var start = _start ??= CornflowerClock.Now;
            if (CornflowerClock.Now - start >= (_opening ? OpenLength : CloseLength))
            {
                var opening = _opening;
                Rest();
                _bar.SetValue(ShowsOpenProperty, opening);
                return;
            }

            Apply();
            _frame = CornflowerClock.RequestFrame(_bar, Step);
        }

        /// <summary>The layout root's top where the motion has the bar's, for the bar's height now.</summary>
        private void Apply()
        {
            if (_root is null)
                return;
            var margin = new Thickness(0, Lag(), 0, 0);
            if (_root.Margin != margin)
                _root.Margin = margin;
        }

        /// <summary>Stops the motion and gives the layout root back its own margin.</summary>
        private void Rest()
        {
            _frame?.Dispose();
            _frame = null;
            _moving = false;
            _root?.ClearValue(Layoutable.MarginProperty);
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (_moving)
                Apply();
        }

        /// <summary>Out of the window there are no frames: the bar goes straight to where it was going.</summary>
        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Rest();
            _bar.SetValue(ShowsOpenProperty, _bar.IsOpen);
        }

        /// <summary>A tap on the closed minimal bar, away from its ellipsis (or any button), opens it.</summary>
        private void OnTapped(object? sender, TappedEventArgs e)
        {
            if (_bar.IsOpen || CommandBarOptions.GetClosedDisplayMode(_bar) != AppBarClosedDisplayMode.Minimal)
                return;
            if (e.Source is Visual source && source.GetSelfAndVisualAncestors().TakeWhile(v => v != _bar).Any(v => v is Button))
                return;
            _bar.SetCurrentValue(CommandBar.IsOpenProperty, true);
        }
    }
}
