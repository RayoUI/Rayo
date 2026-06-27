using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class CheckboxPage : Component
{
    public override VisualElement Build()
    {
        var isChecked = UseSignal<bool>(false);
        var option1 = UseSignal<bool>(true);
        var option2 = UseSignal<bool>(false);
        var option3 = UseSignal<bool>(false);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Checkbox", "Selectable checkbox with label"),

                Helper.CreateExampleSection("Basic Checkbox",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new Checkbox("Accept terms and conditions")
                                .IsChecked(isChecked.Value)
                                .OnChanged(value => isChecked.Value = value),

                            new Label()
                                .Text(isChecked.Map(v => v ? "✓ Accepted" : "Not accepted"))
                                .Foreground(isChecked.Map(v => v ? ColorDefault.Success : ColorDefault.Secondary).Value)
                        )
                ),

                Helper.CreateExampleSection("Multiple Checkboxes",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new PaletteLabel("Select your preferences:", colors => colors.OnSurface)
                                .FontSize(14),

                            new Checkbox("Receive email notifications")
                                .IsChecked(option1.Value)
                                .OnChanged(value => option1.Value = value),

                            new Checkbox("Enable dark mode")
                                .IsChecked(option2.Value)
                                .OnChanged(value => option2.Value = value),

                            new Checkbox("Show advanced features")
                                .IsChecked(option3.Value)
                                .OnChanged(value => option3.Value = value),

                            new PaletteFrame(colors => colors.SurfaceHover)
                                .BorderRadius(6)
                                .Padding(new Thickness(12))
                                .Margin(new Thickness(0, 10, 0, 0))
                                .Content(
                                    new Label()
                                        .Text(
                                            new Computed<string>(() =>
                                            {
                                                var count = (option1.Value ? 1 : 0) +
                                                           (option2.Value ? 1 : 0) +
                                                           (option3.Value ? 1 : 0);
                                                return $"Selected: {count} option(s)";
                                            })
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Custom Colors",
                    new HStack()
                        .Spacing(20)
                        .Children(
                            new Checkbox("Red")
                                .CheckedBackground(ColorDefault.Danger),

                            new Checkbox("Green")
                                .CheckedBackground(ColorDefault.Success),

                            new Checkbox("Blue")
                                .CheckedBackground(ColorDefault.Primary)
                        )
                )
            );
    }
}
