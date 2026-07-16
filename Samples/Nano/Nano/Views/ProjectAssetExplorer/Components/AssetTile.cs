using Rayo.Controls;
using Rayo.Core.Input;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal sealed class AssetTile : Frame, IPointerHandler
{
    private static readonly Color NormalBackground = new(30, 41, 59);
    private static readonly Color HoverBackground = new(51, 65, 85);
    private static readonly Color PressedBackground = new(62, 126, 214);
    private readonly Action _action;
    private bool _isPressed;

    public AssetTile(Action action)
    {
        _action = action;
        Background = NormalBackground;
    }

    public void OnPointerEntered(PointerEventArgs args)
    {
        if (!_isPressed)
        {
            Background = HoverBackground;
        }
    }

    public void OnPointerExited(PointerEventArgs args) => ResetInteraction();

    public void OnPointerPressed(PointerEventArgs args)
    {
        if (args.Button != 0)
        {
            return;
        }

        _isPressed = true;
        Background = PressedBackground;
    }

    public void OnPointerReleased(PointerEventArgs args)
    {
        if (!_isPressed)
        {
            return;
        }

        _isPressed = false;
        Background = HoverBackground;
        _action();
    }

    public void OnPointerCanceled(PointerEventArgs args) => ResetInteraction();

    private void ResetInteraction()
    {
        _isPressed = false;
        Background = NormalBackground;
    }
}
