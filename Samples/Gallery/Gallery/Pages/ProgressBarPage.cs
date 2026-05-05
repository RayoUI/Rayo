using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class ProgressBarPage : UserControl
{
    public override VisualElement Build()
    {
        var progress = new Signal<float>(0);
        var isIndeterminate = new Signal<bool>(false);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .JustifyContent(JustifyContent.Start)
            .Children(
                Helper.CreatePageHeader("ProgressBar", "Progress indicators for operations"),

                Helper.CreateExampleSection("Determinate Progress",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new ProgressBar()
                                .Value(progress),

                            new HStack()
                                .Spacing(10)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .Children(
                                    new Button()
                                        .Text("0%")
                                        .Width(70)
                                        .Background(ColorDefault.Primary)
                                        .OnTapped(() => progress.Value = 0),
                                    new Button()
                                        .Text("25%")
                                        .Width(70)
                                        .Background(ColorDefault.Primary)
                                        .OnTapped(() => progress.Value = 25),
                                    new Button()
                                        .Text("50%")
                                        .Width(70)
                                        .Background(ColorDefault.Primary)
                                        .OnTapped(() => progress.Value = 50),
                                    new Button()
                                        .Text("75%")
                                        .Width(70)
                                        .Background(ColorDefault.Primary)
                                        .OnTapped(() => progress.Value = 75),
                                    new Button()
                                        .Text("100%")
                                        .Width(70)
                                        .Background(ColorDefault.Success)
                                        .OnTapped(() => progress.Value = 100)
                                ),

                            new Label()
                                .Text(progress.Map(p => $"Progress: {p}%"))
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("Indeterminate Progress",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new ProgressBar()
                                .IsIndeterminate(true),

                            new Label("Used for operations with unknown duration")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary)
                        )
                ),

                Helper.CreateExampleSection("Custom Styles",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new ProgressBar()
                                .Value(60)
                                .ForegroundColor(ColorDefault.Success)
                                .BarHeight(8),

                            new ProgressBar()
                                .Value(40)
                                .ForegroundColor(ColorDefault.Warning)
                                .BarHeight(12)
                                .CornerRadius(6),

                            new ProgressBar()
                                .Value(80)
                                .ForegroundColor(ColorDefault.Danger)
                                .BarHeight(16)
                                .CornerRadius(8)
                        )
                )
            );
    }
}
