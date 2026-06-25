using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace MobileApp.Components;

public sealed record AppBarOverflowItem(string Text, IconData Icon, Action Action);

public class AppBar : Component
{
    private static VisualElement? _openOverflowOverlay;

    private readonly IReadableSignal<string> _title;
    private readonly IReadableSignal<bool> _canGoBack;
    private readonly Action _onBack;
    private readonly Action _onOpenMenu;
    private readonly IReadOnlyList<AppBarOverflowItem> _overflowItems;

    public AppBar(
        IReadableSignal<string> title,
        IReadableSignal<bool> canGoBack,
        Action onBack,
        Action onOpenMenu,
        IReadOnlyList<AppBarOverflowItem> overflowItems)
    {
        _title = title;
        _canGoBack = canGoBack;
        _onBack = onBack;
        _onOpenMenu = onOpenMenu;
        _overflowItems = overflowItems;
    }

    public override VisualElement Build()
    {
        if (_overflowItems.Count == 0)
        {
            CloseOverflowMenu();
        }

        return new Frame()
            .Height(60)
            .Background(new Color(25, 39, 62))
            .Padding(new Thickness(8, 8))
            .Content(
                new Grid()
                    .Height(44)
                    .Columns(GridLength.Pixels(44), GridLength.Star, GridLength.Pixels(44))
                    .Rows(GridLength.Star)
                    .AddChild(BuildNavigationButton(), 0, 0)
                    .AddChild(
                        new Label()
                            .Text(_title)
                            .FontSize(18)
                            .Foreground(Color.White)
                            .VerticalAlignment(VerticalAlignment.Center),
                        0,
                        1)
                    .AddChild(BuildOverflowButton(), 0, 2));
    }

    private VisualElement BuildNavigationButton()
    {
        if (_canGoBack.Value)
        {
            return CreateAppBarButton(Icons.ChevronLeft, _onBack);
        }

        return CreateAppBarButton(Icons.Menu, _onOpenMenu);
    }

    private VisualElement BuildOverflowButton()
    {
        if (_overflowItems.Count == 0)
        {
            return new Frame()
                .Size(new Size(44, 44))
                .Background(Color.Transparent);
        }

        ButtonIcon? button = null;
        button = CreateAppBarButton(Icons.MoreVertical, () =>
        {
            if (button is not null)
            {
                ToggleOverflowMenu(button);
            }
        });

        return button.WithTooltip("More options");
    }

    private static ButtonIcon CreateAppBarButton(IconData icon, Action onTapped)
    {
        return new ButtonIcon(icon)
            .Size(44)
            .IconSize(24)
            .IconColor(Color.White)
            .Background(Color.Transparent)
            .HoverBackground(new Color(43, 63, 94))
            .PressedBackground(new Color(16, 27, 43))
            .BorderWidth(0)
            .OnTapped(onTapped);
    }

    private void ToggleOverflowMenu(VisualElement anchor)
    {
        if (_openOverflowOverlay is not null)
        {
            OverlayManager.RemoveOverlay(_openOverflowOverlay);
            _openOverflowOverlay = null;
            return;
        }

        var overlay = BuildOverflowOverlay(anchor);
        _openOverflowOverlay = overlay;
        OverlayManager.AddOverlay(overlay);
    }

    private VisualElement BuildOverflowOverlay(VisualElement anchor)
    {
        var menu = BuildOverflowMenu(anchor);

        return new OverflowOverlayFrame(menu, CloseOverflowMenu)
            .Background(Color.Transparent)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
    }

    private VisualElement BuildOverflowMenu(VisualElement anchor)
    {
        const float menuWidth = 196f;
        var x = Math.Max(8f, anchor.ComputedX + anchor.ComputedWidth - menuWidth);
        var y = anchor.ComputedY + anchor.ComputedHeight + 6f;

        return new Frame()
            .X(x)
            .Y(y)
            .Width(menuWidth)
            .Background(new Color(45, 55, 72))
            .BorderColor(new Color(76, 89, 112))
            .BorderWidth(1)
            .BorderRadius(8)
            .Padding(new Thickness(6))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new VStack()
                    .Spacing(2)
                    .Children(_overflowItems.Select(CreateOverflowMenuItem).ToArray()));
    }

    private VisualElement CreateOverflowMenuItem(AppBarOverflowItem item)
    {
        return new MenuItem(item.Text, () =>
            {
                CloseOverflowMenu();
                item.Action();
            })
            .IconOptions(new MenuItemIconOptions(item.Icon, new Color(196, 210, 232), Size: 16f))
            .Height(40)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    private static void CloseOverflowMenu()
    {
        if (_openOverflowOverlay is null)
        {
            return;
        }

        OverlayManager.RemoveOverlay(_openOverflowOverlay);
        _openOverflowOverlay = null;
    }

    private sealed class OverflowOverlayFrame : Absolute, Rayo.Core.Input.IPointerHandler
    {
        private readonly VisualElement _menu;
        private readonly Action _close;

        public OverflowOverlayFrame(VisualElement menu, Action close)
        {
            _menu = menu;
            _close = close;
            AddChild(menu);
        }

        public void OnPointerReleased(Rayo.Core.Input.PointerEventArgs args)
        {
            var position = args.Position;
            var insideMenu =
                position.X >= _menu.ComputedX &&
                position.X <= _menu.ComputedX + _menu.ComputedWidth &&
                position.Y >= _menu.ComputedY &&
                position.Y <= _menu.ComputedY + _menu.ComputedHeight;

            if (!insideMenu)
            {
                _close();
            }
        }
    }
}
