using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Cornflower.Controls;

/// <summary>The data of <see cref="DatePickerFlyout.DatePicked"/>: WinRT's <c>DatePickedEventArgs</c>.</summary>
public sealed class DatePickedEventArgs : EventArgs
{
    public DatePickedEventArgs(DateTimeOffset oldDate, DateTimeOffset newDate)
    {
        OldDate = oldDate;
        NewDate = newDate;
    }

    /// <summary>The flyout's <see cref="DatePickerFlyout.Date"/> before the pick.</summary>
    public DateTimeOffset OldDate { get; }

    /// <summary>The date picked, the flyout's <see cref="DatePickerFlyout.Date"/> now.</summary>
    public DateTimeOffset NewDate { get; }
}

/// <summary>
/// WinRT's <c>DatePickerFlyout</c>: the phone's full-screen date picker, shown on its own from any element, with no
/// <see cref="DatePicker"/> on the page. Its wheels start at <see cref="Date"/>; accept sets it, raises
/// <see cref="DatePicked"/> (even when the date is the same) and closes it, and <see cref="ShowAtAsync"/> returns the
/// date. Cancel, Back or <c>Hide()</c> close it with nothing raised and no date.
/// </summary>
/// <remarks>
/// Behaviour only; its presenter is a <see cref="DatePickerFlyoutPresenter"/>, Avalonia's
/// <see cref="DatePickerPresenter"/>, which a theme draws. Under the Windows Phone 8.1 theme it is the DatePicker's own
/// picker: full screen, its wheels and the pickers' bar, in the date and time pickers' rows. Gregorian only, as Avalonia's
/// presenter is.
/// </remarks>
/// <example>
/// <code>var date = await new DatePickerFlyout { Date = birthday }.ShowAtAsync(button);</code>
/// </example>
public class DatePickerFlyout : PickerFlyoutBase
{
    /// <summary>Defines <see cref="Date"/>.</summary>
    public static readonly StyledProperty<DateTimeOffset> DateProperty =
        AvaloniaProperty.Register<DatePickerFlyout, DateTimeOffset>(nameof(Date));

    /// <summary>Defines <see cref="MinYear"/>.</summary>
    public static readonly StyledProperty<DateTimeOffset> MinYearProperty =
        AvaloniaProperty.Register<DatePickerFlyout, DateTimeOffset>(nameof(MinYear),
            DatePickerPresenter.MinYearProperty.GetDefaultValue(typeof(DatePickerPresenter)));

    /// <summary>Defines <see cref="MaxYear"/>.</summary>
    public static readonly StyledProperty<DateTimeOffset> MaxYearProperty =
        AvaloniaProperty.Register<DatePickerFlyout, DateTimeOffset>(nameof(MaxYear),
            DatePickerPresenter.MaxYearProperty.GetDefaultValue(typeof(DatePickerPresenter)));

    /// <summary>Defines <see cref="DayVisible"/>.</summary>
    public static readonly StyledProperty<bool> DayVisibleProperty =
        AvaloniaProperty.Register<DatePickerFlyout, bool>(nameof(DayVisible), true);

    /// <summary>Defines <see cref="MonthVisible"/>.</summary>
    public static readonly StyledProperty<bool> MonthVisibleProperty =
        AvaloniaProperty.Register<DatePickerFlyout, bool>(nameof(MonthVisible), true);

    /// <summary>Defines <see cref="YearVisible"/>.</summary>
    public static readonly StyledProperty<bool> YearVisibleProperty =
        AvaloniaProperty.Register<DatePickerFlyout, bool>(nameof(YearVisible), true);

    private TaskCompletionSource<DateTimeOffset?>? _result;

    /// <summary>Today, as WinRT's.</summary>
    public DatePickerFlyout()
    {
        SetCurrentValue(DateProperty, DateTimeOffset.Now);
    }

    /// <summary>The date the wheels start at, and the one picked: today by default.</summary>
    public DateTimeOffset Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    /// <summary>The first year the year wheel shows. Avalonia's presenter's default.</summary>
    public DateTimeOffset MinYear
    {
        get => GetValue(MinYearProperty);
        set => SetValue(MinYearProperty, value);
    }

    /// <summary>The last year the year wheel shows. Avalonia's presenter's default.</summary>
    public DateTimeOffset MaxYear
    {
        get => GetValue(MaxYearProperty);
        set => SetValue(MaxYearProperty, value);
    }

    /// <summary>Whether the day wheel shows. True by default.</summary>
    public bool DayVisible
    {
        get => GetValue(DayVisibleProperty);
        set => SetValue(DayVisibleProperty, value);
    }

    /// <summary>Whether the month wheel shows. True by default.</summary>
    public bool MonthVisible
    {
        get => GetValue(MonthVisibleProperty);
        set => SetValue(MonthVisibleProperty, value);
    }

    /// <summary>Whether the year wheel shows. True by default.</summary>
    public bool YearVisible
    {
        get => GetValue(YearVisibleProperty);
        set => SetValue(YearVisibleProperty, value);
    }

    /// <summary>Raised by accept, in its frame, before the flyout closes: even when the date is the one it had.</summary>
    public event EventHandler<DatePickedEventArgs>? DatePicked;

    /// <summary>
    /// Shows the flyout at <paramref name="target"/> and waits for it: the date picked, or null if it was closed any
    /// other way (in <c>Closed</c>'s frame) or couldn't open.
    /// </summary>
    public Task<DateTimeOffset?> ShowAtAsync(Control target)
    {
        _result?.TrySetResult(null);
        _result = null;
        // Shown first: shown at another target, it closes there, and that close ends no wait of this call's.
        ShowAt(target);
        if (!IsOpen)
            return Task.FromResult<DateTimeOffset?>(null);
        _result = new TaskCompletionSource<DateTimeOffset?>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _result.Task;
    }

    /// <inheritdoc />
    protected override Control CreatePresenter() => Hook(new DatePickerFlyoutPresenter());

    /// <summary>The flyout has its accept and cancel buttons, as WinRT's.</summary>
    protected override bool ShouldShowConfirmationButtons() => true;

    /// <summary>The presenter's range, wheels and date, its wheels turned to the date again.</summary>
    protected override void PreparePresenter(PickerPresenterBase presenter)
    {
        base.PreparePresenter(presenter);
        if (presenter is not DatePickerPresenter picker)
            return;
        picker.MinYear = MinYear;
        picker.MaxYear = MaxYear;
        picker.DayVisible = DayVisible;
        picker.MonthVisible = MonthVisible;
        picker.YearVisible = YearVisible;
        // Avalonia's presenter turns its wheels only when its date changes: one cancelled after they were turned would
        // open, and accept, where they were left.
        var date = Date;
        picker.Date = date.AddTicks(date >= picker.MaxYear ? -1 : 1);
        picker.Date = date;
        // Measured in its window: Avalonia measures a flyout's presenter unbounded the first time, to place it, and the
        // presenter's wheels can't be.
        if (picker.DesiredSize == default && TopLevel.GetTopLevel(Target) is { } top)
            picker.Measure(top.ClientSize);
    }

    /// <summary>Accept: the date set, <see cref="DatePicked"/> and the result, then the flyout closes.</summary>
    protected override void OnConfirmed()
    {
        if (CurrentPresenter is DatePickerPresenter picker)
        {
            var old = Date;
            SetCurrentValue(DateProperty, picker.Date);
            DatePicked?.Invoke(this, new DatePickedEventArgs(old, Date));
            _result?.TrySetResult(Date);
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
/// The presenter of a <see cref="DatePickerFlyout"/>, WinRT's <c>DatePickerFlyoutPresenter</c>: Avalonia's
/// <see cref="DatePickerPresenter"/>, a type of its own so a theme can draw it as the flyout's (it has no members of its
/// own, as WinRT's has none).
/// </summary>
public class DatePickerFlyoutPresenter : DatePickerPresenter
{
}
