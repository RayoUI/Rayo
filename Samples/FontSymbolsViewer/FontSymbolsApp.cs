using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace FontSymbolsViewer;

public sealed class FontSymbolsApp : UserControl
{
    public const string FontAlias = "Lineicons";

    private const string FontAssetPath = "Assets/Fonts/Lineicons.ttf";
    private const int Columns = 8;

    public override VisualElement Build()
    {
        var glyphs = TtfCodepointReader.ReadCodepoints(FontAssetPath)
            .Select(codePoint => new FontGlyph(codePoint, char.ConvertFromUtf32(codePoint)))
            .ToList();

        var content = new VStack()
            .Spacing(16)
            .Padding(new Thickness(24))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                BuildHeader(glyphs.Count),
                BuildPreviewStrip(glyphs)
            );

        foreach (var row in BuildRows(glyphs))
        {
            content.AddChild(row);
        }

        return new VStack()
            .Background(new Color(17, 19, 24))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                new ScrollView(content)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .ScrollbarBackground(new Color(31, 35, 43))
                    .ScrollbarThumb(new Color(112, 122, 138))
            );
    }

    private static VisualElement BuildHeader(int glyphCount)
    {
        return new VStack()
            .Spacing(8)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                new Label("Lineicons.ttf")
                    .FontSize(34)
                    .FontWeight(FontWeight.Bold)
                    .Foreground(new Color(245, 247, 251)),
                new Label($"{glyphCount} simbolos detectados desde la tabla cmap de la fuente")
                    .FontSize(14)
                    .Foreground(new Color(166, 176, 192)),
                new Label("Cada celda muestra el glifo renderizado con FontFamily(\"Lineicons\") y su codepoint Unicode.")
                    .FontSize(12)
                    .Foreground(new Color(120, 132, 150))
            );
    }

    private static VisualElement BuildPreviewStrip(IReadOnlyList<FontGlyph> glyphs)
    {
        var strip = new HStack()
            .Spacing(10)
            .Height(76)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Alignment(Alignment.Center);

        foreach (var glyph in glyphs.Take(12))
        {
            strip.AddChild(
                new Label(glyph.Text)
                    .FontFamily(FontAlias)
                    .FontSize(38)
                    .Width(54)
                    .Height(54)
                    .Background(new Color(28, 32, 39))
                    .BorderRadius(new CornerRadius(6))
                    .Foreground(new Color(129, 218, 200))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .TextVerticalAlignment(VerticalAlignment.Center)
            );
        }

        return strip;
    }

    private static IEnumerable<VisualElement> BuildRows(IReadOnlyList<FontGlyph> glyphs)
    {
        for (var index = 0; index < glyphs.Count; index += Columns)
        {
            var row = new HStack()
                .Spacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Alignment(Alignment.Stretch);

            foreach (var glyph in glyphs.Skip(index).Take(Columns))
            {
                row.AddChild(BuildGlyphCard(glyph));
            }

            var missingCells = Columns - Math.Min(Columns, glyphs.Count - index);
            for (var i = 0; i < missingCells; i++)
            {
                row.AddChild(new Frame().Width(104).Height(112).Background(Color.Transparent));
            }

            yield return row;
        }
    }

    private static VisualElement BuildGlyphCard(FontGlyph glyph)
    {
        return new Frame()
            .Width(104)
            .Height(112)
            .Background(new Color(25, 28, 35))
            .BorderColor(new Color(47, 54, 67))
            .BorderWidth(1)
            .BorderRadius(new CornerRadius(6))
            .Padding(new Thickness(10))
            .Content(
                new VStack()
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new Label(glyph.Text)
                            .FontFamily(FontAlias)
                            .FontSize(38)
                            .Width(72)
                            .Height(58)
                            .Background(new Color(34, 39, 48))
                            .BorderRadius(new CornerRadius(4))
                            .Foreground(new Color(236, 241, 248))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .TextVerticalAlignment(VerticalAlignment.Center),
                        new Label($"U+{glyph.CodePoint:X4}")
                            .FontSize(11)
                            .Foreground(new Color(176, 185, 198))
                            .TextHorizontalAlignment(HorizontalAlignment.Center),
                        new Label($"dec {glyph.CodePoint}")
                            .FontSize(9)
                            .Foreground(new Color(105, 116, 132))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }

    private readonly record struct FontGlyph(int CodePoint, string Text);
}
