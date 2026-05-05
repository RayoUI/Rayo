using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Gallery.Pages;

public class SideBarPage : UserControl
{
    private Signal<string> _selectedItem = new("Home");
    private Signal<bool> _isCollapsed = new(false);

    public override VisualElement Build()
    {
        var contentLabel = new Label("Select an item from the sidebar")
            .FontSize(16)
            .Foreground(new Color(180, 185, 195));

        _selectedItem.Subscribe(item =>
        {
            contentLabel.Text($"Selected: {item}");
        });

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("SideBar", "Fixed navigation sidebar with collapsible support"),

                Helper.CreateExampleSection("Interactive SideBar",
                    new HStack()
                        .Spacing(0)
                        .Height(400)
                        .Children(
                            // SideBar
                            new SideBar()
                                .ExpandedWidth(200)
                                .CollapsedWidth(56)
                                .IsCollapsed(_isCollapsed.Value)
                                .OnCollapsedChanged(collapsed => _isCollapsed.Value = collapsed)
                                .SelectedKey(_selectedItem.Value)
                                .OnSelectionChanged(key => _selectedItem.Value = key)
                                .AddCollapseToggle()
                                .AddItem("Home", "H")
                                .AddItem("Dashboard", "D")
                                .AddItem("Projects", "P")
                                .AddItem("Tasks", "T")
                                .AddItem("Calendar", "C")
                                .AddItem("Messages", "M")
                                .AddItem("Settings", "S"),

                            // Content area
                            new Frame()
                                .Background(new Color(20, 22, 28))
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .VerticalAlignment(VerticalAlignment.Stretch)
                                .Padding(new Thickness(20))
                                .Content(
                                    new VStack()
                                        .Spacing(12)
                                        .Children(
                                            new Label("Content Area")
                                                .FontSize(18)
                                                .Foreground(Color.White),
                                            contentLabel,
                                            new Label("Click the < button in the sidebar to collapse it.")
                                                .FontSize(13)
                                                .Foreground(new Color(120, 125, 140))
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Custom Styled SideBar",
                    new HStack()
                        .Spacing(0)
                        .Height(300)
                        .Children(
                            new SideBar()
                                .ExpandedWidth(220)
                                .Background(new Color(15, 23, 42))
                                .ItemColors(
                                    Color.Transparent,
                                    new Color(30, 41, 59),
                                    new Color(14, 165, 233)
                                )
                                .TextColors(
                                    new Color(148, 163, 184),
                                    Color.White
                                )
                                .SelectedKey("Analytics")
                                .Header(
                                    new Frame()
                                        .Padding(new Thickness(16, 20, 16, 20))
                                        .Content(
                                            new Label("ACME Inc")
                                                .FontSize(18)
                                                .Foreground(Color.White)
                                        )
                                )
                                .AddItem("Overview", "O")
                                .AddItem("Analytics", "A")
                                .AddItem("Reports", "R")
                                .AddItem("Users", "U")
                                .Footer(
                                    new Frame()
                                        .Padding(new Thickness(12))
                                        .Content(
                                            new Button()
                                                .Text("Logout")
                                                .Background(new Color(239, 68, 68))
                                                .HoverBackground(new Color(255, 88, 88))
                                                .TextColor(Color.White)
                                                .BorderWidth(0)
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .Padding(new Thickness(12, 8, 12, 8))
                                        )
                                ),

                            new Frame()
                                .Background(new Color(30, 41, 59))
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .VerticalAlignment(VerticalAlignment.Stretch)
                                .Padding(new Thickness(20))
                                .Content(
                                    new Label("Dashboard Content")
                                        .FontSize(16)
                                        .Foreground(Color.White)
                                )
                        )
                ),

                Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Collapsible with smooth transition"),
                            CreateFeatureItem("Custom header and footer support"),
                            CreateFeatureItem("Icon-only mode when collapsed"),
                            CreateFeatureItem("Selection state with events"),
                            CreateFeatureItem("Fully customizable colors and sizing"),
                            CreateFeatureItem("Scrollable item list for many items")
                        )
                )
            );
    }

    private VisualElement CreateFeatureItem(string text)
    {
        return new HStack()
            .Spacing(8)
            .Children(
                new Label("*")
                    .FontSize(14)
                    .Foreground(new Color(59, 130, 246)),
                new Label(text)
                    .FontSize(14)
                    .Foreground(new Color(180, 185, 195))
            );
    }
}
