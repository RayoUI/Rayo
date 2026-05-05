namespace Rayo.Core.Interactions;

using System;
using Rayo.Core;

/// <summary>
/// Centralized notifier that broadcasts when scrolling occurs anywhere in the UI tree.
/// Allows overlays/menus to react without ScrollView knowing about them (OCP compliant).
/// </summary>
public static class ScrollInteractionNotifier
{
    public static event Action<VisualElement>? ScrollActivity;

    public static void NotifyScrollActivity(VisualElement source)
    {
        ScrollActivity?.Invoke(source);
    }
}
