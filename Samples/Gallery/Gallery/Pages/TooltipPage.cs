using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class TooltipPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Tooltip", "Contextual help on hover"),

                Helper.CreateExampleSection("Basic Tooltips",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Button()
                                .Text("Hover Me")
                                .WithTooltip("This is a tooltip!"),

                            new Button()
                                .Text("Save")
                                .Background(ColorDefault.Success)
                                .WithTooltip("Save your work"),

                            new Button()
                                .Text("Delete")
                                .Background(ColorDefault.Danger)
                                .WithTooltip("Delete this item")
                        )
                ),

                Helper.CreateExampleSection("Tooltip Placements",
                    new VStack()
                        .Spacing(15)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Button()
                                .Text("Top Tooltip")
                                .WithTooltip("Tooltip on top", TooltipPlacement.Top),

                            new Button()
                                .Text("Bottom Tooltip")
                                .WithTooltip("Tooltip on bottom", TooltipPlacement.Bottom),

                            new HStack()
                                .Spacing(20)
                                .Children(
                                    new Button()
                                        .Text("Left")
                                        .WithTooltip("Tooltip on left", TooltipPlacement.Left),
                                    new Button()
                                        .Text("Right")
                                        .WithTooltip("Tooltip on right", TooltipPlacement.Right)
                                )
                        )
                )
            );
    }
}
