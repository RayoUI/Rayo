using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class LabelPage : UserControl
{
    public override VisualElement Build()
    {
        var counter = new Signal<int>(0);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Label", "Text display component with styling support"),

                // ── Basic Labels ────────────────────────────────────────────────
                Helper.CreateExampleSection("Basic Labels",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Default label text"),

                            new Label("Small text — 12px")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary),

                            new Label("Large text — 20px")
                                .FontSize(20)
                                .Foreground(ColorDefault.Primary),

                            new Label("Extra large heading — 28px")
                                .FontSize(28)
                                .Foreground(Color.White)
                        )
                ),

                // ── Font Weight ─────────────────────────────────────────────────
                Helper.CreateExampleSection("Font Weight",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label("Thin (100)")
                                .FontWeight(FontWeight.Thin)
                                .Foreground(Color.White),

                            new Label("Light (300)")
                                .FontWeight(FontWeight.Light)
                                .Foreground(Color.White),

                            new Label("Normal (400) — default")
                                .FontWeight(FontWeight.Normal)
                                .Foreground(Color.White),

                            new Label("Medium (500)")
                                .FontWeight(FontWeight.Medium)
                                .Foreground(Color.White),

                            new Label("SemiBold (600)")
                                .FontWeight(FontWeight.SemiBold)
                                .Foreground(Color.White),

                            new Label("Bold (700) — simulated via double-draw when no bold font is registered")
                                .FontWeight(FontWeight.Bold)
                                .Foreground(Color.White),

                            new Label("ExtraBold (800)")
                                .FontWeight(FontWeight.ExtraBold)
                                .Foreground(Color.White),

                            new Label("Black (900)")
                                .FontWeight(FontWeight.Black)
                                .Foreground(Color.White)
                        )
                ),

                // ── Font Style ──────────────────────────────────────────────────
                Helper.CreateExampleSection("Font Style",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label("Normal style — default")
                                .FontStyle(FontStyle.Normal)
                                .Foreground(Color.White),

                            new Label("Italic — requires \"{FontFamily}-Italic\" registered in AssetManager")
                                .FontStyle(FontStyle.Italic)
                                .Foreground(ColorDefault.Secondary),

                            new Label("Bold + Italic combined")
                                .FontWeight(FontWeight.Bold)
                                .FontStyle(FontStyle.Italic)
                                .Foreground(ColorDefault.Primary)
                        )
                ),

                // ── Text Decorations ────────────────────────────────────────────
                Helper.CreateExampleSection("Text Decorations",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Underline")
                                .TextDecorations(TextDecorations.Underline)
                                .Foreground(Color.White),

                            new Label("Strikethrough — useful for deleted / discounted items")
                                .TextDecorations(TextDecorations.Strikethrough)
                                .Foreground(ColorDefault.Danger),

                            new Label("Overline")
                                .TextDecorations(TextDecorations.Overline)
                                .Foreground(Color.White),

                            new Label("Underline + Strikethrough combined")
                                .TextDecorations(TextDecorations.Underline | TextDecorations.Strikethrough)
                                .Foreground(ColorDefault.Warning),

                            new Label("All decorations: Underline + Strikethrough + Overline")
                                .TextDecorations(TextDecorations.Underline | TextDecorations.Strikethrough | TextDecorations.Overline)
                                .Foreground(ColorDefault.Info)
                        )
                ),

                // ── Combined Styles ─────────────────────────────────────────────
                Helper.CreateExampleSection("Combined Styles",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Bold heading with underline")
                                .FontSize(18)
                                .FontWeight(FontWeight.Bold)
                                .TextDecorations(TextDecorations.Underline)
                                .Foreground(ColorDefault.Primary),

                            new Label("$99.99")
                                .FontSize(16)
                                .TextDecorations(TextDecorations.Strikethrough)
                                .Foreground(ColorDefault.Secondary),

                            new Label("$49.99")
                                .FontSize(20)
                                .FontWeight(FontWeight.Bold)
                                .Foreground(ColorDefault.Success),

                            new HStack()
                                .Spacing(16)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Label("NEW")
                                        .FontSize(11)
                                        .FontWeight(FontWeight.Bold)
                                        .Background(ColorDefault.Success)
                                        .Foreground(Color.White)
                                        .Padding(new Thickness(8, 3))
                                        .BorderRadius(4),

                                    new Label("SALE")
                                        .FontSize(11)
                                        .FontWeight(FontWeight.Bold)
                                        .Background(ColorDefault.Danger)
                                        .Foreground(Color.White)
                                        .Padding(new Thickness(8, 3))
                                        .BorderRadius(4),

                                    new Label("OUT OF STOCK")
                                        .FontSize(11)
                                        .FontWeight(FontWeight.SemiBold)
                                        .TextDecorations(TextDecorations.Strikethrough)
                                        .Background(new Color(60, 60, 70))
                                        .Foreground(ColorDefault.Secondary)
                                        .Padding(new Thickness(8, 3))
                                        .BorderRadius(4)
                                )
                        )
                ),

                // ── Line Height ─────────────────────────────────────────────────
                Helper.CreateExampleSection("Line Height",
                    new HStack()
                        .Spacing(20)
                        .Children(
                            CreateLineHeightDemo("Compact\n(1.1)", 1.1f),
                            CreateLineHeightDemo("Default\n(1.5)", 1.5f),
                            CreateLineHeightDemo("Relaxed\n(2.0)", 2.0f),
                            CreateLineHeightDemo("Spacious\n(2.5)", 2.5f)
                        )
                ),

                // ── FontFamily — Icon Fonts ─────────────────────────────────────
                Helper.CreateExampleSection("FontFamily — Icon Fonts",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new Label("Register icon fonts in AssetManager and reference them by alias")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160)),

                            new HStack()
                                .Spacing(20)
                                .Children(
                                    CreateFontFamilyIcon("\uEA1C", "Heart",    new Color(239, 68, 68)),
                                    CreateFontFamilyIcon("\uEA1D", "Star",     new Color(234, 179, 8)),
                                    CreateFontFamilyIcon("\uEA44", "Home",     new Color(59, 130, 246)),
                                    CreateFontFamilyIcon("\uEA54", "User",     new Color(168, 85, 247)),
                                    CreateFontFamilyIcon("\uEA5E", "Search",   new Color(34, 197, 94)),
                                    CreateFontFamilyIcon("\uEA5F", "Settings", new Color(6, 182, 212)),
                                    CreateFontFamilyIcon("\uEB9F", "Close",    new Color(6, 182, 212))
                                )
                        )
                ),

                // ── FontFamily — Icon Sizes ─────────────────────────────────────
                Helper.CreateExampleSection("FontFamily — Icon Sizes",
                    new HStack()
                        .Spacing(24)
                        .Alignment(Alignment.End)
                        .Children(
                            CreateFontFamilyIconSized("\uEB9F", 16, "16px"),
                            CreateFontFamilyIconSized("\uEA1D", 24, "24px"),
                            CreateFontFamilyIconSized("\uEA1D", 32, "32px"),
                            CreateFontFamilyIconSized("\uEA1D", 48, "48px"),
                            CreateFontFamilyIconSized("\uEB9F", 64, "64px")
                        )
                ),

                // ── Colors ──────────────────────────────────────────────────────
                Helper.CreateExampleSection("Colors",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label("Primary")   .Foreground(ColorDefault.Primary),
                            new Label("Success")   .Foreground(ColorDefault.Success),
                            new Label("Warning")   .Foreground(ColorDefault.Warning),
                            new Label("Danger")    .Foreground(ColorDefault.Danger),
                            new Label("Info")      .Foreground(ColorDefault.Info),
                            new Label("Secondary") .Foreground(ColorDefault.Secondary)
                        )
                ),

                // ── With Background ─────────────────────────────────────────────
                Helper.CreateExampleSection("With Background",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label("Label with background")
                                .Background(ColorDefault.Primary)
                                .Foreground(Color.White)
                                .Padding(new Thickness(8))
                                .BorderRadius(4),

                            new Label("Pill badge")
                                .Background(ColorDefault.Success)
                                .Foreground(Color.White)
                                .Padding(new Thickness(12, 4))
                                .BorderRadius(12),

                            new HStack()
                                .Spacing(8)
                                .Children(
                                    new Label("\uEA1C")
                                        .FontFamily("Lineicons").FontSize(16)
                                        .Background(new Color(239, 68, 68))
                                        .Foreground(Color.White)
                                        .Padding(new Thickness(8)).BorderRadius(20),
                                    new Label("\uEA1D")
                                        .FontFamily("Lineicons").FontSize(16)
                                        .Background(new Color(234, 179, 8))
                                        .Foreground(Color.White)
                                        .Padding(new Thickness(8)).BorderRadius(20),
                                    new Label("\uE86C")
                                        .FontFamily("Lineicons").FontSize(16)
                                        .Background(new Color(34, 197, 94))
                                        .Foreground(Color.White)
                                        .Padding(new Thickness(8)).BorderRadius(20)
                                )
                        )
                ),

                // ── Reactive Label ──────────────────────────────────────────────
                Helper.CreateExampleSection("Reactive Label",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(counter.Map(c => $"Counter: {c}"))
                                .FontSize(16)
                                .FontWeight(FontWeight.SemiBold)
                                .Foreground(ColorDefault.Info),

                            new HStack()
                                .Spacing(10)
                                .Children(
                                    new Button().Text("Increment").OnTapped(() => counter.Value++),
                                    new Button().Text("Decrement").OnTapped(() => counter.Value--),
                                    new Button().Text("Reset")
                                        .Background(ColorDefault.Danger)
                                        .OnTapped(() => counter.Value = 0)
                                )
                        )
                ),

                // ── Multiline Text ──────────────────────────────────────────────
                Helper.CreateExampleSection("Multiline Text",
                    new Label("Line 1\nLine 2\nLine 3\nMultiline text is supported using \\n")
                        .Width(400)
                        .Background(new Color(40, 40, 50))
                        .Padding(new Thickness(12))
                        .BorderRadius(6)
                )
            );
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static VisualElement CreateLineHeightDemo(string text, float lineHeight)
    {
        return new VStack()
            .Spacing(4)
            .Children(
                new Frame()
                    .Background(new Color(40, 43, 55))
                    .BorderRadius(6)
                    .Padding(new Thickness(12))
                    .Content(
                        new Label(text)
                            .FontSize(13)
                            .LineHeight(lineHeight)
                            .Foreground(Color.White)
                    ),
                new Label($"×{lineHeight:F1}")
                    .FontSize(10)
                    .Foreground(new Color(140, 145, 160))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
            );
    }

    private static VisualElement CreateFontFamilyIcon(string unicode, string label, Color color)
    {
        return new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(48, 48))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Label(unicode)
                            .FontFamily("Lineicons")
                            .FontSize(16)
                            .Foreground(color)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(10)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private static VisualElement CreateFontFamilyIconSized(string unicode, float size, string label)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(size + 16, size + 16))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Label(unicode)
                            .FontFamily("Lineicons")
                            .FontSize(size)
                            .Foreground(new Color(234, 179, 8))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
            );
    }
}
