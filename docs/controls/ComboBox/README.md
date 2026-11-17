# ComboBox and ComboBoxItem

Status: done but for the real-window run · [#155](https://github.com/wdcossey/CornflowerUI/issues/155) · device run passed 2026-11-17 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page combobox` on `main` at `9a24f7f`, 17:40 to 17:44, cold starts 747 to 813 ms over the run's pages: the page opened in 355 ms; the box opened by a tap inline and full screen, each drawn right) · owed: the real-window run

The Windows Phone 8.1 ComboBox on Avalonia's own `ComboBox` and `ComboBoxItem`, themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/ComboBox.axaml`. Five items or fewer open inline, the box growing in place; six or
more open the full-screen `ListPickerFlyout`. By the count, not the room on screen (#25). [PLAN.md](PLAN.md) has every
value and its source and the ticked checklists; [COMPARISON.md](COMPARISON.md) has the emulator's captures beside ours.

## Usage

```xml
<ComboBox cf:ControlHeader.Header="size" SelectedIndex="2">
  <x:String>small</x:String>
  <x:String>medium</x:String>
  <x:String>large</x:String>
</ComboBox>
```

Avalonia's API stands: `ItemsSource`, `ItemTemplate`, `DisplayMemberBinding`, `SelectedIndex`, `SelectedItem`,
`IsDropDownOpen`, `PlaceholderText`, `SelectionChanged`, `DropDownOpened`, `DropDownClosed`. The header is
`cf:ControlHeader.Header` and `HeaderTemplate`, as on Slider and TextBox.

## What it does

- **Closed**: the header in `HeaderContentPresenterStyle`, then the box (2.5 border, 38.33 tall, inside the 9.5 touch
  overhang) showing the selected item; with none, "choose an item" (the phone's default `PlaceholderText`, set by the
  theme) in `PhoneMidBrush`.
- **A press** fills the box with the accent and tilts it (the Button's tilt); the release opens it, the tilt gone at once.
- **Inline (5 items or fewer)**: the box shows open 78 ms (five 64 Hz ticks) after `IsDropDownOpen`, in one frame, as
  tall as its items (51.67 each on the phone): white, the accent border, black text, the selected item in the accent.
  A tap on an item selects it and closes the box at once. A tap anywhere else goes on to what it was on, then closes it (on the release, so a Button below still clicks);
  Back closes it before anything else sees Back.
- **Full screen (6 or more)**: the box shows the selected item. Opening it opens a `ListPickerFlyout` titled with the
  placeholder in capitals ("CHOOSE AN ITEM"), the selected item in the accent; a tap picks it (`SelectedIndex`) and the
  picker closes, then `IsDropDownOpen` goes false (of two equal items, the one tapped). Back closes the picker with the
  selection kept. Opened again while closing, it opens once the close has run.
- **The count crossing 6 while open**, or the items changing under the open picker, closes it; so does leaving the window.
- **Disabled**: header, border and text in `PhoneBaseLowColor`. A disabled item is dimmed to 0.4.

## How it is built

- **The theme** holds the items in the box: `PhoneComboBoxItemsHost` (internal) lays out the phone's `TemplateSettings`
  (closed: 33.33 tall, the items lifted so the selected one shows; open: the items' height).
- **`PhoneComboBox`** (internal, turned on by the theme's setter, as `PhoneToggleSwitch` is) watches the count and
  `IsDropDownOpen`: it shows the inline box open on time, takes a press on the open items before the ComboBox (which
  would close it on the press), closes on a tap outside and on Back (`PhonePopupHost.TakeBackFirst`), and opens and
  answers the full-screen picker.
- **`PART_Popup`** is empty: Avalonia's ComboBox needs it, and it raises `DropDownOpened` and `DropDownClosed`.
- **No subclass**: a behaviour on Avalonia's control gives every `<ComboBox>` both forms (PLAN.md, "The ladder").

## The ladder

Rung 1 for the themes; rung 2 for `PhoneComboBox`, `PhoneComboBoxItemsHost` and `ControlHeader`. No type substitution.

## Resource keys

The phone's, in `Tokens/`: `ComboBoxForegroundThemeBrush`, `ComboBoxBackgroundThemeBrush`, `ComboBoxBorderThemeBrush`,
`ComboBoxBorderThemeThickness`, `PhoneFontFamilyNormal`, `ContentControlFontSize`, `ComboBoxPressed*`,
`ComboBoxHighlighted*`, `ComboBoxDisabled*`, `PhoneMidBrush`, `ComboBoxItemMinHeightThemeSize`,
`PhoneTouchTargetOverhang`, `ComboBoxPlaceholderTextThemeMargin`, `ComboBoxItemDisabledThemeOpacity`,
`PhoneHighContrastSelectedForegroundThemeBrush`, `ComboBoxItemHighContrastBackgroundThemeMargin`,
`ComboBoxItemSelectedForegroundThemeBrush` (the picker's selected item); the styles `Wp81HeaderContentPresenterTheme`
and `ComboBoxPlaceholderTextBlockStyle`. Cornflower's, in `Controls/ComboBox.axaml`: `Wp81ComboBoxPlaceholderText`
("choose an item").

## Known gaps

- **An item is 51.25 tall, not 51.67**: Avalonia rounds each side of its padding to the pixel; five items open 2.1 px short.
- **The storyboards' ease isn't run**: the phone's never reaches the screen (#149), so ours opens and closes as the
  screen does.
- **A tap outside and Back** on the open inline box weren't read; the picker's title source (placeholder or a fixed
  string) isn't told apart by the reads (PLAN.md, "Decided on the user's behalf").
- **The editable ComboBox** (`IsEditable`, which the phone hasn't) isn't themed: Wp81's template has no edit box, so an
  editable ComboBox can't be typed in under Wp81 (it could under Simple's template while ComboBox was a hole).
- **The Full form keeps its hidden inline items**: a container for each item, never shown, as the phone's template keeps
  its ItemsPresenter.
- **Items that are controls** (a `ComboBoxItem` written in XAML) go to the full-screen picker as their content, or the
  text of a `TextBlock` content; other control content shows as an empty row. One disabled in the box is disabled in
  the picker.
