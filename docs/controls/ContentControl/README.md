# ContentControl and ContentPresenter

Status: done, keyed (turning it on by type waits on the roll-up, [below](#turning-it-on-by-type)) · [#117](https://github.com/wdcossey/CornflowerUI/issues/117) · device run passed 2026-11-15 (Pixel 8, Release, over wireless adb, `run.ps1 -Page contentcontrol` on `main` at `c5ee6fb`: cold 762 ms, the page opened in 252 ms, every specimen drawn) · real-window run passed 2026-11-15 (`tools/real-window/drive.ps1` under `pwsh` 7.7, Debug, on `main` at `fedf781`: 135 of 135, the contentcontrol page opened in 8 ms with 53 elements)

The Windows Phone 8.1 ContentControl: Avalonia's `ContentControl` in `Wp81ContentControlTheme`, the phone's default
style and template, in `src/Cornflower.Themes.Wp81/Controls/ContentControl.axaml`. The bare `ContentPresenter` keeps
Avalonia's own: the phone's style for it doesn't reach a template's presenter. The plan, with every value and its
source, is [PLAN.md](PLAN.md); the phone's capture beside ours is in [COMPARISON.md](COMPARISON.md).

## Usage

On the phone a `ContentControl` with no style is the phone's. Here, for now, it takes the theme by its key:

```xml
<!-- 8.1 -->
<ContentControl Content="content" />
<!-- Avalonia, with Wp81Theme in Application.Styles -->
<ContentControl Theme="{DynamicResource Wp81ContentControlTheme}" Content="content" />
```

A ContentControl with no theme is Avalonia's own, as with Simple alone.

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

## Turning it on by type

On the phone every ContentControl has this style. Keyed by type here, it would also reach three things outside this
control's files, so the theme is keyed until the roll-up makes these changes (each tried in this PR's worktree: with
all four, the full suite is green, and every hole's frame and tree are as with the theme keyed):

1. `src/Cornflower.Themes.Wp81/HoleParts.cs`: a second internal flag, `FallBackToOwn`. A part in a template written
   for the theme under Wp81 whose type has no theme there (Simple has none for ContentControl) takes an empty
   `ControlTheme` for its type, so Avalonia's own template and defaults, as under that theme alone:

   ```csharp
   public static readonly AttachedProperty<bool> FallBackToOwnProperty =
       AvaloniaProperty.RegisterAttached<TemplatedControl, bool>("FallBackToOwn", typeof(HoleParts));
   private static readonly ConditionalWeakTable<Type, ControlTheme> Bare = new();
   // in Check, after the theme under Wp81 is looked for:
   if (ThemeFor(control.StyleKey, below, variant) is { } fallback)
       control.Theme = fallback;
   else if (control.GetValue(FallBackToOwnProperty))
       control.Theme = Bare.GetValue(control.StyleKey, key => new ControlTheme(key));
   ```

   Not for the existing parts: `HoleToggleParts`' test that a part with no theme for its type under Wp81 keeps Wp81's
   holds for them.
2. `samples/Cornflower.Sample/SampleShell.axaml`: the page host stretches its page, as the phone's `Frame` does
   (`frame.json`: both content alignments Stretch):

   ```xml
   <ContentControl x:Name="Host" HorizontalContentAlignment="Stretch" VerticalContentAlignment="Stretch">
   ```
3. `tests/Cornflower.Tests/Behaviours/TiltTests.cs`, `An_Outer_Tilt_Returning_Doesnt_Move_An_Inner_Ones_Eye`: the item
   whose transparent background the test presses keeps Avalonia's template, which draws it:
   `Theme = new ControlTheme(typeof(ContentControl))`.
4. `Controls/ContentControl.axaml`: the type-keyed theme, on the keyed one:

   ```xml
   <ControlTheme x:Key="{x:Type ContentControl}" TargetType="ContentControl" BasedOn="{StaticResource Wp81ContentControlTheme}">
     <Setter Property="wp81:HoleParts.FallBackToOwn" Value="True" />
     <Setter Property="wp81:HoleParts.FallBack" Value="True" />
   </ControlTheme>
   ```

   with `ContentControlTests.A_Bare_ContentControl_Keeps_Avalonias_Own` turned round (a bare one wears it), a test that
   every hole's ContentControls keep Avalonia's own (as `HolePartsTests` does for Buttons), and the sample page's
   `Theme` setter dropped.

An app that hosts its pages in a ContentControl then sets Stretch on it, as a phone app's `Frame` has.

## Known gaps

- **Keyed, not by type**, until the changes above.
- **A ContentPresenter written in a page**: whether the phone's style (Left, Top) reaches it is unread; Avalonia's
  Stretch stands. Owed: a probe.
- **No capture of a bare ContentControl**: the comparison uses Pivot's title, which the default style lays out. Owed:
  a capture with a background, a padding and a small element.
- **The text is Selawik**: "CORNFLOWER" in Semibold is 1.7% wider.
- **`IsTabStop`**: the phone's ContentControl is a tab stop; Avalonia's isn't focusable, and stays so.
- **The checklist generator** looks for Avalonia's type in `Avalonia.Controls` only, so it called ContentPresenter (in
  `Avalonia.Controls.Presenters`) "a new control"; corrected by hand in the plan.

## The checklist

In [PLAN.md](PLAN.md#checklist): ContentControl, 4 rows done or kept as Avalonia's, 6 left out with their reasons;
ContentPresenter, 9 kept, 109 left out (100 of them every element's).
