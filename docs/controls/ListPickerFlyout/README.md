# ListPickerFlyout, PickerFlyout and their presenters, and the pickers' bar

Status: done but for the device and real-window runs · [#153](https://github.com/wdcossey/CornflowerUI/issues/153) · owed: the device run on the Pixel 8 · owed: the real-window run

The Windows Phone 8.1 full-screen pickers: Cornflower's `ListPickerFlyout` and `PickerFlyout` (on Avalonia's
`PopupFlyoutBase`) and their presenters, `ListPickerFlyoutPresenter` and `PickerFlyoutPresenter` (on Avalonia's
`PickerPresenterBase`), in `src/Cornflower.Controls/Controls/Pickers/`; themed by `Wp81Theme` in
`src/Cornflower.Themes.Wp81/Controls/ListPickerFlyoutPresenter.axaml` and `PickerFlyoutPresenter.axaml`, filling the
window and turning in and out as the Flyout's full popup does (#151). The pickers' bar, accept and cancel at the foot,
is in `Controls/PickerBar.axaml`, for the date and time pickers (#156) too. [PLAN.md](PLAN.md) has every value and its
source; [COMPARISON.md](COMPARISON.md) has the emulator's grabs beside ours.

## Usage

```csharp
var picker = new ListPickerFlyout { ItemsSource = colours, SelectedIndex = 4 };
PickerFlyoutBase.SetTitle(picker, "COLOUR");
IReadOnlyList<object>? picked = await picker.ShowAtAsync(button);   // null: nothing picked

var several = new ListPickerFlyout { ItemsSource = colours, SelectionMode = ListPickerFlyoutSelectionMode.Multiple };
several.ItemsPicked += (_, e) => Show(e.AddedItems, e.RemovedItems);

var any = new PickerFlyout { Content = myPanel, ConfirmationButtonsVisible = true };
bool accepted = await any.ShowAtAsync(button);
```

They are flyouts, so `<Button.Flyout><cf:ListPickerFlyout ... /></Button.Flyout>` and `ShowAt` work too.

- **ListPickerFlyout**: `ItemsSource`, `ItemTemplate`, `DisplayMemberBinding` (WinRT's `DisplayMemberPath`),
  `SelectionMode` (`Single`, `Multiple`), `SelectedIndex`, `SelectedItem`, `SelectedItems`, `ItemsPicked`,
  `ShowAtAsync`. In Single a tap on an item picks it and closes the picker; the phone draws no selection. In Multiple
  each item has a check box (those in `SelectedItems` checked), a tap checks or unchecks, and accept picks the checked
  ones. A pick sets the selection, raises `ItemsPicked` with what was added and removed, and is `ShowAtAsync`'s result,
  all in the tap's frame; `Closed` comes when the closing motion has run.
- **PickerFlyout**: `Content`, `ConfirmationButtonsVisible`, `Confirmed`, `ShowAtAsync`. Accept raises `Confirmed` and
  gives true.
- **Both**: `PickerFlyoutBase.Title` (attached, as WinRT's) above the content; none, no title. Back, cancel and `Hide()`
  close with nothing: null or false, in `Closed`'s frame, with no event. `Opening` and `Opened` come in the call's frame.

## What it looks like

- **The picker** fills the window (inside the safe area) in `FlyoutBackgroundThemeBrush`, its content 19 in and 37
  down. The title in `FlyoutPickerTitleTextBlockStyle` (the list picker's) or `TitleTextBlockStyle` (the picker
  flyout's), 32.5 above what follows.
- **The list picker's items**: Wp81's ListView, each item 39.17 tall with 19 under it, its text `PhoneFontFamilySemiLight`
  25.5; in Multiple a 25.5 check box at the left, the text beside it. An 80 footer under the last item.
- **The bar** shows in Multiple and with `ConfirmationButtonsVisible`; then the content ends at its top.
- **Opening and closing**: the PickerFlyout's are the full Flyout's bands, as #149 read it. The list picker's are its
  rows, 66.7 px, rows 0 to 4 turning from 80° at 12.5, 9.6, 7.8, 6.6 and 5.7° a tick and the rest revealed downwards;
  it closes in twelve bands, `Closed` at 30 ticks (468.8 ms). Back closes either ahead of the page.

## The pickers' bar

The shell draws a picker's accept and cancel as the page's compact app bar (#150, B15). Here it is Wp81's `CommandBar`
with two `CommandBarButton`s and Cornflower's own glyphs. Put it in a full-screen presenter's template, over the foot
of a `Grid` whose second row keeps the closed bar's height free:

```xml
<Grid RowDefinitions="*,Auto">
  <!-- the presenter's content, row 0 -->
  <Panel x:Name="PickerBarSpace" Grid.Row="1" Height="{DynamicResource Wp81CommandBarCompactHeight}" />
  <CommandBar x:Name="PickerBar" Grid.RowSpan="2" VerticalAlignment="Bottom">
    <CommandBarButton x:Name="PART_AcceptButton" Label="done">
      <CommandBarButton.Icon>
        <TemplatedControl Theme="{DynamicResource Wp81PickerAcceptGlyphTheme}" />
      </CommandBarButton.Icon>
    </CommandBarButton>
    <CommandBarButton x:Name="PART_DismissButton" Label="cancel">
      <CommandBarButton.Icon>
        <TemplatedControl Theme="{DynamicResource Wp81PickerCancelGlyphTheme}" />
      </CommandBarButton.Icon>
    </CommandBarButton>
  </CommandBar>
</Grid>
```

- **The names** `PART_AcceptButton` and `PART_DismissButton` are the ones Avalonia's `DatePickerPresenter` and
  `TimePickerPresenter` look for (any `Button`; a `CommandBarButton` is one), and the ones Cornflower's presenters
  look for: a click confirms or dismisses.
- **Showing it**: the bar and its space are hidden in the list picker's templates until the picker asks
  (`cf:PickerPresenterOptions.ShowsConfirmationButtons`, which any `PickerPresenterBase` can carry). A date or time
  presenter always shows it.
- **The glyphs**: `Wp81PickerAcceptGlyphTheme` and `Wp81PickerCancelGlyphTheme` fill the button's 40.75 circle and
  stroke `Wp81.Glyph.PickerAccept` and `Wp81.Glyph.PickerCancel` in the button's icon foreground (white on the accent
  while pressed).
- **The rest is the CommandBar's**: 62.92 tall, the circles 73.75 apart and centred, the "..." that opens it to show
  the labels, the press.

## Extensions

| Name | On | What |
|---|---|---|
| `PickerPresenterOptions.Title` | `PickerPresenterBase` | the picker's title, for the template; the picker sets it |
| `PickerPresenterOptions.ShowsConfirmationButtons` | `PickerPresenterBase` | whether the template shows the bar; the picker sets it |
| `ListPickerFlyoutPresenter.ItemsHost` | `ListPickerFlyoutPresenter` | the template's `ListBox`, for ComboBox's full-screen form (#155) |

## Resource keys

The phone's, in `Tokens/`: `FlyoutBackgroundThemeBrush`, `PhoneForegroundBrush`, `PickerFlyoutContentPanelPortraitThemeMargin`,
`PickerFlyoutContentPanelLandscapeThemeMargin`, `PickerFlyoutTitleThemeMargin`, `FlyoutContentThemeMargin`,
`ListPickerFlyoutPresenterItemMargin`, `ListPickerFlyoutPresenterMultiselectCheckBoxMargin`, `PhoneFontFamilySemiLight`,
`TextStyleExtraLargeFontSize`; the text styles `FlyoutPickerTitleTextBlockStyle` and `TitleTextBlockStyle`; the
CommandBar's `Wp81CommandBarCompactHeight`. Cornflower's, in `Controls/PickerBar.axaml`: `Wp81.Glyph.PickerAccept`,
`Wp81.Glyph.PickerCancel`, `Wp81PickerAcceptGlyphThickness` (3.16), `Wp81PickerCancelGlyphThickness` (3.10),
`Wp81PickerAcceptGlyphTheme`, `Wp81PickerCancelGlyphTheme`. `ListPickerFlyoutFooterThemeHeight` (80) is written into the
list's `Padding`: a `Thickness` can't take a double's key.

## The template's parts

The phone's names: `Background`, `ContentPanel`, `TitlePresenter`, `ItemsHostPanel` (the list picker's). Ours:
`PART_ItemsHost` (the list's `ListBox`), `ScrollViewer` and `PART_ContentPresenter` (the picker flyout's),
`PickerBarSpace`, `PickerBar`, `PART_AcceptButton`, `PART_DismissButton`.

## The ladder

Rung 5 for the four types and `PickerFlyoutBase` (Avalonia has none); rung 1 for their themes; rung 2 for the popup
(`PhonePickerFlyout`, internal to the theme, on #151's `PhonePopupHost`) and `PickerPresenterOptions`.

## Known gaps

- **The list picker's reveal** below its five turning rows takes the Flyout's steps, scaled to the rows below 333 px,
  all of it at 269 ms; #149 read the list picker's fill only as "the background fills downwards", done by about
  367 ms. The rows are 66.7 px slices from the popup's top, not the header's and each item's own heights.
- **`Closed` after accept** comes at 343.8 ms; the phone's PickerFlyout's at 323 (318 after Back): as the Flyout's.
- **The bar is part of the picker**, not a popup of its own: it turns with the picker's bands. The phone's date picker
  draws its bar last, by 400 ms; the list pickers' wasn't read.
- **The bar's labels** are "done" and "cancel", lower case as the shell shows a page's; the open picker bar wasn't
  captured.
- **The check's ink** is 1.1 view px shorter than the phone's; its width and centre match.
- **A tap on the selected item in Single** picks it again (`ItemsPicked` with nothing added or removed): not read.
- **Each opening starts at the top of the list**; whether the phone scrolls to the item picked wasn't read.
- **Selection in Single** isn't drawn, as on the phone; a press tilts the item (the ListView's), not read for the picker.
