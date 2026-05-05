namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Assets;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Text label component with support for background, padding, and alignment.
/// Uses hybrid reactive approach: Generator for simple properties.
/// Migrated to new MAUI-like architecture: inherits from View<Label>
/// </summary>
public class Label : Rayo.Core.View<Label>
{
    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Text
    [LayoutProperty]
    public string Text
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = string.Empty;
    #endregion

    #region Foreground
    [PaintProperty]
    public Brush Foreground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region FontSize
    [LayoutProperty]
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 14;
    #endregion

    // =========================================================================
    // FONT FAMILY (Custom font support via AssetManager)
    // =========================================================================

    private IFont? _cachedFont;
    private IFont? _cachedBoldFont;
    private IFont? _cachedItalicFont;
    private IFont? _cachedBoldItalicFont;
    private IRenderer? _lastRenderer;
    private float _cachedFontSize;

    /// <summary>
    /// Font family name (alias registered in AssetManager).
    /// When set, uses the custom font for rendering instead of the default font.
    /// Similar to MAUI's FontFamily property.
    /// </summary>
    /// <example>
    /// // In Program.cs:
    /// app.ConfigureAssets(assets => assets.AddFont("Fonts/Lineicons.ttf", "Icons"));
    ///
    /// // In UI code:
    /// new Label("\uf007").FontFamily("Icons").FontSize(32)
    /// </example>
    #region FontFamily
    [LayoutProperty]
    public string? FontFamily
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                _cachedFont = null; // Force reload
            }
        }
    }

    
    #endregion

    #region TextHorizontalAlignment
    [LayoutProperty]
    public HorizontalAlignment TextHorizontalAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = HorizontalAlignment.Left;
    #endregion

    #region TextVerticalAlignment
    [LayoutProperty]
    public VerticalAlignment TextVerticalAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = VerticalAlignment.Top;
    #endregion

    #region FontWeight
    /// <summary>
    /// Font weight. Values >= Bold (700) render as bold.
    /// When a "{FontFamily}-Bold" font is registered in AssetManager it is used;
    /// otherwise bold is simulated by drawing text twice with a 1 px x-offset.
    /// </summary>
    [PaintProperty]
    public FontWeight FontWeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = FontWeight.Normal;
    #endregion

    #region FontStyle
    /// <summary>
    /// Font style. Italic rendering requires a font variant registered as
    /// "{FontFamily}-Italic" in AssetManager. Falls back to Normal when not found.
    /// </summary>
    [PaintProperty]
    public FontStyle FontStyle
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = FontStyle.Normal;
    #endregion

    #region TextDecorations
    /// <summary>
    /// Text decorations drawn over the rendered text (underline, strikethrough, overline).
    /// Multiple flags can be combined: TextDecorations.Underline | TextDecorations.Strikethrough
    /// </summary>
    [PaintProperty]
    public TextDecorations TextDecorations
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = TextDecorations.None;
    #endregion

    #region LineHeight
    /// <summary>
    /// Line height multiplier for multiline text (relative to FontSize).
    /// Default is 1.5, matching the previous hardcoded behaviour.
    /// </summary>
    [LayoutProperty]
    public float LineHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 1.5f;
    #endregion


    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public Label()
    {
    }

    public Label(string text) : this()
    {
        Text = text;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Calculate desired size based on content
        // Support for multiline text with \n escape characters
        if (string.IsNullOrEmpty(Text))
        {
            DesiredWidth = Width > 0 ? Width : Padding.Horizontal;
            DesiredHeight = Height > 0 ? Height : Padding.Vertical;
            return;
        }

        // Split by newlines to support multiline text
        var lines = Text.Split('\n');
        var lineSpacing = FontSize * LineHeight;

        // Find the longest line for width calculation
        float maxLineWidth = 0;
        foreach (var line in lines)
        {
            // Replace tabs with 4 spaces for width calculation
            var processedLine = line.Replace("\t", "    ");
            float lineWidth = EstimateLineWidth(processedLine);
            if (lineWidth > maxLineWidth)
                maxLineWidth = lineWidth;
        }

        float estimatedWidth = maxLineWidth + Padding.Horizontal;

        // A single text line spans ascenders (≈75% of em) + descenders (≈25% of em).
        // FontSize * 1.35 gives enough headroom for letters like p, q, g, y without
        // relying on actual font metrics (which are not available at measure time).
        float lineHeight = FontSize * 1.35f;
        float textHeight = lines.Length switch
        {
            0 => 0,
            1 => lineHeight,
            _ => lineHeight + (lines.Length - 1) * lineSpacing
        };

        float estimatedHeight = textHeight + Padding.Vertical;

        // If user set explicit size, use it. Otherwise use estimated.
        DesiredWidth = Width > 0 ? Width : estimatedWidth;
        DesiredHeight = Height > 0 ? Height : estimatedHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Draw background if not transparent
        var bgColor = Background.PrimaryColor;
        if (bgColor.A > 0)
        {
            if (BorderRadius.TopLeft > 0 || BorderRadius.TopRight > 0 ||
                BorderRadius.BottomLeft > 0 || BorderRadius.BottomRight > 0)
            {
                renderer.DrawRoundedRect(
                    ComputedX,
                    ComputedY,
                    ComputedWidth,
                    ComputedHeight,
                    BorderRadius.TopLeft,
                    bgColor
                );
            }
            else
            {
                renderer.DrawRect(
                    ComputedX,
                    ComputedY,
                    ComputedWidth,
                    ComputedHeight,
                    bgColor
                );
            }
        }

        // Draw text if not empty - supports multiline with \n and tabs with \t
        if (!string.IsNullOrEmpty(Text))
        {
            bool isBold   = (int)FontWeight >= (int)FontWeight.Bold;
            bool isItalic = FontStyle == FontStyle.Italic;

            // Resolve font variant: prefer registered variant, fall back to regular + simulation
            IFont? activeFont = ResolveFont(renderer, isBold, isItalic);

            // Split text by newlines to support multiline
            var lines = Text.Split('\n');
            var lineSpacing = FontSize * LineHeight;

            // Must match the lineHeight used in Measure() so vertical alignment is consistent.
            float lineHeight      = FontSize * 1.35f;
            float totalTextHeight = lines.Length switch
            {
                0 => 0,
                1 => lineHeight,
                _ => lineHeight + (lines.Length - 1) * lineSpacing
            };

            float contentHeight = Math.Max(0, ComputedHeight - Padding.Vertical);
            float contentWidth  = Math.Max(0, ComputedWidth  - Padding.Horizontal);

            // Calculate starting Y position based on vertical alignment
            float startY = ComputedY + Padding.Top;
            switch (TextVerticalAlignment)
            {
                case VerticalAlignment.Center:
                    startY = ComputedY + Padding.Top + (contentHeight - totalTextHeight) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    startY = ComputedY + ComputedHeight - Padding.Bottom - totalTextHeight;
                    break;
                default:
                    startY = ComputedY + Padding.Top;
                    break;
            }

            bool needsFakeBold = isBold && !HasBoldFont(renderer);
            var fgColor = Foreground.PrimaryColor;

            // Render each line
            for (int i = 0; i < lines.Length; i++)
            {
                var processedLine = lines[i].Replace("\t", "    ");
                if (string.IsNullOrEmpty(processedLine))
                    continue;

                // Measure line width for alignment and decorations
                var lineSize = activeFont != null
                    ? renderer.MeasureTextWithFont(processedLine, activeFont, FontSize)
                    : renderer.MeasureText(processedLine, FontSize);

                // Calculate X position based on horizontal alignment
                float textX = ComputedX + Padding.Left;
                switch (TextHorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                        textX = ComputedX + Padding.Left + (contentWidth - lineSize.X) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        textX = ComputedX + ComputedWidth - Padding.Right - lineSize.X;
                        break;
                }

                float textY = startY + (i * lineSpacing);

                // Draw text (with optional font)
                if (activeFont != null)
                {
                    renderer.DrawTextWithFont(processedLine, textX, textY, Foreground, activeFont, FontSize);
                    // Fake bold for renderers that don't handle IsBold natively (e.g. OpenGL)
                    if (needsFakeBold && !activeFont.IsBold)
                        renderer.DrawTextWithFont(processedLine, textX + 1, textY, Foreground, activeFont, FontSize);
                }
                else
                {
                    // No custom font — use styled draw so renderers can apply bold/italic natively
                    renderer.DrawTextStyled(processedLine, textX, textY, Foreground, FontSize, isBold, isItalic);
                }

                // Text decorations (drawn as thin rectangles using Foreground colour)
                if (TextDecorations != TextDecorations.None)
                {
                    float decorThickness = Math.Max(1, FontSize * 0.07f);
                    float lineWidth = lineSize.X;

                    if ((TextDecorations & TextDecorations.Underline) != 0)
                    {
                        float underlineY = textY + FontSize + decorThickness;
                        renderer.DrawRect(textX, underlineY, lineWidth, decorThickness, fgColor);
                    }

                    if ((TextDecorations & TextDecorations.Strikethrough) != 0)
                    {
                        float strikeY = textY + FontSize * 0.55f;
                        renderer.DrawRect(textX, strikeY, lineWidth, decorThickness, fgColor);
                    }

                    if ((TextDecorations & TextDecorations.Overline) != 0)
                    {
                        float overlineY = textY - decorThickness * 2;
                        renderer.DrawRect(textX, overlineY, lineWidth, decorThickness, fgColor);
                    }
                }
            }
        }
    }

    private float EstimateLineWidth(string line)
    {
        if (string.IsNullOrEmpty(line))
            return 0;

        float width = 0;
        foreach (var ch in line)
        {
            if (ch == ' ')
            {
                width += FontSize * 0.4f;
            }
            else if (char.IsControl(ch))
            {
                continue;
            }
            else if (IsPrivateUseAreaChar(ch))
            {
                width += FontSize;
            }
            else
            {
                width += FontSize * 0.6f;
            }
        }

        return width;
    }

    private static bool IsPrivateUseAreaChar(char ch) => ch >= '\uE000' && ch <= '\uF8FF';

    /// <summary>
    /// Resolves the best available font variant for the current FontWeight and FontStyle.
    /// Lookup order for bold+italic: "{FontFamily}-BoldItalic" → "{FontFamily}-Bold" → "{FontFamily}-Italic" → "{FontFamily}" → null
    /// Convention for variant registration in AssetManager:
    ///   Bold:       "{FontFamily}-Bold"
    ///   Italic:     "{FontFamily}-Italic"
    ///   BoldItalic: "{FontFamily}-BoldItalic"
    /// </summary>
    private IFont? ResolveFont(IRenderer renderer, bool isBold, bool isItalic)
    {
        if (string.IsNullOrEmpty(FontFamily)) return null;

        bool rendererChanged = _lastRenderer != renderer;
        bool sizeChanged = Math.Abs(_cachedFontSize - FontSize) >= 0.01f;

        if (rendererChanged || sizeChanged)
        {
            _cachedFont = _cachedBoldFont = _cachedItalicFont = _cachedBoldItalicFont = null;
            _lastRenderer = renderer;
            _cachedFontSize = FontSize;
        }

        if (isBold && isItalic)
        {
            _cachedBoldItalicFont ??=
                TryLoadFont(renderer, $"{FontFamily}-BoldItalic") ??
                TryLoadFont(renderer, $"{FontFamily}-Bold") ??
                TryLoadFont(renderer, $"{FontFamily}-Italic") ??
                Styled(TryLoadFont(renderer, FontFamily!), bold: true, italic: true);
            return _cachedBoldItalicFont;
        }
        if (isBold)
        {
            _cachedBoldFont ??=
                TryLoadFont(renderer, $"{FontFamily}-Bold") ??
                Styled(TryLoadFont(renderer, FontFamily!), bold: true, italic: false);
            return _cachedBoldFont;
        }
        if (isItalic)
        {
            _cachedItalicFont ??=
                TryLoadFont(renderer, $"{FontFamily}-Italic") ??
                Styled(TryLoadFont(renderer, FontFamily!), bold: false, italic: true);
            return _cachedItalicFont;
        }

        _cachedFont ??= TryLoadFont(renderer, FontFamily!);
        return _cachedFont;
    }

    /// <summary>
    /// Wraps an IFont with synthetic bold/italic flags so the renderer can apply them.
    /// Returns null if inner is null.
    /// </summary>
    private static IFont? Styled(IFont? inner, bool bold, bool italic) =>
        inner == null ? null : new FontStyleProxy(inner, bold, italic);

    private bool HasBoldFont(IRenderer renderer) =>
        !string.IsNullOrEmpty(FontFamily) &&
        AssetManager.Instance.GetFont($"{FontFamily}-Bold") != null;

    private bool HasItalicFont(IRenderer renderer) =>
        !string.IsNullOrEmpty(FontFamily) &&
        AssetManager.Instance.GetFont($"{FontFamily}-Italic") != null;

    private IFont? TryLoadFont(IRenderer renderer, string name)
    {
        var asset = AssetManager.Instance.GetFont(name);
        if (asset?.FontData == null) return null;
        try { return renderer.LoadFont(asset.FontData, FontSize); }
        catch { return null; }
    }

    /// <summary>
    /// Wraps an existing IFont to signal that synthetic bold/italic should be applied by the renderer.
    /// </summary>
    private sealed class FontStyleProxy : IFont
    {
        private readonly IFont _inner;
        public FontStyleProxy(IFont inner, bool isBold, bool isItalic)
        {
            _inner = inner;
            IsBold   = isBold;
            IsItalic = isItalic;
        }
        public string Name    => _inner.Name;
        public float  Size    => _inner.Size;
        public bool   IsBold   { get; }
        public bool   IsItalic { get; }
        public void   Dispose() => _inner.Dispose();
    }
}
