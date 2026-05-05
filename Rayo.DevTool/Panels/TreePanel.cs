using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using System.Collections.Generic;
using System.Linq;

namespace Rayo.DevTool.Frames;

public class TreeFrame : UserControl
{
    private readonly DevToolState _state;

    public TreeFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        var highlightButton = new IconButton(Icons.Target)
            .Size(30)
            .IconSize(20)
            .IconColor(_state.IsHighlightEnabled.Map(enabled => 
                enabled ? new Color(59, 130, 246) : new Color(160, 160, 160)))
            .Background(_state.IsHighlightEnabled.Map(enabled =>
                enabled ? new Color(59, 130, 246, 0.2f) : Color.Transparent))
            .HoverBackground(new Color(255, 255, 255, 0.1f))
            .BorderRadius(new CornerRadius(4))
            .OnTapped(() =>
            {
                _state.IsHighlightEnabled.Value = !_state.IsHighlightEnabled.Value;
            });

        var header = new Frame()
            .Background(new Color(40, 40, 45))
            .Height(30)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .VerticalAlignment(VerticalAlignment.Top)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .JustifyContent(JustifyContent.SpaceBetween)
                    .Children(
                        new Label("Element Tree")
                            .FontSize(14)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Foreground(Color.White),
                        highlightButton
                    )
            );

        var treeScroll = new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                BuildTreeView()
            );

        return new Frame()
            .Width(350)
            .Background(new Color(28, 28, 32))
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
            .Padding(new Thickness(8))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var mainTreeHeader = new Label("Main Tree")
            .Foreground(new Color(120, 180, 255))
            .FontSize(11)
            .Padding(new Thickness(0, 0, 0, 4));

        var mainTreeContainer = new VStack()
            .Spacing(0)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var overlaysHeader = new Label("Overlays")
            .Foreground(new Color(255, 180, 120))
            .FontSize(11)
            .Padding(new Thickness(0, 12, 0, 4));

        var overlaysContainer = new VStack()
            .Spacing(0)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var emptyLabel = new Label("No connection or empty tree")
            .Foreground(ColorDefault.Secondary)
            .FontSize(12);

        _state.RootNode.Subscribe(root =>
        {
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
        });

        _state.OverlayNodes.Subscribe(overlays =>
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
        });

        mainTreeHeader.IsVisible = false;
        mainTreeContainer.IsVisible = false;
        overlaysHeader.IsVisible = false;
        overlaysContainer.IsVisible = false;

        container.AddChild(emptyLabel);
        container.AddChild(mainTreeHeader);
        container.AddChild(mainTreeContainer);
        container.AddChild(overlaysHeader);
        container.AddChild(overlaysContainer);

        return container;
    }

    private VisualElement BuildTreeNode(ElementNode node, int depth)
    {
        var indent = depth * 16;
        var hasChildren = node.Children.Count > 0;

        if (!_state.ExpandedStates.TryGetValue(node.Id, out var savedExpanded))
        {
            // Expand all nodes by default in DevTools to show full tree structure
            // User can collapse nodes manually with double click
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
        
        // Create chevron icon
        var chevronIcon = hasChildren 
            ? new Icon(isExpanded.Value ? Icons.ChevronDown : Icons.ChevronRight)
                .Size(12)
                .Color(new Color(160, 160, 160))
            : null;

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
        
        var textLabel = new Label(displayText)
            .Foreground(hasInvalidDimensions ? new Color(245, 158, 11) : new Color(200, 200, 200))
            .FontSize(12);

        var leftContent = new HStack()
            .Spacing(4)
            .Alignment(Alignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);

        if (chevronIcon != null)
        {
            leftContent.AddChild(chevronIcon);
        }
        else
        {
            leftContent.AddChild(new Frame().Width(12).Height(12));
        }

        leftContent.AddChild(textLabel);

        var content = new Grid()
            .Rows(GridLength.Auto)
            .Columns(GridLength.Star, GridLength.Auto)
            .Padding(new Thickness(indent + 4, 4, 4, 4))
            .VerticalAlignment(VerticalAlignment.Top)
            .AddChild(leftContent, 0, 0);

        if (hasChildren)
        {
            var badge = new Label($"{node.Children.Count}")
                .FontSize(9)
                .Foreground(new Color(140, 140, 150))
                .Padding(new Thickness(4, 1))
                .Background(new Color(50, 52, 60))
                .BorderRadius(new CornerRadius(6))
                .VerticalAlignment(VerticalAlignment.Center);

            content.AddChild(badge, 0, 1);
        }

        void SelectNode()
        {
            _state.SelectedElementId.Value = node.Id;

            // Highlight if highlight mode is enabled, otherwise clear highlight
            _ = _state.Client.HighlightAsync(_state.IsHighlightEnabled.Value ? node.Id : null);

            _ = _state.LoadPropertiesAsync(node.Id);
        }

        void ToggleExpanded()
        {
            if (!hasChildren)
                return;

            isExpanded.Value = !isExpanded.Value;
            _state.ExpandedStates[node.Id] = isExpanded.Value;
        }

        var header = new Button()
            .TextAlignment(HorizontalAlignment.Left)
            .Padding(new Thickness(0))
            .Text("")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Background(_state.SelectedElementId.Map(id =>
                id == node.Id ? new Color(59, 130, 246, 0.3f) : Color.Transparent))
            .HoverBackground(_state.SelectedElementId.Map(id =>
                id == node.Id ? new Color(59, 130, 246, 0.4f) : new Color(255, 255, 255, 0.1f)))
            .BorderWidth(0);

        header.Tapped += args =>
        {
            SelectNode();

            if (args.TapCount >= 2)
            {
                ToggleExpanded();
            }
        };

        // Wrap button and content in a container
        var headerContainer = new Frame()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)  // Prevent infinite height
            .Content(
                new Grid()
                    .Rows(GridLength.Auto)
                    .Columns(GridLength.Star)
                    .VerticalAlignment(VerticalAlignment.Top)  // Prevent infinite height
                    .AddChild(header, 0, 0)
                    .AddChild(content, 0, 0)
            );

        if (hasChildren && chevronIcon != null)
        {
            isExpanded.Subscribe(expanded =>
            {
                chevronIcon.IconData = expanded ? Icons.ChevronDown : Icons.ChevronRight;
            });
        }

        nodeContainer.AddChild(headerContainer);

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
}
