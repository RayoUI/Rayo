using System.Numerics;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Layout;

namespace Rayo.Tests;

public sealed class TouchFocusTests
{
    [Fact]
    public void First_touch_on_editor_commits_focus_only_after_a_completed_tap()
    {
        var editor = new Editor("first\nsecond\nthird").Height(120);
        var tree = CreateTree(editor);
        var position = Center(editor);

        tree.EventManager!.ProcessTouchDown(PointerEventArgs.FromTouch(1, position, 1));

        Assert.Null(tree.EventManager.FocusedElement);

        tree.EventManager.ProcessTouchUp(Released(1, position));

        Assert.Same(editor, tree.EventManager.FocusedElement);
    }

    [Fact]
    public void First_drag_on_editor_scroll_surface_does_not_show_keyboard()
    {
        var editor = new Editor("first\nsecond\nthird").Height(120);
        var tree = CreateTree(editor);
        var start = Center(editor);
        var end = start - new Vector2(0, 24);

        tree.EventManager!.ProcessTouchDown(PointerEventArgs.FromTouch(1, start, 1));
        tree.EventManager.ProcessTouchMove(PointerEventArgs.FromTouch(1, end, 1));
        tree.EventManager.ProcessTouchUp(Released(1, end));

        Assert.Null(tree.EventManager.FocusedElement);
    }

    [Fact]
    public void Tapping_button_keeps_text_focus_until_tap_is_released()
    {
        var entry = new Entry().Height(40);
        bool tapped = false;
        var button = new Button()
            .Text("Create")
            .Height(40)
            .OnTapped(() => tapped = true);
        var root = new VStack()
            .Width(300)
            .Spacing(8)
            .Children(entry, button);
        var tree = new UITree();
        tree.SetRoot(root);
        tree.InitializeEventManager(null);
        tree.Update(320, 240);
        tree.EventManager!.SetFocus(entry);

        var position = new Vector2(
            button.ComputedX + button.ComputedWidth / 2,
            button.ComputedY + button.ComputedHeight / 2);

        tree.EventManager.ProcessTouchDown(PointerEventArgs.FromTouch(1, position, 1));

        Assert.Same(entry, tree.EventManager.FocusedElement);
        Assert.False(tapped);

        var up = PointerEventArgs.FromTouch(1, position, 0);
        up.IsInContact = false;
        tree.EventManager.ProcessTouchUp(up);

        Assert.True(tapped);
        Assert.Null(tree.EventManager.FocusedElement);
    }

    private static UITree CreateTree(VisualElement element)
    {
        var tree = new UITree();
        tree.SetRoot(element);
        tree.InitializeEventManager(null);
        tree.Update(320, 240);
        return tree;
    }

    private static Vector2 Center(VisualElement element) => new(
        element.ComputedX + element.ComputedWidth / 2,
        element.ComputedY + element.ComputedHeight / 2);

    private static PointerEventArgs Released(int pointerId, Vector2 position)
    {
        var args = PointerEventArgs.FromTouch(pointerId, position, 0);
        args.IsInContact = false;
        return args;
    }

}
