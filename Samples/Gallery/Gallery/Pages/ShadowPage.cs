using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
using static Rayo.Core.UIHelpers;
using Shadow = Rayo.Controls.Shadow;

namespace Gallery.Pages;

public class ShadowPage : Component
{
    public override VisualElement Build()
    {
        var palette = (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors;

        if (PlatformDetector.IsMobile)
        {
            return new VStack()
                .Spacing(20)
                .Padding(new Thickness(20))
                .Children(
                    Helper.CreatePageHeader("Shadow", "Drop shadow effects for elevated UI elements"),
                    Helper.CreateInfoCard(
                        "Mobile preview",
                        "This page shows a reduced set of shadows on mobile to keep scrolling responsive."
                    ),
                    Helper.CreateExampleSection("Presets",
                        new HStack()
                            .Spacing(16)
                            .Alignment(Alignment.Center)
                            .Children(
                                CreateShadowDemo("None", Shadow.None),
                                CreateShadowDemo("Subtle", Shadow.Subtle),
                                CreateShadowDemo("Default", Shadow.Default),
                                CreateShadowDemo("Strong", Shadow.Strong),
                                CreateColoredShadowDemo("Colored", palette.Primary, Shadow.Colored(palette.Primary))
                            )
                    ),
                    Helper.CreateExampleSection("Glow",
                        new HStack()
                            .Spacing(16)
                            .Alignment(Alignment.Center)
                            .Children(
                                CreateColoredShadowDemo("Blue", new Color(59, 130, 246), Shadow.Colored(new Color(59, 130, 246, 255))),
                                CreateColoredShadowDemo("Green", new Color(34, 197, 94), Shadow.Colored(new Color(34, 197, 94, 255)))
                            )
                    ),
                    Helper.CreateExampleSection("Parameters",
                        new HStack()
                            .Spacing(16)
                            .Alignment(Alignment.Center)
                            .Children(
                                CreateShadowDemo("Alpha 25%", new Shadow(new Color(0, 0, 0, 64), 0, 4, 12)),
                                CreateShadowDemo("Offset X", new Shadow(new Color(0, 0, 0, 160), 8, 0, 10)),
                                CreateShadowDemo("Soft", new Shadow(new Color(0, 0, 0, 120), 0, 8, 24))
                            )
                    )
                );
        }

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Shadow", "Drop shadow effects for elevated UI elements"),

                Helper.CreateInfoCard(
                    "How shadows work",
                    "Shadows are rendered as semi-transparent layers behind the element. " +
                    "Each demo uses theme surfaces and borders so the elevation stays visible in light and dark themes."
                ),

                // ── Presets ──────────────────────────────────────────────────────
                Helper.CreateExampleSection("Presets",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateShadowDemo("None",    Shadow.None),
                            CreateShadowDemo("Subtle",  Shadow.Subtle),
                            CreateShadowDemo("Default", Shadow.Default),
                            CreateShadowDemo("Strong",  Shadow.Strong),
                            CreateColoredShadowDemo("Colored", palette.Primary, Shadow.Colored(palette.Primary))
                        )
                ),

                // ── Blur Radius ───────────────────────────────────────────────────
                Helper.CreateExampleSection("Blur Radius",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateShadowDemo("Blur 0",  new Shadow(new Color(0, 0, 0, 220), 3, 3, 0)),
                            CreateShadowDemo("Blur 4",  new Shadow(new Color(0, 0, 0, 220), 3, 3, 4)),
                            CreateShadowDemo("Blur 8",  new Shadow(new Color(0, 0, 0, 220), 3, 3, 8)),
                            CreateShadowDemo("Blur 16", new Shadow(new Color(0, 0, 0, 220), 3, 3, 16)),
                            CreateShadowDemo("Blur 24", new Shadow(new Color(0, 0, 0, 220), 3, 3, 24))
                        )
                ),

                // ── Directional Offset ────────────────────────────────────────────
                Helper.CreateExampleSection("Directional Offset",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateShadowDemo("Bottom",       new Shadow(new Color(0, 0, 0, 220), 0, 6, 8)),
                            CreateShadowDemo("Right",        new Shadow(new Color(0, 0, 0, 220), 6, 0, 8)),
                            CreateShadowDemo("Bottom-Right", new Shadow(new Color(0, 0, 0, 220), 5, 5, 8)),
                            CreateShadowDemo("Top-Left",     new Shadow(new Color(0, 0, 0, 220), -4, -4, 8)),
                            CreateShadowDemo("Centered",     new Shadow(new Color(0, 0, 0, 220), 0, 0, 14))
                        )
                ),

                Helper.CreateExampleSection("Opacity",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateShadowDemo("Alpha 20%", new Shadow(new Color(0, 0, 0, 51), 0, 4, 14)),
                            CreateShadowDemo("Alpha 35%", new Shadow(new Color(0, 0, 0, 89), 0, 4, 14)),
                            CreateShadowDemo("Alpha 55%", new Shadow(new Color(0, 0, 0, 140), 0, 4, 14)),
                            CreateShadowDemo("Alpha 75%", new Shadow(new Color(0, 0, 0, 191), 0, 4, 14))
                        )
                ),

                Helper.CreateExampleSection("Elevation Profiles",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateShadowDemo("Flat Lift", new Shadow(new Color(0, 0, 0, 80), 0, 1, 3)),
                            CreateShadowDemo("Card", new Shadow(new Color(0, 0, 0, 110), 0, 4, 12)),
                            CreateShadowDemo("Popover", new Shadow(new Color(0, 0, 0, 145), 0, 8, 20)),
                            CreateShadowDemo("Modal", new Shadow(new Color(0, 0, 0, 170), 0, 14, 32))
                        )
                ),

                Helper.CreateExampleSection("Axis Combinations",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateShadowDemo("X 10 / Y 2", new Shadow(new Color(0, 0, 0, 150), 10, 2, 12)),
                            CreateShadowDemo("X -10 / Y 2", new Shadow(new Color(0, 0, 0, 150), -10, 2, 12)),
                            CreateShadowDemo("X 0 / Y -8", new Shadow(new Color(0, 0, 0, 150), 0, -8, 14)),
                            CreateShadowDemo("Ring Glow", new Shadow(palette.Primary.WithAlpha(0.85f), 0, 0, 22))
                        )
                ),

                // ── Colored Shadows ───────────────────────────────────────────────
                Helper.CreateExampleSection("Colored Shadows (Glow Effects)",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateColoredShadowDemo("Blue",   new Color(59, 130, 246),  Shadow.Colored(new Color(59, 130, 246, 255))),
                            CreateColoredShadowDemo("Green",  new Color(34, 197, 94),   Shadow.Colored(new Color(34, 197, 94, 255))),
                            CreateColoredShadowDemo("Red",    new Color(239, 68, 68),   Shadow.Colored(new Color(239, 68, 68, 255))),
                            CreateColoredShadowDemo("Purple", new Color(168, 85, 247),  Shadow.Colored(new Color(168, 85, 247, 255))),
                            CreateColoredShadowDemo("Amber",  new Color(234, 179, 8),   Shadow.Colored(new Color(234, 179, 8, 255)))
                        )
                ),

                // ── Real-world Patterns ───────────────────────────────────────────
                Helper.CreateExampleSection("Real-world Patterns",
                    new HStack()
                        .Spacing(24)
                        .Alignment(Alignment.Center)
                        .Children(
                            // Floating action button with glow
                            WrapInTile(
                                new Border()
                                    .CornerRadius(new CornerRadius(28))
                                    .Background(ColorDefault.Primary)
                                    .Shadow(new Shadow(new Color(59, 130, 246, 255), 0, 4, 16))
                                    .Padding(new Thickness(18, 12))
                                    .Content(
                                        new Label("+ New Item")
                                            .FontWeight(FontWeight.SemiBold)
                                            .Foreground(Color.White)
                                    )
                            ),

                            // Elevated card
                            WrapInTile(
                                new Border()
                                    .CornerRadius(new CornerRadius(10))
                                    .Background(palette.Surface)
                                    .BorderBrush(palette.Border)
                                    .BorderThickness(new Thickness(1))
                                    .Shadow(Shadow.Strong)
                                    .Padding(new Thickness(16))
                                    .Content(
                                        new VStack()
                                            .Spacing(4)
                                            .Children(
                                                new Label("Elevated Card")
                                                    .FontSize(14).FontWeight(FontWeight.SemiBold)
                                                    .Foreground(palette.OnSurface),
                                                new Label("Shadow.Strong preset")
                                                    .FontSize(12)
                                                    .Foreground(palette.OnDisabled)
                                            )
                                    )
                            ),

                            // Glow badge
                            WrapInTile(
                                new Border()
                                    .CornerRadius(new CornerRadius(8))
                                    .Background(palette.Surface)
                                    .BorderBrush(new Color(239, 68, 68, 100))
                                    .BorderThickness(new Thickness(1))
                                    .Shadow(new Shadow(new Color(239, 68, 68, 255), 0, 0, 16))
                                    .Padding(new Thickness(14, 10))
                                    .Content(
                                        new HStack()
                                            .Spacing(8).Alignment(Alignment.Center)
                                            .Children(
                                                new Label("\uEA1C")
                                                    .FontFamily("Lineicons").FontSize(14)
                                                    .Foreground(new Color(239, 68, 68)),
                                                new Label("3 Alerts")
                                                    .FontSize(13).FontWeight(FontWeight.Medium)
                                                    .Foreground(palette.OnSurface)
                                            )
                                    )
                            )
                        )
                )
            );
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    // Wraps content in a lighter background tile with padding so shadow is visible
    private static VisualElement WrapInTile(VisualElement content)
    {
        return new Border()
            .CornerRadius(new CornerRadius(10))
            .Background(GetTileBackground())
            .Padding(new Thickness(24, 20))
            .Content(content);
    }

    private static VisualElement CreateShadowDemo(string label, Shadow shadow)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                // Lighter tile so dark shadow has contrast
                new Border()
                    .CornerRadius(new CornerRadius(10))
                    .Background(GetTileBackground())
                    .Padding(new Thickness(24, 20))
                    .Content(
                        new Border()
                            .CornerRadius(new CornerRadius(8))
                            .Background((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Surface)
                            .BorderBrush((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Border)
                            .BorderThickness(new Thickness(1))
                            .Shadow(shadow)
                            .Padding(new Thickness(20, 12))
                            .Content(
                                new Label("Aa")
                                    .FontSize(16).FontWeight(FontWeight.SemiBold)
                                    .Foreground((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.OnSurface)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                            )
                    ),
                new Label(label)
                    .FontSize(11)
                    .Foreground((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.OnDisabled)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
            );
    }

    private static VisualElement CreateColoredShadowDemo(string label, Color accent, Shadow shadow)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Border()
                    .CornerRadius(new CornerRadius(10))
                    .Background(GetTileBackground())
                    .Padding(new Thickness(24, 20))
                    .Content(
                        new Border()
                            .CornerRadius(new CornerRadius(8))
                            .Background((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Surface)
                            .BorderBrush(accent.WithAlpha(0.3f))
                            .BorderThickness(new Thickness(1))
                            .Shadow(shadow)
                            .Padding(new Thickness(20, 12))
                            .Content(
                                new Label("Aa")
                                    .FontSize(16).FontWeight(FontWeight.SemiBold)
                                    .Foreground(accent)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                            )
                    ),
                new Label(label)
                    .FontSize(11)
                    .Foreground((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.OnDisabled)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
            );
    }

    private static Color GetTileBackground() => (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.SurfaceHover;
}
