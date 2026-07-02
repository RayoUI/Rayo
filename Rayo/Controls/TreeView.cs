namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.DevTools;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
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
    private TreeNode _node;
    private readonly TreeView _treeView;
    private readonly bool _includeChildren;
    private VStack? _layout;
    private VStack? _childrenContainer;
    private TreeNodeHeaderButton? _headerButton;
    private bool _isSelected;

    public TreeNode Node => _node;

    public TreeNodeView(TreeNode node, TreeView treeView, bool includeChildren = true)
    {
        _node = node;
        _treeView = treeView;
        _includeChildren = includeChildren;
        BuildComponents();
    }

    public void BindNode(TreeNode node)
    {
        if (ReferenceEquals(_node, node))
        {
            RefreshVisuals();
            _headerButton?.RefreshContent();
            return;
        }

        _node = node;

        if (!_includeChildren)
        {
            ResetVirtualizedContent();
        }
        else
        {
            _headerButton?.RefreshContent();
            UpdateExpandedState();
        }

        RefreshVisuals();
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

        if (_includeChildren)
        {
            _childrenContainer = new VStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch);

            if (_node.IsExpanded)
            {
                _layout.AddChild(_childrenContainer);
                RebuildChildren();
            }
        }

        RefreshVisuals();
    }

    private void ResetVirtualizedContent()
    {
        if (_layout == null)
            return;

        _layout.ClearChildren();
        _headerButton = null;

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

        InvalidateMeasure();
    }

    private void OnNodeClicked()
    {
        if (!_node.IsEnabled) return;

        if (_node.HasChildren)
        {
            _node.IsExpanded = !_node.IsExpanded;
            UpdateExpandedState();
            _treeView.NotifyNodeExpanded(_node, _node.IsExpanded);
            _treeView.RequestTreeRefresh();
        }

        _treeView.SelectNode(_node);
    }

    private void UpdateExpandedState()
    {
        if (_layout == null) return;
        if (!_includeChildren || _childrenContainer == null)
        {
            _headerButton?.RefreshContent();
            MarkNeedsPaint();
            return;
        }

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
        InvalidateMeasure();
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

    protected override void Measure(float availableWidth, float availableHeight)
    {
        if (_layout != null)
        {
            _layout.MeasureUpdate(availableWidth, availableHeight);
            DesiredWidth = _layout.DesiredWidth;
            DesiredHeight = _layout.DesiredHeight;
        }
        else
        {
            DesiredWidth = availableWidth;
            DesiredHeight = _treeView.ItemHeight;
        }
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_layout != null)
        {
            _layout.ArrangeUpdate(x, y, width, height);
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
            IsInputTransparent = false;
            Background = Color.Transparent;

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
                _owner._treeView.RequestTreeRefresh();
                return;
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

            _tapRecognizer.Reset();
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

        protected override void Measure(float availableWidth, float availableHeight)
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
                
                chevronView.MeasureUpdate(chevronSize, chevronSize);
                chevronView.ArrangeUpdate(currentX, chevronY, chevronSize, chevronSize);
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

                iconView.MeasureUpdate(nodeIconSize, nodeIconSize);
                iconView.ArrangeUpdate(currentX, iconY, nodeIconSize, nodeIconSize);
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

                Brush checkboxColor = node.IsChecked ? treeView.SelectedColor : treeView.BorderBrush;
                renderer.DrawRoundedRect(checkboxX, checkboxY, checkboxSize, checkboxSize, 3, checkboxColor);

                if (node.IsChecked)
                {
                    // Draw checkmark
                    float checkIconSize = checkboxSize - 4;
                    var checkIcon = new Icon(Icons.Check)
                    {
                        Width = checkIconSize,
                        Height = checkIconSize,
                        Color = treeView.SelectedTextColor
                    };
                    checkIcon.MeasureUpdate(checkIconSize, checkIconSize);
                    checkIcon.ArrangeUpdate(checkboxX + 2, checkboxY + 2, checkIconSize, checkIconSize);
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
public class TreeView : BorderCompositeView<TreeView>
{
    public List<TreeNode> RootNodes
    {
        get => field;
        set => this.SetProperty(ref field, value ?? new List<TreeNode>(), RebuildTree);
    } = new();
    
    private TreeNode? _selectedNode = null;
    private ScrollView? _scrollView;
    private VirtualizedTreePanel? _treeContainer;
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
    public Brush SelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Transparent;

    [PaintProperty]
    public Brush SelectedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Transparent;

    [PaintProperty]
    public Brush HoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Transparent;

    [PaintProperty]
    public Brush PressedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Transparent;

    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Transparent;

    [PaintProperty]
    public Brush DisabledTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateNodeVisuals);
    } = Color.Transparent;

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
        InitializeTheme();
        BuildComponents();
        ApplyStyles();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(Background), (Brush)palette.Surface, value => Background = value);
        SetThemeValue(nameof(SelectedColor), (Brush)palette.Primary, value => SelectedColor = value);
        SetThemeValue(nameof(SelectedTextColor), (Brush)palette.OnPrimary, value => SelectedTextColor = value);
        SetThemeValue(nameof(HoverColor), (Brush)palette.SurfaceHover, value => HoverColor = value);
        SetThemeValue(nameof(PressedColor), (Brush)palette.SurfacePressed, value => PressedColor = value);
        SetThemeValue(nameof(TextColor), (Brush)palette.OnSurface, value => TextColor = value);
        SetThemeValue(nameof(DisabledTextColor), (Brush)palette.OnDisabled, value => DisabledTextColor = value);
        SetThemeValue(nameof(BorderBrush), (Brush)palette.Border, value => BorderBrush = value);
    }

    protected override void OnBorderBrushChanged()
    {
        base.OnBorderBrushChanged();
        ApplyStyles();
        UpdateNodeVisuals();
    }

    protected override void OnBorderThicknessChanged()
    {
        base.OnBorderThicknessChanged();
        ApplyStyles();
    }

    private void BuildComponents()
    {
        _scrollView = new ScrollView()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        _treeContainer = new VirtualizedTreePanel(this, _scrollView);
        _scrollView.Content(_treeContainer);

        _rootFrame = new Frame()
            .Background(Background)
            .BorderBrush(BorderBrush.PrimaryColor)
            .BorderThickness(BorderThickness)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(_scrollView);

        AddChild(_rootFrame);
    }

    private void ApplyStyles()
    {
        _rootFrame?
            .Background(Background)
            .BorderBrush(BorderBrush.PrimaryColor)
            .BorderThickness(BorderThickness);
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

        EnsureNodeVisible(node);
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

    internal void RequestTreeRefresh()
    {
        RebuildTree();
    }

    internal void ReplaceVisibleNodeViews(Dictionary<TreeNode, TreeNodeView> visibleViews)
    {
        _nodeViews.Clear();

        foreach (var pair in visibleViews)
        {
            _nodeViews[pair.Key] = pair.Value;
            pair.Value.RefreshVisuals();
        }
    }

    private void RebuildTree()
    {
        if (_treeContainer == null) return;

        _treeContainer.Configure(
            GetVisibleNodes(),
            ItemHeight,
            () => new TreeNodeView(new TreeNode(string.Empty), this, includeChildren: false),
            BindVirtualizedNodeView);

        _treeContainer.InvalidateMeasure();
        _treeContainer.InvalidateArrange();
        _scrollView?.InvalidateMeasure();
        _scrollView?.InvalidateArrange();
        InvalidateMeasure();
        MarkNeedsPaint();

        // If the control is already on-screen, force an immediate relayout so
        // expand/collapse updates the visible node list in the same frame.
        if (_rootFrame != null && ComputedWidth > 0 && ComputedHeight > 0)
        {
            _rootFrame.MeasureUpdate(ComputedWidth, ComputedHeight);
            _rootFrame.ForceArrange(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }
    }

    private void BindVirtualizedNodeView(VisualElement element, TreeNode node)
    {
        if (element is TreeNodeView nodeView)
        {
            nodeView.BindNode(node);
        }
    }

    private List<TreeNode> GetVisibleNodes()
    {
        var visibleNodes = new List<TreeNode>();

        foreach (var rootNode in RootNodes)
        {
            AddVisibleNodeRecursive(rootNode, visibleNodes);
        }

        return visibleNodes;
    }

    private static void AddVisibleNodeRecursive(TreeNode node, List<TreeNode> visibleNodes)
    {
        visibleNodes.Add(node);

        if (!node.IsExpanded)
            return;

        foreach (var child in node.Children)
        {
            AddVisibleNodeRecursive(child, visibleNodes);
        }
    }

    private void EnsureNodeVisible(TreeNode node)
    {
        if (_scrollView == null)
            return;

        var visibleNodes = GetVisibleNodes();
        int index = visibleNodes.IndexOf(node);
        if (index < 0)
            return;

        float itemY = index * Math.Max(1, ItemHeight);
        _scrollView.EnsureRectVisible(0, itemY, 1, Math.Max(1, ItemHeight));
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 300;
        float measuredHeight = Height > 0 ? Height : 400;

        if (_rootFrame != null)
        {
            _rootFrame.MeasureUpdate(measuredWidth, measuredHeight);
            DesiredWidth = _rootFrame.DesiredWidth;
            DesiredHeight = _rootFrame.DesiredHeight;
        }
        else
        {
            DesiredWidth = measuredWidth;
            DesiredHeight = measuredHeight;
        }
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        _rootFrame?.ArrangeUpdate(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        _rootFrame?.Render(renderer);
    }
}

internal sealed class VirtualizedTreePanel : CompositeView<VirtualizedTreePanel>
{
    private readonly TreeView _ownerTreeView;
    private readonly ScrollView _ownerScrollView;
    private IReadOnlyList<TreeNode> _visibleNodes = Array.Empty<TreeNode>();
    private float _itemHeight;
    private Func<VisualElement>? _itemFactory;
    private Action<VisualElement, TreeNode>? _itemBinder;
    private readonly Dictionary<int, VisualElement> _activeChildren = new();
    private readonly Stack<VisualElement> _recycledChildren = new();
    private readonly List<VisualElement> _orderedChildrenBuffer = new();
    private int _firstMaterializedIndex = -1;
    private int _lastMaterializedIndex = -1;
    private int _version;
    private int _materializedVersion = -1;
    private const int OverscanItems = 2;

    public VirtualizedTreePanel(TreeView ownerTreeView, ScrollView ownerScrollView)
    {
        _ownerTreeView = ownerTreeView;
        _ownerScrollView = ownerScrollView;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public void Configure(
        IReadOnlyList<TreeNode> visibleNodes,
        float itemHeight,
        Func<VisualElement> itemFactory,
        Action<VisualElement, TreeNode> itemBinder)
    {
        float previousDesiredHeight = _visibleNodes.Count * Math.Max(1, _itemHeight);
        _visibleNodes = visibleNodes ?? Array.Empty<TreeNode>();
        _itemHeight = itemHeight;
        _itemFactory = itemFactory;
        _itemBinder = itemBinder;
        _version++;
        _firstMaterializedIndex = -1;
        _lastMaterializedIndex = -1;

        float nextDesiredHeight = _visibleNodes.Count * Math.Max(1, _itemHeight);
        if (previousDesiredHeight != nextDesiredHeight)
            InvalidateMeasure();
        else
            InvalidateArrange();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsInfinity(availableWidth) || availableWidth <= 0 ? Width : availableWidth;
        DesiredHeight = _visibleNodes.Count * Math.Max(1, _itemHeight);
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_itemFactory == null || _itemBinder == null || _visibleNodes.Count == 0)
        {
            ClearMaterializedChildren();
            return;
        }

        float itemExtent = Math.Max(1, _itemHeight);
        float viewportHeight = Math.Max(0, _ownerScrollView.ComputedHeight - _ownerScrollView.Padding.Vertical);
        float scrollOffset = _ownerScrollView.VerticalScrollOffset;

        int firstVisible = Math.Max(0, (int)MathF.Floor(scrollOffset / itemExtent) - OverscanItems);
        int visibleCount = Math.Max(1, (int)MathF.Ceiling(viewportHeight / itemExtent) + OverscanItems * 2);
        int lastVisible = Math.Min(_visibleNodes.Count - 1, firstVisible + visibleCount - 1);

        if (firstVisible != _firstMaterializedIndex ||
            lastVisible != _lastMaterializedIndex ||
            _materializedVersion != _version)
        {
            MaterializeRange(firstVisible, lastVisible);
        }

        for (int localIndex = 0; localIndex < Children.Count; localIndex++)
        {
            var child = Children[localIndex];
            int itemIndex = _firstMaterializedIndex + localIndex;
            float itemY = y + itemIndex * itemExtent;
            child.MeasureUpdate(width, itemExtent);
            child.ArrangeUpdate(x, itemY, width, itemExtent);
        }
    }

    public override void Render(IRenderer renderer)
    {
    }

    private void MaterializeRange(int firstIndex, int lastIndex)
    {
        bool requiresRebind = _materializedVersion != _version;
        bool rangeUnchanged = firstIndex == _firstMaterializedIndex && lastIndex == _lastMaterializedIndex;

        if (!requiresRebind &&
            !rangeUnchanged &&
            _firstMaterializedIndex != -1 &&
            TryUpdateRangeInPlace(firstIndex, lastIndex))
        {
            _firstMaterializedIndex = firstIndex;
            _lastMaterializedIndex = lastIndex;
            _materializedVersion = _version;
            _ownerTreeView.ReplaceVisibleNodeViews(CaptureVisibleNodeViews(firstIndex));
            return;
        }

        _orderedChildrenBuffer.Clear();
        var visibleViews = new Dictionary<TreeNode, TreeNodeView>();

        foreach (var active in _activeChildren.ToArray())
        {
            if (active.Key >= firstIndex && active.Key <= lastIndex)
                continue;

            active.Value.Parent = null;
            _activeChildren.Remove(active.Key);
            _recycledChildren.Push(active.Value);
            PerformanceTracker.RecordVirtualizedRecycled();
        }

        for (int index = firstIndex; index <= lastIndex; index++)
        {
            var node = _visibleNodes[index];
            bool isNewChild = false;

            if (!_activeChildren.TryGetValue(index, out var child))
            {
                bool reused = _recycledChildren.Count > 0;
                child = reused ? _recycledChildren.Pop() : _itemFactory!();
                _activeChildren[index] = child;
                isNewChild = true;
                if (reused)
                    PerformanceTracker.RecordVirtualizedReused();
                else
                    PerformanceTracker.RecordVirtualizedCreated();
            }

            if (isNewChild || requiresRebind)
            {
                _itemBinder!(child, node);
                PerformanceTracker.RecordVirtualizedRebound();
            }

            _orderedChildrenBuffer.Add(child);

            if (child is TreeNodeView nodeView)
            {
                visibleViews[node] = nodeView;
            }
        }

        if (!rangeUnchanged)
            Children = [.. _orderedChildrenBuffer];

        _firstMaterializedIndex = firstIndex;
        _lastMaterializedIndex = lastIndex;
        _materializedVersion = _version;

        _ownerTreeView.ReplaceVisibleNodeViews(visibleViews);
    }

    private bool TryUpdateRangeInPlace(int firstIndex, int lastIndex)
    {
        if (Children.Count == 0)
            return false;

        int oldFirst = _firstMaterializedIndex;
        int oldLast = _lastMaterializedIndex;
        if (oldFirst == -1 || oldLast == -1)
            return false;

        int overlapFirst = Math.Max(firstIndex, oldFirst);
        int overlapLast = Math.Min(lastIndex, oldLast);
        if (overlapFirst > overlapLast)
            return false;

        bool structureChanged = false;

        while (_firstMaterializedIndex < firstIndex && Children.Count > 0)
        {
            var removed = Children[0];
            Children.RemoveAt(0);
            removed.Parent = null;
            _activeChildren.Remove(_firstMaterializedIndex);
            _recycledChildren.Push(removed);
            PerformanceTracker.RecordVirtualizedRecycled();
            _firstMaterializedIndex++;
            structureChanged = true;
        }

        while (_lastMaterializedIndex > lastIndex && Children.Count > 0)
        {
            var removed = Children[^1];
            Children.RemoveAt(Children.Count - 1);
            removed.Parent = null;
            _activeChildren.Remove(_lastMaterializedIndex);
            _recycledChildren.Push(removed);
            PerformanceTracker.RecordVirtualizedRecycled();
            _lastMaterializedIndex--;
            structureChanged = true;
        }

        for (int index = oldFirst - 1; index >= firstIndex; index--)
        {
            bool reused = _recycledChildren.Count > 0;
            var child = reused ? _recycledChildren.Pop() : _itemFactory!();
            _activeChildren[index] = child;
            if (child.Parent != this)
                child.Parent = this;
            _itemBinder!(child, _visibleNodes[index]);
            if (reused)
                PerformanceTracker.RecordVirtualizedReused();
            else
                PerformanceTracker.RecordVirtualizedCreated();
            PerformanceTracker.RecordVirtualizedRebound();
            Children.Insert(0, child);
            structureChanged = true;
        }

        for (int index = oldLast + 1; index <= lastIndex; index++)
        {
            bool reused = _recycledChildren.Count > 0;
            var child = reused ? _recycledChildren.Pop() : _itemFactory!();
            _activeChildren[index] = child;
            if (child.Parent != this)
                child.Parent = this;
            _itemBinder!(child, _visibleNodes[index]);
            if (reused)
                PerformanceTracker.RecordVirtualizedReused();
            else
                PerformanceTracker.RecordVirtualizedCreated();
            PerformanceTracker.RecordVirtualizedRebound();
            Children.Add(child);
            structureChanged = true;
        }

        if (structureChanged)
            RaiseTreeStructureChanged(this);

        return true;
    }

    private Dictionary<TreeNode, TreeNodeView> CaptureVisibleNodeViews(int firstIndex)
    {
        var visibleViews = new Dictionary<TreeNode, TreeNodeView>(Children.Count);

        for (int localIndex = 0; localIndex < Children.Count; localIndex++)
        {
            if (Children[localIndex] is not TreeNodeView nodeView)
                continue;

            int itemIndex = firstIndex + localIndex;
            if ((uint)itemIndex >= (uint)_visibleNodes.Count)
                continue;

            visibleViews[_visibleNodes[itemIndex]] = nodeView;
        }

        return visibleViews;
    }

    private void ClearMaterializedChildren()
    {
        if (Children.Count == 0 && _firstMaterializedIndex == -1 && _lastMaterializedIndex == -1)
            return;

        foreach (var child in Children)
        {
            child.Parent = null;
            _recycledChildren.Push(child);
        }

        Children = [];
        _activeChildren.Clear();
        _firstMaterializedIndex = -1;
        _lastMaterializedIndex = -1;
        _materializedVersion = _version;

        _ownerTreeView.ReplaceVisibleNodeViews(new Dictionary<TreeNode, TreeNodeView>());
    }
}
