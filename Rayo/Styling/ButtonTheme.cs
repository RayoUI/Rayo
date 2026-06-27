namespace Rayo.Styling;

using Rayo.Rendering;

/// <summary>
/// Colors used by a button variant in its interactive states.
/// </summary>
public sealed record ButtonColors
{
    public required Color Background { get; init; }
    public required Color HoverBackground { get; init; }
    public required Color PressedBackground { get; init; }
    public required Color Foreground { get; init; }
    public required Color Border { get; init; }
}

/// <summary>
/// Typed color tokens for every built-in button variant.
/// </summary>
public sealed record ButtonTheme
{
    public required ButtonColors Primary { get; init; }
    public required ButtonColors Secondary { get; init; }
    public required ButtonColors Danger { get; init; }
    public required ButtonColors Ghost { get; init; }

    public static ButtonTheme FromPalette(ColorPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        return new ButtonTheme
        {
            Primary = new ButtonColors
            {
                Background = palette.Primary,
                HoverBackground = palette.PrimaryHover,
                PressedBackground = palette.PrimaryPressed,
                Foreground = palette.OnPrimary,
                Border = palette.PrimaryPressed,
            },
            Secondary = new ButtonColors
            {
                Background = palette.Secondary,
                HoverBackground = palette.SecondaryHover,
                PressedBackground = palette.SecondaryPressed,
                Foreground = palette.OnSecondary,
                Border = palette.SecondaryPressed,
            },
            Danger = new ButtonColors
            {
                Background = palette.Danger,
                HoverBackground = Shade(palette.Danger, 0.88f),
                PressedBackground = Shade(palette.Danger, 0.72f),
                Foreground = palette.OnDanger,
                Border = Shade(palette.Danger, 0.72f),
            },
            Ghost = new ButtonColors
            {
                Background = Color.Transparent,
                HoverBackground = palette.SurfaceHover,
                PressedBackground = palette.SurfacePressed,
                Foreground = palette.Primary,
                Border = Color.Transparent,
            },
        };
    }

    private static Color Shade(Color color, float factor) =>
        new(color.R * factor, color.G * factor, color.B * factor, color.A);
}
