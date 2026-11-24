# Preview

What Cornflower looks like today: the pages of the sample app, drawn by the Windows Phone 8.1 theme at the phone's
own size (450 × 800 view pixels), dark and light, the popups open and at rest. Each control the phone had is measured against the Windows Phone 8.1 emulator;
its comparison is in [`docs/controls/`](docs/controls/). The plain themes, for controls it never had, are Cornflower's own design.

The pictures are drawn headless, with Skia, by
[`PreviewGallery`](tests/Cornflower.Tests/Sample/PreviewGallery.cs), so they are the theme's own pixels and can be
drawn again on any OS:

```sh
CORNFLOWER_PREVIEW=/absolute/folder dotnet test tests/Cornflower.Tests --filter FullyQualifiedName~PreviewGallery
```

The sample's pages are test benches, one control each in every state, rather than an app. A page that looks like an
app is still to come.

## The catalogue and the accent

The sample's own list of pages, and its theme page: the accent (Cobalt, the phone's default) and the phone's 20
named accents. An app picks another by setting `SystemColorControlAccentColor`.

<p>
<img src="docs/preview/dark__catalogue.png" width="270" alt="The catalogue, dark">
<img src="docs/preview/light__catalogue.png" width="270" alt="The catalogue, light">
<img src="docs/preview/dark__theme.png" width="270" alt="The theme page: the accent and the 20 named accents">
</p>

## Pivot and Hub

Pivot's header strip scrolls with its items and wraps from the last to the first. Hub's sections pan under a
parallax header and loop.

<p>
<img src="docs/preview/dark__pivot.png" width="270" alt="Pivot, dark">
<img src="docs/preview/light__pivot.png" width="270" alt="Pivot, light">
</p>
<p>
<img src="docs/preview/dark__hub.png" width="270" alt="Hub, dark">
<img src="docs/preview/light__hub.png" width="270" alt="Hub, light">
</p>

## The app bar and the list

The page's app bar (Avalonia's `CommandBar`), with its round buttons and the ellipsis. ListView, Avalonia's `ListBox`
themed, with the phone's tilt on a press and its check mode.

<p>
<img src="docs/preview/dark__commandbar.png" width="270" alt="The app bar, dark">
<img src="docs/preview/light__commandbar.png" width="270" alt="The app bar, light">
</p>
<p>
<img src="docs/preview/dark__listview.png" width="270" alt="ListView, dark">
<img src="docs/preview/light__listview.png" width="270" alt="ListView, light">
</p>

## Flyouts and menus

A Button's Flyout, across the top of the window or filling it (`FlyoutOptions.IsFullScreen`, WinRT's
`Placement="Full"`); each swings in, and a tap below it or Back closes it. A Button's MenuFlyout, under its Button,
the full width of the window.

<p>
<img src="docs/preview/dark__flyout__top.png" width="270" alt="The top Flyout, dark">
<img src="docs/preview/light__flyout__top.png" width="270" alt="The top Flyout, light">
<img src="docs/preview/dark__flyout__full.png" width="270" alt="The full Flyout, dark">
</p>
<p>
<img src="docs/preview/dark__menu-flyout__menu.png" width="270" alt="MenuFlyout, dark">
<img src="docs/preview/light__menu-flyout__menu.png" width="270" alt="MenuFlyout, light">
</p>

## Pickers and dialogs

ListPickerFlyout fills the window: in Multiple with check boxes and the pickers' bar, accept and cancel; in Single a
tap picks. ContentDialog swings in at the top of the window over the dimmed page. LoopingSelector, the date and time
pickers' wheel, here with the month wheel active (the picture is of the day it was drawn).

ComboBox, Avalonia's own themed: with 5 items or fewer it opens inline, the box growing in place; with 6 or more it
opens the full-screen ListPickerFlyout. DatePicker and TimePicker, Avalonia's own themed: closed, the value in the
culture's short form (en-US here, as the phone's); open, the wheels fill the window over the pickers' bar (the date
is the day the picture was drawn). DatePickerFlyout and TimePickerFlyout, shown from any element, are those same open
pickers, so they have no pictures of their own.

<p>
<img src="docs/preview/dark__listpicker__multiple.png" width="270" alt="ListPickerFlyout in Multiple, dark">
<img src="docs/preview/light__listpicker__single.png" width="270" alt="ListPickerFlyout in Single, light">
</p>
<p>
<img src="docs/preview/dark__contentdialog__dialog.png" width="270" alt="ContentDialog, dark">
<img src="docs/preview/light__contentdialog__dialog.png" width="270" alt="ContentDialog, light">
</p>
<p>
<img src="docs/preview/dark__loopingselector__active.png" width="270" alt="LoopingSelector, the month wheel active, dark">
<img src="docs/preview/light__loopingselector__active.png" width="270" alt="LoopingSelector, the month wheel active, light">
</p>
<p>
<img src="docs/preview/dark__combobox__inline.png" width="270" alt="ComboBox open inline, five items, dark">
<img src="docs/preview/light__combobox__full.png" width="270" alt="ComboBox open full screen, seven items, light">
</p>
<p>
<img src="docs/preview/dark__datetimepicker.png" width="270" alt="DatePicker and TimePicker closed, dark">
<img src="docs/preview/light__datetimepicker__date.png" width="270" alt="DatePicker open, light">
</p>

## Toggles, sliders and progress, in three accents

<p>
<img src="docs/preview/dark__toggleswitch.png" width="270" alt="ToggleSwitch, dark, Cobalt">
<img src="docs/preview/light__toggleswitch__lime.png" width="270" alt="ToggleSwitch, light, Lime">
</p>
<p>
<img src="docs/preview/dark__slider__magenta.png" width="270" alt="Slider, dark, Magenta">
<img src="docs/preview/light__slider.png" width="270" alt="Slider, light, Cobalt">
</p>
<p>
<img src="docs/preview/dark__progressring.png" width="270" alt="ProgressRing, dark">
<img src="docs/preview/light__progressbar__lime.png" width="270" alt="ProgressBar, light, Lime">
</p>

## Text and buttons

TextBox; AutoSuggestBox (Avalonia's `AutoCompleteBox` themed), its suggestions hanging from the box; below it the disabled
box (under the list), the light section and, last, so a phone's keyboard covers it, the box with
`AutoMaximizeSuggestionArea` false ("not maximized", #201, #206).

<p>
<img src="docs/preview/dark__textbox.png" width="270" alt="TextBox, dark">
<img src="docs/preview/light__textbox.png" width="270" alt="TextBox, light">
</p>
<p>
<img src="docs/preview/dark__autosuggestbox__list.png" width="270" alt="AutoSuggestBox with its suggestions, dark">
<img src="docs/preview/light__autosuggestbox__list.png" width="270" alt="AutoSuggestBox with its suggestions, light">
</p>
<p>
<img src="docs/preview/dark__button.png" width="270" alt="Button, dark">
<img src="docs/preview/light__button.png" width="270" alt="Button, light">
</p>
<p>
<img src="docs/preview/dark__checkbox.png" width="270" alt="CheckBox, dark">
<img src="docs/preview/light__radiobutton.png" width="270" alt="RadioButton, light">
</p>

## Tier 4's lists, the FlipView and SemanticZoom

The 8.1 ListBox, the list kept for 8.0's apps, as a keyed theme (`Wp81ListBoxTheme`): a pale box with black 12 px
text in both themes, the selection in the accent. The GridView (`Wp81GridViewTheme`): tiles that abut, three a row,
and in the check mode the accent border on every tile and the earmark on a selected one.

<p>
<img src="docs/preview/dark__listbox.png" width="270" alt="ListBox, dark">
<img src="docs/preview/light__listbox.png" width="270" alt="ListBox, light">
</p>
<p>
<img src="docs/preview/dark__gridview.png" width="270" alt="GridView, Single and the check mode, dark">
<img src="docs/preview/light__gridview.png" width="270" alt="GridView, light">
</p>

Grouped lists (`ListBoxOptions.GroupedItemsSource`): each group's header, then its items, an empty group hidden. The
ListView's header pins at its top as it scrolls; the GridView's don't. The sample's grid items are bare text, so in
the grid they run together. SemanticZoom's zoomed-in view, a contacts list grouped a to z; its jump list opens over
the whole window on a tap of a header.

<p>
<img src="docs/preview/dark__grouped-lists.png" width="270" alt="A grouped ListView over a grouped GridView, dark">
<img src="docs/preview/dark__semantic-zoom.png" width="270" alt="SemanticZoom, zoomed in, dark">
</p>

FlipView, Avalonia's `Carousel` themed: the item fills the FlipView less 2.5 a side, with no buttons and no
indicator; above, sideways, and below, up and down.

<p>
<img src="docs/preview/dark__flipview.png" width="270" alt="FlipView, sideways and up and down, dark">
<img src="docs/preview/light__flipview.png" width="270" alt="FlipView, light">
</p>

## Plain themes

For the controls Avalonia has and the phone never had, Cornflower's own design in the era's style
([ADR 0007](docs/decisions/0007-plain-themes.md)): not measured, and labelled so in each README. Batches 1 to 7, 39
controls: the hosts and small pieces, with a popup open in the overlay host's 12-point text; the buttons, with the
DropDownButton's menu open; text and content; the menus, with the menu bar's `file` open and the ContextMenu in the
MenuFlyout's look; the tabs, as the Pivot's headers without its swipe; disclosure and navigation; and the dates. The
dates page follows the day it is drawn (today outlined, a day beside it selected), so its pictures change by the day.

<p>
<img src="docs/preview/dark__plain-hosts.png" width="270" alt="Plain hosts: ItemsControl, Separator, dark">
<img src="docs/preview/light__plain-hosts__popup.png" width="270" alt="Plain hosts, the popup open, light">
</p>
<p>
<img src="docs/preview/dark__plain-buttons.png" width="270" alt="Plain buttons: DropDownButton, SplitButton, ButtonSpinner, NumericUpDown, dark">
<img src="docs/preview/light__plain-buttons__menu.png" width="270" alt="Plain buttons, the DropDownButton open, light">
</p>
<p>
<img src="docs/preview/dark__plain-text.png" width="270" alt="Plain text: Label, HeaderedContentControl, GroupBox, PathIcon, TransitioningContentControl, dark">
<img src="docs/preview/light__plain-text.png" width="270" alt="Plain text, light">
</p>
<p>
<img src="docs/preview/dark__plain-menus__file.png" width="270" alt="Plain menus: the menu bar with file open, dark">
<img src="docs/preview/light__plain-menus__context.png" width="270" alt="Plain menus: the ContextMenu, light">
</p>
<p>
<img src="docs/preview/dark__plain-tabs.png" width="270" alt="Plain tabs: TabControl, TabStrip, TabbedPage, CarouselPage, dark">
<img src="docs/preview/light__plain-tabs.png" width="270" alt="Plain tabs, light">
</p>
<p>
<img src="docs/preview/dark__plain-disclosure.png" width="270" alt="Plain disclosure: Expander, TreeView, SplitView, DrawerPage, dark">
<img src="docs/preview/light__plain-disclosure.png" width="270" alt="Plain disclosure, light">
</p>
<p>
<img src="docs/preview/dark__plain-dates.png" width="270" alt="Plain dates: Calendar and CalendarDatePicker, dark">
<img src="docs/preview/light__plain-dates.png" width="270" alt="Plain dates, light">
</p>

## Not shown

Motion: the tilt under a finger, the turnstile between pages, the Pivot's slide, the Hub's pan and entrance, the
popups' swing and the wheel's coast run on the phone's own measured curves, which still pictures can't show. Each
Hub picture is taken after the entrance's turn. An app can slow the motion, speed it up or turn it off
(`CornflowerConfig`); the phone's is the default. The page moving up for the keyboard (#179, #201, #206) isn't shown
either: the gallery has no keyboard. Nor are the FlipView's swipe, which is motion, and SemanticZoom's jump
list, which the gallery doesn't open.
