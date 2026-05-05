namespace Rayo.Styling;

/// <summary>
/// Window / screen orientation, equivalent to the CSS
/// <c>orientation</c> media feature.
/// </summary>
public enum Orientation
{
    /// <summary>Window height is greater than its width.</summary>
    Portrait,
    /// <summary>Window width is greater than or equal to its height.</summary>
    Landscape,
}

/// <summary>
/// Resolves the current window <see cref="Orientation"/> and fires
/// <see cref="OrientationChanged"/> when it flips.
/// </summary>
internal static class OrientationHelper
{
    private static Orientation _last = Orientation.Landscape;

    /// <summary>Fired when the window crosses the portrait/landscape boundary.</summary>
    internal static event Action<Orientation>? OrientationChanged;

    /// <summary>Current orientation derived from the active window dimensions.</summary>
    public static Orientation Current
    {
        get
        {
            var app = Rayo.Core.UIApplication.Current;
            float w = app?.WindowWidth  ?? 1024f;
            float h = app?.WindowHeight ??  768f;
            return h > w ? Orientation.Portrait : Orientation.Landscape;
        }
    }

    /// <summary>
    /// Compares the current orientation with the last known value and fires
    /// <see cref="OrientationChanged"/> if they differ.
    /// Called from <c>UIApplication.HandleWindowSizeOrStateChange</c>.
    /// </summary>
    internal static void NotifyIfChanged()
    {
        var current = Current;
        if (current == _last) return;
        _last = current;
        OrientationChanged?.Invoke(current);
    }
}
