using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using VisualScripting.NodeTypes;

namespace VisualScripting.Controls;

/// <summary>
/// A draggable palette entry in the NodeToolbar.
/// Displays as a button but implements IDraggable so the user can drag it
/// directly onto the NodeEditorCanvas to spawn a node.
/// Clicking also spawns the node via the OnTap action.
/// </summary>
public class PaletteItem : View<PaletteItem>, IDraggable, IPointerHandler
{
    private readonly string _label;
    private readonly NodeTypeId _nodeType;
    private readonly Color _categoryColor;

    private bool _isHovered;
    private bool _isPressed;

    public bool IsDragging { get; set; }

    /// <summary>Called when the item is clicked (tap) instead of dragged.</summary>
    public Action OnTap { get; set; }

    public PaletteItem(string label, NodeTypeId nodeType, Color categoryColor)
    {
        _label         = label;
        _nodeType      = nodeType;
        _categoryColor = categoryColor;

        Height = 28f;
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    // -------------------------------------------------------------------------
    // IDraggable
    // -------------------------------------------------------------------------

    public DragData OnDragStart(float mouseX, float mouseY)
    {
        return new DragData("palette-node", _nodeType, this);
    }

    public void OnDragging(float mouseX, float mouseY) { }

    public void OnDragEnd(bool wasDropped)
    {
        IsDragging = false;
        _isPressed = false;
        MarkNeedsPaint();
    }

    public float DragThreshold => 6f;
    public bool ShouldRenderWhileDragging => true;

    // -------------------------------------------------------------------------
    // IPointerHandler — hover / press visual feedback
    // -------------------------------------------------------------------------

    public void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        MarkNeedsPaint();
    }

    public void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        MarkNeedsPaint();
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        _isPressed = true;
        MarkNeedsPaint();
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (_isPressed && _isHovered)
            OnTap?.Invoke();
        _isPressed = false;
        MarkNeedsPaint();
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth  = availableWidth;
        DesiredHeight = 28f;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, 28f);
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX, y = ComputedY, w = ComputedWidth, h = ComputedHeight;

        Color bg = _isPressed
            ? new Color(30, 32, 40)
            : _isHovered
                ? new Color(55, 58, 70)
                : new Color(40, 42, 50);

        renderer.DrawRoundedRect(x, y, w, h, 4, bg);

        // Colored left accent bar matching category
        renderer.DrawRect(x, y + 4f, 3f, h - 8f, _categoryColor);

        var textSize = renderer.MeasureText(_label, 11f);
        renderer.DrawText(_label,
            x + 10f,
            y + (h - textSize.Y) / 2f,
            new Color(200, 200, 210), 11f);

        // Drag hint icon on right
        renderer.DrawText("⠿", x + w - 14f, y + (h - 11f) / 2f, new Color(80, 80, 100), 10f);
    }
}
