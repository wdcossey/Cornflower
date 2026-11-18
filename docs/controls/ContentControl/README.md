# ContentControl and ContentPresenter

Status: done, by type since the Tier 2 roll-up, #139 ([below](#by-type)) · [#117](https://github.com/wdcossey/Cornflower/issues/117) · device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page contentcontrol` on `main` at `c5ee6fb`: cold 762 ms, the page opened in 252 ms, every specimen drawn) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the contentcontrol page opened in 8 ms with 53 elements)

The Windows Phone 8.1 ContentControl: Avalonia's `ContentControl` in the phone's default style and template, keyed by
type and as `Wp81ContentControlTheme`, in `src/Cornflower.Themes.Wp81/Controls/ContentControl.axaml`. The bare `ContentPresenter` keeps
Avalonia's own: the phone's style for it doesn't reach a template's presenter. The plan, with every value and its
source, is [PLAN.md](PLAN.md); the phone's capture beside ours is in [COMPARISON.md](COMPARISON.md).

## Usage

A `ContentControl` with no style is the phone's, as on the phone:

```xml
<!-- 8.1, and Avalonia with Wp81Theme in Application.Styles -->
<ContentControl Content="content" />
```

A template can also set the keyed theme by name, `Theme="{DynamicResource Wp81ContentControlTheme}"`. A ContentControl
in the template of a control Wp81 doesn't theme yet keeps Avalonia's own, as with Simple alone ([below](#by-type)). An
app that hosts its pages in a ContentControl sets both content alignments to Stretch, as the phone's `Frame` has them:

```xml
<ContentControl x:Name="Host" HorizontalContentAlignment="Stretch" VerticalContentAlignment="Stretch" />
```

**What changes for an app** since the theme went in by type: a ContentControl of its own (a page or view host, a
`ContentControl Content="{Binding CurrentPage}"`) puts its content at the top left at its own size, and draws no
`Background` or border, so a transparent background no longer takes taps. Set the two alignments to Stretch to fill,
or give it an empty theme to keep Avalonia's own template:
`<ControlTheme x:Key="PlainContentControl" TargetType="ContentControl" />` and
`Theme="{StaticResource PlainContentControl}"`. A ContentControl in the template of a theme added after Wp81 counts as
the app's, and wears Wp81's too.

## Behaviours

- **The content sits at the top left at its own size**: `HorizontalContentAlignment` Left and
  `VerticalContentAlignment` Top, the phone's, where Avalonia's are Stretch. An app's own alignments place it, as on
  the phone.
- **The padding** is the presenter's margin.
- **Nothing of its own is drawn**: a `Background`, `BorderBrush` or `BorderThickness` given to it draws nothing and the
  padding takes no hits, as the phone's template binds none. Focused (an app can make it focusable), it draws no focus
  rectangle, as on the phone (neither Avalonia nor Simple gives a ContentControl one, so the theme needs no setter).
- **A string's line** is the phone's 1.33 em, through `PhoneHeaderLine`; an app's `ContentTemplate` keeps its own. No
  access keys, as on the phone.
- **The bare ContentPresenter** is Avalonia's: in a template, one with nothing set fills its slot, as the phone's do.

## Resource keys

None: the style's two values are enum values.

## The template's parts

`PART_ContentPresenter` (the phone's `ContentPresenter`, under the name Avalonia's ContentControl looks for).

## The ladder

Rung 1 (theme) for the two setters and the template; rung 2 (attached behaviour) for the line (`PhoneHeaderLine`,
internal, existing). No type is substituted. `ContentTransitions` and `IsTabStop` are recorded differences (rung 6).

## Extensions

None.

## By type

On the phone every ContentControl has this style, and so it is here, since the Tier 2 roll-up, #139 (the user's decision of
2026-11-15). Keyed by type it also reaches three things outside this control's files, changed with it:

1. **The holes.** Simple's templates for DataValidationErrors, MenuItem and the managed file dialog hold plain
   ContentControls. `HoleParts` keeps them as Simple lays them out: the type-keyed theme sets `HoleParts.FallBack` and
   `HoleParts.FallBackToOwn`, so a part in a template written for the theme under Wp81 takes that theme's
   ContentControl theme, or, as Simple has none, an empty `ControlTheme` for its type, which gives it Avalonia's own
   template and defaults. `HoleContentControlPartsTests` holds it.
2. **The sample's page host**, `SampleShell`'s `Host`, stretches its page, as the phone's `Frame` does (`frame.json`:
   both content alignments Stretch). Since the Tier 2 roll-up the host is a `NavigationPage`, and each page's
   `ContentPage` stretches it.
3. **`TiltTests`' fixture** that presses a ContentControl's transparent background gives it Avalonia's own template,
   which draws it.

Checked when it went in: every frame saver's 253 frames, and every hole's, every sample page's and a TextBox's frame and
tree, dark and light, were the same before and after (the ProgressRing page, NotificationCard and TimePickerPresenter
change from run to run on their own).

## Known gaps

- **A ContentPresenter written in a page**: read by #114 (run `20261115-2048-dev11`): the phone's style (Left, Top)
  doesn't reach it either, so no ContentPresenter theme stands. Not compared pixel for pixel.
- **A bare ContentControl**: #114 captured one with a background and a padding (run `20261115-2048-dev11`); it draws
  no background, as ours doesn't. The comparison still uses Pivot's title; the new capture isn't in `ref/wp81/` yet.
- **The text is Selawik**: "CORNFLOWER" in Semibold is 1.7% wider.
- **`IsTabStop`**: the phone's ContentControl is a tab stop; Avalonia's isn't focusable, and stays so.
- **The checklist generator** looks for Avalonia's type in `Avalonia.Controls` only, so it called ContentPresenter (in
  `Avalonia.Controls.Presenters`) "a new control"; corrected by hand in the plan.

## The checklist

In [PLAN.md](PLAN.md#checklist): ContentControl, 4 rows done or kept as Avalonia's, 6 left out with their reasons;
ContentPresenter, 9 kept, 109 left out (100 of them every element's).
