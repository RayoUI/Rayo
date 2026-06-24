using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class EntryNumberPage : Component
{
    public override VisualElement Build()
    {
        var quantity = UseSignal(1d);
        var price = UseSignal(19.95d);
        var temperature = UseSignal(-3.5d);
        var percent = UseSignal(50d);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("EntryNumber", "Single-line input optimized for numeric values"),

                Helper.CreateExampleSection("Basic Number",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Quantity:")
                                .Foreground(ColorDefault.Secondary),

                            new EntryNumber(1)
                                .Width(220)
                                .Placeholder("0")
                                .IntegerOnly(allowNegative: false)
                                .Minimum(0)
                                .Maximum(999)
                                .OnValueChanged(value => quantity.Value = value),

                            new Label()
                                .Text(quantity.Map(value => $"Selected quantity: {value:0}"))
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("Decimal Value",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Price:")
                                .Foreground(ColorDefault.Secondary),

                            new EntryNumber(19.95)
                                .Width(220)
                                .Placeholder("0.00")
                                .Minimum(0)
                                .ValueFormat("0.00")
                                .OnValueChanged(value => price.Value = value),

                            new Label()
                                .Text(price.Map(value => $"Total preview: {(value * 3):0.00}"))
                                .Foreground(ColorDefault.Success)
                        )
                ),

                Helper.CreateExampleSection("Negative and Decimal",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Temperature:")
                                .Foreground(ColorDefault.Secondary),

                            new EntryNumber(-3.5)
                                .Width(220)
                                .Placeholder("-0.0")
                                .Minimum(-50)
                                .Maximum(50)
                                .ValueFormat("0.0")
                                .OnValueChanged(value => temperature.Value = value),

                            new Label()
                                .Text(temperature.Map(value => $"Current temperature: {value:0.0} C"))
                                .Foreground(temperature.Map(value =>
                                {
                                    if (value < 0) return ColorDefault.Primary;
                                    if (value < 30) return ColorDefault.Success;
                                    return ColorDefault.Danger;
                                }))
                        )
                ),

                Helper.CreateExampleSection("Range Validation",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Percent (0-100):")
                                .Foreground(ColorDefault.Secondary),

                            new EntryNumber(50)
                                .Width(220)
                                .Placeholder("0")
                                .Minimum(0)
                                .Maximum(100)
                                .ValueFormat("0")
                                .OnValueChanged(value => percent.Value = value),

                            new ProgressBar()
                                .Width(300)
                                .Height(12)
                                .Value(percent.Map(value => (float)value)),

                            new Label()
                                .Text(percent.Map(value => $"Stored value: {value:0}%"))
                                .Foreground(ColorDefault.Secondary)
                        )
                ),

                Helper.CreateExampleSection("Custom Style",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Compact financial input:")
                                .Foreground(ColorDefault.Secondary),

                            new EntryNumber(1250.75)
                                .Width(260)
                                .ValueFormat("0.00")
                                .Background(new Color(20, 35, 32))
                                .TextColor(new Color(190, 245, 220))
                                .PlaceholderColor(new Color(90, 140, 120))
                                .BorderColor(new Color(45, 180, 125))
                                .FocusBorderColor(new Color(80, 220, 160))
                                .FontSize(18)
                        )
                )
            );
    }
}
