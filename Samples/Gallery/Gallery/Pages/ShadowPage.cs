using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Shadow = Rayo.Controls.Shadow;

namespace Gallery.Pages;

public class ShadowPage : UserControl
{
    // Lighter surface used as the background tile so dark shadows have contrast
    private static readonly Color SurfaceBg = new Color(62, 65, 80);

    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Shadow", "Drop shadow effects for elevated UI elements"),

                Helper.CreateInfoCard(
                    "How shadows work",
                    "Shadows are rendered as semi-transparent layers behind the element. " +
                    "They require a lighter background to be visible — each demo card below sits " +
                    "on a lighter tile for contrast."
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
                            CreateShadowDemo("Strong",  Shadow.Strong)
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
                                    .Background(new Color(45, 48, 60))
                                    .BorderBrush(new Color(70, 73, 87))
                                    .BorderThickness(new Thickness(1))
                                    .Shadow(Shadow.Strong)
                                    .Padding(new Thickness(16))
                                    .Content(
                                        new VStack()
                                            .Spacing(4)
                                            .Children(
                                                new Label("Elevated Card")
                                                    .FontSize(14).FontWeight(FontWeight.SemiBold)
                                                    .Foreground(Color.White),
                                                new Label("Shadow.Strong preset")
                                                    .FontSize(12)
                                                    .Foreground(ColorDefault.Secondary)
                                            )
                                    )
                            ),

                            // Glow badge
                            WrapInTile(
                                new Border()
                                    .CornerRadius(new CornerRadius(8))
                                    .Background(new Color(35, 38, 48))
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
                                                    .Foreground(Color.White)
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
            .Background(SurfaceBg)
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
                    .Background(SurfaceBg)
                    .Padding(new Thickness(24, 20))
                    .Content(
                        new Border()
                            .CornerRadius(new CornerRadius(8))
                            .Background(new Color(35, 38, 50))
                            .BorderBrush(new Color(60, 63, 78))
                            .BorderThickness(new Thickness(1))
                            .Shadow(shadow)
                            .Padding(new Thickness(20, 12))
                            .Content(
                                new Label("Aa")
                                    .FontSize(16).FontWeight(FontWeight.SemiBold)
                                    .Foreground(Color.White)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                            )
                    ),
                new Label(label)
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
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
                    .Background(SurfaceBg)
                    .Padding(new Thickness(24, 20))
                    .Content(
                        new Border()
                            .CornerRadius(new CornerRadius(8))
                            .Background(new Color(28, 30, 42))
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
                    .Foreground(new Color(140, 145, 160))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
            );
    }
}
