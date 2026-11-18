# AutoSuggestBox

Status: done · [#158](https://github.com/wdcossey/Cornflower/issues/158) · device run passed 2026-11-17 (Pixel 8, Android 17, Release, over wireless adb, `run.ps1 -Page autosuggestbox` on `main` at `9a24f7f`, 17:40 to 17:44, cold starts 747 to 813 ms over the run's pages: the page opened in 385 ms; the suggestion list shown with 'a' typed, above the keyboard; the page stays where it is, which is [#179](https://github.com/wdcossey/Cornflower/issues/179)) · Back on the list passed 2026-11-17 (Pixel 8, over wireless adb, `run.ps1 -Page autosuggestbox` on `main` at `03082b3`, 20:42 to 20:45, cold starts 757 to 879 ms over the run's pages: the page opened in 396 ms; the first Back closes the list and the keyboard and drops the focus, the second goes back to the catalogue) · real-window run passed 2026-11-17 (`tools/real-window/drive.ps1`, Debug, on `main` at `03082b3`: 195 of 195) · the keyboard's pan, [#179](https://github.com/wdcossey/Cornflower/issues/179), device run passed 2026-11-18 (Pixel 8, Release, on `07a0d02`, 12:02, after a run at 11:51 on `a0f1a02` found two faults, fixed in `07a0d02`: the page moves up until the header's top is at the window's top, the list hangs from the box and stops 2.5 above the keyboard, and Back takes the keyboard, the list and the move) · owed: captures of the list's motion and of landscape

The Windows Phone 8.1 AutoSuggestBox: Avalonia's `AutoCompleteBox`, themed by `Wp81Theme` with the phone's default
style and template, in `src/Cornflower.Themes.Wp81/Controls/AutoCompleteBox.axaml`. WinRT's `Header` is
`cf:ControlHeader.Header`, attached. The API differs from the phone's; [PLAN.md](PLAN.md#the-api-side-by-side) has
each difference, and what a small helper could give. The phone's grabs beside ours are in
[COMPARISON.md](COMPARISON.md).

## Usage

As on the phone, the app gives the suggestions; the box doesn't filter them. The phone's cue is `TextChanged` with
`UserInput`; Avalonia's is `Populating`, which the theme raises 150 ms after the key, as the phone does.

```xml
<!-- 8.1 -->
<AutoSuggestBox Header="fruit" PlaceholderText="type a fruit" TextChanged="OnTextChanged" SuggestionChosen="OnChosen" />
<!-- Avalonia, with Wp81Theme in Application.Styles; xmlns:cf="using:Cornflower.Controls" -->
<AutoCompleteBox cf:ControlHeader.Header="fruit" PlaceholderText="type a fruit" Populating="OnPopulating" SelectionChanged="OnChosen" />
```

```csharp
// 8.1: if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) sender.ItemsSource = Find(sender.Text);
void OnPopulating(object? sender, PopulatingEventArgs e) => ((AutoCompleteBox)sender!).ItemsSource = Find(e.Parameter);
```

| 8.1 | Here |
|---|---|
| `IsSuggestionListOpen` | `IsDropDownOpen` |
| `MaxSuggestionListHeight` | `MaxDropDownHeight` |
| `TextMemberPath="Name"` | `ValueMemberBinding="{Binding Name}"` |
| `UpdateTextOnSelect="False"` | `TextSelector = (typed, _) => typed` |
| `TextBoxStyle` | a style for `AutoCompleteBox /template/ TextBox#PART_TextBox` |

## Behaviours

- **The box** is the TextBox's (#36): `#FFCCCCCC` at rest, white with the accent round it when focused, the header
  above it, the placeholder in it.
- **The list** hangs from the box: its top border on the box's bottom border, the box's width, white with a 2.5 accent
  border and black text in both themes; rows 39.17 tall, 48.75 apart (the phone's ListViewItems, with the tilt).
- **Its height**: its rows', up to 2.5 above the keyboard's top (Avalonia's `InputPane`), or above the window's bottom
  with no keyboard, and never over `MaxDropDownHeight`; the rest scroll.
- **A key** asks the app 150 ms after the last key (`Populating`), and the list opens if it gave anything; emptying
  the box asks again. `Text` set from code doesn't open the list.
- **While open** the list follows the box as the page lays out; a tap on the box goes to the box and the list stays;
  with less than one suggestion's room below the box the list closes.
- **`SelectionChanged`** comes for a tapped suggestion and also when the typed text equals one (Avalonia's box selects
  an exact match); the phone's `SuggestionChosen` comes for a tap alone.
- **A tap on a suggestion** puts it in the box and closes the list; the box keeps the focus, so the keyboard stays.
- **Back** closes the list and takes the focus from the box, before the page's `NavigationPage` sees it; the next Back
  is the page's. The keyboard going down (Android's keyboard takes the first Back itself) does the same.
- **The list is drawn in the window**, in its overlay layer, never as a window of its own.
- **The keyboard moves the page up** until the control's top (its header's) is at the window's top, as #150 read
  (21.67 up at the reference app's place), and back down as it goes; the list is hung and sized again as the page
  moves (#179: `PhoneKeyboardPan`, the TextBox part's, [its plan](../KeyboardPan/PLAN.md)). The move is at once
  (the phone's motion unmeasured). The device run passed on 2026-11-18.

## Resource keys

The phone's own, all in `Tokens/`, by `DynamicResource`: `TextControlMarginThemeThickness`,
`AutoSuggestListViewItemMargin`, `ContentControlFontSize`, `TextBoxForegroundThemeBrush`,
`AutoSuggestBackgroundThemeBrush`, `PhoneAccentBrush`, `TextControlBorderThemeThickness`, and the TextBox's
`TextBoxPlaceholderTextThemeBrush`. No new key.

## The template's parts

A `Grid` › `PART_TextBox` (Wp81's TextBox) and `PART_Popup` › `SuggestionsContainer` › `PART_SelectingItemsControl`
(Wp81's ListBox, which styles its own `ListBoxItem`s). Neither the list nor its items take the focus.

## The ladder

Rung 1 (theme) for the setters, the template and the list's items; rung 2 for `Header` (`ControlHeader`, attached) and
`PhoneSuggestionList`, internal: the list's place and height, closing it on a tap, Back and the keyboard going down;
`PhoneKeyboardPan`, internal, the TextBox part's, for the page's move (#179); rung 6 (recorded) for the events' order and reason, Enter, and the rounding. No type is substituted.

## Extensions

None new. `ControlHeader` (#96) gives `Header` and `HeaderTemplate`.

## Known gaps

In [COMPARISON.md](COMPARISON.md#known-gaps): the page's move, the events' reason and order, Enter, one raw pixel at the
right edge, the list's motion and landscape unmeasured.

## The checklist

In [PLAN.md](PLAN.md#checklist): 28 rows done or kept as Avalonia's, 30 left out with their reasons.
