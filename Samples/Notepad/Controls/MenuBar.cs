using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Reactivity;

namespace Notepad.Controls;

public class MenuBar(IReadableSignal<TabControl?>? tabControl = null) : UserControl
{
    private int _untitledCounter = 1;

    public override VisualElement Build()
    {
        var fileMenu = new Menu("File")
            .AddItem(new MenuItem("New", () => {
                if (tabControl?.Value != null)
                {
                    var newTab = new EditorTab("")
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .VerticalAlignment(VerticalAlignment.Stretch);

                    tabControl.Value.AddTab($"Untitled-{_untitledCounter++}", newTab);
                    tabControl.Value.SelectedIndex = tabControl.Value.TabCount - 1;
                }
            }))
            .AddItem(new MenuItem("Open"))
            .AddItem(new MenuItem("Save"))
            .AddItem(new MenuItem("Exit", () => UIApplication.Current?.Exit()));

        var editMenu = new Menu("Edit")
            .AddItem(new MenuItem("Cut"))
            .AddItem(new MenuItem("Copy"))
            .AddItem(new MenuItem("Paste"));

        var viewMenu = new Menu("View")
            .AddItem(new MenuItem("Zoom In"))
            .AddItem(new MenuItem("Zoom Out"));

        var helpMenu = new Menu("Help")
            .AddItem(new MenuItem("About", () => {
                Dialog.Show(
                    "About Rayo Notepad",
                    "Version 1.0\n\nBuilt with Rayo Framework\n.NET 10 + OpenGL"
                );
            }));

        return new Frame()
            .Height(30)
            .Background(new Color(45, 45, 48))
            .Content(
                new HStack()
                    .Spacing(0)
                    .Alignment(Alignment.Center)
                    .Children(
                        fileMenu,
                        editMenu,
                        viewMenu,
                        helpMenu
                    )
            );
    }
}
