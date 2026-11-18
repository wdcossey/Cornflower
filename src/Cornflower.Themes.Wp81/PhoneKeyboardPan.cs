using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The phone shell's pan: while the keyboard is up, the window's content moves up so the focused text box is not under
/// it, and comes back down when the keyboard goes (#179). Turned on by the TextBox and PasswordBox themes' own setter,
/// and so for the AutoSuggestBox's TextBox part too. Avalonia shows the keyboard on focus and says what it covers
/// (<c>TopLevel.InputPane</c>, read through <see cref="PhoneSuggestionList.KeyboardOf"/>), but moves nothing, and Android
/// neither pans nor resizes an Avalonia view.
/// </summary>
/// <remarks>
/// <para><b>How far.</b> An AutoSuggestBox goes up until the control's top (its header's) is at the window's top, inside
/// its safe area, as #150 read on the phone (48.33 to 26.67 for the box, 21.67 up), even when the keyboard didn't cover
/// it. A TextBox or PasswordBox goes up only as far as it must for its bottom to clear the keyboard's top, and no
/// further than puts its own top at the window's top; one the keyboard doesn't cover stays. That a TextBox moves the
/// least, not to the top, is assumed (the phone's shell's usual way; no read of it yet).</para>
/// <para><b>What moves.</b> The window's content, all of it, through a <see cref="TranslateTransform"/> at animation
/// priority over the content's own (so an app's own transform comes back as it was): a box in a
/// <see cref="ScrollViewer"/> moves with the page, not by scrolling, as a scroll can't take a box at the end of its
/// content up. A box in a popup (a ContentDialog's) moves nothing: #150 saw a focused TextBox's dialog stay at the top.
/// The safe area, the popups and the keyboard stay where they are; the AutoSuggestBox's list is hung from its box again
/// as the page moves (<see cref="Moved"/>).</para>
/// <para><b>When.</b> As the keyboard comes up, changes or goes, and as the focus comes to or leaves a box. The move is
/// at once, not a slide: whether the phone's slides, and how fast, is unmeasured.</para>
/// </remarks>
internal static class PhoneKeyboardPan
{
    /// <summary>Set by a theme: while this box has the focus and the keyboard is up, the page moves up to show it.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsEnabled", typeof(PhoneKeyboardPan));

    private static readonly AttachedProperty<Pan?> PanProperty =
        AvaloniaProperty.RegisterAttached<TopLevel, Pan?>("Pan", typeof(PhoneKeyboardPan));

    static PhoneKeyboardPan()
    {
        InputElement.GotFocusEvent.AddClassHandler<TextBox>((box, _) => PanOf(box)?.Update());
        // The focus leaving: once it has settled, as it may be on its way to another box.
        InputElement.LostFocusEvent.AddClassHandler<TextBox>((box, _) =>
        {
            if (PanOf(box) is { } pan)
                Dispatcher.UIThread.Post(pan.Update);
        });
    }

    /// <summary>Raised with the window as its content moves, after it has.</summary>
    internal static event Action<TopLevel>? Moved;

    public static bool GetIsEnabled(TextBox box) => box.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox box, bool value) => box.SetValue(IsEnabledProperty, value);

    /// <summary>How far the window's content is moved up now (0 when it isn't).</summary>
    internal static double ShiftOf(TopLevel top) => top.GetValue(PanProperty)?.Shift ?? 0;

    /// <summary>The window's pan, made the first time a box of ours in it has the focus.</summary>
    private static Pan? PanOf(TextBox box)
    {
        // A popup of its own (a desktop's, not the phone's overlay) is a window the page's move can't take.
        if (!GetIsEnabled(box) || TopLevel.GetTopLevel(box) is not { } top || top is PopupRoot)
            return null;
        if (top.GetValue(PanProperty) is not { } pan)
            top.SetValue(PanProperty, pan = new Pan(top));
        return pan;
    }

    /// <summary>One window's pan: its keyboard, its content and how far that is moved.</summary>
    private sealed class Pan
    {
        private readonly TopLevel _top;
        private readonly PhoneSuggestionList.IPhoneKeyboard? _keyboard;
        private readonly TranslateTransform _transform = new();
        private Visual? _moved;
        private IDisposable? _shown;

        public Pan(TopLevel top)
        {
            _top = top;
            _keyboard = PhoneSuggestionList.KeyboardOf(top);
            if (_keyboard is not null)
                _keyboard.Changed += (_, _) => Update();
        }

        public double Shift { get; private set; }

        public void Update()
        {
            var content = _top.Content as Visual;
            var shift = Wanted(content);
            if (shift == Shift && content == _moved)
                return;
            _shown?.Dispose();
            _shown = null;
            _moved = null;
            Shift = shift;
            if (shift > 0 && content is not null)
            {
                _transform.Y = -shift;
                _shown = content.SetValue(Visual.RenderTransformProperty, _transform, BindingPriority.Animation);
                _moved = content;
            }
            Moved?.Invoke(_top);
        }

        /// <summary>How far the content should be moved up for the box that has the focus, if one of ours has.</summary>
        private double Wanted(Visual? content)
        {
            if (_keyboard is not { IsOpen: true } keyboard || keyboard.Occluded.Height <= 0
                || _top.FocusManager?.GetFocusedElement() is not TextBox box || !GetIsEnabled(box)
                || content is null || !content.IsVisualAncestorOf(box))
                return 0;
            Visual target = box.TemplatedParent is AutoCompleteBox suggest && PhoneSuggestionList.GetIsEnabled(suggest)
                ? suggest
                : box;
            if (target.TranslatePoint(default, _top) is not { } at)
                return 0;
            // Where it would be were the content not moved.
            var top = at.Y + Shift;
            var bottom = top + target.Bounds.Height;
            var area = PhonePopupHost.AreaOf(_top);
            if (target is AutoCompleteBox)
                return Math.Max(0, top - area.Top);
            // The least move from where the content is that shows the box above the keyboard, its top if it is taller.
            var shift = Shift;
            if (bottom - shift > keyboard.Occluded.Top)
                shift = bottom - keyboard.Occluded.Top;
            if (top - shift < area.Top)
                shift = top - area.Top;
            return Math.Max(0, shift);
        }
    }
}
