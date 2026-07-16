namespace Rayo.Core.Interfaces;

/// <summary>
/// Describes how a Rayo overlay interacts with platform-native overlay views.
/// </summary>
public interface INativeOverlayPolicy
{
    /// <summary>
    /// Gets whether platform-native overlay views must be hidden while this overlay is active.
    /// </summary>
    bool BlocksNativeOverlays { get; }
}
