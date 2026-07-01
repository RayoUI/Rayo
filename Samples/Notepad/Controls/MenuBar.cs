using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;

namespace Notepad.Controls;

public sealed class MenuBar(NotepadWorkspace workspace) : Component
{
    public override VisualElement Build()
    {
        var fileMenu = new Menu("File")
            .AddItem(new MenuItem("New", workspace.NewDocument))
            .AddItem(new MenuItem("Open...", workspace.OpenDocument))
            .AddItem(new MenuItem("Save", workspace.SaveDocument))
            .AddItem(new MenuItem("Save As...", workspace.SaveDocumentAs))
            .AddItem(new MenuItem("Close", workspace.CloseDocument))
            .AddItem(new MenuItem("Exit", workspace.Exit));

        var editMenu = new Menu("Edit")
            .AddItem(new MenuItem("Undo", workspace.Undo))
            .AddItem(new MenuItem("Redo", workspace.Redo))
            .AddItem(new MenuItem("Cut", workspace.Cut))
            .AddItem(new MenuItem("Copy", workspace.Copy))
            .AddItem(new MenuItem("Paste", workspace.Paste))
            .AddItem(new MenuItem("Select All", workspace.SelectAll));

        var viewMenu = new Menu("View")
            .AddItem(new MenuItem("Zoom In", workspace.ZoomIn))
            .AddItem(new MenuItem("Zoom Out", workspace.ZoomOut))
            .AddItem(new MenuItem("Reset Zoom", workspace.ResetZoom))
            .AddItem(new MenuItem("Toggle Word Wrap", workspace.ToggleWordWrap))
            .AddItem(
                new MenuItem("Theme")
                    .AddItem(new MenuItem("Light", workspace.UseLightTheme)
                        .CheckedWhen(workspace.IsLightTheme))
                    .AddItem(new MenuItem("Dark", workspace.UseDarkTheme)
                        .CheckedWhen(workspace.IsDarkTheme))
                    .AddItem(new MenuItem("Neon", workspace.UseNeonTheme)
                        .CheckedWhen(workspace.IsNeonTheme)));

        var helpMenu = new Menu("Help")
            .AddItem(new MenuItem("About", () =>
            {
                Dialog.Show(
                    "About Rayo Notepad",
                    "Version 1.0\n\nA tabbed text editor built with Rayo Framework and .NET 10.");
            }));

        return new ThemeFrame(colors => colors.Surface, colors => colors.Border)
            .Height(31)
            .BorderThickness(new Thickness(0, 0, 0, 1))
            .Content(
                new HStack()
                    .Spacing(0)
                    .Alignment(Alignment.Center)
                    .Children(fileMenu, editMenu, viewMenu, helpMenu)
            );
    }
}
