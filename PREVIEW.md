# Preview

What Cornflower looks like today: the pages of the sample app, drawn by the Windows Phone 8.1 theme at the phone's
own size (450 × 800 view pixels), dark and light. Each control is measured against the Windows Phone 8.1 emulator;
its comparison is in [`docs/controls/`](docs/controls/).

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

<p>
<img src="docs/preview/dark__textbox.png" width="270" alt="TextBox, dark">
<img src="docs/preview/light__textbox.png" width="270" alt="TextBox, light">
</p>
<p>
<img src="docs/preview/dark__button.png" width="270" alt="Button, dark">
<img src="docs/preview/light__button.png" width="270" alt="Button, light">
</p>
<p>
<img src="docs/preview/dark__checkbox.png" width="270" alt="CheckBox, dark">
<img src="docs/preview/light__radiobutton.png" width="270" alt="RadioButton, light">
</p>

## Not shown

Motion: the tilt under a finger, the turnstile between pages, the Pivot's slide and the Hub's pan run on the phone's
own measured curves, which still pictures can't show. The pickers, flyouts and dialogs are in progress (Tier 3 of
the [roadmap](docs/ROADMAP.md)).
