using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Threading;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone's full-screen date and time pickers, for Avalonia's <see cref="DatePicker"/> and <see cref="TimePicker"/>:
/// set by their Wp81 themes on the template's <c>PART_Popup</c>, it shows the picker's presenter in a popup of the
/// theme's own, hosted by <see cref="PhonePopupHost"/> (filling the window, opening and closing in the date and time
/// pickers' rows, <see cref="PhonePopupMotion.DateTimeRows"/>, Back closing it), and the closed picker's text.
/// </summary>
/// <remarks>
/// <para>Avalonia's pickers open <c>PART_Popup</c> on a click of their button and, the moment their presenter confirms
/// or dismisses, close it with <c>Close()</c> and then (on confirm) set the value, in the same call: there is no way to
/// hold that close for the phone's closing motion, and a host whose popup closes another way ends its motion where it is.
/// So <c>PART_Popup</c> is a stand-in that never shows (its placement target, at animation priority while this is on,
/// is a control in no window, so Avalonia's popup can't open, and keeps <c>IsOpen</c> as asked), and the presenter is in
/// the popup <see cref="ShownInProperty"/> names. The stand-in's <c>IsOpen</c> going true opens that popup, its
/// presenter set again to the picker's value; going false (accept, cancel, Escape, Enter) dismisses it the phone's way,
/// in the tap's frame, the value set by Avalonia straight after. Back dismisses it, and the stand-in is closed with it,
/// so the next click opens it again. Opened again while it closes, it opens again once closed.</para>
/// <para>The presenter, Avalonia's own, is placed full screen by the host as any popup's child is: no part class.</para>
/// </remarks>
internal static class PhoneDateTimePicker
{
    /// <summary>Set on the picker's <c>PART_Popup</c>: the popup the presenter is shown in, which the phone's host moves.</summary>
    public static readonly AttachedProperty<Popup?> ShownInProperty =
        AvaloniaProperty.RegisterAttached<Popup, Popup?>("ShownIn", typeof(PhoneDateTimePicker));

    /// <summary>
    /// The closed DatePicker's text from its <c>SelectedDate</c>, <c>DayVisible</c>, <c>MonthVisible</c> and
    /// <c>YearVisible</c>: the date in the culture's short pattern with its visible parts ("7/15/2014"; "7/15" with no
    /// year); none, nothing.
    /// </summary>
    public static readonly IMultiValueConverter DateText = new FuncMultiValueConverter<object?, string>(values =>
    {
        var list = values.ToList();
        bool Shown(int i) => list.Count <= i || list[i] is not false;
        return Date(list.Count > 0 ? list[0] as DateTimeOffset? : null, Shown(1), Shown(2), Shown(3), CultureInfo.CurrentCulture);
    });

    /// <summary>
    /// The closed TimePicker's text from its <c>SelectedTime</c>, <c>ClockIdentifier</c> and <c>UseSeconds</c>: the
    /// culture's pattern on the picker's clock ("12:30 PM", "12:30", "12:30:45 PM"); none, nothing.
    /// </summary>
    public static readonly IMultiValueConverter TimeText = new FuncMultiValueConverter<object?, string>(values =>
    {
        var list = values.ToList();
        return Time(list.Count > 0 ? list[0] as TimeSpan? : null, list.Count > 1 ? list[1] as string : null,
            CultureInfo.CurrentCulture, list.Count > 2 && list[2] is true);
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
    /// A date in the culture's short pattern, its hidden parts (and the separators by them) left out; none shown,
    /// nothing.
    /// </summary>
    internal static string Date(DateTimeOffset? date, bool day, bool month, bool year, CultureInfo culture)
    {
        if (date is not { } value)
            return string.Empty;

        // The pattern's fields (runs of d, M or y) and what lies between them.
        var pattern = culture.DateTimeFormat.ShortDatePattern;
        var fields = new List<string>();
        var separator = string.Empty;
        for (var i = 0; i < pattern.Length;)
        {
            var j = i;
            if ("dMy".Contains(pattern[i]))
            {
                while (j < pattern.Length && pattern[j] == pattern[i])
                    j++;
                var shown = pattern[i] switch { 'd' => day, 'M' => month, _ => year };
                if (shown)
                    fields.Add(pattern[i..j]);
            }
            else
            {
                while (j < pattern.Length && !"dMy".Contains(pattern[j]))
                    j++;
                if (separator.Length == 0)
                    separator = pattern[i..j];
            }

            i = j;
        }

        // A field alone is formatted as a pattern, not as a standard format's one letter.
        return fields.Count == 0 ? string.Empty : value.ToString(string.Join(separator, fields) + (fields.Count == 1 ? " " : ""), culture).TrimEnd();
    }

    /// <summary>
    /// A time on a clock: the culture's short pattern (its long one with seconds), its hours made 12 (with the culture's
    /// AM and PM, unpadded) or 24 as <paramref name="clock"/> says (Avalonia's identifiers, 12 by default). Letters in
    /// quotes are the pattern's text and stay. A time outside a day is taken round it.
    /// </summary>
    internal static string Time(TimeSpan? time, string? clock, CultureInfo culture, bool seconds = false)
    {
        if (time is not { } value)
            return string.Empty;

        var day = TimeSpan.FromDays(1);
        value = TimeSpan.FromTicks(((value.Ticks % day.Ticks) + day.Ticks) % day.Ticks);
        var pattern = seconds ? culture.DateTimeFormat.LongTimePattern : culture.DateTimeFormat.ShortTimePattern;
        var twelve = clock != "24HourClock";
        var hasTwelve = Unquoted(pattern).Contains('h');
        if (twelve && !hasTwelve)
            pattern = MapUnquoted(pattern, (c, next) => c == 'H' ? (next == 'H' ? "" : "h") : c.ToString()) + " tt";
        else if (!twelve && hasTwelve)
            pattern = MapUnquoted(pattern, (c, _) => c switch { 'h' => "H", 't' => "", _ => c.ToString() }).Trim();
        return DateTime.MinValue.Add(value).ToString(pattern, culture);
    }

    /// <summary>The pattern's letters outside quotes and escapes.</summary>
    private static string Unquoted(string pattern) => MapUnquoted(pattern, (c, _) => c.ToString(), quoted: false);

    /// <summary>The pattern, each letter outside quotes and escapes mapped (given the next one); the rest kept, or dropped.</summary>
    private static string MapUnquoted(string pattern, Func<char, char, string> map, bool quoted = true)
    {
        var text = new StringBuilder();
        char? quote = null;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (quote is null && c == '\\' && i + 1 < pattern.Length)
            {
                if (quoted)
                    text.Append(c).Append(pattern[i + 1]);
                i++;
            }
            else if (quote is null && c is '\'' or '"')
            {
                quote = c;
                if (quoted)
                    text.Append(c);
            }
            else if (c == quote)
            {
                quote = null;
                if (quoted)
                    text.Append(c);
            }
            else if (quote is null)
            {
                text.Append(map(c, i + 1 < pattern.Length ? pattern[i + 1] : '\0'));
            }
            else if (quoted)
            {
                text.Append(c);
            }
        }

        return text.ToString();
    }

    /// <summary>One picker's stand-in and the popup its presenter is shown in.</summary>
    private sealed class Bridge : IDisposable
    {
        private readonly Popup _standIn;
        private readonly Popup _popup;
        private readonly IDisposable? _target;
        private bool _reopen;

        public Bridge(Popup standIn, Popup popup)
        {
            _standIn = standIn;
            _popup = popup;
            // In no window: Avalonia's popup asked to open can't, and stays asked (IsOpen true) until it is closed. Over
            // whatever the template or an app gives, until this goes.
            _target = standIn.SetValue(Popup.PlacementTargetProperty, new Control(), BindingPriority.Animation);
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
            _target?.Dispose();
            if (_standIn.IsOpen)
                _standIn.Close();
        }

        private void OnStandInChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property != Popup.IsOpenProperty)
                return;

            if (change.GetNewValue<bool>())
            {
                Open();
            }
            else
            {
                _reopen = false;
                Host?.Dismiss();
            }
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

            // Asked again while it closes: it opens again once closed.
            if (Host is { IsClosing: true })
            {
                _reopen = true;
                return;
            }

            Reset(_popup.Child);
            _popup.IsOpen = true;
        }

        /// <summary>
        /// The presenter's wheels set again to its value: Avalonia's set them only when the value changes, so a picker
        /// cancelled after its wheels were turned would open, and accept, where they were left.
        /// </summary>
        private static void Reset(Control? presenter)
        {
            switch (presenter)
            {
                case DatePickerPresenter date:
                {
                    var value = date.Date;
                    date.SetCurrentValue(DatePickerPresenter.DateProperty, value.AddTicks(value >= date.MaxYear ? -1 : 1));
                    date.SetCurrentValue(DatePickerPresenter.DateProperty, value);
                    break;
                }

                case TimePickerPresenter time:
                {
                    var value = time.Time;
                    time.SetCurrentValue(TimePickerPresenter.TimeProperty, value + TimeSpan.FromTicks(1));
                    time.SetCurrentValue(TimePickerPresenter.TimeProperty, value);
                    break;
                }
            }
        }

        /// <summary>Closed: opened again if asked to while it closed; else the picker's own popup closed too.</summary>
        private void OnClosed(object? sender, EventArgs e)
        {
            if (_reopen && _standIn.IsOpen)
            {
                // After the host has seen it close.
                _reopen = false;
                Dispatcher.UIThread.Post(() =>
                {
                    if (_standIn.IsOpen && !_popup.IsOpen && Bridges.TryGetValue(_standIn, out var bridge) && bridge == this)
                        Open();
                });
                return;
            }

            if (_standIn.IsOpen)
                _standIn.Close();
        }
    }
}
