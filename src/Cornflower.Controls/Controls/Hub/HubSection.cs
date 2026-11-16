using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Cornflower.Controls;

/// <summary>
/// One section of a <see cref="Hub"/>, WinRT's <c>HubSection</c>: a <see cref="Header"/> and content made from its
/// <see cref="ContentTemplate"/>, side by side with the other sections under the Hub's header.
/// </summary>
/// <remarks>
/// <para>
/// Behaviour only, as WinRT's is. Its content is a template, not an element, as on the phone: the section builds it once,
/// and it takes the section's <c>DataContext</c>. When it is the first and the Hub wraps, the Hub draws it past the last
/// section (the phone's <c>WrappingTransform</c>, on its template's root; here on the section, at animation priority
/// while it lasts, as Avalonia hit tests nothing a template draws outside its control). The template's parts:
/// <c>PART_HubHeaderPlaceholder</c>, which the Hub makes as tall as its own header, so the section's header sits
/// below it; <c>PART_Header</c>, the header; and <c>PART_ContentPresenter</c>, where the content goes.
/// </para>
/// <para>
/// With <see cref="IsHeaderInteractive"/>, a tap or click on the header raises the Hub's
/// <see cref="Hub.SectionHeaderClick"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;cf:HubSection Header="recent"&gt;
///   &lt;DataTemplate&gt;&lt;TextBlock Text="…" /&gt;&lt;/DataTemplate&gt;
/// &lt;/cf:HubSection&gt;
/// </code>
/// </example>
[TemplatePart("PART_HubHeaderPlaceholder", typeof(Layoutable))]
[TemplatePart("PART_Header", typeof(Control))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
public class HubSection : TemplatedControl
{
    /// <summary>Defines <see cref="Header"/>.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<HubSection, object?>(nameof(Header));

    /// <summary>Defines <see cref="HeaderTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<HubSection, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines <see cref="ContentTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<HubSection, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Defines <see cref="IsHeaderInteractive"/>.</summary>
    public static readonly StyledProperty<bool> IsHeaderInteractiveProperty =
        AvaloniaProperty.Register<HubSection, bool>(nameof(IsHeaderInteractive));

    /// <summary>Defines <see cref="HorizontalContentAlignment"/>.</summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<HubSection>();

    /// <summary>Defines <see cref="VerticalContentAlignment"/>.</summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<HubSection>();

    private readonly TranslateTransform _wrap = new();
    private IDisposable? _wrapping;
    private Layoutable? _placeholder;
    private Control? _header;
    private ContentPresenter? _presenter;
    private Control? _content;
    private double _hubHeaderHeight = double.NaN;
    private double _hubWidth = double.NaN;

    /// <summary>Initialises a section with no header or content.</summary>
    public HubSection()
    {
        AddHandler(TappedEvent, OnTapped);
    }

    /// <summary>The section's header, above its content, through <see cref="HeaderTemplate"/>.</summary>
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
    /// The section's content, as a template, which the section builds once and shows below its header; the content
    /// takes the section's <c>DataContext</c>. In XAML, the section's own content.
    /// </summary>
    [Content]
    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <summary>
    /// Whether a tap or click on the header raises the Hub's <see cref="Hub.SectionHeaderClick"/>. False by default.
    /// </summary>
    public bool IsHeaderInteractive
    {
        get => GetValue(IsHeaderInteractiveProperty);
        set => SetValue(IsHeaderInteractiveProperty, value);
    }

    /// <summary>Where the content sits across the section, WinRT's <c>Control</c>'s; the phone's style sets Stretch.</summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>Where the content sits down the section, WinRT's <c>Control</c>'s; the phone's style sets Stretch.</summary>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    /// <summary>The Hub this section is in, if any.</summary>
    internal Hub? Owner { get; set; }

    /// <summary>The content built from <see cref="ContentTemplate"/>, if any.</summary>
    internal Control? BuiltContent => _content;

    /// <summary>How far right of its place the Hub draws the section: past the last when it wraps.</summary>
    internal double WrapTranslation
    {
        get => _wrap.X;
        set
        {
            _wrap.X = value;
            if (value != 0)
            {
                _wrapping ??= SetValue(RenderTransformProperty, _wrap, BindingPriority.Animation);
            }
            else
            {
                _wrapping?.Dispose();
                _wrapping = null;
            }
        }
    }

    /// <summary>The Hub's header's height, which the template's placeholder takes, so the section's header sits below it.</summary>
    internal double HubHeaderHeight
    {
        get => _hubHeaderHeight;
        set
        {
            if (_hubHeaderHeight.Equals(value)) return;
            _hubHeaderHeight = value;
            ApplyHubHeaderHeight();
        }
    }

    /// <summary>
    /// The width the Hub gives the section, 0.9 of its own (the phone's, on three profiles), unless the section has a
    /// <c>Width</c> of its own; NaN outside a Hub.
    /// </summary>
    internal double HubWidth
    {
        get => _hubWidth;
        set
        {
            if (_hubWidth.Equals(value)) return;
            _hubWidth = value;
            InvalidateMeasure();
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsNaN(_hubWidth) || !double.IsNaN(Width))
            return base.MeasureOverride(availableSize);
        var desired = base.MeasureOverride(availableSize.WithWidth(_hubWidth));
        return new Size(_hubWidth, desired.Height);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_presenter is { } oldPresenter && ReferenceEquals(oldPresenter.Content, _content))
            oldPresenter.Content = null;

        _placeholder = e.NameScope.Find<Layoutable>("PART_HubHeaderPlaceholder");
        _header = e.NameScope.Find<Control>("PART_Header");
        _presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        ApplyHubHeaderHeight();
        ShowContent();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentTemplateProperty)
        {
            // A new template makes new content.
            if (_presenter is { } presenter && ReferenceEquals(presenter.Content, _content))
                presenter.Content = null;
            _content = null;
            ShowContent();
        }
    }

    /// <summary>Builds the content once, from the template, and puts it in the template's presenter.</summary>
    private void ShowContent()
    {
        if (_content is null && ContentTemplate is not null)
            _content = ContentTemplate.Build(DataContext);
        if (_presenter is { } presenter)
            presenter.Content = _content;
    }

    private void ApplyHubHeaderHeight()
    {
        if (_placeholder is not { } placeholder) return;
        if (double.IsNaN(_hubHeaderHeight))
            placeholder.ClearValue(HeightProperty);
        else
            placeholder.Height = _hubHeaderHeight;
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsHeaderInteractive || Owner is not { } hub || _header is not { } header || e.Source is not Visual source)
            return;
        if (!ReferenceEquals(source, header) && !header.IsVisualAncestorOf(source))
            return;
        e.Handled = true;
        hub.RaiseSectionHeaderClick(this);
    }
}
