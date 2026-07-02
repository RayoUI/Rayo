using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
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
            .AddItem("Dashboard", "\u2302")
            .AddItem("Settings", "S")
            .SelectedKey("Dashboard");
        var scope = new ThemeScope(RayoThemes.Dark, sidebar);

        scope.Theme = RayoThemes.Light;

        Assert.Equal(RayoThemes.Light.Colors.OnSurface, sidebar.ItemTextColor.PrimaryColor);
        var selectedLabel = Descendants(sidebar)
            .OfType<Label>()
            .Single(element => element.Text == "Dashboard");
        var normalLabel = Descendants(sidebar)
            .OfType<Label>()
            .Single(element => element.Text == "Settings");
        Assert.Equal(RayoThemes.Light.Colors.OnPrimary, selectedLabel.Foreground.PrimaryColor);
        Assert.Equal(RayoThemes.Light.Colors.OnSurface, normalLabel.Foreground.PrimaryColor);

        scope.Theme = RayoThemes.Dark;
        selectedLabel = Descendants(sidebar)
            .OfType<Label>()
            .Single(element => element.Text == "Dashboard");
        normalLabel = Descendants(sidebar)
            .OfType<Label>()
            .Single(element => element.Text == "Settings");
        Assert.Equal(RayoThemes.Dark.Colors.OnPrimary, selectedLabel.Foreground.PrimaryColor);
        Assert.Equal(RayoThemes.Dark.Colors.OnSurface, normalLabel.Foreground.PrimaryColor);
    }

    [Fact]
    public void Carousel_indicators_ignore_button_theme_minimum_height()
    {
        var carousel = new Carousel()
            .IndicatorSize(8)
            .AddSlides(new Frame(), new Frame(), new Frame());
        var scope = new ThemeScope(RayoThemes.Light, carousel);

        carousel.MeasureUpdate(440, 180);

        var lightIndicators = Descendants(carousel)
            .OfType<Button>()
            .Where(button => string.IsNullOrEmpty(button.Text))
            .ToArray();
        Assert.Equal(3, lightIndicators.Length);
        Assert.All(lightIndicators, indicator => Assert.Equal(8, indicator.Height));
        Assert.All(lightIndicators, indicator => Assert.Equal(0, indicator.MinHeight));
        Assert.All(lightIndicators, indicator => Assert.Equal(8, indicator.DesiredHeight));

        scope.Theme = RayoThemes.Dark;
        carousel.MeasureUpdate(440, 180);

        var darkIndicators = Descendants(carousel)
            .OfType<Button>()
            .Where(button => string.IsNullOrEmpty(button.Text))
            .ToArray();
        Assert.Equal(3, darkIndicators.Length);
        Assert.All(darkIndicators, indicator => Assert.Equal(8, indicator.DesiredHeight));
        Assert.Equal(lightIndicators, darkIndicators);
        Assert.Equal(
            RayoThemes.Dark.Colors.Primary,
            darkIndicators[carousel.SelectedIndex].Background.PrimaryColor);
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
        Assert.All(
            descendants.OfType<Frame>(),
            frame => Assert.Equal(second.Colors.Surface, frame.Background.PrimaryColor));
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

    [Fact]
    public void Sidebar_collapse_reallocates_space_to_sibling_content()
    {
        var sidebar = new SideBar
        {
            ExpandedWidth = 180,
            CollapsedWidth = 60,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        sidebar
            .AddItem("Home", "H")
            .AddItem("Themes", "T")
            .AddItem("Settings", "S");
        var content = new Frame
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var layout = new HStack
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        layout.AddChild(sidebar);
        layout.AddChild(content);
        var host = new Frame(layout)
        {
            Width = 440,
            Height = 240,
        };
        using var app = new UIApplication();
        app.Tree.SetRoot(host);

        app.Tree.Update(440, 240);
        Assert.Equal(180, sidebar.ComputedWidth);
        var expandedContentWidth = content.ComputedWidth;

        sidebar.IsCollapsed = true;
        app.Tree.Update(440, 240);
        Assert.Equal(60, sidebar.ComputedWidth);
        Assert.True(content.ComputedWidth > expandedContentWidth);
        var collapsedIcons = Descendants(sidebar)
            .OfType<Label>()
            .Where(label => label.Text is "H" or "T" or "S")
            .ToArray();
        Assert.Equal(3, collapsedIcons.Length);
        Assert.All(collapsedIcons, icon =>
        {
            Assert.True(icon.ComputedWidth > 0);
            Assert.True(icon.ComputedHeight > 0);
        });

        sidebar.IsCollapsed = false;
        app.Tree.Update(440, 240);
        Assert.Equal(180, sidebar.ComputedWidth);
        Assert.Equal(expandedContentWidth, content.ComputedWidth);
    }

    [Fact]
    public void Mounted_sidebar_items_keep_their_geometry_after_application_theme_change()
    {
        using var app = new UIApplication();
        app.UseTheme(RayoThemes.Light);
        var sidebar = new SideBar()
            .ExpandedWidth(180)
            .AddItem("Home", "H")
            .AddItem("Themes", "T")
            .AddItem("Settings", "S")
            .SelectedKey("Home");
        var content = new Frame
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var layout = new HStack
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        layout.AddChild(sidebar);
        layout.AddChild(content);
        app.Tree.SetRoot(new Frame(layout)
        {
            Width = 440,
            Height = 240,
        });
        app.Tree.Update(440, 240);
        var lightLabels = Descendants(sidebar)
            .OfType<Label>()
            .Where(label => label.Text is "Home" or "Themes" or "Settings")
            .ToArray();

        app.UseTheme(RayoThemes.Dark);
        app.Tree.Update(440, 240);

        var darkLabels = Descendants(sidebar)
            .OfType<Label>()
            .Where(label => label.Text is "Home" or "Themes" or "Settings")
            .ToArray();
        Assert.Equal(lightLabels, darkLabels);
        Assert.All(darkLabels, label =>
        {
            Assert.True(label.ComputedWidth > 0);
            Assert.True(label.ComputedHeight > 0);
        });
        Assert.Equal(RayoThemes.Dark.Colors.OnPrimary, darkLabels[0].Foreground.PrimaryColor);
        Assert.Equal(RayoThemes.Dark.Colors.OnSurface, darkLabels[1].Foreground.PrimaryColor);
        Assert.Equal(RayoThemes.Dark.Colors.OnSurface, darkLabels[2].Foreground.PrimaryColor);

        sidebar.IsCollapsed = true;
        app.Tree.Update(440, 240);
        var darkIcons = Descendants(sidebar)
            .OfType<Label>()
            .Where(label => label.Text is "H" or "T" or "S")
            .ToArray();

        app.UseTheme(RayoThemes.Light);
        app.Tree.Update(440, 240);

        var lightIcons = Descendants(sidebar)
            .OfType<Label>()
            .Where(label => label.Text is "H" or "T" or "S")
            .ToArray();
        Assert.Equal(darkIcons, lightIcons);
        Assert.All(lightIcons, icon =>
        {
            Assert.True(icon.ComputedWidth > 0);
            Assert.True(icon.ComputedHeight > 0);
        });
        Assert.Equal(RayoThemes.Light.Colors.OnPrimary, lightIcons[0].Foreground.PrimaryColor);
        Assert.Equal(RayoThemes.Light.Colors.OnSurface, lightIcons[1].Foreground.PrimaryColor);
        Assert.Equal(RayoThemes.Light.Colors.OnSurface, lightIcons[2].Foreground.PrimaryColor);
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
