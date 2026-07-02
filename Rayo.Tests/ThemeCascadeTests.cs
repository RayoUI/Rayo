using Rayo.Controls;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Styling;

namespace Rayo.Tests;

public sealed class ThemeCascadeTests
{
    [Fact]
    public void Styles_override_theme_defaults()
    {
        var button = new Button();
        var styleColor = new Color(90, 40, 160);
        StyleSheet sheet =
        [
            new Style<Button>().Background(styleColor),
        ];

        StyleEngine.Apply(sheet, button);

        Assert.Equal(styleColor, button.Background.PrimaryColor);
        Assert.Equal(PropertyValueOrigin.Style, button.GetValueOrigin(nameof(Button.Background)));
    }

    [Fact]
    public void Local_value_wins_when_styles_are_reapplied()
    {
        var button = new Button();
        StyleSheet sheet =
        [
            new Style<Button>().Background(new Color(90, 40, 160)),
        ];
        StyleEngine.Apply(sheet, button);
        var local = new Color(12, 23, 34);
        button.Background = local;

        StyleEngine.Apply(sheet, button);

        Assert.Equal(local, button.Background.PrimaryColor);
        Assert.Equal(PropertyValueOrigin.Local, button.GetValueOrigin(nameof(Button.Background)));
    }

    [Fact]
    public void Generated_signal_setters_record_binding_origin()
    {
        var button = new Button();
        var fontSize = new Signal<float>(19f);

        button.FontSize(fontSize);

        Assert.Equal(19f, button.FontSize);
        Assert.Equal(PropertyValueOrigin.Binding, button.GetValueOrigin(nameof(Button.FontSize)));
    }

    [Fact]
    public void Theme_changes_update_the_baseline_under_an_active_style()
    {
        var button = new Button();
        var scope = new ThemeScope(RayoThemes.Light, button);
        StyleSheet active =
        [
            new Style<Button>().Background(new Color(90, 40, 160)),
        ];
        StyleEngine.Apply(active, button);

        scope.Theme = RayoThemes.Dark;
        StyleSheet noLongerMatching =
        [
            new Style<Button>(".missing").Background(Color.White),
        ];
        StyleEngine.Apply(noLongerMatching, button);

        Assert.Equal(RayoThemes.Dark.Colors.Primary, button.Background.PrimaryColor);
        Assert.Equal(PropertyValueOrigin.Theme, button.GetValueOrigin(nameof(Button.Background)));
    }
}
