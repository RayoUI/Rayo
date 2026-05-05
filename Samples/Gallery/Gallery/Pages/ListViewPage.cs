using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class ListViewPage : UserControl
{
    public override VisualElement Build()
    {
        // List of fruits to display in the simple list example.
        var fruits = new List<string>
        {
            "🍎 Apple", "🍌 Banana", "🍒 Cherry", "🌴 Date", "🫐 Elderberry",
            "🍇 fig", "🍇 Grape", "🍈 Honeydew", "🥝 Kiwi", "🍋 Lemon"
        };

        // List of users with their roles to display in the user list example.
        var users = new List<(string Name, string Role)>
        {
            ("Alice Johnson", "Administrator"),
            ("Bob Smith", "Developer"),
            ("Carol White", "Designer"),
            ("David Brown", "Manager"),
            ("Eve Davis", "Developer"),
            ("Alice Johnson", "Administrator"),
            ("Bob Smith", "Developer"),
            ("Carol White", "Designer"),
            ("David Brown", "Manager"),
            ("Eve Davis", "Developer")
        };

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ListView", "Scrollable list of items"),

                Helper.CreateExampleSection("Simple List",
                    new ListView<string>()
                        .Width(300)
                        .Height(200)
                        .Items(fruits)
                ),

                Helper.CreateExampleSection("User List with Details",
                    new ListView<(string Name, string Role)>()
                        .Width(400)
                        .Height(250)
                        .Items(users)
                        .WithDisplayFunc(user => $"{user.Name} - {user.Role}")
                ),

                Helper.CreateExampleSection("Selectable List",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Click an item to select:")
                                .Foreground(ColorDefault.Secondary),

                            CreateSelectableList()
                        )
                )
            );
    }

    // Creates a selectable list with custom item selection and hover effects.
    private ListView<string> CreateSelectableList()
    {
        var listView = new ListView<string>();
        listView.ItemSelectedBackground = ColorDefault.Primary;
        listView.ItemHoverBackground = new Color(60, 60, 70);
        listView.ItemSelected += (item, index) =>
        {
            System.Console.WriteLine($"Selected: {item}");
        };

        return listView
            .Width(300)
            .Height(180)
            .Items(new List<string> {
                "✅ Option 1",
                "⭐ Option 2",
                "🔔 Option 3",
                "⚡ Option 4",
                "❌ Option 5",
                "✅ Option 1",
                "⭐ Option 2",
                "🔔 Option 3",
                "⚡ Option 4",
                "❌ Option 5"
            });
    }
}
