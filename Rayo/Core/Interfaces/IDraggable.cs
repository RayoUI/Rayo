namespace Rayo.Core.Interfaces;

/// <summary>
/// Interface for elements that can be dragged with support for universal drag & drop.
/// Implement this interface in any UIElement you want to make draggable.
/// </summary>
public interface IDraggable
{
    /// <summary>
    /// Indicates whether the element is currently being dragged.
    /// </summary>
    bool IsDragging { get; set; }

    /// <summary>
    /// Called when dragging starts.
    /// Returns the data to be dragged, or null to cancel the drag.
    /// </summary>
    /// <param name="mouseX">Mouse X position when the drag started</param>
    /// <param name="mouseY">Mouse Y position when the drag started</param>
    /// <returns>DragData with the information to be dragged, or null to cancel</returns>
    DragData? OnDragStart(float mouseX, float mouseY);

    /// <summary>
    /// Called while the element is being dragged.
    /// Allows updating the visual state during the drag.
    /// </summary>
    /// <param name="mouseX">Current mouse X position</param>
    /// <param name="mouseY">Current mouse Y position</param>
    void OnDragging(float mouseX, float mouseY);

    /// <summary>
    /// Called when dragging ends (mouse button is released).
    /// </summary>
    /// <param name="wasDropped">True if dropped on a valid target, false if canceled</param>
    void OnDragEnd(bool wasDropped);

    /// <summary>
    /// Minimum distance in pixels the mouse must move before starting the drag.
    /// Prevents accidental activation. Default: 5 pixels.
    /// </summary>
    float DragThreshold => 5f;

    /// <summary>
    /// Allows customizing the rendering during the drag.
    /// Returns true to use the default rendering, false to show nothing during the drag.
    /// </summary>
    bool ShouldRenderWhileDragging => true;
}