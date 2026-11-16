using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Cornflower.Controls.Primitives;

/// <summary>
/// One header in a <see cref="Pivot"/>'s header strip, WinRT's <c>Primitives.PivotHeaderItem</c>: the Pivot makes one
/// for each of its items, with the item's header as its content and the Pivot's
/// <see cref="Pivot.HeaderTemplate"/> as its template, and an app doesn't make them.
/// </summary>
/// <remarks>
/// Behaviour only. Its state is WinRT's <c>SelectionStates</c> group as pseudoclasses: <c>:selected</c> for the header
/// of the selected item; <c>:unselectedlocked</c> for every other header while the Pivot
/// <see cref="Pivot.IsLocked"/>; neither is the phone's <c>Unselected</c>. A tap on a header that isn't selected
/// selects its item, at the tap's release, as on the phone; the Pivot handles it.
/// </remarks>
[PseudoClasses(SelectedPseudoClass, UnselectedLockedPseudoClass)]
public class PivotHeaderItem : ContentControl
{
    private const string SelectedPseudoClass = ":selected";
    private const string UnselectedLockedPseudoClass = ":unselectedlocked";

    /// <summary>The index of the Pivot's item this header is for.</summary>
    internal int Index { get; set; }

    /// <summary>Sets the header's state: the phone's <c>Selected</c>, <c>UnselectedLocked</c> or <c>Unselected</c>.</summary>
    internal void SetState(bool selected, bool locked)
    {
        PseudoClasses.Set(SelectedPseudoClass, selected);
        PseudoClasses.Set(UnselectedLockedPseudoClass, !selected && locked);
    }
}
