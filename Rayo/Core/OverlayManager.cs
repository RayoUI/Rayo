namespace Rayo.Core;

public static class OverlayManager
{
    private static UITree? _currentTree;
    private static float _windowWidth;
    private static float _windowHeight;

    public static void SetTree(UITree tree)
    {
        _currentTree = tree;
    }

    public static void SetWindowSize(float width, float height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public static float WindowWidth => _windowWidth;
    public static float WindowHeight => _windowHeight;

    public static void AddOverlay(VisualElement overlay, VisualElement? owner = null)
    {
        var app = UIApplication.Current;
        if (app != null)
        {
            app.AddOverlay(overlay, owner);
            return;
        }

        _currentTree?.AddOverlay(overlay, owner);
    }

    public static void RemoveOverlay(VisualElement overlay)
    {
        var app = UIApplication.Current;
        if (app != null)
        {
            app.RemoveOverlay(overlay);
            return;
        }

        _currentTree?.RemoveOverlay(overlay);
    }

    /// <summary>Moves an overlay above every other overlay in the current UI tree.</summary>
    public static void BringOverlayToFront(VisualElement overlay)
    {
        var app = UIApplication.Current;
        if (app != null)
        {
            app.BringOverlayToFront(overlay);
            return;
        }

        _currentTree?.BringOverlayToFront(overlay);
    }

    public static EventManager? EventManager => UIApplication.Current?.EventManager ?? _currentTree?.EventManager;
}
