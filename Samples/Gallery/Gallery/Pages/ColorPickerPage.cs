using Gallery;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class ColorPickerPage : Component
{
    public override VisualElement Build()
    {
        var basicColorState = new Signal<Color>(new Color(59, 130, 246));
        var variantColorState = new Signal<Color>(new Color(234, 179, 8));
        var noAlphaColorState = new Signal<Color>(new Color(59, 130, 246));
        var bindingColorState = new Signal<Color>(new Color(16, 185, 129));
        var dialogColorState = new Signal<Color>(new Color(168, 85, 247));

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

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ColorPicker", "Compact popup and dialog color selection with optional alpha support"),

                Helper.CreateExampleSection("Compact Popup",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Open a compact picker beside the swatch, with gradient, hue, and RGB controls.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            CreateColorLauncher("Primary color", "Compact popup", basicColorState),
                            new Label()
                                .FontSize(14)
                                .Foreground(ColorDefault.Info)
                                .Text(basicColorState.Map(value => $"Last selection: {FormatHex(value)}"))
                        )
                ),

                Helper.CreateExampleSection("Configured Variants",
                    new HStack()
                        .Spacing(16)
                        .Wrap(true)
                        .Children(
                            CreateVariantCard("Preset value", "Start the popup with your brand or theme color.",
                                CreateColorLauncher("Brand color", "Includes alpha", variantColorState)),
                            CreateVariantCard("Hide alpha", "Leave only RGB controls for opaque palettes.",
                                CreateColorLauncher("Opaque color", "RGB only", noAlphaColorState, showAlpha: false))
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
                            CreateColorLauncher("ThemeData color", "Updates the live preview", bindingColorState)
                        )
                ),

                Helper.CreateExampleSection("Standalone Dialog",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Launch the picker from any button or custom surface.")
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
        return new PaletteFrame(colors => colors.SurfaceHover, colors => colors.Border)
            .BorderRadius(10)
            .BorderThickness(1)
            .Padding(new Thickness(16))
            .Width(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new PaletteLabel(title, colors => colors.OnSurface)
                            .FontSize(15),
                        new PaletteLabel(description, colors => colors.OnDisabled)
                            .FontSize(13),
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
            .BorderThickness(1)
            .BorderBrush(new Color(255, 255, 255, 25))
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

    private static VisualElement CreateColorLauncher(string title, string subtitle, Signal<Color> colorState, bool showAlpha = true)
    {
        var swatch = new Frame()
            .Size(40)
            .BorderRadius(10)
            .Background(colorState)
            .BorderThickness(1)
            .BorderBrush(new Color(255, 255, 255, 30));

        var launcherContent = new PaletteFrame(colors => colors.SurfaceHover, colors => colors.Border)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(new Thickness(14, 12))
            .BorderRadius(new CornerRadius(10))
            .BorderThickness(1)
            .Content(
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        swatch,
                        new VStack()
                            .Spacing(2)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Children(
                                new PaletteLabel(title, colors => colors.OnSurface)
                                    .FontSize(15),
                                new PaletteLabel(string.Empty, colors => colors.OnDisabled)
                                    .FontSize(12)
                                    .Text(colorState.Map(color => $"{subtitle} - {FormatHex(color)}"))
                            )
                    )
            );

        ColorPicker.ClickableFrame? launcher = null;
        launcher = new ColorPicker.ClickableFrame(() => ColorPicker.ShowPopup(
                launcher!,
                colorState.Value,
                color => colorState.Value = color,
                configure: picker => picker.ShowAlpha = showAlpha))
            .Background(Color.Transparent)
            .HoverBackground(new Color(255, 255, 255, 0.08f))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(launcherContent);
        return launcher;
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
