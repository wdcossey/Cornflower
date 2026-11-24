using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornflower.Controls;
using static Cornflower.Tests.Controls.PlainParts;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The plain dates (ADR 0007, #240): Calendar, CalendarItem, CalendarDayButton, CalendarButton and CalendarDatePicker.
/// No box; the header's three buttons in Wp81PlainGlyphButtonTheme, with the tilt; days and months in the body's type,
/// at the phone's touch height across the width, tilted; the accent only for a selected or pressed day, today an
/// outline; the picker Wp81's TextBox with the glyph button in it. Simple's is a bordered box with an accent header band
/// and small cells, so these fail without the theme.
/// </summary>
public class CalendarTests
{
    private static readonly DateTime Shown = new(2026, 11, 1);

    private static ThemeVariant Variant(string variant) => variant == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static List<CalendarDayButton> Days(Calendar calendar) =>
        calendar.GetVisualDescendants().OfType<CalendarDayButton>().ToList();

    private static CalendarDayButton Day(Calendar calendar, DateTime date) =>
        Days(calendar).First(day => day.DataContext is DateTime d && d == date.Date);

    // In the TextBox's InnerRightContent, so its templated parent is the TextBox.
    private static Button PickerButton(CalendarDatePicker picker) =>
        picker.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_Button");

    private static CalendarItem Item(Calendar calendar) => Part<CalendarItem>(calendar, "PART_CalendarItem");

    private static IBrush? Fill(Control button) => Part<Border>(button, "Border").Background;

    private static IBrush? Text(Control button) => Part<ContentPresenter>(button, "PART_ContentPresenter").Foreground;

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // A Calendar on a 411 page, inside a page's 19 margins.
    private static (Calendar Calendar, Scene Scene) OnAPage(Calendar calendar, ThemeVariant? variant = null) =>
        (calendar, new Scene(new StackPanel { Margin = new Thickness(19, 0), Children = { calendar } }, variant, width: 411));

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Its_Header_Is_Three_Glyph_Buttons_With_The_Tilt_And_No_Box(string variant)
    {
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = Shown }, Variant(variant));
        using var _ = scene;
        var item = Item(calendar);
        var glyph = Resource<ControlTheme>(calendar, "Wp81PlainGlyphButtonTheme");

        Assert.Null(Part<Border>(item, "PART_Border").Background);
        Assert.Null(Part<Border>(item, "PART_Border").BorderBrush);
        Assert.DoesNotContain(item.GetVisualDescendants(), v => v is Avalonia.Controls.Shapes.Rectangle);
        Assert.Equal(373, calendar.Bounds.Width, 0);

        foreach (var (name, kind) in new[] { ("PART_PreviousButton", LucideIconKind.ChevronLeft), ("PART_NextButton", LucideIconKind.ChevronRight) })
        {
            var button = Part<Button>(item, name);
            Assert.Same(glyph, button.Theme);
            Assert.True(Tilt.GetIsEnabled(button));
            Assert.Equal(57.5, button.Bounds.Width);
            Assert.Equal(57.5, button.Bounds.Height);
            var arrow = button.GetVisualDescendants().OfType<LucideIcon>().Single();
            Assert.Equal(kind, arrow.Kind);
            Assert.Same(Resource<IBrush>(calendar, "PhoneForegroundBrush"), arrow.Foreground);
        }

        var header = Part<Button>(item, "PART_HeaderButton");
        Assert.Same(glyph, header.Theme);
        Assert.True(Tilt.GetIsEnabled(header));
        Assert.Equal(23.73, header.FontSize);
        Assert.Equal(Resource<FontFamily>(calendar, "PhoneFontFamilySemiLight"), header.FontFamily);
        Assert.Same(Resource<IBrush>(calendar, "PhoneMidBrush"), header.Foreground);
        Assert.Equal(57.5, header.Bounds.Height);
        Assert.Equal(Colors.Transparent, ColorOf(Fill(header)));
    }

    // At 373 wide (a 411 screen less a page's margins), seven days share the width: each 53.3 by the phone's 57.5, in
    // the body's type, with the tilt, transparent at rest. The days' names are the small print.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void A_Day_Is_A_Seventh_Of_The_Width_And_Finger_Tall(string variant)
    {
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = Shown, IsTodayHighlighted = false }, Variant(variant));
        using var _ = scene;

        var days = Days(calendar);
        Assert.Equal(42, days.Count);
        Assert.All(days, day =>
        {
            Assert.InRange(day.Bounds.Width, 52.5, 53.75);     // 373 / 7, on the 2.4 pixel grid
            Assert.Equal(57.5, day.Bounds.Height);
            Assert.True(Tilt.GetIsEnabled(day));
            Assert.Equal(18.14, day.FontSize);
            Assert.Equal(Colors.Transparent, ColorOf(Fill(day)));
        });
        var day = Day(calendar, new DateTime(2026, 11, 10));
        Assert.Equal(Resource<FontFamily>(day, "PhoneFontFamilyNormal"), day.FontFamily);
        Assert.Same(Resource<IBrush>(day, "PhoneForegroundBrush"), day.Foreground);

        var names = Part<Grid>(Item(calendar), "PART_MonthView").Children.OfType<TextBlock>().ToList();
        Assert.Equal(7, names.Count);
        Assert.All(names, name =>
        {
            Assert.Same(Resource<ControlTheme>(calendar, "ControlContextualInfoTextBlockStyle"), name.Theme);
            Assert.Same(Resource<IBrush>(calendar, "PhoneMidBrush"), name.Foreground);
        });
        // A day of another month is mid.
        Assert.Same(Resource<IBrush>(calendar, "PhoneMidBrush"), Text(Day(calendar, new DateTime(2026, 10, 31))));
    }

    // The accent is for the selected day only, at rest: a filled cell with the text over it. Today is an outline in the
    // foreground, with no accent.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void The_Selected_Day_Is_The_Accent_And_Today_An_Outline(string variant)
    {
        var today = DateTime.Today;
        var other = today.Day == 15 ? today.AddDays(1) : new DateTime(today.Year, today.Month, 15);
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = today, SelectedDate = other }, Variant(variant));
        using var _ = scene;

        var selected = Day(calendar, other);
        Assert.Same(Resource<IBrush>(calendar, "PhoneAccentBrush"), Fill(selected));
        Assert.Same(Resource<IBrush>(calendar, "PhoneButtonBasePressedForegroundBrush"), Text(selected));

        var now = Day(calendar, today);
        Assert.Contains(":today", now.Classes);
        Assert.Equal(Colors.Transparent, ColorOf(Fill(now)));
        Assert.Same(Resource<IBrush>(calendar, "PhoneForegroundBrush"), now.Foreground);
        var mark = Part<Border>(now, "TodayMark");
        Assert.True(mark.IsVisible);
        Assert.Equal(new Thickness(2.5), mark.BorderThickness);
        Assert.Same(Resource<IBrush>(calendar, "PhoneForegroundBrush"), mark.BorderBrush);
        Assert.All(Days(calendar).Where(d => d != now), d => Assert.False(Part<Border>(d, "TodayMark").IsVisible));
        Assert.All(Days(calendar).Where(d => d != selected), d => Assert.NotSame(Resource<IBrush>(calendar, "PhoneAccentBrush"), Fill(d)));
    }

    // A press tilts the day and fills it with the accent; the release selects it.
    [AvaloniaFact]
    public void A_Press_Tilts_A_Day_And_Selects_It()
    {
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = Shown });
        using var _ = scene;
        var day = Day(calendar, new DateTime(2026, 11, 12));

        var at = Press(scene, day);
        Assert.NotEqual(TiltState.Rest, Tilt.Current(day));
        Assert.Same(Resource<IBrush>(calendar, "PhoneAccentBrush"), Fill(day));
        Release(scene, at);

        Assert.Equal(new DateTime(2026, 11, 12), calendar.SelectedDate);
    }

    // The press is the accent on its own: with nothing to select (SelectionMode None), a pressed day still fills.
    [AvaloniaFact]
    public void A_Press_Is_The_Accent_Where_Nothing_Is_Selected()
    {
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = Shown, SelectionMode = CalendarSelectionMode.None });
        using var _ = scene;
        var day = Day(calendar, new DateTime(2026, 11, 12));

        var at = Press(scene, day);
        Assert.Null(calendar.SelectedDate);
        Assert.Same(Resource<IBrush>(calendar, "PhoneAccentBrush"), Fill(day));
        Assert.Same(Resource<IBrush>(calendar, "PhoneButtonBasePressedForegroundBrush"), Text(day));
        Release(scene, at);
        Assert.Equal(Colors.Transparent, ColorOf(Fill(day)));
    }

    // The year's months: four across, the phone's height, tilted, the month shown in the accent.
    [AvaloniaFact]
    public void The_Year_Is_Four_Months_Across_With_The_Shown_One_In_The_Accent()
    {
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = Shown, DisplayMode = CalendarMode.Year });
        using var _ = scene;

        var months = calendar.GetVisualDescendants().OfType<CalendarButton>().ToList();
        Assert.Equal(12, months.Count);
        Assert.All(months, month =>
        {
            Assert.InRange(month.Bounds.Width, 92.75, 93.75);  // 373 / 4, on the 2.4 pixel grid
            Assert.Equal(57.5, month.Bounds.Height);
            Assert.True(Tilt.GetIsEnabled(month));
            Assert.Equal(18.14, month.FontSize);
        });
        var november = months.Single(m => m.Classes.Contains(":selected"));
        Assert.Same(Resource<IBrush>(calendar, "PhoneAccentBrush"), Fill(november));
        Assert.Same(Resource<IBrush>(calendar, "PhoneButtonBasePressedForegroundBrush"), Text(november));
        Assert.All(months.Where(m => m != november), m => Assert.Equal(Colors.Transparent, ColorOf(Fill(m))));
    }

    // Disabled, the days, their names and the header are PhoneDisabledBrush; a blacked-out day is too.
    [AvaloniaFact]
    public void Disabled_And_Blacked_Out_Days_Are_PhoneDisabledBrush()
    {
        var calendar = new Calendar { DisplayDate = Shown, IsEnabled = false };
        var (_, scene) = OnAPage(calendar);
        using var s = scene;
        var disabled = Resource<IBrush>(calendar, "PhoneDisabledBrush");

        Assert.Same(disabled, Text(Day(calendar, new DateTime(2026, 11, 10))));
        Assert.All(Part<Grid>(Item(calendar), "PART_MonthView").Children.OfType<TextBlock>(), name => Assert.Same(disabled, name.Foreground));
        Assert.Same(disabled, Text(Part<Button>(Item(calendar), "PART_HeaderButton")));

        var open = new Calendar { DisplayDate = Shown };
        open.BlackoutDates.Add(new CalendarDateRange(new DateTime(2026, 11, 20)));
        var (_, other) = OnAPage(open);
        using var o = other;
        Assert.Same(Resource<IBrush>(open, "PhoneDisabledBrush"), Text(Day(open, new DateTime(2026, 11, 20))));
    }

    // Disabled, nothing is in the accent or the foreground: a selected day and the shown month aren't filled, and today's
    // outline is PhoneDisabledBrush.
    [AvaloniaFact]
    public void Disabled_A_Selection_Isnt_Filled_And_Today_Is_Dimmed()
    {
        var today = DateTime.Today;
        var other = today.Day == 15 ? today.AddDays(1) : new DateTime(today.Year, today.Month, 15);
        var calendar = new Calendar { DisplayDate = today, SelectedDate = other, IsEnabled = false };
        var year = new Calendar { DisplayDate = Shown, DisplayMode = CalendarMode.Year, IsEnabled = false };
        using var scene = new Scene(new StackPanel { Width = 373, Children = { calendar, year } });
        var disabled = Resource<IBrush>(calendar, "PhoneDisabledBrush");

        Assert.Equal(Colors.Transparent, ColorOf(Fill(Day(calendar, other))));
        Assert.Same(disabled, Text(Day(calendar, other)));
        Assert.Same(disabled, Part<Border>(Day(calendar, today), "TodayMark").BorderBrush);
        var november = year.GetVisualDescendants().OfType<CalendarButton>().Single(m => m.Classes.Contains(":selected"));
        Assert.Equal(Colors.Transparent, ColorOf(Fill(november)));
        Assert.Same(disabled, Text(november));
    }

    // The days aren't focusable, as in Simple's, so a press leaves the focus off them. Avalonia's Calendar isn't focusable
    // by default either; one an app makes focusable takes the focus from a press, and the arrow keys move the day.
    [AvaloniaFact]
    public void The_Days_Take_No_Focus_And_A_Focusable_Calendar_Takes_The_Arrows()
    {
        var (calendar, scene) = OnAPage(new Calendar { DisplayDate = Shown, Focusable = true });
        using var _ = scene;
        var day = Day(calendar, new DateTime(2026, 11, 12));

        Assert.False(day.Focusable);
        Release(scene, Press(scene, day));
        Assert.False(day.IsFocused);
        Assert.True(calendar.IsFocused);

        scene.Window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new DateTime(2026, 11, 13), calendar.SelectedDate);
    }

    // In a host with an app's own font, a day keeps the phone's family and size.
    [AvaloniaFact]
    public void A_Day_Keeps_The_Phones_Type_In_An_Apps_Font()
    {
        var calendar = new Calendar { DisplayDate = Shown };
        var host = new StackPanel { Width = 373, Children = { calendar } };
        host.SetValue(TextElement.FontFamilyProperty, new FontFamily("Courier New"));
        host.SetValue(TextElement.FontSizeProperty, 9.0);
        using var scene = new Scene(host);

        var day = Day(calendar, new DateTime(2026, 11, 10));
        Assert.Equal(Resource<FontFamily>(day, "PhoneFontFamilyNormal"), day.FontFamily);
        Assert.Equal(18.14, day.FontSize);
        Assert.Equal(23.73, Part<Button>(Item(calendar), "PART_HeaderButton").FontSize);
    }

    // The picker is Wp81's TextBox (its box, no brushes passed), at a TextBox's margin, with the glyph button in the box.
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void The_Picker_Is_Wp81s_TextBox_With_The_Glyph_Button_In_It(string variant)
    {
        var picker = new CalendarDatePicker { SelectedDate = new DateTime(2026, 11, 24), Background = Brushes.Red };
        using var scene = new Scene(new StackPanel { Width = 373, Children = { picker } }, Variant(variant));

        Assert.Equal(Resource<Thickness>(picker, "TextControlMarginThemeThickness"), picker.Margin);
        var box = Part<TextBox>(picker, "PART_TextBox");
        Assert.Null(box.Theme);
        Assert.Equal(new Thickness(0), box.Margin);
        Assert.Contains(box.GetVisualDescendants(), v => v is Control { Name: "BorderElement" });
        Assert.NotSame(Brushes.Red, box.Background);

        var button = PickerButton(picker);
        Assert.Same(Resource<ControlTheme>(picker, "Wp81PlainGlyphButtonTheme"), button.Theme);
        Assert.True(Tilt.GetIsEnabled(button));
        Assert.Equal(LucideIconKind.Calendar, button.GetVisualDescendants().OfType<LucideIcon>().Single().Kind);
        Assert.Same(box.InnerRightContent, button);
        Assert.True(box.IsVisualAncestorOf(button));
    }

    // A tap on the button opens the plain Calendar below the picker, on the flyout's surface, at least as wide as it.
    [AvaloniaFact]
    public void A_Tap_Opens_The_Plain_Calendar_On_The_Flyouts_Surface()
    {
        var picker = new CalendarDatePicker { SelectedDate = new DateTime(2026, 11, 24) };
        using var scene = new Scene(new StackPanel { Width = 373, Children = { picker } });

        Release(scene, Press(scene, PickerButton(picker)));

        Assert.True(picker.IsDropDownOpen);
        var popup = Part<Popup>(picker, "PART_Popup");
        Assert.True(popup.IsOpen);
        Assert.Equal(PlacementMode.Bottom, popup.Placement);
        Assert.True(popup.ShouldUseOverlayLayer);
        var surface = Assert.IsType<Border>(popup.Child);
        Assert.Same(Resource<IBrush>(picker, "FlyoutBackgroundThemeBrush"), surface.Background);
        Assert.Equal(picker.Bounds.Width, surface.MinWidth);
        var calendar = Assert.IsType<Calendar>(surface.Child);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(Resource<ControlTheme>(picker, "Wp81PlainGlyphButtonTheme"), Part<Button>(Item(calendar), "PART_HeaderButton").Theme);
        Assert.Same(Resource<IBrush>(picker, "PhoneAccentBrush"), Fill(Day(calendar, new DateTime(2026, 11, 24))));
    }

    // A selection at load or from code never moves the page (#220, #251, #252): a Calendar and a picker with a selected
    // date low down on a page that lays out short at first leave it at the top.
    [AvaloniaFact]
    public void A_Selection_Doesnt_Scroll_The_Page()
    {
        var calendar = new Calendar { DisplayDate = Shown, SelectedDate = new DateTime(2026, 11, 20) };
        var picker = new CalendarDatePicker { SelectedDate = new DateTime(2026, 11, 20) };
        var page = new ScrollViewer { Height = 200, Content = new StackPanel { Children = { new Border { Height = 600 }, calendar, picker } } };
        using var scene = new Scene(new StackPanel { Children = { page } });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, page.Offset.Y);

        calendar.SelectedDate = new DateTime(2026, 11, 27);
        calendar.DisplayDate = new DateTime(2026, 12, 1);
        picker.SelectedDate = new DateTime(2026, 12, 3);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, page.Offset.Y);
    }
}
