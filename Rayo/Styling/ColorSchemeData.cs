namespace Rayo.Styling;

using Rayo.Rendering;

/// <summary>
/// Immutable semantic color roles shared by Rayo controls.
/// Create variants from an existing palette with a <c>with</c> expression.
/// </summary>
public sealed record ColorScheme
{
    public required Color Primary { get; init; }
    public required Color PrimaryHover { get; init; }
    public required Color PrimaryPressed { get; init; }
    public required Color OnPrimary { get; init; }

    public required Color Secondary { get; init; }
    public required Color SecondaryHover { get; init; }
    public required Color SecondaryPressed { get; init; }
    public required Color OnSecondary { get; init; }

    public required Color Background { get; init; }
    public required Color OnBackground { get; init; }
    public required Color Surface { get; init; }
    public required Color SurfaceHover { get; init; }
    public required Color SurfacePressed { get; init; }
    public required Color OnSurface { get; init; }

    public required Color Border { get; init; }
    public required Color Focus { get; init; }
    public required Color Disabled { get; init; }
    public required Color OnDisabled { get; init; }

    public required Color Success { get; init; }
    public required Color OnSuccess { get; init; }
    public required Color Warning { get; init; }
    public required Color OnWarning { get; init; }
    public required Color Danger { get; init; }
    public required Color OnDanger { get; init; }
    public required Color Info { get; init; }
    public required Color OnInfo { get; init; }
}

/// <summary>
/// Built-in semantic color palettes.
/// </summary>
public static class ColorSchemes
{
    public static ColorScheme Light { get; } = new()
    {
        Primary = new Color(59, 130, 246),
        PrimaryHover = new Color(37, 99, 235),
        PrimaryPressed = new Color(29, 78, 216),
        OnPrimary = Color.White,

        Secondary = new Color(107, 114, 128),
        SecondaryHover = new Color(75, 85, 99),
        SecondaryPressed = new Color(55, 65, 81),
        OnSecondary = Color.White,

        Background = new Color(249, 250, 251),
        OnBackground = new Color(17, 24, 39),
        Surface = Color.White,
        SurfaceHover = new Color(243, 244, 246),
        SurfacePressed = new Color(229, 231, 235),
        OnSurface = new Color(17, 24, 39),

        Border = new Color(209, 213, 219),
        Focus = new Color(37, 99, 235),
        Disabled = new Color(229, 231, 235),
        OnDisabled = new Color(156, 163, 175),

        Success = new Color(34, 197, 94),
        OnSuccess = Color.White,
        Warning = new Color(245, 158, 11),
        OnWarning = new Color(17, 24, 39),
        Danger = new Color(239, 68, 68),
        OnDanger = Color.White,
        Info = new Color(139, 92, 246),
        OnInfo = Color.White,
    };

    public static ColorScheme Dark { get; } = new()
    {
        Primary = new Color(96, 165, 250),
        PrimaryHover = new Color(59, 130, 246),
        PrimaryPressed = new Color(37, 99, 235),
        OnPrimary = new Color(15, 23, 42),

        Secondary = new Color(156, 163, 175),
        SecondaryHover = new Color(107, 114, 128),
        SecondaryPressed = new Color(75, 85, 99),
        OnSecondary = new Color(17, 24, 39),

        Background = new Color(17, 24, 39),
        OnBackground = new Color(243, 244, 246),
        Surface = new Color(31, 41, 55),
        SurfaceHover = new Color(55, 65, 81),
        SurfacePressed = new Color(75, 85, 99),
        OnSurface = new Color(243, 244, 246),

        Border = new Color(75, 85, 99),
        Focus = new Color(96, 165, 250),
        Disabled = new Color(55, 65, 81),
        OnDisabled = new Color(156, 163, 175),

        Success = new Color(74, 222, 128),
        OnSuccess = new Color(17, 24, 39),
        Warning = new Color(251, 191, 36),
        OnWarning = new Color(17, 24, 39),
        Danger = new Color(248, 113, 113),
        OnDanger = new Color(17, 24, 39),
        Info = new Color(167, 139, 250),
        OnInfo = new Color(17, 24, 39),
    };

    public static ColorScheme HighContrast { get; } = new()
    {
        Primary = new Color(255, 255, 0),
        PrimaryHover = new Color(255, 255, 128),
        PrimaryPressed = new Color(255, 215, 0),
        OnPrimary = Color.Black,

        Secondary = new Color(0, 255, 255),
        SecondaryHover = new Color(128, 255, 255),
        SecondaryPressed = new Color(0, 215, 215),
        OnSecondary = Color.Black,

        Background = Color.Black,
        OnBackground = Color.White,
        Surface = Color.Black,
        SurfaceHover = new Color(32, 32, 32),
        SurfacePressed = new Color(64, 64, 64),
        OnSurface = Color.White,

        Border = Color.White,
        Focus = new Color(255, 255, 0),
        Disabled = new Color(48, 48, 48),
        OnDisabled = new Color(192, 192, 192),

        Success = new Color(0, 255, 0),
        OnSuccess = Color.Black,
        Warning = new Color(255, 255, 0),
        OnWarning = Color.Black,
        Danger = new Color(255, 96, 96),
        OnDanger = Color.Black,
        Info = new Color(0, 255, 255),
        OnInfo = Color.Black,
    };
}
