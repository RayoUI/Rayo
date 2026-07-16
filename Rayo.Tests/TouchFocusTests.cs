using System.Numerics;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Layout;

namespace Rayo.Tests;

public sealed class TouchFocusTests
{
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
}
