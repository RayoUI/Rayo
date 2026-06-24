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
                new Label($"{icons.Count} icons available")
                    .FontSize(12)
                    .Foreground(new Color(150, 160, 175))
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
        return new Frame()
            .Background(new Color(29, 32, 38))
            .BorderColor(new Color(48, 54, 64))
            .BorderWidth(1)
            .BorderRadius(new CornerRadius(6))
            .Padding(new Thickness(10))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new Frame()
                            .Width(46)
                            .Height(46)
                            .Background(new Color(38, 43, 52))
                            .BorderRadius(new CornerRadius(4))
                            .Content(
                                new Icon(icon)
                                    .Size(28)
                                    .Color(new Color(230, 235, 245))
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                            ),
                        new Label(name)
                            .FontSize(10)
                            .Foreground(new Color(205, 212, 224))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }
}
