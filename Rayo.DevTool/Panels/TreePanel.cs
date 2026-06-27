using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using System.Collections.Generic;
using System.Linq;
using Rayo.Core.Input;

namespace Rayo.DevTool.Frames;

public class TreeFrame : Component
{
    private readonly DevToolState _state;
    private readonly Dictionary<string, VisualElement> _nodeRows = new();
    private ScrollView? _treeScroll;
    private VisualElement? _treeContent;

    public TreeFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        var highlightButton = new ButtonIcon(Icons.Target)
            .Size(30)
            .IconSize(20)
            .IconColor(_state.IsHighlightEnabled.Map(enabled => 
                enabled ? DevToolTheme.Colors.Primary : DevToolTheme.Colors.OnDisabled))
            .Background(_state.IsHighlightEnabled.Map(enabled =>
                enabled ? DevToolTheme.Colors.Primary.WithAlpha(0.2f) : Color.Transparent))
            .HoverBackground(DevToolTheme.Colors.SurfaceHover)
            .OnTapped(() =>
            {
                _state.IsHighlightEnabled.Value = !_state.IsHighlightEnabled.Value;
            });

        highlightButton.BorderRadius = _state.IsHighlightEnabled.Value
            ? new CornerRadius(0)
            : new CornerRadius(4);

        _state.IsHighlightEnabled.Subscribe(enabled =>
        {
            highlightButton.BorderRadius = enabled ? new CornerRadius(0) : new CornerRadius(4);
        });

        var clearLayoutOutlinesButton = new ButtonIcon(Icons.Broom)
            .Size(30)
            .IconSize(16)
            .IconColor(_state.LayoutOutlineElementIds.Map(ids =>
                ids.Count > 0 ? DevToolTheme.Colors.Warning : DevToolTheme.Colors.OnDisabled))
            .Background(_state.LayoutOutlineElementIds.Map(ids =>
                ids.Count > 0 ? DevToolTheme.Colors.Warning.WithAlpha(0.18f) : Color.Transparent))
            .HoverBackground(DevToolTheme.Colors.SurfaceHover)
            .PressedBackground(DevToolTheme.Colors.SurfacePressed)
            .BorderThickness(0)
            .BorderRadius(new CornerRadius(4))
            .OnTapped(() => _state.ClearLayoutOutlines());

        var header = new Frame()
            .Background(DevToolTheme.Colors.SurfaceHover)
            .Height(30)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .VerticalAlignment(VerticalAlignment.Top)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Spacing(4)
                    .Alignment(Alignment.Center)
                    .JustifyContent(JustifyContent.Start)
                    .Children(
                        highlightButton.WithTooltip("Inspect client element"),
                        clearLayoutOutlinesButton.WithTooltip("Clear layout outlines")
                    )
            );

        var treeContent = BuildTreeView();
        _treeContent = treeContent;

        var treeScroll = new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Auto)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
            .ShowScrollbars(true)
            .Content(treeContent);
        _treeScroll = treeScroll;

        _state.SelectedElementRevealRequests.Subscribe(_ => EnsureSelectedNodeVisible());

        return new Frame()
            .Width(350)
            .Background(DevToolTheme.Colors.Background)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new Grid()
                    .Rows(GridLength.Auto, GridLength.Star)
                    .Columns(GridLength.Star)
                    .AddChild(header, 0, 0)
                    .AddChild(treeScroll, 1, 0)
            );
    }

    private VisualElement BuildTreeView()
    {
        var container = new VStack()
            .Spacing(2)
            .Padding(new Thickness(0, 8, 0, 8))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var mainTreeHeader = new Label("Main Tree")
            .Foreground(DevToolTheme.Colors.Primary)
            .FontSize(11)
            .Margin(new Thickness(left: 10))
            .Padding(new Thickness(0, 0, 0, 4));

        var mainTreeContainer = new VStack()
            .Spacing(0)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var overlaysHeader = new Label("Overlays")
            .Foreground(DevToolTheme.Colors.Warning)
            .FontSize(11)
            .Margin(new Thickness(left: 10))
            .Padding(new Thickness(0, 12, 0, 4));

        var overlaysContainer = new VStack()
            .Spacing(0)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var emptyLabel = new Label("No connection or empty tree")
            .Foreground(DevToolTheme.Colors.OnDisabled)
            .FontSize(12);

        void UpdateRoot(ElementNode? root)
        {
            _nodeRows.Clear();
            mainTreeContainer.ClearChildren();

            if (root != null)
            {
                mainTreeHeader.IsVisible = true;
                mainTreeContainer.IsVisible = true;
                emptyLabel.IsVisible = false;
                mainTreeContainer.AddChild(BuildTreeNode(root, 0));
            }
            else
            {
                mainTreeHeader.IsVisible = false;
                mainTreeContainer.IsVisible = false;
                overlaysHeader.IsVisible = false;
                overlaysContainer.IsVisible = false;
                emptyLabel.IsVisible = true;
            }

            mainTreeContainer.MarkNeedsLayout();
        }

        void UpdateOverlays(List<ElementNode> overlays)
        {
            overlaysContainer.ClearChildren();

            if (overlays.Count > 0 && _state.RootNode.Value != null)
            {
                overlaysHeader.IsVisible = true;
                overlaysContainer.IsVisible = true;

                foreach (var overlay in overlays)
                {
                    overlaysContainer.AddChild(BuildTreeNode(overlay, 0));
                }
            }
            else
            {
                overlaysHeader.IsVisible = false;
                overlaysContainer.IsVisible = false;
            }

            overlaysContainer.MarkNeedsLayout();
        }

        _state.RootNode.Subscribe(UpdateRoot);
        _state.OverlayNodes.Subscribe(UpdateOverlays);

        mainTreeHeader.IsVisible = false;
        mainTreeContainer.IsVisible = false;
        overlaysHeader.IsVisible = false;
        overlaysContainer.IsVisible = false;

        container.AddChild(emptyLabel);
        container.AddChild(mainTreeHeader);
        container.AddChild(mainTreeContainer);
        container.AddChild(overlaysHeader);
        container.AddChild(overlaysContainer);

        UpdateRoot(_state.RootNode.Value);
        UpdateOverlays(_state.OverlayNodes.Value);

        return container;
    }

    private VisualElement BuildTreeNode(ElementNode node, int depth)
    {
        var indent = depth * 16;
        var hasChildren = node.Children.Count > 0;

        if (!_state.ExpandedStates.TryGetValue(node.Id, out var savedExpanded))
        {
            // Expand all nodes by default in DevTools to show full tree structure.
            // Users can collapse/expand nodes with a single click on the chevron.
            savedExpanded = true;
            _state.ExpandedStates[node.Id] = savedExpanded;
        }
        var isExpanded = new Signal<bool>(savedExpanded);

        var nodeContainer = new VStack()
            .Spacing(0)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var nameText = !string.IsNullOrEmpty(node.Name) ? $" \"{node.Name}\"" : "";

        // Check for invalid dimensions (NaN or Infinity only - zero is valid for elements not yet laid out)
        bool hasInvalidDimensions = float.IsInfinity(node.Width) || float.IsInfinity(node.Height) ||
                                    float.IsNaN(node.Width) || float.IsNaN(node.Height);
        
        // Create text label with dimension info
        var displayText = $"{node.TypeName}{nameText}";
        if (hasInvalidDimensions)
        {
            if (float.IsInfinity(node.Width) || float.IsInfinity(node.Height))
            {
                displayText += " [INF]";
            }
            else
            {
                displayText += " [!]";
            }
        }
        
        void SelectNode()
        {
            _state.SelectedElementId.Value = node.Id;

            _ = _state.LoadPropertiesAsync(node.Id);
        }

        void ToggleExpanded()
        {
            if (!hasChildren)
                return;

            isExpanded.Value = !isExpanded.Value;
            _state.ExpandedStates[node.Id] = isExpanded.Value;
        }

        var rowBackground = new Computed<Color>(() =>
        {
            if (_state.SelectedElementId.Value == node.Id)
            {
                return DevToolTheme.Colors.Primary.WithAlpha(0.3f);
            }

            return _state.HoveredElementId.Value == node.Id
                ? DevToolTheme.Colors.SurfaceHover
                : Color.Transparent;
        });
        ButtonIcon? chevronButton = null;
        VisualElement chevronElement = hasChildren
            ? chevronButton = new ButtonIcon(isExpanded.Value ? Icons.ChevronDown : Icons.ChevronRight)
                .Size(20)
                .IconSize(12)
                .IconColor(DevToolTheme.Colors.OnDisabled)
                .Background(Color.Transparent)
                .HoverBackground(DevToolTheme.Colors.SurfaceHover)
                .PressedBackground(DevToolTheme.Colors.SurfacePressed)
                .BorderThickness(0)
                .Padding(new Thickness(4))
                .BorderRadius(new CornerRadius(3))
            : new Frame().Width(20).Height(20);

        if (chevronButton != null)
        {
            chevronButton.Tapped += _ => ToggleExpanded();

            isExpanded.Subscribe(expanded =>
            {
                chevronButton.IconData = expanded ? Icons.ChevronDown : Icons.ChevronRight;
            });

            chevronElement = chevronButton.WithTooltip("Expand or collapse node");
        }

        var titleButton = (HoverableTreeNodeButton)new HoverableTreeNodeButton()
            .TextAlignment(HorizontalAlignment.Left)
            .Padding(new Thickness(0, 3, 0, 3))
            .Text(displayText)
            .TextColor(hasInvalidDimensions ? DevToolTheme.Colors.Warning : DevToolTheme.Colors.OnSurface)
            .FontSize(12)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Background(Color.Transparent)
            .HoverBackground(Color.Transparent)
            .PressedBackground(Color.Transparent)
            .BorderThickness(0);

        titleButton.Tapped += _ => SelectNode();

        var headerGrid = new Grid()
            .Rows(GridLength.Auto)
            .Columns(GridLength.Pixels(indent + 4), GridLength.Pixels(20), GridLength.Star, GridLength.Auto)
            .Padding(new Thickness(0, 2, 4, 2))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .AddChild(chevronElement, 0, 1)
            .AddChild(titleButton, 0, 2);

        if (node.IsLayout)
        {
            var badge = new Button()
                .Text($"{node.Children.Count}")
                .FontSize(9)
                .TextColor(_state.LayoutOutlineElementIds.Map(ids =>
                    ids.Contains(node.Id) ? DevToolTheme.Colors.OnPrimary : DevToolTheme.Colors.OnSurface))
                .Padding(new Thickness(4, 1))
                .Background(_state.LayoutOutlineElementIds.Map(ids =>
                    ids.Contains(node.Id) ? DevToolTheme.Colors.Primary : DevToolTheme.Colors.SurfacePressed))
                .HoverBackground(DevToolTheme.Colors.PrimaryHover)
                .PressedBackground(DevToolTheme.Colors.PrimaryPressed)
                .BorderThickness(0)
                .BorderRadius(new CornerRadius(6))
                .VerticalAlignment(VerticalAlignment.Center)
                .OnTapped(() => _state.ToggleLayoutOutline(node.Id))
                .WithTooltip("Toggle layout outline");

            headerGrid.AddChild(badge, 0, 3);
        }

        var rowFrame = new TreeNodeRowFrame(
                onSelect: SelectNode,
                onHoverChanged: isHovered =>
                {
                    if (isHovered)
                    {
                        _state.HoverElement(node.Id);
                    }
                    else
                    {
                        _state.ClearHoveredElement(node.Id);
                    }
                })
            .Background(rowBackground)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(headerGrid);

        _nodeRows[node.Id] = rowFrame;
        nodeContainer.AddChild(rowFrame);

        if (hasChildren)
        {
            var childrenContainer = new VStack()
                .Spacing(0)
                .VerticalAlignment(VerticalAlignment.Top)
                .HorizontalAlignment(HorizontalAlignment.Stretch);

            foreach (var child in node.Children.ToArray())
            {
                childrenContainer.AddChild(BuildTreeNode(child, depth + 1));
            }

            // Use VStack directly instead of Frame to avoid infinite height issues
            // VStack with VerticalAlignment.Top will size to content properly
            childrenContainer.IsVisible = isExpanded.Value;

            isExpanded.Subscribe(expanded =>
            {
                childrenContainer.IsVisible = expanded;
            });

            nodeContainer.AddChild(childrenContainer);
        }

        return nodeContainer;
    }

    private void EnsureSelectedNodeVisible()
    {
        var selectedElementId = _state.SelectedElementId.Value;
        if (selectedElementId == null ||
            _treeScroll == null ||
            _treeContent == null ||
            !_nodeRows.TryGetValue(selectedElementId, out var selectedRow))
        {
            return;
        }

        if (selectedRow.ComputedWidth <= 0 || selectedRow.ComputedHeight <= 0)
        {
            return;
        }

        var rectY = Math.Max(0, selectedRow.ComputedY - _treeContent.ComputedY);
        var viewportHeight = Math.Max(0, _treeScroll.ComputedHeight - _treeScroll.Padding.Vertical);
        var centeredOffset = rectY + selectedRow.ComputedHeight / 2f - viewportHeight / 2f;

        _treeScroll.VerticalScrollOffset = centeredOffset;
    }

    private sealed class HoverableTreeNodeButton : Button, Rayo.Core.Input.IPointerHandler
    {
        public event System.Action<Rayo.Core.Input.PointerEventArgs>? PointerEntered;
        public event System.Action<Rayo.Core.Input.PointerEventArgs>? PointerExited;

        void Rayo.Core.Input.IPointerHandler.OnPointerEntered(Rayo.Core.Input.PointerEventArgs e)
        {
            base.OnPointerEntered(e);

            if (e.PointerType == Rayo.Core.Input.PointerType.Mouse)
            {
                PointerEntered?.Invoke(e);
            }
        }

        void Rayo.Core.Input.IPointerHandler.OnPointerExited(Rayo.Core.Input.PointerEventArgs e)
        {
            base.OnPointerExited(e);

            if (e.PointerType == Rayo.Core.Input.PointerType.Mouse)
            {
                PointerExited?.Invoke(e);
            }
        }

        void Rayo.Core.Input.IPointerHandler.OnPointerPressed(Rayo.Core.Input.PointerEventArgs e)
        {
            base.OnPointerPressed(e);
        }

        void Rayo.Core.Input.IPointerHandler.OnPointerReleased(Rayo.Core.Input.PointerEventArgs e)
        {
            base.OnPointerReleased(e);
        }
    }

    private sealed class TreeNodeRowFrame : Frame, IPointerHandler
    {
        private readonly System.Action _onSelect;
        private readonly System.Action<bool> _onHoverChanged;
        private bool _isPressed;

        public TreeNodeRowFrame(System.Action onSelect, System.Action<bool> onHoverChanged)
        {
            _onSelect = onSelect;
            _onHoverChanged = onHoverChanged;
            BorderThickness = 0;
            Padding = new Thickness(0);
        }

        void IPointerHandler.OnPointerEntered(PointerEventArgs e)
        {
            if (e.PointerType == PointerType.Mouse)
            {
                _onHoverChanged(true);
            }
        }

        void IPointerHandler.OnPointerExited(PointerEventArgs e)
        {
            if (e.PointerType == PointerType.Mouse)
            {
                _onHoverChanged(false);
            }

            _isPressed = false;
        }

        void IPointerHandler.OnPointerPressed(PointerEventArgs e)
        {
            if (!e.Handled)
            {
                _isPressed = true;
            }
        }

        void IPointerHandler.OnPointerReleased(PointerEventArgs e)
        {
            if (_isPressed && !e.Handled)
            {
                _onSelect();
                e.Handled = true;
            }

            _isPressed = false;
        }
    }
}
