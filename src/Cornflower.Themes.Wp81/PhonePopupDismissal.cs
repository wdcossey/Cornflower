namespace Cornflower.Themes.Wp81;

/// <summary>
/// How a phone popup is dismissed: the one place its rules live, for <see cref="PhonePopupHost"/> and every popup that
/// copies the Flyout's (docs/controls/Flyout/README.md, "What the popups share").
/// </summary>
/// <remarks>
/// <para><b>PENDING #150: the dismissal.</b> None of these is read from the emulator yet. Each is the default the
/// platform's documentation and Avalonia's flyout give, until #150 reads the Back key, a tap outside and a tap on the dim
/// layer for each popup, and the order of the events. Drop #150's answers in here; nothing else holds them.</para>
/// </remarks>
internal static class PhonePopupDismissal
{
    /// <summary>PENDING #150. Whether the Back key closes the topmost popup, before the page or its Frame sees it.</summary>
    internal const bool BackCloses = true;

    /// <summary>PENDING #150. Whether a tap outside a Flyout (below a top one) closes it: Avalonia's light dismiss.</summary>
    internal const bool FlyoutTapOutsideCloses = true;

    /// <summary>
    /// PENDING #150. Whether that tap also reaches what is under it. False, as Avalonia's flyout has it by default; a
    /// flyout whose app sets <c>OverlayDismissEventPassThrough</c> keeps the app's.
    /// </summary>
    internal const bool TapOutsidePassesThrough = false;

    /// <summary>
    /// PENDING #150. Whether a tap on a popup's dim layer is a tap outside it (closing it if it closes on one), rather
    /// than taken by the layer. True: the layer takes no hits.
    /// </summary>
    internal const bool DimPassesTaps = true;
}
