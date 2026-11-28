# NotificationCard

Plain theme: Cornflower's design in the era's style, not a reproduction. Nothing in it is measured from the phone.

Status: themed, plain ([ADR 0007](../../decisions/0007-plain-themes.md)), [#241](https://github.com/wdcossey/Cornflower/issues/241) · its look on the Pixel done 2026-11-24 (no timings)

Avalonia's `NotificationCard`, themed by type in `src/Cornflower.Themes.Wp81/Controls/NotificationCard.axaml`.
8.1 showed the system's toast, a banner across the top (its look unmeasured), so a card here is a banner in its spirit.
Its host is the [`WindowNotificationManager`](../WindowNotificationManager/README.md).

## Usage

```csharp
var notifications = new WindowNotificationManager(TopLevel.GetTopLevel(this));
notifications.Show(new Notification("Saved", "Your changes are in.", NotificationType.Success));
```

A card can also stand on its own (`<NotificationCard Content="Copied." />`); it looks the same, as the
notification's template is the card's own.

## What it sets

| Property | Value |
|---|---|
| `Background` | `FlyoutBackgroundThemeBrush` (`PhoneChromeColor`): a floating surface (rule 5) |
| `Foreground` | `PhoneForegroundBrush` |
| `FontFamily`, `FontSize` | `PhoneFontFamilyNormal`, `TextStyleMediumFontSize` (16, the small print's size, rule 2) |
| `Padding` | `CornflowerPlainNotificationPadding` (19, 9.5, 0, 9.5) round the content |
| `BorderThickness` | `CornflowerPlainRuleThickness` (the 1.5 line under it, rule 4) |
| `BorderBrush` | by type (below) |
| `HorizontalAlignment` | stretch: as wide as its host |

A `Notification` (any `INotification`) shows its `Title` in `PhoneFontFamilySemiBold` on one line, trimmed, and
its `Message` under it, wrapped, two lines at most; either is left out when empty. Other content shows as its own.
Square, no shadow, no accent at rest (rule 3).

| `NotificationType` | The line under it |
|---|---|
| `Information` | the surface's own (no line seen) |
| `Success` | `CornflowerPlainSuccessBrush`, the Green accent (`#FF60A917`) |
| `Warning` | `CornflowerPlainWarningBrush`, the Amber accent (`#FFF0A30A`) |
| `Error` | `CornflowerPlainErrorBrush`, the Red accent (`#FFE51400`) |

The same in dark and light. Every banner is the same height whatever its type.

**Closing.** `PART_CloseButton`, at the banner's right, closes it (`NotificationCard.CloseOnClick`): a fingertip
wide and tall (`PhoneButtonMinHeight`, 57.5, rule 6), `Wp81PlainGlyphButtonTheme` round Lucide's `X` (pressed, the
accent fill). It takes no focus and asks for no keyboard (`InputMethod.IsInputMethodEnabled` off, #254). It takes its
press, so the manager's tap (the notification's `OnClick`) doesn't run; a tap on the rest of the banner runs it and
closes the card, as Avalonia's manager does.

**No motion** (rule 8). Simple's and Fluent's cards fly in and out and set `IsClosed` when their closing animation
ends; here `PlainNotificationClose` (internal) sets it at once, or, when the press that closed it (a manager closes a card
on its press) is still down, as that press ends (its capture lost: released or cancelled), so the release isn't the
page's under it. A card in a manager then leaves; a closed card on its own is hidden.

## Resource keys

- `CornflowerPlainSuccessBrush`, `CornflowerPlainWarningBrush`, `CornflowerPlainNotificationPadding` (new, in
  `Tokens/Plain.axaml`); `CornflowerPlainErrorBrush` and `CornflowerPlainRuleThickness` (earlier batches).
- `FlyoutBackgroundThemeBrush`, `PhoneForegroundBrush`, `PhoneFontFamilyNormal`, `PhoneFontFamilySemiBold`,
  `TextStyleMediumFontSize`, `PhoneButtonMinHeight`; `Wp81PlainGlyphButtonTheme` (the close button).

## The template's parts

- `PART_Border`: the banner's surface and its line; it holds the `INotification` template (`PART_Title`, `PART_Message`).
- `PART_ContentPresenter`: the content, inside `Padding`.
- `PART_CloseButton`: the close button.

## Glyphs

Lucide's `X` (ISC), through `LucideIcon`: a plain theme's use of the third-party set (plan section 2, rule 10).

## Known gaps

- No motion in or out, and no swipe to dismiss (the phone's toast went with a swipe to the right).
- A closed card on its own stays closed: `Close()` can't be undone (Avalonia's), so make a new card to show it again.
- An app's own `DataTemplate` for `INotification` higher in the tree is behind the card's own, as it is behind
  Simple's manager's; an app's template for its own notification type set as the card's `ContentTemplate` wins.
