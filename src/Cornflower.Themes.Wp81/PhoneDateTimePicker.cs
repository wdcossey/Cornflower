using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's full-screen date and time pickers, for Avalonia's <see cref="DatePicker"/> and <see cref="TimePicker"/>:
/// set by their Wp81 themes on the template's <c>PART_Popup</c>, it shows the picker's presenter in a popup of the
/// theme's own, hosted by <see cref="PhonePopupHost"/> (filling the window, opening and closing in the date and time
/// pickers' rows, <see cref="PhonePopupMotion.DateTimeRows"/>, Back closing it), and the closed picker's text.
/// </summary>
/// <remarks>
/// <para>Avalonia's pickers open <c>PART_Popup</c> on a click of their button and close it with <c>Close()</c> the moment
/// their presenter confirms or dismisses, before they set the value: there is no way to hold that close for the phone's
/// closing motion, and a host whose popup closes another way ends its motion where it is. So <c>PART_Popup</c> is a
/// stand-in that never shows (its placement target is a control in no window, so Avalonia's popup can't open, and keeps
/// <c>IsOpen</c> as asked), and the presenter is in the popup <see cref="ShownInProperty"/> names. The stand-in's
/// <c>IsOpen</c> going true opens that popup; going false (accept, cancel, Escape, Enter) dismisses it the phone's way,
/// the picker's value already set, in the tap's frame. Back dismisses it too, and the stand-in is closed with it, so the
/// next click opens it again.</para>
/// <para>The presenter, Avalonia's own, is placed full screen by the host as any popup's child is: no part class.</para>
/// </remarks>
internal static class PhoneDateTimePicker
{
    /// <summary>Set on the picker's <c>PART_Popup</c>: the popup the presenter is shown in, which the phone's host moves.</summary>
    public static readonly AttachedProperty<Popup?> ShownInProperty =
        AvaloniaProperty.RegisterAttached<Popup, Popup?>("ShownIn", typeof(PhoneDateTimePicker));

    /// <summary>The closed DatePicker's text: the date in the culture's short pattern ("7/15/2014"); none, nothing.</summary>
    public static readonly IValueConverter DateText = new FuncValueConverter<DateTimeOffset?, string>(date =>
        date?.ToString("d", CultureInfo.CurrentCulture) ?? string.Empty);

    /// <summary>
    /// The closed TimePicker's text from its <c>SelectedTime</c> and <c>ClockIdentifier</c>: the culture's short pattern
    /// on the picker's clock ("12:30 PM", "12:30"); none, nothing.
    /// </summary>
    public static readonly IMultiValueConverter TimeText = new FuncMultiValueConverter<object?, string>(values =>
    {
        var list = values.ToList();
        return Time(list.Count > 0 ? list[0] as TimeSpan? : null, list.Count > 1 ? list[1] as string : null,
            CultureInfo.CurrentCulture);
    });

    private static readonly ConditionalWeakTable<Popup, Bridge> Bridges = new();

    static PhoneDateTimePicker()
    {
        ShownInProperty.Changed.AddClassHandler<Popup>((standIn, change) =>
        {
            if (Bridges.TryGetValue(standIn, out var old))
            {
                old.Dispose();
                Bridges.Remove(standIn);
            }

            if (change.GetNewValue<Popup?>() is { } popup)
                Bridges.Add(standIn, new Bridge(standIn, popup));
        });
    }

    public static Popup? GetShownIn(Popup standIn) => standIn.GetValue(ShownInProperty);

    public static void SetShownIn(Popup standIn, Popup? value) => standIn.SetValue(ShownInProperty, value);

    /// <summary>The host of the popup a stand-in shows its picker in, while the theme sets it.</summary>
    internal static PhonePopupHost? HostOf(Popup standIn) => Bridges.TryGetValue(standIn, out var bridge) ? bridge.Host : null;

    /// <summary>
    /// A time on a clock: the culture's short pattern, its hours made 12 (with the culture's AM and PM) or 24 as
    /// <paramref name="clock"/> says (Avalonia's identifiers, 12 by default).
    /// </summary>
    internal static string Time(TimeSpan? time, string? clock, CultureInfo culture)
    {
        if (time is not { } value)
            return string.Empty;

        var pattern = culture.DateTimeFormat.ShortTimePattern;
        var twelve = clock != "24HourClock";
        if (twelve && !pattern.Contains('h'))
            pattern = pattern.Replace('H', 'h') + " tt";
        else if (!twelve && pattern.Contains('h'))
            pattern = pattern.Replace("tt", string.Empty).Replace('h', 'H').Trim();
        return DateTime.MinValue.Add(value).ToString(pattern, culture);
    }

    /// <summary>One picker's stand-in and the popup its presenter is shown in.</summary>
    private sealed class Bridge : IDisposable
    {
        private readonly Popup _standIn;
        private readonly Popup _popup;
        private readonly bool _hadTarget;
        private readonly Control? _target;

        public Bridge(Popup standIn, Popup popup)
        {
            _standIn = standIn;
            _popup = popup;
            _hadTarget = standIn.IsSet(Popup.PlacementTargetProperty);
            _target = standIn.PlacementTarget;
            // In no window: Avalonia's popup asked to open can't, and stays asked (IsOpen true) until it is closed.
            standIn.PlacementTarget = new Control();
            popup.IsLightDismissEnabled = false;
            standIn.PropertyChanged += OnStandInChanged;
            popup.Closed += OnClosed;
            if (standIn.IsOpen)
                Open();
        }

        /// <summary>The popup's host, made at its first opening, when the template has given it its presenter.</summary>
        public PhonePopupHost? Host { get; private set; }

        public void Dispose()
        {
            _standIn.PropertyChanged -= OnStandInChanged;
            _popup.Closed -= OnClosed;
            Host?.Dispose();
            _popup.IsOpen = false;
            if (_hadTarget)
                _standIn.PlacementTarget = _target;
            else
                _standIn.ClearValue(Popup.PlacementTargetProperty);
        }

        private void OnStandInChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property != Popup.IsOpenProperty)
                return;

            if (change.GetNewValue<bool>())
                Open();
            else
                Host?.Dismiss();
        }

        private void Open()
        {
            if (Host is null && _popup.Child is Control content)
            {
                Host = new PhonePopupHost(_popup, content, () => _popup.IsOpen = false, host =>
                {
                    host.Placement = PhonePopupPlacement.Full;
                    host.Rows = PhonePopupMotion.DateTimeRows;
                });
            }

            _popup.IsOpen = true;
        }

        /// <summary>Closed another way (Back): the picker's own popup closes too, so its next click opens it again.</summary>
        private void OnClosed(object? sender, EventArgs e)
        {
            if (_standIn.IsOpen)
                _standIn.Close();
        }
    }
}
