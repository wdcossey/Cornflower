using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The plain NotificationCard's close (ADR 0007, #241): with no closing motion (rule 8), <c>IsClosed</c> follows
/// <c>IsClosing</c> (Avalonia's own themes set it at the end of their closing animation): at once, unless a pointer
/// pressed on the card is still down (a manager closes a card on its press), and then once that press ends, so the card
/// keeps the press to its end and its release doesn't land on what is under it.
/// </summary>
internal static class PlainNotificationClose
{
    /// <summary>Set by the plain NotificationCard theme.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<NotificationCard, bool>("IsEnabled", typeof(PlainNotificationClose));

    /// <summary>Each pointer pressed on a card and not yet released or lost, with its card.</summary>
    private static readonly Dictionary<IPointer, NotificationCard> Pressed = new();

    static PlainNotificationClose()
    {
        NotificationCard.IsClosingProperty.Changed.AddClassHandler<NotificationCard>((card, _) => Settle(card));
        InputElement.PointerPressedEvent.AddClassHandler<NotificationCard>(OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        // A press ends with its capture lost, wherever it was: released (Avalonia lets the capture go after the release),
        // a cancelled touch, or a capture taken by something outside the card.
        InputElement.PointerCaptureLostEvent.AddClassHandler<InputElement>((_, e) => Let(e.Pointer), handledEventsToo: true);
    }

    public static bool GetIsEnabled(NotificationCard card) => card.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(NotificationCard card, bool value) => card.SetValue(IsEnabledProperty, value);

    private static void OnPressed(NotificationCard card, PointerPressedEventArgs e)
    {
        if (card.GetValue(IsEnabledProperty))
            Pressed[e.Pointer] = card;
    }

    private static void Let(IPointer pointer)
    {
        if (Pressed.Count == 0 || !Pressed.TryGetValue(pointer, out var card))
            return;
        Pressed.Remove(pointer);
        Settle(card);
    }

    /// <summary>A closing card with no press on it is closed.</summary>
    private static void Settle(NotificationCard card)
    {
        if (!card.GetValue(IsEnabledProperty) || !card.IsClosing || card.IsClosed || Pressed.ContainsValue(card))
            return;
        card.SetCurrentValue(NotificationCard.IsClosedProperty, true);
    }
}
