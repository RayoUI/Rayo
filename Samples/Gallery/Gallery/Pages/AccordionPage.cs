using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gallery.Pages;

public class AccordionPage : UserControl
{
    private Label? _statusLabel;

    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Accordion and Expander Controls",
                    "Demonstration of Accordion and Expander controls with various configurations."),
                CreateAccordionExample(),
                CreateExpandersExample(),
                CreateEventsExample(),
                CreateStyledExample()
            );
    }

    /// <summary>
    /// Example 1: Basic Accordion with SingleExpand
    /// </summary>
    private VisualElement CreateAccordionExample()
    {
        return Helper.CreateExampleSection("Accordion with Single Expand",
            new Accordion()
                .SingleExpand(true)
                .AddItem("🎨 Section 1: Design",
                    new VStack()
                        .Spacing(8)
                        .Children(
                            new Label("Content for design section."),
                            new Label("Only one section can be expanded at a time."),
                            new Label("Click headers to expand/collapse.")
                        ),
                    startExpanded: true)
                .AddItem("⚙️ Section 2: Settings",
                    new VStack()
                        .Spacing(8)
                        .Children(
                            new Label("Configuration options here."),
                            new Label("Chevron icons automatically change on expand/collapse.")
                        ))
                .AddItem("📚 Section 3: Documentation",
                    new VStack()
                        .Spacing(8)
                        .Children(
                            new Label("Great for FAQs and documentation."),
                            new Label("Smooth animations included.")
                        ))
                .Width(500)
        );
    }

    /// <summary>
    /// Example 2: Individual Expanders
    /// </summary>
    private VisualElement CreateExpandersExample()
    {
        return Helper.CreateExampleSection("Individual Expandable Sections",
            new VStack()
                .Spacing(10)
                .Width(500)
                .Children(
                    new Expander("👤 User Settings")
                        .Content(
                            new VStack()
                                .Spacing(8)
                                .Children(
                                    new Label("✓ Profile settings")
                                        .Foreground(new Color(100, 100, 100)),
                                    new Label("✓ Notification preferences")
                                        .Foreground(new Color(100, 100, 100)),
                                    new Label("✓ Privacy options")
                                        .Foreground(new Color(100, 100, 100))
                                )
                        )
                        .IsExpanded(true),

                    new Expander("🔧 Advanced Options")
                        .Content(
                            new VStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Advanced configuration options")
                                        .Foreground(new Color(100, 100, 100)),
                                    new Label("Developer tools")
                                        .Foreground(new Color(100, 100, 100))
                                )
                        ),

                    new Expander("📊 Statistics")
                        .Content(
                            new VStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Usage statistics")
                                        .Foreground(new Color(100, 100, 100)),
                                    new Label("Performance metrics")
                                        .Foreground(new Color(100, 100, 100))
                                )
                        )
                )
        );
    }

    /// <summary>
    /// Example 3: Expander with Events
    /// </summary>
    private VisualElement CreateEventsExample()
    {
        _statusLabel = new Label()
            .Text("Status: Click an expander to see events")
            .Foreground(new Color(100, 100, 100))
            .FontSize(14);

        var expander1 = new Expander("📝 Expander with Events")
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new Label("This expander fires events when expanded/collapsed"),
                        new Label("Check the status label below!")
                    )
            )
            .OnExpandedChanged(isExpanded =>
            {
                string state = isExpanded ? "expanded ▼" : "collapsed ▶";
                _statusLabel?.Text($"Status: Expander was {state}");
            });

        var expander2 = new Expander("🎯 Another Expander")
            .Content(new Label("Click me to trigger events too!"))
            .OnExpandedChanged(isExpanded =>
            {
                string state = isExpanded ? "opened" : "closed";
                _statusLabel?.Text($"Status: Second expander {state}");
            });

        return Helper.CreateExampleSection("Expanders with Events",
            new VStack()
                .Spacing(10)
                .Width(500)
                .Children(
                    new Label()
                        .Text("Try expanding/collapsing to see events in action")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    expander1,
                    expander2,
                    _statusLabel
                )
        );
    }

    /// <summary>
    /// Example 4: Styled Expanders
    /// </summary>
    private VisualElement CreateStyledExample()
    {
        return Helper.CreateExampleSection("Styled Expanders",
            new VStack()
                .Spacing(10)
                .Width(500)
                .Children(
                    new Expander("🔵 Blue Theme")
                        .HeaderBackground(new Color(219, 234, 254))
                        .HeaderHoverColor(new Color(191, 219, 254))
                        .ContentBackground(new Color(239, 246, 255))
                        .TextColor(new Color(30, 64, 175))
                        .Content(
                            new Label("Custom blue styling with colored backgrounds")
                                .Foreground(new Color(30, 64, 175))
                        )
                        .IsExpanded(true),

                    new Expander("🟢 Green Theme")
                        .HeaderBackground(new Color(220, 252, 231))
                        .HeaderHoverColor(new Color(187, 247, 208))
                        .ContentBackground(new Color(240, 253, 244))
                        .TextColor(new Color(21, 128, 61))
                        .Content(
                            new Label("Custom green styling for success states")
                                .Foreground(new Color(21, 128, 61))
                        ),

                    new Expander("🟣 Purple Theme")
                        .HeaderBackground(new Color(233, 213, 255))
                        .HeaderHoverColor(new Color(216, 180, 254))
                        .ContentBackground(new Color(250, 245, 255))
                        .TextColor(new Color(107, 33, 168))
                        .Content(
                            new Label("Custom purple styling for premium features")
                                .Foreground(new Color(107, 33, 168))
                        )
                )
        );
    }
}
