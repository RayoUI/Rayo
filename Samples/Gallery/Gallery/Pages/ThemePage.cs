using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;

namespace Gallery.Pages;

public sealed class ThemePage : Component
{
    private readonly Action<ThemeData> _applyTheme;

    public static ThemeData BrandTheme { get; } = CreateBrandTheme();

    public ThemePage(Action<ThemeData> applyTheme)
    {
        _applyTheme = applyTheme;
    }

    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader(
                    "Themes",
                    "Switch themes at runtime and watch mounted controls, overlays and semantic variants update together."),

                Helper.CreateExampleSection(
                    "Global theme",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new HStack()
                                .Spacing(10)
                                .Children(
                                    CreateThemeButton("Light", RayoThemes.Light, ButtonVariant.Secondary),
                                    CreateThemeButton("Dark", RayoThemes.Dark, ButtonVariant.Secondary),
                                    CreateThemeButton("Rayo Brand", BrandTheme, ButtonVariant.Primary)
                                ),
                            new PaletteLabel(
                                "The active theme is propagated through UIApplication.UseTheme without rebuilding the control tree.",
                                colors => colors.OnDisabled)
                                .FontSize(12)
                        )
                ),

                Helper.CreateExampleSection(
                    "Semantic palette",
                    new Flex()
                        .Gap(10)
                        .RowGap(10)
                        .Wrap(FlexWrap.Wrap)
                        .Children(
                            CreateSwatch("Primary", colors => colors.Primary, colors => colors.OnPrimary),
                            CreateSwatch("Secondary", colors => colors.Secondary, colors => colors.OnSecondary),
                            CreateSwatch("Surface", colors => colors.Surface, colors => colors.OnSurface),
                            CreateSwatch("Success", colors => colors.Success, colors => colors.OnSuccess),
                            CreateSwatch("Warning", colors => colors.Warning, colors => colors.OnWarning),
                            CreateSwatch("Danger", colors => colors.Danger, colors => colors.OnDanger),
                            CreateSwatch("Info", colors => colors.Info, colors => colors.OnInfo)
                        )
                ),

                Helper.CreateExampleSection(
                    "ButtonTheme variants",
                    new HStack()
                        .Spacing(12)
                        .Children(
                            CreateVariantButton("Primary", ButtonVariant.Primary),
                            CreateVariantButton("Secondary", ButtonVariant.Secondary),
                            CreateVariantButton("Danger", ButtonVariant.Danger),
                            CreateVariantButton("Ghost", ButtonVariant.Ghost)
                        )
                ),

                Helper.CreateExampleSection(
                    "Controls following the theme",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new Entry()
                                .Placeholder("Theme-aware text input")
                                .Width(320),
                            new HStack()
                                .Spacing(20)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Checkbox("Checkbox") { IsChecked = true },
                                    new RadioButton("Radio") { IsChecked = true },
                                    new ToggleSwitch { IsOn = true },
                                    new Loading().Size(28)
                                ),
                            new Slider()
                                .Value(65)
                                .Width(320),
                            new ProgressBar()
                                .Value(65)
                                .Width(320),
                            new Card()
                                .Width(420)
                                .Header(new Label("Theme-aware card").FontSize(16))
                                .Content(new Label("Surface, border and text roles all come from the active palette."))
                        )
                ),

                Helper.CreateInfoCard(
                    "Explicit overrides win",
                    "Properties assigned by an application are preserved during theme changes. Call UseThemeDefaults() on Button or ButtonIcon to resume following the global theme.")
            );
    }

    private Button CreateThemeButton(string text, ThemeData theme, ButtonVariant variant)
    {
        return new Button()
            .Text(text)
            .Variant(variant)
            .Width(120)
            .Height(40)
            .OnTapped(() => _applyTheme(theme));
    }

    private static Button CreateVariantButton(string text, ButtonVariant variant)
    {
        return new Button()
            .Text(text)
            .Variant(variant)
            .Width(110)
            .Height(40);
    }

    private static VisualElement CreateSwatch(
        string name,
        Func<ColorScheme, Color> background,
        Func<ColorScheme, Color> foreground)
    {
        return new PaletteFrame(background)
            .Width(92)
            .Height(64)
            .Padding(new Thickness(8))
            .BorderRadius(new CornerRadius(8))
            .Content(
                new PaletteLabel(name, foreground)
                    .FontSize(11)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .TextVerticalAlignment(VerticalAlignment.Center)
            );
    }

    private static ThemeData CreateBrandTheme()
    {
        var palette = ColorSchemes.Dark with
        {
            Primary = new Color(236, 72, 153),
            PrimaryHover = new Color(219, 39, 119),
            PrimaryPressed = new Color(190, 24, 93),
            OnPrimary = Color.White,
            Secondary = new Color(99, 102, 241),
            SecondaryHover = new Color(79, 70, 229),
            SecondaryPressed = new Color(67, 56, 202),
            OnSecondary = Color.White,
            Focus = new Color(244, 114, 182),
        };

        var buttons = ButtonTheme.FromScheme(palette);
        buttons = buttons with
        {
            Ghost = buttons.Ghost with
            {
                Foreground = palette.Primary,
                Border = palette.Primary.WithAlpha(0.35f),
            },
        };

        return new ThemeData(
            "rayo-brand",
            palette,
            buttons,
            ThemeBrightness.Dark);
    }
}
