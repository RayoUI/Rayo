namespace Rayo.Styling;

using Rayo.Rendering;

/// <summary>Accessibility and perceptual color helpers used by theme authors.</summary>
public static class ThemeColorUtilities
{
    public static float ContrastRatio(Color foreground, Color background)
    {
        var lighter = MathF.Max(RelativeLuminance(foreground), RelativeLuminance(background));
        var darker = MathF.Min(RelativeLuminance(foreground), RelativeLuminance(background));
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    public static bool MeetsWcagAA(Color foreground, Color background, bool largeText = false) =>
        ContrastRatio(foreground, background) >= (largeText ? 3f : 4.5f);

    /// <summary>
    /// Changes perceived lightness in OKLab while retaining chroma and hue.
    /// </summary>
    public static Color AdjustLightness(Color color, float delta)
    {
        var r = SrgbToLinear(color.R);
        var g = SrgbToLinear(color.G);
        var b = SrgbToLinear(color.B);

        var l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
        var m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
        var s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

        var lRoot = MathF.Cbrt(l);
        var mRoot = MathF.Cbrt(m);
        var sRoot = MathF.Cbrt(s);

        var lightness = Math.Clamp(
            0.2104542553f * lRoot + 0.793617785f * mRoot - 0.0040720468f * sRoot + delta,
            0,
            1);
        var a = 1.9779984951f * lRoot - 2.428592205f * mRoot + 0.4505937099f * sRoot;
        var labB = 0.0259040371f * lRoot + 0.7827717662f * mRoot - 0.808675766f * sRoot;

        var newL = lightness + 0.3963377774f * a + 0.2158037573f * labB;
        var newM = lightness - 0.1055613458f * a - 0.0638541728f * labB;
        var newS = lightness - 0.0894841775f * a - 1.291485548f * labB;
        newL *= newL * newL;
        newM *= newM * newM;
        newS *= newS * newS;

        return new Color(
            LinearToSrgb(+4.0767416621f * newL - 3.3077115913f * newM + 0.2309699292f * newS),
            LinearToSrgb(-1.2684380046f * newL + 2.6097574011f * newM - 0.3413193965f * newS),
            LinearToSrgb(-0.0041960863f * newL - 0.7034186147f * newM + 1.707614701f * newS),
            color.A);
    }

    private static float RelativeLuminance(Color color) =>
        0.2126f * SrgbToLinear(color.R) +
        0.7152f * SrgbToLinear(color.G) +
        0.0722f * SrgbToLinear(color.B);

    private static float SrgbToLinear(float value) =>
        value <= 0.04045f
            ? value / 12.92f
            : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);

    private static float LinearToSrgb(float value)
    {
        value = Math.Clamp(value, 0, 1);
        return value <= 0.0031308f
            ? 12.92f * value
            : 1.055f * MathF.Pow(value, 1f / 2.4f) - 0.055f;
    }
}
