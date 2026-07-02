namespace Rayo.Core;

using Rayo.Controls;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;

/// <summary>
/// Métodos helper para patrones comunes de UI y binding
/// </summary>
public static class UIHelpers
{
    /// <summary>
    /// Colores predefinidos para uso común
    /// </summary>
    public static class ColorDefault
    {
        public static Color Primary => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Primary;
        public static Color Success => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Success;
        public static Color Warning => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Warning;
        public static Color Danger => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Danger;
        public static Color Info => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Info;
        public static Color Secondary => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Secondary;
        public static Color Dark => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Background;
        public static Color Light => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.SurfaceHover;
    }
}
