using NanoApp.Controls;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace NanoApp.Pages;

public sealed class SceneEditorPage : Component
{
    private readonly SceneCanvas _sceneCanvas = new();
    private readonly EntityPropertiesPanel _propertiesPanel = new();
    private readonly Drawer _entityDrawer;
    private bool _isPropertiesPanelVisible = true;

    public SceneEditorPage()
    {
        _sceneCanvas.SelectionChanged += _propertiesPanel.ShowEntity;
        _entityDrawer = new Drawer()
            .Position(DrawerPosition.Left)
            .DrawerWidth(252)
            .ShowOverlay(false)
            .Background(new Color(20, 27, 40))
            .Content(BuildEntityPanel());
    }

    public override VisualElement Build()
    {
        // The canvas and properties panel preserve the scene while this page
        // rebuilds to show or hide the properties column.
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
                GridLength.Pixels(20),
                GridLength.Star,
                GridLength.Pixels(20),
                GridLength.Pixels(_isPropertiesPanelVisible ? 252 : 0))
            .Background(new Color(12, 16, 24));

        workspace
            .AddChild(
                BuildPanelTab(_entityDrawer.Open, isLeft: true),
                0,
                0)
            .AddChild(_sceneCanvas, 0, 1)
            .AddChild(
                BuildPanelTab(TogglePropertiesPanel, isLeft: false),
                0,
                2);

        if (_isPropertiesPanelVisible)
        {
            workspace.AddChild(_propertiesPanel, 0, 3);
        }

        return workspace;
    }

    private VisualElement BuildEntityPanel()
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
                        new EntityPaletteItem(
                            SceneEntityKind.Rectangle,
                            "Rectangle",
                            _sceneCanvas.TryAddEntityAt),
                        new EntityPaletteItem(
                            SceneEntityKind.Circle,
                            "Circle",
                            _sceneCanvas.TryAddEntityAt),
                        new EntityPaletteItem(
                            SceneEntityKind.Triangle,
                            "Triangle",
                            _sceneCanvas.TryAddEntityAt)));
    }

    private static VisualElement BuildPanelTab(Action onTapped, bool isLeft)
    {
        return new Frame()
            .Background(Color.Transparent)
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

    private void TogglePropertiesPanel()
    {
        _isPropertiesPanelVisible = !_isPropertiesPanelVisible;
        Rebuild();
    }
}
