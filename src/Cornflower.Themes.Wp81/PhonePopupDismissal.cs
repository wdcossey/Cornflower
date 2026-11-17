namespace Cornflower.Themes.Wp81;

/// <summary>
/// How a phone popup is dismissed: the one place its rules live, for <see cref="PhonePopupHost"/> and every popup that
/// copies the Flyout's (docs/controls/Flyout/README.md, "What the popups share").
/// </summary>
/// <remarks>
/// <para>As #150 read them (tools/refapp/README.md, PR #172): Back closes a top and a full Flyout, and the app's own
/// Back isn't raised while a popup is open, handled or not; a tap outside a top Flyout closes it, and a tap on the
/// flyout itself doesn't; the closing tap reaches nothing under it; a ContentDialog's dim layer takes a tap and does
/// nothing; a MenuFlyout's full-window layer is a tap outside (Avalonia's light dismiss, with no dim). 8.1's flyouts
/// raise <c>Opening</c> then <c>Opened</c> in the call's frame and <c>Closed</c> when the close has been drawn; they have no
/// <c>Closing</c>. Avalonia's flyout has one: ours raises it when the flyout is asked to close and again as its closing
/// motion ends, so an app may cancel at either.</para>
/// </remarks>
internal static class PhonePopupDismissal
{
    /// <summary>The Back key closes the topmost popup, before the page or its Frame sees it.</summary>
    internal const bool BackCloses = true;

    /// <summary>
    /// A focused element that takes Back has it before any popup: one Back hides the keyboard, closes an
    /// AutoSuggestBox's list and takes the focus; the next reaches the popup, or the app (#150).
    /// </summary>
    internal const bool BackTakesFocusFirst = true;

    /// <summary>A tap outside a Flyout (below a top one) closes it: Avalonia's light dismiss.</summary>
    internal const bool FlyoutTapOutsideCloses = true;

    /// <summary>
    /// The closing tap reaches nothing under it. A flyout whose app sets <c>OverlayDismissEventPassThrough</c> keeps the
    /// app's.
    /// </summary>
    internal const bool TapOutsidePassesThrough = false;

    /// <summary>
    /// A tap on a popup's dim layer is taken by the layer and does nothing (the ContentDialog's). A popup whose layer is
    /// a tap outside (the MenuFlyout's) has no dim: Avalonia's light dismiss is that layer.
    /// </summary>
    internal const bool DimPassesTaps = false;
}
