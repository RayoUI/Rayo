using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class MenuPage : Component
{
    private string _lastAction = "No action yet";
    private Label? _actionLabel;

    public override VisualElement Build()
    {
        _actionLabel = new Label(_lastAction)
            .FontSize(13)
            .Foreground(GalleryPalette.Muted);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Menu & MenuItem", "Dropdown menus for application actions and navigation"),

                Helper.CreateExampleSection("Basic Menu",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("File")
                                                .AddItem(new MenuItem("New", () => UpdateAction("New file created"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Add, new Color(34, 197, 94))))
                                                .AddItem(new MenuItem("Open", () => UpdateAction("Open file dialog"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Folder, new Color(59, 130, 246))))
                                                .AddItem(new MenuItem("Save", () => UpdateAction("File saved"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Save, new Color(234, 179, 8))))
                                                .AddSeparator()
                                                .AddItem(new MenuItem("Exit", () => UpdateAction("Exit clicked"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Error, new Color(239, 68, 68))))
                                        )
                                ),
                            new HStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Last action:")
                                        .FontSize(13)
                                        .Foreground(GalleryPalette.Muted),
                                    _actionLabel
                                )
                        )
                ),

                Helper.CreateExampleSection("Multiple Menus (Menu Bar)",
                    new PaletteFrame(colors => colors.SurfacePressed)
                        .Height(40)
                        .BorderRadius(4)
                        .Content(
                            new HStack()
                                .Spacing(0)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Menu("File")
                                        .AddItem(new MenuItem("New", () => UpdateAction("File > New")))
                                        .AddItem(new MenuItem("Open", () => UpdateAction("File > Open")))
                                        .AddSeparator()
                                        .AddItem(new MenuItem("Save", () => UpdateAction("File > Save")))
                                        .AddItem(new MenuItem("Save As", () => UpdateAction("File > Save As")))
                                        .AddSeparator()
                                        .AddItem(new MenuItem("Exit", () => UpdateAction("File > Exit"))),

                                    new Menu("Edit")
                                        .AddItem(new MenuItem("Undo", () => UpdateAction("Edit > Undo")))
                                        .AddItem(new MenuItem("Redo", () => UpdateAction("Edit > Redo")))
                                        .AddSeparator()
                                        .AddItem(new MenuItem("Cut", () => UpdateAction("Edit > Cut")))
                                        .AddItem(new MenuItem("Copy", () => UpdateAction("Edit > Copy")))
                                        .AddItem(new MenuItem("Paste", () => UpdateAction("Edit > Paste"))),

                                    new Menu("View")
                                        .AddItem(new MenuItem("Zoom In", () => UpdateAction("View > Zoom In")))
                                        .AddItem(new MenuItem("Zoom Out", () => UpdateAction("View > Zoom Out")))
                                        .AddSeparator()
                                        .AddItem(new MenuItem("Full Screen", () => UpdateAction("View > Full Screen"))),

                                    new Menu("Help")
                                        .AddItem(new MenuItem("Documentation", () => UpdateAction("Help > Documentation")))
                                        .AddSeparator()
                                        .AddItem(new MenuItem("About", () => UpdateAction("Help > About")))
                                )
                        )
                ),

                Helper.CreateExampleSection("Menus with Icons",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .BorderRadius(4)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("Insert")
                                                .AddItem(new MenuItem("Image", () => UpdateAction("Insert > Image"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.File, new Color(59, 130, 246))))
                                                .AddItem(new MenuItem("Component", () => UpdateAction("Insert > Component"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Add, new Color(34, 197, 94))))
                                                .AddItem(new MenuItem("Snippet", () => UpdateAction("Insert > Snippet"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Edit, new Color(234, 179, 8)))),

                                            new Menu("Run")
                                                .AddItem(new MenuItem("Start", () => UpdateAction("Run > Start"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Play, new Color(34, 197, 94))))
                                                .AddItem(new MenuItem("Pause", () => UpdateAction("Run > Pause"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Pause, new Color(234, 179, 8))))
                                                .AddSeparator()
                                                .AddItem(new MenuItem("Stop", () => UpdateAction("Run > Stop"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Error, new Color(239, 68, 68)))))
                                ),

                            new Label("Menu items can include icons with custom colors and sizes")
                                .FontSize(12)
                                .Foreground(GalleryPalette.Muted)
                        )
                ),

                Helper.CreateExampleSection("Separators",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .BorderRadius(4)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("File")
                                                .AddItem(new MenuItem("New", () => UpdateAction("Separator > New"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Add, new Color(34, 197, 94))))
                                                .AddItem(new MenuItem("Open", () => UpdateAction("Separator > Open"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Folder, new Color(59, 130, 246))))
                                                .AddSeparator()
                                                .AddItem(new MenuItem("Save", () => UpdateAction("Separator > Save"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Save, new Color(234, 179, 8))))
                                                .AddItem(new MenuItem("Save As", () => UpdateAction("Separator > Save As"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Save, new Color(234, 179, 8))))
                                                .AddSeparator()
                                                .AddItem(new MenuItem("Exit", () => UpdateAction("Separator > Exit"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Error, new Color(239, 68, 68)))),

                                            new Menu("Edit")
                                                .AddItem(new MenuItem("Undo", () => UpdateAction("Separator > Undo")))
                                                .AddItem(new MenuItem("Redo", () => UpdateAction("Separator > Redo")))
                                                .AddSeparator()
                                                .AddItem(
                                                    new MenuItem("Clipboard")
                                                        .AddItem(new MenuItem("Cut", () => UpdateAction("Separator > Cut")))
                                                        .AddItem(new MenuItem("Copy", () => UpdateAction("Separator > Copy")))
                                                        .AddItem(new MenuItem("Paste", () => UpdateAction("Separator > Paste")))
                                                        .AddSeparator()
                                                        .AddItem(new MenuItem("Select All", () => UpdateAction("Separator > Select All"))))
                                        )
                                ),

                            new Label("Use AddSeparator() to group related actions in menus and submenus")
                                .FontSize(12)
                                .Foreground(GalleryPalette.Muted)
                        )
                ),

                Helper.CreateExampleSection("Application-Style Menu Bar",
                    CreateApplicationMenuBar()
                ),

                Helper.CreateExampleSection("Text Alignment",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .BorderRadius(4)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("Left Aligned")
                                                .AddItem(new MenuItem("Option 1", () => UpdateAction("Left > Option 1"))
                                                    .TextAlignment(HorizontalAlignment.Left))
                                                .AddItem(new MenuItem("Option 2", () => UpdateAction("Left > Option 2"))
                                                    .TextAlignment(HorizontalAlignment.Left))
                                                .AddItem(new MenuItem("Longer Option", () => UpdateAction("Left > Longer"))
                                                    .TextAlignment(HorizontalAlignment.Left))
                                        )
                                ),
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .BorderRadius(4)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("Center Aligned")
                                                .AddItem(new MenuItem("Option 1", () => UpdateAction("Center > Option 1"))
                                                    .TextAlignment(HorizontalAlignment.Center))
                                                .AddItem(new MenuItem("Option 2", () => UpdateAction("Center > Option 2"))
                                                    .TextAlignment(HorizontalAlignment.Center))
                                                .AddItem(new MenuItem("Longer Option", () => UpdateAction("Center > Longer"))
                                                    .TextAlignment(HorizontalAlignment.Center))
                                        )
                                ),
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .BorderRadius(4)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("Right Aligned")
                                                .AddItem(new MenuItem("Option 1", () => UpdateAction("Right > Option 1"))
                                                    .TextAlignment(HorizontalAlignment.Right))
                                                .AddItem(new MenuItem("Option 2", () => UpdateAction("Right > Option 2"))
                                                    .TextAlignment(HorizontalAlignment.Right))
                                                .AddItem(new MenuItem("Longer Option", () => UpdateAction("Right > Longer"))
                                                    .TextAlignment(HorizontalAlignment.Right))
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Menu with Actions",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new PaletteFrame(colors => colors.SurfacePressed)
                                .Height(40)
                                .BorderRadius(4)
                                .Content(
                                    new HStack()
                                        .Spacing(0)
                                        .Alignment(Alignment.Center)
                                        .Children(
                                            new Menu("Actions")
                                                .AddItem(new MenuItem("Show Toast", () => {
                                                    Rayo.Controls.ToastService.ShowInfo("Toast triggered from menu!");
                                                    UpdateAction("Toast shown");
                                                })
                                                    .IconOptions(new MenuItemIconOptions(Icons.Notification, new Color(59, 130, 246))))
                                                .AddItem(new MenuItem("Show Dialog", () => {
                                                    Dialog.Show("Menu Dialog", "This dialog was opened from a menu item.");
                                                    UpdateAction("Dialog shown");
                                                })
                                                    .IconOptions(new MenuItemIconOptions(Icons.Info, new Color(234, 179, 8))))
                                                .AddSeparator()
                                                .AddItem(new MenuItem("Log to Console", () => {
                                                    Console.WriteLine("Menu item clicked!");
                                                    UpdateAction("Logged to console");
                                                })
                                                    .IconOptions(new MenuItemIconOptions(Icons.Edit, new Color(168, 85, 247))))
                                        )
                                ),
                            new Label("Click menu items to trigger actions")
                                .FontSize(12)
                                .Foreground(GalleryPalette.Muted)
                        )
                ),

                Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Click menu title to open dropdown"),
                            CreateFeatureItem("Clicking outside closes the menu"),
                            CreateFeatureItem("Only one menu open at a time"),
                            CreateFeatureItem("Menu items support click handlers"),
                            CreateFeatureItem("AddSeparator() groups related menu items"),
                            CreateFeatureItem("Separators work in menus and submenus"),
                            CreateFeatureItem("Hover highlighting on menu items"),
                            CreateFeatureItem("Menu closes after item selection")
                        )
                ),

                Helper.CreateExampleSection("Usage Example",
                    new PaletteFrame(colors => colors.Surface)
                        .BorderRadius(8)
                        .Padding(new Thickness(16))
                        .Content(
                            new VStack()
                                .Spacing(4)
                                .Children(
                                    new Label("// Create a menu with items and separators")
                                        .FontSize(12)
                                        .Foreground(new Color(106, 153, 85)),
                                    new Label("var menu = new Menu(\"File\")")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddItem(new MenuItem(\"New\", OnNew))")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddItem(new MenuItem(\"Open\", OnOpen))")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddSeparator()")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddItem(new MenuItem(\"Save\", OnSave))")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddSeparator()")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddItem(new MenuItem(\"Exit\", OnExit));")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("")
                                        .FontSize(12),
                                    new Label("// Create a menu bar with multiple menus")
                                        .FontSize(12)
                                        .Foreground(new Color(106, 153, 85)),
                                    new Label("new HStack().Children(fileMenu, editMenu, viewMenu)")
                                        .FontSize(12)
                                        .Foreground(new Color(156, 220, 254))
                                )
                        )
                )
            );
    }

    private VisualElement CreateApplicationMenuBar()
    {
        return new VStack()
            .Spacing(0)
            .Children(
                // Menu bar
                new PaletteFrame(colors => colors.SurfacePressed)
                    .Height(32)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(
                        new HStack()
                            .Spacing(0)
                            .Alignment(Alignment.Center)
                            .Children(
                                // App icon/title
                                new HStack()
                                    .Spacing(8)
                                    .Padding(new Thickness(12, 0, 16, 0))
                                    .Children(
                                        new PaletteIcon(Icons.Edit, colors => colors.Primary)
                                            .Size(16),
                                        new PaletteLabel("Rayo Editor", colors => colors.OnSurface)
                                            .FontSize(12)
                                    ),

                                // Menus
                                new Menu("File")
                                    .AddItem(new MenuItem("New File", () => UpdateAction("New File")))
                                    .AddItem(new MenuItem("New Window", () => UpdateAction("New Window")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Open File", () => UpdateAction("Open File")))
                                    .AddItem(new MenuItem("Open Folder", () => UpdateAction("Open Folder")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Save", () => UpdateAction("Save")))
                                    .AddItem(new MenuItem("Save All", () => UpdateAction("Save All")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Close", () => UpdateAction("Close"))),

                                new Menu("Edit")
                                    .AddItem(new MenuItem("Undo", () => UpdateAction("Undo")))
                                    .AddItem(new MenuItem("Redo", () => UpdateAction("Redo")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Cut", () => UpdateAction("Cut")))
                                    .AddItem(new MenuItem("Copy", () => UpdateAction("Copy")))
                                    .AddItem(new MenuItem("Paste", () => UpdateAction("Paste")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Find", () => UpdateAction("Find")))
                                    .AddItem(new MenuItem("Replace", () => UpdateAction("Replace"))),

                                new Menu("Selection")
                                    .AddItem(new MenuItem("Select All", () => UpdateAction("Select All")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Expand Selection", () => UpdateAction("Expand Selection")))
                                    .AddItem(new MenuItem("Shrink Selection", () => UpdateAction("Shrink Selection"))),

                                new Menu("View")
                                    .AddItem(new MenuItem("Command Palette", () => UpdateAction("Command Palette")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("Explorer", () => UpdateAction("Explorer")))
                                    .AddItem(new MenuItem("Search", () => UpdateAction("Search")))
                                    .AddItem(new MenuItem("Terminal", () => UpdateAction("Terminal")))
                                    .AddItem(new MenuItem("Problems", () => UpdateAction("Problems"))),

                                new Menu("Help")
                                    .AddItem(new MenuItem("Welcome", () => UpdateAction("Welcome")))
                                    .AddItem(new MenuItem("Documentation", () => UpdateAction("Documentation")))
                                    .AddItem(new MenuItem("Release Notes", () => UpdateAction("Release Notes")))
                                    .AddSeparator()
                                    .AddItem(new MenuItem("About", () => UpdateAction("About")))
                            )
                    ),

                // Simulated content area
                new PaletteFrame(colors => colors.Background)
                    .Height(120)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(
                        new VStack()
                            .Spacing(8)
                            .Padding(new Thickness(16))
                            .Alignment(Alignment.Center)
                            .Children(
                                new PaletteIcon(Icons.Edit, colors => colors.OnDisabled)
                                    .Size(32),
                                new PaletteLabel("Application Content Area", colors => colors.OnDisabled)
                                    .FontSize(14),
                                new PaletteLabel("Click the menus above to see actions", colors => colors.OnDisabled)
                                    .FontSize(12)
                            )
                    )
            );
    }

    private void UpdateAction(string action)
    {
        _lastAction = action;
        if (_actionLabel != null)
        {
            _actionLabel.Text(action);
            _actionLabel.Foreground(new Color(59, 130, 246));
        }
    }

    private VisualElement CreateFeatureItem(string text)
    {
        return new HStack()
            .Spacing(8)
            .Children(
                new Icon(Icons.Check)
                    .Size(14)
                    .Color(new Color(34, 197, 94)),
                new Label(text)
                    .FontSize(14)
                    .Foreground(GalleryPalette.Muted)
            );
    }
}
