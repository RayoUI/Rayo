using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using VisualScripting.Controls;

namespace VisualScripting;

/// <summary>
/// Main view for the Visual Scripting editor application using MVVM pattern.
///
/// Layout (top-to-bottom):
///   ┌─────────────────────────────────────────────────────┐
///   │ NodeToolbar │    NodeEditorCanvas    │  Properties  │
///   │             │                        │              │
///   ├─────────────────────────────────────────────────────┤
///   │                  ConsolePanel                       │
///   └─────────────────────────────────────────────────────┘
/// </summary>
public class VisualScriptingView : ViewBase<VisualScriptingViewModel>
{
    public override VisualElement Build()
    {
        var editor = new NodeEditorCanvas(ViewModel.Graph);
        var toolbar = new NodeToolbar(editor);
        var properties = new NodePropertiesPanel();
        var console = new ConsolePanel(ViewModel);

        // Wire node selection to properties panel and ViewModel
        editor.OnNodeSelected = node =>
        {
            properties.Node(node);
            ViewModel.SelectNode(node?.Model);
        };

        toolbar.ZIndex = 10;

        // Main horizontal area
        var mainArea = new HStack()
            .Spacing(0f)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(toolbar, editor, properties);

        // Root splits available height: mainArea gets the rest, console gets fixed height
        return new EditorRoot(mainArea, console);
    }
}

/// <summary>
/// Two-row layout: top row (main editor) stretches, bottom row (console) is fixed height.
/// </summary>
internal class EditorRoot : CompositeView<EditorRoot>
{
    private readonly VisualElement _main;
    private readonly ConsolePanel _console;

    public EditorRoot(VisualElement main, ConsolePanel console)
    {
        _main = main;
        _console = console;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        AddChild(_main);
        AddChild(_console);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float mainH = Math.Max(0f, availableHeight - ConsolePanel.PanelHeight);
        _main.Measure(availableWidth, mainH);
        _console.Measure(availableWidth, ConsolePanel.PanelHeight);
        DesiredWidth = availableWidth;
        DesiredHeight = availableHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        float mainH = Math.Max(0f, height - ConsolePanel.PanelHeight);
        _main.Arrange(x, y, width, mainH);
        _console.Arrange(x, y + mainH, width, ConsolePanel.PanelHeight);
    }

    public override void Render(IRenderer renderer) { /* children render themselves */ }
}
