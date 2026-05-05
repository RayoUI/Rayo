using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class LoadingPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Loading", "Animated loading indicators"),

                Helper.CreateExampleSection("Spinner Types",
                    new HStack()
                        .Spacing(40)
                        .Alignment(Alignment.Center)
                        .Children(
                            new VStack()
                                .Spacing(10)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Loading()
                                        .Type(SpinnerType.Circle)
                                        .Size(40),
                                    new Label("Circle")
                                        .FontSize(12)
                                        .Foreground(ColorDefault.Secondary)
                                ),

                            new VStack()
                                .Spacing(10)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Loading()
                                        .Type(SpinnerType.Dots)
                                        .Size(40),
                                    new Label("Dots")
                                        .FontSize(12)
                                        .Foreground(ColorDefault.Secondary)
                                ),

                            new VStack()
                                .Spacing(10)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Loading()
                                        .Type(SpinnerType.Bars)
                                        .Size(40),
                                    new Label("Bars")
                                        .FontSize(12)
                                        .Foreground(ColorDefault.Secondary)
                                ),

                            new VStack()
                                .Spacing(10)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Loading()
                                        .Type(SpinnerType.Ring)
                                        .Size(40),
                                    new Label("Ring")
                                        .FontSize(12)
                                        .Foreground(ColorDefault.Secondary)
                                )
                        )
                ),

                Helper.CreateExampleSection("With Text",
                    new Loading()
                        .Type(SpinnerType.Circle)
                        .Size(new Size(50, 80))
                        .TextColor(ColorDefault.Info)
                        .Text("Loading...")
                )
            );
    }
}
