using Avalonia;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 AutoSuggestBox as the phone has it: the values its default style sets (the 8.1 Update 1 runtime's
/// <c>autosuggestbox.json</c>, which equals the SDK's design files), and what #150 read of it on the reference profile
/// (device 11, 1080 × 1920 at 2.4; PR #172, <c>tools/refapp/README.md</c>, "AutoSuggestBox" and "How each popup is
/// dismissed, and the AutoSuggestBox's events"): the screen grabs of run <c>20261116-1944-dev11</c>, in
/// <c>ref/wp81/AutoSuggestBox/</c>, and the events of run <c>20261116-1951-dev11</c>. Values only: the JSON stays in
/// <c>R:\cornflower-reference</c>.
/// </summary>
internal static class AutoSuggestBoxReadings
{
    // The reference app's specimen (tools/refapp/app/Popups.cs, Suggest): new AutoSuggestBox { Header = "auto suggest
    // box", PlaceholderText = "type a fruit" }, on the popup stage, in a slot 19 in, 12 down and 19 from the right; its
    // suggestions the first n of twelve fruit names.
    public const string Header = "auto suggest box";
    public const string Placeholder = "type a fruit";
    public static readonly Thickness Slot = new(19, 12, 19, 0);
    public static readonly string[] Fruits =
        ["apple", "apricot", "avocado", "banana", "blackberry", "blueberry", "cherry", "grape", "lemon", "mango", "orange", "peach"];

    // The style's setters.
    public static readonly Thickness Margin = new(0, 9.5, 0, 9.5);      // TextControlMarginThemeThickness
    public static readonly Thickness ItemMargin = new(6.5, 9.5, 0, 0);  // AutoSuggestListViewItemMargin
    public const double ItemFontSize = 20.26;                           // ContentControlFontSize

    // At rest (dark__AutoSuggestBox__rest): the box's border rows 116 to 209, columns 46 to 1034: 48.33 to 87.5.
    public const double RestBoxTop = 48.3333;
    public const double RestBoxBottom = 87.5;
    public const double BoxLeft = 19.1667;

    // Focused, the phone moves the page up until the control's top (its header's) is at the window's top: the box from
    // 26.67 to 65.83, 21.67 up. The list's top is the box's bottom border (63.33, the two borders on each other).
    public const double FocusedBoxTop = 26.6667;
    public const double FocusedBoxBottom = 65.8333;
    public const double ListTop = 63.3333;
    public const double ListBorder = 2.5;                               // TextControlBorderThemeThickness

    // The list's rows: 39.17 tall, 48.75 apart, the first 12.08 below the list's top. One suggestion makes it 53.75 tall,
    // three 151.25; twelve run down to 2.5 above the keyboard's top, 387.5 tall.
    public const double RowHeight = 39.1667;
    public const double RowPitch = 48.75;
    public const double FirstRow = 12.0833;
    public const double OneTall = 53.75;
    public const double ThreeTall = 151.25;
    public const double TwelveTall = 387.5;
    public const double KeyboardGap = 2.5;

    // The keyboard: 346.67 tall from 453.33 (the screen's bottom 832 raw pixels).
    public static readonly Rect Keyboard = new(0, 453.3333, 450, 346.6667);

    // The grabs' raw rows and columns, focused, with the list open (list-1, list-3, list-12), dark and light alike: the
    // box's top border rows 64 to 69, its inside from 70, the borders on each other rows 152 to 157, the list's inside
    // from 158; its bottom border 275 to 280 for one, 509 to 514 for three, 1076 to 1081 for twelve; the sides' border
    // columns 46 to 51 and 1029 to 1034.
    public const int BoxTopRow = 64;
    public const int SharedBorderRow = 152;
    public const int ListInsideRow = 158;
    public const int ThreeBottomBorderRow = 509;
    public const int LeftBorderColumn = 46;
    public const int BorderPixels = 6;

    // The colours, the same in both themes but the page's: the accent border (Cobalt), the white inside, the black text.
    public const string Accent = "#FF3E65FF";                           // PhoneAccentBrush
    public const string White = "#FFFFFFFF";                            // AutoSuggestBackgroundThemeBrush
    public const string Black = "#FF000000";                            // TextBoxForegroundThemeBrush

    // The events (run 20261116-1951-dev11): a key's TextChanged (UserInput) 150 to 177 ms after its KeyDown, the app's
    // cue. The theme waits the least of them.
    public const double CueAfterKey = 150;
}
