# ItemClick and IsItemClickEnabled: plan

[#132](https://github.com/wdcossey/CornflowerUI/issues/132). WinRT's ListView raises `ItemClick` when
`IsItemClickEnabled` is on. Avalonia's `ListBox` has neither. Both come as an attached behaviour on `ListBox`, with
WinRT's names, in `Cornflower.Controls`: `ListBoxOptions.IsItemClickEnabled` and `ListBoxOptions.ItemClick`, with
`ItemClickEventArgs.ClickedItem`. The ListView's look is #119's ([ListView](../ListView/README.md)); this adds only
what a tap does.

## Where the behaviour comes from

The reference is #113's press probes (`-Probe listpress`), runs `20261115-1941-dev11` (12 items) and
`20261115-1948-dev11` (40, so the list pans), on the reference profile (device 11, 1080 × 1920 at 2.4):
[`tools/refapp/README.md`](../../../tools/refapp/README.md), "ListView: an item's press". The probe's ListView has
`IsItemClickEnabled="True"` and the default `SelectionMode`, Single. It logs `ItemClick`, `SelectionChanged`, the
pointer events and every visual state group of each item. Read from the two `probe.txt` files:

| What the finger did | `ItemClick` | `SelectionChanged` | Item's `SelectionStates` |
|---|---|---|---|
| 10 presses held 600 ms (centres of items 1 to 3; 5%, 25%, 75% and 95% across item 2; 10% and 90% down it), in each run | one each, for the item under the finger, with the release | none | no change |
| 5 taps on item 4 (80 ms three times, 150 ms twice), in each run | one each, with the release | none | no change |
| A press held 300 ms, then dragged 80 px up (the list pans) or down; flicks | none: the list took the pointer about 50 ms into the move | none | no change |
| Dragged off the item where the list can't pan (12 items) | none | none | no change |
| A press moved 10 px sideways and let go | one | none | no change |

So **on the phone, in Single mode with item click on, a tap clicks and selects nothing**: 30 clicks, no selection.
`ItemClick` is logged before the list's own `PointerReleased`, as the item raises it in its release, and `Tapped`
after. The press tilts as any press does (#119).

WinRT's later documentation for WinUI says that with `IsItemClickEnabled` and a `SelectionMode` of Single, Multiple or
Extended, `ItemClick` is raised and then `SelectionChanged`. That isn't what the 8.1 phone does in Single mode (above),
and the phone is the reference.

Read since, in #148 (`-Probe listclick`, run `20261116-1717-dev11`, `tools/refapp/README.md`, "ListView: `ItemClick`
in each `SelectionMode`"): with item click on, every tap clicks and nothing selects, in every mode. In `Multiple`, the
check mode with its boxes shown, a tap on the box or on the text clicks the item and leaves it unchecked, and no
`SelectionChanged` comes; a press on the box tilts the box alone, as in #136. `Extended`, `None` and `Single` click
and select nothing. Not probed: the keyboard (the phone has none). Below is what we do for each.

## Avalonia's ListBox against the phone's ListView

| | Phone | Avalonia 12.1.4 | Ours |
|---|---|---|---|
| `IsItemClickEnabled` | a property of ListViewBase, False by default | none | `ListBoxOptions.IsItemClickEnabled`, attached, False by default |
| `ItemClick` | an event of ListViewBase, `ItemClickEventArgs.ClickedItem` | none | `ListBoxOptions.ItemClickEvent`, a direct routed event raised on the list, `ItemClickEventArgs.ClickedItem` the item (the data) |
| A tap, Single mode, item click on | `ItemClick` with the release; no selection | selects on a touch's release | clicks with the release; selects nothing |
| A pan; a release off the item | no `ItemClick` | the scroll gesture takes the pointer; a release off the item selects nothing | clicks nothing (Avalonia's rule, below) |
| The mouse | `ItemClick` with the left button's release (as the touch) | selects on the press, left or right | the press selects nothing; the left button's release on the item clicks |
| Space and Enter on the focused item | not read (no keyboard) | select it, on the press | click it once, as the key is let go; select nothing |
| A control in an item that takes the press (a TextBox, a plain list) | no `ItemClick` | it takes the press; the item selects on a touch's release | nothing clicked or selected |
| A Button in an item | the Button's click, no `ItemClick` | the Button takes the press; nothing selected | the same: the Button clicks, the item doesn't |
| Multiple (the check mode) | `ItemClick`; nothing checked, on the box or off it (#148); a press on the box tilts the box alone | `Multiple, Toggle`: a tap toggles | clicks; checks nothing (#169); the box's press as before |
| Extended | `ItemClick`; nothing selected (#148) | `Multiple`, no check boxes (#142) | as Single: a tap clicks and selects nothing, Ctrl+A selects nothing (#145) |
| None | `ItemClick` alone (#148) | no such mode | Single with item click on is the same: no tap selects |
| Arrow keys | not read | move the selection | Avalonia's (a gap) |

### How a click is told

Avalonia's item selects in its own `OnPointerPressed`, `OnPointerReleased` and `OnKeyDown`, through
`SelectingItemsControl.UpdateSelectionFromEvent`, on the events `ItemSelectionEventTriggers.ShouldTriggerSelection`
accepts: a touch or a pen let go within the item, a mouse button (left or right) pressed on it, or Space or Enter
from the focused item. An event that arrives handled selects nothing.

So the behaviour **takes those events**: with item click on, in any mode, each event Avalonia's
item would select on is marked handled just before it reaches the item, and nothing is selected. Avalonia marked those
same events handled when it selected, so an app's handler on the item or the list sees what it saw before.

A click is a touch or the mouse's left button let go within the item, by the pointer whose press reached the item
unhandled; or Space or Enter pressed and let go on the focused item. The touch's rule is Avalonia's for selecting, so
a tap clicks exactly where it used to select: a pan takes the pointer from the item, so its release never reaches the
item; a release off the item is outside its bounds; a press moved 10 px is still inside them. The mouse clicks on its
release, as a Button does, where Avalonia selected on its press. A press something in the item took (a Button, a
TextBox, a plain list's item) clicks nothing when it is let go, whichever the pointer. A press is forgotten when its
pointer is taken from the item (a pan) or the press becomes a hold (Avalonia's `Holding`, which opens a context menu).
An event from a popup that a control in the item owns is routed through the item but isn't on it, so it is left
alone. A key clicks as it is let go, so a held key, whose press repeats, clicks once; a key is forgotten when the
item loses the focus, and a key on a control inside the item is that control's. A click gives the platform's click feedback by the item's
`PlatformFeedback.FeedbackType`, as Avalonia's selection and Button do.

Where it takes them: the item's class handler is the first on the item to see an event on its way up, and nothing
outside the item can come before it there. So the behaviour's class handlers for the pointer act on the item's
template root, after everything inside the item has seen the event (a Button in the item takes its press first, and
the press arrives handled). A key's source is the focused item itself, so its handlers act on the key's way down, at
the item, where nothing inside the item comes after it. The handlers are registered once, when `ListBoxOptions` is
first used. The pointer's run on every element of a press's and a release's route, so they return at once while no
list has item click on (a count of the lists that have).

Limits: the root's own handlers come after the class handler, so they don't see an event that is taken; a `ListBox`
subclass that overrides its protected `ShouldTriggerSelection` isn't followed, as the rule read is Avalonia's default
(`ItemSelectionEventTriggers`).

A pointer an app captures to the `ListBoxItem` itself sends its release from the item, past the template root: that
release selects, as Avalonia's, and clicks nothing (a limit; taking it on the way down would have to come after the
item's own pressed state, which a class handler can't be sure of).

## Extensions

None: both are WinRT's API. `ListBoxOptions` holds them because Avalonia's `ListBox` has no place for them, as
`CommandBarOptions` holds `ClosedDisplayMode`.

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/ItemClickTests.cs` unless named.

1. **A tap tilts, then clicks the item on its release and selects nothing**, the tilt returning after.
   `A_Tap_Tilts_Then_Clicks_The_Item_On_Its_Release_And_Selects_Nothing`.
2. **Each probed press clicks its item**: the probes' ten held presses, each the item under the finger, none
   selecting. `Each_Probed_Press_Clicks_Its_Item_And_Selects_Nothing`.
3. **Off, a tap selects and clicks nothing**, as before. `Off_A_Tap_Selects_And_Clicks_Nothing`; turned off again, the
   list selects again, `Turned_Off_The_List_Selects_Again`.
4. **A selection set in code stands**: a tap leaves it. `A_Selection_Set_In_Code_Stands`.
5. **A pan clicks nothing** (the 40-item probe). `A_Pan_Clicks_Nothing`.
6. **Dragged off the item, a press clicks nothing** (the 12-item probe). `Dragged_Off_The_Item_Clicks_Nothing`.
7. **A press moved 10 px sideways still clicks.** `A_Press_Moved_Ten_Pixels_Sideways_Still_Clicks`.
8. **In the check mode a tap clicks and checks nothing** (#169), `Multiple, Toggle`, with or without `AlwaysSelected`,
   on the box and off it, with no `SelectionChanged`, and a press on the box still presses the box,
   `In_The_Check_Mode_A_Tap_Clicks_And_Checks_Nothing`; Single's other flags keep item click,
   `Without_Multiple_A_Tap_Clicks`; and `Multiple` without `Toggle`, WinRT's Extended, clicks as Single does (#145),
   `Multiple_Without_Toggle_A_Tap_Clicks`, by the mouse and the keys too, Ctrl+A selecting nothing there and in
   the check mode,
   `Multiple_Without_Toggle_The_Mouse_And_Keys_Select_Nothing`, while a TextBox's own Ctrl+A in an item is the
   TextBox's, `Select_All_In_A_TextBox_In_An_Item_Is_The_TextBoxs`.
9. **The mouse clicks on the left button's release and selects nothing**, the item pressed meanwhile; the right button clicks nothing.
   `A_Mouse_Clicks_On_The_Left_Buttons_Release_And_Selects_Nothing`; let go off the item, nothing,
   `A_Mouse_Let_Go_Off_The_Item_Clicks_Nothing`; a release with no press on the item, nothing,
   `A_Release_Without_A_Press_On_The_Item_Clicks_Nothing`; a press whose pointer is taken, nothing,
   `A_Press_Whose_Pointer_Is_Taken_Clicks_Nothing`; a release something in the item took forgets the press,
   `A_Release_Something_In_The_Item_Took_Forgets_The_Press`; the right button clicked while the left is held, nothing,
   `A_Right_Click_While_The_Left_Is_Held_Clicks_Nothing`; with Avalonia's hold on the mouse, which moves its selection to the
   release, neither button selects, `With_Hold_On_The_Mouse_Neither_Button_Selects`.
10. **Space and Enter click the focused item once, as they are let go**, a held key's repeats included.
    `Space_And_Enter_Click_The_Focused_Item_Once_As_They_Are_Let_Go`; a key let go without its press, nothing,
    `A_Key_Let_Go_Without_Its_Press_Clicks_Nothing`; pressed before the focus left, nothing,
    `A_Key_Pressed_Before_The_Focus_Left_Clicks_Nothing`; on a Button in the item, the Button's,
    `A_Key_On_A_Control_In_The_Item_Is_The_Controls`.
11. **A Button in an item takes its own tap.** `A_Button_In_An_Item_Takes_Its_Own_Tap`; a press something in the item
    took clicks nothing, the mouse's and a touch's, `A_Press_Something_In_The_Item_Took_Clicks_Nothing`, and forgets
    an earlier press whose release never came back, `A_Press_Something_In_The_Item_Took_Forgets_An_Earlier_Press`.
12. **A list in an item clicks its own item alone**; the outer list hears nothing.
    `A_List_In_An_Item_Clicks_Its_Own_Item_Alone`. **A tap in a popup over the item clicks nothing**,
    `A_Tap_In_A_Popup_Over_The_Item_Clicks_Nothing`; **nor does a hold**, `A_Hold_Clicks_Nothing`.
13. **Behaviour only**: under Simple's ListBox it clicks too. `It_Works_Under_Another_Theme`.
14. **An item that is its own container is clicked as itself.**
    `An_Item_That_Is_Its_Own_Container_Is_Clicked_As_Itself`.
15. **The sample's page** (`Sample/ItemClickPageTests.cs`): in the catalogue after the ListView's, both lists
    clicking in the phone's look, its header, a tap's click shown, and "select" turning item click off and putting the
    list in the check mode, and taking it out with the checks cleared and item click back on.

## Decided on the user's behalf

- **With item click on, a tap selects nothing**, against the WinUI documentation's "both". The 8.1 Update 1 phone,
  the reference, selected nothing on 30 taps in Single mode (above), and none in any mode in #148. To reverse: let the
  events through in `ListBoxOptions.OnPressed` and `OnReleased` after raising the click.
- **In the check mode, Ctrl+A selects nothing** (#169). #132 decided, without a probe, that a tap in the check mode
  (`Multiple` and `Toggle`, WinRT's Multiple) checks and clicks nothing; #148 read the phone: it clicks, on the box or
  off it, and leaves the item unchecked, so that is what we do now. The keys follow the taps, so that no key selects
  in a list that clicks; the phone has no keyboard to read. To reverse: let the select-all keys through in
  `ListBoxOptions.OnListKeyDown` when the list's `SelectionMode` has both `Multiple` and `Toggle`.
- **`Multiple` alone (WinRT's Extended) isn't the check mode: with item click on, a tap clicks and selects nothing, as
  in Single** (#145, following #142). #132 took any mode with `Multiple` as the check mode, when the theme showed the boxes for it too.
  #136 read the phone's Extended: no boxes, and a tap is Single's; #142 took the boxes away. Kept as before, a tap
  there would select an item that draws nothing and click nothing: a dead tap. The phone's Extended with item click
  wasn't read; Single's, the nearest read, clicks. Ctrl+A, which Avalonia's ListBox takes to select every item in
  any mode with `Multiple`, is taken from a list that clicks, from the list or its own items (not a control inside
  an item), so nothing selects there either. This is WinRT's rule, so it holds under any theme, where a `Multiple`
  selection might draw: under item click an app's Extended list clicks, as WinRT's does. #148 read the phone's
  Extended with item click since: a tap clicks and selects nothing, as here.
- **WinRT's `None` has no mode of its own.** Single with item click on is the same to the user: no tap, click or key
  selects. A selection set in code still shows (it draws nothing in a ListView outside the check mode, #119).
  To reverse: nothing to undo; a None mode would need a ListBox subclass.
- **The mouse clicks on the left button's release, and its press selects nothing**, as a touch, and as WinRT's
  Button. The right button's press selects nothing either (Avalonia's selects the item under a context menu);
  its release is left alone, as before. To reverse: let the press through in `ListBoxOptions.OnPressed`.
- **Space and Enter click the focused item, as they are let go.** Avalonia's item selects on them; left as they
  were, a key would select an item a tap can't. On the release, so a held key clicks once (Avalonia's Button clicks
  on Enter's press, and repeats). The phone has no keyboard to read. To reverse: drop the key handlers.
- **Arrow keys move the selection as Avalonia's ListBox does** (a gap): changing them needs the ListBox's own key
  handling.
- **`ItemClick` is a direct routed event**, raised on the list alone, as WinRT's is a plain event of the list: a
  list in an item of a list doesn't reach the outer list's handler with an item of another type (the first review's
  finding; it bubbled at first). To reverse: register it `Bubble`.
- **A click gives the platform's click feedback**, as Avalonia's selection and Button do, by the item's
  `PlatformFeedback.FeedbackType` (Auto on a `ListBoxItem`, Avalonia's). The 8.1 phone wasn't read for it, and a
  headless test can't reach it (owed: the device run). To reverse: drop `PerformFeedback`.
- **The class is `ListBoxOptions`**, Avalonia's type and "Options", as `CommandBarOptions` is, and its members WinRT's
  names. In XAML: `cf:ListBoxOptions.IsItemClickEnabled="True" cf:ListBoxOptions.ItemClick="OnItemClick"`.
- **Its own sample page, `itemclick`**, not a specimen on the listview page, which is #119's.

## Dependencies and captures

- #119 ListView (merged as #130): the look, the tilt, the check mode. #113's probes. No capture: item click has no
  look of its own.
- **Owed:** a probe of `IsItemClickEnabled` with `SelectionMode` Multiple, Extended and None (#136 read Extended and
  None without item click); the device run on the Pixel 8.

## Checklist

From the ListView's checklist ([PLAN](../ListView/PLAN.md#listview)), the two rows this does:

| | Member | Type | Avalonia | Notes |
|---|---|---|---|---|
| [x] | `IsItemClickEnabled` | `Boolean` | `ListBoxOptions.IsItemClickEnabled` | attached on `ListBox`, False by default |
| [x] | `ItemClick` | `ItemClickEventHandler` | `ListBoxOptions.ItemClickEvent` | a direct routed event on the list; `ItemClickEventArgs.ClickedItem` |
