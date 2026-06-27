using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Gallery.Pages;

public class SideBarPage : Component
{
    private readonly Signal<string> _selectedItem;
    private readonly Signal<bool> _isCollapsed;

    public SideBarPage()
    {
        _selectedItem = UseSignal("Home");
        _isCollapsed = UseSignal(false);
    }

    public override VisualElement Build()
    {
        var contentLabel = new PaletteLabel("Select an item from the sidebar", colors => colors.OnDisabled)
            .FontSize(16);

        UseSubscription(_selectedItem, item =>
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
                            new PaletteFrame(colors => colors.Background)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .VerticalAlignment(VerticalAlignment.Stretch)
                                .Padding(new Thickness(20))
                                .Content(
                                    new VStack()
                                        .Spacing(12)
                                        .Children(
                                            new PaletteLabel("Content Area", colors => colors.OnBackground)
                                                .FontSize(18),
                                            contentLabel,
                                            new PaletteLabel("Click the < button in the sidebar to collapse it.", colors => colors.OnDisabled)
                                                .FontSize(13)
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
                                .SelectedKey("Analytics")
                                .Header(
                                    new Frame()
                                        .Padding(new Thickness(16, 20, 16, 20))
                                        .Content(
                                            new PaletteLabel("ACME Inc", colors => colors.OnBackground)
                                                .FontSize(18)
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
                                                .Variant(ButtonVariant.Danger)
                                                .BorderThickness(0)
                                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                .Padding(new Thickness(12, 8, 12, 8))
                                        )
                                ),

                            new PaletteFrame(colors => colors.SurfaceHover)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .VerticalAlignment(VerticalAlignment.Stretch)
                                .Padding(new Thickness(20))
                                .Content(
                                    new PaletteLabel("Dashboard Content", colors => colors.OnSurface)
                                        .FontSize(16)
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
                    .Foreground(GalleryPalette.Primary),
                new Label(text)
                    .FontSize(14)
                    .Foreground(GalleryPalette.Muted)
            );
    }
}
