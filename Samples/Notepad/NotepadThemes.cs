using Rayo.Rendering;
using Rayo.Styling;

namespace Notepad;

/// <summary>
/// Application-specific themes for the Notepad sample.
/// These themes are intentionally kept outside Rayo's built-in theme catalog.
/// </summary>
internal static class NotepadThemes
{
    public static ThemeData Neon { get; } = CreateNeonTheme();

    private static ThemeData CreateNeonTheme()
    {
        var palette = ColorSchemes.Dark with
        {
            Primary = new Color(0, 255, 213),
            PrimaryHover = new Color(72, 255, 225),
            PrimaryPressed = new Color(0, 204, 170),
            OnPrimary = new Color(4, 18, 24),

            Secondary = new Color(255, 0, 153),
            SecondaryHover = new Color(255, 77, 184),
            SecondaryPressed = new Color(204, 0, 122),
            OnSecondary = new Color(24, 4, 18),

            Background = new Color(5, 8, 18),
            OnBackground = new Color(225, 255, 249),
            Surface = new Color(13, 20, 36),
            SurfaceHover = new Color(22, 34, 56),
            SurfacePressed = new Color(31, 48, 76),
            OnSurface = new Color(225, 255, 249),

            Border = new Color(31, 107, 112),
            Focus = new Color(0, 255, 213),
            Disabled = new Color(38, 48, 65),
            OnDisabled = new Color(116, 139, 148),

            Success = new Color(57, 255, 20),
            OnSuccess = new Color(5, 24, 8),
            Warning = new Color(255, 230, 0),
            OnWarning = new Color(24, 20, 0),
            Danger = new Color(255, 45, 117),
            OnDanger = Color.White,
            Info = new Color(163, 73, 255),
            OnInfo = Color.White,
        };

        return new ThemeData(
            "notepad-neon",
            palette,
            brightness: ThemeBrightness.Dark);
    }
}
