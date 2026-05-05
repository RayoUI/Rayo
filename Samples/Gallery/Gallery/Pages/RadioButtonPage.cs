using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class RadioButtonPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("RadioButton", "Mutually exclusive selection"),

                Helper.CreateExampleSection("Basic Radio Group",
                    new Frame()
                        .Background(new Color(40, 40, 50))
                        .BorderRadius(8)
                        .Padding(new Thickness(16))
                        .Content(
                            new VStack()
                                .Spacing(12)
                                .Children(
                                    new Label("Choose your favorite framework:")
                                        .FontSize(14)
                                        .Foreground(ColorDefault.Secondary),

                                    new RadioButton("React", "framework")
                                        .IsChecked(true),

                                    new RadioButton("Vue", "framework"),

                                    new RadioButton("Angular", "framework"),

                                    new RadioButton("Svelte", "framework")
                                )
                        )
                ),

                Helper.CreateExampleSection("Multiple Groups",
                    new HStack()
                        .Spacing(30)
                        .Children(
                            new Frame()
                                .Background(new Color(40, 40, 50))
                                .BorderRadius(8)
                                .Padding(new Thickness(16))
                                .Content(
                                    new VStack()
                                        .Spacing(10)
                                        .Children(
                                            new Label("Size:")
                                                .Foreground(ColorDefault.Secondary),

                                            new RadioButton("Small", "size"),
                                            new RadioButton("Medium", "size")
                                                .IsChecked(true),
                                            new RadioButton("Large", "size")
                                        )
                                ),

                            new Frame()
                                .Background(new Color(40, 40, 50))
                                .BorderRadius(8)
                                .Padding(new Thickness(16))
                                .Content(
                                    new VStack()
                                        .Spacing(10)
                                        .Children(
                                            new Label("Color:")
                                                .Foreground(ColorDefault.Secondary),

                                            new RadioButton("Red", "color")
                                                .CheckedBackground(ColorDefault.Danger),
                                            new RadioButton("Green", "color")
                                                .CheckedBackground(ColorDefault.Success)
                                                .IsChecked(true),
                                            new RadioButton("Blue", "color")
                                                .CheckedBackground(ColorDefault.Primary)
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("With Event Handler",
                    new Frame()
                        .Background(new Color(40, 40, 50))
                        .BorderRadius(8)
                        .Padding(new Thickness(16))
                        .Content(
                            new VStack()
                                .Spacing(12)
                                .Children(
                                    new Label("Select a theme:")
                                        .Foreground(ColorDefault.Secondary),

                                    new RadioButton("Light Theme", "theme")
                                        .OnChanged(isChecked =>
                                        {
                                            if (isChecked)
                                                System.Console.WriteLine("Light theme selected");
                                        }),

                                    new RadioButton("Dark Theme", "theme")
                                        .IsChecked(true)
                                        .OnChanged(isChecked =>
                                        {
                                            if (isChecked)
                                                System.Console.WriteLine("Dark theme selected");
                                        }),

                                    new RadioButton("Auto Theme", "theme")
                                        .OnChanged(isChecked =>
                                        {
                                            if (isChecked)
                                                System.Console.WriteLine("Auto theme selected");
                                        })
                                )
                        )
                )
            );
    }
}
