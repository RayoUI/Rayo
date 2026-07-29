using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
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
        var titleLabel = new PaletteLabel(title, colors => colors.Primary)
            .FontSize(28);

        VisualElement titleContent = titleLabel;
        if (!PlatformDetector.IsMobile)
        {
            titleContent = new Grid()
                .Rows(GridLength.Auto)
                .Columns(GridLength.Star, GridLength.Auto)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .AddChild(titleLabel, 0, 0)
                .AddChild(
                    new ThemeToggleButton()
                        .Variant(ButtonVariant.Ghost)
                        .Size(34)
                        .IconSize(17)
                        .OnTapped(ToggleTheme),
                    0,
                    1);
        }

        return new VStack()
            .Spacing(8)
            .Margin(new Thickness(0, 0, 0, 20))
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                titleContent,

                new PaletteLabel(description, colors => colors.OnDisabled)
                    .FontSize(14)
            );
    }

    private static void ToggleTheme()
    {
        var current = UIApplication.Current?.ActiveTheme ?? RayoThemes.Light;
        RayoThemes.UseTheme(
            current.Brightness == ThemeBrightness.Dark
                ? RayoThemes.Light
                : RayoThemes.Dark);
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
                            .LineBreakMode(LineBreakMode.WordWrap)
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
    private static ColorScheme Colors => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors;

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
