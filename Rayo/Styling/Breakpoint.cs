namespace Rayo.Styling;

/// <summary>
/// Named window-width breakpoints for responsive styles, equivalent to CSS <c>@media</c> queries.
/// Evaluated against <see cref="Rayo.Core.UIApplication.WindowWidth"/> at apply time.
/// </summary>
public enum Breakpoint
{
    /// <summary>Window width &lt; 480 px.</summary>
    XSmall,
    /// <summary>Window width 480 – 767 px.</summary>
    Small,
    /// <summary>Window width 768 – 1023 px.</summary>
    Medium,
    /// <summary>Window width 1024 – 1439 px.</summary>
    Large,
    /// <summary>Window width ≥ 1440 px.</summary>
    XLarge,
}

/// <summary>Resolves the active <see cref="Breakpoint"/> from the current window width.</summary>
internal static class BreakpointHelper
{
    private static Breakpoint _last = Breakpoint.Large;

    /// <summary>
    /// Fired when the active breakpoint changes (i.e. the window crosses a width threshold).
    /// Subscribers receive the new <see cref="Breakpoint"/> value.
    /// </summary>
    internal static event Action<Breakpoint>? BreakpointChanged;

    /// <summary>
    /// Fired on <em>every</em> window size change, regardless of whether the active named
    /// breakpoint changed. Required by custom min/max-width style conditions so they can
    /// re-evaluate at any pixel boundary, not just at predefined thresholds.
    /// Subscribers receive the new window width in pixels.
    /// </summary>
    internal static event Action<float>? WindowResized;

    public static Breakpoint Current
    {
        get
        {
            var width = Rayo.Core.UIApplication.Current?.WindowWidth ?? 1024f;
            return width switch
            {
                < 480f  => Breakpoint.XSmall,
                < 768f  => Breakpoint.Small,
                < 1024f => Breakpoint.Medium,
                < 1440f => Breakpoint.Large,
                _       => Breakpoint.XLarge,
            };
        }
    }

    /// <summary>
    /// Compares the current breakpoint with the last known value and fires
    /// <see cref="BreakpointChanged"/> if they differ. Called from
    /// <c>UIApplication.OnResize</c> on every window-resize event.
    /// </summary>
    internal static void NotifyIfChanged()
    {
        // Always fire WindowResized so custom min/max-width conditions re-evaluate.
        var width = Rayo.Core.UIApplication.Current?.WindowWidth ?? 1024f;
        WindowResized?.Invoke(width);

        var current = Current;
        if (current == _last) return;
        _last = current;
        BreakpointChanged?.Invoke(current);
    }
}
