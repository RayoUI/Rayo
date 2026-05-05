namespace Rayo.Core;

using Rayo.Controls;
using Rayo.Layout;
using Rayo.Rendering;

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
        public static Color Primary => new Color(59, 130, 246);      // Azul
        public static Color Success => new Color(34, 197, 94);       // Verde
        public static Color Warning => new Color(245, 158, 11);      // Naranja
        public static Color Danger => new Color(239, 68, 68);        // Rojo
        public static Color Info => new Color(139, 92, 246);         // Morado
        public static Color Secondary => new Color(107, 114, 128);   // Gris
        public static Color Dark => new Color(31, 41, 55);           // Gris oscuro
        public static Color Light => new Color(243, 244, 246);       // Gris claro
    }
}