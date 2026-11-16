using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>Hub</c>: a large <see cref="Header"/> over a row of <see cref="HubSection"/>s that pans sideways, the
/// header moving at 1/18 of the sections' speed and a background image between the two, and that loops from the last
/// section round to the first.
/// </summary>
/// <remarks>
/// <para>
/// Behaviour only: the sections, the pan and its motion, the parallax of the header and the background, and the loop. A
/// theme's template draws it, with these parts: <c>PART_ScrollViewer</c>, the area a pan starts in (the whole Hub
/// without it); <c>PART_Panel</c>, a <see cref="Panel"/> the Hub puts its sections in, side by side; <c>PART_HeaderHost</c>,
/// the header, which the Hub moves; and <c>PART_Background</c>, holding <c>PART_MainBackground</c> and
/// <c>PART_WrapBackground</c>, which draw the <see cref="TemplatedControl.Background"/>. The motion, the phone's, read
/// from the 8.1 emulator, is the control's own, frame by frame on <see cref="CornflowerClock"/>
/// (<see cref="HubMotion"/>).
/// </para>
/// <para>
/// Each section is 0.9 of the Hub's width unless it has a width of its own, so the next one's edge shows. A swipe goes
/// on to the next section or back to the one before, a slow drag to the nearer; past the last section the Hub loops to
/// the first, which shows past the last at the end.
/// </para>
/// <para>
/// A <see cref="TemplatedControl.Background"/> that is an <see cref="ImageBrush"/> is drawn as the phone draws it: at the
/// image's own pixel width and the window's height, moving slower than the sections so that its right edge reaches the
/// view's as the last section's does, and repeated past its end for the loop. The states are WinRT's
/// <c>BackgroundImageLoadStates</c>: <c>:backgroundhidden</c> while an image brush has no image, <c>:backgroundshowing</c>
/// otherwise (the phone goes straight from one to the other, #137).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;cf:Hub Header="my app"&gt;
///   &lt;cf:HubSection Header="one"&gt;&lt;DataTemplate&gt;…&lt;/DataTemplate&gt;&lt;/cf:HubSection&gt;
///   &lt;cf:HubSection Header="two"&gt;&lt;DataTemplate&gt;…&lt;/DataTemplate&gt;&lt;/cf:HubSection&gt;
/// &lt;/cf:Hub&gt;
/// </code>
/// </example>
[TemplatePart("PART_ScrollViewer", typeof(Control))]
[TemplatePart("PART_Panel", typeof(Panel))]
[TemplatePart("PART_HeaderHost", typeof(Control))]
[TemplatePart("PART_Background", typeof(Control))]
[TemplatePart("PART_MainBackground", typeof(Control))]
[TemplatePart("PART_WrapBackground", typeof(Control))]
[PseudoClasses(BackgroundHiddenPseudoClass, BackgroundShowingPseudoClass)]
public class Hub : TemplatedControl
{
    /// <summary>Defines <see cref="Header"/>.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<Hub, object?>(nameof(Header));

    /// <summary>Defines <see cref="HeaderTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<Hub, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines <see cref="DefaultSectionIndex"/>.</summary>
    public static readonly StyledProperty<int> DefaultSectionIndexProperty =
        AvaloniaProperty.Register<Hub, int>(nameof(DefaultSectionIndex));

    /// <summary>A section's width, as a fraction of the Hub's: 405 of 450, 360 of 400, 441.82 of 490.91 (#137's dumps).</summary>
    internal const double SectionWidthRatio = 0.9;

    private const string BackgroundHiddenPseudoClass = ":backgroundhidden";
    private const string BackgroundShowingPseudoClass = ":backgroundshowing";

    private readonly AvaloniaList<HubSection> _sections = [];
    private readonly HubMotion _motion;
    private readonly TranslateTransform _panelTranslation = new();
    private readonly TranslateTransform _headerTranslation = new();
    private readonly TranslateTransform _backgroundTranslation = new();
    private readonly TranslateTransform _wrapTranslation = new();
    private readonly SectionHeaderList _sectionHeaders;
    private List<HubSection> _inView = [];
    private Control? _scrollViewer;
    private Panel? _panel;
    private Control? _headerHost;
    private Control? _background;
    private Control? _wrapBackground;
    private ImageBrush? _imageBrush;
    private TopLevel? _topLevel;
    private double[] _starts = [];
    private double _length;
    private double _view;
    private Size _size;
    private HubSection? _restSection;
    private HubSection? _pending;

    /// <summary>Initialises an empty Hub.</summary>
    public Hub()
    {
        _motion = new HubMotion(this);
        _sectionHeaders = new SectionHeaderList(_sections);
        GestureRecognizers.Add(new HubPanGestureRecognizer(this));
        _sections.CollectionChanged += OnSectionsChanged;
        UpdateBackgroundState();
    }

    /// <summary>Raised when a section's header is tapped or clicked, if it is <see cref="HubSection.IsHeaderInteractive"/>.</summary>
    public event EventHandler<HubSectionHeaderClickEventArgs>? SectionHeaderClick;

    /// <summary>Raised when sections come into view or go out of it.</summary>
    public event EventHandler<SectionsInViewChangedEventArgs>? SectionsInViewChanged;

    /// <summary>What the template shows above the sections, large; the phone's is the app's name.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The template for <see cref="Header"/>.</summary>
    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>
    /// The index of the section shown first, at the left, when the Hub is shown in a window. 0 by default; an index
    /// past the last shows the last.
    /// </summary>
    public int DefaultSectionIndex
    {
        get => GetValue(DefaultSectionIndexProperty);
        set => SetValue(DefaultSectionIndexProperty, value);
    }

    /// <summary>The Hub's sections, in order. In XAML, the Hub's content.</summary>
    [Content]
    public IList<HubSection> Sections => _sections;

    /// <summary>Each section's <see cref="HubSection.Header"/>, in the sections' order, as they are now.</summary>
    public IReadOnlyList<object?> SectionHeaders => _sectionHeaders;

    /// <summary>The sections any part of which is in view, from left to right.</summary>
    public IReadOnlyList<HubSection> SectionsInView => _inView;

    internal HubMotion Motion => _motion;

    /// <summary>Each section's left edge at an offset of 0, in view pixels from the first's.</summary>
    internal IReadOnlyList<double> SectionStarts => _starts;

    /// <summary>The sections' width in all, the first section's place after the last.</summary>
    internal double SectionsLength => _length;

    /// <summary>The width of the view the sections move in.</summary>
    internal double ViewWidth => _view;

    /// <summary>Whether a finger can pan the Hub now: enabled, with somewhere to go.</summary>
    internal bool CanPan => IsEffectivelyEnabled && _starts.Length > 1 && _view > 0;

    /// <summary>Whether <paramref name="point"/>, in the Hub's coordinates, is in the part a pan starts in.</summary>
    internal bool IsInPanArea(Point point)
    {
        if (_scrollViewer is null) return new Rect(Bounds.Size).Contains(point);
        return this.TranslatePoint(point, _scrollViewer) is { } local && new Rect(_scrollViewer.Bounds.Size).Contains(local);
    }

    /// <summary>Moves the sections at once so that <paramref name="section"/> is at the left, or as far as they go.</summary>
    public void ScrollToSection(HubSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (!_sections.Contains(section)) return;
        _restSection = section;
        if (_starts.Length == _sections.Count && _starts.Length > 0)
        {
            _pending = null;
            _motion.Rest(StartOf(section));
        }
        else
        {
            _pending = section;
        }
    }

    /// <summary>
    /// Draws the motion's state: the sections, the first past the last when it wraps, the header, the background and
    /// its copy; and works out which sections are in view.
    /// </summary>
    internal void ApplyMotion()
    {
        var offset = _motion.Offset;
        _panelTranslation.X = -offset;
        _headerTranslation.X = _motion.HeaderX;

        var wrapped = _starts.Length > 1 && offset >= _starts[1];
        if (_sections.Count > 0)
            _sections[0].WrapTranslation = wrapped ? _length : 0;

        ApplyBackground(offset);
        UpdateSectionsInView(offset, wrapped);

        if (!_motion.IsMoving && !_motion.IsDragging && _starts.Length == _sections.Count)
        {
            for (var i = 0; i < _starts.Length; i++)
            {
                if (_starts[i] == offset)
                    _restSection = _sections[i];
            }
        }
    }

    internal void RaiseSectionHeaderClick(HubSection section) =>
        SectionHeaderClick?.Invoke(this, new HubSectionHeaderClickEventArgs(section));

    /// <summary>
    /// The background image's left edge for an offset: until the offset is the sections' width less the view's, it
    /// moves (W − V) / (L − V) of the offset, so its right edge reaches the view's as the last section's does; then
    /// (V − 1) / V, so that at L, where the first section is back at its place, its copy W − 1 to the right is at 0
    /// (#137: 0.982906 and 0.384615 for 1600 and 900 px images, within 0.001 px).
    /// </summary>
    internal static double BackgroundX(double offset, double imageWidth, double length, double view)
    {
        var knee = length - view;
        if (knee <= 0 || view <= 0) return 0;
        if (offset <= knee) return -offset * (imageWidth - view) / knee;
        return -(imageWidth - view) - (offset - knee) * (view - 1) / view;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_panel is { } oldPanel)
        {
            foreach (var section in _sections)
                oldPanel.Children.Remove(section);
            if (ReferenceEquals(oldPanel.RenderTransform, _panelTranslation))
                oldPanel.RenderTransform = null;
        }

        ClearTransform(_headerHost, _headerTranslation);
        ClearTransform(_background, _backgroundTranslation);
        ClearTransform(_wrapBackground, _wrapTranslation);

        _scrollViewer = e.NameScope.Find<Control>("PART_ScrollViewer");
        _panel = e.NameScope.Find<Panel>("PART_Panel");
        _headerHost = e.NameScope.Find<Control>("PART_HeaderHost");
        _background = e.NameScope.Find<Control>("PART_Background");
        _wrapBackground = e.NameScope.Find<Control>("PART_WrapBackground");

        if (_panel is { } panel)
        {
            panel.RenderTransform = _panelTranslation;
            foreach (var section in _sections)
                panel.Children.Add(section);
        }

        if (_headerHost is not null) _headerHost.RenderTransform = _headerTranslation;
        if (_background is not null) _background.RenderTransform = _backgroundTranslation;
        if (_wrapBackground is not null) _wrapBackground.RenderTransform = _wrapTranslation;
        if (_motion.IsMoving || _motion.IsDragging)
            _motion.Finish();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = !double.IsInfinity(availableSize.Width) ? availableSize.Width : _topLevel?.ClientSize.Width ?? double.NaN;
        var sectionWidth = width > 0 ? width * SectionWidthRatio : double.NaN;
        double headerHeight = double.NaN;
        if (_headerHost is { } host)
        {
            host.Measure(Size.Infinity);
            headerHeight = Math.Max(0, host.DesiredSize.Height - host.Margin.Top - host.Margin.Bottom);
        }

        foreach (var section in _sections)
        {
            section.HubWidth = sectionWidth;
            section.HubHeaderHeight = headerHeight;
        }

        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _size = finalSize;
        return base.ArrangeOverride(finalSize);
    }

    /// <summary>
    /// After each layout pass, reads where the panel put the sections (a pass can lay out the panel alone, after the
    /// Hub's own arrange) and, at rest, keeps the section that was at the left there.
    /// </summary>
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var changed = UpdateLayoutOfSections();
        if (_motion.IsMoving && changed)
            _motion.Finish();

        if (_motion.IsMoving || _motion.IsDragging || _starts.Length != _sections.Count)
            return;

        var section = _pending ?? _restSection;
        if (_pending is not null && _starts.Length > 0)
            _pending = null;
        var offset = section is null ? 0 : StartOf(section);
        if (changed || offset != _motion.Offset)
            _motion.Rest(offset);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
        LayoutUpdated += OnLayoutUpdated;
        // Shown in a window, the Hub starts at its default section, unless code asked for another.
        _pending ??= DefaultSection();
        InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is not null)
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
        _topLevel = null;
        LayoutUpdated -= OnLayoutUpdated;
        if (_motion.IsMoving || _motion.IsDragging)
            _motion.Finish();
        _motion.StopFrames();
        _pending = null;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BackgroundProperty)
        {
            if (_imageBrush is not null)
                _imageBrush.PropertyChanged -= OnImageBrushPropertyChanged;
            _imageBrush = change.GetNewValue<IBrush?>() as ImageBrush;
            if (_imageBrush is not null)
                _imageBrush.PropertyChanged += OnImageBrushPropertyChanged;
            UpdateBackgroundState();
            ApplyBackground(_motion.Offset);
        }
    }

    private void OnImageBrushPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ImageBrush.SourceProperty) return;
        UpdateBackgroundState();
        ApplyBackground(_motion.Offset);
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
        {
            InvalidateMeasure();
            ApplyBackground(_motion.Offset);
        }
    }

    private void OnSectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (HubSection section in e.OldItems)
                Detach(section);
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var section in LogicalChildren.OfType<HubSection>().ToList())
                Detach(section);
        }

        if (e.NewItems is not null)
        {
            var at = e.NewStartingIndex;
            foreach (HubSection section in e.NewItems)
            {
                section.Owner = this;
                LogicalChildren.Add(section);
                _panel?.Children.Insert(Math.Min(at < 0 ? _panel.Children.Count : at, _panel.Children.Count), section);
                if (at >= 0) at++;
            }
        }

        if (_restSection is not null && !_sections.Contains(_restSection))
            _restSection = null;
        if (_pending is not null && !_sections.Contains(_pending))
            _pending = null;
        if (_motion.IsMoving || _motion.IsDragging)
            _motion.Finish();
        InvalidateMeasure();
    }

    private void Detach(HubSection section)
    {
        section.Owner = null;
        section.WrapTranslation = 0;
        section.HubWidth = double.NaN;
        section.HubHeaderHeight = double.NaN;
        _panel?.Children.Remove(section);
        LogicalChildren.Remove(section);
    }

    private HubSection? DefaultSection() =>
        _sections.Count == 0 ? null : _sections[Math.Clamp(DefaultSectionIndex, 0, _sections.Count - 1)];

    private double StartOf(HubSection section)
    {
        var index = _sections.IndexOf(section);
        return index >= 0 && index < _starts.Length ? _starts[index] : 0;
    }

    /// <summary>Reads where the panel laid the sections out; whether that changed.</summary>
    private bool UpdateLayoutOfSections()
    {
        var starts = new double[_sections.Count];
        double length = 0;
        for (var i = 0; i < _sections.Count; i++)
        {
            var section = _sections[i];
            starts[i] = section.Bounds.X - section.Margin.Left;
            length = section.Bounds.Right + section.Margin.Right;
        }

        if (starts.Length > 0)
        {
            var origin = starts[0];
            for (var i = 0; i < starts.Length; i++) starts[i] -= origin;
            length -= origin;
        }

        var view = _scrollViewer is { Bounds.Width: > 0 } scroll ? scroll.Bounds.Width : Bounds.Width;
        var changed = !starts.AsSpan().SequenceEqual(_starts) || length != _length || view != _view;
        _starts = starts;
        _length = length;
        _view = view;
        return changed;
    }

    private void UpdateBackgroundState()
    {
        var hidden = Background is ImageBrush { Source: null };
        PseudoClasses.Set(BackgroundHiddenPseudoClass, hidden);
        PseudoClasses.Set(BackgroundShowingPseudoClass, !hidden);
    }

    /// <summary>
    /// Sizes and places the background: an image at its own pixel width by the window's height, moved by
    /// <see cref="BackgroundX"/>, with its copy W − 1 to the left until the offset is half the sections' width less the
    /// view's, and to the right from there (#137); any other brush over the whole Hub, still.
    /// </summary>
    private void ApplyBackground(double offset)
    {
        if (_background is not { } background) return;
        if (Background is ImageBrush { Source: Bitmap bitmap })
        {
            var width = (double)bitmap.PixelSize.Width;
            var height = _topLevel?.ClientSize.Height ?? _size.Height;
            SetSize(background, width, height);
            _backgroundTranslation.X = BackgroundX(offset, width, _length, _view);
            _wrapTranslation.X = offset >= (_length - _view) / 2 ? width - 1 : -(width - 1);
            if (_wrapBackground is not null) _wrapBackground.IsVisible = true;
        }
        else
        {
            SetSize(background, _size.Width, _size.Height);
            _backgroundTranslation.X = 0;
            _wrapTranslation.X = 0;
            if (_wrapBackground is not null) _wrapBackground.IsVisible = false;
        }
    }

    private static void SetSize(Layoutable element, double width, double height)
    {
        if (element.Width != width) element.Width = width;
        if (element.Height != height) element.Height = height;
    }

    private void UpdateSectionsInView(double offset, bool wrapped)
    {
        if (_starts.Length != _sections.Count) return;
        var now = new List<(double X, HubSection Section)>();
        for (var i = 0; i < _sections.Count; i++)
        {
            var section = _sections[i];
            var x = _starts[i] - offset + (i == 0 && wrapped ? _length : 0);
            var end = (i + 1 < _starts.Length ? _starts[i + 1] : _length) - _starts[i];
            if (x < _view && x + end > 0)
                now.Add((x, section));
        }

        var inView = now.OrderBy(p => p.X).Select(p => p.Section).ToList();
        if (inView.SequenceEqual(_inView)) return;
        var added = inView.Where(s => !_inView.Contains(s)).ToList();
        var removed = _inView.Where(s => !inView.Contains(s)).ToList();
        _inView = inView;
        if (added.Count > 0 || removed.Count > 0)
            SectionsInViewChanged?.Invoke(this, new SectionsInViewChangedEventArgs(added, removed));
    }

    private static void ClearTransform(Control? element, Transform transform)
    {
        if (element is not null && ReferenceEquals(element.RenderTransform, transform))
            element.RenderTransform = null;
    }

    /// <summary>The sections' headers, read as they are.</summary>
    private sealed class SectionHeaderList(IList<HubSection> sections) : IReadOnlyList<object?>
    {
        public object? this[int index] => sections[index].Header;

        public int Count => sections.Count;

        public IEnumerator<object?> GetEnumerator()
        {
            foreach (var section in sections)
                yield return section.Header;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
