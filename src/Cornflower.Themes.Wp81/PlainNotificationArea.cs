using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The plain WindowNotificationManager's place (ADR 0007, #241): installed by its host in the window's adorner layer,
/// which covers the whole window, status bar and all, it keeps its banners inside the window's safe area, so they sit
/// under a phone's status bar, not behind it. The safe area becomes the manager's &lt;c&gt;Padding&lt;/c&gt; (at style priority,
/// so an app's own wins), kept up as it changes. A manager an app places in its own layout is already inside its
/// view's padding, so it is left alone.
/// </summary>
internal static class PlainNotificationArea
{
    /// <summary>Set by the plain WindowNotificationManager theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<WindowNotificationManager, bool>("IsEnabled", typeof(PlainNotificationArea));

    /// <summary>A window's safe area and its changes.</summary>
    internal interface ISafeArea
    {
        Thickness Padding { get; }

        event EventHandler<SafeAreaChangedArgs>? Changed;
    }

    /// <summary>Where a window's safe area comes from: its insets (none on a desktop without them), or a test's.</summary>
    internal static Func<TopLevel, ISafeArea?> SafeAreaOf = top => top.InsetsManager is { } insets ? new FromInsets(insets) : null;

    /// <summary>Each manager's hold on its window's insets while it is in a window's adorner layer.</summary>
    private static readonly ConditionalWeakTable<WindowNotificationManager, Hold> Holds = new();

    static PlainNotificationArea()
    {
        IsEnabledProperty.Changed.AddClassHandler<WindowNotificationManager>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(WindowNotificationManager manager) => manager.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(WindowNotificationManager manager, bool value) => manager.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(WindowNotificationManager manager, AvaloniaPropertyChangedEventArgs e)
    {
        manager.AttachedToVisualTree -= OnAttached;
        manager.DetachedFromVisualTree -= OnDetached;
        if (e.GetNewValue<bool>())
        {
            manager.AttachedToVisualTree += OnAttached;
            manager.DetachedFromVisualTree += OnDetached;
        }

        Update(manager, TopLevel.GetTopLevel(manager));
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) =>
        Update((WindowNotificationManager)sender!, TopLevel.GetTopLevel((Visual)sender!));

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) =>
        Update((WindowNotificationManager)sender!, null);

    /// <summary>Lets go of the manager's window, then holds the one it is in now (none: it has left its window).</summary>
    private static void Update(WindowNotificationManager manager, TopLevel? top)
    {
        if (Holds.TryGetValue(manager, out var held))
        {
            held.Dispose();
            Holds.Remove(manager);
        }

        if (!manager.GetValue(IsEnabledProperty) || manager.Parent is not AdornerLayer || top is null ||
            SafeAreaOf(top) is not { } area)
            return;
        Holds.Add(manager, new Hold(manager, area));
    }

    private sealed class Hold : IDisposable
    {
        private readonly WindowNotificationManager _manager;
        private readonly ISafeArea _area;
        private IDisposable? _padding;

        public Hold(WindowNotificationManager manager, ISafeArea area)
        {
            _manager = manager;
            _area = area;
            _area.Changed += OnSafeAreaChanged;
            Pad(area.Padding);
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => Pad(e.SafeAreaPadding);

        private void Pad(Thickness safeArea)
        {
            _padding?.Dispose();
            _padding = _manager.SetValue(TemplatedControl.PaddingProperty, safeArea, BindingPriority.Style);
        }

        public void Dispose()
        {
            _area.Changed -= OnSafeAreaChanged;
            _padding?.Dispose();
            _padding = null;
        }
    }

    private sealed class FromInsets(IInsetsManager insets) : ISafeArea
    {
        public Thickness Padding => insets.SafeAreaPadding;

        public event EventHandler<SafeAreaChangedArgs>? Changed
        {
            add => insets.SafeAreaChanged += value;
            remove => insets.SafeAreaChanged -= value;
        }
    }
}
