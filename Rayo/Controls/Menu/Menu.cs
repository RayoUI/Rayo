namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interactions;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using System.Numerics;

/// <summary>
/// A theme-aware dropdown menu with checked items and nested submenus.
/// </summary>
public class Menu : Component, IGlobalPointerHandler
{
    private const float MenuWidth = 210f;
    private readonly string _title;
    private readonly List<MenuItem> _items = [];
    private readonly List<(int Depth, VisualElement Overlay)> _openOverlays = [];
    private readonly Dictionary<int, MenuItem> _openSubmenuItems = [];
    private VisualElement? _anchor;
    private bool _isOpen;

    private static Menu? _currentlyOpenMenu;

    static Menu()
    {
        ScrollInteractionNotifier.ScrollActivity += _ => CloseCurrentMenu();
    }

    public Menu(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _title = title;
    }

    public static void CloseCurrentMenu()
    {
        _currentlyOpenMenu?.CloseMenu();
    }

    public Menu AddItem(MenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
        return this;
    }

    public override VisualElement Build()
    {
        var titleButton = new MenuButton()
            .Text(_title)
            .Variant(ButtonVariant.Ghost)
            .FontSize(12)
            .Padding(new Thickness(10, 5))
            .BorderRadius(0);

        titleButton.OnTapped(() =>
        {
            if (_isOpen)
                CloseMenu();
            else
                OpenMenu(titleButton);
        });

        return titleButton;
    }

    private void OpenMenu(VisualElement anchor)
    {
        if (_currentlyOpenMenu != null && _currentlyOpenMenu != this)
            _currentlyOpenMenu.CloseMenu();

        _isOpen = true;
        _currentlyOpenMenu = this;
        _anchor = anchor;

        var popup = BuildPopup(
            _items,
            anchor.ComputedX,
            anchor.ComputedY + anchor.ComputedHeight,
            depth: 0);

        AddPopup(0, popup);
    }

    private VisualElement BuildPopup(
        IReadOnlyList<MenuItem> items,
        float x,
        float y,
        int depth)
    {
        var entries = items
            .Select(item => item.BuildEntry(
                onHovered: (hoveredItem, anchor) => OnItemHovered(hoveredItem, anchor, depth),
                onActivated: (activatedItem, anchor) => OnItemActivated(activatedItem, anchor, depth)))
            .ToArray();

        return new MenuSurfaceFrame()
            .Width(MenuWidth)
            .X(x)
            .Y(y)
            .BorderThickness(1)
            .Padding(new Thickness(3))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(new VStack().Spacing(0).Children(entries));
    }

    private void OnItemHovered(MenuItem item, VisualElement anchor, int depth)
    {
        int submenuDepth = depth + 1;
        if (!item.HasSubmenu)
        {
            ClosePopupsFrom(submenuDepth);
            return;
        }

        if (_openSubmenuItems.TryGetValue(submenuDepth, out var openItem) &&
            ReferenceEquals(openItem, item))
        {
            return;
        }

        ClosePopupsFrom(submenuDepth);
        _openSubmenuItems[submenuDepth] = item;

        var popup = BuildPopup(
            item.Items,
            anchor.ComputedX + anchor.ComputedWidth,
            anchor.ComputedY,
            submenuDepth);

        AddPopup(submenuDepth, popup);
    }

    private void OnItemActivated(MenuItem item, VisualElement anchor, int depth)
    {
        if (item.HasSubmenu)
        {
            OnItemHovered(item, anchor, depth);
            return;
        }

        item.Invoke();
        CloseMenu();
    }

    private void AddPopup(int depth, VisualElement popup)
    {
        _openOverlays.Add((depth, popup));
        OverlayManager.AddOverlay(popup);
        OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    private void ClosePopupsFrom(int depth)
    {
        foreach (var entry in _openOverlays.Where(entry => entry.Depth >= depth).ToArray())
        {
            OverlayManager.RemoveOverlay(entry.Overlay);
            _openOverlays.Remove(entry);
        }

        foreach (int key in _openSubmenuItems.Keys.Where(key => key >= depth).ToArray())
            _openSubmenuItems.Remove(key);
    }

    private void CloseMenu()
    {
        ClosePopupsFrom(0);
        _isOpen = false;
        _anchor = null;
        OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);

        if (_currentlyOpenMenu == this)
            _currentlyOpenMenu = null;
    }

    public bool HandleGlobalPointer(Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen)
        {
            return false;
        }

        if (IsInsideMenu(hitElement))
        {
            return true;
        }

        if (_anchor?.ContainsWindowPoint(position) == true)
        {
            return true;
        }

        foreach (var (_, overlay) in _openOverlays)
        {
            if (overlay.ContainsWindowPoint(position))
            {
                return true;
            }
        }

        CloseMenu();
        return false;
    }

    private bool IsInsideMenu(VisualElement? element)
    {
        var current = element;
        while (current != null)
        {
            if (current == _anchor || _openOverlays.Any(entry => entry.Overlay == current))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}

/// <summary>Ghost button whose foreground follows surface content colors.</summary>
internal sealed class MenuButton : Button
{
    protected override void OnThemeApplied(Theme theme)
    {
        base.OnThemeApplied(theme);
        SetThemeValue(nameof(TextColor), (Brush)theme.Colors.OnSurface, value => TextColor = value);
    }
}

/// <summary>Popup surface that continues following the active theme while open.</summary>
internal sealed class MenuSurfaceFrame : Frame
{
    public MenuSurfaceFrame()
    {
        InitializeTheme();
    }

    protected override void OnThemeApplied(Theme theme)
    {
        SetThemeValue(nameof(Background), (Brush)theme.Colors.Surface, value => Background = value);
        SetThemeValue(nameof(BorderBrush), (Brush)theme.Colors.Border, value => BorderBrush = value);
    }
}
