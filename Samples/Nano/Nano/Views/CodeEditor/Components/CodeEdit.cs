using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

namespace Nano.Views.CodeEditor.Components;

/// <summary>
/// Text editor specialized for source code. It reuses <see cref="Editor"/>'s
/// input, selection and scrolling behavior while drawing a syntax layer and
/// line-number gutter within the same viewport.
/// </summary>
public sealed class CodeEdit : Editor
{
    private const int TabSize = 4;
    private const string SoftTab = "    ";
    private const float GutterWidth = 42f;
    private const float ContentPadding = 10f;
    private const int TokenCacheLimit = 2048;
    private readonly ICodeLanguage _language;
    private readonly Dictionary<int, CodeToken[]> _tokenCache = [];
    private readonly Queue<int> _tokenCacheOrder = [];
    private string[] _lines = [string.Empty];
    private string _trackedText = string.Empty;
    private SnippetSession? _snippetSession;
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

    public CodeEdit(string text, ICodeLanguage language)
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
        _trackedText = Text;
        TextChanged += OnEditorTextChanged;
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

    protected override bool ShouldRenderTextContent => false;

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

    public override bool HandleInput(InputEventArgs args)
    {
        if (!IsReadOnly && args.EventType == InputEventType.TextInput && args.Character is { } character)
        {
            if (character is '\r' or '\n')
            {
                InsertSmartNewLine();
                return true;
            }

            if (TryInsertSmartCharacter(character))
                return true;
        }

        if (!IsReadOnly && args.EventType is InputEventType.KeyDown or InputEventType.KeyRepeat)
        {
            switch (args.KeyCode)
            {
                case InputKey.Return:
                    InsertSmartNewLine();
                    return true;
                case InputKey.Tab:
                    if (HandleSnippetTab(args.IsShiftPressed))
                        return true;
                    ChangeIndentation(args.IsShiftPressed);
                    return true;
            }
        }

        return base.HandleInput(args);
    }

    public override void DeleteChar()
    {
        if (!IsReadOnly && !HasSelection && _cursorPosition > 0 && _cursorPosition < Text.Length &&
            IsPair(Text[_cursorPosition - 1], Text[_cursorPosition]))
        {
            var start = _cursorPosition - 1;
            CommitEdit(Text.Remove(start, 2), start);
            return;
        }

        if (!DeleteTabsAsUnit || IsReadOnly || HasSelection || _cursorPosition < TabSize)
        {
            base.DeleteChar();
            return;
        }

        int tabStart = _cursorPosition - TabSize;
        int lineStart = Text.LastIndexOf('\n', _cursorPosition - 1) + 1;
        if (!Text.AsSpan(tabStart, TabSize).SequenceEqual(SoftTab.AsSpan()) ||
            !Text.AsSpan(lineStart, _cursorPosition - lineStart).Trim().IsEmpty)
        {
            base.DeleteChar();
            return;
        }

        CommitEdit(Text.Remove(tabStart, TabSize), tabStart);
    }

    private bool TryInsertSmartCharacter(char character)
    {
        if (character is not ('(' or '[' or '{' or ')' or ']' or '}' or '\'' or '"'))
            return false;

        if (character is ')' or ']' or '}' or '\'' or '"')
        {
            if (character is ')' or ']' or '}' && !HasSelection)
                OutdentClosingCharacter();

            if (!HasSelection && _cursorPosition < Text.Length && Text[_cursorPosition] == character &&
                !IsEscaped(_cursorPosition))
            {
                MoveCaret(_cursorPosition + 1);
                return true;
            }

        }

        if (!TryGetClosingCharacter(character, out var closing) ||
            (character is '\'' or '"' && !ShouldAutoCloseQuote()))
        {
            InsertPlainText(character.ToString());
            return true;
        }

        if (HasSelection)
        {
            var start = Math.Min(_selectionStart, _selectionEnd);
            var end = Math.Max(_selectionStart, _selectionEnd);
            var selected = Text[start..end];
            var replacement = $"{character}{selected}{closing}";
            if (MaxLength > 0 && Text.Length - selected.Length + replacement.Length > MaxLength)
                return true;
            var updated = Text.Remove(start, end - start).Insert(start, replacement);
            CommitEdit(updated, start + replacement.Length, start + 1, end + 1);
            return true;
        }

        if (MaxLength > 0 && Text.Length + 2 > MaxLength)
        {
            InsertPlainText(character.ToString());
            return true;
        }
        var paired = Text.Insert(_cursorPosition, $"{character}{closing}");
        CommitEdit(paired, _cursorPosition + 1);
        return true;
    }

    private void InsertSmartNewLine()
    {
        if (IsReadOnly)
            return;

        var start = HasSelection ? Math.Min(_selectionStart, _selectionEnd) : _cursorPosition;
        var end = HasSelection ? Math.Max(_selectionStart, _selectionEnd) : _cursorPosition;
        var source = HasSelection ? Text.Remove(start, end - start) : Text;
        var lineStart = source.LastIndexOf('\n', Math.Max(0, start - 1)) + 1;
        var linePrefix = source[lineStart..start];
        var indentationLength = 0;
        while (indentationLength < linePrefix.Length && linePrefix[indentationLength] is ' ' or '\t')
            indentationLength++;
        var indentation = linePrefix[..indentationLength];
        var trimmedPrefix = linePrefix.TrimEnd();
        var indentUnit = indentation.Contains('\t') ? "\t" : SoftTab;
        var extraIndent = OpensBlock(trimmedPrefix) ? indentUnit : string.Empty;

        var betweenPair = start > 0 && start < source.Length && IsPair(source[start - 1], source[start]);
        var insertion = betweenPair
            ? $"\n{indentation}{indentUnit}\n{indentation}"
            : $"\n{indentation}{extraIndent}";
        var caret = start + 1 + indentation.Length + (betweenPair ? indentUnit.Length : extraIndent.Length);
        CommitEdit(source.Insert(start, insertion), caret);
    }

    private void ChangeIndentation(bool outdent)
    {
        if (HasSelection)
        {
            ChangeSelectedLinesIndentation(outdent);
            return;
        }

        var lineStart = Text.LastIndexOf('\n', Math.Max(0, _cursorPosition - 1)) + 1;
        if (outdent)
        {
            var remove = IndentationToRemove(Text, lineStart);
            if (remove > 0)
                CommitEdit(Text.Remove(lineStart, remove), Math.Max(lineStart, _cursorPosition - remove));
            return;
        }

        var column = _cursorPosition - lineStart;
        var spaces = TabSize - column % TabSize;
        InsertPlainText(new string(' ', spaces));
    }

    private void ChangeSelectedLinesIndentation(bool outdent)
    {
        var selectionStart = Math.Min(_selectionStart, _selectionEnd);
        var selectionEnd = Math.Max(_selectionStart, _selectionEnd);
        var blockStart = Text.LastIndexOf('\n', Math.Max(0, selectionStart - 1)) + 1;
        if (selectionEnd > blockStart && selectionEnd <= Text.Length && Text[selectionEnd - 1] == '\n')
            selectionEnd--;
        var blockEnd = Text.IndexOf('\n', selectionEnd);
        if (blockEnd < 0)
            blockEnd = Text.Length;

        var lineStarts = new List<int> { blockStart };
        for (var index = blockStart; index < blockEnd; index++)
        {
            if (Text[index] == '\n' && index + 1 <= blockEnd)
                lineStarts.Add(index + 1);
        }

        var updated = Text;
        var delta = 0;
        for (var index = lineStarts.Count - 1; index >= 0; index--)
        {
            var position = lineStarts[index];
            if (outdent)
            {
                var remove = IndentationToRemove(updated, position);
                if (remove == 0)
                    continue;
                updated = updated.Remove(position, remove);
                delta -= remove;
            }
            else
            {
                updated = updated.Insert(position, SoftTab);
                delta += SoftTab.Length;
            }
        }

        CommitEdit(updated, blockEnd + delta, blockStart, blockEnd + delta);
    }

    private void OutdentClosingCharacter()
    {
        var lineStart = Text.LastIndexOf('\n', Math.Max(0, _cursorPosition - 1)) + 1;
        if (!Text.AsSpan(lineStart, _cursorPosition - lineStart).Trim().IsEmpty)
            return;
        var remove = IndentationToRemove(Text, lineStart);
        if (remove > 0)
            CommitEdit(Text.Remove(lineStart, remove), _cursorPosition - remove);
    }

    private void InsertPlainText(string value)
    {
        var start = HasSelection ? Math.Min(_selectionStart, _selectionEnd) : _cursorPosition;
        var end = HasSelection ? Math.Max(_selectionStart, _selectionEnd) : _cursorPosition;
        var available = MaxLength > 0 ? Math.Max(0, MaxLength - (Text.Length - (end - start))) : int.MaxValue;
        if (value.Length > available)
            value = value[..available];
        if (value.Length == 0)
            return;
        CommitEdit(Text.Remove(start, end - start).Insert(start, value), start + value.Length);
    }

    private void CommitEdit(string text, int caret, int? selectionStart = null, int? selectionEnd = null)
    {
        AssignTextPreservingCursor(text);
        _cursorPosition = Math.Clamp(caret, 0, Text.Length);
        if (selectionStart.HasValue && selectionEnd.HasValue)
        {
            _selectionStart = Math.Clamp(selectionStart.Value, 0, Text.Length);
            _selectionEnd = Math.Clamp(selectionEnd.Value, 0, Text.Length);
        }
        else
        {
            ClearSelection();
        }
        ResetCursorBlink();
        EnsureCursorVisible();
        MarkNeedsPaint();
    }

    private void MoveCaret(int position)
    {
        _cursorPosition = Math.Clamp(position, 0, Text.Length);
        ClearSelection();
        ResetCursorBlink();
        EnsureCursorVisible();
        MarkNeedsPaint();
    }

    private bool ShouldAutoCloseQuote()
    {
        if (IsEscaped(_cursorPosition))
            return false;
        if (_cursorPosition >= Text.Length)
            return true;
        return char.IsWhiteSpace(Text[_cursorPosition]) || Text[_cursorPosition] is ')' or ']' or '}' or ',' or ';';
    }

    private bool IsEscaped(int position)
    {
        var slashes = 0;
        for (var index = position - 1; index >= 0 && Text[index] == '\\'; index--)
            slashes++;
        return slashes % 2 != 0;
    }

    private static bool OpensBlock(string prefix)
    {
        if (prefix.EndsWith('{') || prefix.EndsWith('[') || prefix.EndsWith('('))
            return true;
        var lastWordStart = prefix.Length;
        while (lastWordStart > 0 && (char.IsLetterOrDigit(prefix[lastWordStart - 1]) || prefix[lastWordStart - 1] == '_'))
            lastWordStart--;
        var lastWord = prefix[lastWordStart..];
        return lastWord is "then" or "do" or "repeat" or "function" ||
            prefix.StartsWith("function ", StringComparison.Ordinal) ||
            prefix.StartsWith("local function ", StringComparison.Ordinal) ||
            prefix.Contains("= function", StringComparison.Ordinal);
    }

    private static int IndentationToRemove(string text, int lineStart)
    {
        if (lineStart >= text.Length)
            return 0;
        if (text[lineStart] == '\t')
            return 1;
        var spaces = 0;
        while (spaces < TabSize && lineStart + spaces < text.Length && text[lineStart + spaces] == ' ')
            spaces++;
        return spaces;
    }

    private static bool TryGetClosingCharacter(char character, out char closing)
    {
        closing = character switch
        {
            '(' => ')',
            '[' => ']',
            '{' => '}',
            '\'' => '\'',
            '"' => '"',
            _ => '\0'
        };
        return closing != '\0';
    }

    private static bool IsPair(char opening, char closing) =>
        opening switch
        {
            '(' => closing == ')',
            '[' => closing == ']',
            '{' => closing == '}',
            '\'' => closing == '\'',
            '"' => closing == '"',
            _ => false
        };

    private void InvalidateCodeCache()
    {
        _codeCacheDirty = true;
        ClearTokenCache();
    }

    private void OnEditorTextChanged(string text)
    {
        UpdateSnippetSession(_trackedText, text);
        _trackedText = text;
        InvalidateCodeCache();
    }

    private bool HandleSnippetTab(bool backwards)
    {
        if (_snippetSession is not null)
        {
            var current = _snippetSession.Current;
            if (_cursorPosition >= current.Start && _cursorPosition <= current.End)
            {
                if (backwards)
                {
                    if (_snippetSession.MovePrevious())
                        SelectSnippetPlaceholder(_snippetSession.Current);
                    return true;
                }

                if (_snippetSession.MoveNext())
                {
                    SelectSnippetPlaceholder(_snippetSession.Current);
                }
                else
                {
                    var completionCaret = _snippetSession.FinalCaret;
                    _snippetSession = null;
                    MoveCaret(completionCaret);
                }
                return true;
            }

            _snippetSession = null;
        }

        if (backwards || HasSelection || _language is not ICodeSnippetProvider provider)
            return false;

        var lineStart = Text.LastIndexOf('\n', Math.Max(0, _cursorPosition - 1)) + 1;
        var linePrefix = Text[lineStart.._cursorPosition];
        var indentationLength = 0;
        while (indentationLength < linePrefix.Length && linePrefix[indentationLength] is ' ' or '\t')
            indentationLength++;
        var triggerText = linePrefix[indentationLength..];
        CodeSnippet? snippet = null;
        foreach (var candidate in provider.Snippets)
        {
            if (string.Equals(candidate.Trigger, triggerText, StringComparison.Ordinal) &&
                (snippet is null || candidate.Trigger.Length > snippet.Value.Trigger.Length))
                snippet = candidate;
        }
        if (snippet is null)
            return false;

        var indentation = linePrefix[..indentationLength];
        var indentUnit = indentation.Contains('\t') ? "\t" : SoftTab;
        var expansion = ExpandSnippet(snippet.Value.Template, indentation, indentUnit);
        var triggerStart = lineStart + indentationLength;
        var updated = Text.Remove(triggerStart, _cursorPosition - triggerStart).Insert(triggerStart, expansion.Text);
        var placeholders = expansion.Placeholders
            .Select(placeholder => new SnippetPlaceholder(
                placeholder.Number,
                triggerStart + placeholder.Start,
                triggerStart + placeholder.End))
            .OrderBy(placeholder => placeholder.Number)
            .ToList();
        var finalCaret = triggerStart + expansion.FinalCaret;

        if (placeholders.Count == 0)
        {
            CommitEdit(updated, finalCaret);
            return true;
        }

        var first = placeholders[0];
        CommitEdit(updated, first.End, first.Start, first.End);
        _snippetSession = new SnippetSession(placeholders, finalCaret);
        return true;
    }

    private void SelectSnippetPlaceholder(SnippetPlaceholder placeholder)
    {
        _selectionStart = placeholder.Start;
        _selectionEnd = placeholder.End;
        _cursorPosition = placeholder.End;
        ResetCursorBlink();
        EnsureCursorVisible();
        MarkNeedsPaint();
    }

    private void UpdateSnippetSession(string previousText, string currentText)
    {
        if (_snippetSession is null || string.Equals(previousText, currentText, StringComparison.Ordinal))
            return;

        var prefix = 0;
        var sharedLength = Math.Min(previousText.Length, currentText.Length);
        while (prefix < sharedLength && previousText[prefix] == currentText[prefix])
            prefix++;

        var suffix = 0;
        while (suffix < previousText.Length - prefix && suffix < currentText.Length - prefix &&
               previousText[previousText.Length - suffix - 1] == currentText[currentText.Length - suffix - 1])
            suffix++;

        var oldEnd = previousText.Length - suffix;
        var newEnd = currentText.Length - suffix;
        var delta = newEnd - oldEnd;
        var active = _snippetSession.Current;
        if (prefix < active.Start || oldEnd > active.End)
        {
            _snippetSession = null;
            return;
        }

        active.End += delta;
        for (var index = _snippetSession.CurrentIndex + 1; index < _snippetSession.Placeholders.Count; index++)
        {
            _snippetSession.Placeholders[index].Start += delta;
            _snippetSession.Placeholders[index].End += delta;
        }
        _snippetSession.FinalCaret += delta;
    }

    private static SnippetExpansion ExpandSnippet(string template, string indentation, string indentUnit)
    {
        var text = new System.Text.StringBuilder(template.Length + indentation.Length * 4);
        var placeholders = new List<SnippetPlaceholder>();
        var finalCaret = -1;

        for (var index = 0; index < template.Length;)
        {
            if (template[index] == '\n')
            {
                text.Append('\n').Append(indentation);
                index++;
                continue;
            }
            if (template[index] == '\t')
            {
                text.Append(indentUnit);
                index++;
                continue;
            }
            if (template[index] == '$' && index + 2 < template.Length && template[index + 1] == '{')
            {
                var close = template.IndexOf('}', index + 2);
                if (close > index)
                {
                    var descriptor = template[(index + 2)..close];
                    var separator = descriptor.IndexOf(':');
                    var numberText = separator < 0 ? descriptor : descriptor[..separator];
                    if (int.TryParse(numberText, out var number))
                    {
                        var defaultText = separator < 0 ? string.Empty : descriptor[(separator + 1)..];
                        var start = text.Length;
                        text.Append(defaultText);
                        if (number == 0)
                            finalCaret = start;
                        else
                            placeholders.Add(new SnippetPlaceholder(number, start, text.Length));
                        index = close + 1;
                        continue;
                    }
                }
            }

            text.Append(template[index++]);
        }

        if (finalCaret < 0)
            finalCaret = text.Length;
        return new SnippetExpansion(text.ToString(), placeholders, finalCaret);
    }

    private sealed class SnippetSession(List<SnippetPlaceholder> placeholders, int finalCaret)
    {
        public List<SnippetPlaceholder> Placeholders { get; } = placeholders;
        public int CurrentIndex { get; private set; }
        public int FinalCaret { get; set; } = finalCaret;
        public SnippetPlaceholder Current => Placeholders[CurrentIndex];

        public bool MoveNext()
        {
            if (CurrentIndex + 1 >= Placeholders.Count)
                return false;
            CurrentIndex++;
            return true;
        }

        public bool MovePrevious()
        {
            if (CurrentIndex == 0)
                return false;
            CurrentIndex--;
            return true;
        }
    }

    private sealed class SnippetPlaceholder(int number, int start, int end)
    {
        public int Number { get; } = number;
        public int Start { get; set; } = start;
        public int End { get; set; } = end;
    }

    private readonly record struct SnippetExpansion(
        string Text,
        List<SnippetPlaceholder> Placeholders,
        int FinalCaret);

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
        ClearTokenCache();
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
            while (_tokenCacheOrder.Count > 0)
            {
                if (_tokenCache.Remove(_tokenCacheOrder.Dequeue()))
                    break;
            }
        }

        tokens = _language.Tokenize(_lines[lineIndex]).ToArray();
        _tokenCache[lineIndex] = tokens;
        _tokenCacheOrder.Enqueue(lineIndex);
        return tokens;
    }

    private void ClearTokenCache()
    {
        _tokenCache.Clear();
        _tokenCacheOrder.Clear();
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
