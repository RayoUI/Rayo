using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class SliderPage : UserControl
{
    public override VisualElement Build()
    {
        var volume = new Signal<float>(50);
        var brightness = new Signal<float>(75);
        var temperature = new Signal<float>(20);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Slider", "Adjustable value slider"),

                Helper.CreateExampleSection("Basic Slider",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(volume.Map(v => $"Volume: {v:F0}%"))
                                .Foreground(ColorDefault.Info),

                            new Slider()
                                .Width(300)
                                .Value(volume.Value)
                                .OnValueChanged(val => volume.Value = val)
                        )
                ),

                Helper.CreateExampleSection("Brightness Control",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new HStack()
                                .Spacing(10)
                                .Children(
                                    new Label("🔅")
                                        .FontSize(20),

                                    new Slider()
                                        .Width(250)
                                        
                                        
                                        .Value(brightness.Value)
                                        .OnValueChanged(val => brightness.Value = val),

                                    new Label("🔆")
                                        .FontSize(20)
                                ),

                            new Label()
                                .Text(brightness.Map(b => $"Brightness: {b:F0}%"))
                                .Foreground(ColorDefault.Secondary)
                        )
                ),

                Helper.CreateExampleSection("Temperature Range",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(temperature.Map(t => $"Temperature: {t:F1}°C"))
                                .FontSize(18)
                                .Foreground(new Computed<Color>(() =>
                                {
                                    var temp = temperature.Value;
                                    if (temp < 15) return ColorDefault.Primary;
                                    if (temp < 25) return ColorDefault.Success;
                                    return ColorDefault.Danger;
                                }).Value),

                            new Slider()
                                .Width(300)
                                
                                
                                .Value(temperature.Value)
                                .OnValueChanged(val => temperature.Value = val),

                            new HStack()
                                .Spacing(20)
                                .JustifyContent(JustifyContent.SpaceBetween)
                                .Width(300)
                                .Children(
                                    new Label("0°C")
                                        .Foreground(ColorDefault.Secondary),
                                    new Label("40°C")
                                        .Foreground(ColorDefault.Secondary)
                                )
                        )
                ),

                Helper.CreateExampleSection("Multiple Sliders",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new Label("Audio Settings")
                                .FontSize(16)
                                .Foreground(Color.White),

                            new VStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Master Volume")
                                        .Foreground(ColorDefault.Secondary),
                                    new Slider()
                                        .Width(300)
                                        .Value(70)
                                ),

                            new VStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Music Volume")
                                        .Foreground(ColorDefault.Secondary),
                                    new Slider()
                                        .Width(300)
                                        .Value(60)
                                ),

                            new VStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Effects Volume")
                                        .Foreground(ColorDefault.Secondary),
                                    new Slider()
                                        .Width(300)
                                        .Value(80)
                                )
                        )
                )
    
            );
    }
}
