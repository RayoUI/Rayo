using Rayo.Controls;
using Rayo.Core;
using Rayo.Rendering;
using Rayo.Styling;

namespace Rayo.Tests;

public sealed class AccordionThemeTests
{
    [Fact]
    public void Flush_appearance_reacts_to_theme_and_restores_normal_defaults()
    {
        var expander = new Expander("Section", new Label().Text("Content"));
        var accordion = new Accordion()
            .Flush(true)
            .AddExpander(expander);
        var scope = new ThemeScope(RayoThemes.Light, accordion);

        Assert.Equal(Color.Transparent, expander.HeaderBackground.PrimaryColor);
        Assert.Equal(RayoThemes.Light.Colors.SurfacePressed, expander.HeaderHoverColor.PrimaryColor);
        Assert.Equal(Color.Transparent, expander.ContentBackground.PrimaryColor);
        Assert.Equal(CornerRadius.None, expander.HeaderCornerRadius);

        scope.Theme = RayoThemes.Dark;

        Assert.Equal(Color.Transparent, expander.HeaderBackground.PrimaryColor);
        Assert.Equal(RayoThemes.Dark.Colors.SurfacePressed, expander.HeaderHoverColor.PrimaryColor);
        Assert.Equal(Color.Transparent, expander.ContentBackground.PrimaryColor);

        accordion.Flush = false;

        Assert.Equal(RayoThemes.Dark.Colors.SurfaceHover, expander.HeaderBackground.PrimaryColor);
        Assert.Equal(RayoThemes.Dark.Colors.SurfacePressed, expander.HeaderHoverColor.PrimaryColor);
        Assert.Equal(RayoThemes.Dark.Colors.Surface, expander.ContentBackground.PrimaryColor);
        Assert.Equal(new CornerRadius(8), expander.HeaderCornerRadius);
    }

    [Fact]
    public void Flush_appearance_preserves_consumer_theme_overrides()
    {
        var customHover = new Color(12, 34, 56);
        var expander = new Expander("Section")
            .HeaderHoverColor(customHover);
        var accordion = new Accordion()
            .Flush(true)
            .AddExpander(expander);
        var scope = new ThemeScope(RayoThemes.Light, accordion);

        scope.Theme = RayoThemes.Dark;
        accordion.Flush = false;

        Assert.Equal(customHover, expander.HeaderHoverColor.PrimaryColor);
    }
}
