namespace Rayo.Core;

using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// Base class for visual elements that contain a single child element.
/// Similar to MAUI ContentView - examples: Frame, ScrollView, Border, Card.
/// Inherits from CompositeElement to support internal child management.
/// </summary>
public abstract class ContentView<T> : VisualElement<T> where T : ContentView<T>
{
    private VisualElement? _content;

    /// <summary>
    /// The single child element contained in this view.
    /// </summary>
    [LayoutProperty]
    public VisualElement? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                if (_content != null)
                {
                    _content.Parent = null;
                }

                _content = value;

                if (_content != null)
                {
                    _content.Parent = this;
                }

                MarkNeedsLayout();
                RaiseTreeStructureChanged(this);
            }
        }
    }

    /// <summary>
    /// Clears the content element.
    /// </summary>
    public virtual void ClearContent()
    {
        Content = null;
    }

    /// <summary>
    /// Gets the content as children enumerable (for rendering system).
    /// </summary>
    internal override IEnumerable<VisualElement> GetChildren()
    {
        if (_content != null)
            yield return _content;
    }
}
