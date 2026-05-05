namespace Rayo.Controls;

using System;
using System.Collections.Generic;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Multi-line text input control (MAUI-compatible Editor).
/// This is a specialized version of TextBox locked to multi-line mode.
/// Implements IScrollable for mouse wheel scrolling support.
/// Supports optional word wrapping.
/// </summary>
public class Editor : TextBox<Editor>, IScrollable
{
    // Line height factor (same as used in TextBox rendering)
    private const float LineHeightFactor = 1.2f;

    public Editor()
    {
        // Lock to multi-line mode
        base.IsMultiline = true;

        // Default size for editor (larger than Entry)
        if (Height <= 0)
        {
            Height = 100;
        }
    }

    public Editor(string text) : this()
    {
        Text = text;
    }

    // MAUI-compatible event name (maps to internal completion logic)
    public Editor OnCompletedHandler(System.Action handler)
    {
        // In MAUI Editor, Completed fires when focus is lost
        // For now, we can map it to a custom event or leave as no-op
        // since multiline doesn't typically have "enter to submit" behavior
        return this;
    }

    // Fluent API for MaxLength (base property) - hide inherited version
    public Editor SetMaxLength(int maxLength)
    {
        MaxLength = maxLength;
        return this;
    }

    // Fluent API for IsReadOnly (base property) - hide inherited version
    //public new Editor IsReadOnly(bool isReadOnly)
    //{
    //    IsReadOnly = isReadOnly;
    //    return this;
    //}

    /// <summary>
    /// Auto-size height based on content (MAUI property).
    /// </summary>
    #region AutoSize
    [LayoutProperty]
    public bool AutoSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = false;
    #endregion

    // =========================================================================
    // WORD WRAP
    // =========================================================================

    #region WordWrap
    /// <summary>
    /// When true, text wraps at word boundaries to fit within the editor width.
    /// </summary>
    [LayoutProperty]
    public bool WordWrap
    {
        get => field;
        set => this.SetProperty(ref field, value, InvalidateWrappedLines);
    } = false;
    #endregion

    // Wrapped line cache: each entry is (startIndex, length) into the Text string
    private List<(int start, int length)> _wrappedLines = new();
    private bool _wrappedLinesDirty = true;
    private string _lastWrappedText = string.Empty;
    private float _lastWrappedWidth = -1;

    private void InvalidateWrappedLines()
    {
        _wrappedLinesDirty = true;
        MarkNeedsLayout();
    }

    private void EnsureWrappedLines()
    {
        float availableWidth = GetTextAreaWidth();

        // Check if cache is still valid
        if (!_wrappedLinesDirty &&
            ReferenceEquals(_lastWrappedText, Text) &&
            Math.Abs(_lastWrappedWidth - availableWidth) < 0.5f)
        {
            return;
        }

        BuildWrappedLines(availableWidth);
        _wrappedLinesDirty = false;
        _lastWrappedText = Text;
        _lastWrappedWidth = availableWidth;
    }

    private float GetTextAreaWidth()
    {
        float width = ComputedWidth - Padding.Horizontal - BorderWidth * 2;
        if (ShowScrollbar)
        {
            width -= ScrollbarWidth + 4;
        }
        return Math.Max(1, width);
    }

    private void BuildWrappedLines(float availableWidth)
    {
        _wrappedLines.Clear();

        if (string.IsNullOrEmpty(Text))
        {
            _wrappedLines.Add((0, 0));
            return;
        }

        int segmentStart = 0;

        for (int i = 0; i <= Text.Length; i++)
        {
            if (i == Text.Length || Text[i] == '\n')
            {
                WrapSegment(segmentStart, i - segmentStart, availableWidth);
                segmentStart = i + 1; // skip '\n'
            }
        }
    }

    private void WrapSegment(int start, int length, float availableWidth)
    {
        if (length == 0)
        {
            _wrappedLines.Add((start, 0));
            return;
        }

        int pos = start;
        int remaining = length;

        while (remaining > 0)
        {
            // Find how many characters fit
            int fitCount = FindFitCount(pos, remaining, availableWidth);

            if (fitCount >= remaining)
            {
                // Everything fits
                _wrappedLines.Add((pos, remaining));
                break;
            }

            if (fitCount <= 0)
            {
                // At least one character must be placed
                fitCount = 1;
            }

            // Try to break at a word boundary (last space within fitting portion)
            int breakAt = fitCount;
            int lastSpace = -1;
            for (int i = pos; i < pos + fitCount; i++)
            {
                if (Text[i] == ' ')
                {
                    lastSpace = i - pos + 1; // break after the space
                }
            }

            if (lastSpace > 0)
            {
                breakAt = lastSpace;
            }

            _wrappedLines.Add((pos, breakAt));
            pos += breakAt;
            remaining -= breakAt;
        }
    }

    private int FindFitCount(int start, int length, float availableWidth)
    {
        // Binary search for the max number of chars that fit
        int lo = 0;
        int hi = length;

        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            float w = MeasureTextWidth(Text.Substring(start, mid));
            if (w <= availableWidth)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    // =========================================================================
    // SCROLLBAR PROPERTIES
    // =========================================================================

    #region ScrollbarBackground
    /// <summary>
    /// Background brush of the scrollbar track.
    /// </summary>
    [PaintProperty]
    public Brush ScrollbarBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(40, 40, 40);
    #endregion

    #region ScrollbarThumb
    /// <summary>
    /// Brush of the scrollbar thumb.
    /// </summary>
    [PaintProperty]
    public Brush ScrollbarThumb
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region ScrollbarWidth
    /// <summary>
    /// Width of the scrollbar in pixels.
    /// </summary>
    [LayoutProperty]
    public float ScrollbarWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 8;
    #endregion

    #region ShowScrollbar
    /// <summary>
    /// Whether to show the vertical scrollbar.
    /// </summary>
    [PaintProperty]
    public bool ShowScrollbar
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    /// <summary>
    /// Sets the scrollbar colors.
    /// </summary>
    public Editor ScrollbarColors(Brush background, Brush thumb)
    {
        ScrollbarBackground = background;
        ScrollbarThumb = thumb;
        return this;
    }

    // =========================================================================
    // IScrollable IMPLEMENTATION
    // =========================================================================

    // Cache for the maximum line width (avoids re-measuring every frame)
    private float _cachedMaxLineWidth = -1;
    private string _lastMaxLineText = string.Empty;

    /// <summary>
    /// Gets the total width of the content (longest line, measured in pixels).
    /// Returns viewport width when WordWrap is on (no horizontal scroll needed).
    /// </summary>
    public float ContentWidth
    {
        get
        {
            if (WordWrap)
                return ComputedWidth - Padding.Horizontal - BorderWidth * 2;

            // Re-measure when text changes
            if (!ReferenceEquals(Text, _lastMaxLineText))
            {
                _cachedMaxLineWidth = -1;
                _lastMaxLineText = Text;
            }

            if (_cachedMaxLineWidth >= 0)
                return _cachedMaxLineWidth;

            if (_cachedRenderer == null || string.IsNullOrEmpty(Text))
            {
                _cachedMaxLineWidth = 0;
                return _cachedMaxLineWidth;
            }

            float max = 0;
            int lineStart = 0;
            for (int i = 0; i <= Text.Length; i++)
            {
                if (i == Text.Length || Text[i] == '\n')
                {
                    if (i > lineStart)
                    {
                        float w = _cachedRenderer.MeasureText(Text.Substring(lineStart, i - lineStart), FontSize).X;
                        if (w > max) max = w;
                    }
                    lineStart = i + 1;
                }
            }

            _cachedMaxLineWidth = max + 20; // 20px right padding so cursor isn't clipped at end
            return _cachedMaxLineWidth;
        }
    }

    /// <summary>
    /// Gets the total height of the content based on line count.
    /// </summary>
    public float ContentHeight
    {
        get
        {
            int lineCount = GetLineCount();
            float lineHeight = FontSize * LineHeightFactor;
            return lineCount * lineHeight;
        }
    }

    /// <summary>
    /// Scrolls the content horizontally (used by mouse wheel with Shift, or native horizontal wheel).
    /// No-op when WordWrap is on.
    /// </summary>
    public void ScrollHorizontal(float deltaX)
    {
        if (WordWrap) return;

        float viewportWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2 - (ShowScrollbar ? ScrollbarWidth + 4 : 0);
        float maxScroll = Math.Max(0, ContentWidth - viewportWidth);

        float newOffset = _scrollOffsetX + deltaX;
        newOffset = Math.Clamp(newOffset, 0, maxScroll);

        if (Math.Abs(_scrollOffsetX - newOffset) > 0.001f)
        {
            _scrollOffsetX = newOffset;
            MarkNeedsPaint();
        }
    }

    /// <summary>
    /// Scrolls the content vertically.
    /// </summary>
    /// <param name="deltaY">Amount to scroll (positive = down, negative = up)</param>
    public void Scroll(float deltaY)
    {
        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2;
        float maxScroll = Math.Max(0, ContentHeight - viewportHeight);

        float newOffset = _scrollOffsetY + deltaY;
        newOffset = Math.Clamp(newOffset, 0, maxScroll);

        if (Math.Abs(_scrollOffsetY - newOffset) > 0.001f)
        {
            _scrollOffsetY = newOffset;
            MarkNeedsPaint();
        }
    }

    /// <summary>
    /// Gets the current vertical scroll offset.
    /// </summary>
    public float VerticalScrollOffset => _scrollOffsetY;

    /// <summary>
    /// Sets the vertical scroll offset directly.
    /// </summary>
    public Editor SetVerticalScrollOffset(float offset)
    {
        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2;
        float maxScroll = Math.Max(0, ContentHeight - viewportHeight);
        _scrollOffsetY = Math.Clamp(offset, 0, maxScroll);
        MarkNeedsPaint();
        return this;
    }

    /// <summary>
    /// Scrolls to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        _scrollOffsetY = 0;
        MarkNeedsPaint();
    }

    /// <summary>
    /// Scrolls to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2;
        float maxScroll = Math.Max(0, ContentHeight - viewportHeight);
        _scrollOffsetY = maxScroll;
        MarkNeedsPaint();
    }

    /// <summary>
    /// Ensures the cursor is visible by scrolling if necessary.
    /// Called after cursor movement operations.
    /// </summary>
    public void EnsureCursorVisible()
    {
        float lineHeight = FontSize * LineHeightFactor;
        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2
                               - (NeedsHorizontalScrollbar ? ScrollbarWidth + 2 : 0);
        float viewportWidth  = ComputedWidth  - Padding.Horizontal - BorderWidth * 2
                               - (ShowScrollbar ? ScrollbarWidth + 4 : 0);

        bool dirty = false;

        // Vertical: find which line the cursor is on
        int cursorLine = GetCursorLineIndex();
        float cursorY = cursorLine * lineHeight;
        float cursorBottom = cursorY + lineHeight;

        if (cursorY < _scrollOffsetY)
        {
            _scrollOffsetY = cursorY;
            dirty = true;
        }
        else if (cursorBottom > _scrollOffsetY + viewportHeight)
        {
            _scrollOffsetY = cursorBottom - viewportHeight;
            dirty = true;
        }

        // Horizontal (only when no word wrap)
        if (!WordWrap && _cachedRenderer != null)
        {
            int safeCursorPos = Math.Clamp(_cursorPosition, 0, Text.Length);
            int lineStart = 0;
            for (int i = 0; i < safeCursorPos; i++)
            {
                if (Text[i] == '\n') lineStart = i + 1;
            }
            string textBeforeCursorInLine = Text.Substring(lineStart, safeCursorPos - lineStart);
            float cursorX = _cachedRenderer.MeasureText(textBeforeCursorInLine, FontSize).X;

            const float margin = 10;
            if (cursorX - _scrollOffsetX > viewportWidth - margin)
            {
                _scrollOffsetX = cursorX - viewportWidth + margin;
                dirty = true;
            }
            else if (cursorX - _scrollOffsetX < margin)
            {
                _scrollOffsetX = Math.Max(0, cursorX - margin);
                dirty = true;
            }
        }

        if (dirty) MarkNeedsPaint();
    }

    private bool NeedsHorizontalScrollbar =>
        ShowScrollbar && !WordWrap && ContentWidth > ComputedWidth - Padding.Horizontal - BorderWidth * 2 - (ShowScrollbar ? ScrollbarWidth + 4 : 0);

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Gets the number of lines in the text (or wrapped lines when WordWrap is on).
    /// </summary>
    private int GetLineCount()
    {
        if (WordWrap)
        {
            EnsureWrappedLines();
            return Math.Max(1, _wrappedLines.Count);
        }

        if (string.IsNullOrEmpty(Text))
            return 1;

        int count = 1;
        foreach (char c in Text)
        {
            if (c == '\n')
                count++;
        }
        return count;
    }

    /// <summary>
    /// Gets the visual line index where the cursor is located.
    /// </summary>
    private int GetCursorLineIndex()
    {
        if (WordWrap)
        {
            EnsureWrappedLines();
            for (int i = 0; i < _wrappedLines.Count; i++)
            {
                var (start, length) = _wrappedLines[i];
                if (_cursorPosition >= start && _cursorPosition <= start + length)
                {
                    // If cursor is exactly at the start+length and there's a next line starting there,
                    // prefer the next line (cursor is at the beginning of the next wrapped line)
                    if (_cursorPosition == start + length && i + 1 < _wrappedLines.Count &&
                        _wrappedLines[i + 1].start == _cursorPosition)
                    {
                        continue;
                    }
                    return i;
                }
            }
            return Math.Max(0, _wrappedLines.Count - 1);
        }

        if (string.IsNullOrEmpty(Text))
            return 0;

        int lineIndex = 0;
        for (int i = 0; i < _cursorPosition && i < Text.Length; i++)
        {
            if (Text[i] == '\n')
                lineIndex++;
        }

        return lineIndex;
    }

    // =========================================================================
    // RENDERING
    // =========================================================================

    /// <summary>
    /// Renders the editor content and scrollbar.
    /// </summary>
    public override void Render(Rendering.IRenderer renderer)
    {
        if (!WordWrap)
        {
            // Original behavior: TextBox handles rendering, we add scrollbar
            base.Render(renderer);
            RenderScrollbar(renderer);
            return;
        }

        // Word wrap rendering
        _cachedRenderer = renderer;
        EnsureWrappedLines();

        // Reset horizontal scroll when word wrapping
        _scrollOffsetX = 0;

        // Safety bounds check
        if (_cursorPosition > Text.Length) _cursorPosition = Text.Length;
        if (_cursorPosition < 0) _cursorPosition = 0;
        if (_selectionStart > Text.Length) _selectionStart = Text.Length;
        if (_selectionEnd > Text.Length) _selectionEnd = Text.Length;

        var bgColor = IsFocused ? FocusBackground : Background;
        var borderBrush = IsFocused ? FocusBorderColor : BorderColor;

        // Draw background
        renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius.TopLeft, bgColor);

        // Draw border
        if (BorderWidth > 0)
        {
            renderer.DrawRoundedRect(
                ComputedX + BorderWidth,
                ComputedY + BorderWidth,
                ComputedWidth - BorderWidth * 2,
                ComputedHeight - BorderWidth * 2,
                BorderRadius.TopLeft,
                bgColor
            );
            renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius.TopLeft, borderBrush);
            renderer.DrawRoundedRect(
                ComputedX + BorderWidth,
                ComputedY + BorderWidth,
                ComputedWidth - BorderWidth * 2,
                ComputedHeight - BorderWidth * 2,
                BorderRadius.TopLeft,
                bgColor
            );
        }

        // Content area
        float contentX = ComputedX + Padding.Left + BorderWidth;
        float contentY = ComputedY + Padding.Top + BorderWidth;
        float contentWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2;
        float contentHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2;
        float lineHeight = FontSize * LineHeightFactor;

        renderer.PushScissor(contentX, contentY, contentWidth, contentHeight);

        try
        {
            // Draw selection
            if (HasSelection && IsFocused)
            {
                int selStart = Math.Min(_selectionStart, _selectionEnd);
                int selEnd = Math.Max(_selectionStart, _selectionEnd);

                for (int i = 0; i < _wrappedLines.Count; i++)
                {
                    var (start, length) = _wrappedLines[i];
                    int lineEnd = start + length;

                    // Skip if selection doesn't intersect this line
                    if (selEnd <= start || selStart >= lineEnd)
                        continue;

                    float lineY = contentY + (i * lineHeight) - _scrollOffsetY;

                    // Cull invisible lines
                    if (lineY + lineHeight < contentY || lineY > contentY + contentHeight)
                        continue;

                    // Intersect selection with this line
                    int lineSelStart = Math.Max(selStart, start);
                    int lineSelEnd = Math.Min(selEnd, lineEnd);

                    if (lineSelEnd <= lineSelStart) continue;

                    // Measure text before selection in this line
                    string textBefore = Text.Substring(start, lineSelStart - start);
                    var sizeBefore = renderer.MeasureText(textBefore, FontSize);

                    // Measure selected text in this line
                    string selectedText = Text.Substring(lineSelStart, lineSelEnd - lineSelStart);
                    var selSize = renderer.MeasureText(selectedText, FontSize);

                    renderer.DrawRect(
                        contentX + sizeBefore.X,
                        lineY,
                        selSize.X,
                        lineHeight,
                        SelectionBackground
                    );
                }
            }

            // Draw text or placeholder
            bool showPlaceholder = string.IsNullOrEmpty(Text);
            Brush textColor = showPlaceholder ? PlaceholderColor : TextColor;

            if (showPlaceholder)
            {
                if (!string.IsNullOrEmpty(Placeholder))
                {
                    renderer.DrawText(Placeholder, contentX, contentY, textColor, FontSize);
                }
            }
            else
            {
                for (int i = 0; i < _wrappedLines.Count; i++)
                {
                    var (start, length) = _wrappedLines[i];
                    float lineY = contentY + (i * lineHeight) - _scrollOffsetY;

                    // Cull invisible lines
                    if (lineY + lineHeight < contentY || lineY > contentY + contentHeight)
                        continue;

                    if (length > 0)
                    {
                        string lineText = Text.Substring(start, length).Replace("\t", "    ");
                        renderer.DrawText(lineText, contentX, lineY, textColor, FontSize);
                    }
                }
            }

            // Draw cursor
            if (IsFocused && IsCursorBlinkVisible())
            {
                int cursorLine = GetCursorLineIndex();
                var (cStart, cLength) = _wrappedLines[cursorLine];
                int cursorOffsetInLine = _cursorPosition - cStart;

                string textBeforeCursor = Text.Substring(cStart, cursorOffsetInLine);
                var cursorSize = renderer.MeasureText(textBeforeCursor, FontSize);

                float cursorX = contentX + cursorSize.X;
                float cursorY = contentY + (cursorLine * lineHeight) - _scrollOffsetY;

                renderer.DrawRect(cursorX, cursorY, 2, lineHeight, TextColor);
            }
        }
        finally
        {
            renderer.PopScissor();
        }

        // Render scrollbar
        RenderScrollbar(renderer);
    }

    /// <summary>
    /// Check cursor blink visibility using the base class blink state.
    /// </summary>
    private bool IsCursorBlinkVisible()
    {
        return _cursorVisible;
    }

    // =========================================================================
    // CURSOR MOVEMENT OVERRIDES (Word Wrap)
    // =========================================================================

    public override void MoveCursorUp(bool shiftPressed = false)
    {
        if (!WordWrap)
        {
            base.MoveCursorUp(shiftPressed);
            return;
        }

        EnsureWrappedLines();
        int currentLine = GetCursorLineIndex();

        if (currentLine == 0)
        {
            // Already on first line, move to start
            if (shiftPressed)
            {
                if (!HasSelection) _selectionStart = _cursorPosition;
                _cursorPosition = 0;
                _selectionEnd = _cursorPosition;
            }
            else
            {
                _cursorPosition = 0;
                ClearSelection();
            }
            ResetCursorBlink();
            EnsureCursorVisible();
            MarkNeedsPaint();
            return;
        }

        // Get cursor offset within current line
        var (curStart, curLength) = _wrappedLines[currentLine];
        int charOffset = _cursorPosition - curStart;

        // Move to previous line at same character offset
        int prevLine = currentLine - 1;
        var (prevStart, prevLength) = _wrappedLines[prevLine];
        int newOffset = Math.Min(charOffset, prevLength);
        int newPos = prevStart + newOffset;

        if (shiftPressed)
        {
            if (!HasSelection) _selectionStart = _cursorPosition;
            _cursorPosition = newPos;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = newPos;
            ClearSelection();
        }

        ResetCursorBlink();
        EnsureCursorVisible();
        MarkNeedsPaint();
    }

    public override void MoveCursorDown(bool shiftPressed = false)
    {
        if (!WordWrap)
        {
            base.MoveCursorDown(shiftPressed);
            return;
        }

        EnsureWrappedLines();
        int currentLine = GetCursorLineIndex();

        if (currentLine >= _wrappedLines.Count - 1)
        {
            // Already on last line, move to end
            if (shiftPressed)
            {
                if (!HasSelection) _selectionStart = _cursorPosition;
                _cursorPosition = Text.Length;
                _selectionEnd = _cursorPosition;
            }
            else
            {
                _cursorPosition = Text.Length;
                ClearSelection();
            }
            ResetCursorBlink();
            EnsureCursorVisible();
            MarkNeedsPaint();
            return;
        }

        // Get cursor offset within current line
        var (curStart, curLength) = _wrappedLines[currentLine];
        int charOffset = _cursorPosition - curStart;

        // Move to next line at same character offset
        int nextLine = currentLine + 1;
        var (nextStart, nextLength) = _wrappedLines[nextLine];
        int newOffset = Math.Min(charOffset, nextLength);
        int newPos = nextStart + newOffset;

        if (shiftPressed)
        {
            if (!HasSelection) _selectionStart = _cursorPosition;
            _cursorPosition = newPos;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = newPos;
            ClearSelection();
        }

        ResetCursorBlink();
        EnsureCursorVisible();
        MarkNeedsPaint();
    }

    protected override int GetCursorPositionFromMouse(float mouseX, float mouseY)
    {
        if (!WordWrap)
        {
            return base.GetCursorPositionFromMouse(mouseX, mouseY);
        }

        if (string.IsNullOrEmpty(Text))
            return 0;

        EnsureWrappedLines();

        float contentX = ComputedX + Padding.Left + BorderWidth;
        float contentY = ComputedY + Padding.Top + BorderWidth;
        float lineHeight = FontSize * LineHeightFactor;

        // Determine which visual line was clicked
        float localY = mouseY - contentY + _scrollOffsetY;
        int clickedLine = Math.Max(0, (int)(localY / lineHeight));
        clickedLine = Math.Min(clickedLine, _wrappedLines.Count - 1);

        var (start, length) = _wrappedLines[clickedLine];
        string lineText = length > 0 ? Text.Substring(start, length) : string.Empty;

        float localX = mouseX - contentX;

        if (localX <= 0)
            return start;

        float lineWidth = MeasureTextWidth(lineText);
        if (localX >= lineWidth)
            return start + length;

        // Binary search for character position
        for (int i = 0; i <= length; i++)
        {
            float widthUpTo = MeasureTextWidth(lineText.Substring(0, i));

            if (i == length)
                return start + i;

            float widthUpToNext = MeasureTextWidth(lineText.Substring(0, i + 1));

            if (localX >= widthUpTo && localX < widthUpToNext)
            {
                float midpoint = (widthUpTo + widthUpToNext) / 2;
                return start + (localX < midpoint ? i : i + 1);
            }
        }

        return start + length;
    }

    // =========================================================================
    // SCROLLBAR RENDERING
    // =========================================================================

    /// <summary>
    /// Renders the vertical and (when needed) horizontal scrollbar.
    /// </summary>
    private void RenderScrollbar(Rendering.IRenderer renderer)
    {
        if (!ShowScrollbar) return;

        bool showHoriz = NeedsHorizontalScrollbar;
        float horizBarHeight = showHoriz ? ScrollbarWidth + 2 : 0;

        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2 - horizBarHeight;
        float contentHeight = ContentHeight;

        // Vertical scrollbar
        if (contentHeight > viewportHeight)
        {
            float scrollbarX = ComputedX + ComputedWidth - ScrollbarWidth - BorderWidth - 2;
            float scrollbarY = ComputedY + Padding.Top + BorderWidth;
            float scrollbarHeight = viewportHeight;

            renderer.DrawRoundedRect(scrollbarX, scrollbarY, ScrollbarWidth, scrollbarHeight,
                ScrollbarWidth / 2, ScrollbarBackground.PrimaryColor);

            float viewportRatio = Math.Min(1.0f, viewportHeight / contentHeight);
            float thumbHeight = Math.Max(20, scrollbarHeight * viewportRatio);
            float maxScroll = Math.Max(0, contentHeight - viewportHeight);
            float scrollRatio = maxScroll > 0 ? Math.Clamp(_scrollOffsetY / maxScroll, 0, 1) : 0;
            float thumbY = scrollbarY + (scrollbarHeight - thumbHeight) * scrollRatio;

            renderer.DrawRoundedRect(scrollbarX, thumbY, ScrollbarWidth, thumbHeight,
                ScrollbarWidth / 2, ScrollbarThumb.PrimaryColor);
        }

        // Horizontal scrollbar
        if (showHoriz)
        {
            float viewportWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2 - ScrollbarWidth - 4;
            float contentWidth  = ContentWidth;

            float scrollbarY = ComputedY + ComputedHeight - BorderWidth - ScrollbarWidth - 2;
            float scrollbarX = ComputedX + Padding.Left + BorderWidth;
            float scrollbarWidth = viewportWidth;

            renderer.DrawRoundedRect(scrollbarX, scrollbarY, scrollbarWidth, ScrollbarWidth,
                ScrollbarWidth / 2, ScrollbarBackground.PrimaryColor);

            float viewportRatio = Math.Min(1.0f, viewportWidth / contentWidth);
            float thumbWidth = Math.Max(20, scrollbarWidth * viewportRatio);
            float maxScroll = Math.Max(0, contentWidth - viewportWidth);
            float scrollRatio = maxScroll > 0 ? Math.Clamp(_scrollOffsetX / maxScroll, 0, 1) : 0;
            float thumbX = scrollbarX + (scrollbarWidth - thumbWidth) * scrollRatio;

            renderer.DrawRoundedRect(thumbX, scrollbarY, thumbWidth, ScrollbarWidth,
                ScrollbarWidth / 2, ScrollbarThumb.PrimaryColor);
        }
    }

    // =========================================================================
    // SCROLLBAR DRAG STATE
    // =========================================================================

    // Vertical scrollbar drag state
    private bool _isDraggingThumb = false;
    private float _thumbDragStartOffset = 0;
    private float _thumbDragStartMouseY = 0;

    // Horizontal scrollbar drag state
    private bool _isDraggingHorizThumb = false;
    private float _horizThumbDragStartOffset = 0;
    private float _horizThumbDragStartMouseX = 0;

    private bool IsPointOnScrollbarThumb(System.Numerics.Vector2 position)
    {
        if (!ShowScrollbar) return false;

        bool showHoriz = NeedsHorizontalScrollbar;
        float horizBarHeight = showHoriz ? ScrollbarWidth + 2 : 0;
        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2 - horizBarHeight;
        float contentHeight = ContentHeight;

        if (contentHeight > viewportHeight)
        {
            float scrollbarX = ComputedX + ComputedWidth - ScrollbarWidth - BorderWidth - 2;
            float scrollbarY = ComputedY + Padding.Top + BorderWidth;
            float scrollbarHeight = viewportHeight;

            float viewportRatio = Math.Min(1.0f, viewportHeight / contentHeight);
            float thumbHeight = Math.Max(20, scrollbarHeight * viewportRatio);
            float maxScroll = Math.Max(0, contentHeight - viewportHeight);
            float scrollRatio = maxScroll > 0 ? Math.Clamp(_scrollOffsetY / maxScroll, 0, 1) : 0;
            float thumbY = scrollbarY + (scrollbarHeight - thumbHeight) * scrollRatio;

            if (position.X >= scrollbarX && position.X <= scrollbarX + ScrollbarWidth &&
                position.Y >= thumbY && position.Y <= thumbY + thumbHeight)
                return true;
        }

        return false;
    }

    private bool IsPointOnHorizScrollbarThumb(System.Numerics.Vector2 position)
    {
        if (!NeedsHorizontalScrollbar) return false;

        float viewportWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2 - ScrollbarWidth - 4;
        float contentWidth  = ContentWidth;
        float scrollbarY    = ComputedY + ComputedHeight - BorderWidth - ScrollbarWidth - 2;
        float scrollbarX    = ComputedX + Padding.Left + BorderWidth;
        float scrollbarWidth = viewportWidth;

        float viewportRatio = Math.Min(1.0f, viewportWidth / contentWidth);
        float thumbWidth = Math.Max(20, scrollbarWidth * viewportRatio);
        float maxScroll = Math.Max(0, contentWidth - viewportWidth);
        float scrollRatio = maxScroll > 0 ? Math.Clamp(_scrollOffsetX / maxScroll, 0, 1) : 0;
        float thumbX = scrollbarX + (scrollbarWidth - thumbWidth) * scrollRatio;

        return position.X >= thumbX && position.X <= thumbX + thumbWidth &&
               position.Y >= scrollbarY && position.Y <= scrollbarY + ScrollbarWidth;
    }

    private bool IsPointOnScrollbarTrack(System.Numerics.Vector2 position)
    {
        if (!ShowScrollbar) return false;

        bool showHoriz = NeedsHorizontalScrollbar;
        float horizBarHeight = showHoriz ? ScrollbarWidth + 2 : 0;
        float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2 - horizBarHeight;
        float contentHeight = ContentHeight;

        // Vertical track
        if (contentHeight > viewportHeight)
        {
            float scrollbarX = ComputedX + ComputedWidth - ScrollbarWidth - BorderWidth - 2;
            float scrollbarY = ComputedY + Padding.Top + BorderWidth;
            float scrollbarHeight = viewportHeight;

            if (position.X >= scrollbarX && position.X <= scrollbarX + ScrollbarWidth &&
                position.Y >= scrollbarY && position.Y <= scrollbarY + scrollbarHeight)
                return true;
        }

        // Horizontal track
        if (showHoriz)
        {
            float scrollbarY = ComputedY + ComputedHeight - BorderWidth - ScrollbarWidth - 2;
            float scrollbarX = ComputedX + Padding.Left + BorderWidth;
            float scrollbarWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2 - ScrollbarWidth - 4;

            if (position.X >= scrollbarX && position.X <= scrollbarX + scrollbarWidth &&
                position.Y >= scrollbarY && position.Y <= scrollbarY + ScrollbarWidth)
                return true;
        }

        return false;
    }

    // =========================================================================
    // INPUT HANDLING OVERRIDES
    // =========================================================================

    // Override CanHandleInput - Editor can always handle input (scrollbar, navigation)
    public override bool CanHandleInput => true;

    // Override HandleInput to handle scrollbar drag and enforce IsReadOnly for text editing
    public override bool HandleInput(InputEventArgs args)
    {
        // Handle scrollbar thumb dragging first (highest priority)
        switch (args.EventType)
        {
            case InputEventType.MouseDown:
                if (IsPointOnScrollbarThumb(args.Position))
                {
                    _isDraggingThumb = true;
                    _thumbDragStartOffset = _scrollOffsetY;
                    _thumbDragStartMouseY = args.Position.Y;
                    return true;
                }
                if (IsPointOnHorizScrollbarThumb(args.Position))
                {
                    _isDraggingHorizThumb = true;
                    _horizThumbDragStartOffset = _scrollOffsetX;
                    _horizThumbDragStartMouseX = args.Position.X;
                    return true;
                }
                if (IsPointOnScrollbarTrack(args.Position))
                    return true;
                break;

            case InputEventType.MouseDrag:
                if (_isDraggingThumb)
                {
                    float horizBarHeight = NeedsHorizontalScrollbar ? ScrollbarWidth + 2 : 0;
                    float viewportHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2 - horizBarHeight;
                    float contentHeight = ContentHeight;
                    float thumbHeight = Math.Max(20, viewportHeight * Math.Min(1.0f, viewportHeight / contentHeight));
                    float availableTravel = viewportHeight - thumbHeight;

                    if (availableTravel > 0)
                    {
                        float maxScroll = Math.Max(0, contentHeight - viewportHeight);
                        float scrollDelta = ((args.Position.Y - _thumbDragStartMouseY) / availableTravel) * maxScroll;
                        _scrollOffsetY = Math.Clamp(_thumbDragStartOffset + scrollDelta, 0, maxScroll);
                        MarkNeedsPaint();
                    }
                    return true;
                }
                if (_isDraggingHorizThumb)
                {
                    float viewportWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2 - (ShowScrollbar ? ScrollbarWidth + 4 : 0);
                    float contentWidth  = ContentWidth;
                    float thumbWidth = Math.Max(20, viewportWidth * Math.Min(1.0f, viewportWidth / contentWidth));
                    float availableTravel = viewportWidth - thumbWidth;

                    if (availableTravel > 0)
                    {
                        float maxScroll = Math.Max(0, contentWidth - viewportWidth);
                        float scrollDelta = ((args.Position.X - _horizThumbDragStartMouseX) / availableTravel) * maxScroll;
                        _scrollOffsetX = Math.Clamp(_horizThumbDragStartOffset + scrollDelta, 0, maxScroll);
                        MarkNeedsPaint();
                    }
                    return true;
                }
                break;

            case InputEventType.MouseUp:
                if (_isDraggingThumb)  { _isDraggingThumb = false;       return true; }
                if (_isDraggingHorizThumb) { _isDraggingHorizThumb = false; return true; }
                break;
        }

        // In read-only mode, allow navigation but not editing
        if (IsReadOnly)
        {
            // Allow mouse events for focus and selection (but not if on scrollbar)
            if (args.EventType == InputEventType.MouseDown ||
                args.EventType == InputEventType.MouseDrag ||
                args.EventType == InputEventType.MouseUp)
            {
                bool handled = base.HandleInput(args);
                if (handled) EnsureCursorVisible();
                return handled;
            }

            // Allow arrow keys and selection keys
            if (args.EventType == InputEventType.KeyDown || args.EventType == InputEventType.KeyRepeat)
            {
                if (args.KeyCode.HasValue)
                {
                    var key = args.KeyCode.Value;
                    // Allow navigation and selection including Up/Down for multiline
                    if (key == InputKey.Left || key == InputKey.Right ||
                        key == InputKey.Up || key == InputKey.Down ||
                        key == InputKey.Home || key == InputKey.End ||
                        key == InputKey.PageUp || key == InputKey.PageDown ||
                        (args.IsControlPressed && key == InputKey.A) ||
                        (args.IsControlPressed && key == InputKey.C))
                    {
                        bool handled = base.HandleInput(args);
                        if (handled) EnsureCursorVisible();
                        return handled;
                    }
                }
            }

            return false; // Block all other input
        }

        // Normal mode: handle input and ensure cursor is visible
        bool result = base.HandleInput(args);

        // After keyboard navigation, text input, or mouse drag selection, ensure cursor is visible
        if (result && (args.EventType == InputEventType.KeyDown ||
                       args.EventType == InputEventType.KeyRepeat ||
                       args.EventType == InputEventType.TextInput ||
                       args.EventType == InputEventType.MouseDrag))
        {
            EnsureCursorVisible();
        }

        // Invalidate wrapped lines after text editing
        if (result && WordWrap &&
            (args.EventType == InputEventType.TextInput ||
             args.EventType == InputEventType.KeyDown ||
             args.EventType == InputEventType.KeyRepeat))
        {
            _wrappedLinesDirty = true;
        }

        return result;
    }
}
