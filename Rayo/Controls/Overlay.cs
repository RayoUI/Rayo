namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

/// <summary>
/// Helper component to manage adding/removing content to the global overlay layer.
/// </summary>
public class Overlay : UserControl
{
    private readonly VisualElement _content;
    private readonly UIApplication _app;
    private bool _isAdded = false;

    public Overlay(UIApplication app, VisualElement content)
    {
        _app = app;
        _content = content;
    }

    public override VisualElement Build()
    {
        // This component doesn't render anything itself in the main tree.
        // It manages the lifecycle of the content in the overlay layer.
        
        // We return an empty Frame as a placeholder in the main tree.
        return new Frame().Size(new Size(0, 0)).IsVisible(false);
    }

    // We need a way to hook into lifecycle (mount/unmount).
    // Currently Component doesn't have explicit Mount/Unmount hooks exposed easily.
    // But we can use the Build method to add it, and we need a way to remove it.
    
    // Since we don't have a full lifecycle system yet, we'll use a pattern where
    // the user calls Show() / Hide() or we do it in the constructor/dispose if we had it.
    
    // Better approach for now:
    // This component adds the content to the overlay when it's built (if not already added).
    // But removing it is tricky if the component is removed from the tree.
    
    // Let's change strategy:
    // The Overlay component will be a wrapper that, when rendered/built, ensures its content is in the overlay layer.
    // But wait, Build is called once.
    
    // Let's just expose static helper methods or use this as a manager.
    
    public void Show()
    {
        if (!_isAdded)
        {
            _app.AddOverlay(_content);
            _isAdded = true;
        }
    }

    public void Hide()
    {
        if (_isAdded)
        {
            _app.RemoveOverlay(_content);
            _isAdded = false;
        }
    }
}
