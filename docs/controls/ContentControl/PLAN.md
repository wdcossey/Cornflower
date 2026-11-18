# ContentControl and ContentPresenter: plan

[#117](https://github.com/wdcossey/Cornflower/issues/117). The 8.1 default styles of the two bare types,
`ContentControl` and `ContentPresenter`, against Avalonia's. Neither has states, resources or brushes: each style sets
only where the content goes.

## Where the values come from

- The styles: the 8.1 Update 1 runtime's, `R:\cornflower-reference\flash-update1\xbf-json\contentcontrol.json` and
  `contentpresenter.json`, which equal the SDK's `generic.xaml` (its "Default style for" ContentControl and
  ContentPresenter).
- The phone's layout of them: the template-tree dumps of refapp run `20261108-1732-dev11` on the reference profile
  (device 11, 450 × 800 at 2.4). No capture shows a bare ContentControl or a ContentPresenter written in a page; the
  phone's own templates hold both, so their dumps are the readings:
  - `Pivot__two-items__base`, dark and light: Pivot's `TitleContentControl`, a ContentControl whose own style
    (`PivotTitleContentControlStyle`) sets only its font and margin, so its alignment and template are the default
    style's. Its PNGs are in [`ref/wp81/ContentControl/`](../../../ref/wp81/ContentControl/README.md).
  - `ToggleSwitch__on__base`: the ToggleSwitch's `SwitchCurtainBounds` and `SwitchCurtainClip`, two ContentPresenters
    with nothing set (`<ContentPresenter x:Name="SwitchCurtainBounds">`). The PNGs are already in
    [`ref/wp81/ToggleSwitch/`](../../../ref/wp81/ToggleSwitch/README.md).
  - Every other ContentControl and ContentPresenter in the run's dumps, read the same way (below).
- Avalonia's: Avalonia 12.1.4's `ContentControl`, read in a test window. Simple has **no** theme for `ContentControl`
  (so it isn't in `Holes.txt`): a bare one takes the control's own default template, a `ContentPresenter` named
  `PART_ContentPresenter` that binds `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`, `Padding` and both
  content alignments, which are `Stretch`. `ContentPresenter` has no theme in Simple or in Wp81.

## The phone's ContentControl

```
Style TargetType ContentControl
  HorizontalContentAlignment  Left
  VerticalContentAlignment    Top
  Template
    ContentPresenter          Content, ContentTemplate, ContentTransitions: TemplateBinding
                              Margin: TemplateBinding Padding
                              HorizontalAlignment: TemplateBinding HorizontalContentAlignment
                              VerticalAlignment: TemplateBinding VerticalContentAlignment
```

No visual states, no transitions, no resources. The template binds no `Background`, `BorderBrush` or
`BorderThickness`, and the 8.1 `ContentPresenter` has none to bind: **a phone ContentControl draws nothing of its
own**, whatever it is given.

And ours, `Wp81ContentControlTheme` in `src/Cornflower.Themes.Wp81/Controls/ContentControl.axaml`, written new: the
same two setters and one `ContentPresenter`, named `PART_ContentPresenter` (the part Avalonia's ContentControl looks
for), with `PhoneHeaderLine` on it for a string's line.

### What the dump reads (Pivot's title)

| | Bounds on the stage | Alignment |
|---|---|---|
| `TitleContentControl` (margin 19,38,0,0; Segoe WP Semibold 18.14) | 19.1667, 37.9167, 430.8333 × 24.1667 | `HorizontalContentAlignment` Left, `VerticalContentAlignment` Top |
| its `ContentPresenter` | 19.1667, 37.9167, 119.5833 × 24.1667 | Left, Top |
| the `TextBlock` "CORNFLOWER" | 19.1667, 37.9167, 119.31 × 24.1267 | |

The control stretches across the stage; its presenter sits at its top left, as wide as its text; the text's line is
1.33 × 18.14 = 24.13, Segoe WP's (ADR 0003). Hub's `Header` and `HeaderHost`, ContentDialog's `Title` and the
TextBox's `PlaceholderTextContentPresenter`, which set no content alignment either, read Left and Top too.

## The phone's ContentPresenter

```
Style TargetType ContentPresenter
  HorizontalAlignment  Left
  VerticalAlignment    Top
```

**The phone doesn't apply it to a presenter in a template.** The ToggleSwitch's `SwitchCurtainBounds` and
`SwitchCurtainClip` set nothing, and read Stretch and Stretch, each filling the `InnerBorder`'s 66.25 × 21.6667 inside
its 2.5 border, 61.25 × 16.6667, round a 64-wide `Rectangle` (at Left they would be 64 wide). The ComboBox's
`ContentPresenter` in its `FlyoutButton` (`MinHeight="32.5"`, no alignment) reads Stretch too. Every template
presenter in the run that reads Left or Top sets it itself, or binds it to a content alignment that is Left or Top.

Whether the phone applies it to a presenter written in a page, outside any template, isn't read: no capture has one.
The probe that would read it is owed (below). Until then, Avalonia's own stands, and Wp81 has no theme for the bare
`ContentPresenter`: one keyed by type would reach every template's presenter (Simple's `Window` among them), which the
phone shows its style doesn't.

## Avalonia measured against the phone

| | Phone | Avalonia 12.1.4 | Rung |
|---|---|---|---|
| ContentControl's content alignment | Left, Top: content at the top left at its own size | Stretch, Stretch: content stretched, a fixed-size element centred | 1, theme |
| ContentControl's Padding | the presenter's Margin | the presenter's Padding | 1, theme (the same layout; ours as the phone's) |
| ContentControl's Background, BorderBrush, BorderThickness | not drawn | drawn by the presenter, and the background takes hits | 1, theme |
| A string's line | Segoe WP's 1.33 em | Selawik's 1.2 em | 2, `PhoneHeaderLine` (internal, existing) |
| Access keys in a string | none: "_open" draws its underscore | none: Avalonia's ContentControl doesn't recognise them | match |
| `ContentTransitions` | passed to the presenter; none by default | no equivalent | 6, recorded: none by default on the phone either |
| `IsTabStop` | `Control`'s default, true | `Focusable` false | 6, recorded (below) |
| Clipping | an element arranged smaller than it asks is clipped to its slot | `ClipToBounds` true | kept as Avalonia's, unmeasured |
| ContentPresenter in a template, nothing set | Stretch | Stretch | match: no theme |
| ContentPresenter in a page | not reached by the style (#114, run `20261115-2048-dev11`) | Stretch | match: no theme |

### Keyed, not by type: why

*Superseded at the Tier 2 roll-up (#139):* the user decided on 2026-11-15 to key it by type, with the three changes below
made there ([README](README.md#by-type)). This section is the reason it first shipped keyed.

A theme keyed `{x:Type ContentControl}` in Wp81 reaches every bare ContentControl. That breaks three things today, each
outside this PR's own files (measured with the theme keyed by type: the full suite, and a frame of every templated
control, Simple then Wp81, before and after):

1. **The holes.** Simple's templates for ComboBox, DataValidationErrors (in every Simple TextBox with an error:
   AutoCompleteBox, NumericUpDown, ButtonSpinner, the pickers), MenuItem's two icon presenters, DatePicker,
   TimePicker, CalendarDatePicker and the managed file dialog hold bare ContentControls laid out for Avalonia's
   Stretch. Under a type-keyed theme a MenuItem's icon, an error's icon and a ComboBox's selection move (their frames
   change). `HoleParts` gives a hole's part the theme under Wp81 for its type, and Simple has none for ContentControl,
   so it leaves Wp81's in place.
2. **A page host.** An Avalonia app hosts its pages in a ContentControl (the sample's `SampleShell` does: `Host`); the
   phone hosts them in a `Frame`, whose style sets both content alignments to Stretch (`frame.json`). Under the type key
   every sample page shrinks to its content: seven sample tests go red.
3. **A test fixture.** `TiltTests.An_Outer_Tilt_Returning_Doesnt_Move_An_Inner_Ones_Eye` presses a ContentControl's
   transparent background, which the phone's template doesn't draw.

None of them is a Tier 1 control's frame: every Tier 1 control's frame is the same pixels with the theme keyed by type
(none of their templates holds a bare ContentControl). So the theme ships keyed, `Wp81ContentControlTheme`, for a
template or an app to set, and the three changes that let it be keyed by type are proposed for the roll-up
([README](README.md#by-type)). Decided on the user's behalf, in the PR.

### IsTabStop

WinRT's `Control.IsTabStop` is true by default and the style doesn't change it, so a phone ContentControl is a tab stop
(the phone's templates set `IsTabStop="False"` on theirs). Avalonia's ContentControl isn't focusable. Left as
Avalonia's: a phone has no Tab key, a focused ContentControl draws and does nothing, and a stop would only add one
where an Avalonia app doesn't expect it.

## The behaviours, and their tests

`tests/Cornflower.Tests/Controls/ContentControlTests.cs`:

| Behaviour | Evidence | Test |
|---|---|---|
| The two setters and one presenter, `PART_ContentPresenter`; no focus adorner | `contentcontrol.json` (no focus states) | `The_Theme_Sets_The_Phones_Two_Values_And_One_Presenter` |
| The content placed by the content alignments: at Left and Top, the top left at its own size | the template; the dump's presenter at the control's corner | `The_Content_Is_Placed_By_The_Content_Alignments` (four cases) |
| The Padding is the presenter's margin | the template | `The_Padding_Is_The_Presenters_Margin` |
| Pivot's title laid out as the dump: the control stretched, 430.83 × 24.17, the presenter at its corner, 24.17 tall, the text 1.33 em | the dump (dark; light reads the same) | `Pivots_Title_Is_Laid_Out_As_The_Dump` |
| A string's text has the phone's line and no access key | ADR 0003; the template (a WinRT ContentControl has none) | `A_Strings_Text_Has_The_Phones_Line_And_No_Access_Key` |
| An app's `ContentTemplate` is used and keeps its own line | the template | `An_Apps_ContentTemplate_Is_Used_And_Keeps_Its_Own_Line` |
| Background and border not drawn; the padding takes no hits | the template binds none | `Background_And_Border_Draw_Nothing_And_The_Padding_Takes_No_Hits` (two cases) |
| A `ContentTemplate` set later takes the line off the same, recycled text | `PhoneHeaderLine` | `A_ContentTemplate_Set_Later_Takes_The_Line_Off_The_Same_Text` |
| A bare ContentControl wears the theme, by type (the roll-up; it kept Avalonia's own until then) | the phone's default style | `A_Bare_ContentControl_Wears_The_Phones_Theme` |
| A hole's ContentControls keep Avalonia's own (`HoleParts.FallBackToOwn`) | the holes' and the shell's frames | `HoleContentControlPartsTests` |
| A template's presenter with nothing set fills its slot, as the phone's do | the curtain presenters' dump | `A_Template_Presenter_With_Nothing_Set_Fills_Its_Slot_As_The_Phones_Do` |

`tests/Cornflower.Tests/Sample/ContentControlPageTests.cs` holds the sample page (four tests).
`ContentControlFrames` saves our frames for the comparison.

## Visual states and resource keys

None. Neither style uses a resource key; its values are enum values, written as such.

## Extensions

None.

## Dependencies

- `PhoneHeaderLine` (#111), for a string's line: exists. It is the sibling of `PhoneContentLine` for a presenter that
  doesn't recognise access keys; its name says header, its job is any such presenter.
- Pivot (#121) and Hub (#122) hold ContentControls (`TitleContentControl`, `Header`, `HeaderHost`) that take the phone's
  default ContentControl style: their templates can set `Wp81ContentControlTheme`.
- Owed, not blocking: a probe of a ContentPresenter written in a page (does the phone's style reach it?), and a capture
  of a bare ContentControl with a background, a padding and a small element, for a picture of its own. Both need the
  reference VM, which another agent holds in this batch.

## Checklist

Generated on 2026-11-15 from the phone SDK's `Windows.winmd`
(`Microsoft SDKs\WindowsPhoneApp\v8.1\Tools\MDILXAPCompile\WinMDs`) and its design folder
(`Windows Phone Kits\8.1\Include\abi\Xaml\Design`). `[x]` is done or kept as Avalonia's; `[-]` is left out, with its
reason.

> Generated by `dotnet run --project tools/winrt-api -- checklist ContentControl <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ContentControl`; Avalonia `Avalonia.Controls.ContentControl`.

### Properties (5)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `ContentTransitions` | `TransitionCollection` | none | Left out: Avalonia has no content transitions on a ContentControl (`TransitioningContentControl` is its own control); the phone's default is none |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Left out: an Avalonia `IDataTemplate` matches its content by itself |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentControl.ContentTemplate` | Kept: the template passes it to the presenter, as the phone's does |
| [x] | `Content` | `Object` | `ContentControl.Content` | Kept: the template passes it to the presenter, as the phone's does |
| [-] | `ContentTemplateRoot` (read-only) (not a dependency property) | `UIElement` | `ContentControl.Presenter.Child` | Left out: the presenter's `Child` is it |

### Events (0)

_none_

### Methods (3)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentChanged(Object oldContent, Object newContent)` (protected, overridable) | `OnPropertyChanged` | Left out: a subclass overrides `OnPropertyChanged`; not a theme's |
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | `OnPropertyChanged` | Left out: a subclass overrides `OnPropertyChanged`; not a theme's |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | `OnPropertyChanged` | Left out: a subclass overrides `OnPropertyChanged`; not a theme's |

### Default style: setters (2)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `HorizontalContentAlignment` | `Left` | `ContentControl.HorizontalContentAlignment` | Themed: the setter, by its value (an enum, not a key) |
| [x] | `VerticalContentAlignment` | `Top` | `ContentControl.VerticalContentAlignment` | Themed: the setter, by its value (an enum, not a key) |

### Template parts (0)

Avalonia's control looks for: `PART_ContentPresenter`.

_none_

### Visual states (0)

_none_

### Transitions (0)

_none_

### Resources (0)

_none_

<details><summary>ContentPresenter: 118 rows, 9 kept as Avalonia's, 109 left out (100 of them every element's, from FrameworkElement, UIElement and DependencyObject)</summary>

> Generated by `dotnet run --project tools/winrt-api -- checklist ContentPresenter <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.ContentPresenter`; Avalonia `Avalonia.Controls.Presenters.ContentPresenter` (the generator wrote "none: a new control": it looks only in `Avalonia.Controls`; corrected by hand, and its Avalonia column with it for the presenter's own members).

### Properties (14)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `TextLineBounds` | `TextLineBounds` | none | Left out: no Avalonia equivalent; the style doesn't set it |
| [-] | `OpticalMarginAlignment` | `OpticalMarginAlignment` | none | Left out: no Avalonia equivalent; the style doesn't set it |
| [x] | `Foreground` | `Brush` | `ContentPresenter.Foreground` | Kept as Avalonia's; the style doesn't set it |
| [x] | `FontWeight` | `FontWeight` | `ContentPresenter.FontWeight` | Kept as Avalonia's; the style doesn't set it |
| [x] | `FontStyle` | `FontStyle` | `ContentPresenter.FontStyle` | Kept as Avalonia's; the style doesn't set it |
| [x] | `FontStretch` | `FontStretch` | `ContentPresenter.FontStretch` | Kept as Avalonia's; the style doesn't set it |
| [x] | `FontSize` | `Double` | `ContentPresenter.FontSize` | Kept as Avalonia's; the style doesn't set it |
| [x] | `FontFamily` | `FontFamily` | `ContentPresenter.FontFamily` | Kept as Avalonia's; the style doesn't set it |
| [-] | `ContentTransitions` | `TransitionCollection` | none | Left out: no Avalonia equivalent |
| [-] | `ContentTemplateSelector` | `DataTemplateSelector` | none | Left out: an Avalonia `IDataTemplate` matches its content by itself |
| [x] | `ContentTemplate` | `DataTemplate` | `ContentPresenter.ContentTemplate` | Kept as Avalonia's; the style doesn't set it |
| [x] | `Content` | `Object` | `ContentPresenter.Content` | Kept as Avalonia's; the style doesn't set it |
| [x] | `CharacterSpacing` | `Int32` | `ContentPresenter.LetterSpacing` | Kept as Avalonia's: in DIPs, n × FontSize / 1000; the style doesn't set it |
| [-] | `IsTextScaleFactorEnabled` | `Boolean` | none | Left out: Avalonia has no text scale factor; the style doesn't set it |

### Events (0)

_none_

### Methods (2)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)` (protected, overridable) | `OnPropertyChanged` | Left out: a subclass overrides `OnPropertyChanged`; not a theme's |
| [-] | `void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)` (protected, overridable) | `OnPropertyChanged` | Left out: a subclass overrides `OnPropertyChanged`; not a theme's |

### Properties, from `FrameworkElement` (22)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `Margin` | `Thickness` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Language` | `String` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `HorizontalAlignment` | `HorizontalAlignment` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `FlowDirection` | `FlowDirection` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `DataContext` | `Object` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `VerticalAlignment` | `VerticalAlignment` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `MaxWidth` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `MaxHeight` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Height` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `MinHeight` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Width` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Tag` | `Object` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Style` | `Style` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Resources` (not a dependency property) | `ResourceDictionary` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Name` | `String` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `MinWidth` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ActualWidth` (read-only) | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `BaseUri` (read-only) (not a dependency property) | `Uri` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Parent` (read-only) (not a dependency property) | `DependencyObject` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Triggers` (read-only) (not a dependency property) | `TriggerCollection` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ActualHeight` (read-only) | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `RequestedTheme` | `ElementTheme` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Events, from `FrameworkElement` (5)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `LayoutUpdated` | `EventHandler<Object>` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Loaded` | `RoutedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `SizeChanged` | `SizeChangedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Unloaded` | `RoutedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `DataContextChanged` | `TypedEventHandler<FrameworkElement, DataContextChangedEventArgs>` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Methods, from `FrameworkElement` (7)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `Object FindName(String name)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void SetBinding(DependencyProperty dp, BindingBase binding)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `BindingExpression GetBindingExpression(DependencyProperty dp)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Size MeasureOverride(Size availableSize)` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Size ArrangeOverride(Size finalSize)` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void OnApplyTemplate()` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Boolean GoToElementStateCore(String stateName, Boolean useTransitions)` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Properties, from `UIElement` (20)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `CompositeMode` | `ElementCompositeMode` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ManipulationMode` | `ManipulationModes` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `IsTapEnabled` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `IsRightTapEnabled` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `IsHoldingEnabled` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `IsHitTestVisible` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `IsDoubleTapEnabled` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Clip` | `RectangleGeometry` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `CacheMode` | `CacheMode` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Opacity` | `Double` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `AllowDrop` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Visibility` | `Visibility` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `UseLayoutRounding` | `Boolean` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Transitions` | `TransitionCollection` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `RenderTransformOrigin` | `Point` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `RenderTransform` | `Transform` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Projection` | `Projection` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `DesiredSize` (read-only) (not a dependency property) | `Size` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerCaptures` (read-only) | `IReadOnlyList<Pointer>` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `RenderSize` (read-only) (not a dependency property) | `Size` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Events, from `UIElement` (25)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [-] | `DoubleTapped` | `DoubleTappedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `DragEnter` | `DragEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `DragLeave` | `DragEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `DragOver` | `DragEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Drop` | `DragEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `GotFocus` | `RoutedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Holding` | `HoldingEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `KeyDown` | `KeyEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `KeyUp` | `KeyEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `LostFocus` | `RoutedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ManipulationCompleted` | `ManipulationCompletedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ManipulationDelta` | `ManipulationDeltaEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ManipulationInertiaStarting` | `ManipulationInertiaStartingEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ManipulationStarted` | `ManipulationStartedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `ManipulationStarting` | `ManipulationStartingEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerCanceled` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerCaptureLost` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerEntered` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerExited` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerMoved` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerPressed` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerReleased` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `PointerWheelChanged` | `PointerEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `RightTapped` | `RightTappedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Tapped` | `TappedEventHandler` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Methods, from `UIElement` (15)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `void Measure(Size availableSize)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void Arrange(Rect finalRect)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Boolean CapturePointer(Pointer value)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void ReleasePointerCapture(Pointer value)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void ReleasePointerCaptures()` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void AddHandler(RoutedEvent routedEvent, Object handler, Boolean handledEventsToo)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void RemoveHandler(RoutedEvent routedEvent, Object handler)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `GeneralTransform TransformToVisual(UIElement visual)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void InvalidateMeasure()` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void InvalidateArrange()` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void UpdateLayout()` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Boolean CancelDirectManipulations()` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `AutomationPeer OnCreateAutomationPeer()` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void OnDisconnectVisualChildren()` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `IEnumerable<IEnumerable<Point>> FindSubElementsForTouchTargeting(Point point, Rect boundingRect)` (protected, overridable) | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Properties, from `DependencyObject` (1)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [-] | `Dispatcher` (read-only) (not a dependency property) | `CoreDispatcher` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Events, from `DependencyObject` (0)

_none_

### Methods, from `DependencyObject` (5)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [-] | `Object GetValue(DependencyProperty dp)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void SetValue(DependencyProperty dp, Object value)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `void ClearValue(DependencyProperty dp)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Object ReadLocalValue(DependencyProperty dp)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |
| [-] | `Object GetAnimationBaseValue(DependencyProperty dp)` | none | Not the theme's: every element's, Avalonia's `Control` or none; the style sets only the two below |

### Default style: setters (2)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [-] | `HorizontalAlignment` | `Left` | `Layoutable.HorizontalAlignment` | Left out: the phone doesn't apply it to a template's presenter (the ToggleSwitch's curtain presenters read Stretch); outside a template unread (owed). No theme for the bare type |
| [-] | `VerticalAlignment` | `Top` | `Layoutable.VerticalAlignment` | Left out: the phone doesn't apply it to a template's presenter (the ToggleSwitch's curtain presenters read Stretch); outside a template unread (owed). No theme for the bare type |

### Template parts (0)

_none_

### Visual states (0)

_none_

### Transitions (0)

_none_

### Resources (0)

_none_

</details>
