namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Assets;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using Rayo.Styling;
using System.Text;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Text label component with support for background, padding, and alignment.
/// Uses hybrid reactive approach: Generator for simple properties.
/// Migrated to new MAUI-like architecture: inherits from View<Label>
/// </summary>
public class Label : BorderView<Label>
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
    } = Color.Transparent;
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

    #region LineBreakMode
    /// <summary>
    /// Controls whether text wraps or is truncated when it exceeds the available width.
    /// The values match .NET MAUI's <see cref="Rayo.Core.LineBreakMode"/>.
    /// </summary>
    [LayoutProperty]
    public LineBreakMode LineBreakMode
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = LineBreakMode.NoWrap;
    #endregion

    #region CharacterSpacing
    /// <summary>
    /// Additional spacing, in device-independent units, inserted between displayed characters.
    /// </summary>
    [LayoutProperty]
    public float CharacterSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region MaxLines
    /// <summary>
    /// Maximum number of displayed lines. A value of -1 shows all lines; 0 hides the text.
    /// </summary>
    [LayoutProperty]
    public int MaxLines
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = -1;
    #endregion

    #region TextTransform
    /// <summary>
    /// Casing transformation applied when displaying <see cref="Text"/>.
    /// </summary>
    [LayoutProperty]
    public TextTransform TextTransform
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = TextTransform.Default;
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
        InitializeTheme();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        SetThemeValue(nameof(Foreground), (Brush)theme.Colors.OnSurface, value => Foreground = value);
        SetThemeValue(
            nameof(FontSize),
            theme.Typography.Body.FontSize * theme.Preferences.TextScale,
            value => FontSize = value);
        SetThemeValue(
            nameof(FontWeight),
            theme.Typography.Body.FontWeight,
            value => FontWeight = value);
        if (!string.IsNullOrWhiteSpace(theme.Typography.Body.FontFamily))
        {
            SetThemeValue(
                nameof(FontFamily),
                theme.Typography.Body.FontFamily,
                value => FontFamily = value);
        }
    }

    public Label(string text) : this()
    {
        Text = text;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        // Calculate desired size based on content
        // Support for multiline text with \n escape characters
        if (string.IsNullOrEmpty(Text))
        {
            DesiredWidth = Width > 0 ? Width : Padding.Horizontal;
            DesiredHeight = Height > 0 ? Height : Padding.Vertical;
            return;
        }

        float contentWidth = GetAvailableContentWidth(availableWidth);
        var lines = CreateLines(GetDisplayText(), contentWidth, EstimateLineWidth);
        float maxLineWidth = lines.Length == 0 ? 0 : lines.Max(EstimateLineWidth);

        // Wrapping and truncation obey the width constraint; NoWrap retains intrinsic sizing.
        DesiredWidth = Width > 0 ? Width : maxLineWidth + Padding.Horizontal;
        DesiredHeight = Height > 0 ? Height : GetTextHeight(lines.Length) + Padding.Vertical;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Draw background if not transparent
        var bgColor = Background.PrimaryColor;
        if (bgColor.A > 0)
        {
            if (HasAnyRadius(BorderRadius))
            {
                if (IsUniformRadius(BorderRadius))
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
                    var path = VectorPath.RoundedRectangle(
                        ComputedX,
                        ComputedY,
                        ComputedWidth,
                        ComputedHeight,
                        BorderRadius.TopLeft,
                        BorderRadius.TopRight,
                        BorderRadius.BottomRight,
                        BorderRadius.BottomLeft
                    );
                    renderer.DrawPath(path, bgColor);
                }
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

            float contentHeight = Math.Max(0, ComputedHeight - Padding.Vertical);
            float contentWidth  = Math.Max(0, ComputedWidth  - Padding.Horizontal);
            var lines = CreateLines(
                GetDisplayText(),
                contentWidth,
                line => MeasureLineWidth(renderer, line, activeFont));
            var lineSpacing = FontSize * LineHeight;

            // Must match the lineHeight used in Measure() so vertical alignment is consistent.
            float lineHeight      = FontSize * 1.35f;
            float totalTextHeight = lines.Length switch
            {
                0 => 0,
                1 => lineHeight,
                _ => lineHeight + (lines.Length - 1) * lineSpacing
            };

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
                var processedLine = lines[i];
                if (string.IsNullOrEmpty(processedLine))
                    continue;

                // Measure line width for alignment and decorations
                var lineSize = MeasureLine(renderer, processedLine, activeFont);

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
                    DrawText(renderer, processedLine, textX, textY, activeFont, isBold, isItalic, needsFakeBold);
                }
                else
                {
                    // No custom font — use styled draw so renderers can apply bold/italic natively
                    DrawText(renderer, processedLine, textX, textY, null, isBold, isItalic, needsFakeBold);
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

    private float GetAvailableContentWidth(float availableWidth)
    {
        if (Width > 0)
            return Math.Max(0, Width - Padding.Horizontal);

        return float.IsPositiveInfinity(availableWidth)
            ? float.PositiveInfinity
            : Math.Max(0, availableWidth - Padding.Horizontal);
    }

    private float GetTextHeight(int lineCount)
    {
        if (lineCount == 0)
            return 0;

        float lineHeight = FontSize * 1.35f;
        return lineCount == 1
            ? lineHeight
            : lineHeight + (lineCount - 1) * FontSize * LineHeight;
    }

    private string GetDisplayText() => TextTransform switch
    {
        TextTransform.Lowercase => Text.ToLower(),
        TextTransform.Uppercase => Text.ToUpper(),
        _ => Text,
    };

    private string[] CreateLines(string text, float maxWidth, Func<string, float> measure)
    {
        var mode = GetEffectiveLineBreakMode();
        var lines = new List<string>();

        foreach (var hardLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = hardLine.Replace("\t", "    ");
            if (float.IsPositiveInfinity(maxWidth) || mode == LineBreakMode.NoWrap)
            {
                lines.Add(line);
                continue;
            }

            switch (mode)
            {
                case LineBreakMode.WordWrap:
                    AddWrappedLines(lines, line, maxWidth, measure, breakAtWord: true);
                    break;
                case LineBreakMode.CharacterWrap:
                    AddWrappedLines(lines, line, maxWidth, measure, breakAtWord: false);
                    break;
                case LineBreakMode.HeadTruncation:
                    lines.Add(TruncateLine(line, maxWidth, measure, TruncationPosition.Head));
                    break;
                case LineBreakMode.MiddleTruncation:
                    lines.Add(TruncateLine(line, maxWidth, measure, TruncationPosition.Middle));
                    break;
                case LineBreakMode.TailTruncation:
                    lines.Add(TruncateLine(line, maxWidth, measure, TruncationPosition.Tail));
                    break;
                default:
                    lines.Add(line);
                    break;
            }
        }

        return ApplyMaxLines(lines, maxWidth, measure, mode);
    }

    private string[] ApplyMaxLines(List<string> lines, float maxWidth, Func<string, float> measure, LineBreakMode mode)
    {
        if (MaxLines < 0 || lines.Count <= MaxLines)
            return lines.ToArray();
        if (MaxLines == 0)
            return Array.Empty<string>();

        var visible = lines.Take(MaxLines).ToList();
        if (!float.IsPositiveInfinity(maxWidth) && mode is LineBreakMode.WordWrap or LineBreakMode.CharacterWrap)
        {
            var remainder = string.Join(" ", lines.Skip(MaxLines - 1));
            visible[^1] = TruncateLine(remainder, maxWidth, measure, TruncationPosition.Tail);
        }

        return visible.ToArray();
    }

    private LineBreakMode GetEffectiveLineBreakMode() => LineBreakMode;

    private static void AddWrappedLines(List<string> lines, string text, float maxWidth, Func<string, float> measure, bool breakAtWord)
    {
        if (text.Length == 0 || maxWidth <= 0)
        {
            lines.Add(text);
            return;
        }

        int start = 0;
        while (start < text.Length)
        {
            int end = start;
            int lastWordBreak = -1;
            while (end < text.Length && measure(text[start..(end + 1)]) <= maxWidth)
            {
                if (char.IsWhiteSpace(text[end]))
                    lastWordBreak = end;
                end++;
            }

            if (end == text.Length)
            {
                lines.Add(text[start..end].TrimEnd());
                break;
            }

            if (end == start)
            {
                // A single glyph is wider than the constraint; still make progress.
                lines.Add(text[start..++end]);
                start = end;
                continue;
            }

            if (breakAtWord && lastWordBreak > start)
            {
                lines.Add(text[start..lastWordBreak].TrimEnd());
                start = lastWordBreak + 1;
                while (start < text.Length && char.IsWhiteSpace(text[start]))
                    start++;
            }
            else
            {
                lines.Add(text[start..end]);
                start = end;
            }
        }
    }

    private string TruncateLine(string text, float maxWidth, Func<string, float> measure, TruncationPosition position)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            return string.Empty;

        if (measure(text) <= maxWidth)
            return text;

        const string ellipsis = "…";
        float ellipsisWidth = measure(ellipsis);
        if (ellipsisWidth > maxWidth)
            return string.Empty;

        float availableWidth = maxWidth - ellipsisWidth;
        return position switch
        {
            TruncationPosition.Head => TruncateHead(text, availableWidth, measure, ellipsis),
            TruncationPosition.Middle => TruncateMiddle(text, availableWidth, measure, ellipsis),
            _ => TruncateTail(text, availableWidth, measure, ellipsis),
        };
    }

    private static string TruncateTail(string text, float availableWidth, Func<string, float> measure, string ellipsis)
    {
        for (int length = text.Length - 1; length > 0; length--)
            if (measure(text[..length]) <= availableWidth)
                return text[..length] + ellipsis;
        return ellipsis;
    }

    private static string TruncateHead(string text, float availableWidth, Func<string, float> measure, string ellipsis)
    {
        for (int start = 1; start < text.Length; start++)
            if (measure(text[start..]) <= availableWidth)
                return ellipsis + text[start..];
        return ellipsis;
    }

    private static string TruncateMiddle(string text, float availableWidth, Func<string, float> measure, string ellipsis)
    {
        for (int retained = text.Length - 1; retained > 0; retained--)
        {
            int prefixLength = (retained + 1) / 2;
            int suffixLength = retained - prefixLength;
            var candidate = text[..prefixLength] + ellipsis + text[^suffixLength..];
            if (measure(candidate) <= availableWidth + measure(ellipsis))
                return candidate;
        }

        return ellipsis;
    }

    private enum TruncationPosition { Head, Tail, Middle }

    private float MeasureLineWidth(IRenderer renderer, string text, IFont? font)
    {
        var size = font != null
            ? renderer.MeasureTextWithFont(text, font, FontSize).X
            : renderer.MeasureText(text, FontSize).X;
        return size + Math.Max(0, text.EnumerateRunes().Count() - 1) * CharacterSpacing;
    }

    private System.Numerics.Vector2 MeasureLine(IRenderer renderer, string text, IFont? font)
    {
        var size = font != null
            ? renderer.MeasureTextWithFont(text, font, FontSize)
            : renderer.MeasureText(text, FontSize);
        return new System.Numerics.Vector2(
            size.X + Math.Max(0, text.EnumerateRunes().Count() - 1) * CharacterSpacing,
            size.Y);
    }

    private void DrawText(IRenderer renderer, string text, float x, float y, IFont? font, bool isBold, bool isItalic, bool needsFakeBold)
    {
        if (CharacterSpacing == 0)
        {
            if (font != null)
            {
                renderer.DrawTextWithFont(text, x, y, Foreground, font, FontSize);
                if (needsFakeBold && !font.IsBold)
                    renderer.DrawTextWithFont(text, x + 1, y, Foreground, font, FontSize);
            }
            else
            {
                renderer.DrawTextStyled(text, x, y, Foreground, FontSize, isBold, isItalic);
            }
            return;
        }

        float currentX = x;
        foreach (var rune in text.EnumerateRunes())
        {
            var glyph = rune.ToString();
            if (font != null)
            {
                renderer.DrawTextWithFont(glyph, currentX, y, Foreground, font, FontSize);
                if (needsFakeBold && !font.IsBold)
                    renderer.DrawTextWithFont(glyph, currentX + 1, y, Foreground, font, FontSize);
            }
            else
            {
                renderer.DrawTextStyled(glyph, currentX, y, Foreground, FontSize, isBold, isItalic);
            }

            currentX += MeasureLineWidth(renderer, glyph, font) + CharacterSpacing;
        }
    }

    private static bool HasAnyRadius(CornerRadius radius) =>
        radius.TopLeft > 0 || radius.TopRight > 0 ||
        radius.BottomRight > 0 || radius.BottomLeft > 0;

    private static bool IsUniformRadius(CornerRadius radius) =>
        radius.TopLeft == radius.TopRight &&
        radius.TopRight == radius.BottomRight &&
        radius.BottomRight == radius.BottomLeft;

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

        return width + Math.Max(0, line.EnumerateRunes().Count() - 1) * CharacterSpacing;
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
