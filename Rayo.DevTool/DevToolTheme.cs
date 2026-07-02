using Rayo.Core;
using Rayo.Styling;

namespace Rayo.DevTool;

/// <summary>
/// Semantic palette used by the DevTools UI.
/// </summary>
internal static class DevToolTheme
{
    public static ColorScheme Colors =>
        (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors;
}
