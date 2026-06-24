using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class LinkPage : Component
{
    private Label? _statusLabel;

    public override VisualElement Build()
    {
        _statusLabel = new Label("Status: no link activated")
            .Foreground(new Color(90, 90, 90))
            .FontSize(13);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Link", "Interactive text links for navigation and actions"),

                Helper.CreateExampleSection("Basic Links",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Link("Documentation", "https://example.com/docs"),
                            new Link("Release notes", "https://example.com/releases")
                                .UnderlineOnHoverOnly(true),
                            new Link("Visited link")
                                .IsVisited(true)
                        )
                ),

                Helper.CreateExampleSection("Custom Actions",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new HStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Label("Need help?")
                                        .Foreground(new Color(70, 70, 70)),
                                    new Link("Contact support")
                                        .OpenUrlOnTap(false)
                                        .OnActivated(_ => _statusLabel?.Text("Status: support link activated"))
                                ),
                            _statusLabel
                        )
                ),

                Helper.CreateExampleSection("Styled Links",
                    new HStack()
                        .Spacing(24)
                        .Children(
                            new Link("Success link")
                                .NormalColor(new Color(34, 197, 94))
                                .HoverColor(new Color(21, 128, 61))
                                .PressedColor(new Color(22, 101, 52)),

                            new Link("Danger link")
                                .NormalColor(new Color(239, 68, 68))
                                .HoverColor(new Color(185, 28, 28))
                                .PressedColor(new Color(153, 27, 27)),

                            new Link("Large centered")
                                .FontSize(18)
                                .Width(160)
                                .TextHorizontalAlignment(HorizontalAlignment.Center)
                        )
                )
            );
    }
}
