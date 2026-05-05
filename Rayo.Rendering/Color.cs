namespace Rayo.Rendering;

/// <summary>
/// RGBA color structure (0-1 range per channel).
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    private const float Epsilon = 0.0001f;

    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }

    #region Constructors

    public Color(float r, float g, float b, float a = 1f)
    {
        R = Clamp01(r);
        G = Clamp01(g);
        B = Clamp01(b);
        A = Clamp01(a);
    }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r / 255f;
        G = g / 255f;
        B = b / 255f;
        A = a / 255f;
    }

    public Color(int r, int g, int b, int a = 255)
        : this(
            (byte)Math.Clamp(r, 0, 255),
            (byte)Math.Clamp(g, 0, 255),
            (byte)Math.Clamp(b, 0, 255),
            (byte)Math.Clamp(a, 0, 255))
    {
    }

    #endregion

    #region Static Helpers

    private static float Clamp01(float v)
        => v < 0f ? 0f : (v > 1f ? 1f : v);

    #endregion

    #region Withers

    public Color WithAlpha(float alpha)
        => new(R, G, B, alpha);

    #endregion

    #region Equality

    public bool Equals(Color other)
    {
        return MathF.Abs(R - other.R) < Epsilon &&
               MathF.Abs(G - other.G) < Epsilon &&
               MathF.Abs(B - other.B) < Epsilon &&
               MathF.Abs(A - other.A) < Epsilon;
    }

    public override bool Equals(object? obj)
        => obj is Color other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    public static bool operator ==(Color left, Color right)
        => left.Equals(right);

    public static bool operator !=(Color left, Color right)
        => !left.Equals(right);

    #endregion

    #region Operators

    public static Color operator +(Color c1, Color c2)
        => new(
            c1.R + c2.R,
            c1.G + c2.G,
            c1.B + c2.B,
            c1.A + c2.A);

    public static Color operator -(Color c1, Color c2)
        => new(
            c1.R - c2.R,
            c1.G - c2.G,
            c1.B - c2.B,
            c1.A - c2.A);

    public static Color operator *(Color c, float scalar)
        => new(
            c.R * scalar,
            c.G * scalar,
            c.B * scalar,
            c.A * scalar);

    public static Color operator *(float scalar, Color c)
        => c * scalar;

    public static Color operator /(Color c, float scalar)
        => scalar == 0f
            ? throw new DivideByZeroException()
            : new(
                c.R / scalar,
                c.G / scalar,
                c.B / scalar,
                c.A / scalar);

    #endregion

    #region Utilities

    public (byte R, byte G, byte B, byte A) ToBytes()
        => (
            (byte)(Clamp01(R) * 255f),
            (byte)(Clamp01(G) * 255f),
            (byte)(Clamp01(B) * 255f),
            (byte)(Clamp01(A) * 255f)
        );

    #endregion

    #region Predefined Colors
    // Basic colors
    public static Color White => new(1f, 1f, 1f, 1f);
    public static Color Black => new(0f, 0f, 0f, 1f);
    public static Color Red => new(1f, 0f, 0f, 1f);
    public static Color Green => new(0f, 1f, 0f, 1f);
    public static Color Blue => new(0f, 0f, 1f, 1f);
    public static Color Transparent => new(0f, 0f, 0f, 0f);

    // Extended colors
    public static Color Yellow => new(1f, 1f, 0f, 1f);
    public static Color Cyan => new(0f, 1f, 1f, 1f);
    public static Color Magenta => new(1f, 0f, 1f, 1f);
    public static Color Orange => new(1f, 0.5f, 0f, 1f);
    public static Color Purple => new(0.5f, 0f, 0.5f, 1f);
    public static Color Gray => new(0.5f, 0.5f, 0.5f, 1f);
    public static Color LightGray => new(0.75f, 0.75f, 0.75f, 1f);
    public static Color DarkGray => new(0.25f, 0.25f, 0.25f, 1f);

    // Additional named colors
    public static Color Brown => new(0.6f, 0.4f, 0.2f, 1f);
    public static Color Pink => new(1f, 0.75f, 0.8f, 1f);
    public static Color Lime => new(0.75f, 1f, 0f, 1f);
    public static Color Navy => new(0f, 0f, 0.5f, 1f);
    public static Color Teal => new(0f, 0.5f, 0.5f, 1f);
    public static Color Olive => new(0.5f, 0.5f, 0f, 1f);
    public static Color Maroon => new(0.5f, 0f, 0f, 1f);
    public static Color Silver => new(0.75f, 0.75f, 0.75f, 1f);
    public static Color Gold => new(1f, 0.84f, 0f, 1f);
    public static Color Coral => new(1f, 0.5f, 0.31f, 1f);
    public static Color Indigo => new(0.29f, 0f, 0.51f, 1f);
    public static Color Violet => new(0.56f, 0.0f, 1f, 1f);
    public static Color AliceBlue => new(240 / 255f, 248 / 255f, 255 / 255f, 1f);
    public static Color AntiqueWhite => new(250 / 255f, 235 / 255f, 215 / 255f, 1f);
    public static Color Aqua => new(0f, 1f, 1f, 1f);
    public static Color Aquamarine => new(127 / 255f, 1f, 212 / 255f, 1f);
    public static Color Azure => new(240 / 255f, 255 / 255f, 255 / 255f, 1f);
    public static Color Beige => new(245 / 255f, 245 / 255f, 220 / 255f, 1f);
    public static Color Bisque => new(255 / 255f, 228 / 255f, 196 / 255f, 1f);
    public static Color BlanchedAlmond => new(255 / 255f, 235 / 255f, 205 / 255f, 1f);
    public static Color BlueViolet => new(138 / 255f, 43 / 255f, 226 / 255f, 1f);
    public static Color BurlyWood => new(222 / 255f, 184 / 255f, 135 / 255f, 1f);
    public static Color CadetBlue => new(95 / 255f, 158 / 255f, 160 / 255f, 1f);
    public static Color Chartreuse => new(127 / 255f, 1f, 0f, 1f);
    public static Color Chocolate => new(210 / 255f, 105 / 255f, 30 / 255f, 1f);
    public static Color CornflowerBlue => new(100 / 255f, 149 / 255f, 237 / 255f, 1f);
    public static Color Cornsilk => new(255 / 255f, 248 / 255f, 220 / 255f, 1f);
    public static Color Crimson => new(220 / 255f, 20 / 255f, 60 / 255f, 1f);
    public static Color DarkBlue => new(0f, 0f, 139 / 255f, 1f);
    public static Color DarkCyan => new(0f, 139 / 255f, 139 / 255f, 1f);
    public static Color DarkGoldenrod => new(184 / 255f, 134 / 255f, 11 / 255f, 1f);
    public static Color DarkGreen => new(0f, 100 / 255f, 0f, 1f);
    public static Color DarkKhaki => new(189 / 255f, 183 / 255f, 107 / 255f, 1f);
    public static Color DarkMagenta => new(139 / 255f, 0f, 139 / 255f, 1f);
    public static Color DarkOliveGreen => new(85 / 255f, 107 / 255f, 47 / 255f, 1f);
    public static Color DarkOrange => new(1f, 140 / 255f, 0f, 1f);
    public static Color DarkOrchid => new(153 / 255f, 50 / 255f, 204 / 255f, 1f);
    public static Color DarkRed => new(139 / 255f, 0f, 0f, 1f);
    public static Color DarkSalmon => new(233 / 255f, 150 / 255f, 122 / 255f, 1f);
    public static Color DarkSeaGreen => new(143 / 255f, 188 / 255f, 143 / 255f, 1f);
    public static Color DarkSlateBlue => new(72 / 255f, 61 / 255f, 139 / 255f, 1f);
    public static Color DarkSlateGray => new(47 / 255f, 79 / 255f, 79 / 255f, 1f);
    public static Color DarkTurquoise => new(0f, 206 / 255f, 209 / 255f, 1f);
    public static Color DarkViolet => new(148 / 255f, 0f, 211 / 255f, 1f);
    public static Color DeepPink => new(1f, 20 / 255f, 147 / 255f, 1f);
    public static Color DeepSkyBlue => new(0f, 191 / 255f, 1f, 1f);
    public static Color DimGray => new(105 / 255f, 105 / 255f, 105 / 255f, 1f);
    public static Color DodgerBlue => new(30 / 255f, 144 / 255f, 1f, 1f);
    public static Color Firebrick => new(178 / 255f, 34 / 255f, 34 / 255f, 1f);
    public static Color FloralWhite => new(255 / 255f, 250 / 255f, 240 / 255f, 1f);
    public static Color ForestGreen => new(34 / 255f, 139 / 255f, 34 / 255f, 1f);
    public static Color Gainsboro => new(220 / 255f, 220 / 255f, 220 / 255f, 1f);
    public static Color GhostWhite => new(248 / 255f, 248 / 255f, 255 / 255f, 1f);
    public static Color Goldenrod => new(218 / 255f, 165 / 255f, 32 / 255f, 1f);
    public static Color GreenYellow => new(173 / 255f, 1f, 47 / 255f, 1f);
    public static Color Honeydew => new(240 / 255f, 1f, 240 / 255f, 1f);
    public static Color HotPink => new(1f, 105 / 255f, 180 / 255f, 1f);
    public static Color IndianRed => new(205 / 255f, 92 / 255f, 92 / 255f, 1f);
    public static Color Ivory => new(1f, 1f, 240 / 255f, 1f);
    public static Color Khaki => new(240 / 255f, 230 / 255f, 140 / 255f, 1f);
    public static Color Lavender => new(230 / 255f, 230 / 255f, 250 / 255f, 1f);
    public static Color LavenderBlush => new(1f, 240 / 255f, 245 / 255f, 1f);
    public static Color LawnGreen => new(124 / 255f, 252 / 255f, 0f, 1f);
    public static Color LemonChiffon => new(1f, 250 / 255f, 205 / 255f, 1f);
    public static Color LightBlue => new(173 / 255f, 216 / 255f, 230 / 255f, 1f);
    public static Color LightCoral => new(240 / 255f, 128 / 255f, 128 / 255f, 1f);
    public static Color LightCyan => new(224 / 255f, 1f, 1f, 1f);
    public static Color LightGoldenrodYellow => new(250 / 255f, 250 / 255f, 210 / 255f, 1f);
    public static Color LightGreen => new(144 / 255f, 238 / 255f, 144 / 255f, 1f);
    public static Color LightPink => new(1f, 182 / 255f, 193 / 255f, 1f);
    public static Color LightSalmon => new(1f, 160 / 255f, 122 / 255f, 1f);
    public static Color LightSeaGreen => new(32 / 255f, 178 / 255f, 170 / 255f, 1f);
    public static Color LightSkyBlue => new(135 / 255f, 206 / 255f, 250 / 255f, 1f);
    public static Color LightSlateGray => new(119 / 255f, 136 / 255f, 153 / 255f, 1f);
    public static Color LightSteelBlue => new(176 / 255f, 196 / 255f, 222 / 255f, 1f);
    public static Color LightYellow => new(1f, 1f, 224 / 255f, 1f);
    public static Color LimeGreen => new(50 / 255f, 205 / 255f, 50 / 255f, 1f);
    public static Color Linen => new(250 / 255f, 240 / 255f, 230 / 255f, 1f);
    public static Color MediumAquamarine => new(102 / 255f, 205 / 255f, 170 / 255f, 1f);
    public static Color MediumBlue => new(0f, 0f, 205 / 255f, 1f);
    public static Color MediumOrchid => new(186 / 255f, 85 / 255f, 211 / 255f, 1f);
    public static Color MediumPurple => new(147 / 255f, 112 / 255f, 219 / 255f, 1f);
    public static Color MediumSeaGreen => new(60 / 255f, 179 / 255f, 113 / 255f, 1f);
    public static Color MediumSlateBlue => new(123 / 255f, 104 / 255f, 238 / 255f, 1f);
    public static Color MediumSpringGreen => new(0f, 250 / 255f, 154 / 255f, 1f);
    public static Color MediumTurquoise => new(72 / 255f, 209 / 255f, 204 / 255f, 1f);
    public static Color MediumVioletRed => new(199 / 255f, 21 / 255f, 133 / 255f, 1f);
    public static Color MidnightBlue => new(25 / 255f, 25 / 255f, 112 / 255f, 1f);
    public static Color MintCream => new(245 / 255f, 1f, 250 / 255f, 1f);
    public static Color MistyRose => new(1f, 228 / 255f, 225 / 255f, 1f);
    public static Color Moccasin => new(1f, 228 / 255f, 181 / 255f, 1f);
    public static Color NavajoWhite => new(1f, 222 / 255f, 173 / 255f, 1f);
    public static Color OldLace => new(253 / 255f, 245 / 255f, 230 / 255f, 1f);
    public static Color OrangeRed => new(1f, 69 / 255f, 0f, 1f);
    public static Color Orchid => new(218 / 255f, 112 / 255f, 214 / 255f, 1f);
    public static Color PaleGoldenrod => new(238 / 255f, 232 / 255f, 170 / 255f, 1f);
    public static Color PaleGreen => new(152 / 255f, 251 / 255f, 152 / 255f, 1f);
    public static Color PaleTurquoise => new(175 / 255f, 238 / 255f, 238 / 255f, 1f);
    public static Color PaleVioletRed => new(219 / 255f, 112 / 255f, 147 / 255f, 1f);
    public static Color PapayaWhip => new(1f, 239 / 255f, 213 / 255f, 1f);
    public static Color PeachPuff => new(1f, 218 / 255f, 185 / 255f, 1f);
    public static Color Peru => new(205 / 255f, 133 / 255f, 63 / 255f, 1f);
    public static Color Plum => new(221 / 255f, 160 / 255f, 221 / 255f, 1f);
    public static Color PowderBlue => new(176 / 255f, 224 / 255f, 230 / 255f, 1f);
    public static Color RosyBrown => new(188 / 255f, 143 / 255f, 143 / 255f, 1f);
    public static Color RoyalBlue => new(65 / 255f, 105 / 255f, 225 / 255f, 1f);
    public static Color SaddleBrown => new(139 / 255f, 69 / 255f, 19 / 255f, 1f);
    public static Color Salmon => new(250 / 255f, 128 / 255f, 114 / 255f, 1f);
    public static Color SandyBrown => new(244 / 255f, 164 / 255f, 96 / 255f, 1f);
    public static Color SeaGreen => new(46 / 255f, 139 / 255f, 87 / 255f, 1f);
    public static Color Seashell => new(1f, 245 / 255f, 238 / 255f, 1f);
    public static Color Sienna => new(160 / 255f, 82 / 255f, 45 / 255f, 1f);
    public static Color SkyBlue => new(135 / 255f, 206 / 255f, 235 / 255f, 1f);
    public static Color SlateBlue => new(106 / 255f, 90 / 255f, 205 / 255f, 1f);
    public static Color SlateGray => new(112 / 255f, 128 / 255f, 144 / 255f, 1f);
    public static Color Snow => new(1f, 250 / 255f, 250 / 255f, 1f);
    public static Color SpringGreen => new(0f, 1f, 127 / 255f, 1f);
    public static Color SteelBlue => new(70 / 255f, 130 / 255f, 180 / 255f, 1f);
    public static Color Tan => new(210 / 255f, 180 / 255f, 140 / 255f, 1f);
    public static Color Thistle => new(216 / 255f, 191 / 255f, 216 / 255f, 1f);
    public static Color Tomato => new(1f, 99 / 255f, 71 / 255f, 1f);
    public static Color Turquoise => new(64 / 255f, 224 / 255f, 208 / 255f, 1f);
    public static Color Wheat => new(245 / 255f, 222 / 255f, 179 / 255f, 1f);
    public static Color WhiteSmoke => new(245 / 255f, 245 / 255f, 245 / 255f, 1f);
    public static Color YellowGreen => new(154 / 255f, 205 / 255f, 50 / 255f, 1f); 
    #endregion
}

