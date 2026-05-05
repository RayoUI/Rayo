using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class MenuPage : UserControl
{
    private string _lastAction = "No action yet";
    private Label? _actionLabel;

    public override VisualElement Build()
    {
        _actionLabel = new Label(_lastAction)
            .FontSize(13)
            .Foreground(new Color(140, 145, 160));

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Menu & MenuItem", "Dropdown menus for application actions and navigation"),

                Helper.CreateExampleSection("Basic Menu",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new Frame()
                                .Height(40)
                                .Background(new Color(45, 45, 48))
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
                                                .AddItem(new MenuItem("Exit", () => UpdateAction("Exit clicked"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Error, new Color(239, 68, 68))))
                                        )
                                ),
                            new HStack()
                                .Spacing(8)
                                .Children(
                                    new Label("Last action:")
                                        .FontSize(13)
                                        .Foreground(new Color(180, 185, 195)),
                                    _actionLabel
                                )
                        )
                ),

                Helper.CreateExampleSection("Multiple Menus (Menu Bar)",
                    new Frame()
                        .Height(40)
                        .Background(new Color(45, 45, 48))
                        .BorderRadius(4)
                        .Content(
                            new HStack()
                                .Spacing(0)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Menu("File")
                                        .AddItem(new MenuItem("New", () => UpdateAction("File > New")))
                                        .AddItem(new MenuItem("Open", () => UpdateAction("File > Open")))
                                        .AddItem(new MenuItem("Save", () => UpdateAction("File > Save")))
                                        .AddItem(new MenuItem("Save As", () => UpdateAction("File > Save As")))
                                        .AddItem(new MenuItem("Exit", () => UpdateAction("File > Exit"))),

                                    new Menu("Edit")
                                        .AddItem(new MenuItem("Undo", () => UpdateAction("Edit > Undo")))
                                        .AddItem(new MenuItem("Redo", () => UpdateAction("Edit > Redo")))
                                        .AddItem(new MenuItem("Cut", () => UpdateAction("Edit > Cut")))
                                        .AddItem(new MenuItem("Copy", () => UpdateAction("Edit > Copy")))
                                        .AddItem(new MenuItem("Paste", () => UpdateAction("Edit > Paste"))),

                                    new Menu("View")
                                        .AddItem(new MenuItem("Zoom In", () => UpdateAction("View > Zoom In")))
                                        .AddItem(new MenuItem("Zoom Out", () => UpdateAction("View > Zoom Out")))
                                        .AddItem(new MenuItem("Full Screen", () => UpdateAction("View > Full Screen"))),

                                    new Menu("Help")
                                        .AddItem(new MenuItem("Documentation", () => UpdateAction("Help > Documentation")))
                                        .AddItem(new MenuItem("About", () => UpdateAction("Help > About")))
                                )
                        )
                ),

                Helper.CreateExampleSection("Menus with Icons",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Frame()
                                .Height(40)
                                .Background(new Color(45, 45, 48))
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
                                                .AddItem(new MenuItem("Stop", () => UpdateAction("Run > Stop"))
                                                    .IconOptions(new MenuItemIconOptions(Icons.Error, new Color(239, 68, 68)))))
                                ),

                            new Label("Menu items can include icons with custom colors and sizes")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160))
                        )
                ),

                Helper.CreateExampleSection("Application-Style Menu Bar",
                    CreateApplicationMenuBar()
                ),

                Helper.CreateExampleSection("Text Alignment",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            new Frame()
                                .Height(40)
                                .Background(new Color(45, 45, 48))
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
                            new Frame()
                                .Height(40)
                                .Background(new Color(45, 45, 48))
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
                            new Frame()
                                .Height(40)
                                .Background(new Color(45, 45, 48))
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
                            new Frame()
                                .Height(40)
                                .Background(new Color(45, 45, 48))
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
                                                .AddItem(new MenuItem("Log to Console", () => {
                                                    Console.WriteLine("Menu item clicked!");
                                                    UpdateAction("Logged to console");
                                                })
                                                    .IconOptions(new MenuItemIconOptions(Icons.Edit, new Color(168, 85, 247))))
                                        )
                                ),
                            new Label("Click menu items to trigger actions")
                                .FontSize(12)
                                .Foreground(new Color(120, 125, 140))
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
                            CreateFeatureItem("Hover highlighting on menu items"),
                            CreateFeatureItem("Menu closes after item selection")
                        )
                ),

                Helper.CreateExampleSection("Usage Example",
                    new Frame()
                        .Background(new Color(30, 33, 42))
                        .BorderRadius(8)
                        .Padding(new Thickness(16))
                        .Content(
                            new VStack()
                                .Spacing(4)
                                .Children(
                                    new Label("// Create a menu with items")
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
                                    new Label("    .AddItem(new MenuItem(\"Save\", OnSave));")
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
                new Frame()
                    .Height(32)
                    .Background(new Color(32, 32, 36))
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
                                        new Icon(Icons.Edit)
                                            .Size(16)
                                            .Color(new Color(59, 130, 246)),
                                        new Label("Rayo Editor")
                                            .FontSize(12)
                                            .Foreground(new Color(200, 205, 215))
                                    ),

                                // Menus
                                new Menu("File")
                                    .AddItem(new MenuItem("New File", () => UpdateAction("New File")))
                                    .AddItem(new MenuItem("New Window", () => UpdateAction("New Window")))
                                    .AddItem(new MenuItem("Open File", () => UpdateAction("Open File")))
                                    .AddItem(new MenuItem("Open Folder", () => UpdateAction("Open Folder")))
                                    .AddItem(new MenuItem("Save", () => UpdateAction("Save")))
                                    .AddItem(new MenuItem("Save All", () => UpdateAction("Save All")))
                                    .AddItem(new MenuItem("Close", () => UpdateAction("Close"))),

                                new Menu("Edit")
                                    .AddItem(new MenuItem("Undo", () => UpdateAction("Undo")))
                                    .AddItem(new MenuItem("Redo", () => UpdateAction("Redo")))
                                    .AddItem(new MenuItem("Cut", () => UpdateAction("Cut")))
                                    .AddItem(new MenuItem("Copy", () => UpdateAction("Copy")))
                                    .AddItem(new MenuItem("Paste", () => UpdateAction("Paste")))
                                    .AddItem(new MenuItem("Find", () => UpdateAction("Find")))
                                    .AddItem(new MenuItem("Replace", () => UpdateAction("Replace"))),

                                new Menu("Selection")
                                    .AddItem(new MenuItem("Select All", () => UpdateAction("Select All")))
                                    .AddItem(new MenuItem("Expand Selection", () => UpdateAction("Expand Selection")))
                                    .AddItem(new MenuItem("Shrink Selection", () => UpdateAction("Shrink Selection"))),

                                new Menu("View")
                                    .AddItem(new MenuItem("Command Palette", () => UpdateAction("Command Palette")))
                                    .AddItem(new MenuItem("Explorer", () => UpdateAction("Explorer")))
                                    .AddItem(new MenuItem("Search", () => UpdateAction("Search")))
                                    .AddItem(new MenuItem("Terminal", () => UpdateAction("Terminal")))
                                    .AddItem(new MenuItem("Problems", () => UpdateAction("Problems"))),

                                new Menu("Help")
                                    .AddItem(new MenuItem("Welcome", () => UpdateAction("Welcome")))
                                    .AddItem(new MenuItem("Documentation", () => UpdateAction("Documentation")))
                                    .AddItem(new MenuItem("Release Notes", () => UpdateAction("Release Notes")))
                                    .AddItem(new MenuItem("About", () => UpdateAction("About")))
                            )
                    ),

                // Simulated content area
                new Frame()
                    .Height(120)
                    .Background(new Color(25, 25, 30))
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(
                        new VStack()
                            .Spacing(8)
                            .Padding(new Thickness(16))
                            .Alignment(Alignment.Center)
                            .Children(
                                new Icon(Icons.Edit)
                                    .Size(32)
                                    .Color(new Color(80, 85, 100)),
                                new Label("Application Content Area")
                                    .FontSize(14)
                                    .Foreground(new Color(100, 105, 120)),
                                new Label("Click the menus above to see actions")
                                    .FontSize(12)
                                    .Foreground(new Color(80, 85, 100))
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
                    .Foreground(new Color(180, 185, 195))
            );
    }
}
