namespace Cornflower.Controls;

/// <summary>The data of a <see cref="Hub"/>'s <see cref="Hub.SectionHeaderClick"/>: the section whose header was clicked.</summary>
public class HubSectionHeaderClickEventArgs : EventArgs
{
    /// <summary>Initialises the data for <paramref name="section"/>.</summary>
    public HubSectionHeaderClickEventArgs(HubSection section) => Section = section;

    /// <summary>The section whose header was clicked.</summary>
    public HubSection Section { get; }
}
