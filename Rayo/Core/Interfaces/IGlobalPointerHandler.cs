namespace Rayo.Core.Interfaces;

using System.Numerics;

/// <summary>
/// Interface for components that need to handle global pointer events
/// to detect clicks outside their area (e.g., floating Frames, dropdowns, popups).
/// </summary>
public interface IGlobalPointerHandler
{
    /// <summary>
    /// Called when a pointer event occurs anywhere in the application.
    /// The component should check if the pointer is outside its area and react accordingly.
    /// </summary>
    /// <param name="position">The pointer position in screen coordinates</param>
    /// <param name="hitElement">The element that was hit by the pointer (if any)</param>
    /// <returns>True if the handler consumed the event and no further handlers should be called</returns>
    bool HandleGlobalPointer(Vector2 position, VisualElement? hitElement);
}
