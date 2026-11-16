# AutoSuggestBox

Status: done but for the device and real-window runs · [#158](https://github.com/wdcossey/CornflowerUI/issues/158) · owed: the device run on the Pixel 8 (the keyboard's rectangle and Back on Android) · owed: the real-window run · owed: captures of the list's motion and of landscape

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
- **A key** asks the app 150 ms later (`Populating`), and the list opens if it gave anything; emptying the box asks
  again. `Text` set from code doesn't open the list.
- **A tap on a suggestion** puts it in the box and closes the list; the box keeps the focus, so the keyboard stays.
- **Back** closes the list and takes the focus from the box, before the page's `NavigationPage` sees it; the next Back
  is the page's. The keyboard going down (Android's keyboard takes the first Back itself) does the same.
- **The list is drawn in the window**, in its overlay layer, never as a window of its own.
- **Not done**: the page doesn't move up so the box is at the window's top, as the phone's does (the shell's, not the
  control's; see the plan).

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
rung 6 (recorded) for the page's move, the events' order and reason, Enter, and the rounding. No type is substituted.

## Extensions

None new. `ControlHeader` (#96) gives `Header` and `HeaderTemplate`.

## Known gaps

In [COMPARISON.md](COMPARISON.md#known-gaps): the page's move, the events' reason and order, Enter, one raw pixel at the
right edge, the list's motion and landscape unmeasured.

## The checklist

In [PLAN.md](PLAN.md#checklist): 28 rows done or kept as Avalonia's, 30 left out with their reasons.
