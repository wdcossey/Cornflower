using Avalonia;
using Avalonia.Media;
using Cornflower.Controls;

namespace Cornflower.Tests.Controls;

/// <summary>
/// The 8.1 ListView and ListViewItem as the phone has them: the values their default styles and the item's template set
/// (the 8.1 Update 1 runtime's <c>listview.json</c>, <c>listviewitem.json</c> and <c>themeresources.json</c>, which equal
/// the SDK's design files), what the reference app read and drew of a <c>ListViewItem { Content = "list view item" }</c>
/// (unselected and selected) and of a ListView of "one", "two" and "three" with the second selected, on the reference
/// profile, run <c>20261108-1443-dev11</c> (device 11, 1080 × 1920 at 2.4), whose PNGs are in
/// <c>ref/wp81/ListView/</c>, and the press probes of runs <c>20261115-1941-dev11</c> and <c>20261115-1948-dev11</c>
/// (<c>tools/refapp/README.md</c>, "ListView: an item's press"). Values only: the JSON and the logs stay in
/// <c>R:\cornflower-reference</c>.
/// </summary>
internal static class ListViewReadings
{
    // The stage (Suite.Place in tools/refapp): the page's background, a slot 19 in and 12 down. 450 × 100 for an item,
    // 450 × 260 for the list.
    public const double StageWidth = 450;
    public const double ItemStageHeight = 100;
    public const double ListStageHeight = 260;
    public static readonly Thickness Slot = new(19, 12, 19, 0);

    // The dumps' elementInStage: the item [19.1667, 12.0833, 412.0833, 87.9167] (it stretches in the slot); the list
    // [19.1667, 12.0833, 412.0833, 247.9167]. The slot's width, 412 view pixels, is 988.8 raw: the phone's 989 (412.0833),
    // Avalonia's 988 once the 19 on each side are 46 raw (ours 411.6667; ADR 0003).
    public static readonly Point InStage = new(19.1667, 12.0833);
    public const double PhoneWidth = 412.0833;
    public const double OurWidth = 411.6667;

    // The list's items, each 39.1667 tall, one under the other from the top: SelectedBorder's 2.5 + 34 + 2.5, 93.6 raw
    // pixels, which both round to 94. The dump's three ListViewItems are at 0, 39.1667 and 78.3333.
    public const double ItemHeight = 39.1667;

    // The dump's contentPresenter, in the item: [0, 0, 20.4167, 16.25] for "one", its TextBlock 15.96 tall: Segoe WP's
    // line, 1.33 em at 12.
    public const double ContentHeight = 16.25;

    // The dump's CheckboxTiltContainer, in the item: [0, 9.5833, 25.4167, 25.4167]; the Rectangle is 25.5 in the
    // template, 61.2 raw: the phone's 61, Avalonia's 62 (ours 25.8333; ADR 0003).
    public static readonly Point BoxAt = new(0, 9.5833);
    public const double PhoneBox = 25.4167;
    public const double OurBox = 25.8333;

    // The dump's CheckGlyph in ListMultiSelect, in the item: [3.75, 13.75, 18.3333, 17.0833], so 3.75, 4.1667 in the
    // box. At rest the CheckboxContainer is 40.5 to the left: [-40.5, 9.5833].
    public static readonly Point GlyphInBox = new(3.75, 4.1667);
    public static readonly Size Glyph = new(18.3333, 17.0833);

    // The dump's SelectedCheckMark, in the item: [375.4167, 2.5, 34.1667, 34.1667], at the right inside the 2.5 border;
    // ours 375 across, as the item is one raw pixel narrower.
    public const double PhoneCheckMarkLeft = 375.4167;
    public const double OurCheckMarkLeft = 375;
    public const double CheckMarkTop = 2.5;
    public const double CheckMark = 34.1667;

    // ListMultiSelect's two offsets (ListViewItemContentOffsetX and ListViewItemContentTranslateX) and its transitions'
    // GeneratedDuration, both ways.
    public const double ContentOffsetX = -40.5;
    public const double ContentTranslateX = 40.5;
    public static readonly TimeSpan MultiSelectDuration = TimeSpan.FromSeconds(0.15);

    // The styles' setters (listview.json, listviewitem.json), with the values themeresources.json gives their keys.
    public const double FontSize = 12;                                           // ControlContentThemeFontSize
    public static readonly Thickness ItemMargin = new(0);                        // ListViewItemMargin
    public static readonly Thickness CheckBoxMargin = new(0, 9.5, 0, 0);         // ListViewItemMultiselectCheckBoxMargin
    public const double BoxStroke = 2.5;                                         // CheckBoxBorderThemeThickness
    public static readonly Thickness SelectedBorderThickness = new(2.5);         // GridViewItemMultiselectBorderThickness
    public const double DisabledOpacity = 0.4;                                   // ListViewItemDisabledThemeOpacity, Update 1's (RTM's 0.55)

    // The brushes, per variant, from the dumps (base, CheckboxPressed, Disabled).
    public static readonly Color Transparent = Color.FromUInt32(0x00FFFFFF);     // CheckBoxBackgroundThemeBrush
    public static readonly Color Accent = Color.FromUInt32(0xFF3E65FF);          // CheckBoxPressedBackgroundThemeBrush, ListViewItemSelectedBackgroundThemeBrush
    public static readonly Color OverAccent = Color.FromUInt32(0xFFFFFFFF);      // CheckBoxPressedForegroundThemeBrush, both variants

    public static Color Foreground(bool dark) => Color.FromUInt32(dark ? 0xFFFFFFFFu : 0xFF000000u);   // PhoneForegroundColor

    public static Color Disabled(bool dark) => Color.FromUInt32(dark ? 0x73FFFFFFu : 0x40000000u);     // PhoneBaseLowColor

    public static Color Page(bool dark) => Color.FromUInt32(dark ? 0xFF000000u : 0xFFFFFFFFu);         // ApplicationPageBackgroundThemeBrush

    // The PNGs (ListMultiSelect), in raw pixels: the box's border is 6 pixels wide (2.5 at 2.4), its left side columns
    // 46-51 and its top rows 52-57, every pixel the border's colour, its right side 101-106. Ours is laid out 62 wide but
    // drawn at the Rectangle's own 25.5, 61.2 raw: its right side is whole in 102-106, with a fifth of column 107 and
    // four fifths of 101.
    public static readonly int[] LeftColumns = [46, 47, 48, 49, 50, 51];
    public static readonly int[] TopRows = [52, 53, 54, 55, 56, 57];
    public static readonly int[] RightColumns = [102, 103, 104, 105, 106];

    // The press probes: a ListView of TextBlocks at 19.1667, 80 in the 450 × 800 view, 412.0833 wide, its items 39.1667
    // tall from 80 down. Each press, in the view, and the tilt it held (TiltContainer's PlaneProjection), from the log.
    public static readonly Thickness ProbeListMargin = new(19, 80, 19, 0);

    public static readonly (string Name, Point Press, int Item, TiltState Held)[] Presses =
    [
        ("item 1 centre", new(225, 138.361), 1, new(-2.127, 0.009, -21.894)),
        ("item 2 centre", new(225, 178.361), 2, new(-1.523, 0.009, -22.771)),
        ("item 3 centre", new(225, 216.656), 3, new(-1.591, 0.009, -22.673)),
        ("item 2 at 5% across", new(39.971, 178.361), 2, new(-2.630, 8.087, -9.413)),
        ("item 2 at 25% across", new(122.493, 178.361), 2, new(-1.646, 4.398, -16.209)),
        ("item 2 at 75% across", new(328.301, 178.361), 2, new(-1.644, -4.415, -16.187)),
        ("item 2 at 95% across", new(410.824, 178.361), 2, new(-2.627, -8.104, -9.393)),
        ("item 2 at 10% down", new(225, 162.492), 2, new(-8.110, 0.009, -13.191)),
        ("item 2 at 90% down", new(225, 193.311), 2, new(5.572, 0.009, -16.882)),
        ("item 4 centre", new(225, 256.656), 4, new(-0.956, 0.009, -23.597)),
    ];

    // The most the tilt's model (the Button's rule, TiltModel) is off these readings, on our one raw pixel narrower item:
    // 0.09° (item 2 at 5% across, RotationX -2.549 against -2.630) and 0.11 of depth (-9.519 against -9.413).
    public const double AngleTolerance = 0.1;
    public const double DepthTolerance = 0.15;
}
