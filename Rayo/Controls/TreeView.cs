namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Defines how the selection highlight is rendered in the tree view
/// </summary>
public enum SelectionHighlightMode
{
    /// <summary>
    /// Highlight only covers the content (icon + text) with padding
    /// </summary>
    Compact,
    
    /// <summary>
    /// Highlight stretches across the full horizontal width like a cell
    /// </summary>
    Stretch
}

/// <summary>
/// Tree node data with extended functionality
/// </summary>
public class TreeNode
{
    public string Text { get; set; } = "";
    public object? Tag { get; set; }
    public List<TreeNode> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = false;
    public bool IsSelected { get; set; } = false;
    public bool IsCheckable { get; set; } = false;
    public bool IsChecked { get; set; } = false;
    public TreeNode? Parent { get; set; }

    /// <summary>
    /// Custom icon for this node. If null, uses default folder/file icons.
    /// </summary>
    public IconData? Icon { get; set; } = null;

    /// <summary>
    /// Custom template for rendering this node. If null, uses default template.
    /// </summary>
    public Func<TreeNode, TreeView, VisualElement>? CustomTemplate { get; set; } = null;

    /// <summary>
    /// Whether this node is enabled for interaction
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Custom data for user-specific purposes
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public TreeNode(string text, object? tag = null)
    {
        Text = text;
        Tag = tag;
    }

    public TreeNode AddChild(TreeNode child)
    {
        child.Parent = this;
        Children.Add(child);
        return this;
    }

    public TreeNode AddChild(string text, object? tag = null)
    {
        var child = new TreeNode(text, tag);
        AddChild(child);
        return child;
    }

    public bool HasChildren => Children.Count > 0;

    public int GetDepth()
    {
        int depth = 0;
        TreeNode? current = Parent;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }

    /// <summary>
    /// Finds all ancestor nodes from this node up to root
    /// </summary>
    public List<TreeNode> GetAncestors()
    {
        var ancestors = new List<TreeNode>();
        TreeNode? current = Parent;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }
        return ancestors;
    }

    /// <summary>
    /// Gets all descendant nodes recursively
    /// </summary>
    public List<TreeNode> GetDescendants()
    {
        var descendants = new List<TreeNode>();
        foreach (var child in Children)
        {
            descendants.Add(child);
            descendants.AddRange(child.GetDescendants());
        }
        return descendants;
    }
}

/// <summary>
/// Visual representation of a tree node with modern icon support
/// </summary>
internal class TreeNodeView : CompositeView<TreeNodeView>
{
    private readonly TreeNode _node;
    private readonly TreeView _treeView;
    private VStack? _layout;
    private VStack? _childrenContainer;
    private TreeNodeHeaderButton? _headerButton;
    private bool _isSelected;

    public TreeNode Node => _node;

    public TreeNodeView(TreeNode node, TreeView treeView)
    {
        _node = node;
        _treeView = treeView;
        BuildComponents();
    }

    private void BuildComponents()
    {
        _layout = new VStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        AddChild(_layout);

        // Use custom template if provided, otherwise use default button
        if (_node.CustomTemplate != null)
        {
            var customContent = _node.CustomTemplate(_node, _treeView);
            _layout.AddChild(customContent);
        }
        else
        {
            _headerButton = new TreeNodeHeaderButton(this);
            _headerButton.Tapped += _ => OnNodeClicked();
            _layout.AddChild(_headerButton);
        }

        _childrenContainer = new VStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        if (_node.IsExpanded)
        {
            _layout.AddChild(_childrenContainer);
            RebuildChildren();
        }

        RefreshVisuals();
    }

    private void OnNodeClicked()
    {
        if (!_node.IsEnabled) return;

        if (_node.HasChildren)
        {
            _node.IsExpanded = !_node.IsExpanded;
            UpdateExpandedState();
            _treeView.NotifyNodeExpanded(_node, _node.IsExpanded);
        }

        _treeView.SelectNode(_node);
    }

    private void UpdateExpandedState()
    {
        if (_layout == null || _childrenContainer == null) return;

        if (_node.IsExpanded)
        {
            if (_childrenContainer.Parent == null)
            {
                _layout.AddChild(_childrenContainer);
            }
            RebuildChildren();
        }
        else if (_childrenContainer.Parent != null)
        {
            _layout.RemoveChild(_childrenContainer);
        }

        _headerButton?.RefreshContent();
        MarkNeedsLayout();
    }

    private void RebuildChildren()
    {
        if (_childrenContainer == null) return;

        _childrenContainer.ClearChildren();

        foreach (var child in _node.Children.ToArray())
        {
            var childView = new TreeNodeView(child, _treeView);
            _childrenContainer.AddChild(childView);
            _treeView.RegisterNodeView(child, childView);
        }
    }

    public void RefreshVisuals()
    {
        IsSelected = _treeView.SelectedNode == _node;
    }

    internal bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            _headerButton?.UpdateSelectionState(value);
            _headerButton?.RefreshContent();
        }
    }

    internal Brush GetTextColor() => _isSelected ? _treeView.SelectedTextColor : _treeView.TextColor;

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (_layout != null)
        {
            _layout.Measure(availableWidth, availableHeight);
            DesiredWidth = _layout.DesiredWidth;
            DesiredHeight = _layout.DesiredHeight;
        }
        else
        {
            DesiredWidth = availableWidth;
            DesiredHeight = _treeView.ItemHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_layout != null)
        {
            _layout.Arrange(x, y, width, height);
        }
    }

    public override void Render(IRenderer renderer)
    {
        _layout?.Render(renderer);
    }

    private sealed class TreeNodeHeaderButton : Rayo.Core.View<TreeNodeHeaderButton>,
        IPointerHandler,
        ITappable,
        IGestureRecognizerHost
    {
        private readonly TreeNodeView _owner;
        private bool _isSelected;
        private bool _isHovered;
        private bool _isPressed;
        private readonly TapRecognizer _tapRecognizer;

        public List<IGestureRecognizer> GestureRecognizers { get; } = new();
        public event Action<TapGestureEventArgs>? Tapped;

        public TreeNodeHeaderButton(TreeNodeView owner)
        {
            _owner = owner;
            Padding = new Thickness(0);
            HorizontalAlignment = HorizontalAlignment.Stretch;

            _tapRecognizer = new TapRecognizer(
                maxMovementThreshold: 15f,
                maxPressDurationMs: 500,
                doubleTapWindowMs: 300
            );
            _tapRecognizer.TapDetected += OnTapDetected;
            GestureRecognizers.Add(_tapRecognizer);
        }

        private void OnTapDetected(TapGestureEventArgs e)
        {
            // Handle double tap for expand/collapse
            if (e.TapCount == 2 && _owner._node.HasChildren)
            {
                _owner._node.IsExpanded = !_owner._node.IsExpanded;
                _owner.UpdateExpandedState();
                _owner._treeView.NotifyNodeExpanded(_owner._node, _owner._node.IsExpanded);
            }

            Tapped?.Invoke(e);
        }

        public void OnPointerEntered(PointerEventArgs e)
        {
            if (e.PointerType == PointerType.Mouse)
            {
                _isHovered = true;
                MarkNeedsPaint();
            }
        }

        public void OnPointerExited(PointerEventArgs e)
        {
            if (e.PointerType == PointerType.Mouse)
            {
                _isHovered = false;
                MarkNeedsPaint();
            }
        }

        public void OnPointerPressed(PointerEventArgs e)
        {
            _isPressed = true;
            MarkNeedsPaint();
        }

        public void OnPointerReleased(PointerEventArgs e)
        {
            _isPressed = false;
            MarkNeedsPaint();
        }

        public void RefreshContent()
        {
            MarkNeedsPaint();
        }

        public void UpdateSelectionState(bool isSelected)
        {
            if (_isSelected != isSelected)
            {
                _isSelected = isSelected;
                MarkNeedsPaint();
            }
        }

        public override void Measure(float availableWidth, float availableHeight)
        {
            float width = availableWidth;
            if (float.IsPositiveInfinity(width))
            {
                width = _owner.Width > 0 ? _owner.Width : 300;
            }

            DesiredWidth = Math.Max(0, width);
            DesiredHeight = Math.Max(_owner._treeView.ItemHeight, 0);
        }

        public override void Render(IRenderer renderer)
        {
            var treeView = _owner._treeView;
            var node = _owner._node;
            float indent = node.GetDepth() * treeView.IndentSize;

            // Calculate positions
            float currentX = ComputedX + indent;
            float centerY = ComputedY + ComputedHeight / 2f;

            // Determine icon sizes
            float chevronSize = treeView.ChevronSize;
            float nodeIconSize = treeView.NodeIconSize;
            float spacing = treeView.IconSpacing;

            // Measure text
            string text = node.Text ?? string.Empty;
            float availableTextWidth = Math.Max(0, ComputedWidth - (currentX - ComputedX));

            // Subtract icon widths from available space
            if (node.HasChildren)
            {
                availableTextWidth -= chevronSize + spacing;
            }
            else
            {
                availableTextWidth -= nodeIconSize + spacing;
            }

            string displayText = renderer.TruncateTextToFit(text, availableTextWidth, treeView.FontSize);
            var textMeasure = renderer.MeasureText(displayText, treeView.FontSize);

            // Calculate highlight based on mode
            float highlightX;
            float highlightY = ComputedY;
            float highlightWidth;
            float highlightHeight = ComputedHeight;
            float highlightPadding = 6f;

            if (treeView.HighlightMode == SelectionHighlightMode.Stretch)
            {
                // Stretch mode: highlight spans full width from indent to edge
                highlightX = ComputedX + indent;
                highlightWidth = Math.Max(0, ComputedWidth - indent);
            }
            else
            {
                // Compact mode: highlight only covers content
                float contentWidth = 0f;
                if (node.HasChildren)
                {
                    contentWidth = chevronSize + spacing + textMeasure.X;
                }
                else
                {
                    contentWidth = nodeIconSize + spacing + textMeasure.X;
                }

                highlightX = currentX;
                highlightWidth = Math.Min(Math.Max(0, contentWidth + highlightPadding * 2), Math.Max(0, ComputedWidth - indent - 4));
            }

            // Determine highlight color
            Brush highlightColor = Color.Transparent;
            if (_isSelected)
            {
                highlightColor = treeView.SelectedColor;
            }
            else if (_isPressed)
            {
                highlightColor = treeView.PressedColor;
            }
            else if (_isHovered)
            {
                highlightColor = treeView.HoverColor;
            }

            // Draw highlight
            if (highlightColor.PrimaryColor.A > 0)
            {
                renderer.DrawRoundedRect(highlightX, highlightY, highlightWidth, highlightHeight, 4, highlightColor);
            }

            Brush iconColor = _owner.GetTextColor();
            Brush textColor = _owner.GetTextColor();

            // Draw chevron or node icon
            if (node.HasChildren)
            {
                // Draw chevron using Icon
                IconData chevron = node.IsExpanded ? Icons.ChevronDown : Icons.ChevronRight;
                float chevronY = centerY - chevronSize / 2f;
                
                // Render icon
                var chevronView = new Icon(chevron)
                {
                    Width = chevronSize,
                    Height = chevronSize,
                    Color = iconColor.PrimaryColor
                };
                
                chevronView.Measure(chevronSize, chevronSize);
                chevronView.Arrange(currentX, chevronY, chevronSize, chevronSize);
                chevronView.Render(renderer);

                currentX += chevronSize + spacing;
            }
            else
            {
                // Draw node icon (custom or default file icon)
                IconData nodeIcon = node.Icon ?? Icons.File;
                float iconY = centerY - nodeIconSize / 2f;

                var iconView = new Icon(nodeIcon)
                {
                    Width = nodeIconSize,
                    Height = nodeIconSize,
                    Color = iconColor.PrimaryColor
                };

                iconView.Measure(nodeIconSize, nodeIconSize);
                iconView.Arrange(currentX, iconY, nodeIconSize, nodeIconSize);
                iconView.Render(renderer);

                currentX += nodeIconSize + spacing;
            }

            // Draw text
            float textY = centerY - textMeasure.Y / 2f;
            renderer.DrawText(displayText, currentX, textY, textColor, treeView.FontSize);

            // Draw checkbox if node is checkable
            if (node.IsCheckable)
            {
                float checkboxSize = 16f;
                float checkboxX = ComputedX + ComputedWidth - checkboxSize - 8f;
                float checkboxY = centerY - checkboxSize / 2f;

                Brush checkboxColor = node.IsChecked ? treeView.SelectedColor : treeView.BorderColor;
                renderer.DrawRoundedRect(checkboxX, checkboxY, checkboxSize, checkboxSize, 3, checkboxColor);

                if (node.IsChecked)
                {
                    // Draw checkmark
                    float checkIconSize = checkboxSize - 4;
                    var checkIcon = new Icon(Icons.Check)
                    {
                        Width = checkIconSize,
                        Height = checkIconSize,
                        Color = Color.White
                    };
                    checkIcon.Measure(checkIconSize, checkIconSize);
                    checkIcon.Arrange(checkboxX + 2, checkboxY + 2, checkIconSize, checkIconSize);
                    checkIcon.Render(renderer);
                }
            }
        }
    }
}

/// <summary>
/// TreeView component - Hierarchical data display with expand/collapse
/// Extended with icon support, customization, and modern events
/// </summary>
public class TreeView : CompositeView<TreeView>
{
    public List<TreeNode> RootNodes
    {
        get => field;
        set => this.SetProperty(ref field, value ?? new List<TreeNode>(), RebuildTree);
    } = new();
    
    private TreeNode? _selectedNode = null;
    private VStack? _treeContainer;
    private ScrollView? _scrollView;
    private Frame? _rootFrame;
    private readonly Dictionary<TreeNode, TreeNodeView> _nodeViews = new();

    // =========================================================================
    // STYLING PROPERTIES
    // =========================================================================
    public new Brush Background
    {
        get => base.Background;
        set
        {
            base.Background = value;
            ApplyStyles();
        }
    }
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplyStyles);
    } = new Color(220, 220, 220);

    [PaintProperty]
    public Brush SelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = new Color(0, 120, 215);

    [PaintProperty]
    public Brush SelectedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.White;

    [PaintProperty]
    public Brush HoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = new Color(240, 240, 240);

    [PaintProperty]
    public Brush PressedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = new Color(220, 220, 220);

    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Black;

    [PaintProperty]
    public Brush DisabledTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = new Color(150, 150, 150);

    [LayoutProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplyStyles);
    } = 1;
    
    [LayoutProperty]
    public float ItemHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 28f;

    [LayoutProperty]
    public float IndentSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 20f;

    /// <summary>
    /// Size of chevron icons (expand/collapse arrows)
    /// </summary>
    [LayoutProperty]
    public float ChevronSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 14f;

    /// <summary>
    /// Size of node icons (folder, file, custom icons)
    /// </summary>
    [LayoutProperty]
    public float NodeIconSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 16f;

    [LayoutProperty]
    public float IconSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 8f;

    [LayoutProperty]
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 14f;

    /// <summary>
    /// Defines how the selection highlight is rendered
    /// </summary>
    [LayoutProperty]
    public SelectionHighlightMode HighlightMode
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = SelectionHighlightMode.Compact;

    /// <summary>
    /// Whether to show checkboxes for all nodes
    /// </summary>
    [LayoutProperty]
    public bool ShowCheckboxes
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildTree);
    } = false;

    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>
    /// Fired when a node is selected
    /// </summary>
    public event Action<TreeNode>? NodeSelected;

    /// <summary>
    /// Fired when a node is expanded or collapsed
    /// </summary>
    public event Action<TreeNode, bool>? NodeExpanded;

#pragma warning disable CS0067
    /// <summary>
    /// Fired when a node is double-clicked
    /// </summary>
    public event Action<TreeNode>? NodeDoubleClicked;

    /// <summary>
    /// Fired when a node is right-clicked
    /// </summary>
    public event Action<TreeNode>? NodeRightClicked;

    /// <summary>
    /// Fired when a node's checkbox state changes
    /// </summary>
    public event Action<TreeNode, bool>? NodeCheckedChanged;

    /// <summary>
    /// Fired before a node is expanded (can be cancelled)
    /// </summary>
    public event Func<TreeNode, bool>? NodeExpanding;

    /// <summary>
    /// Fired before a node is collapsed (can be cancelled)
    /// </summary>
    public event Func<TreeNode, bool>? NodeCollapsing;
#pragma warning restore CS0067

    [NotFluent]
    public TreeNode? SelectedNode => _selectedNode;

    public TreeView()
    {
        Background = Color.White;
        BuildComponents();
        ApplyStyles();
    }

    private void BuildComponents()
    {
        _treeContainer = new VStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        _scrollView = new ScrollView()
            .Content(_treeContainer)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        _rootFrame = new Frame()
            .Background(Background)
            .BorderColor(BorderColor.PrimaryColor)
            .BorderWidth(BorderWidth)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(_scrollView);

        AddChild(_rootFrame);
    }

    private void ApplyStyles()
    {
        _rootFrame?
            .Background(Background)
            .BorderColor(BorderColor.PrimaryColor)
            .BorderWidth(BorderWidth);
    }

    private void UpdateNodeVisuals()
    {
        foreach (var view in _nodeViews.Values)
        {
            view.RefreshVisuals();
        }
        MarkNeedsPaint();
    }

    // =========================================================================
    // FLUENT API
    // =========================================================================

    public TreeView AddRootNode(TreeNode node)
    {
        RootNodes = [..RootNodes, node];
        
        // Apply ShowCheckboxes setting to new node
        if (ShowCheckboxes)
        {
            node.IsCheckable = true;
            foreach (var descendant in node.GetDescendants())
            {
                descendant.IsCheckable = true;
            }
        }
        
        RebuildTree();
        return this;
    }

    public TreeView AddRootNode(string text, object? tag = null)
    {
        var node = new TreeNode(text, tag);
        return AddRootNode(node);
    }

    public TreeView Clear()
    {
        RootNodes = new List<TreeNode>();
        _selectedNode = null;
        _nodeViews.Clear();
        RebuildTree();
        return this;
    }

    public TreeView ExpandAll()
    {
        foreach (var node in RootNodes)
        {
            ExpandNodeRecursive(node);
        }
        RebuildTree();
        return this;
    }

    public TreeView CollapseAll()
    {
        foreach (var node in RootNodes)
        {
            CollapseNodeRecursive(node);
        }
        RebuildTree();
        return this;
    }

    /// <summary>
    /// Expands a specific node and optionally all its ancestors to make it visible
    /// </summary>
    public TreeView ExpandNode(TreeNode node, bool expandAncestors = true)
    {
        if (expandAncestors)
        {
            foreach (var ancestor in node.GetAncestors())
            {
                ancestor.IsExpanded = true;
            }
        }
        
        node.IsExpanded = true;
        RebuildTree();
        return this;
    }

    /// <summary>
    /// Finds a node by predicate
    /// </summary>
    public TreeNode? FindNode(Func<TreeNode, bool> predicate)
    {
        foreach (var root in RootNodes)
        {
            if (predicate(root)) return root;
            
            var found = FindNodeRecursive(root, predicate);
            if (found != null) return found;
        }
        return null;
    }

    private TreeNode? FindNodeRecursive(TreeNode node, Func<TreeNode, bool> predicate)
    {
        foreach (var child in node.Children)
        {
            if (predicate(child)) return child;
            
            var found = FindNodeRecursive(child, predicate);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Selects a node and expands its ancestors to make it visible
    /// </summary>
    public TreeView SelectAndReveal(TreeNode node)
    {
        ExpandNode(node, expandAncestors: true);
        SelectNode(node);
        return this;
    }

    private void ExpandNodeRecursive(TreeNode node)
    {
        node.IsExpanded = true;
        foreach (var child in node.Children.ToArray())
        {
            ExpandNodeRecursive(child);
        }
    }

    private void CollapseNodeRecursive(TreeNode node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children.ToArray())
        {
            CollapseNodeRecursive(child);
        }
    }

    internal void SelectNode(TreeNode node)
    {
        if (_selectedNode != null && _nodeViews.TryGetValue(_selectedNode, out var previousView))
        {
            previousView.IsSelected = false;
        }

        _selectedNode = node;

        if (_selectedNode != null && _nodeViews.TryGetValue(_selectedNode, out var newView))
        {
            newView.IsSelected = true;
        }

        NodeSelected?.Invoke(node);
        MarkNeedsPaint();
    }

    internal void RegisterNodeView(TreeNode node, TreeNodeView view)
    {
        _nodeViews[node] = view;
        view.RefreshVisuals();
    }

    internal void NotifyNodeExpanded(TreeNode node, bool isExpanded)
    {
        NodeExpanded?.Invoke(node, isExpanded);
    }

    private void RebuildTree()
    {
        if (_treeContainer == null) return;

        _treeContainer.ClearChildren();
        _nodeViews.Clear();

        foreach (var rootNode in RootNodes)
        {
            var nodeView = new TreeNodeView(rootNode, this);
            _treeContainer.AddChild(nodeView);
            RegisterNodeView(rootNode, nodeView);
        }

        MarkNeedsLayout();
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 300;
        float measuredHeight = Height > 0 ? Height : 400;

        if (_rootFrame != null)
        {
            _rootFrame.Measure(measuredWidth, measuredHeight);
            DesiredWidth = _rootFrame.DesiredWidth;
            DesiredHeight = _rootFrame.DesiredHeight;
        }
        else
        {
            DesiredWidth = measuredWidth;
            DesiredHeight = measuredHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        _rootFrame?.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        _rootFrame?.Render(renderer);
    }
}
