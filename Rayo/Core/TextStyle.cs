namespace Rayo.Core;

/// <summary>
/// Font weight, following Avalonia/CSS conventions (numeric values 100–900).
/// Practically, the renderer can only distinguish Normal and Bold without a
/// dedicated bold font; intermediate values are clamped to the nearest supported level.
/// </summary>
public enum FontWeight
{
    Thin       = 100,
    ExtraLight = 200,
    Light      = 300,
    Normal     = 400,
    Medium     = 500,
    SemiBold   = 600,
    Bold       = 700,
    ExtraBold  = 800,
    Black      = 900,
}

/// <summary>
/// Font style — Normal or Italic.
/// Italic rendering requires a separate italic font variant registered in AssetManager
/// under the name "{FontFamily}-Italic". Without it the text renders in Normal style.
/// </summary>
public enum FontStyle
{
    Normal,
    Italic,
}

/// <summary>
/// Text decoration flags. Multiple values can be combined.
/// Decorations are rendered as geometric lines over / under the text using the Foreground colour.
/// </summary>
[System.Flags]
public enum TextDecorations
{
    None          = 0,
    Underline     = 1 << 0,
    Strikethrough = 1 << 1,
    Overline      = 1 << 2,
}
