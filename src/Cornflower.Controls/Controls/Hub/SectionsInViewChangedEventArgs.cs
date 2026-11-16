namespace Cornflower.Controls;

/// <summary>
/// The data of a <see cref="Hub"/>'s <see cref="Hub.SectionsInViewChanged"/>: the sections that came into view and the
/// ones that went out of it.
/// </summary>
public class SectionsInViewChangedEventArgs : EventArgs
{
    /// <summary>Initialises the data.</summary>
    public SectionsInViewChangedEventArgs(IReadOnlyList<HubSection> addedSections, IReadOnlyList<HubSection> removedSections)
    {
        AddedSections = addedSections;
        RemovedSections = removedSections;
    }

    /// <summary>The sections that came into view.</summary>
    public IReadOnlyList<HubSection> AddedSections { get; }

    /// <summary>The sections that went out of view.</summary>
    public IReadOnlyList<HubSection> RemovedSections { get; }
}
