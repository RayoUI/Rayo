using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Shadow = Rayo.Controls.Shadow;

namespace Gallery.Pages;

public class CardPage : Component
{
    public override VisualElement Build()
    {
        // Note: GalleryBuilder already wraps pages in a ScrollView
        return new VStack()
            .Spacing(24)
            .Padding(new Thickness(20))
            .Children(
                        Helper.CreatePageHeader("Card", "Content container with header, content, and footer sections"),

                        // Basic Card
                        Helper.CreateExampleSection("Basic Card",
                            new Card()
                                .Content(
                                    new Label("This is a basic card with just content. Cards provide a visual container for related information.")
                                        .Foreground(GalleryPalette.OnSurface)
                                )
                                .Width(400)
                        ),

                        // Card with Header
                        Helper.CreateExampleSection("Card with Header",
                            new Card()
                                .Header(
                                    new Label("Card Title")
                                        .FontSize(18)
                                        .Foreground(GalleryPalette.OnSurface)
                                )
                                .Content(
                                    new Label("Cards can have a header section with a different background color to visually separate the title from the content.")
                                        .Foreground(GalleryPalette.OnSurface)
                                )
                                .Width(400)
                        ),

                        // Card with Header and Footer
                        Helper.CreateExampleSection("Card with Header and Footer",
                            new Card()
                                .Width(400)
                                .Header(
                                    new Label("User Profile")
                                        .FontSize(18)
                                        .Foreground(GalleryPalette.OnSurface)
                                )
                                .Content(
                                    new VStack()
                                        .Spacing(8)
                                        .Children(
                                            new Label("Name: John Doe").Foreground(GalleryPalette.OnSurface),
                                            new Label("Email: john@example.com").Foreground(GalleryPalette.OnSurface),
                                            new Label("Role: Administrator").Foreground(GalleryPalette.OnSurface)
                                        )
                                )
                                .Footer(
                                    new HStack()
                                        .Spacing(10)
                                        .Children(
                                            new Button()
                                                .Text("Edit")
                                                .Background(ColorDefault.Primary),
                                            new Button()
                                                .Text("Delete")
                                                .Background(ColorDefault.Danger)
                                        )
                                )
                        ),

                        // Card Styles
                        Helper.CreateExampleSection("Card Styles",
                            new HStack()
                                .Spacing(16)
                                .Children(
                                    // Default with shadow
                                    new Card()
                                        .Width(200)
                                        .Header(
                                            new Label("With Shadow")
                                                .FontSize(14)
                                                .Foreground(GalleryPalette.OnSurface)
                                        )
                                        .Content(
                                            new Label("Default card with shadow enabled")
                                                .Foreground(GalleryPalette.OnSurface)
                                        ),

                                    // No shadow
                                    new Card()
                                        .Width(200)
                                        .Shadow(Shadow.None)
                                        .Header(
                                            new Label("No Shadow")
                                                .FontSize(14)
                                                .Foreground(GalleryPalette.OnSurface)
                                        )
                                        .Content(
                                            new Label("Card with shadow disabled")
                                                .Foreground(GalleryPalette.OnSurface)
                                        ),

                                    // Custom border
                                    new Card()
                                        .Width(200)
                                        .Shadow(Shadow.None)
                                        .BorderBrush(ColorDefault.Primary)
                                        .BorderThickness(2)
                                        .Header(
                                            new Label("Custom Border")
                                                .FontSize(14)
                                                .Foreground(GalleryPalette.OnSurface)
                                        )
                                        .Content(
                                            new Label("Card with colored border")
                                                .Foreground(GalleryPalette.OnSurface)
                                        )
                                )
                        ),

                        // Custom Colors
                        Helper.CreateExampleSection("Custom Colors",
                            new HStack()
                                .Spacing(16)
                                .Children(
                                    new Card()
                                        .Width(200)
                                        .Background(new Color(240, 249, 255))
                                        .HeaderBackground(new Color(59, 130, 246))
                                        .BorderBrush(new Color(59, 130, 246))
                                        .Header(
                                            new Label("Info Card")
                                                .FontSize(14)
                                                .Foreground(Color.White)
                                        )
                                        .Content(
                                            new Label("Light blue theme")
                                                .Foreground(new Color(30, 64, 175))
                                        ),

                                    new Card()
                                        .Width(200)
                                        .Background(new Color(240, 253, 244))
                                        .HeaderBackground(new Color(34, 197, 94))
                                        .BorderBrush(new Color(34, 197, 94))
                                        .Header(
                                            new Label("Success Card")
                                                .FontSize(14)
                                                .Foreground(Color.White)
                                        )
                                        .Content(
                                            new Label("Green success theme")
                                                .Foreground(new Color(22, 101, 52))
                                        ),

                                    new Card()
                                        .Width(200)
                                        .Background(new Color(254, 242, 242))
                                        .HeaderBackground(new Color(239, 68, 68))
                                        .BorderBrush(new Color(239, 68, 68))
                                        .Header(
                                            new Label("Error Card")
                                                .FontSize(14)
                                                .Foreground(Color.White)
                                        )
                                        .Content(
                                            new Label("Red error theme")
                                                .Foreground(new Color(153, 27, 27))
                                        )
                                )
                        ),

                        // Product Card Example
                        Helper.CreateExampleSection("Product Card Example",
                            new Card()
                                .Width(300)
                                .Shadow(new Shadow(new Color(0, 0, 0, 50), 4))
                                .CornerRadius(12)
                                .Content(
                                    new VStack()
                                        .Spacing(12)
                                        .Children(
                                            // Product image placeholder
                                            new Frame()
                                                .Background(GalleryPalette.SurfacePressed)
                                                .Height(150)
                                                .BorderRadius(8)
                                                .Content(
                                                    new Label("Product Image")
                                                        .Foreground(GalleryPalette.Muted)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .VerticalAlignment(VerticalAlignment.Center)
                                                ),

                                            new Label("Premium Headphones")
                                                .FontSize(18)
                                                .Foreground(GalleryPalette.OnSurface),

                                            new Label("High-quality wireless headphones with noise cancellation")
                                                .Foreground(GalleryPalette.Muted),

                                            new HStack()
                                                .Spacing(8)
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .Children(
                                                    new Label("$299.99")
                                                        .FontSize(20)
                                                        .Foreground(ColorDefault.Primary),
                                                    new Label("$349.99")
                                                        .FontSize(14)
                                                        .Foreground(GalleryPalette.Muted)
                                                ),

                                            new Button()
                                                .Text("Add to Cart")
                                                .Background(ColorDefault.Primary)
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        )
                                )
                        )
            );
    }
}
