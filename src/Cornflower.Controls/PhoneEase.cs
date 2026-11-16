namespace Cornflower.Controls;

/// <summary>
/// WinRT's <c>ExponentialEase</c>, as the phone's motion was fitted with it (<c>tools/motion</c>): its ease-in is
/// (e^(k x) − 1) / (e^k − 1) for an exponent k, and its ease-out 1 − ease-in(1 − x). Every Cornflower timeline that
/// follows one eases through this: the Pivot's slide (exponent 4), the Hub's header coming back round the loop (7), the
/// page transitions (the turnstile's 6, the slide's 6 and 4.28, the continuum's 4.61 and 3.85) and the app bar's
/// opening and closing (4.5 and 5.5).
/// </summary>
/// <remarks>
/// The ease-out is worked out as (1 − e^(−k x)) / (1 − e^(−k)), the same curve: it can differ from the definition's
/// form in the last bit of a double, nowhere more.
/// </remarks>
internal static class PhoneEase
{
    /// <summary>WinRT's <c>ExponentialEase</c> with <c>EasingMode.EaseOut</c>, at <paramref name="progress"/> (0 to 1).</summary>
    public static double ExponentialOut(double exponent, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        return (1 - Math.Exp(-exponent * progress)) / (1 - Math.Exp(-exponent));
    }

    /// <summary>WinRT's <c>ExponentialEase</c> with <c>EasingMode.EaseIn</c>, at <paramref name="progress"/> (0 to 1).</summary>
    public static double ExponentialIn(double exponent, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        return (Math.Exp(exponent * progress) - 1) / (Math.Exp(exponent) - 1);
    }

    /// <summary>
    /// How far a value falling from <paramref name="from"/> to 0 on an ease-out over <paramref name="length"/> still is,
    /// <paramref name="time"/> in: from × (1 − ease).
    /// </summary>
    public static double Remaining(double from, double exponent, TimeSpan time, TimeSpan length) =>
        from * (1 - ExponentialOut(exponent, time / length));
}
