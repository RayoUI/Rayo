using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class TreeViewPage : UserControl
{
    private Label? _statusLabel;
    private TreeView? _searchableTree;

    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(30)
            .Padding(new Thickness(20))
            .Children(
                CreateHighlightModesExample(),
                CreateFileExplorerExample(),
                CreateCustomIconsExample(),
                CreateCheckboxesExample(),
                CreateEventsExample(),
                CreateSearchExample(),
                CreateDisabledNodesExample()
            );
    }

    /// <summary>
    /// Example: Highlight Modes - Compact vs Stretch
    /// </summary>
    private VisualElement CreateHighlightModesExample()
    {
        // Compact mode tree (default)
        var compactTree = new TreeView()
            .Width(350)
            .Height(250)
            .SelectedColor(new Color(59, 130, 246, 0.3f))
            .HoverColor(new Color(59, 130, 246, 0.1f))
            .SelectedTextColor(Color.Black)
            .HighlightMode(SelectionHighlightMode.Compact)
            .ItemHeight(28);

        // Stretch mode tree
        var stretchTree = new TreeView()
            .Width(350)
            .Height(250)
            .SelectedColor(new Color(59, 130, 246, 0.3f))
            .HoverColor(new Color(59, 130, 246, 0.1f))
            .SelectedTextColor(Color.Black)
            .HighlightMode(SelectionHighlightMode.Stretch)
            .ItemHeight(28);

        // Build same tree structure for both
        void BuildSampleTree(TreeView tree)
        {
            var uiNode = new TreeNode("UI Components")
            {
                Icon = Icons.Folder,
                IsExpanded = true
            };

            var controls = uiNode.AddChild("Controls");
            controls.Icon = Icons.Folder;
            controls.IsExpanded = true;
            
            var button = controls.AddChild("Button");
            button.Icon = Icons.File;
            
            var label = controls.AddChild("Label");
            label.Icon = Icons.File;
            
            var textBox = controls.AddChild("TextBox");
            textBox.Icon = Icons.File;

            var layout = uiNode.AddChild("Layout");
            layout.Icon = Icons.Folder;
            
            var stack = layout.AddChild("Stack");
            stack.Icon = Icons.File;
            
            var grid = layout.AddChild("Grid");
            grid.Icon = Icons.File;

            tree.AddRootNode(uiNode);

            var data = new TreeNode("Data")
            {
                Icon = Icons.Folder,
                IsExpanded = true
            };
            
            var models = data.AddChild("Models");
            models.Icon = Icons.File;
            
            var services = data.AddChild("Services");
            services.Icon = Icons.File;
            
            tree.AddRootNode(data);
        }

        BuildSampleTree(compactTree);
        BuildSampleTree(stretchTree);

        return Helper.CreateExampleSection("Highlight Modes",
            new HStack()
                .Spacing(20)
                .Children(
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label()
                                .Text("Compact Mode (Default)")
                                .Foreground(new Color(100, 100, 100))
                                .FontSize(14),
                            new Label()
                                .Text("Highlight wraps content")
                                .Foreground(new Color(120, 120, 120))
                                .FontSize(12),
                            compactTree
                        ),
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label()
                                .Text("Stretch Mode")
                                .Foreground(new Color(100, 100, 100))
                                .FontSize(14),
                            new Label()
                                .Text("Highlight spans full width")
                                .Foreground(new Color(120, 120, 120))
                                .FontSize(12),
                            stretchTree
                        )
                )
        );
    }

    /// <summary>
    /// Example 1: File Explorer with proper icons
    /// </summary>
    private VisualElement CreateFileExplorerExample()
    {
        var tree = new TreeView()
            .Width(400)
            .Height(300)
            .SelectedColor(new Color(59, 130, 246))
            .HoverColor(new Color(240, 240, 245))
            .ChevronSize(16f)
            .NodeIconSize(18f);

        // Build tree structure with custom icons
        var documentsNode = new TreeNode("Documents")
        {
            Icon = Icons.Folder,
            IsExpanded = true
        };
        
        var file1 = documentsNode.AddChild("Report.pdf");
        file1.Icon = Icons.File;
        file1.Tag = "pdf";

        var file2 = documentsNode.AddChild("Presentation.pptx");
        file2.Icon = Icons.File;
        file2.Tag = "presentation";

        var subfolderNode = documentsNode.AddChild("Images");
        subfolderNode.Icon = Icons.Folder;
        var img1 = subfolderNode.AddChild("Photo1.jpg");
        img1.Icon = Icons.Image;
        var img2 = subfolderNode.AddChild("Photo2.png");
        img2.Icon = Icons.Image;

        var picturesNode = new TreeNode("Pictures")
        {
            Icon = Icons.Folder
        };
        var camera1 = picturesNode.AddChild("Vacation.jpg");
        camera1.Icon = Icons.Camera;
        var camera2 = picturesNode.AddChild("Birthday.jpg");
        camera2.Icon = Icons.Camera;

        var projectsNode = new TreeNode("Projects")
        {
            Icon = Icons.Folder,
            IsExpanded = true
        };
        
            var pikoUi = projectsNode.AddChild("Rayo");
            pikoUi.Icon = Icons.Folder;
            var components = pikoUi.AddChild("Components");
        components.Icon = Icons.Folder;
            var layout = pikoUi.AddChild("Layout");
        layout.Icon = Icons.Folder;
            var core = pikoUi.AddChild("Core");
        core.Icon = Icons.Folder;

        tree.AddRootNode(documentsNode);
        tree.AddRootNode(picturesNode);
        tree.AddRootNode(projectsNode);

        // Event handlers
        tree.OnNodeSelected(node =>
        {
            Console.WriteLine($"Selected: {node.Text}");
        });

        return Helper.CreateExampleSection("File Explorer with Icons",
            new VStack()
                .Spacing(10)
                .Children(
                    new Label()
                        .Text("Tree with folder/file icons from Icons.cs")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    tree,
                    new HStack()
                        .Spacing(10)
                        .Children(
                            new Button()
                                .Text("Expand All")
                                .Background(ColorDefault.Primary)
                                .TextColor(Color.White)
                                .OnTapped(() => tree.ExpandAll()),
                            new Button()
                                .Text("Collapse All")
                                .Background(ColorDefault.Secondary)
                                .TextColor(Color.White)
                                .OnTapped(() => tree.CollapseAll())
                        )
                )
        );
    }

    /// <summary>
    /// Example 2: Custom Icons for different node types
    /// </summary>
    private VisualElement CreateCustomIconsExample()
    {
        var tree = new TreeView()
            .Width(400)
            .Height(250)
            .SelectedColor(new Color(16, 185, 129))
            .HoverColor(new Color(236, 253, 245))
            .NodeIconSize(20f)
            .ChevronSize(14f);

        // System nodes with custom icons
        var systemNode = new TreeNode("System")
        {
            Icon = Icons.Settings,
            IsExpanded = true
        };

        var usersNode = systemNode.AddChild("Users");
        usersNode.Icon = Icons.Person;
        var user1 = usersNode.AddChild("Admin");
        user1.Icon = Icons.Person;
        var user2 = usersNode.AddChild("Guest");
        user2.Icon = Icons.Person;

        var securityNode = systemNode.AddChild("Security");
        securityNode.Icon = Icons.Lock;
        var locked = securityNode.AddChild("Encrypted Files");
        locked.Icon = Icons.Lock;
        var unlocked = securityNode.AddChild("Public Files");
        unlocked.Icon = Icons.Unlock;

        var notificationsNode = systemNode.AddChild("Notifications");
        notificationsNode.Icon = Icons.Notification;
        var alert1 = notificationsNode.AddChild("System Update");
        alert1.Icon = Icons.Info;
        var alert2 = notificationsNode.AddChild("Warning: Low Disk Space");
        alert2.Icon = Icons.Warning;
        var alert3 = notificationsNode.AddChild("Error: Failed Backup");
        alert3.Icon = Icons.Error;

        tree.AddRootNode(systemNode);

        return Helper.CreateExampleSection("Custom Icons",
            new VStack()
                .Spacing(10)
                .Children(
                    new Label()
                        .Text("Different icon types: Settings, Person, Lock, Notifications, Info, Warning, Error")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    tree
                )
        );
    }

    /// <summary>
    /// Example 3: TreeView with checkboxes
    /// </summary>
    private VisualElement CreateCheckboxesExample()
    {
        var tree = new TreeView()
            .Width(400)
            .Height(280)
            .SelectedColor(new Color(139, 92, 246))
            .HoverColor(new Color(245, 243, 255))
            .ShowCheckboxes(true);

        var tasksNode = new TreeNode("Tasks")
        {
            Icon = Icons.Folder,
            IsExpanded = true,
            IsChecked = true
        };

        var todo1 = tasksNode.AddChild("Complete documentation");
        todo1.Icon = Icons.File;
        todo1.IsChecked = true;

        var todo2 = tasksNode.AddChild("Write unit tests");
        todo2.Icon = Icons.File;
        todo2.IsChecked = false;

        var bugfixesNode = tasksNode.AddChild("Bug Fixes");
        bugfixesNode.Icon = Icons.Folder;
        var bug1 = bugfixesNode.AddChild("Fix memory leak");
        bug1.Icon = Icons.Error;
        bug1.IsChecked = false;
        var bug2 = bugfixesNode.AddChild("Resolve UI glitch");
        bug2.Icon = Icons.Warning;
        bug2.IsChecked = true;

        var featuresNode = new TreeNode("Features")
        {
            Icon = Icons.Star,
            IsExpanded = true
        };

        var feature1 = featuresNode.AddChild("Add dark mode");
        feature1.Icon = Icons.Settings;
        feature1.IsChecked = false;

        var feature2 = featuresNode.AddChild("Implement search");
        feature2.Icon = Icons.Search;
        feature2.IsChecked = true;

        tree.AddRootNode(tasksNode);
        tree.AddRootNode(featuresNode);

        var checkedLabel = new Label()
            .Text("Checked items: 3")
            .Foreground(new Color(100, 100, 100))
            .FontSize(14);

        tree.OnNodeCheckedChanged((node, isChecked) =>
        {
            int count = CountCheckedNodes(tree.RootNodes);
            checkedLabel.Text($"Checked items: {count}");
        });

        return Helper.CreateExampleSection("Checkboxes",
            new VStack()
                .Spacing(10)
                .Children(
                    new Label()
                        .Text("Tree with checkboxes enabled (ShowCheckboxes = true)")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    tree,
                    checkedLabel
                )
        );
    }

    private int CountCheckedNodes(List<TreeNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node.IsChecked) count++;
            count += CountCheckedNodes(node.Children);
        }
        return count;
    }

    /// <summary>
    /// Example 4: Events and Interaction
    /// </summary>
    private VisualElement CreateEventsExample()
    {
        var tree = new TreeView()
            .Width(400)
            .Height(200)
            .SelectedColor(new Color(236, 72, 153))
            .HoverColor(new Color(253, 242, 248));

        _statusLabel = new Label()
            .Text("Status: Ready")
            .Foreground(new Color(100, 100, 100))
            .FontSize(14);

        // Build simple tree
        var root = new TreeNode("Root Node")
        {
            Icon = Icons.Home,
            IsExpanded = true
        };

        var child1 = root.AddChild("Child 1");
        child1.Icon = Icons.File;
        var child2 = root.AddChild("Child 2");
        child2.Icon = Icons.File;
        var child3 = root.AddChild("Child 3");
        child3.Icon = Icons.File;

        tree.AddRootNode(root);

        // Event handlers
        tree.OnNodeSelected(node =>
        {
            _statusLabel?.Text($"Selected: {node.Text}");
        });

        tree.OnNodeDoubleClicked(node =>
        {
            _statusLabel?.Text($"Double-clicked: {node.Text}");
        });

        tree.OnNodeExpanded((node, isExpanded) =>
        {
            string action = isExpanded ? "expanded" : "collapsed";
            _statusLabel?.Text($"Node {action}: {node.Text}");
        });

        return Helper.CreateExampleSection("Events",
            new VStack()
                .Spacing(10)
                .Children(
                    new Label()
                        .Text("Try: single click, double click, expand/collapse")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    tree,
                    _statusLabel
                )
        );
    }

    /// <summary>
    /// Example 5: Search and Selection
    /// </summary>
    private VisualElement CreateSearchExample()
    {
        _searchableTree = new TreeView()
            .Width(400)
            .Height(280)
            .SelectedColor(new Color(245, 158, 11))
            .HoverColor(new Color(254, 252, 232));

        // Build a larger tree for searching
        var animals = new TreeNode("Animals")
        {
            Icon = Icons.Folder,
            IsExpanded = true
        };

        var mammals = animals.AddChild("Mammals");
        mammals.Icon = Icons.Folder;
        var dogNode = mammals.AddChild("Dog");
        dogNode.Icon = Icons.Star;
        dogNode.Tag = "mammal";
        var catNode = mammals.AddChild("Cat");
        catNode.Icon = Icons.Star;
        catNode.Tag = "mammal";
        var elephantNode = mammals.AddChild("Elephant");
        elephantNode.Icon = Icons.Star;
        elephantNode.Tag = "mammal";

        var birds = animals.AddChild("Birds");
        birds.Icon = Icons.Folder;
        var eagleNode = birds.AddChild("Eagle");
        eagleNode.Icon = Icons.Star;
        eagleNode.Tag = "bird";
        var parrotNode = birds.AddChild("Parrot");
        parrotNode.Icon = Icons.Star;
        parrotNode.Tag = "bird";
        var penguinNode = birds.AddChild("Penguin");
        penguinNode.Icon = Icons.Star;
        penguinNode.Tag = "bird";

        var reptiles = animals.AddChild("Reptiles");
        reptiles.Icon = Icons.Folder;
        var snakeNode = reptiles.AddChild("Snake");
        snakeNode.Icon = Icons.Star;
        snakeNode.Tag = "reptile";
        var lizardNode = reptiles.AddChild("Lizard");
        lizardNode.Icon = Icons.Star;
        lizardNode.Tag = "reptile";
        var turtleNode = reptiles.AddChild("Turtle");
        turtleNode.Icon = Icons.Star;
        turtleNode.Tag = "reptile";

        _searchableTree.AddRootNode(animals);

        var searchButtons = new HStack()
            .Spacing(10)
            .Children(
                new Button()
                    .Text("Find Dog")
                    .Background(ColorDefault.Primary)
                    .TextColor(Color.White)
                    .OnTapped(() =>
                    {
                        if (_searchableTree != null)
                        {
                            var found = _searchableTree.FindNode(n => n.Text == "Dog");
                            if (found != null) _searchableTree.SelectAndReveal(found);
                        }
                    }),
                new Button()
                    .Text("Find Eagle")
                    .Background(ColorDefault.Secondary)
                    .TextColor(Color.White)
                    .OnTapped(() =>
                    {
                        if (_searchableTree != null)
                        {
                            var found = _searchableTree.FindNode(n => n.Text == "Eagle");
                            if (found != null) _searchableTree.SelectAndReveal(found);
                        }
                    }),
                new Button()
                    .Text("Find Snake")
                    .Background(ColorDefault.Success)
                    .TextColor(Color.White)
                    .OnTapped(() =>
                    {
                        if (_searchableTree != null)
                        {
                            var found = _searchableTree.FindNode(n => n.Text == "Snake");
                            if (found != null) _searchableTree.SelectAndReveal(found);
                        }
                    })
            );

        return Helper.CreateExampleSection("Search & Selection",
            new VStack()
                .Spacing(10)
                .Children(
                    new Label()
                        .Text("Click buttons to find and reveal specific animals")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    searchButtons,
                    _searchableTree
                )
        );
    }

    /// <summary>
    /// Example 6: Disabled Nodes
    /// </summary>
    private VisualElement CreateDisabledNodesExample()
    {
        var tree = new TreeView()
            .Width(400)
            .Height(200)
            .SelectedColor(new Color(99, 102, 241))
            .DisabledTextColor(new Color(200, 200, 200));

        var menu = new TreeNode("Menu")
        {
            Icon = Icons.Menu,
            IsExpanded = true
        };

        var item1 = menu.AddChild("Available Option");
        item1.Icon = Icons.Check;
        item1.IsEnabled = true;

        var item2 = menu.AddChild("Disabled Option");
        item2.Icon = Icons.Close;
        item2.IsEnabled = false;

        var submenu = menu.AddChild("Submenu");
        submenu.Icon = Icons.Folder;
        var sub1 = submenu.AddChild("Active");
        sub1.Icon = Icons.Check;
        sub1.IsEnabled = true;
        var sub2 = submenu.AddChild("Locked");
        sub2.Icon = Icons.Lock;
        sub2.IsEnabled = false;

        tree.AddRootNode(menu);

        return Helper.CreateExampleSection("Disabled Nodes",
            new VStack()
                .Spacing(10)
                .Children(
                    new Label()
                        .Text("Some nodes are disabled (IsEnabled = false)")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(13),
                    tree,
                    new Label()
                        .Text("💡 Disabled nodes cannot be selected or interacted with")
                        .Foreground(new Color(100, 100, 100))
                        .FontSize(12)
                )
        );
    }
}
