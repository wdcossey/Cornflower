# AutoSuggestBox: comparison

**The phone:** #150's screen grabs, Emulator 8.1 U1 1080P 5.5 inch (device 11), 1080 × 1920 at 2.4, run
`20261116-1944-dev11`, in [`ref/wp81/AutoSuggestBox/`](../../../ref/wp81/AutoSuggestBox/README.md):
`new AutoSuggestBox { Header = "auto suggest box", PlaceholderText = "type a fruit" }` at rest, focused from code
(the keyboard up and the page moved up so the control's top is at the window's top), with `Text` "ap", and with 1
("ap"), 3 and 12 ("a") suggestions. Segoe WP.
**Ours:** headless frames from `tests/Cornflower.Tests/Controls/AutoSuggestBoxFrames.cs`, Avalonia 12.1.4 with Skia,
450 × 800 at 2.4, an `AutoCompleteBox` with the same values (the header by `cf:ControlHeader.Header`), in
[`frames/`](frames). Focused, ours is stood where the phone's page moved it (ours doesn't move: a recorded gap), its
caret transparent; no keyboard is drawn, and twelve suggestions stop at the phone's keyboard's top, as the list does
when Avalonia reports that keyboard. Selawik.

Text isn't compared by pixel: the fonts differ by design ([ADR 0001](../../decisions/0001-what-faithful-means.md)).
Sizes, positions and colours are compared by measurement, here and in the tests.

## Dark

| Grab | Phone | Ours |
|---|---|---|
| `rest` | ![](../../../ref/wp81/AutoSuggestBox/dark__AutoSuggestBox__rest__popup__screen.png) | ![](frames/dark__AutoSuggestBox__rest__popup__screen.png) |
| `focused` | ![](../../../ref/wp81/AutoSuggestBox/dark__AutoSuggestBox__focused__popup__screen.png) | ![](frames/dark__AutoSuggestBox__focused__popup__screen.png) |
| `text` | ![](../../../ref/wp81/AutoSuggestBox/dark__AutoSuggestBox__text__popup__screen.png) | ![](frames/dark__AutoSuggestBox__text__popup__screen.png) |
| `list-1` | ![](../../../ref/wp81/AutoSuggestBox/dark__AutoSuggestBox__list-1__popup__screen.png) | ![](frames/dark__AutoSuggestBox__list-1__popup__screen.png) |
| `list-3` | ![](../../../ref/wp81/AutoSuggestBox/dark__AutoSuggestBox__list-3__popup__screen.png) | ![](frames/dark__AutoSuggestBox__list-3__popup__screen.png) |
| `list-12` | ![](../../../ref/wp81/AutoSuggestBox/dark__AutoSuggestBox__list-12__popup__screen.png) | ![](frames/dark__AutoSuggestBox__list-12__popup__screen.png) |

## Light

| Grab | Phone | Ours |
|---|---|---|
| `rest` | ![](../../../ref/wp81/AutoSuggestBox/light__AutoSuggestBox__rest__popup__screen.png) | ![](frames/light__AutoSuggestBox__rest__popup__screen.png) |
| `focused` | ![](../../../ref/wp81/AutoSuggestBox/light__AutoSuggestBox__focused__popup__screen.png) | ![](frames/light__AutoSuggestBox__focused__popup__screen.png) |
| `text` | ![](../../../ref/wp81/AutoSuggestBox/light__AutoSuggestBox__text__popup__screen.png) | ![](frames/light__AutoSuggestBox__text__popup__screen.png) |
| `list-1` | ![](../../../ref/wp81/AutoSuggestBox/light__AutoSuggestBox__list-1__popup__screen.png) | ![](frames/light__AutoSuggestBox__list-1__popup__screen.png) |
| `list-3` | ![](../../../ref/wp81/AutoSuggestBox/light__AutoSuggestBox__list-3__popup__screen.png) | ![](frames/light__AutoSuggestBox__list-3__popup__screen.png) |
| `list-12` | ![](../../../ref/wp81/AutoSuggestBox/light__AutoSuggestBox__list-12__popup__screen.png) | ![](frames/light__AutoSuggestBox__list-12__popup__screen.png) |

## Measurements

Raw pixels, the column at x 540 and the row at y 300 (y 160 at rest), read from both sets of PNGs:

| What | Phone | Ours |
|---|---|---|
| The box at rest: border rows | 116 to 209, `#FFCCCCCC` (both themes) | 116 to 209, `#FFCCCCCC` dark, `#FFCBCBCB` light |
| Focused: the box's top border, inside, bottom border | rows 64 to 69 accent, 70 to 151 white, 152 to 157 accent | the same |
| The list's top border | the box's bottom border, rows 152 to 157 | the same |
| The list's inside from | row 158, white | the same |
| One suggestion: the list's bottom border | rows 275 to 280 (53.75 tall) | the same |
| Three: the bottom border | rows 509 to 514 (151.25 tall) | the same |
| Twelve: the bottom border | rows 1076 to 1081, the keyboard from 1088 (387.5 tall, 2.5 above it) | the same |
| The sides: left border, inside, right border | columns 46 to 51, 52 to 1028, 1029 to 1034 | 46 to 51, 52 to 1027, 1028 to 1033 |
| The rows | 39.17 tall, 48.75 apart, the first 12.08 below the list's top | the same (laid out) |
| The text | black on white, 20.26, in both themes | the same |

## Behaviours

| Behaviour | Phone (#150) | Ours |
|---|---|---|
| Focus | the keyboard comes up; the page moves up 21.67, the control's top to the window's top | the platform's keyboard (Avalonia's, on a phone); the page stays |
| A key | `TextChanged` (`UserInput`) 150 to 177 ms after `KeyDown`; the app sets `ItemsSource`, the list opens | `TextChanged` at once; `Populating` 150 ms after, the app sets `ItemsSource`, the list opens |
| A tap on a suggestion | `SuggestionChosen` (`Text` still typed), the focus lost and regained in one frame, `TextChanged` (`SuggestionChosen`) 162 ms later, the list closed, the keyboard stays | the text set and `TextChanged`, `SelectionChanged`, the list closed, the focus kept |
| Enter | `KeyDown` only | with the list open, Avalonia's commit closes it |
| `Text` from code | `TextChanged` (`ProgrammaticChange`), the list stays shut | `TextChanged`, the list stays shut |
| Back | hides the keyboard, closes the list, takes the focus, in one frame; the next reaches the app | closes the list, takes the focus (and so the keyboard); the next reaches the page. On Android the keyboard's own Back comes first, and its going down does the same |
| A tap on the box, the list open | not read | the tap goes to the box (its caret); the list stays |
| A tap off the box and list | the focus, the keyboard and the list go | the list goes (Avalonia's light dismiss); the focus stays where the tap leaves it (unmeasured whether the tap reaches the page) |

## Known gaps

- **The page doesn't move up** under the keyboard (Avalonia has no such pan); the list still takes the room down to
  the keyboard from wherever the box is.
- **The right edge is one raw pixel short** (ADR 0003's rounding, as the TextBox's).
- **At rest in light the box is 203 grey, the phone's 204**: Skia's blend, as the TextBox's.
- **The events**: no reason on `TextChanged`, `SelectionChanged` after the text is set, Enter closes the list (the
  plan's API table).
- **Unmeasured**: the list's open and close motion, landscape (the list upward), a tap on the box with the list open.
- **The text is Selawik** (ADR 0001): its baseline about 2 raw pixels above Segoe WP's in the rows.

## The checklist

In [PLAN.md](PLAN.md#checklist): 28 rows done or kept as Avalonia's, 30 left out with their reasons.
