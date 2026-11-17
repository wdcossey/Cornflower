using Avalonia;
using Avalonia.Media;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ComboBox as the phone has it: its default style and template (the 8.1 Update 1 runtime's
/// <c>combobox.json</c>, <c>comboboxitem.json</c> and <c>themeresources.json</c>), what the reference app read and drew of
/// a <c>ComboBox { Header = "combo box" }</c> with "one", "two", "three", "one" selected, closed, at rest and in each state
/// (run <c>20261108-1705-dev11</c>), opened with 3 to 10 items "item 1" to "item N", "item 1" selected (run
/// <c>20261109-2039-dev11</c>), and the inline motion (#149, run <c>20261116-1835-dev11</c>); device 11, 2.4. The PNGs are
/// in <c>ref/wp81/ComboBox/</c>. Values only: the JSON stays in <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ComboBoxReadings
{
    // The stage (Suite.Place): 450 wide, a slot 19 in and 12 down; the control stretched across it, 412.08 wide.
    public const double StageWidth = 450;
    public const double ClosedStageHeight = 140;
    public const double OpenStageHeight = 400;
    public static readonly Thickness Slot = new(19, 12, 19, 0);
    public const double ControlWidth = 412.0833;

    // The dump's HeaderContentPresenter, in the control: [0, 0, 80, 21.6667], margin 0,0,0,-4.5.
    public const double HeaderHeight = 21.6667;

    // The dump's Background (the box), in the control: [0, 26.6667, 412.0833, 38.3333]: the header less 4.5, then
    // PhoneTouchTargetOverhang's 9.5; 2.5 of border round a 33.33 host (ComboBoxItemMinHeightThemeSize 33.5, rounded).
    // The same in the Full form (the FlyoutButton's Border).
    public static readonly Rect BoxBounds = new(0, 26.6667, 412.0833, 38.3333);

    // The selected item's text, closed: the ContentPresenter at 9.1667, 29.8 in the control (the item 6.5 in, its 8.5
    // top padding lifted away, 0.8 down), 27.0833 tall. Full: 9.1667, 30 (6.5 in, 0.8 down, its own line 32.5 tall).
    public const double TextLeft = 9.1667;
    public const double InlineTextTop = 29.8;
    public const double FullTextTop = 30;
    public const double ItemTextHeight = 27.0833;

    // The inline placeholder (PlaceholderTextContentPresenter): 9, 29.9667 (ComboBoxPlaceholderTextThemeMargin 6.5,0.8).
    public const double PlaceholderLeft = 9;
    public const double PlaceholderTop = 29.9667;

    // An item: 51.6667 tall (Padding 6.5,8.5,0,16 round 27.08 of text), the step of TemplateSettings; the box open is the
    // items' height, 155 for 3 and 258.33 for 5, plus its border: 160 and 263.33.
    public const double ItemStep = 51.6667;
    public static double OpenBoxHeight(int items) => items * ItemStep + 5;

    // Ours is 51.25 at 2.4 (123 raw pixels to the phone's 124): Avalonia rounds the 8.5 and the 16 of padding to the
    // pixel each (20 and 38), the phone rounds the item. A known gap, at most this much an item.
    public const double StepShort = 0.4167;

    // The inline motion: the box shows open on the fifth frame, 78.125 ms after IsDropDownOpen, and closed on the first.
    public const double Tick = 15.625;
    public const double OpenAfter = 5 * Tick;

    // The colours, from the dumps.
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);           // pressed fill, highlighted border, selected item
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);
    public static readonly Color OpenBackground = Color.FromUInt32(0xFFFFFFFF);   // ComboBoxHighlightedBackgroundThemeBrush, both
    public static readonly Color OpenForeground = Color.FromUInt32(0xFF000000);   // ComboBoxHighlightedForegroundThemeBrush, both
    public static readonly Color OverAccent = Color.FromUInt32(0xFFFFFFFF);       // ComboBoxPressedForegroundThemeBrush

    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);   // box, border, text

    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);     // PhoneBaseLowColor

    public static Color Header(bool dark) => Color.FromUInt32(dark ? 0x99FFFFFFu : 0x73000000u);       // PhoneMidBrush

    // The Full form's placeholder: ComboBoxPlaceholderTextBlockStyle, the box's foreground at 0.6 (dark) or 0.45 (light).
    public static double FullPlaceholderOpacity(bool dark) => dark ? 0.6 : 0.45;
}
