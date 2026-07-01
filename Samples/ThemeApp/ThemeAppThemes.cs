using Rayo.Rendering;
using Rayo.Styling;

namespace ThemeApp;

internal static class ThemeAppThemes
{
    public static Theme Neon { get; } = new("neon", new ColorPalette
    {
        Primary = new Color(0, 255, 194),
        PrimaryHover = new Color(51, 255, 211),
        PrimaryPressed = new Color(0, 204, 155),
        OnPrimary = new Color(3, 12, 20),

        Secondary = new Color(255, 45, 185),
        SecondaryHover = new Color(255, 91, 202),
        SecondaryPressed = new Color(214, 24, 148),
        OnSecondary = Color.White,

        Background = new Color(4, 8, 20),
        OnBackground = new Color(226, 255, 248),
        Surface = new Color(10, 20, 38),
        SurfaceHover = new Color(17, 36, 59),
        SurfacePressed = new Color(24, 52, 76),
        OnSurface = new Color(226, 255, 248),

        Border = new Color(32, 104, 117),
        Focus = new Color(0, 255, 194),
        Disabled = new Color(28, 45, 58),
        OnDisabled = new Color(105, 139, 145),

        Success = new Color(57, 255, 20),
        OnSuccess = new Color(3, 12, 20),
        Warning = new Color(255, 230, 0),
        OnWarning = new Color(20, 16, 0),
        Danger = new Color(255, 49, 94),
        OnDanger = Color.White,
        Info = new Color(69, 124, 255),
        OnInfo = Color.White,
    });

    public static Theme Obsidian { get; } = new("obsidian", new ColorPalette
    {
        Primary = new Color(196, 143, 255),
        PrimaryHover = new Color(214, 174, 255),
        PrimaryPressed = new Color(157, 101, 219),
        OnPrimary = new Color(20, 14, 27),

        Secondary = new Color(92, 107, 128),
        SecondaryHover = new Color(116, 132, 153),
        SecondaryPressed = new Color(69, 81, 99),
        OnSecondary = Color.White,

        Background = new Color(10, 10, 13),
        OnBackground = new Color(239, 236, 244),
        Surface = new Color(22, 22, 28),
        SurfaceHover = new Color(34, 34, 42),
        SurfacePressed = new Color(47, 47, 58),
        OnSurface = new Color(239, 236, 244),

        Border = new Color(61, 58, 70),
        Focus = new Color(196, 143, 255),
        Disabled = new Color(39, 38, 45),
        OnDisabled = new Color(119, 115, 128),

        Success = new Color(90, 200, 140),
        OnSuccess = new Color(9, 25, 17),
        Warning = new Color(224, 178, 89),
        OnWarning = new Color(29, 21, 7),
        Danger = new Color(224, 91, 112),
        OnDanger = Color.White,
        Info = new Color(111, 154, 232),
        OnInfo = new Color(8, 17, 32),
    });
}
