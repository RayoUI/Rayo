using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;

namespace Gallery;

public static class Helper
{
    // Creates a page header with a title and description.
    public static VisualElement CreatePageHeader(string title, string description)
    {
        return new VStack()
            .Spacing(8)
            .Margin(new Thickness(0, 0, 0, 20))
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                new Label(title)
                    .FontSize(28)
                    .Foreground(ColorDefault.Primary),

                new Label(description)
                    .FontSize(14)
                    .Foreground(ColorDefault.Secondary)
            );
    }

    // Creates a section with a title and content for examples.
    public static VisualElement CreateExampleSection(string title, VisualElement content)
    {
        return new Frame()
            .Background(new Color(35, 35, 40))
            .BorderRadius(8)
            .Padding(new Thickness(20))
            .Content(
                new VStack()
                    .Spacing(15)
                    .Children(
                        new Label(title)
                            .FontSize(16)
                            .Foreground(Color.White),

                        content
                    )
            );
    }

    // Creates an informational card with a title and description.
    public static VisualElement CreateInfoCard(string title, string description)
    {
        return new Frame()
            .Background(new Color(40, 40, 50))
            .BorderRadius(8)
            .Padding(new Thickness(16))
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new Label(title)
                            .FontSize(16)
                            .Foreground(ColorDefault.Primary),

                        new Label(description)
                            .FontSize(14)
                            .Foreground(ColorDefault.Secondary)
                    )
            );
    }

    // Creates a section title for organizing examples.
    public static VisualElement CreateSectionTitle(string title)
    {
        return new Label(title)
            .FontSize(20)
            .Foreground(ColorDefault.Primary)
            .Margin(new Thickness(0, 10, 0, 0));
    }
}
