using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// A popup in motion, as the phone's shell draws it: a picture of the popup taken as the motion starts, cut into
/// horizontal bands, each drawn turned by its own angle about its own centre line (<see cref="PhonePopupMotion.Band"/>).
/// <see cref="PhonePopupHost"/> lays it over the popup, which it hides meanwhile, and takes it away at rest. It takes no
/// hits.
/// </summary>
internal sealed class PhonePopupBands : Control
{
    private readonly RenderTargetBitmap _picture;
    private readonly double _scaling;
    private readonly Point _eye;

    /// <param name="picture">The popup, drawn at <paramref name="scaling"/>.</param>
    /// <param name="size">The popup's size, in view px.</param>
    /// <param name="scaling">Raw px a view px of the picture.</param>
    /// <param name="eye">The camera's centre, in the popup's own space.</param>
    /// <param name="bands">The bands, from the top, in the popup's own space.</param>
    public PhonePopupBands(RenderTargetBitmap picture, Size size, double scaling, Point eye, IReadOnlyList<Rect> bands)
    {
        _picture = picture;
        _scaling = scaling;
        _eye = eye;
        Bands = bands;
        Angles = new double[bands.Count];
        Width = size.Width;
        Height = size.Height;
        IsHitTestVisible = false;
    }

    /// <summary>The bands, from the top.</summary>
    public IReadOnlyList<Rect> Bands { get; }

    /// <summary>Each band's angle, in degrees; at <see cref="PhonePopupMotion.CloseTo"/> back, not drawn.</summary>
    public double[] Angles { get; }

    /// <summary>The camera's centre, in the popup's own space.</summary>
    public Point Eye => _eye;

    /// <summary>Sets the angles and draws them.</summary>
    public void Turn(Func<int, double> angle)
    {
        for (var i = 0; i < Angles.Length; i++)
            Angles[i] = angle(i);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        for (var i = 0; i < Bands.Count; i++)
        {
            if (Angles[i] <= -PhonePopupMotion.CloseTo)
                continue;
            var band = Bands[i];
            var source = new Rect(band.X * _scaling, band.Y * _scaling, band.Width * _scaling, band.Height * _scaling);
            using (context.PushTransform(PhonePopupMotion.Band(Angles[i], band.Center.Y, _eye)))
                context.DrawImage(_picture, source, band);
        }
    }

    /// <summary>Lets go of the picture.</summary>
    public void Release() => _picture.Dispose();
}
