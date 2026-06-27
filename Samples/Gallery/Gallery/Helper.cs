using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;

namespace Gallery;

public static class Helper
{
    // Creates a page header with a title and description.
    public static VisualElement CreatePageHeader(string title, string description)
    {
        return new VStack()
            .Spacing(8)
            .Margin(new Thickness(0, 0, 0, 20))
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                new PaletteLabel(title, colors => colors.Primary)
                    .FontSize(28),

                new PaletteLabel(description, colors => colors.OnDisabled)
                    .FontSize(14)
            );
    }

    // Creates a section with a title and content for examples.
    public static VisualElement CreateExampleSection(string title, VisualElement content)
    {
        return new PaletteFrame(colors => colors.Surface)
            .BorderRadius(8)
            .Padding(new Thickness(20))
            .Content(
                new VStack()
                    .Spacing(15)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new PaletteLabel(title, colors => colors.OnSurface)
                            .FontSize(16),

                        content
                    )
            );
    }

    // Creates an informational card with a title and description.
    public static VisualElement CreateInfoCard(string title, string description)
    {
        return new PaletteFrame(colors => colors.SurfaceHover)
            .BorderRadius(8)
            .Padding(new Thickness(16))
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new PaletteLabel(title, colors => colors.Primary)
                            .FontSize(16),

                        new PaletteLabel(description, colors => colors.OnDisabled)
                            .FontSize(14)
                    )
            );
    }

    // Creates a section title for organizing examples.
    public static VisualElement CreateSectionTitle(string title)
    {
        return new PaletteLabel(title, colors => colors.Primary)
            .FontSize(20)
            .Margin(new Thickness(0, 10, 0, 0));
    }
}

/// <summary>
/// Semantic colors used by Gallery examples. Pages should use these values for
/// layout and meaning; literal colors are reserved for color-centric samples.
/// </summary>
public static class GalleryPalette
{
    private static ColorPalette Colors => RayoThemes.Current.Colors;

    public static Color Primary => Colors.Primary;
    public static Color PrimaryHover => Colors.PrimaryHover;
    public static Color PrimaryPressed => Colors.PrimaryPressed;
    public static Color OnPrimary => Colors.OnPrimary;
    public static Color Secondary => Colors.Secondary;
    public static Color OnSecondary => Colors.OnSecondary;
    public static Color Background => Colors.Background;
    public static Color OnBackground => Colors.OnBackground;
    public static Color Surface => Colors.Surface;
    public static Color SurfaceHover => Colors.SurfaceHover;
    public static Color SurfacePressed => Colors.SurfacePressed;
    public static Color OnSurface => Colors.OnSurface;
    public static Color Border => Colors.Border;
    public static Color Disabled => Colors.Disabled;
    public static Color Muted => Colors.OnDisabled;
    public static Color Success => Colors.Success;
    public static Color OnSuccess => Colors.OnSuccess;
    public static Color Warning => Colors.Warning;
    public static Color OnWarning => Colors.OnWarning;
    public static Color Danger => Colors.Danger;
    public static Color OnDanger => Colors.OnDanger;
    public static Color Info => Colors.Info;
    public static Color OnInfo => Colors.OnInfo;
}
