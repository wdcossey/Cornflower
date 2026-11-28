using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// Back for the plain CalendarDatePicker (ADR 0007), turned on by its theme (<c>Controls/CalendarDatePicker.axaml</c>):
/// with its drop-down open, Back closes it and stops there, as every Wp81 popup closes on Back first (#150, #255). Its
/// popup isn't one of Wp81's <see cref="PhonePopupHost"/>s (it opens and closes at once, below the picker), so it takes
/// part through the hosts' Back handler (<see cref="PhonePopupHost.TakeBackFirst"/>), as an inline ComboBox does: a
/// picker open in the window, not under the popup Back would close, closes. The next Back goes on.
/// </summary>
internal static class PhoneCalendarDatePicker
{
    /// <summary>Set by the CalendarDatePicker theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<CalendarDatePicker, bool>("IsEnabled", typeof(PhoneCalendarDatePicker));

    /// <summary>
    /// The pickers of ours open now, in the order they opened: weakly, as one may leave its window still open, or be
    /// set open in none.
    /// </summary>
    private static readonly List<WeakReference<CalendarDatePicker>> Open = [];

    static PhoneCalendarDatePicker()
    {
        CalendarDatePicker.IsDropDownOpenProperty.Changed.AddClassHandler<CalendarDatePicker>((picker, _) => Update(picker));
        // Set open before the theme turned this on (in XAML, or from code before it is styled): listed now.
        IsEnabledProperty.Changed.AddClassHandler<CalendarDatePicker>((picker, _) => Update(picker));
        PhonePopupHost.TakeBackFirst(TakesBack);
    }

    public static bool GetIsEnabled(CalendarDatePicker picker) => picker.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(CalendarDatePicker picker, bool value) => picker.SetValue(IsEnabledProperty, value);

    /// <summary>Lists the picker while it is open and ours, at the end; drops it, and any gone, otherwise.</summary>
    private static void Update(CalendarDatePicker picker)
    {
        Open.RemoveAll(entry => !entry.TryGetTarget(out var listed) || listed == picker);
        if (picker.IsDropDownOpen && GetIsEnabled(picker))
            Open.Add(new WeakReference<CalendarDatePicker>(picker));
    }

    /// <summary>
    /// Back: the last picker opened in the window, not under the popup Back would close, closes, and Back stops there.
    /// </summary>
    private static bool TakesBack(TopLevel top, PhonePopupHost? host)
    {
        for (var i = Open.Count - 1; i >= 0; i--)
        {
            if (!Open[i].TryGetTarget(out var picker) || TopLevel.GetTopLevel(picker) != top || (host is not null && !host.Holds(picker)))
                continue;
            picker.SetCurrentValue(CalendarDatePicker.IsDropDownOpenProperty, false);
            return true;
        }

        return false;
    }
}
