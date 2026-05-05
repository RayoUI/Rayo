using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Reactivity;
using Rayo.DevTool.Frames;

namespace Rayo.DevTool;

public class DevToolUI : UserControl
{
    // Static so the same instance survives hot-reload rebuilds of the DevTool UI itself.
    // The live TCP connection is preserved across reloads.
    private static readonly DevToolState _state = new();

    public override VisualElement Build()
    {
        return new VStack()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                // Top: Connection & Tools
                new ToolbarFrame(_state),

                // Middle: Main Workspace
                new Splitter()
                    .Orientation(SplitterOrientation.Vertical)
                    .Children(
                        // Top part of splitter: Tree & Properties/Performance using nested Splitter
                        new Splitter()
                            .Orientation(SplitterOrientation.Horizontal)
                            .Children(
                                // Left Sidebar: Tree View
                                new TreeFrame(_state),

                                // Right Panel: Properties / Performance tabs
                                new TabControl()
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .VerticalAlignment(VerticalAlignment.Stretch)
                                    .ShowTabCloseButtons(false)
                                    .EnableTabReorder(false)
                                    .Position(TabPosition.Top)
                                    .AddTab("Properties",  new PropertyFrame(_state)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        .VerticalAlignment(VerticalAlignment.Stretch))
                                    .AddTab("Performance", new PerformanceFrame(_state)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        .VerticalAlignment(VerticalAlignment.Stretch))
                            ),

                        // Bottom part of splitter: Console
                        new ConsoleFrame(_state)
                            .Height(100)
                            .IsVisible(_state.IsConsoleMaximized)
                    ),

                // Bottom: Status & Quick Actions
                new StatusBarFrame(_state)
            );
    }
}
