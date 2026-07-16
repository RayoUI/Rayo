using Rayo.Controls;

namespace Rayo.Tests;

public sealed class TextBoxSelectionTests
{
    [Fact]
    public void Entry_uses_progressive_double_click_selection()
    {
        var entry = new Entry();

        Assert.Equal(TextSelectionUnit.WordThenLine, entry.DoubleTapSelectionUnit);
    }

    [Fact]
    public void Double_tap_line_mode_selects_the_complete_line()
    {
        var editor = new TestEditor
        {
            Text = "alpha\nbeta gamma\nomega",
            DoubleTapSelectionUnit = TextSelectionUnit.Line
        };

        editor.SelectDoubleTapUnit(9);

        Assert.Equal("beta gamma", editor.GetSelectedText());
    }

    [Fact]
    public void Double_tap_line_mode_excludes_carriage_return()
    {
        var editor = new TestEditor
        {
            Text = "alpha\r\nbeta gamma\r\nomega",
            DoubleTapSelectionUnit = TextSelectionUnit.Line
        };

        editor.SelectDoubleTapUnit(10);

        Assert.Equal("beta gamma", editor.GetSelectedText());
    }

    [Fact]
    public void Double_tap_word_mode_remains_the_default()
    {
        var editor = new TestEditor { Text = "alpha beta gamma" };

        editor.SelectDoubleTapUnit(8);

        Assert.Equal("beta", editor.GetSelectedText());
    }

    [Fact]
    public void Progressive_double_tap_selects_word_then_complete_line()
    {
        var editor = new TestEditor
        {
            Text = "alpha beta gamma\nomega",
            DoubleTapSelectionUnit = TextSelectionUnit.WordThenLine
        };

        editor.SelectDoubleTapUnit(8);
        Assert.Equal("beta", editor.GetSelectedText());

        editor.SelectDoubleTapUnit(8);
        Assert.Equal("alpha beta gamma", editor.GetSelectedText());
    }

    [Fact]
    public void Double_tap_selects_the_adjacent_horizontal_whitespace()
    {
        var editor = new TestEditor { Text = "alpha \t  beta\nomega" };

        editor.SelectDoubleTapUnit(7);

        Assert.Equal(" \t  ", editor.GetSelectedText());
    }

    [Fact]
    public void Progressive_double_tap_selects_whitespace_then_complete_line()
    {
        var editor = new TestEditor
        {
            Text = "alpha \t  beta\nomega",
            DoubleTapSelectionUnit = TextSelectionUnit.WordThenLine
        };

        editor.SelectDoubleTapUnit(7);
        Assert.Equal(" \t  ", editor.GetSelectedText());

        editor.SelectDoubleTapUnit(7);
        Assert.Equal("alpha \t  beta", editor.GetSelectedText());
    }

    [Fact]
    public void Multiline_selection_includes_the_line_break_after_intermediate_lines()
    {
        var editor = new TestEditor { Text = "alpha\nbeta\ngamma" };

        Assert.True(editor.IncludesLineBreak(lineEnd: 5, selectionEnd: 9));
    }

    [Fact]
    public void Selection_ending_at_the_line_edge_does_not_include_the_line_break()
    {
        var editor = new TestEditor { Text = "alpha\nbeta" };

        Assert.False(editor.IncludesLineBreak(lineEnd: 5, selectionEnd: 5));
    }

    private sealed class TestEditor : Editor
    {
        public void SelectDoubleTapUnit(int position) => SelectDoubleTapUnitAt(position);

        public bool IncludesLineBreak(int lineEnd, int selectionEnd) =>
            SelectionIncludesLineBreakAfter(lineEnd, selectionEnd);
    }
}
