using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Orientation = Rayo.Controls.Orientation;

namespace Gallery.Pages;

public class StepperPage : UserControl
{
    public override VisualElement Build()
    {
        var value1 = new Signal<double>(0);
        var value2 = new Signal<double>(50);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Stepper", "Increment/decrement control for numeric values"),

                Helper.CreateExampleSection("Basic Stepper",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Stepper()
                                .Minimum(0)
                                .Maximum(10)
                                .Value(0)
                                .OnValueChanged(v => value1.Value = v),

                            new Label()
                                .Text(value1.Map(v => $"Value: {v}"))
                                .Foreground(Color.White)
                        )
                ),

                Helper.CreateExampleSection("Custom Range and Increment",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new Label()
                                .Text("Range: 0-100, Increment: 5")
                                .Foreground(ColorDefault.Secondary),

                            new HStack()
                                .Spacing(20)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Stepper()
                                        .Minimum(0)
                                        .Maximum(100)
                                        .Increment(5)
                                        .Value(50)
                                        .OnValueChanged(v => value2.Value = v),

                                    new Label()
                                        .Text(value2.Map(v => $"Value: {v}"))
                                        .Foreground(Color.White)
                                )
                        )
                ),

                Helper.CreateExampleSection("Without Value Display",
                    new Stepper()
                        .Minimum(0)
                        .Maximum(10)
                        .ShowValue(false)
                        .Width(90)
                ),

                Helper.CreateExampleSection("Vertical Orientation",
                    new Stepper()
                        .Minimum(0)
                        .Maximum(10)
                        .Orientation(Orientation.Vertical)
                        .Width(50)
                        .Height(120)
                ),

                Helper.CreateExampleSection("Custom Colors",
                    new Stepper()
                        .Minimum(0)
                        .Maximum(100)
                        .ButtonColor(new Color(76, 175, 80))
                        .ButtonHoverColor(new Color(96, 195, 100))
                        .Background(new Color(40, 50, 40))
                ),

                Helper.CreateExampleSection("Decimal Values",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label()
                                .Text("Increment: 0.1, Format: 0.0")
                                .Foreground(ColorDefault.Secondary),

                            new Stepper()
                                .Minimum(0)
                                .Maximum(1)
                                .Increment(0.1)
                                .Value(0.5)
                                .ValueFormat("0.0")
                                .Width(160)
                        )
                )
            );
    }
}
