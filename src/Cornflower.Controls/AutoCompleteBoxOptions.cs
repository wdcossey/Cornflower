using Avalonia;
using Avalonia.Controls;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>AutoSuggestBox.AutoMaximizeSuggestionArea</c>, for Avalonia's <see cref="AutoCompleteBox"/>, which lacks
/// it. It holds data only; a theme reads it, as the Windows Phone 8.1 theme does: true, the keyboard coming up moves the
/// page up until the control's top is at the window's top, to give the suggestions the room down to the keyboard, even
/// for a box the keyboard doesn't cover; false, the page moves as for a TextBox, and only for a box the keyboard covers
/// (#200's read, #201). A theme that doesn't read it ignores it.
/// </summary>
/// <example>
/// <code>&lt;AutoCompleteBox cf:AutoCompleteBoxOptions.AutoMaximizeSuggestionArea="False" /&gt;</code>
/// </example>
public static class AutoCompleteBoxOptions
{
    /// <summary>
    /// Whether the page moves up to put the control at the window's top while the keyboard is up: WinRT's
    /// <c>AutoMaximizeSuggestionArea</c>. True by default, as WinRT's.
    /// </summary>
    public static readonly AttachedProperty<bool> AutoMaximizeSuggestionAreaProperty =
        AvaloniaProperty.RegisterAttached<AutoCompleteBox, bool>(
            "AutoMaximizeSuggestionArea", typeof(AutoCompleteBoxOptions), defaultValue: true);

    public static bool GetAutoMaximizeSuggestionArea(AutoCompleteBox box) => box.GetValue(AutoMaximizeSuggestionAreaProperty);

    public static void SetAutoMaximizeSuggestionArea(AutoCompleteBox box, bool value) =>
        box.SetValue(AutoMaximizeSuggestionAreaProperty, value);
}
