using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ListBox and ListBoxItem as the phone has them: the values their default styles and the item's template set
/// (the SDK's <c>generic.xaml</c> and <c>themeresources.xaml</c>, which the 8.1 Update 1 runtime's <c>listbox.json</c>
/// and <c>listboxitem.json</c> match), and what the reference app read and drew of a
/// <c>ListBoxItem { Content = "list box item" }</c> and of a ListBox of "one", "two" and "three" with the second
/// selected, on the reference profile, run <c>20261118-2210-dev11</c> (device 11, 1080 × 1920 at 2.4, #211), whose PNGs
/// are in <c>ref/wp81/ListBox/</c> (<c>tools/refapp/README.md</c>, "ListBox and ListBoxItem"). Values only: the dumps stay
/// in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ListBoxReadings
{
    // The list's setters and the item's.
    public const double FontSize = 12;                                            // ControlContentThemeFontSize
    public static readonly Thickness BorderThickness = new(1.5);                    // ListBoxBorderThemeThickness
    public static readonly Thickness ItemPadding = new(6.5, 8);

    // The dump's bounds, in the list: LayoutRoot's ScrollViewer at 1.6667 (the 1.5 border, 3.6 raw, laid out at 4); the
    // three items 32.0833 tall (8 + 16.25 + 8, 77.4 raw, laid out at 77) one under the other; each one's content at
    // 6.6667, 7.9167 in the item (6.5 and 8, laid out at 16 and 19 raw), 16.25 tall, Segoe WP's 1.33 em line at 12.
    public const double Inset = 1.6667;
    public const double ItemHeight = 32.0833;
    public static readonly double[] ItemTops = [1.6667, 33.75, 65.8333];
    public static readonly Point ContentInItem = new(6.6667, 7.9167);
    public const double ContentHeight = 16.25;

    // An item on its own, 87.9167 tall in its 100-tall stage: its content centred (WinRT's VerticalContentAlignment).
    public const double StandaloneHeight = 87.9167;

    // The captures' colours, dark then light.
    public static Color ListBackground(bool dark) => Color.FromUInt32(dark ? 0xCCFFFFFFu : 0xBF000000u);   // ListBoxBackgroundThemeBrush, PhoneBaseMediumHighColor

    public static readonly Color ListForeground = Color.FromUInt32(0xFF000000);                            // ListBoxForegroundThemeBrush, both

    public static readonly Color ListBorder = Color.FromUInt32(0x00FFFFFF);                                // ListBoxBorderThemeBrush, Transparent

    public static Color ListDisabledBorder(bool dark) => Color.FromUInt32(dark ? 0x66FFFFFFu : 0x66000000u); // ListBoxDisabledForegroundThemeBrush, PhoneDisabledColor

    public static Color PressedBackground(bool dark) => ListBackground(dark);                              // ListBoxItemPressedBackgroundThemeBrush

    public static readonly Color PressedForeground = Color.FromUInt32(0xFF000000);                         // ListBoxItemPressedForegroundThemeBrush, both

    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);                                    // ListBoxItemSelectedBackgroundThemeBrush (Cobalt)

    public static readonly Color SelectedForeground = Color.FromUInt32(0xFFFFFFFF);                        // ListBoxItemSelectedForegroundThemeBrush, both

    public static Color DisabledForeground(bool dark) => Color.FromUInt32(dark ? 0x80FFFFFFu : 0x80000000u);          // ListBoxItemDisabledForegroundThemeBrush, PhoneBaseMediumMidColor

    public static Color SelectedDisabledBackground(bool dark) => Color.FromUInt32(dark ? 0x66FFFFFFu : 0x66000000u);  // ListBoxItemSelectedDisabledBackgroundThemeBrush, PhoneDisabledColor

    public static Color SelectedDisabledForeground(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);  // ListBoxItemSelectedDisabledForegroundThemeBrush, PhoneBaseLowColor
}
