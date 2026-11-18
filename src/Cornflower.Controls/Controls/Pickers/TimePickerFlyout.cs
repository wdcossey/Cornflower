using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Cornflower.Controls;

/// <summary>The data of <see cref="TimePickerFlyout.TimePicked"/>: WinRT's <c>TimePickedEventArgs</c>.</summary>
public sealed class TimePickedEventArgs : EventArgs
{
    public TimePickedEventArgs(TimeSpan oldTime, TimeSpan newTime)
    {
        OldTime = oldTime;
        NewTime = newTime;
    }

    /// <summary>The flyout's <see cref="TimePickerFlyout.Time"/> before the pick.</summary>
    public TimeSpan OldTime { get; }

    /// <summary>The time picked, the flyout's <see cref="TimePickerFlyout.Time"/> now.</summary>
    public TimeSpan NewTime { get; }
}

/// <summary>
/// WinRT's <c>TimePickerFlyout</c>: the phone's full-screen time picker, shown on its own from any element, with no
/// <see cref="TimePicker"/> on the page. Its wheels start at <see cref="Time"/>; accept sets it, raises
/// <see cref="TimePicked"/> (even when the time is the same) and closes it, and <see cref="ShowAtAsync"/> returns the
/// time. Cancel, Back or <c>Hide()</c> close it with nothing raised and no time.
/// </summary>
/// <remarks>
/// Behaviour only; its presenter is a <see cref="TimePickerFlyoutPresenter"/>, Avalonia's
/// <see cref="TimePickerPresenter"/>, which a theme draws. Under the Windows Phone 8.1 theme it is the TimePicker's own
/// picker: full screen, its wheels and the pickers' bar, in the date and time pickers' rows.
/// </remarks>
/// <example>
/// <code>var time = await new TimePickerFlyout { MinuteIncrement = 15 }.ShowAtAsync(button);</code>
/// </example>
public class TimePickerFlyout : PickerFlyoutBase
{
    /// <summary>Defines <see cref="Time"/>.</summary>
    public static readonly StyledProperty<TimeSpan> TimeProperty =
        AvaloniaProperty.Register<TimePickerFlyout, TimeSpan>(nameof(Time));

    /// <summary>Defines <see cref="ClockIdentifier"/>.</summary>
    public static readonly StyledProperty<string> ClockIdentifierProperty =
        AvaloniaProperty.Register<TimePickerFlyout, string>(nameof(ClockIdentifier), "12HourClock",
            validate: value => value is "12HourClock" or "24HourClock");

    /// <summary>Defines <see cref="MinuteIncrement"/>.</summary>
    public static readonly StyledProperty<int> MinuteIncrementProperty =
        AvaloniaProperty.Register<TimePickerFlyout, int>(nameof(MinuteIncrement), 1, validate: value => value is >= 1 and <= 59);

    private TaskCompletionSource<TimeSpan?>? _result;

    /// <summary>Now, on the culture's clock, as WinRT's.</summary>
    public TimePickerFlyout()
    {
        SetCurrentValue(TimeProperty, DateTime.Now.TimeOfDay);
        // The pattern's letters, not its quoted text: fr-CA's "HH 'h' mm" is a 24-hour clock.
        if (!Regex.Replace(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern, @"'[^']*'|""[^""]*""|\\.", "").Contains('h'))
            SetCurrentValue(ClockIdentifierProperty, "24HourClock");
    }

    /// <summary>The time the wheels start at, and the one picked: now by default.</summary>
    public TimeSpan Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    /// <summary>
    /// The clock: <c>12HourClock</c> (an AM/PM wheel) or <c>24HourClock</c>; any other is refused. The culture's by
    /// default, as WinRT's.
    /// </summary>
    public string ClockIdentifier
    {
        get => GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    /// <summary>The minute wheel's step, in minutes, 1 to 59. 1 by default.</summary>
    public int MinuteIncrement
    {
        get => GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    /// <summary>Raised by accept, in its frame, before the flyout closes: even when the time is the one it had.</summary>
    public event EventHandler<TimePickedEventArgs>? TimePicked;

    /// <summary>
    /// Shows the flyout at <paramref name="target"/> and waits for it: the time picked, or null if it was closed any
    /// other way (in <c>Closed</c>'s frame) or couldn't open.
    /// </summary>
    public Task<TimeSpan?> ShowAtAsync(Control target)
    {
        _result?.TrySetResult(null);
        _result = null;
        // Shown first: shown at another target, it closes there, and that close ends no wait of this call's.
        ShowAt(target);
        if (!IsOpen)
            return Task.FromResult<TimeSpan?>(null);
        _result = new TaskCompletionSource<TimeSpan?>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _result.Task;
    }

    /// <inheritdoc />
    protected override Control CreatePresenter() => Hook(new TimePickerFlyoutPresenter());

    /// <summary>The flyout has its accept and cancel buttons, as WinRT's.</summary>
    protected override bool ShouldShowConfirmationButtons() => true;

    /// <summary>The presenter's clock, step and time, its wheels turned to the time again.</summary>
    protected override void PreparePresenter(PickerPresenterBase presenter)
    {
        base.PreparePresenter(presenter);
        if (presenter is not TimePickerPresenter picker)
            return;
        picker.ClockIdentifier = ClockIdentifier;
        picker.MinuteIncrement = MinuteIncrement;
        // Avalonia's presenter turns its wheels only when its time changes: one cancelled after they were turned would
        // open, and accept, where they were left.
        var time = Time;
        picker.Time = time + TimeSpan.FromTicks(1);
        picker.Time = time;
        // Measured in its window: Avalonia measures a flyout's presenter unbounded the first time, to place it, and the
        // presenter's wheels can't be.
        if (picker.DesiredSize == default && TopLevel.GetTopLevel(Target) is { } top)
            picker.Measure(top.ClientSize);
    }

    /// <summary>Accept: the time set, <see cref="TimePicked"/> and the result, then the flyout closes.</summary>
    protected override void OnConfirmed()
    {
        if (CurrentPresenter is TimePickerPresenter picker)
        {
            var old = Time;
            SetCurrentValue(TimeProperty, picker.Time);
            TimePicked?.Invoke(this, new TimePickedEventArgs(old, Time));
            _result?.TrySetResult(Time);
        }

        base.OnConfirmed();
    }

    /// <inheritdoc />
    protected override void OnClosed()
    {
        _result?.TrySetResult(null);
        _result = null;
        base.OnClosed();
    }
}

/// <summary>
/// The presenter of a <see cref="TimePickerFlyout"/>, WinRT's <c>TimePickerFlyoutPresenter</c>: Avalonia's
/// <see cref="TimePickerPresenter"/>, a type of its own so a theme can draw it as the flyout's (it has no members of its
/// own, as WinRT's has none).
/// </summary>
public class TimePickerFlyoutPresenter : TimePickerPresenter
{
}
