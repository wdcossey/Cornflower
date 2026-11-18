using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using Cornflower.Themes.Wp81;
using static Cornflower.Tests.Controls.DateTimePickerReadings;
using Stage = Cornflower.Tests.Controls.ListPickerFlyoutTests.Stage;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Cornflower's <see cref="DatePickerFlyout"/> and <see cref="TimePickerFlyout"/> under Wp81's themes (their presenters'
/// in <c>Controls/DatePicker.axaml</c> and <c>Controls/TimePicker.axaml</c>, on the phone's popup host by
/// <see cref="PhonePickerFlyout"/>), against #150's and #149's readings of the reference app's flyouts
/// (docs/controls/DatePickerFlyout/PLAN.md).
/// </summary>
public class DatePickerFlyoutTests
{
    /// <summary>
    /// #149 (run <c>20261116-1835-dev11</c>): the flyouts' <c>Closed</c> 17 ticks after accept or cancel, in all eight
    /// calls of each, a tick after their last band has gone.
    /// </summary>
    private const int FlyoutClosedTicks = 17;

    private static readonly DateTimeOffset July15 = DateTimePickerTests.July15;

    private static readonly TimeSpan HalfPastTwelve = DateTimePickerTests.HalfPastTwelve;

    /// <summary>The reference app's culture, en-US, for a test.</summary>
    private sealed class EnUs : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

        public EnUs(string name = "en-US") => CultureInfo.CurrentCulture = new CultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _culture;
    }

    private static DatePickerFlyout Date() => new() { Date = July15 };

    private static TimePickerFlyout Time() => new() { Time = HalfPastTwelve, ClockIdentifier = "12HourClock" };

    public static TheoryData<string> Flyouts => ["date", "time"];

    private static PickerFlyoutBase Make(string which) => which == "date" ? Date() : Time();

    private static T Done<T>(Task<T> task)
    {
        Assert.True(task.IsCompleted, "the wait hasn't ended");
        return task.Result;
    }

    /// <summary>Turns a wheel of the open flyout: the date's month to August, or the time's minute to 45.</summary>
    private static void Turn(Stage stage)
    {
        if (stage.Picker is DatePickerFlyout)
            stage.Part<DateTimePickerPanel>("PART_MonthSelector").SelectedValue = 8;
        else
            stage.Part<DateTimePickerPanel>("PART_MinuteSelector").SelectedValue = 45;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Flyouts))]
    public void ShowAt_Opens_The_Pickers_Own_Presenter_Filling_The_Window_On_The_Phones_Host(string which)
    {
        using var culture = new EnUs();
        using var stage = new Stage(Make(which));
        stage.OpenAtRest();

        Assert.IsType(which == "date" ? typeof(DatePickerFlyoutPresenter) : typeof(TimePickerFlyoutPresenter), stage.Presenter);
        var host = Assert.IsType<PhonePopupHost>(PhonePickerFlyout.HostOf(stage.Presenter));
        Assert.Equal(PhonePopupPlacement.Full, host.Placement);
        Assert.Equal(Screen, stage.InWindow(stage.Presenter));
        // The DatePicker's picker: its three wheels side by side, over the pickers' bar.
        var wheels = stage.Presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible)
            .Select(p => stage.InWindow(p)).OrderBy(r => r.X).ToList();
        Assert.Equal(WheelLefts, wheels.Select(r => r.X), (a, b) => Math.Abs(a - b) < 0.5);
        Assert.All(wheels, r => Assert.Equal(WheelTop, r.Y, 0.5));
        Assert.Equal(BarTop, stage.InWindow(stage.Part<Control>("PART_AcceptButton").GetVisualAncestors()
            .OfType<CommandBar>().First()).Y, 0.5);
    }

    [AvaloniaTheory]
    [InlineData("date", "Dark")]
    [InlineData("date", "Light")]
    [InlineData("time", "Dark")]
    [InlineData("time", "Light")]
    public void At_Rest_It_Is_Drawn_As_The_Pickers_Own_Picker(string which, string variant)
    {
        using var culture = new EnUs();
        var theme = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        WriteableBitmap expected;
        using (var picker = new DateTimePickerTests.Stage(which == "date" ? DateTimePickerTests.Date() : DateTimePickerTests.Time(), theme))
        {
            picker.OpenAtRest();
            expected = picker.Scene.Capture();
        }

        using var held = expected;
        using var stage = new Stage(Make(which), theme);
        stage.OpenAtRest();
        using var frame = stage.Scene.Capture();
        // The DatePicker's open picker, which docs/controls/DatePicker/COMPARISON.md sets beside the phone's flyouts.
        Assert.True(Frames.SameAs(expected, frame));
    }

    [AvaloniaFact]
    public void The_Wheels_Start_At_The_Flyouts_Value_The_Dates_In_Two_Lines()
    {
        using var culture = new EnUs();
        using (var stage = new Stage(Date()))
        {
            stage.OpenAtRest();
            var month = stage.Part<DateTimePickerPanel>("PART_MonthSelector");
            Assert.Equal("07\nJuly", month.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.IsSelected).Content);
            Assert.Equal(15, stage.Part<DateTimePickerPanel>("PART_DaySelector").SelectedValue);
            Assert.Equal(2014, stage.Part<DateTimePickerPanel>("PART_YearSelector").SelectedValue);
        }

        using var time = new Stage(Time());
        time.OpenAtRest();
        Assert.Equal(12, time.Part<DateTimePickerPanel>("PART_HourSelector").SelectedValue);
        Assert.Equal(30, time.Part<DateTimePickerPanel>("PART_MinuteSelector").SelectedValue);
    }

    [AvaloniaFact]
    public void Its_Members_Reach_The_Wheels()
    {
        using var culture = new EnUs();
        using (var stage = new Stage(new DatePickerFlyout
               {
                   Date = July15, DayVisible = false, MinYear = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero),
                   MaxYear = new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero),
               }))
        {
            stage.OpenAtRest();
            Assert.False(stage.Part<Control>("PART_DayHost").IsVisible);
            Assert.True(stage.Part<Control>("PART_MonthHost").IsVisible);
            Assert.True(stage.Part<Control>("PART_YearHost").IsVisible);
            var year = stage.Part<DateTimePickerPanel>("PART_YearSelector");
            Assert.Equal(2010, year.MinimumValue);
            Assert.Equal(2020, year.MaximumValue);
        }

        using (var stage = new Stage(new DatePickerFlyout { Date = July15, MonthVisible = false, YearVisible = false }))
        {
            stage.OpenAtRest();
            Assert.False(stage.Part<Control>("PART_MonthHost").IsVisible);
            Assert.False(stage.Part<Control>("PART_YearHost").IsVisible);
        }

        using var time = new Stage(new TimePickerFlyout { Time = HalfPastTwelve, ClockIdentifier = "24HourClock", MinuteIncrement = 15 });
        time.OpenAtRest();
        Assert.Equal(23, time.Part<DateTimePickerPanel>("PART_HourSelector").MaximumValue);
        Assert.Equal(15, time.Part<DateTimePickerPanel>("PART_MinuteSelector").Increment);
        Assert.False(time.Part<Control>("PART_PeriodHost").IsVisible);
    }

    [AvaloniaFact]
    public void By_Default_It_Is_Today_100_Years_Either_Way_And_Now_On_The_Cultures_Clock()
    {
        using (new EnUs())
        {
            var now = DateTimeOffset.Now;
            var date = new DatePickerFlyout();
            Assert.True(Math.Abs((date.Date - now).TotalMinutes) < 1);
            Assert.Equal((now.Year - 100, 1, 1), (date.MinYear.Year, date.MinYear.Month, date.MinYear.Day));
            Assert.Equal((now.Year + 100, 12, 31), (date.MaxYear.Year, date.MaxYear.Month, date.MaxYear.Day));
            Assert.True(date.DayVisible);
            var time = new TimePickerFlyout();
            Assert.Equal("12HourClock", time.ClockIdentifier);
            Assert.Equal(1, time.MinuteIncrement);
            // Round midnight too.
            var apart = Math.Abs((DateTime.Now.TimeOfDay - time.Time).TotalMinutes);
            Assert.True(Math.Min(apart, 24 * 60 - apart) < 1);
        }

        using (new EnUs("de-DE"))
            Assert.Equal("24HourClock", new TimePickerFlyout().ClockIdentifier);
        // An "h" in quotes is text, not the 12-hour clock: "HH 'h' mm".
        using (new EnUs("fr-CA"))
            Assert.Equal("24HourClock", new TimePickerFlyout().ClockIdentifier);
    }

    [AvaloniaFact]
    public void A_Clock_Or_Minute_Step_The_Wheels_Cant_Take_Is_Refused()
    {
        var time = new TimePickerFlyout();
        Assert.ThrowsAny<ArgumentException>(() => time.ClockIdentifier = "24h");
        Assert.ThrowsAny<ArgumentException>(() => time.MinuteIncrement = 0);
        Assert.ThrowsAny<ArgumentException>(() => time.MinuteIncrement = 60);
    }

    [AvaloniaFact]
    public void Its_Year_Range_Can_Move_Past_The_Last_One()
    {
        using var culture = new EnUs();
        var flyout = new DatePickerFlyout
        {
            Date = July15, MinYear = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero),
            MaxYear = new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero),
        };
        using var stage = new Stage(flyout);
        stage.OpenAtRest();
        stage.Back();
        stage.At(stage.Now + 500);

        flyout.MaxYear = new DateTimeOffset(2040, 12, 31, 0, 0, 0, TimeSpan.Zero);
        flyout.MinYear = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        stage.OpenAtRest();
        var year = stage.Part<DateTimePickerPanel>("PART_YearSelector");
        Assert.Equal((2030, 2040), (year.MinimumValue, year.MaximumValue));
    }

    [AvaloniaFact]
    public void A_Date_Past_Its_Range_Opens_Again_At_The_End_Of_It()
    {
        using var culture = new EnUs();
        using var stage = new Stage(new DatePickerFlyout
        {
            Date = new DateTimeOffset(2030, 7, 15, 0, 0, 0, TimeSpan.Zero),
            MinYear = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero),
            MaxYear = new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero),
        });
        stage.OpenAtRest();
        Assert.Equal(2020, stage.Part<DateTimePickerPanel>("PART_YearSelector").SelectedValue);
        stage.Part<DateTimePickerPanel>("PART_MonthSelector").SelectedValue = 8;
        stage.Back();
        stage.At(stage.Now + 500);

        stage.OpenAtRest();
        Assert.Equal(12, stage.Part<DateTimePickerPanel>("PART_MonthSelector").SelectedValue);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Flyouts))]
    public void It_Opens_In_The_Date_And_Time_Pickers_Five_160_Rows(string which)
    {
        using var culture = new EnUs();
        using var stage = new Stage(Make(which));
        var call = stage.Now;
        stage.Open();
        var first = stage.FirstFrameAfter(call);
        stage.At(first);
        var bands = Assert.IsType<PhonePopupBands>(stage.Bands);
        Assert.Equal(5, bands.Bands.Count);
        Assert.All(bands.Bands, b => Assert.Equal(RowHeight, b.Height, 1e-9));
        stage.At(first + 4 * Tick);
        for (var row = 0; row < 5; row++)
            Assert.Equal(80 - RowRates[row] * 4, stage.Bands!.Angles[row], 1e-9);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Flyouts))]
    public void Accept_Picks_In_The_Taps_Frame_And_Closed_Comes_At_17_Ticks(string which)
    {
        using var culture = new EnUs();
        using var stage = new Stage(Make(which));
        var picked = new List<(object Old, object New, double At)>();
        var closed = new List<double>();
        Task result;
        if (stage.Picker is DatePickerFlyout date)
        {
            date.DatePicked += (_, e) => picked.Add((e.OldDate, e.NewDate, stage.Now));
            result = date.ShowAtAsync(stage.Button);
        }
        else
        {
            var time = (TimePickerFlyout)stage.Picker;
            time.TimePicked += (_, e) => picked.Add((e.OldTime, e.NewTime, stage.Now));
            result = time.ShowAtAsync(stage.Button);
        }

        stage.Picker.Closed += (_, _) => closed.Add(stage.Now);
        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Now + 500);
        Turn(stage);

        var tap = stage.Now;
        stage.Tap(stage.Part<Control>("PART_AcceptButton"));

        var august15 = new DateTimeOffset(2014, 8, 15, 0, 0, 0, July15.Offset);
        var quarterToOne = new TimeSpan(12, 45, 0);
        var (old, now, at) = Assert.Single(picked);
        Assert.Equal(tap, at);
        if (stage.Picker is DatePickerFlyout d)
        {
            Assert.Equal((July15, august15), ((DateTimeOffset)old, (DateTimeOffset)now));
            Assert.Equal(august15, d.Date);
            Assert.Equal(august15, Done((Task<DateTimeOffset?>)result));
        }
        else
        {
            Assert.Equal((HalfPastTwelve, quarterToOne), ((TimeSpan)old, (TimeSpan)now));
            Assert.Equal(quarterToOne, ((TimePickerFlyout)stage.Picker).Time);
            Assert.Equal(quarterToOne, Done((Task<TimeSpan?>)result));
        }

        // Five bands, the last gone at 16.2 ticks; Closed a tick later, at 17, where the DatePicker's popup closes at 16.
        var first = stage.FirstFrameAfter(tap);
        stage.At(first + 4 * Tick);
        Assert.Equal(-10.95 * 4, stage.Bands!.Angles[0], 1e-9);
        stage.At(first + (FlyoutClosedTicks - 1) * Tick);
        Assert.True(stage.Picker.Popup.IsOpen);
        Assert.Empty(closed);
        stage.At(first + FlyoutClosedTicks * Tick);
        Assert.False(stage.Picker.Popup.IsOpen);
        Assert.Equal([first + FlyoutClosedTicks * Tick], closed);
    }

    [AvaloniaFact]
    public void Accept_With_Nothing_Changed_Still_Picks()
    {
        using var culture = new EnUs();
        using var stage = new Stage(Date());
        var picked = new List<DatePickedEventArgs>();
        ((DatePickerFlyout)stage.Picker).DatePicked += (_, e) => picked.Add(e);
        stage.OpenAtRest();

        stage.Tap(stage.Part<Control>("PART_AcceptButton"));

        // The probe: old and new both 2014-07-15.
        var e = Assert.Single(picked);
        Assert.Equal(July15, e.OldDate);
        Assert.Equal(July15, e.NewDate);
    }

    [AvaloniaTheory]
    [InlineData("date", "cancel")]
    [InlineData("date", "back")]
    [InlineData("date", "escape")]
    [InlineData("time", "cancel")]
    [InlineData("time", "back")]
    public void Cancel_Back_And_Escape_Pick_Nothing_And_It_Opens_Again_At_Its_Value(string which, string how)
    {
        using var culture = new EnUs();
        using var stage = new Stage(Make(which));
        var picked = 0;
        Task result;
        if (stage.Picker is DatePickerFlyout date)
        {
            date.DatePicked += (_, _) => picked++;
            result = date.ShowAtAsync(stage.Button);
        }
        else
        {
            var time = (TimePickerFlyout)stage.Picker;
            time.TimePicked += (_, _) => picked++;
            result = time.ShowAtAsync(stage.Button);
        }

        Dispatcher.UIThread.RunJobs();
        stage.At(stage.Now + 500);
        Turn(stage);
        var called = stage.Now;
        switch (how)
        {
            case "cancel":
                stage.Tap(stage.Part<Control>("PART_DismissButton"));
                break;
            case "back":
                stage.Back();
                break;
            default:
                stage.Scene.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                Dispatcher.UIThread.RunJobs();
                break;
        }

        Assert.Equal(0, picked);
        Assert.False(result.IsCompleted);
        var first = stage.FirstFrameAfter(called);
        stage.At(first + (FlyoutClosedTicks - 1) * Tick);
        Assert.True(stage.Picker.Popup.IsOpen);
        stage.At(first + FlyoutClosedTicks * Tick);
        Assert.False(stage.Picker.Popup.IsOpen);
        Assert.Equal(0, picked);
        if (result is Task<DateTimeOffset?> dated)
        {
            Assert.Null(Done(dated));
            Assert.Equal(July15, ((DatePickerFlyout)stage.Picker).Date);
        }
        else
        {
            Assert.Null(Done((Task<TimeSpan?>)result));
            Assert.Equal(HalfPastTwelve, ((TimePickerFlyout)stage.Picker).Time);
        }

        // Opened again, the wheels are at the flyout's value, not where they were left.
        stage.OpenAtRest();
        if (which == "date")
            Assert.Equal(7, stage.Part<DateTimePickerPanel>("PART_MonthSelector").SelectedValue);
        else
            Assert.Equal(30, stage.Part<DateTimePickerPanel>("PART_MinuteSelector").SelectedValue);
    }

    [AvaloniaFact]
    public void A_Window_Of_Another_Height_Is_Cut_In_Five_Rows_And_Closed_Comes_At_17_Ticks()
    {
        using var culture = new EnUs();
        using var stage = new Stage(Time(), width: 411, height: 914);
        var call = stage.Now;
        stage.Open();
        stage.At(stage.FirstFrameAfter(call));
        Assert.Equal(5, stage.Bands!.Bands.Count);
        Assert.Equal(914 / 5.0, stage.Bands.Bands[0].Height, 1);
        stage.At(stage.Now + 500);

        var tap = stage.Now;
        stage.Tap(stage.Part<Control>("PART_AcceptButton"));
        var first = stage.FirstFrameAfter(tap);
        stage.At(first + (FlyoutClosedTicks - 1) * Tick);
        Assert.True(stage.Picker.Popup.IsOpen);
        stage.At(first + FlyoutClosedTicks * Tick);
        Assert.False(stage.Picker.Popup.IsOpen);
    }

    [AvaloniaFact]
    public void A_Second_Accept_While_It_Closes_Picks_Nothing()
    {
        using var culture = new EnUs();
        using var stage = new Stage(Date());
        var picked = 0;
        ((DatePickerFlyout)stage.Picker).DatePicked += (_, _) => picked++;
        stage.OpenAtRest();
        stage.Tap(stage.Part<Control>("PART_AcceptButton"));
        stage.Scene.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, picked);
    }
}
