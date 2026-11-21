# Plain themes: the plan (#216)

**Status:** a draft for the maintainer to pick from (2026-11-21). Nothing here is decided; the recommendations are marked
as such. Each batch the maintainer picks gets its own issue.

**Question.** Avalonia has templated controls the phone never had, and today they fall back to Simple. Which are they, what
would an 8.1 app have used instead, what does "plain, in the era's style, labelled as ours" mean, and in what order should
they be themed?

**Sources.** `tests/Cornflower.Tests/Completeness/Holes.txt` (49 holes at Avalonia 12.1.4, held by `HolesTests`); the theme
files in `src/Cornflower.Themes.Wp81/Controls/` and `Tokens/`; `HoleParts.cs`; the file lists of Avalonia's Fluent and
Simple themes at the `12.1.4` tag, and of the ColorPicker's Simple theme there; the DataGrid's `Themes/Simple.xaml` in
its own repository (`AvaloniaUI/Avalonia.Controls.DataGrid`); ADRs 0001, 0004 and 0006. No Avalonia theme source was read
beyond its list of `ControlTheme` keys.

## 1. The list

**Scope.** The 49 holes in `Holes.txt`, which are every concrete `TemplatedControl` in the `Avalonia` package's assemblies
that Simple themes by type and Wp81 doesn't, plus the free first-party packages the brief names: `Avalonia.Controls.ColorPicker`
(versioned with Avalonia, 12.1.4 exists) and `Avalonia.Controls.DataGrid` (MIT, in a repository of its own and versioned
apart: its latest release is 12.1.2). The repo references neither, so `Holes.cs` doesn't count them (its remarks say a
package's controls join the count when the package is referenced). That makes **60 controls**. Fluent's file list at
12.1.4 is Simple's plus `MenuScrollViewer`, a named theme, so it adds no hole. Simple's `AdornerLayer`,
`SelectableTextBlock`, `ThemeVariantScope` and `WindowDrawnDecorations` aren't counted by `Holes.cs` (it counts only
concrete `TemplatedControl`s that Simple themes by type). `TreeDataGrid` is left out: the repo doesn't reference it and
its licence wasn't checked.

**Sizes.** *Small*: brushes and type, with a short template or none. *Medium*: a template of its own. *Large*: a template
plus behaviour, code of Cornflower's beside it. Avalonia's own behaviour (a `Calendar`'s keyboard, an `Expander`'s toggle)
doesn't count as ours.

**8.1 used.** What an 8.1 app would have reached for: a Cornflower control, a pattern, or nothing.

### Hosts and small pieces

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `Window` | The app's frame: the page background and the phone's font | Small | Every desktop head shows one. Background, foreground, font family and size from the tokens |
| `EmbeddableControlRoot` | The same | Small | Avalonia's root for embedded views. Whether the Android head's view uses it is unchecked |
| `PopupRoot` | Nothing of its own: Wp81's flyouts draw their own surface | Small | Transparent, with the phone's foreground and font |
| `OverlayPopupHost` | The same | Small | As `PopupRoot` |
| `ItemsControl` | `ItemsControl`, which has no default style on the phone (ROADMAP, the phone's types) | Small | A bare `ItemsPresenter`. Wp81 has `ItemsControl` themes only by key (`Wp81CommandBarItemsTheme`) |
| `Separator` | `MenuFlyoutSeparator`, `AppBarSeparator` | Small | A line in the 1.5 thickness. `Wp81MenuFlyoutSeparatorTheme`, keyed, is the phone's inside a menu |
| `PathIcon` | `PathIcon`, an untemplated icon element on the phone; in Cornflower, `LucideIcon` | Small | Foreground only, no fixed size |
| `Label` | A `TextBlock` in `ControlHeaderTextBlockStyle` | Small | A label names a control, which is what the phone's control header is |
| `HeaderedContentControl` | A control header (`ControlHeader`, `HeaderContentPresenter`) | Small | Reuses the header's look |
| `GroupBox` | A pattern: a heading in `GroupHeaderTextBlockStyle` over the content, no box | Small | |
| `ToolTip` | Nothing: in the phone's metadata with no default style; touch has no hover | Small | A flyout's surface and 16 text. Seen only with a pointer |
| `GridSplitter` | Nothing | Small | A bar; the accent while dragged |
| `TransitioningContentControl` | A `Frame`'s page transitions | Small | The template is two presenters; the motion is a question ("For the user to pick") |

### Buttons and fields

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `DropDownButton` | A `Button` with a `Flyout` or `MenuFlyout` | Medium | The Button's look plus a chevron |
| `SplitButton` | Nothing (Windows 10's) | Medium | Two pressable parts, one border |
| `ButtonSpinner` | Nothing | Medium | Two `RepeatButton`s; the shared parts (section 3) |
| `NumericUpDown` | A `TextBox` with the number keyboard (`InputScope` Number) | Medium | Wp81's TextBox inside, with `ButtonSpinner` |
| `DataValidationErrors` | Nothing: a pattern of a line of text under the field | Medium | Needs an error colour; a question ("For the user to pick") |

### Menus

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `ContextMenu` | A `MenuFlyout` opened on hold (`FlyoutBase.ShowAttachedFlyout`) | Large if it takes the MenuFlyout's look and motion, medium if plain | Avalonia can open one on a long press (a touch hold), so a phone app on Android may meet it. A question ("For the user to pick") |
| `Menu` | Nothing: the `CommandBar` and its secondary commands | Medium | A desktop menu bar |
| `MenuItem` | `MenuFlyoutItem` (Wp81's, keyed: `Wp81MenuFlyoutItemTheme`) | Medium | The biggest template here: sub-menus, icon, check, gesture text. Shared by `Menu` and `ContextMenu` |

### Tabs and pages

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `TabControl` | `Pivot` | Medium | A question: the Pivot's headers without its swipe, or a plain strip ("For the user to pick") |
| `TabItem` | `PivotItem` and its header | Medium | Also inside `ColorView`'s template |
| `TabStrip` | The Pivot's header row | Small | |
| `TabStripItem` | `PivotHeaderItem` | Small | |
| `TabbedPage` | `Pivot` | Medium | Avalonia 12's page; reuses the tab pieces |
| `CarouselPage` | `FlipView`, or a `Pivot` without headers | Medium | Wp81 already themes `Carousel` |
| `DrawerPage` | Nothing (the hamburger came with Windows 10) | Medium | Its template holds plain `ToggleButton`s (`HoleParts.cs`) |
| `SplitView` | Nothing | Medium | A pane over or beside the content |
| `PipsPager` | Nothing | Medium | Its template holds plain `Button`s (`HoleParts.cs`) |

### Lists, trees and tables

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `Expander` | Nothing: a pattern of a tappable header that shows more | Medium | Header, chevron, content |
| `TreeView` | A grouped `ListView`, `SemanticZoom`, or a page per level | Medium | |
| `TreeViewItem` | A `ListViewItem` | Medium | Indent, chevron, selection |
| `TableView` | A `ListView` whose item template is a `Grid` | Medium | Avalonia 12's own table; not read further |
| `TableViewRow` | The same | Medium | |
| `TableViewCell` | The same | Small | |
| `TableViewColumnHeader` | Nothing | Small | |
| `DataGrid` (package) | The same as `TableView` | Medium | Cells edit through a `TextBox` and `CheckBox` |
| `DataGridRow` (package) | The same | Medium | |
| `DataGridCell` (package) | The same | Small | |
| `DataGridColumnHeader` (package) | Nothing | Medium | The sort glyph |
| `DataGridRowHeader` (package) | Nothing | Small | |
| `DataGridRowGroupHeader` (package) | A `ListViewHeaderItem` (Cornflower has one) | Medium | |

### Dates

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `Calendar` | `DatePicker` (Wp81 themes it) | Medium | |
| `CalendarItem` | The same | Medium | Its template holds plain `Button`s (`HoleParts.cs`) |
| `CalendarButton` | The same | Small | |
| `CalendarDayButton` | The same | Small | |
| `CalendarDatePicker` | `DatePicker` | Medium | Its template holds a plain `TextBox` (`HoleParts.cs`) |

### Colour

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `ColorPicker` (package) | Nothing; the pattern is the phone's own accent picker, a grid of swatches | Medium | |
| `ColorView` (package) | The same | Medium | Its template uses keyed `TabItem`, `RadioButton`, `ListBox` and `ListBoxItem` themes |
| `ColorSpectrum` (package) | Nothing | Small | |
| `ColorSlider` (package) | `Slider` (Wp81 themes it) | Small | A `Thumb` inside |
| `ColorPreviewer` (package) | Nothing | Small | |

### Feedback and the rest

| Control | 8.1 used | Size | Notes |
|---|---|---|---|
| `NotificationCard` | The system toast: a banner across the top (its look unmeasured) | Medium | |
| `WindowNotificationManager` | The same | Small | The host the cards stack in |
| `RefreshContainer` | Nothing built in; apps built pull-to-refresh on a `ScrollViewer` | Medium | Seen on touch, so on Android |
| `RefreshVisualizer` | The same | Medium | A question: Wp81's `ProgressRing` or the ProgressBar's dots as its sign ("For the user to pick") |
| `TextSelectionHandle` | The phone's own selection grippers | Medium | **Not a plain theme**: the phone had these, so it's a reproduction. `docs/controls/TextBox/COMPARISON.md` lists the handles as unmeasured. Shown on touch selection, so on Android |
| `ManagedFileChooser` | The platform's picker (`FileOpenPicker`) | Large | Avalonia's own file dialog, for a platform without a native one. Its template holds plain `Button`s, `TextBox` and `CheckBox` |
| `ManagedFileChooserOverwritePrompt` | The same | Medium | |

### The count

| Size | Count |
|---|---|
| Small | 25 |
| Medium | 33 |
| Large | 2 (`ContextMenu` with the phone's look; `ManagedFileChooser`) |
| **All** | **60**: 49 holes, 6 DataGrid, 5 ColorPicker |

Of the 60, one (`TextSelectionHandle`) belongs in a reproduction, not here, and two (the file chooser and its prompt)
are recommended to drop.

## 2. The era's style: the rule set

A plain theme is Cornflower's design. It takes the phone's existing tokens and rules where they fit, and claims nothing
measured. **Recommended** throughout; each rule that's open is in "For the user to pick".

1. **Labelled as ours.** Each plain theme's README (`docs/controls/<Name>/README.md`) opens with one line: "Plain theme:
   Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone." The theme file
   says the same in its header comment. A new resource key a plain theme needs is prefixed **`CornflowerPlain`**
   (`CornflowerPlainErrorBrush`), never a WinRT-shaped name (`...ThemeBrush`, `...ThemeThickness`), which in this repo
   means "the phone's value". A keyed theme for a part is named **`Wp81Plain<Part>Theme`**, beside Wp81's
   `Wp81<Part>Theme`s. The theme by type stays keyed by type, so it fills the hole and `HolesTests` sees it.
2. **Type ramp: existing keys only.** Content in `BodyTextBlockStyle`'s size and family (18.14, `PhoneFontFamilyNormal`);
   a control's own header in `ControlHeaderTextBlockStyle` (16, `PhoneMidBrush`); a group's heading (`GroupBox`,
   `Expander`'s header, a tree's or table's group) in `GroupHeaderTextBlockStyle` (23.73, SemiLight); small print (an
   error, a column header) in `ControlContextualInfoTextBlockStyle` (16, mid); a tab's header in the Pivot header's keys
   (`PivotHeaderItemFontSize`, `PivotHeaderItemFontFamily`). Sizes reached by `DynamicResource`, so the Adaptive pack can
   change them (ADR 0004's hooks).
3. **Accent, as the phone uses it.** For what is pressed, selected, checked, on, in progress or a link: a pressed Button's
   fill with `PhoneTextOverAccentColor` text, a selected item's text, a checked box, a dragged splitter. Never as
   decoration at rest: no accent borders, headings or backgrounds on a control nobody is touching. Always
   `PhoneAccentBrush`, which follows `SystemColorControlAccentColor`.
4. **Borders: the phone's two thicknesses, square.** `PhoneBorderThickness` (2.5) round a box a finger presses or types in
   (spinner buttons, a split button, a field); 1.5 (the AppBar's and MenuFlyout's edge thickness) for a line that divides
   or edges a surface (a separator, a table's rule, a pane's edge). Corner radius 0, no shadows, no gradients.
5. **Backgrounds.** Transparent at rest, over the page's `PhonePageBackgroundBrush`. A surface that floats (a tooltip, a
   menu, a notification) takes the flyout's (`FlyoutBackgroundThemeBrush`) or the chrome's (`PhoneChromeBrush`). Disabled
   is `PhoneDisabledBrush`. Nothing translucent.
6. **Touch sizes stay the phone's.** Anything pressed keeps the phone's minimums and overhang (`PhoneButtonMinHeight`,
   `PhoneTouchTargetOverhang`) unless it is a part inside a larger control (section 3). No phone width is written into a
   template (ADR 0004).
7. **The tilt on presses of item-sized things.** `Tilt.IsEnabled` on a button-like part, a tree or menu item, a tab
   header, a day in a calendar: the era's press feedback, at no new cost. None on large surfaces (a pane, a whole grid),
   and none on what is dragged (a splitter, a thumb).
8. **Motion: none of their own.** Simple's transitions are left out. A plain theme that opens a popup uses whatever Wp81's
   popup host already does with it. Any motion a plain theme adds later reads its time through `CornflowerClock` under an
   ADR 0006 area (popups, pages or controls), so `CornflowerConfig.Animated` stops it.
9. **Dark and light.** Every brush from the `Dark` and `Light` dictionaries in `Tokens/Colors.axaml`; no colour written
   in a theme. `Holes.cs` already counts a theme in one variant only as a hole.
10. **Glyphs.** Chevrons, arrows, checks and the sort mark from `LucideIcon` or the theme's `Glyphs`, our own drawings as
    ADR 0001 item 3 asks. No new font.
11. **Done, for a plain theme.** There is no reference to measure, so: the theme's tests (templated, both variants, the
    key prefix, HoleParts' list updated), a frame in the preview, and a look on the Pixel with no timings. Its line leaves
    `Holes.txt`, as for any control.

**New tokens.** None for the rules above. One probably needed: an error colour for `DataValidationErrors` ("For the user to pick").
`MenuItem` in a menu bar and the denser controls may want a smaller row height; if so, each is a `CornflowerPlain` key
with its reason in the batch's PR.

## 3. Shared pieces

What several batches lean on, which is why the order below puts them early.

- **The parts inside a hole's template.** `HoleParts.cs` keeps Wp81's Button, ToggleButton, RepeatButton, CheckBox,
  TextBox and ContentControl out of templates written for the theme *under* Wp81: today Simple's `CalendarItem`,
  `CalendarDatePicker`, `PipsPager`, `DrawerPage`, `NumericUpDown`, `DataValidationErrors`, `MenuItem`, the menu's `ScrollViewer` and the file
  dialogs hold such parts. Once a hole gets a Wp81 theme, its template is Wp81's, the fallback stops, and those parts take
  the phone's Button (109 by 57.5 at the least) unless the plain theme names its own. So the first batch with inner
  buttons makes `Wp81Plain` part themes (a borderless glyph button, a spinner `RepeatButton`, a header `ToggleButton`)
  for the rest to reuse, and each batch's PR updates `HoleParts.cs`'s list and its tests.
- **The tab pieces.** `TabItem` and `TabStripItem` serve `TabControl`, `TabStrip`, `TabbedPage` and `ColorView`.
- **The menu item.** `MenuItem` serves `Menu` and `ContextMenu`; Wp81's keyed `Wp81MenuFlyoutItemTheme` is the start.
- **The row.** A tree's, a table's and a grid's row share the selection, the press and the rule; `TreeViewItem` sets them
  first.

## 4. Batches

Most used first, shared pieces first. Batch 1 is the **recommended first batch**.

| # | Batch | Controls | Why here |
|---|---|---|---|
| **1** | **Hosts and small pieces (recommended first)** | `Window`, `EmbeddableControlRoot`, `PopupRoot`, `OverlayPopupHost`, `ItemsControl`, `Separator` | Every app meets a host. All small, so the batch sets the label, the key prefix and its test on cheap themes |
| 2 | Buttons and the shared parts | `DropDownButton`, `SplitButton`, `ButtonSpinner`, `NumericUpDown`, `GridSplitter` | Makes the `Wp81Plain` part themes the later batches reuse |
| 3 | Text and content | `Label`, `HeaderedContentControl`, `GroupBox`, `PathIcon`, `ToolTip`, `TransitioningContentControl` | Small; settles the type ramp's use and the motion question |
| 4 | Menus | `MenuItem`, `ContextMenu`, `Menu` | `ContextMenu` opens on a long press on Android; `MenuItem` first, for both |
| 5 | Tabs and pages | `TabItem`, `TabStripItem`, `TabStrip`, `TabControl`, `TabbedPage`, `CarouselPage` | Avalonia 12's pages, which an app for phones may pick; the tab pieces first |
| 6 | Disclosure and navigation | `Expander`, `TreeView`, `TreeViewItem`, `SplitView`, `DrawerPage` | The row (`TreeViewItem`) for batch 9 |
| 7 | Dates | `CalendarDayButton`, `CalendarButton`, `CalendarItem`, `Calendar`, `CalendarDatePicker` | One family; uses batch 2's parts |
| 8 | Feedback | `DataValidationErrors`, `NotificationCard`, `WindowNotificationManager`, `RefreshContainer`, `RefreshVisualizer`, `PipsPager` | Pull-to-refresh is touch; the error colour is decided here |
| 9 | Tables | `TableViewCell`, `TableViewColumnHeader`, `TableViewRow`, `TableView` | Avalonia's own table, on batch 6's row |
| 10 | DataGrid (package) | `DataGridCell`, `DataGridColumnHeader`, `DataGridRowHeader`, `DataGridRowGroupHeader`, `DataGridRow`, `DataGrid` | Needs the package referenced, and a separate assembly (below); versioned apart from Avalonia |
| 11 | ColorPicker (package) | `ColorPreviewer`, `ColorSlider`, `ColorSpectrum`, `ColorView`, `ColorPicker` | Needs batch 5's `TabItem`; a separate assembly |
| — | Recommended to drop | `ManagedFileChooser`, `ManagedFileChooserOverwritePrompt` | Desktop-only, and the platforms' own pickers are what an 8.1 app used. They stay holes, listed |
| — | Recommended out of this plan | `TextSelectionHandle` | A reproduction of the phone's grippers, after a capture: its own issue |

## For the user to pick

Each has a recommendation; none is decided.

1. **Which controls.** Include the 57 above, drop the two file-chooser themes, and move `TextSelectionHandle` to a
   reproduction issue of its own? *Recommended: yes.*
2. **The order.** Batches 1 to 11 as above? *Recommended: yes, batch 1 first.* An alternative is to start with what a
   phone app on Android meets (batch 4's `ContextMenu`, batch 8's refresh, then `TextSelectionHandle`'s reproduction).
3. **Where the plain themes live.** In `Wp81Theme`, labelled per control, or in a package of their own beside the
   Adaptive pack (ADR 0004)? *Recommended: in `Wp81Theme`*, since the holes are era 1's completeness measure and without
   the themes an app gets Simple's look anyway. The DataGrid and ColorPicker themes go in an assembly each
   (`Cornflower.Themes.Wp81.DataGrid`, `...ColorPicker`, working names), so a phone app doesn't take those packages.
4. **Density.** The phone's type and touch sizes everywhere, or desktop density for the dense controls (`DataGrid`,
   `TableView`, `TreeView`, `Menu`, `Calendar`)? *Recommended: the phone's everywhere*, with the sizes as `DynamicResource`
   hooks so the Adaptive pack can make them denser on wide windows.
5. **The tilt.** On item-sized presses only (rule 7)? *Recommended: yes.*
6. **Motion.** None of their own (rule 8), and `TransitioningContentControl` with no transition unless the app sets one
   (Wp81 has `TurnstileTransition`, `SlideTransition` and `ContinuumTransition` an app can choose)? *Recommended: yes.*
7. **`ContextMenu`.** The `MenuFlyout`'s look and motion, which is what an 8.1 app showed on hold (large), or a plain menu
   (medium)? *Recommended: the MenuFlyout's look*, labelled as plain since `ContextMenu` itself isn't the phone's.
8. **`TabControl`.** The Pivot's headers without its swipe, or a plain strip? *Recommended: the Pivot's headers*, as the
   control an 8.1 app used.
9. **An error colour.** A new `CornflowerPlainErrorBrush`, from the Red accent colour (`CornflowerAccentRedColor`), or
   the foreground and the accent only? *Recommended: the new brush*, the only new token in the plan.
10. **The label.** The README line and the `CornflowerPlain` and `Wp81Plain` prefixes (rule 1)? *Recommended: yes.*
11. **Done for a plain theme.** Tests, a preview frame and a look on the Pixel, with no measurement (rule 11)?
    *Recommended: yes.*
12. **The refresh sign.** `RefreshVisualizer` showing Wp81's `ProgressRing`, or the indeterminate ProgressBar's dots?
    *Recommended: the `ProgressRing`*, the phone's sign of work in one place, which a pull can turn as it grows.
