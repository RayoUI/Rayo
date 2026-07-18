using Rayo.Controls;
using Rayo.Core;

namespace Rayo.Tests;

public sealed class TextBoxSelectionTests
{
    [Fact]
    public void Selection_context_menu_is_enabled_by_default_and_can_be_customized()
    {
        var entry = new Entry();
        Func<TextSelectionContextMenuContext, VisualElement?> template = _ => null;

        entry.SelectionContextMenuTemplate = template;

        Assert.True(entry.SelectionContextMenuEnabled);
        Assert.Same(template, entry.SelectionContextMenuTemplate);

        entry.SelectionContextMenuEnabled = false;
        Assert.False(entry.SelectionContextMenuEnabled);
    }

    [Fact]
    public void Read_only_selection_context_disables_cut_and_paste()
    {
        var entry = new TestEntry { IsReadOnly = true };

        var context = entry.SelectionMenuContext();

        Assert.True(context.CanCopy);
        Assert.False(context.CanCut);
        Assert.False(context.CanPaste);
    }

    [Fact]
    public void Selection_popup_can_restore_anchor_focus()
    {
        var popup = new AnchoredPopup(new Entry(), new Frame())
        {
            RestoreAnchorFocusOnInteraction = true
        };

        Assert.True(popup.RestoreAnchorFocusOnInteraction);
    }

    [Fact]
    public void Compact_single_line_selection_handle_can_extend_below_the_control()
    {
        var entry = new TestEntry
        {
            Text = "alpha beta",
            Width = 160,
            Height = 32
        };
        var tree = new UITree();
        tree.SetRoot(entry);
        tree.Update(160, 32);

        var handle = entry.SelectionHandleAt(5);
        float handleBottom = handle.Y + entry.HandleStemHeight + entry.HandleRadius;

        Assert.True(handleBottom > entry.ComputedY + entry.ComputedHeight);
    }

    [Fact]
    public void Entry_uses_progressive_double_click_selection()
    {
        var entry = new Entry();

        Assert.Equal(TextSelectionUnit.WordThenLine, entry.DoubleTapSelectionUnit);
    }

    [Fact]
    public void Read_only_entry_and_entry_number_still_accept_selection_input()
    {
        var entry = new Entry { IsReadOnly = true };
        var number = new EntryNumber { IsReadOnly = true };

        Assert.True(entry.CanHandleInput);
        Assert.True(number.CanHandleInput);
    }

    [Fact]
    public void Read_only_text_inputs_do_not_request_the_virtual_keyboard()
    {
        var entry = new TestEntry { IsReadOnly = true };

        Assert.False(entry.ShouldShowVirtualKeyboard);
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

    private sealed class TestEntry : Entry
    {
        public float HandleStemHeight => 14f;
        public float HandleRadius => 9f;

        public System.Numerics.Vector2 SelectionHandleAt(int position) =>
            GetSelectionHandlePosition(position);

        public TextSelectionContextMenuContext SelectionMenuContext() =>
            CreateSelectionContextMenuContext();

    }
}
