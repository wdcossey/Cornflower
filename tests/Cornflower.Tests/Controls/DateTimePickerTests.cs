using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
using static Cornflower.Tests.Controls.DateTimePickerReadings;

namespace Cornflower.Tests.Controls;

/// <summary>
/// Avalonia's DatePicker and TimePicker under Wp81's themes (<c>Controls/DatePicker.axaml</c>,
/// <c>Controls/TimePicker.axaml</c>) and their full-screen pickers (<see cref="PhoneDateTimePicker"/> on the phone's
/// popup host), against the emulator's dumps, grabs and probes (<see cref="DateTimePickerReadings"/>).
/// docs/controls/DatePicker/PLAN.md has each reading.
/// </summary>
public class DateTimePickerTests
{
    /// <summary>The reference app's dates and times: 15 July 2014, 12:30.</summary>
    internal static readonly DateTimeOffset July15 = new(2014, 7, 15, 0, 0, 0, TimeSpan.Zero);

    internal static readonly TimeSpan HalfPastTwelve = new(12, 30, 0);

    /// <summary>The reference app's stage: the control in its slot, headed, under en-US, and the phone's clock.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

        public Stage(TemplatedControl picker, ThemeVariant? variant = null, double width = Scene.ReferenceWidth,
            double height = Scene.ReferenceHeight, IEnumerable<IStyle>? themes = null)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            Clock = new TestClock { FrameInterval = TimeSpan.FromMilliseconds(Tick), FramesOnGrid = true };
            Picker = picker;
            picker.Margin = new Thickness(Slot.X, Slot.Y, 0, 0);
            picker.Width = Slot.Width;
            picker.VerticalAlignment = VerticalAlignment.Top;
            picker.HorizontalAlignment = HorizontalAlignment.Left;
            try
            {
                var page = new Panel { Children = { picker } };
                page.Bind(Panel.BackgroundProperty, page.GetResourceObservable("ApplicationPageBackgroundThemeBrush"));
                Scene = new Scene(page, variant, width, height, themes: themes);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public TestClock Clock { get; }

        public TemplatedControl Picker { get; }

        public Scene Scene { get; } = null!;

        public double Now => Clock.Now.TotalMilliseconds;

        public Button FlyoutButton => Part<Button>(Picker, "PART_FlyoutButton");

        public Popup StandIn => Part<Popup>(Picker, "PART_Popup");

        public Popup Popup => Part<Popup>(Picker, "PickerPopup");

        public PickerPresenterBase Presenter => (PickerPresenterBase)Popup.Child!;

        public PhonePopupHost? Host => PhoneDateTimePicker.HostOf(StandIn);

        public PhonePopupBands? Bands => Host?.Bands;

        /// <summary>The motion's first frame: the next tick after the call.</summary>
        public double FirstFrameAfter(double call) => (Math.Floor(call / Tick) + 1) * Tick;

        public void At(double milliseconds)
        {
            Clock.At(milliseconds);
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>A tap on the closed control's button, which opens the picker.</summary>
        public void Open() => Tap(FlyoutButton);

        public void OpenAtRest()
        {
            Open();
            At(Now + 500);
        }

        public T InPresenter<T>(string name) where T : Control =>
            Presenter.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        public void Tap(Visual control)
        {
            var at = CentreOf(control);
            Scene.Window.MouseDown(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Scene.Window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        public Point CentreOf(Visual control) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Scene.Window)!.Value;

        /// <summary>Where a part is laid out in the window, without the motion.</summary>
        public Rect InWindow(Visual part)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            return new Rect(part.TranslatePoint(default, Scene.Window)!.Value, part.Bounds.Size);
        }

        public void Back()
        {
            Scene.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Scene?.Dispose();
            Clock.Dispose();
            CultureInfo.CurrentCulture = _culture;
        }
    }

    internal static DatePicker Date() => new() { SelectedDate = July15, [ControlHeader.HeaderProperty] = "date picker" };

    /// <summary>On the 12-hour clock, as the reference's en-US: Avalonia's picker takes its clock from the culture it is made in.</summary>
    internal static TimePicker Time() => new()
    {
        SelectedTime = HalfPastTwelve, ClockIdentifier = "12HourClock", [ControlHeader.HeaderProperty] = "time picker",
    };

    private static T Part<T>(Visual control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static Color ColourOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Rect Within(Visual part, Visual control) =>
        new(part.TranslatePoint(default, control)!.Value, part.Bounds.Size);

    public static TheoryData<string> Pickers => ["date", "time"];

    private static TemplatedControl Make(string which) => which == "date" ? Date() : Time();

    [AvaloniaFact]
    public void The_Pickers_And_Their_Presenters_Are_Templated_By_The_Theme_And_Hosted_By_The_Phones_Popup()
    {
        foreach (var which in new[] { "date", "time" })
        {
            using var stage = new Stage(Make(which));
            Assert.NotNull(Part<ContentPresenter>(stage.Picker, "HeaderContentPresenter"));
            stage.OpenAtRest();
            Assert.NotNull(stage.InPresenter<Grid>("Columns"));
            Assert.NotNull(stage.InPresenter<CommandBar>("PickerBar"));
            Assert.NotNull(stage.Host);
            Assert.True(stage.Popup.ShouldUseOverlayLayer);
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Pickers))]
    public void Closed_It_Is_The_Header_Over_A_Button_Showing_The_Value_In_The_Short_Pattern(string which)
    {
        using var stage = new Stage(Make(which));
        var picker = stage.Picker;
        var header = Part<ContentPresenter>(picker, "HeaderContentPresenter");
        Assert.Equal(HeaderHeight, header.Bounds.Height, 1);
        Assert.Equal(Header(dark: true), ColourOf(header.Foreground));

        var button = stage.FlyoutButton;
        var bounds = Within(button, picker);
        Assert.Equal(FlyoutButton.Y, bounds.Y, 1);
        Assert.Equal(FlyoutButton.Width, bounds.Width, 1);
        Assert.Equal(FlyoutButton.Height, bounds.Height, 1);
        var border = Part<Border>(button, "Border");
        Assert.Equal(ButtonBorder.Y, Within(border, picker).Y, 1);
        Assert.Equal(ButtonBorder.Height, border.Bounds.Height, 1);
        Assert.Equal(new Thickness(2.5), border.BorderThickness);
        Assert.Equal(Foreground(dark: true), ColourOf(border.BorderBrush));

        var text = Part<ContentPresenter>(button, "PART_ContentPresenter");
        Assert.Equal(TextLeft, Within(text, picker).X, 1);
        Assert.Equal(which == "date" ? DateText : TimeText, text.Content);
        Assert.Equal(TextSize, text.FontSize, 2);
        Assert.Equal(FontWeight.SemiBold, text.FontWeight);
    }

    [AvaloniaTheory]
    [InlineData("date", true)]
    [InlineData("date", false)]
    [InlineData("time", true)]
    [InlineData("time", false)]
    public void Its_Buttons_States_Are_The_Phones_Pressed_The_Accent_Disabled_Base_Low(string which, bool dark)
    {
        using var stage = new Stage(Make(which), dark ? ThemeVariant.Dark : ThemeVariant.Light);
        var button = stage.FlyoutButton;
        var border = Part<Border>(button, "Border");
        var text = Part<ContentPresenter>(button, "PART_ContentPresenter");

        stage.Scene.Window.MouseDown(stage.CentreOf(button), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Accent, ColourOf(border.Background));
        Assert.Equal(OverAccent, ColourOf(text.Foreground));
        Assert.Equal(Foreground(dark), ColourOf(border.BorderBrush));
        stage.Scene.Window.MouseUp(new Point(-1, -1), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        stage.Picker.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Disabled(dark), ColourOf(border.BorderBrush));
        Assert.Equal(Disabled(dark), ColourOf(text.Foreground));
        Assert.Equal(Header(dark), ColourOf(Part<ContentPresenter>(stage.Picker, "HeaderContentPresenter").Foreground));
    }

    [AvaloniaFact]
    public void With_No_Value_The_Button_Is_Empty()
    {
        foreach (var picker in new TemplatedControl[] { new DatePicker(), new TimePicker() })
        {
            using var stage = new Stage(picker);
            Assert.Equal(string.Empty, Part<ContentPresenter>(stage.FlyoutButton, "PART_ContentPresenter").Content);
        }
    }

    [AvaloniaFact]
    public void The_Time_Is_On_The_Pickers_Clock()
    {
        var us = new CultureInfo("en-US");
        Assert.Equal("2:05 PM", PhoneDateTimePicker.Time(new TimeSpan(14, 5, 0), "12HourClock", us));
        Assert.Equal("14:05", PhoneDateTimePicker.Time(new TimeSpan(14, 5, 0), "24HourClock", us));
        var gb = new CultureInfo("en-GB");
        Assert.Equal("14:05", PhoneDateTimePicker.Time(new TimeSpan(14, 5, 0), "24HourClock", gb));
        Assert.Equal("02:05 " + gb.DateTimeFormat.PMDesignator, PhoneDateTimePicker.Time(new TimeSpan(14, 5, 0), "12HourClock", gb));

        using var stage = new Stage(new TimePicker { SelectedTime = new TimeSpan(14, 5, 0), ClockIdentifier = "24HourClock" });
        var text = Part<ContentPresenter>(stage.FlyoutButton, "PART_ContentPresenter");
        Assert.Equal("14:05", text.Content);
        ((TimePicker)stage.Picker).ClockIdentifier = "12HourClock";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("2:05 PM", text.Content);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Pickers))]
    public void A_Tap_Opens_The_Picker_Filling_The_Window_And_Avalonias_Popup_Never_Shows(string which)
    {
        using var stage = new Stage(Make(which));
        stage.OpenAtRest();

        Assert.True(stage.Popup.IsOpen);
        Assert.Equal(PhonePopupPlacement.Full, stage.Host!.Placement);
        Assert.Equal(Screen, stage.InWindow(stage.Presenter));
        // The stand-in is asked to open, as Avalonia's picker asks, and has no popup of its own in any layer.
        Assert.True(stage.StandIn.IsOpen);
        Assert.Single(stage.Scene.Window.GetVisualDescendants().OfType<PopupRoot>().Concat<Visual>(
            stage.Scene.Window.GetVisualDescendants().OfType<OverlayPopupHost>()));
    }

    [AvaloniaTheory]
    [InlineData("date", true)]
    [InlineData("date", false)]
    [InlineData("time", true)]
    [InlineData("time", false)]
    public void The_Picker_Is_Three_Wheels_Side_By_Side_Over_The_Bar_In_The_Chrome(string which, bool dark)
    {
        using var stage = new Stage(Make(which), dark ? ThemeVariant.Dark : ThemeVariant.Light);
        stage.OpenAtRest();
        Assert.Equal(Chrome(dark), ColourOf(stage.Presenter.Background));

        var wheels = stage.Presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible)
            .OrderBy(p => stage.InWindow(p).X).ToList();
        Assert.Equal(3, wheels.Count);
        for (var i = 0; i < 3; i++)
        {
            var at = stage.InWindow(wheels[i]);
            Assert.Equal(WheelLefts[i], at.X, 1);
            Assert.Equal(WheelTop, at.Y, 1);
            Assert.Equal(WheelWidth, at.Width, 1);
            Assert.Equal(WheelHeight, at.Height, 1);
            Assert.Same(stage.Presenter.FindResource("Wp81LoopingSelectorTheme"), wheels[i].Theme);
        }

        // In the culture's order: month, day, year; hour, minute, AM/PM.
        Assert.Equal(which == "date"
                ? [DateTimePickerPanelType.Month, DateTimePickerPanelType.Day, DateTimePickerPanelType.Year]
                : new[] { DateTimePickerPanelType.Hour, DateTimePickerPanelType.Minute, DateTimePickerPanelType.TimePeriod },
            wheels.Select(w => w.PanelType));
        Assert.Equal(which == "date" ? [7, 15, 2014] : new[] { 12, 30, 1 }, wheels.Select(w => w.SelectedValue));

        var bar = stage.InPresenter<CommandBar>("PickerBar");
        Assert.True(bar.IsEffectivelyVisible);
        Assert.Equal(BarTop, stage.InWindow(bar).Y, 1);
        Assert.NotNull(stage.InPresenter<Button>("PART_AcceptButton"));
        Assert.NotNull(stage.InPresenter<Button>("PART_DismissButton"));
    }

    [AvaloniaFact]
    public void The_Date_Wheels_Show_Two_Lines_And_The_Time_Wheels_One_At_43()
    {
        using (var stage = new Stage(Date()))
        {
            stage.OpenAtRest();
            var month = stage.InPresenter<DateTimePickerPanel>("PART_MonthSelector");
            var selected = month.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.IsSelected);
            Assert.Equal("07\nJuly", selected.Content);
        }

        using var time = new Stage(Time());
        time.OpenAtRest();
        var hour = time.InPresenter<DateTimePickerPanel>("PART_HourSelector");
        var item = hour.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.IsSelected);
        Assert.Same(time.Presenter.FindResource("Wp81LoopingSelectorItemTheme"), item.Theme);
        var texts = item.GetVisualDescendants().OfType<TextBlock>().ToList();
        Assert.Equal("12", Assert.Single(texts).Text);
        Assert.Equal(TimeItemTextSize, texts[0].FontSize);
    }

    [AvaloniaFact]
    public void Visible_Parts_And_The_24_Hour_Clock_Leave_The_Wheels_Centred()
    {
        using (var stage = new Stage(new DatePicker { SelectedDate = July15, DayVisible = false }))
        {
            stage.OpenAtRest();
            var wheels = stage.Presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible)
                .Select(p => stage.InWindow(p).X).Order().ToList();
            Assert.Equal([225 - WheelWidth, 225], wheels, (a, b) => Math.Abs(a - b) < 0.5);
        }

        using var time = new Stage(new TimePicker { SelectedTime = HalfPastTwelve, ClockIdentifier = "24HourClock", MinuteIncrement = 15 });
        time.OpenAtRest();
        var times = time.Presenter.GetVisualDescendants().OfType<DateTimePickerPanel>().Where(p => p.IsEffectivelyVisible).ToList();
        Assert.Equal([225 - WheelWidth, 225], times.Select(p => time.InWindow(p).X).Order(), (a, b) => Math.Abs(a - b) < 0.5);
        Assert.Equal(15, time.InPresenter<DateTimePickerPanel>("PART_MinuteSelector").Increment);
        Assert.Equal(23, time.InPresenter<DateTimePickerPanel>("PART_HourSelector").MaximumValue);
    }

    [AvaloniaFact]
    public void It_Opens_In_Five_160_Rows_Turning_Slower_Down_The_Window()
    {
        using var stage = new Stage(Date());
        var call = stage.Now;
        stage.Open();
        var first = stage.FirstFrameAfter(call);

        stage.At(first);
        var bands = Assert.IsType<PhonePopupBands>(stage.Bands);
        Assert.Equal(5, bands.Bands.Count);
        Assert.All(bands.Bands, b => Assert.Equal(RowHeight, b.Height, 1e-9));
        Assert.All(bands.Angles, a => Assert.Equal(80, a, 1e-9));
        Assert.True(double.IsPositiveInfinity(bands.RevealedTo));

        for (var tick = 1; tick <= 14; tick++)
        {
            stage.At(first + tick * Tick);
            for (var row = 0; row < 5; row++)
                Assert.Equal(Math.Max(0, 80 - RowRates[row] * tick), stage.Bands!.Angles[row], 1e-9);
        }

        // The slowest row, 5.67° a tick, is at rest after 14.1 ticks.
        stage.At(first + 15 * Tick);
        Assert.Null(stage.Bands);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Pickers))]
    public void Accept_Sets_The_Value_In_The_Taps_Frame_And_It_Closes_In_Five_Bands_At_16_Ticks(string which)
    {
        using var stage = new Stage(Make(which));
        stage.OpenAtRest();
        if (stage.Picker is DatePicker)
            stage.InPresenter<DateTimePickerPanel>("PART_MonthSelector").SelectedValue = 8;
        else
            stage.InPresenter<DateTimePickerPanel>("PART_MinuteSelector").SelectedValue = 45;
        var changed = new List<double>();
        if (stage.Picker is DatePicker date)
            date.SelectedDateChanged += (_, _) => changed.Add(stage.Now);
        else
            ((TimePicker)stage.Picker).SelectedTimeChanged += (_, _) => changed.Add(stage.Now);

        var tapped = stage.Now;
        stage.Tap(stage.InPresenter<Button>("PART_AcceptButton"));
        Assert.Equal([tapped], changed);
        if (stage.Picker is DatePicker d)
            Assert.Equal(new DateTimeOffset(2014, 8, 15, 0, 0, 0, July15.Offset), d.SelectedDate);
        else
            Assert.Equal(new TimeSpan(12, 45, 0), ((TimePicker)stage.Picker).SelectedTime);
        Assert.Equal(which == "date" ? "8/15/2014" : "12:45 PM", Part<ContentPresenter>(stage.FlyoutButton, "PART_ContentPresenter").Content);

        var first = stage.FirstFrameAfter(tapped);
        stage.At(first + 4 * Tick);
        Assert.Equal(5, stage.Bands!.Bands.Count);
        Assert.Equal(-10.95 * 4, stage.Bands.Angles[0], 1e-9);
        Assert.Equal(-10.95 * 2, stage.Bands.Angles[1], 1e-9);
        Assert.Equal(0, stage.Bands.Angles[2], 1e-9);

        // Band 4 starts 8 ticks in and is gone 8.2 ticks later: closed on the 16th.
        stage.At(first + (ClosedTicks - 1) * Tick);
        Assert.True(stage.Popup.IsOpen);
        stage.At(first + ClosedTicks * Tick);
        Assert.False(stage.Popup.IsOpen);
        Assert.False(stage.StandIn.IsOpen);
    }

    [AvaloniaFact]
    public void Accept_With_Nothing_Changed_Changes_Nothing()
    {
        using var stage = new Stage(Date());
        stage.OpenAtRest();
        var changed = 0;
        ((DatePicker)stage.Picker).SelectedDateChanged += (_, _) => changed++;
        stage.Tap(stage.InPresenter<Button>("PART_AcceptButton"));
        Assert.Equal(0, changed);
        Assert.Equal(July15, ((DatePicker)stage.Picker).SelectedDate);
        Assert.True(stage.Host!.IsClosing);
    }

    [AvaloniaTheory]
    [InlineData("cancel")]
    [InlineData("back")]
    [InlineData("escape")]
    public void Cancel_Back_And_Escape_Pick_Nothing_And_Close_In_The_Bands_And_It_Opens_Again(string how)
    {
        using var stage = new Stage(Date());
        stage.OpenAtRest();
        stage.InPresenter<DateTimePickerPanel>("PART_MonthSelector").SelectedValue = 8;
        var called = stage.Now;
        switch (how)
        {
            case "cancel":
                stage.Tap(stage.InPresenter<Button>("PART_DismissButton"));
                break;
            case "back":
                stage.Back();
                break;
            default:
                stage.Scene.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                Dispatcher.UIThread.RunJobs();
                break;
        }

        Assert.Equal(July15, ((DatePicker)stage.Picker).SelectedDate);
        Assert.True(stage.Host!.IsClosing);
        var first = stage.FirstFrameAfter(called);
        stage.At(first + (ClosedTicks - 1) * Tick);
        Assert.True(stage.Popup.IsOpen);
        stage.At(first + ClosedTicks * Tick);
        Assert.False(stage.Popup.IsOpen);
        Assert.False(stage.StandIn.IsOpen);

        stage.OpenAtRest();
        Assert.True(stage.Popup.IsOpen);
        Assert.True(stage.StandIn.IsOpen);
    }

    [AvaloniaFact]
    public void Back_Is_Taken_While_It_Closes()
    {
        using var stage = new Stage(Date());
        stage.OpenAtRest();
        stage.Tap(stage.InPresenter<Button>("PART_AcceptButton"));
        var back = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Scene.Window.RaiseEvent(back);
        Assert.True(back.Handled);
        Assert.True(stage.Host!.IsClosing);
    }

    [AvaloniaFact]
    public void In_Landscape_The_Wheels_Take_The_Landscape_Margin()
    {
        using var stage = new Stage(Date(), width: 800, height: 450);
        stage.OpenAtRest();
        Assert.Equal(19.1667, stage.InWindow(stage.InPresenter<DateTimePickerPanel>("PART_MonthSelector")).Y, 2);
    }

    [AvaloniaFact]
    public void Its_Popup_Let_Go_Gives_The_Stand_In_Back_And_Closes_It()
    {
        using var stage = new Stage(Date());
        stage.OpenAtRest();
        var popup = stage.Popup;
        Assert.NotNull(stage.StandIn.PlacementTarget);

        PhoneDateTimePicker.SetShownIn(stage.StandIn, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(stage.Host);
        Assert.False(popup.IsOpen);
        Assert.False(stage.StandIn.IsSet(Popup.PlacementTargetProperty));
        Assert.False(popup.ShouldUseOverlayLayer);
    }

    [AvaloniaFact]
    public void Simple_Alone_Keeps_Avalonias_Own_Picker()
    {
        using var stage = new Stage(Date(), themes: [new Avalonia.Themes.Simple.SimpleTheme()]);
        Assert.DoesNotContain(stage.Picker.GetVisualDescendants().OfType<Popup>(), p => p.Name == "PickerPopup");
        Assert.Null(stage.Picker.GetVisualDescendants().OfType<Popup>().Select(PhoneDateTimePicker.HostOf).FirstOrDefault(h => h is not null));
    }
}
