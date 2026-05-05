using Silk.NET.OpenGL;
using StbTrueTypeSharp;
using System.Numerics;

namespace Rayo.Rendering.OpenGL;

/// <summary>
/// OpenGL font atlas built with StbTrueType.
/// The internal glyph map is keyed on full Unicode codepoints (int) so that
/// emoji and other characters above U+FFFF (surrogate pairs) are supported.
/// </summary>
public unsafe class OpenGLFontAtlas : IFont
{
    private readonly GL _gl;
    private uint _textureId;
    private Dictionary<int, CharInfo> _glyphMap = new();
    private int _atlasWidth;
    private int _atlasHeight;
    private float _fontSize;
    private bool _isDisposed;

    // Font metrics
    private float _ascent;
    private float _descent;
    private float _lineGap;
    private float _scale;

    public struct CharInfo
    {
        public float X0, Y0, X1, Y1;  // Atlas UV coordinates (0-1)
        public float OffsetX, OffsetY; // Render offset
        public float AdvanceX;         // Pen advance to next glyph
        public float Width, Height;    // Glyph bitmap size
    }

    public string Name { get; private set; }
    public float Size => _fontSize;
    public uint TextureId => _textureId;

    /// <summary>
    /// When true this atlas was built with extended emoji/symbol ranges.
    /// </summary>
    public bool IsEmojiAtlas { get; private set; }

    /// <summary>
    /// Distance from the baseline to the top of the tallest glyph, in pixels (positive value).
    /// </summary>
    public float Ascent => _ascent;

    /// <summary>
    /// Distance from the baseline to the bottom of the lowest descender, in pixels (negative value).
    /// </summary>
    public float Descent => _descent;

    /// <summary>
    /// Recommended line height for this font.
    /// </summary>
    public float LineHeight => _ascent - _descent + _lineGap;

    public OpenGLFontAtlas(GL gl, byte[] fontData, float fontSize, string name = "Default", bool isEmojiAtlas = false)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));

        if (fontData == null || fontData.Length == 0)
            throw new ArgumentException("Font data cannot be null or empty", nameof(fontData));

        if (fontSize <= 0)
            throw new ArgumentException("Font size must be positive", nameof(fontSize));

        _fontSize = fontSize;
        Name = name;
        IsEmojiAtlas = isEmojiAtlas;

        // 4096×4096 atlas to fit large glyph sets (icon fonts, emoji).
        GenerateAtlas(fontData, fontSize, 4096, 4096);
    }

    protected OpenGLFontAtlas(GL gl, float fontSize, string name = "Default")
    {
        _gl = gl;
        _fontSize = fontSize;
        Name = name;
    }

    /// <summary>
    /// Returns the list of Unicode codepoints to bake into the atlas.
    /// Subclasses can override to restrict or extend the set.
    /// </summary>
    protected virtual List<int> GetCodePointsToRender()
    {
        var codePoints = new List<int>(512);

        // Basic Latin printable characters (U+0020 – U+007E)
        for (int i = 0x0020; i <= 0x007E; i++)
            codePoints.Add(i);

        // Latin-1 Supplement (U+00A0 – U+00FF) — accented characters, etc.
        for (int i = 0x00A0; i <= 0x00FF; i++)
            codePoints.Add(i);

        // Unicode Private Use Area (U+E000 – U+F8FF) — icon fonts (Lineicons, FontAwesome, etc.)
        for (int i = 0xE000; i <= 0xF8FF; i++)
            codePoints.Add(i);

        // Emoticons block (U+1F600 – U+1F64F) — common emoji like 😀😁😂
        for (int i = 0x1F600; i <= 0x1F64F; i++)
            codePoints.Add(i);

        // Miscellaneous Symbols and Pictographs (U+1F300 – U+1F5FF)
        for (int i = 0x1F300; i <= 0x1F5FF; i++)
            codePoints.Add(i);

        // Transport and Map Symbols (U+1F680 – U+1F6FF)
        for (int i = 0x1F680; i <= 0x1F6FF; i++)
            codePoints.Add(i);

        // Supplemental Symbols and Pictographs (U+1F900 – U+1F9FF)
        for (int i = 0x1F900; i <= 0x1F9FF; i++)
            codePoints.Add(i);

        // Symbols and Pictographs Extended-A (U+1FA00 – U+1FA6F)
        for (int i = 0x1FA00; i <= 0x1FA6F; i++)
            codePoints.Add(i);

        // Miscellaneous Symbols (U+2600 – U+26FF) — ☀☁⭐♥ etc.
        for (int i = 0x2600; i <= 0x26FF; i++)
            codePoints.Add(i);

        // Dingbats (U+2700 – U+27BF)
        for (int i = 0x2700; i <= 0x27BF; i++)
            codePoints.Add(i);

        return codePoints;
    }

    protected void GenerateAtlas(byte[] fontData, float fontSize, int atlasWidth = 1024, int atlasHeight = 512)
    {
        var fontInfo = new StbTrueType.stbtt_fontinfo();
        fixed (byte* ptr = fontData)
        {
            if (StbTrueType.stbtt_InitFont(fontInfo, ptr, 0) == 0)
                throw new Exception("Failed to initialize font");

            var codePoints = GetCodePointsToRender();

            _scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, fontSize);

            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

            _ascent  = ascent  * _scale;
            _descent = descent * _scale;
            _lineGap = lineGap * _scale;

            _atlasWidth  = atlasWidth;
            _atlasHeight = atlasHeight;
            byte[] atlasData = new byte[_atlasWidth * _atlasHeight];

            int penX = 0;
            int penY = 0;
            int rowHeight = 0;

            foreach (int codePoint in codePoints)
            {
                // Skip codepoints absent in this font (glyph index 0 = .notdef).
                int glyphIdx = StbTrueType.stbtt_FindGlyphIndex(fontInfo, codePoint);
                if (codePoint >= 0x00A0 && glyphIdx == 0)
                    continue;

                int advance, lsb;
                StbTrueType.stbtt_GetCodepointHMetrics(fontInfo, codePoint, &advance, &lsb);

                int x0, y0, x1, y1;
                StbTrueType.stbtt_GetCodepointBitmapBox(fontInfo, codePoint, _scale, _scale, &x0, &y0, &x1, &y1);

                int glyphWidth  = x1 - x0;
                int glyphHeight = y1 - y0;

                // Whitespace: store metrics but no bitmap.
                if (glyphWidth <= 0 || glyphHeight <= 0)
                {
                    _glyphMap[codePoint] = new CharInfo
                    {
                        AdvanceX = advance * _scale,
                    };
                    continue;
                }

                // Advance to next row if glyph does not fit in current row.
                if (penX + glyphWidth + 1 >= _atlasWidth)
                {
                    penX = 0;
                    penY += rowHeight + 1;
                    rowHeight = 0;
                }

                if (penY + glyphHeight >= _atlasHeight)
                {
                    // Atlas is full — remaining glyphs will be missing (rendered as blank).
                    Console.WriteLine($"[OpenGLFontAtlas] Atlas full at codepoint U+{codePoint:X4}, remaining glyphs skipped.");
                    break;
                }

                fixed (byte* atlasPtr = atlasData)
                {
                    byte* dest = atlasPtr + penY * _atlasWidth + penX;
                    StbTrueType.stbtt_MakeCodepointBitmap(fontInfo, dest, glyphWidth, glyphHeight,
                        _atlasWidth, _scale, _scale, codePoint);
                }

                _glyphMap[codePoint] = new CharInfo
                {
                    X0      = (float)penX              / _atlasWidth,
                    Y0      = (float)penY              / _atlasHeight,
                    X1      = (float)(penX + glyphWidth)  / _atlasWidth,
                    Y1      = (float)(penY + glyphHeight) / _atlasHeight,
                    OffsetX = x0,
                    OffsetY = -y0,
                    AdvanceX = advance * _scale,
                    Width   = glyphWidth,
                    Height  = glyphHeight,
                };

                penX += glyphWidth + 1;
                rowHeight = Math.Max(rowHeight, glyphHeight);
            }

            // Upload atlas to GPU as a single-channel (red) texture.
            _textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _textureId);

            fixed (byte* atlasPtr = atlasData)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.R8, (uint)_atlasWidth,
                    (uint)_atlasHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, atlasPtr);
            }

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int)TextureWrapMode.ClampToEdge);

            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }
    }

    /// <summary>
    /// Looks up glyph info by Unicode codepoint.
    /// </summary>
    public bool TryGetGlyph(int codePoint, out CharInfo info)
        => _glyphMap.TryGetValue(codePoint, out info);

    /// <summary>
    /// Convenience overload for BMP characters (keeps existing call-sites compiling).
    /// </summary>
    public bool TryGetChar(char c, out CharInfo info)
        => _glyphMap.TryGetValue((int)c, out info);

    /// <summary>
    /// Returns true if this atlas contains a glyph for the given codepoint.
    /// </summary>
    public bool HasGlyph(int codePoint)
        => _glyphMap.ContainsKey(codePoint);

    /// <summary>
    /// Returns true if this atlas contains a glyph for the given BMP character.
    /// </summary>
    public bool HasCharacter(char c)
        => _glyphMap.ContainsKey((int)c);

    /// <summary>
    /// Number of glyphs baked into this atlas.
    /// </summary>
    public int GetCharacterCount()
        => _glyphMap.Count;

    // ── Measurement helpers ────────────────────────────────────────────────

    /// <summary>
    /// Measures the rendered size of <paramref name="text"/> at atlas scale.
    /// </summary>
    public Vector2 MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        float width      = 0;
        float maxHeight  = LineHeight;
        float maxAscent  = _ascent;
        float maxDescent = Math.Abs(_descent);

        CharInfo? lastInfo    = null;
        float     lastAdvance = 0;

        for (int i = 0; i < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            if (_glyphMap.TryGetValue(codePoint, out var info))
            {
                if (lastInfo.HasValue)
                    width += lastAdvance;

                lastInfo    = info;
                lastAdvance = info.AdvanceX;

                float glyphTop    = maxAscent - info.OffsetY;
                float glyphBottom = glyphTop + info.Height;
                maxHeight = Math.Max(maxHeight, glyphBottom + maxDescent);
            }
            else if (codePoint == 0x0020) // space
            {
                width += _fontSize * 0.25f;
            }
        }

        if (lastInfo.HasValue)
            width += lastInfo.Value.OffsetX + lastInfo.Value.Width;

        return new Vector2(width, maxHeight);
    }

    /// <summary>
    /// Measures only the advance width of <paramref name="text"/> at atlas scale.
    /// </summary>
    public float MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        float penX = 0;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            if (_glyphMap.TryGetValue(codePoint, out var info))
            {
                float x0 = penX + info.OffsetX;
                float x1 = x0 + info.Width;
                minX = Math.Min(minX, x0);
                maxX = Math.Max(maxX, x1);
                penX += info.AdvanceX;
            }
            else if (codePoint == 0x0020)
            {
                if (minX == float.MaxValue) minX = penX;
                penX += _fontSize * 0.25f;
                maxX = Math.Max(maxX, penX);
            }
        }

        return minX == float.MaxValue ? 0 : Math.Max(0, maxX - minX);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_textureId != 0)
        {
            _gl?.DeleteTexture(_textureId);
            _textureId = 0;
        }

        _glyphMap.Clear();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~OpenGLFontAtlas()
    {
        if (!_isDisposed && _textureId != 0)
            System.Diagnostics.Debug.WriteLine($"OpenGLFontAtlas '{Name}' was not disposed correctly");
    }
}
