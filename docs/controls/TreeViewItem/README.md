# TreeViewItem

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#239](https://github.com/wdcossey/Cornflower/issues/239) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `TreeViewItem`, themed by type in `src/Cornflower.Themes.Wp81/Controls/TreeViewItem.axaml`. Its nearest on
the phone is a `ListViewItem`. It sets **the row** that batch 9's tables reuse (plan section 3): the rule under the row,
the phone's touch height, the tilt as the press, and the accent for a selected row's text.

## Usage

See [`TreeView`](../TreeView/README.md).

## What it sets

| Property | Value |
|---|---|
| `Background` | `Transparent` |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal` at `TextStyleLargeFontSize` (18.14), the body's (rule 2) |
| `FocusAdorner` | none |
| `cf:Tilt.IsEnabled` | `True`, drawn on `PART_Header` (rule 7) |
| `Template` | the row over the children (below) |

## Resource keys

- `Wp81PlainRowTheme`, in `Controls/PlainRow.axaml` (a part theme on a `Border`, by `DynamicResource`): the row. A
  `Transparent` background, `PhoneButtonMinHeight` (57.5) as its least height, and the rule:
  `CornflowerPlainRuleThickness` (0,0,0,1.5, in `Tokens/Plain.axaml`) in `PhoneLowBrush`. A table's row wears it, and
  copies this theme's selected and disabled styles and its tilt.
- `CornflowerPlainTreeViewItemIndent` (38.5, in `Tokens/Plain.axaml`): each level's indent and the chevron's column, a
  phone Button's box (57.5 less the two 9.5 overhangs), so a child's chevron sits under its parent's header.
- `Wp81PlainHeaderToggleButtonTheme` for the chevron (see [`Expander`](../Expander/README.md)).

## The template's parts

- `PART_Row`: the row, in `Wp81PlainRowTheme`, bound to the item's `Background`; the focus target. It stays untilted and
  takes the finger across the row.
- `PART_Header`: indented by `Level` × the indent (Avalonia's `TreeViewItemIndentConverter`); the tilt's target. Only the
  nearest item tilts, so a child's press leaves its parent still.
- `PART_ChevronColumn` and `PART_ExpandCollapseChevron`: the chevron, a header toggle bound two ways to `IsExpanded`,
  filling the row's height: Lucide's `ChevronRight`, `ChevronDown` open. A leaf hides it and keeps the column, so its
  header lines up with its siblings'.
- `PART_HeaderPresenter`: the `Header`, centred in the row, at the phone's line.
- `PART_ItemsPresenter`: the children, shown while `IsExpanded`, at once (rule 8).

Selected, the row's text and chevron are `PhoneAccentBrush`, with no fill (rule 3). Disabled, `PhoneDisabledBrush`. No
pointer-over look.

## Known gaps

- The phone's `ListViewItem` shows a selection only in its check mode (the tick). A tree needs one always, so this takes
  the accent text the plan's rule 3 names. A `ListBox`'s selection on the phone fills with the accent instead.
- A whole row is 57.5 at the least: a deep tree on a phone is long. The Adaptive pack can make it denser.
- A double tap on the header opens it (Avalonia's own behaviour), as does a tap on the chevron.
