using Gallery;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class ColorPickerPage : UserControl
{
    public override VisualElement Build()
    {
        var basicHexState = new Signal<string>(FormatHex(new Color(59, 130, 246)));
        var bindingColorState = new Signal<Color>(new Color(16, 185, 129));
        var dialogColorState = new Signal<Color>(new Color(168, 85, 247));

        var basicPicker = new ColorPicker()
            .SelectedColor(new Color(59, 130, 246));
        basicPicker.OnColorChanged(color => basicHexState.Value = FormatHex(color));

        var variantPicker = new ColorPicker()
            .SelectedColor(new Color(234, 179, 8));

        var noAlphaPicker = new ColorPicker()
            .ShowAlpha(false)
            .SelectedColor(new Color(59, 130, 246));

        var bindingPreview = CreateColorPreview("Live preview", bindingColorState);
        var dialogPreview = CreateColorPreview("Accent token", dialogColorState);

        var openDialogButton = new Button()
            .Text("Choose color")
            .Width(150)
            .Background(ColorDefault.Primary)
            .HoverBackground(ColorDefault.Info)
            .BorderRadius(8)
            .OnTapped(() => ColorPicker.ShowDialog(
                dialogColorState.Value,
                color => dialogColorState.Value = color,
                configure: picker => picker.ShowAlpha = false
            ));

        var bindingPicker = new ColorPicker()
            .SelectedColor(bindingColorState.Value);
        bindingPicker.OnColorChanged(color => bindingColorState.Value = color);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ColorPicker", "Dialog-driven color selection with HSV sliders, palette swatches, and optional alpha support"),

                Helper.CreateExampleSection("Basic Picker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Tap the preview to open the dialog. Listen to `ColorChanged` to react to selections.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            basicPicker,
                            new Label()
                                .FontSize(14)
                                .Foreground(ColorDefault.Info)
                                .Text(basicHexState.Map(value => $"Last selection: {value}"))
                        )
                ),

                Helper.CreateExampleSection("Configured Variants",
                    new HStack()
                        .Spacing(16)
                        .Wrap(true)
                        .Children(
                            CreateVariantCard("Preset value", "Start the picker with your brand or theme color.", variantPicker),
                            CreateVariantCard("Hide alpha", "Leave only RGB controls for opaque palettes.", noAlphaPicker)
                        )
                ),

                Helper.CreateExampleSection("Reactive Binding",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Bind the picker to signal state for instant previews and theming Frames.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            bindingPreview,
                            bindingPicker
                        )
                ),

                Helper.CreateExampleSection("Standalone Dialog",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Launch the picker from any button or custom surface without rendering the built-in trigger.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            dialogPreview,
                            openDialogButton
                        )
                )
            );
    }

    private static VisualElement CreateVariantCard(string title, string description, VisualElement picker)
    {
        return new Frame()
            .Background(new Color(40, 40, 48))
            .BorderRadius(10)
            .Padding(new Thickness(16))
            .Width(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new Label(title)
                            .FontSize(15)
                            .Foreground(Color.White),
                        new Label(description)
                            .FontSize(13)
                            .Foreground(ColorDefault.Secondary),
                        picker
                    )
            );
    }

    private static VisualElement CreateColorPreview(string title, Signal<Color> colorState)
    {
        return new Frame()
            .Height(90)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .BorderRadius(12)
            .Background(colorState)
            .BorderWidth(1)
            .BorderColor(new Color(255, 255, 255, 25))
            .Padding(new Thickness(16))
            .Content(
                new VStack()
                    .Spacing(4)
                    .Children(
                        new Label(title)
                            .FontSize(12)
                            .Foreground(ColorDefault.Secondary),
                        new Label()
                            .FontSize(16)
                            .Foreground(Color.White)
                            .Text(colorState.Map(FormatHex))
                    )
            );
    }

    private static string FormatHex(Color color)
    {
        int r = (int)MathF.Round(Clamp01(color.R) * 255f);
        int g = (int)MathF.Round(Clamp01(color.G) * 255f);
        int b = (int)MathF.Round(Clamp01(color.B) * 255f);
        int a = (int)MathF.Round(Clamp01(color.A) * 255f);
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
