using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class ButtonGroupPage : Component
{
    private Label? _selectionLabel;

    public override VisualElement Build()
    {
        _selectionLabel = new Label("Selected: Day")
            .Foreground(GalleryPalette.Muted)
            .FontSize(13);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ButtonGroup", "Adjacent selectable buttons for filters and compact choices"),

                Helper.CreateExampleSection("Basic Selection",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new ButtonGroup()

                                .AddItems("Day", "Week", "Month", "Year")
                                .SelectedIndex(0)
                                .OnSelectedItemChanged(item => _selectionLabel?.Text($"Selected: {item ?? "none"}")),
                            _selectionLabel
                        )
                ),

                Helper.CreateExampleSection("Vertical Group",
                    new ButtonGroup()
                        .Orientation(Rayo.Controls.Orientation.Vertical)
                        .AddItems("Small", "Medium", "Large")
                        .SelectedIndex(1)
                        .Width(160)
                ),

                Helper.CreateExampleSection("Allow Deselect",
                    new ButtonGroup()
                        .AddItems("Bold", "Italic", "Underline")
                        .AllowDeselect(true)
                        .SelectedIndex(0)
                ),

                Helper.CreateExampleSection("Content-sized Groups",
                    new VStack()
                        .Spacing(14)
                        .Children(
                            new ButtonGroup()
                                .AddItems("One", "Two", "Three")
                                .SelectedIndex(0),

                            new ButtonGroup()
                                .AddItems("Short", "Much longer option", "Mid")
                                .SelectedIndex(1)
                                .ItemPadding(new Thickness(10, 6, 10, 6)),

                            new ButtonGroup()
                                .Orientation(Rayo.Controls.Orientation.Vertical)
                                .AddItems("Compact", "Natural width", "No stretch")
                                .SelectedIndex(2)
                        )
                ),

                Helper.CreateExampleSection("Fixed Width Groups",
                    new VStack()
                        .Spacing(14)
                        .Children(
                            new ButtonGroup()
                                .Width(320)
                                .AddItems("Left", "Center", "Right")
                                .SelectedIndex(1),

                            new ButtonGroup()
                                .Width(220)
                                .AddItems("A", "B", "C", "D")
                                .SelectedIndex(2),

                            new ButtonGroup()
                                .Orientation(Rayo.Controls.Orientation.Vertical)
                                .Width(220)
                                .AddItems("First", "Second", "Third")
                                .SelectedIndex(0)
                        )
                ),

                Helper.CreateExampleSection("Styled Group",
                    new ButtonGroup()
                        .AddItems("Open", "In Progress", "Closed")
                        .SelectedIndex(1)
                        .Background(new Color(248, 250, 252))
                        .HoverBackground(new Color(241, 245, 249))
                        .PressedBackground(new Color(226, 232, 240))
                        .SelectedBackground(new Color(16, 185, 129))
                        .SelectedBorderBrush(new Color(5, 150, 105))
                        .SelectedTextColor(Color.White)
                        .BorderBrush(new Color(148, 163, 184))
                        .TextColor(new Color(51, 65, 85))
                )
            );
    }
}
