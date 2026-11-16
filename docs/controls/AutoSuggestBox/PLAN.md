# AutoSuggestBox: plan

[#158](https://github.com/wdcossey/CornflowerUI/issues/158). The 8.1 AutoSuggestBox as a theme for Avalonia's
`AutoCompleteBox` (rung 1), with one internal behaviour the theme turns on, `PhoneSuggestionList` (rung 2), and the
header by `ControlHeader` (rung 2, as the TextBox's). No type is substituted. Avalonia's control has a different API;
every difference is in [the API, side by side](#the-api-side-by-side), with what a small attached helper could give and
what can't be had.

## Where the values come from

- **The style**: the 8.1 Update 1 runtime's `autosuggestbox.json` (`R:\cornflower-reference\flash-update1\xbf-json\`),
  equal to the SDK's: a `Grid` holding a `TextBox` named `TextBox` (the phone's own TextBox style; its `Header` and
  `PlaceholderText` the control's; margin 0) and a `Popup` named `SuggestionsPopup` of a `Border` named
  `SuggestionsContainer` (`AutoSuggestBackgroundThemeBrush` in a `TextControlBorderThemeThickness` border of
  `PhoneAccentBrush`) of a `ListView` named `SuggestionsList`. The style sets `Margin`
  (`TextControlMarginThemeThickness`), `VerticalAlignment` Top, `IsTabStop` False and an `ItemContainerStyle` for
  `ListViewItem` (`AutoSuggestListViewItemMargin`, `ContentControlFontSize`, `TextBoxForegroundThemeBrush`).
- **The screen grabs**: #150's, run `20261116-1944-dev11`, in [`ref/wp81/AutoSuggestBox/`](../../../ref/wp81/AutoSuggestBox/README.md):
  at rest, focused, with text, and with 1, 3 and 12 suggestions, dark and light, 1080 × 1920 at 2.4 (device 11).
- **The events**: #150's `-Probe dismiss` run `20261116-1951-dev11`, read with `tools/refapp/dismiss.py`
  ([`tools/refapp/README.md`](../../../tools/refapp/README.md), "AutoSuggestBox" and "How each popup is dismissed, and
  the AutoSuggestBox's events").
- **Avalonia's control**: `AutoCompleteBox` and Simple's template in Avalonia 12.1.4's source (tag `12.1.4`).

## The behaviours

Each has its test in `tests/Cornflower.Tests/Controls/AutoSuggestBoxTests.cs`, against `AutoSuggestBoxReadings`.

| Behaviour | Evidence | Test |
|---|---|---|
| The template is the phone's: a TextBox part (Wp81's TextBox, its header the control's, no margin of its own), a popup of a bordered list | `autosuggestbox.json` | `The_Box_Is_Templated_By_The_Theme_Its_TextBox_The_Phones` |
| The style's `Margin` (0, 9.5) and `VerticalAlignment` Top; no filter, a cue at every change (to nothing too), 150 ms after the key | the style; #150's events (`TextChanged`, `UserInput`, 150 to 177 ms after `KeyDown`) | `The_Style_Sets_What_The_Phone_Does` |
| At rest the box is the phone's: its border from 48.33 to 87.5, 19.17 in | the rest grab's rows 116 to 209 | `At_Rest_The_Box_Is_Where_The_Phones_Is` |
| The list is drawn in the window's overlay layer, never in a window of its own | the phone has one window | `The_List_Is_In_The_Windows_Own_Overlay_Layer` |
| The list hangs from the box's bottom border (63.33, the borders on each other), the box's width; one suggestion 53.75 tall, three 151.25; rows 39.17 tall, 48.75 apart, the first 12.08 down; the phone's ListViewItems (they tilt) at 20.26 in black | the grabs; #150 | `The_List_Hangs_From_The_Boxs_Bottom_Border_As_Tall_As_Its_Rows` |
| Twelve run it down to 2.5 above the keyboard's top (387.5 tall, to 450.83), the rest scrolled to | the `list-12` grab | `Twelve_Fill_The_List_Down_To_The_Keyboard` |
| With no keyboard it may run to 2.5 above the window's bottom; it follows the keyboard; `MaxDropDownHeight` caps it | WinRT's `MaxSuggestionListHeight`; a desktop has no keyboard | `The_List_Takes_The_Room_Above_The_Keyboard_Or_The_Windows_Bottom_Up_To_Its_Max` |
| A keyboard that covers nothing, or one over the list's top (a box low on a page that doesn't move up), is passed over; with no room at all the list closes rather than stay open unseen | Avalonia's `InputPane` can report either; the phone always moves the box up | `A_Keyboard_Covering_Nothing_Or_Over_The_Lists_Top_Is_Passed_Over`, `With_No_Room_Below_The_Box_The_List_Closes` |
| While open, the list follows the box as the page lays it out again (Avalonia places a popup again only when its target's own bounds change) | the list hangs from the box | `The_Open_List_Follows_The_Box` |
| A tap on the box with the list open goes to the box; the list stays | Avalonia's light dismiss passes the box through (`OverlayInputPassThroughElement`); the phone's not read | `A_Tap_On_The_Box_Keeps_The_List` |
| The pixels: the focused box's accent border and white inside, the borders on each other, the list white with accent sides and bottom, the text black, in both themes | the grabs' rows 64 to 69, 152 to 157, 509 to 514; columns 46 to 51 | `Its_Pixels_Are_The_Phones` |
| The app gives the suggestions; the box doesn't filter them | WinRT's API: no filter; the probe set `ItemsSource` in `TextChanged` | `The_App_Gives_The_Suggestions_And_The_Box_Doesnt_Filter_Them` |
| A key asks the app 150 ms later (after the last key: a key within 150 ms starts the wait again), and the list opens then | #150: `TextChanged` (`UserInput`) 150 to 177 ms after `KeyDown`, the list opening as the app sets `ItemsSource` | `A_Key_Asks_The_App_150_Ms_Later_And_The_List_Opens_Then` |
| Emptying the box asks the app again | the phone raises `TextChanged` for every change the user makes | `Emptying_The_Box_Asks_The_App_Again` |
| A tap on a suggestion puts it in the box and closes the list; the box keeps the focus (the keyboard stays) | #150 case 3 | `A_Tap_On_A_Suggestion_Puts_It_In_The_Box_And_Closes_The_List` |
| `Text` from code doesn't open the list | #150 case 5 (`ProgrammaticChange`) | `Text_From_Code_Doesnt_Open_The_List` |
| One Back closes the list and takes the focus, before the page's `NavigationPage`; the next goes back | #150: "one Back hid the keyboard, closed the suggestion list and took the focus ... the next reached the app" | `Back_Closes_The_List_And_Takes_The_Focus_Then_The_Next_Goes_Back`, `Back_With_The_Box_Unfocused_Is_Left_Alone` |
| The keyboard going down (Android's keyboard takes the first Back itself) does the same | as above | `The_Keyboard_Going_Down_Closes_The_List_And_Takes_The_Focus`, `The_Keyboard_Going_Down_With_The_Focus_Elsewhere_Takes_Nothing` |
| Under Simple alone the box is Avalonia's own | the theme turns the behaviour on | `Simple_Alone_Keeps_Avalonias_Own` |

## The visual states

| Group | State | What it changes | Here |
|---|---|---|---|
| `Orientation` | `Landscape`, `Portrait` | nothing | nothing to do |

The box's own states are the TextBox's (#36): at rest `#FFCCCCCC` all through (white at 0.8), focused white with the
accent round it.

## The API, side by side

WinRT's `AutoSuggestBox` is an `ItemsControl` of its own; Avalonia's `AutoCompleteBox` is a `TemplatedControl` that
holds a list. "Helper" says whether a small attached helper in `Cornflower.Controls` could give what is missing. None
is built: no app has asked, and each has an Avalonia way already (decided on the user's behalf, below).

| WinRT | Avalonia | Here | Helper |
|---|---|---|---|
| `Text` | `Text` | the same | |
| `PlaceholderText` | `PlaceholderText` | the same, shown by the TextBox part | |
| `Header` | none | `cf:ControlHeader.Header` (and `HeaderTemplate`), passed on to the TextBox part's header (rung 2) | done |
| `IsSuggestionListOpen` | `IsDropDownOpen` | another name | |
| `MaxSuggestionListHeight` | `MaxDropDownHeight` | another name; `PhoneSuggestionList` keeps the list under it | |
| `TextMemberPath` | `ValueMemberBinding` | a binding (`{Binding Name}`), not a path | could take a path and make the binding |
| `UpdateTextOnSelect` (true) | none: a chosen item always sets the text | `TextSelector = (typed, _) => typed` keeps the typed text | could: an attached bool that sets `TextSelector` |
| `TextBoxStyle` | none | an app's style for `AutoCompleteBox /template/ TextBox#PART_TextBox` | could: an attached `ControlTheme` the template binds to the part's `Theme` |
| `AutoMaximizeSuggestionArea` (true) | none | the list always takes the room down to the keyboard; the page doesn't move up to put the box at the window's top | **can't be had** from the control: the move is the shell's (below) |
| `ItemsSource`, set by the app, not filtered | `ItemsSource`, filtered by `FilterMode` (`StartsWith`) | the theme sets `FilterMode` None. The phone opens the list when `ItemsSource` is set while focused, at any time; Avalonia opens it only after a populate, so the app sets it in `Populating` (or an `AsyncPopulator`, or `Populating`'s `Cancel` then `PopulateComplete()`) | could: open the list when `ItemsSource` changes while focused (`PopulateComplete()`) |
| `ItemTemplate` | `ItemTemplate` | the same | |
| `ItemContainerStyle` (the style's: the items' margin, size, colour) | none | the template's list styles its own items | |
| `TextChanged` with `Reason` (`UserInput`, `ProgrammaticChange`, `SuggestionChosen`) | `TextChanged`, no reason, at once for each | the app's cue, `UserInput`, is Avalonia's `Populating`, at the phone's 150 ms (`MinimumPopulateDelay`) | could: an attached event with the reason, from the TextBox part's input, `Text` set from code and the chosen item |
| `SuggestionChosen` (`SelectedItem`; `Text` still the typed text; `TextChanged` with `SuggestionChosen` 162 ms later) | `SelectionChanged` (`AddedItems`), after the text is set | the order differs | could: raise it from a class handler on the list's `SelectionChanged`, which runs before the box's own handler sets the text |
| no `QuerySubmitted` (Windows 10's); Enter raises only `KeyDown` | Enter with the list open commits and closes it | a recorded difference | could take Enter while the list is open |
| `ItemTemplateSelector`, `ItemContainerStyleSelector`, `ItemContainerTransitions`, `ItemsPanel`, `ItemsPanelRoot`, `GroupStyle`, `GroupStyleSelector`, `IsGrouping`, `DisplayMemberPath`, `Items`, `ItemContainerGenerator` and `ItemsControl`'s methods | none: not an `ItemsControl` | left out | no |

Avalonia's own members the phone lacks are kept as Avalonia's: `SelectedItem`, `SearchText`, `FilterMode`,
`ItemFilter`, `TextFilter`, `ItemSelector`, `TextSelector`, `AsyncPopulator`, `MinimumPrefixLength`,
`MinimumPopulateDelay`, `IsTextCompletionEnabled`, `CaretIndex`, `MaxLength`, `ClearSelectionOnLostFocus`,
`InnerLeftContent`, `InnerRightContent` (passed on to the TextBox part), `Populating`, `Populated`, `DropDownOpening`,
`DropDownOpened`, `DropDownClosing`, `DropDownClosed`.

### Focus, the keyboard and the page

- **Focus brings up the keyboard**: Avalonia's own, on a phone, for the TextBox part (its `TextInputMethodClient`).
- **The page moves up** until the control's top is at the window's top (21.67 at the reference app's place): the
  phone's shell pans the page while the keyboard is up. Avalonia has nothing that does it; its `InputPane` reports the
  keyboard's rectangle, and a `ScrollViewer` can bring the box into view, but nothing moves a page that doesn't scroll.
  Recorded, not done: it would be the page's or the shell's, for every text box, not this control's.
- **The list's room** is the keyboard's top (Avalonia's `InputPane.OccludedRect`) less 2.5, as the phone's; with no
  keyboard, the window's bottom inside its safe area.
- **Back** and **the keyboard going down** close the list and take the focus, as the phone's one Back does.

## Decided on the user's behalf

- **No filter** (`FilterMode` None): the phone's control doesn't filter; the app gives the suggestions. An app that
  wants Avalonia's sets `FilterMode` on the box. To reverse: delete the setter.
- **Every change asks the app** (`MinimumPrefixLength` 0), the box emptied too, as the phone's `TextChanged` comes for
  every change. To reverse: delete the setter (Avalonia's is 1).
- **The cue 150 ms after the key** (`MinimumPopulateDelay`): the least of #150's 150 to 177 ms. The list opens with it, so
  it opens when the phone's does. Avalonia's wait restarts at each key, so quick keys give one cue after the last; the
  phone's two quick keys weren't read. Avalonia's `TextChanged` still comes at once. To reverse: delete the setter.
- **Not Flyout's popup host.** `PhonePopupHost` places a popup at the window's top or over all of it, dims and turns it
  in bands; the list hangs from the box, nothing dims, and no motion of the list was read. So the template's `Popup` is
  Avalonia's own, in the overlay layer (`ShouldUseOverlayLayer`, which the host would only have set), placed below the
  box; `PhoneSuggestionList` moves it onto the box's border and sizes it. To reverse, when the list's motion is read:
  give the host a placement below a target (proposed below) and host the list there.
- **The list and its items take no focus**, so a tap on a suggestion leaves the focus in the box and the keyboard up,
  as on the phone (where the box loses and regains it in one frame). With focusable items Avalonia moved the focus to
  the item in the closed popup. `PhoneSuggestionList` closes the list as a tap selects, as Avalonia's own close (on
  losing the focus) no longer comes and its commit on release is for a mouse alone. To reverse: drop `Focusable` from
  the template.
- **Back takes the focus from a focused box** before the page sees it, from a class handler on `BackRequestedEvent`
  of its own; and the keyboard going down does the same, as Android's keyboard takes the first Back itself. In a
  flyout, the host's class handler and this one run in the order their types were first used, so either the box or the
  flyout goes first (the phone's keyboard goes first). To reverse: remove the handler and the keyboard's hold.
- **No event helper** (the reason, `SuggestionChosen`, `UpdateTextOnSelect`, `TextBoxStyle`): each has an Avalonia way,
  in the table above. A helper is a later PR if an app asks.

## Proposed for shared files

- **`PhonePopupDismissal`**, the one place for #150's rules: add the AutoSuggestBox's, today in `PhoneSuggestionList`:
  `internal const bool BackTakesFocusFirst = true; // one Back hides the keyboard, closes an AutoSuggestBox's list and takes the focus; the next reaches the app (#150)`.
- **`PhonePopupHost`**: if the list's open and close motion is read, a `PhonePopupPlacement.Below` (anchored under a
  target, the target's width, its height down to the keyboard), so the list can be hosted there with its motion; and
  its Back handler leaving the request to a focused text box first, so the order with `PhoneSuggestionList` is fixed.
- **`CLAUDE.md`**: the rows for this PR (the theme, `PhoneSuggestionList`, the tests, the sample page, the captures'
  folder's use), for the roll-up; and in `HoleParts`'s sentence, "the file dialog its CheckBox, AutoCompleteBox,
  CalendarDatePicker, ComboBox, the file dialogs and NumericUpDown their TextBoxes" loses "AutoCompleteBox, " (it is no
  longer a hole).
- **One area rule**: `PhonePopupHost`'s window area inside the safe area (`Area()`) as an internal helper both use;
  `PhoneSuggestionList` repeats it today.

## For you to decide

- Should Cornflower move the page up while the keyboard is up, as the phone's shell does (for every text box, not just
  this one)? It changes every page's layout under the keyboard; no sound default, so not done.

## Dependencies

- Wp81's TextBox (#36), ListView (#119) and ScrollViewer (#116) themes, which the template's parts take.
- #150's grabs and events (PR #172).

## Owed

- The list's open and close motion: no recording reads it.
- Landscape: the phone's template turns the list upward (`UpwardTransform`, `ListItemOrderTransform`); no grab.
- A tap on the box while the list is open (ours keeps the list), and whether a tap off the box reaches the page: not
  read.
- A keyboard that grows while up (another layout of keys): Avalonia's `InputPane` raises only its state, so the list's
  room follows it when the window or the page lays out again, not before.
- The device run on the Pixel 8 (the keyboard's rectangle and Back on Android) and the real-window run.

## Checklist

Generated on 2026-11-16 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with its
reason. The tool's Avalonia column says none, as no Avalonia type has the name; the Avalonia column here is
`AutoCompleteBox`'s.

> Generated by `dotnet run --project tools/winrt-api -- checklist AutoSuggestBox <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.AutoSuggestBox`; Avalonia `Avalonia.Controls.AutoCompleteBox` (a theme, not rung 5).

### Properties (9)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `UpdateTextOnSelect` | `Boolean` | none | Avalonia always sets the text; `TextSelector` keeps the typed text. A helper could |
| [x] | `TextMemberPath` | `String` | `ValueMemberBinding` | a binding, not a path |
| [-] | `TextBoxStyle` | `Style` | none | an app's style on `/template/ TextBox#PART_TextBox`. A helper could |
| [x] | `Text` | `String` | `Text` | Avalonia's |
| [x] | `PlaceholderText` | `String` | `PlaceholderText` | passed on to the TextBox part |
| [x] | `MaxSuggestionListHeight` | `Double` | `MaxDropDownHeight` | `PhoneSuggestionList` keeps the list under it |
| [x] | `IsSuggestionListOpen` | `Boolean` | `IsDropDownOpen` | Avalonia's |
| [x] | `Header` | `Object` | `cf:ControlHeader.Header` | attached, passed on to the TextBox part's header |
| [-] | `AutoMaximizeSuggestionArea` | `Boolean` | none | the list takes the room to the keyboard; the page's move up can't be had |

### Events (2)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `SuggestionChosen` | `TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs>` | `SelectionChanged` | after the text is set, not before. A helper could |
| [x] | `TextChanged` | `TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs>` | `TextChanged`, `Populating` | no reason; `Populating` is `UserInput`'s cue, at 150 ms. A helper could give the reason |

### Methods (0)

_none_

### Properties, from `ItemsControl` (14)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ItemsPanelRoot` (read-only) (not a dependency property) | `Panel` | none | not an `ItemsControl` |
| [x] | `ItemsSource` | `Object` | `ItemsSource` | not filtered (`FilterMode` None); opens after a populate |
| [-] | `ItemsPanel` | `ItemsPanelTemplate` | none | not an `ItemsControl`; an app's style on the list |
| [-] | `ItemTemplateSelector` | `DataTemplateSelector` | none | Avalonia picks templates by their `Match` |
| [x] | `ItemTemplate` | `DataTemplate` | `ItemTemplate` | passed on to the list |
| [-] | `ItemContainerTransitions` | `TransitionCollection` | none | not an `ItemsControl` |
| [-] | `ItemContainerStyleSelector` | `StyleSelector` | none | not an `ItemsControl` |
| [x] | `ItemContainerStyle` | `Style` | none | the style's values, set by the template's list on its items |
| [-] | `GroupStyleSelector` | `GroupStyleSelector` | none | no grouping |
| [-] | `DisplayMemberPath` | `String` | none | `ValueMemberBinding` |
| [-] | `GroupStyle` (read-only) (not a dependency property) | `IObservableVector<GroupStyle>` | none | no grouping |
| [-] | `IsGrouping` (read-only) | `Boolean` | none | no grouping |
| [-] | `ItemContainerGenerator` (read-only) (not a dependency property) | `ItemContainerGenerator` | none | not an `ItemsControl` |
| [-] | `Items` (read-only) (not a dependency property) | `ItemCollection` | none | `ItemsSource` only |

### Events, from `ItemsControl` (0)

_none_

### Methods, from `ItemsControl` (14)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `Object ItemFromContainer(DependencyObject container)` | none | not an `ItemsControl` |
| [-] | `DependencyObject ContainerFromItem(Object item)` | none | not an `ItemsControl` |
| [-] | `Int32 IndexFromContainer(DependencyObject container)` | none | not an `ItemsControl` |
| [-] | `DependencyObject ContainerFromIndex(Int32 index)` | none | not an `ItemsControl` |
| [-] | `Boolean IsItemItsOwnContainerOverride(Object item)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `DependencyObject GetContainerForItemOverride()` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void ClearContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void PrepareContainerForItemOverride(DependencyObject element, Object item)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void OnItemsChanged(Object e)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)` (protected, overridable) | none | not an `ItemsControl` |
| [-] | `void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)` (protected, overridable) | none | not an `ItemsControl` |

### Default style: setters (4)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `Margin` | `{ThemeResource TextControlMarginThemeThickness}` | `Margin` | set |
| [x] | `VerticalAlignment` | `Top` | `VerticalAlignment` | set |
| [x] | `IsTabStop` | `False` | `Focusable` | kept: Avalonia's box passes its focus to the TextBox part |
| [x] | `ItemContainerStyle` | `Style TargetType=ListViewItem` | the list's item style | in the template |

### Template parts (6)

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `TextBox` | `TextBox` | `PART_TextBox` | Wp81's TextBox |
| [x] | `SuggestionsPopup` | `Popup` | `PART_Popup` | in the overlay layer |
| [x] | `SuggestionsContainer` | `Border` | `SuggestionsContainer` | the same name |
| [-] | `UpwardTransform` | `TranslateTransform` | none | the list opening upward, in landscape: no grab |
| [x] | `SuggestionsList` | `ListView` | `PART_SelectingItemsControl` | Wp81's ListBox, its items taking no focus |
| [-] | `ListItemOrderTransform` | `ScaleTransform` | none | as `UpwardTransform` |

### Visual states (2)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `Orientation` | `Landscape` | nothing | none | nothing to do |
| [x] | `Orientation` | `Portrait` | nothing | none | nothing to do |

### Transitions (0)

_none_

### Resources (7)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `TextControlMarginThemeThickness` | `0,9.5,0,9.5` | same | yes | the box's margin |
| [x] | `AutoSuggestListViewItemMargin` | `6.5,9.5,0,0` | same | yes | the items' margin |
| [x] | `ContentControlFontSize` | `20.26` | same | yes | the items' size |
| [x] | `TextBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource TextBoxForegroundColor} → #FF000000` | same | yes | the items' text |
| [x] | `AutoSuggestBackgroundThemeBrush` | `SolidColorBrush #FFFFFFFF` | same | yes | the list's inside |
| [x] | `PhoneAccentBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | the list's border |
| [x] | `TextControlBorderThemeThickness` | `2.5` | same | yes | the list's border |
