using Rayo.Controls;
using Rayo.Core;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using System.Reflection;

namespace Rayo.Tests;

public sealed class ThemeScopeTests
{
    [Fact]
    public void Child_uses_nearest_scope()
    {
        var child = new Button();
        var inner = new ThemeScope(RayoThemes.Dark, child);
        _ = new ThemeScope(RayoThemes.Light, inner);

        Assert.Same(RayoThemes.Dark, child.EffectiveTheme);
        Assert.Equal(RayoThemes.Dark.Colors.Primary, child.Background.PrimaryColor);
    }

    [Fact]
    public void Changing_scope_reapplies_only_its_subtree()
    {
        var child = new Button();
        var scope = new ThemeScope(RayoThemes.Light, child);

        scope.Theme = RayoThemes.Dark;

        Assert.Same(RayoThemes.Dark, child.EffectiveTheme);
        Assert.Equal(RayoThemes.Dark.Colors.Primary, child.Background.PrimaryColor);
    }

    [Fact]
    public void Explicit_value_survives_scope_change()
    {
        var explicitColor = new Color(7, 19, 31);
        var child = new Button();
        var scope = new ThemeScope(RayoThemes.Light, child);
        child.Background = explicitColor;

        scope.Theme = RayoThemes.Dark;

        Assert.Equal(explicitColor, child.Background.PrimaryColor);
    }

    [Fact]
    public void Detached_overlay_captures_owner_theme()
    {
        var tree = new UITree();
        OverlayManager.SetTree(tree);
        var owner = new Button();
        _ = new ThemeScope(RayoThemes.Dark, owner);
        var overlay = new Frame();

        OverlayManager.AddOverlay(overlay, owner);

        Assert.Same(RayoThemes.Dark, overlay.EffectiveTheme);
        OverlayManager.RemoveOverlay(overlay);
    }

    [Fact]
    public void Application_overlays_use_owner_theme_or_application_fallback()
    {
        using var app = new UIApplication();
        app.UseTheme(RayoThemes.Light);
        var owner = new Button();
        _ = new ThemeScope(RayoThemes.Dark, owner);
        var ownedOverlay = new Frame();
        var globalOverlay = new Frame();

        app.AddOverlay(ownedOverlay, owner);
        app.AddOverlay(globalOverlay);

        Assert.Same(RayoThemes.Dark, ownedOverlay.EffectiveTheme);
        Assert.Same(RayoThemes.Light, globalOverlay.EffectiveTheme);
        app.RemoveOverlay(ownedOverlay);
        app.RemoveOverlay(globalOverlay);
    }

    [Fact]
    public void Sidebar_internal_text_reacts_to_runtime_theme_changes()
    {
        var sidebar = new SideBar()
            .AddItem("Dashboard", "\u2302");
        var scope = new ThemeScope(RayoThemes.Dark, sidebar);

        scope.Theme = RayoThemes.Light;

        Assert.Equal(RayoThemes.Light.Colors.OnDisabled, sidebar.ItemTextColor.PrimaryColor);
        var label = Descendants(sidebar)
            .OfType<Label>()
            .Single(element => element.Text == "Dashboard");
        var expected = RayoThemes.Light.Colors.OnDisabled;
        var actual = label.Foreground.PrimaryColor;
        Assert.True(
            expected == actual,
            $"Expected {expected.R},{expected.G},{expected.B}; actual {actual.R},{actual.G},{actual.B}");

        scope.Theme = RayoThemes.Dark;
        label = Descendants(sidebar)
            .OfType<Label>()
            .Single(element => element.Text == "Dashboard");
        Assert.Equal(RayoThemes.Dark.Colors.OnDisabled, label.Foreground.PrimaryColor);
    }

    [Fact]
    public void Composite_control_internal_text_reacts_to_runtime_theme_changes()
    {
        var controls = new VisualElement[]
        {
            new ComboBox(),
            new DatePicker(),
            new TimePicker(),
            new PathPicker(),
            new Stepper(),
        };

        foreach (var control in controls)
        {
            var scope = new ThemeScope(RayoThemes.Dark, control);
            scope.Theme = RayoThemes.Light;

            var labels = Descendants(control).OfType<Label>().ToArray();
            Assert.NotEmpty(labels);
            Assert.DoesNotContain(
                labels,
                label => label.Foreground.PrimaryColor == RayoThemes.Dark.Colors.OnSurface);

            scope.Theme = RayoThemes.Dark;
            labels = Descendants(control).OfType<Label>().ToArray();
            Assert.DoesNotContain(
                labels,
                label => label.Foreground.PrimaryColor == RayoThemes.Light.Colors.OnSurface);
        }
    }

    [Fact]
    public void Stepper_applies_button_and_value_text_colors_after_theme_change()
    {
        var first = (RayoThemes.Light with
        {
            Name = "stepper-first",
            Colors = RayoThemes.Light.Colors with
            {
                OnPrimary = new Color(210, 20, 30),
                OnSurface = new Color(20, 180, 40),
            },
        }).RebuildComponentDefaults();
        var second = (RayoThemes.Dark with
        {
            Name = "stepper-second",
            Colors = RayoThemes.Dark.Colors with
            {
                OnPrimary = new Color(30, 60, 220),
                OnSurface = new Color(230, 190, 20),
            },
        }).RebuildComponentDefaults();
        var stepper = new Stepper();
        var scope = new ThemeScope(first, stepper);

        scope.Theme = second;

        var descendants = Descendants(stepper).ToArray();
        var buttons = descendants
            .OfType<Button>()
            .Where(button => button.Text is "-" or "+")
            .ToArray();
        var valueLabel = descendants
            .OfType<Label>()
            .Single(label => label.Text == stepper.Value.ToString(stepper.ValueFormat));

        Assert.Equal(2, buttons.Length);
        Assert.All(
            buttons,
            button => Assert.Equal(second.Colors.OnPrimary, button.TextColor.PrimaryColor));
        Assert.Equal(second.Colors.OnSurface, valueLabel.Foreground.PrimaryColor);
    }

    [Fact]
    public void Stepper_value_text_component_override_reacts_to_theme_change()
    {
        var firstColor = new Color(210, 20, 30);
        var secondColor = new Color(30, 180, 220);
        var first = RayoThemes.Light.WithComponentTheme(
            ComponentTheme<Stepper>.Empty
                .Set(stepper => stepper.ValueTextColor, (Brush)firstColor));
        var second = RayoThemes.Dark.WithComponentTheme(
            ComponentTheme<Stepper>.Empty
                .Set(stepper => stepper.ValueTextColor, (Brush)secondColor));

        using var app = new UIApplication();
        app.UseTheme(first);
        var stepper = new Stepper();
        var scope = new ThemeScope(first, stepper);

        scope.Theme = second;

        var valueLabel = Descendants(stepper)
            .OfType<Label>()
            .Single(label => label.Text == stepper.Value.ToString(stepper.ValueFormat));
        Assert.Equal(secondColor, stepper.ValueTextColor.PrimaryColor);
        Assert.Equal(secondColor, valueLabel.Foreground.PrimaryColor);
    }

    private static IEnumerable<VisualElement> Descendants(VisualElement root)
    {
        var getChildren = typeof(VisualElement).GetMethod(
            "GetChildren",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("VisualElement.GetChildren was not found.");
        var children = (IEnumerable<VisualElement>)getChildren.Invoke(root, null)!;
        foreach (var child in children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
