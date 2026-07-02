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

    public StateMap<Color> Backgrounds =>
        new StateMap<Color>(Background)
            .With(ControlState.Hovered, HoverBackground)
            .With(ControlState.Pressed, PressedBackground);
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
    public Thickness Padding { get; init; } = new(12, 6, 12, 6);
    public CornerRadius Radius { get; init; } = new(4);
    public TypographyStyle Typography { get; init; } =
        new() { FontSize = 14, FontWeight = Rayo.Core.FontWeight.Medium };
    public float MinHeight { get; init; } = 32;
    public float BorderThickness { get; init; } = 2;

    public static ButtonTheme FromScheme(ColorScheme palette)
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
        ThemeColorUtilities.AdjustLightness(color, factor - 1f);

    internal void Validate()
    {
        if (MinHeight < 0 || BorderThickness < 0)
            throw new ArgumentOutOfRangeException(nameof(ButtonTheme));
        Typography.Validate(nameof(Typography));
    }
}
