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
        public static Color Primary => RayoThemes.Current.Colors.Primary;
        public static Color Success => RayoThemes.Current.Colors.Success;
        public static Color Warning => RayoThemes.Current.Colors.Warning;
        public static Color Danger => RayoThemes.Current.Colors.Danger;
        public static Color Info => RayoThemes.Current.Colors.Info;
        public static Color Secondary => RayoThemes.Current.Colors.Secondary;
        public static Color Dark => RayoThemes.Current.Colors.Background;
        public static Color Light => RayoThemes.Current.Colors.SurfaceHover;
    }
}
