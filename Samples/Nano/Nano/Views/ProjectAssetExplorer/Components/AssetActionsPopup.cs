using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal sealed class AssetActionsPopup
{
    private VisualElement? _overlay;

    public bool IsOpen => _overlay != null;

    public void Toggle(
        VisualElement anchor,
        Action createFolder,
        Action<AssetViewMode> setViewMode,
        VisualElement owner)
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        const float menuWidth = 176;
        var menu = new Frame()
            .X(Math.Max(8, anchor.ComputedX + anchor.ComputedWidth - menuWidth))
            .Y(anchor.ComputedY + anchor.ComputedHeight + 6)
            .Width(menuWidth)
            .Background(new Color(45, 55, 72))
            .BorderBrush(new Color(76, 89, 112))
            .BorderThickness(1)
            .BorderRadius(8)
            .Padding(new Thickness(6))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new VStack()
                    .Spacing(2)
                    .Children(
                        CreateAction("New folder", Icons.FolderCreateNew, () =>
                        {
                            Close();
                            createFolder();
                        }),
                        CreateAction("List view", Icons.ListView, () =>
                        {
                            Close();
                            setViewMode(AssetViewMode.List);
                        }),
                        CreateAction("Grid view", Icons.GridView, () =>
                        {
                            Close();
                            setViewMode(AssetViewMode.Grid);
                        })));

        _overlay = new DismissibleOverlay(menu, Close)
            .Background(Color.Transparent)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        OverlayManager.AddOverlay(_overlay, owner);
    }

    public void Close()
    {
        if (_overlay == null)
        {
            return;
        }

        OverlayManager.RemoveOverlay(_overlay);
        _overlay = null;
    }

    private static VisualElement CreateAction(string text, IconData icon, Action action) =>
        new MenuItem(text, action)
            .IconOptions(new MenuItemIconOptions(icon, new Color(196, 210, 232), Size: 16f))
            .Height(40)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

    private sealed class DismissibleOverlay : Absolute, IPointerHandler
    {
        private readonly VisualElement _menu;
        private readonly Action _close;

        public DismissibleOverlay(VisualElement menu, Action close)
        {
            _menu = menu;
            _close = close;
            AddChild(menu);
        }

        public void OnPointerReleased(PointerEventArgs args)
        {
            var position = args.Position;
            var isInsideMenu =
                position.X >= _menu.ComputedX &&
                position.X <= _menu.ComputedX + _menu.ComputedWidth &&
                position.Y >= _menu.ComputedY &&
                position.Y <= _menu.ComputedY + _menu.ComputedHeight;

            if (!isInsideMenu)
            {
                _close();
            }
        }
    }
}
