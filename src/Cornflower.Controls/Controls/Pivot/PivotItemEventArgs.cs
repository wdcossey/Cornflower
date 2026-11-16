namespace Cornflower.Controls;

/// <summary>
/// The data of a <see cref="Pivot"/>'s <c>PivotItemLoading</c>, <c>PivotItemLoaded</c>, <c>PivotItemUnloading</c> and
/// <c>PivotItemUnloaded</c> events, WinRT's <c>PivotItemEventArgs</c>: the item they are about.
/// </summary>
public sealed class PivotItemEventArgs : EventArgs
{
    internal PivotItemEventArgs(PivotItem item) => Item = item;

    /// <summary>The item being loaded or unloaded: the item's container, for an item given as data.</summary>
    public PivotItem Item { get; }
}
