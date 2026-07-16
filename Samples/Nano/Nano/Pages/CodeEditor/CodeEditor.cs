using Rayo;
using Rayo.Controls;
using Rayo.Core.Platform;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

namespace Nano.Pages.CodeEditor;

/// <summary>
/// Text editor specialized for source code. It reuses <see cref="Editor"/>'s
/// input, selection and scrolling behavior while drawing a syntax layer and
/// line-number gutter within the same viewport.
/// </summary>
public sealed class CodeEditor : Editor
{
    private const float GutterWidth = 42f;
    private const float ContentPadding = 10f;
    private const int TokenCacheLimit = 2048;
    private readonly ICodeLanguage _language;
    private readonly Dictionary<int, CodeToken[]> _tokenCache = [];
    private string[] _lines = [string.Empty];
    private bool _codeCacheDirty = true;

    /// <summary>
    /// When enabled, Backspace removes a complete soft tab as one unit.
    /// </summary>
    public bool DeleteTabsAsUnit { get; set; } = true;

    /// <summary>
    /// Controls whether the line containing the caret is highlighted.
    /// </summary>
    public bool HighlightCurrentLine { get; set; } = true;

    public static IReadOnlyList<VirtualKeyboardAccessoryKey> ProgrammingAccessoryKeys { get; } =
    [
        new("Tab", "    "),
        new("{", "{"), new("}", "}"),
        new("(", "("), new(")", ")"),
        new("[", "["), new("]", "]"),
        new("<", "<"), new(">", ">"),
        new("=", "="), new("+", "+"), new("-", "-"),
        new("*", "*"), new("/", "/"), new("%", "%"),
        new(".", "."), new(",", ","), new(":", ":"), new(";", ";"),
        new("_", "_"), new("\"", "\""), new("'", "'"),
        new("#", "#"), new("$", "$"), new("@", "@"),
        new("\\", "\\"), new("|", "|"), new("&", "&"),
        new("!", "!"), new("?", "?")
    ];

    public CodeEditor(string text, ICodeLanguage language)
    {
        _language = language;
        Text = text;
        WordWrap = false;
        FontSize = 14;
        Padding = new Thickness(GutterWidth + ContentPadding, ContentPadding, ContentPadding, ContentPadding);
        Background = new Color(30, 36, 48);
        FocusBackground = new Color(30, 36, 48);
        TextColor = new Color(220, 223, 228);
        BorderThickness = 0;
        DoubleTapSelectionUnit = TextSelectionUnit.WordThenLine;
        KeyboardAccessoryKeys = ProgrammingAccessoryKeys;
        TextChanged += _ => InvalidateCodeCache();
        InvalidateCodeCache();
    }

    public override void Render(IRenderer renderer)
    {
        base.Render(renderer);

        var lineHeight = FontSize * 1.2f;
        var contentX = ComputedX + Padding.Left + BorderThickness.Left;
        var contentY = ComputedY + Padding.Top + BorderThickness.Top;
        var contentWidth = ComputedWidth - Padding.Horizontal - BorderThickness.Horizontal;
        var contentHeight = ComputedHeight - Padding.Vertical - BorderThickness.Vertical;
        EnsureLineCache();
        var firstLine = Math.Max(0, (int)MathF.Floor(VerticalScrollOffset / lineHeight));
        var lastLine = Math.Min(_lines.Length - 1, (int)MathF.Ceiling((VerticalScrollOffset + contentHeight) / lineHeight));

        renderer.PushScissor(contentX, contentY, contentWidth, contentHeight);
        for (var lineIndex = firstLine; lineIndex <= lastLine; lineIndex++)
        {
            var y = contentY + lineIndex * lineHeight - VerticalScrollOffset;
            var x = contentX - HorizontalScrollOffset;
            foreach (var token in GetTokens(lineIndex))
            {
                string displayText = token.Text.Contains('\t') ? token.Text.Replace("\t", "    ") : token.Text;
                renderer.DrawText(displayText, x, y, TokenColor(token.Kind), FontSize);
                x += MeasureTextWidth(displayText);
            }
        }
        renderer.PopScissor();

        RenderLineNumbers(renderer, lineHeight, firstLine, lastLine, contentY);
        RenderTouchSelectionHandles(renderer);
    }

    protected override Brush GetTextRenderBrush() => Color.Transparent;

    protected override float[] BuildPrefixWidths(
        string sourceText,
        int start,
        int length,
        bool passwordMode)
    {
        if (passwordMode || length == 0)
            return base.BuildPrefixWidths(sourceText, start, length, passwordMode);

        string sourceLine = sourceText.Substring(start, length);
        string tokenizedLine = sourceLine.EndsWith('\r') ? sourceLine[..^1] : sourceLine;
        var tokens = _language.Tokenize(tokenizedLine).ToArray();

        // A language implementation must preserve the complete source text. If
        // it does not, retain the normal text metrics instead of producing an
        // incomplete position table.
        if (!string.Equals(string.Concat(tokens.Select(token => token.Text)), tokenizedLine, StringComparison.Ordinal))
            return base.BuildPrefixWidths(sourceText, start, length, passwordMode);

        var prefixWidths = new float[length + 1];
        int sourceOffset = 0;
        float tokenRunOffset = 0;

        foreach (var token in tokens)
        {
            for (int charOffset = 1; charOffset <= token.Text.Length; charOffset++)
            {
                string tokenPrefix = token.Text[..charOffset].Replace("\t", "    ");
                prefixWidths[sourceOffset + charOffset] = tokenRunOffset + MeasureTextWidth(tokenPrefix);
            }

            string displayText = token.Text.Replace("\t", "    ");
            tokenRunOffset += MeasureTextWidth(displayText);
            sourceOffset += token.Text.Length;
        }

        // CR in a CRLF sequence has no visible advance in the syntax layer.
        while (sourceOffset < length)
        {
            prefixWidths[++sourceOffset] = tokenRunOffset;
        }

        return prefixWidths;
    }

    protected override void RenderTextBackground(
        IRenderer renderer,
        float contentX,
        float contentY,
        float contentWidth,
        float contentHeight)
    {
        if (!HighlightCurrentLine)
            return;

        float lineHeight = FontSize * 1.2f;
        float lineY = contentY + GetCursorLineIndex() * lineHeight - VerticalScrollOffset;
        if (lineY + lineHeight < contentY || lineY > contentY + contentHeight)
            return;

        renderer.DrawRect(contentX, lineY, contentWidth, lineHeight, new Color(48, 57, 74));
    }

    public override void DeleteChar()
    {
        if (!DeleteTabsAsUnit || IsReadOnly || HasSelection || _cursorPosition < 4)
        {
            base.DeleteChar();
            return;
        }

        const int tabSize = 4;
        int tabStart = _cursorPosition - tabSize;
        if (!Text.AsSpan(tabStart, tabSize).SequenceEqual("    ".AsSpan()))
        {
            base.DeleteChar();
            return;
        }

        AssignTextPreservingCursor(Text.Remove(tabStart, tabSize));
        _cursorPosition = tabStart;
        ClearSelection();
        ResetCursorBlink();
        EnsureCursorVisible();
        MarkNeedsPaint();
    }

    private void InvalidateCodeCache()
    {
        _codeCacheDirty = true;
        _tokenCache.Clear();
    }

    private void EnsureLineCache()
    {
        if (!_codeCacheDirty)
        {
            return;
        }

        _lines = Text.Split('\n');
        for (var index = 0; index < _lines.Length; index++)
        {
            if (_lines[index].EndsWith('\r'))
            {
                _lines[index] = _lines[index][..^1];
            }
        }
        _tokenCache.Clear();
        _codeCacheDirty = false;
    }

    private CodeToken[] GetTokens(int lineIndex)
    {
        if (_tokenCache.TryGetValue(lineIndex, out var tokens))
        {
            return tokens;
        }

        if (_tokenCache.Count >= TokenCacheLimit)
        {
            _tokenCache.Clear();
        }

        tokens = _language.Tokenize(_lines[lineIndex]).ToArray();
        _tokenCache[lineIndex] = tokens;
        return tokens;
    }

    private void RenderLineNumbers(IRenderer renderer, float lineHeight, int firstLine, int lastLine, float contentY)
    {
        renderer.DrawRect(ComputedX, ComputedY, GutterWidth, ComputedHeight, new Color(37, 44, 58));
        renderer.DrawRect(ComputedX + GutterWidth - 1, ComputedY, 1, ComputedHeight, new Color(79, 91, 112));

        for (var lineIndex = firstLine; lineIndex <= lastLine; lineIndex++)
        {
            var number = (lineIndex + 1).ToString();
            var width = renderer.MeasureText(number, FontSize).X;
            var y = contentY + lineIndex * lineHeight - VerticalScrollOffset;
            renderer.DrawText(number, ComputedX + GutterWidth - width - 8, y, new Color(139, 151, 174), FontSize);
        }
    }

    private static Color TokenColor(CodeTokenKind kind) => kind switch
    {
        CodeTokenKind.Keyword => new Color(198, 120, 221),
        CodeTokenKind.String => new Color(152, 195, 121),
        CodeTokenKind.Number => new Color(209, 154, 102),
        CodeTokenKind.Comment => new Color(110, 127, 150),
        CodeTokenKind.Builtin => new Color(86, 182, 194),
        _ => new Color(220, 223, 228)
    };
}
