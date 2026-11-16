using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Cornflower.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>
/// The 8.1 Hub: four sections to pan across and loop round, which are in view and the last header clicked shown, and
/// switches for a background image and the light theme; a section that scrolls up and down, and an interactive header.
/// </summary>
[SamplePage("hub", Order = 122)]
public partial class HubPage : UserControl
{
    private readonly HubPageModel _model = new();
    private ImageBrush? _image;

    public HubPage()
    {
        InitializeComponent();
        for (var i = 1; i <= 30; i++)
            _model.Lines.Add($"line {i}");
        _model.PropertyChanged += OnModelChanged;
        Hub.DataContext = _model;
    }

    /// <summary>The page's state, the sections' data.</summary>
    internal HubPageModel State => _model;

    private void OnSectionHeaderClick(object? sender, HubSectionHeaderClickEventArgs e) =>
        _model.LastClick = $"clicked: {e.Section.Header}";

    private void OnSectionsInViewChanged(object? sender, SectionsInViewChangedEventArgs e) =>
        _model.InView = "in view: " + string.Join(", ", Hub.SectionsInView.Select(s => s.Header));

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HubPageModel.Image))
            Hub.Background = _model.Image ? _image ??= new ImageBrush(Stripes(1600, 800)) { Stretch = Stretch.Fill } : null;
        else if (e.PropertyName == nameof(HubPageModel.Light))
            Scope.RequestedThemeVariant = _model.Light ? ThemeVariant.Light : null;
    }

    /// <summary>
    /// A picture to tell the parallax by, as the reference app's probe draws one: a hue running left to right, dimmed,
    /// with a darker line every 50 px and a lighter one every 250.
    /// </summary>
    private static WriteableBitmap Stripes(int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var row = new int[width];
        for (var x = 0; x < width; x++)
        {
            var (r, g, b) = Hue(x * 360.0 / width);
            var scale = x % 250 == 0 ? 1.0 : x % 50 == 0 ? 0.15 : 0.45;
            row[x] = unchecked((int)(0xFF000000u | (uint)(r * scale) << 16 | (uint)(g * scale) << 8 | (uint)(b * scale)));
        }

        using var buffer = bitmap.Lock();
        for (var y = 0; y < height; y++)
            Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, width);
        return bitmap;
    }

    private static (double R, double G, double B) Hue(double degrees)
    {
        double Channel(double n)
        {
            var k = (n + degrees / 60) % 6;
            return 255 * (1 - Math.Max(0, Math.Min(Math.Min(k, 4 - k), 1)));
        }

        return (Channel(5), Channel(3), Channel(1));
    }
}

/// <summary>The Hub page's state, its sections' data: what they show and set.</summary>
internal sealed class HubPageModel : INotifyPropertyChanged
{
    private string _inView = "";
    private string _lastClick = "no header clicked yet";
    private bool _image;
    private bool _light;

    public event PropertyChangedEventHandler? PropertyChanged;

    public List<string> Lines { get; } = [];

    public string InView
    {
        get => _inView;
        set => Set(ref _inView, value);
    }

    public string LastClick
    {
        get => _lastClick;
        set => Set(ref _lastClick, value);
    }

    public bool Image
    {
        get => _image;
        set => Set(ref _image, value);
    }

    public bool Light
    {
        get => _light;
        set => Set(ref _light, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
