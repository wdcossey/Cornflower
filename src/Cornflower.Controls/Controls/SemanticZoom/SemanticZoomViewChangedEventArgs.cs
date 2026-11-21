namespace Cornflower.Controls;

/// <summary>WinRT's <c>SemanticZoomLocation</c>: an item a <see cref="SemanticZoom"/>'s change of view starts from or goes to.</summary>
/// <remarks>WinRT's <c>Bounds</c> is left out: nothing in an 8.1 app's use of the jump list reads it.</remarks>
public sealed class SemanticZoomLocation
{
    internal SemanticZoomLocation(object? item) => Item = item;

    /// <summary>The item: a group, or an item of the zoomed-in list; null when there is none (Back).</summary>
    public object? Item { get; }
}

/// <summary>The data of <see cref="SemanticZoom.ViewChangeStarted"/> and <see cref="SemanticZoom.ViewChangeCompleted"/>: WinRT's <c>SemanticZoomViewChangedEventArgs</c>.</summary>
public sealed class SemanticZoomViewChangedEventArgs : EventArgs
{
    internal SemanticZoomViewChangedEventArgs(bool isSourceZoomedInView, object? source, object? destination)
    {
        IsSourceZoomedInView = isSourceZoomedInView;
        SourceItem = new SemanticZoomLocation(source);
        DestinationItem = new SemanticZoomLocation(destination);
    }

    /// <summary>Whether the change starts from the zoomed-in view: true as the jump list opens, false as it closes.</summary>
    public bool IsSourceZoomedInView { get; }

    /// <summary>
    /// Where the change starts: zooming out, the zoomed-in list's top item; zooming in, the group whose tile was tapped.
    /// Its <see cref="SemanticZoomLocation.Item"/> is null for Back and a change made in code.
    /// </summary>
    public SemanticZoomLocation SourceItem { get; }

    /// <summary>
    /// Where the change goes: zooming out, the group of the header tapped (or of the top item, in code); zooming in, the
    /// group whose tile was tapped. Its <see cref="SemanticZoomLocation.Item"/> is null for Back and a zoom-in made in code.
    /// </summary>
    public SemanticZoomLocation DestinationItem { get; }
}
