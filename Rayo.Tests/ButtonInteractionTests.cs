using System.Numerics;
using Rayo.Controls;
using Rayo.Core.Input;

namespace Rayo.Tests;

public sealed class ButtonInteractionTests
{
    [Fact]
    public void Pointer_cancellation_clears_pressed_state()
    {
        var button = new Button();
        var pointer = PointerEventArgs.FromTouch(1, new Vector2(10, 10));

        button.OnPointerPressed(pointer);
        button.OnPointerCanceled(pointer);

        Assert.False(button.IsPressed);
    }
}
