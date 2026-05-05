using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class TabControlPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("TabControl", "Tabbed navigation container"),

                Helper.CreateExampleSection("Basic Tabs",
                    new TabControl()
                        .Width(500)
                        .Height(300)
                        .AddTab("Home",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new VStack()
                                        .Spacing(10)
                                        .Children(
                                            new Label("Home Tab Content")
                                                .FontSize(18)
                                                .Foreground(Color.White),
                                            new Label("This is the home tab. You can add any content here.")
                                                .Foreground(ColorDefault.Secondary)
                                        )
                                )
                        )
                        .AddTab("Profile",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new VStack()
                                        .Spacing(10)
                                        .Children(
                                            new Label("Profile Information")
                                                .FontSize(18)
                                                .Foreground(Color.White),
                                            new Label("Name: John Doe")
                                                .Foreground(ColorDefault.Info),
                                            new Label("Email: john@example.com")
                                                .Foreground(ColorDefault.Info),
                                            new Label("Role: Developer")
                                                .Foreground(ColorDefault.Info)
                                        )
                                )
                        )
                        .AddTab("Settings",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new VStack()
                                        .Spacing(10)
                                        .Children(
                                            new Label("Settings")
                                                .FontSize(18)
                                                .Foreground(Color.White),
                                            new Checkbox("Enable notifications"),
                                            new Checkbox("Dark mode"),
                                            new Checkbox("Auto-save")
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Vertical Tabs",
                    CreateVerticalTabControl()
                ),

                Helper.CreateExampleSection("Tab Positions",
                    CreateTabPositionsDemo()
                )
            );
    }

    private TabControl CreateVerticalTabControl()
    {
        var tabControl = new TabControl();
        tabControl.Position = TabPosition.Left;
        tabControl.VerticalTabWidth = 220;
        tabControl.VerticalTabHeight = 80;

        return tabControl
            .Width(300)
            .Height(500)
            .AddTab("Dashboard",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new Label("Dashboard content goes here")
                                        .Foreground(Color.White)
                                )
                        )
                        .AddTab("Analytics",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new Label("Analytics charts and graphs")
                                        .Foreground(Color.White)
                                )
                        )
                        .AddTab("Reports",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new Label("Reports and data exports")
                                        .Foreground(Color.White)
                                )
                        );
    }

    private VisualElement CreateTabPositionsDemo()
    {
        return new HStack()
            .Spacing(20)
            .Children(
                new VStack()
                    .Spacing(16)
                    .Children(
                        CreateTabPositionSample("Top Tabs", TabPosition.Top),
                        CreateTabPositionSample("Bottom Tabs", TabPosition.Bottom)
                    ),
                new VStack()
                    .Spacing(16)
                    .Children(
                        CreateTabPositionSample("Left Tabs", TabPosition.Left),
                        CreateTabPositionSample("Right Tabs", TabPosition.Right)
                    )
            );
    }

    private VisualElement CreateTabPositionSample(string title, TabPosition position)
    {
        return new VStack()
            .Spacing(8)
            .Children(
                new Label(title)
                    .FontSize(14)
                    .Foreground(ColorDefault.Secondary),
                CreatePositionedTabControl(position)
            );
    }

    private TabControl CreatePositionedTabControl(TabPosition position)
    {
        bool isVertical = position == TabPosition.Left || position == TabPosition.Right;

        var control = new TabControl()
            .Width(isVertical ? 220 : 360)
            .Height(isVertical ? 360 : 220)
            .Position(position)
            .AddTab("Overview", CreateTabContent("Overview", "General information"))
            .AddTab("Tasks", CreateTabContent("Tasks", "Pending work items"))
            .AddTab("History", CreateTabContent("History", "Recent activity"));

        if (isVertical)
        {
            control.VerticalTabWidth = 160;
            control.VerticalTabHeight = 60;
        }
        else
        {
            control.TabWidth = 140;
            control.TabHeight = 36;
        }

        return control;
    }

    private VisualElement CreateTabContent(string heading, string description)
    {
        return new Frame()
            .Padding(new Thickness(16))
            .Content(
                new VStack()
                    .Spacing(6)
                    .Children(
                        new Label(heading)
                            .FontSize(16)
                            .Foreground(Color.White),
                        new Label(description)
                            .Foreground(ColorDefault.Secondary)
                    )
            );
    }
}
