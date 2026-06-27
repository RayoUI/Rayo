using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using System.Reflection;

namespace Gallery.Pages;

public class IconsPage : Component
{
    private const int Columns = 6;

    public override VisualElement Build()
    {
        var icons = GetIcons();
        var content = new VStack()
            .Spacing(14)
            .Padding(new Thickness(20))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                Helper.CreatePageHeader("Icons", "All vector icons available in Rayo.Controls.Icons"),
                new PaletteLabel($"{icons.Count} icons available", colors => colors.OnDisabled)
                    .FontSize(12)
            );

        foreach (var row in BuildRows(icons))
        {
            content.AddChild(row);
        }

        return content;
    }

    private static List<(string Name, IconData Icon)> GetIcons()
    {
        return typeof(Icons)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(IconData))
            .Select(property => (property.Name, Icon: (IconData)property.GetValue(null)!))
            .OrderBy(icon => icon.Name)
            .ToList();
    }

    private static IEnumerable<VisualElement> BuildRows(List<(string Name, IconData Icon)> icons)
    {
        for (var index = 0; index < icons.Count; index += Columns)
        {
            var row = new HStack()
                .Spacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Alignment(Alignment.Stretch);

            foreach (var icon in icons.Skip(index).Take(Columns))
            {
                row.AddChild(BuildIconCard(icon.Name, icon.Icon));
            }

            var missingCells = Columns - Math.Min(Columns, icons.Count - index);
            for (var i = 0; i < missingCells; i++)
            {
                row.AddChild(new Frame().HorizontalAlignment(HorizontalAlignment.Stretch));
            }

            yield return row;
        }
    }

    private static VisualElement BuildIconCard(string name, IconData icon)
    {
        return new PaletteFrame(colors => colors.Surface, colors => colors.Border)
            .BorderThickness(1)
            .BorderRadius(new CornerRadius(6))
            .Padding(new Thickness(10))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new PaletteFrame(colors => colors.SurfaceHover)
                            .Width(46)
                            .Height(46)
                            .BorderRadius(new CornerRadius(4))
                            .Content(
                                new PaletteIcon(icon, colors => colors.OnSurface)
                                    .Size(28)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                            ),
                        new PaletteLabel(name, colors => colors.OnSurface)
                            .FontSize(10)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }
}
