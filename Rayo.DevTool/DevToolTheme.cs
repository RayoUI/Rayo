using Rayo.Core;
using Rayo.Styling;

namespace Rayo.DevTool;

/// <summary>
/// Semantic palette used by the DevTools UI.
/// </summary>
internal static class DevToolTheme
{
    public static ColorPalette Colors =>
        UIApplication.Current?.ActiveTheme.Colors ?? RayoThemes.Current.Colors;
}
