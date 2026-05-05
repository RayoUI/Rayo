using Rayo.Core;
using Rayo.Controls;
using Rayo.Layout;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using Rayo.Reactivity;
using Notepad.Controls;
using static Rayo.Reactivity.Hooks;
using HarfBuzzSharp;

namespace Rayo.Example;

public class FileInfo
{
    public string FileName { get; set; }
    public string Content { get; set; }
    public FileInfo(string fileName, string content)
    {
        FileName = fileName;
        Content = content;
    }
}

public class NotepadApp : UserControl
{
    private Signal<TabControl?> _tabControl = new(null);

    private readonly List<FileInfo> _files = new()
    {
        new("Untitled-1.txt", "Welcome to Rayo Notepad!\n\nThis is a modular tabbed text editor."),
        new("Notes.md", "- Item 1\n- Item 2\n- Item 3"),
        new("Config.json", "{\n  \"theme\": \"dark\",\n  \"fontSize\": 14\n}")
    };

    protected override void OnAfterBuild(VisualElement builtElement)
    {
        foreach (var file in _files)
        {
            _tabControl.Value?.AddTab(file.FileName, new EditorTab(file.Content));
        }
    }

    public override VisualElement Build()
    {
        _tabControl = new Signal<TabControl?>(null);

        return new VStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Spacing(0)
            .Children(
                // Menu Bar
                new MenuBar(
                    _tabControl
                ),
                // Content Area
                new Frame()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Content(
                         new TabControl()
                            .Ref(t => _tabControl.Value = t)
                            .Position(TabPosition.Top)
                            .TabBackground(new Color(45, 45, 48))
                            .TabActiveBackground(new Color(30, 30, 30))
                            .TabHoverBackground(new Color(60, 60, 60))
                            .ContentBackground(new Color(30, 30, 30))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch)
                    ),

                // Status Bar
                new StatusBar()
            );
    }
}
