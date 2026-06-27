using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class TabControlPage : Component
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
                                            new PaletteLabel("Home Tab Content", colors => colors.OnSurface)
                                                .FontSize(18),
                                            new PaletteLabel("This is the home tab. You can add any content here.", colors => colors.OnDisabled)
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
                                            new PaletteLabel("Profile Information", colors => colors.OnSurface)
                                                .FontSize(18),
                                            new PaletteLabel("Name: John Doe", colors => colors.Info),
                                            new PaletteLabel("Email: john@example.com", colors => colors.Info),
                                            new PaletteLabel("Role: Developer", colors => colors.Info)
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
                                            new PaletteLabel("Settings", colors => colors.OnSurface)
                                                .FontSize(18),
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
                                    new PaletteLabel("Dashboard content goes here", colors => colors.OnSurface)
                                )
                        )
                        .AddTab("Analytics",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new PaletteLabel("Analytics charts and graphs", colors => colors.OnSurface)
                                )
                        )
                        .AddTab("Reports",
                            new Frame()
                                .Padding(new Thickness(20))
                                .Content(
                                    new PaletteLabel("Reports and data exports", colors => colors.OnSurface)
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
                new PaletteLabel(title, colors => colors.OnDisabled)
                    .FontSize(14),
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
                        new PaletteLabel(heading, colors => colors.OnSurface)
                            .FontSize(16),
                        new PaletteLabel(description, colors => colors.OnDisabled)
                    )
            );
    }
}
