using NanoApp.Controls;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace NanoApp;

public sealed class MainView : Component
{
    private readonly SceneCanvas _sceneCanvas = new();
    private readonly EntityPropertiesPanel _propertiesPanel = new();
    private bool _isEntityPanelVisible = true;
    private bool _isPropertiesPanelVisible = true;

    public MainView()
    {
        _sceneCanvas.SelectionChanged += _propertiesPanel.ShowEntity;
    }

    public override VisualElement Build()
    {
        return new TabControl()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddTab("Scene", BuildWorkspace());
    }

    private VisualElement BuildWorkspace()
    {
        // The scene canvas keeps the editor state between component rebuilds.
        // Rebuild() detaches the old root, but descendants remain attached to
        // their immediate containers until those containers are disposed.
        if (_sceneCanvas.Parent is Grid previousWorkspace)
        {
            previousWorkspace.RemoveChild(_sceneCanvas);
        }

        if (_propertiesPanel.Parent is Grid previousPropertiesWorkspace)
        {
            previousPropertiesWorkspace.RemoveChild(_propertiesPanel);
        }

        var workspace = new Grid()
            .Rows(GridLength.Star)
            .Columns(
                GridLength.Pixels(_isEntityPanelVisible ? 232 : 0),
                GridLength.Pixels(34),
                GridLength.Star,
                GridLength.Pixels(34),
                GridLength.Pixels(_isPropertiesPanelVisible ? 252 : 0))
            .Background(new Color(12, 16, 24));

        if (_isEntityPanelVisible)
        {
            workspace.AddChild(BuildEntityPanel(), 0, 0);
        }

        workspace
            .AddChild(
                BuildPanelTab(
                    _isEntityPanelVisible ? "◀" : "▶",
                    "Toggle entities",
                    ToggleEntityPanel,
                    isLeft: true),
                0,
                1)
            .AddChild(_sceneCanvas, 0, 2)
            .AddChild(
                BuildPanelTab(
                    _isPropertiesPanelVisible ? "▶" : "◀",
                    "Toggle properties",
                    TogglePropertiesPanel,
                    isLeft: false),
                0,
                3);

        if (_isPropertiesPanelVisible)
        {
            workspace.AddChild(_propertiesPanel, 0, 4);
        }

        return workspace;
    }

    private static VisualElement BuildEntityPanel()
    {
        return new Frame()
            .Background(new Color(20, 27, 40))
            .BorderBrush(new Color(45, 55, 72))
            .BorderThickness(new Thickness(0, 0, 1, 0))
            .Padding(new Thickness(14))
            .Content(
                new VStack()
                    .Spacing(12)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Children(
                        new Label("Entities")
                            .FontSize(16)
                            .FontWeight(FontWeight.Bold)
                            .Foreground(Color.White),
                        new Label("Drag a shape into the scene")
                            .FontSize(12)
                            .Foreground(new Color(148, 163, 184)),
                        new EntityPaletteItem(SceneEntityKind.Rectangle, "Rectangle"),
                        new EntityPaletteItem(SceneEntityKind.Circle, "Circle"),
                        new EntityPaletteItem(SceneEntityKind.Triangle, "Triangle")));
    }

    private static VisualElement BuildPanelTab(
        string glyph,
        string tooltip,
        Action onTapped,
        bool isLeft)
    {
        return new Frame()
            .Background(new Color(12, 16, 24))
            .Padding(new Thickness(0))
            .Content(
                new Button()
                    .Text("")
                    .Width(20)
                    .Height(20)
                    .FontSize(9)
                    .TextColor(new Color(203, 213, 225))
                    .Background(new Color(30, 41, 59))
                    .HoverBackground(new Color(51, 65, 85))
                    .PressedBackground(new Color(14, 165, 233))
                    .BorderBrush(new Color(71, 85, 105))
                    .BorderThickness(1)
                    .BorderRadius(isLeft
                        ? new CornerRadius(0, 8, 8, 0)
                        : new CornerRadius(8, 0, 0, 8))
                    .OnTapped(onTapped));
    }

    private void ToggleEntityPanel()
    {
        _isEntityPanelVisible = !_isEntityPanelVisible;
        Rebuild();
    }

    private void TogglePropertiesPanel()
    {
        _isPropertiesPanelVisible = !_isPropertiesPanelVisible;
        Rebuild();
    }
}
