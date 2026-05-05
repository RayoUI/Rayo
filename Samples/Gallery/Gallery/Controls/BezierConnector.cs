using System.Numerics;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

namespace Gallery.Controls;

/// <summary>
/// Draws a Bezier curve connection between two points.
/// Useful for creating diagram connectors, flowcharts, and node-based editors.
/// </summary>
public class BezierConnector : View<BezierConnector>
{
    private Vector2 _startPoint;
    private Vector2 _endPoint;
    private Brush _strokeColor = new Color(100, 105, 120);
    private float _strokeWidth = 2f;
    private float _curvature = 0.5f;
    private ConnectorDirection _direction = ConnectorDirection.Horizontal;
    private bool _showArrow = true;
    private float _arrowSize = 8f;

    public enum ConnectorDirection
    {
        /// <summary>
        /// Control points are offset horizontally (good for left-to-right connections)
        /// </summary>
        Horizontal,
        /// <summary>
        /// Control points are offset vertically (good for top-to-bottom connections)
        /// </summary>
        Vertical,
        /// <summary>
        /// Automatically determine based on start/end positions
        /// </summary>
        Auto
    }

    #region StartPoint
    /// <summary>
    /// Starting point of the connector (relative to parent Absolute)
    /// </summary>
    [PaintProperty]
    public Vector2 StartPoint
    {
        get => _startPoint;
        set => this.SetProperty(ref _startPoint, value);
    }
    #endregion

    #region EndPoint
    /// <summary>
    /// Ending point of the connector (relative to parent Absolute)
    /// </summary>
    public Vector2 EndPoint
    {
        get => _endPoint;
        set => this.SetProperty(ref _endPoint, value, MarkNeedsPaint);
    }
    #endregion

    #region StrokeColor
    /// <summary>
    /// Color of the connector line
    /// </summary>
    public Brush StrokeColor
    {
        get => _strokeColor;
        set => this.SetProperty(ref _strokeColor, value, MarkNeedsPaint);
    }
    #endregion

    #region StrokeWidth
    /// <summary>
    /// Width of the connector line
    /// </summary>
    public float StrokeWidth
    {
        get => _strokeWidth;
        set => this.SetProperty(ref _strokeWidth, value, MarkNeedsPaint);
    }
    #endregion

    #region Curvature
    /// <summary>
    /// How much the curve bends (0 = straight line, 1 = maximum curve)
    /// </summary>
    public float Curvature
    {
        get => _curvature;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            this.SetProperty(ref _curvature, clamped, MarkNeedsPaint);
        }
    }
    #endregion

    #region Direction
    /// <summary>
    /// Direction of the curve control points
    /// </summary>
    public ConnectorDirection Direction
    {
        get => _direction;
        set => this.SetProperty(ref _direction, value, MarkNeedsPaint);
    }
    #endregion

    #region ShowArrow
    /// <summary>
    /// Whether to show an arrow at the end point
    /// </summary>
    public bool ShowArrow
    {
        get => _showArrow;
        set => this.SetProperty(ref _showArrow, value, MarkNeedsPaint);
    }
    #endregion

    #region ArrowSize
    /// <summary>
    /// Size of the arrow head
    /// </summary>
    public float ArrowSize
    {
        get => _arrowSize;
        set => this.SetProperty(ref _arrowSize, value, MarkNeedsPaint);
    }
    #endregion

    // Fluent API methods
    //---------------------------------------------
    //public BezierConnector StartPoint(float x, float y)
    //{
    //    StartPoint = new Vector2(x, y);
    //    return this;
    //}

    //public BezierConnector EndPoint(float x, float y)
    //{
    //    EndPoint = new Vector2(x, y);
    //    return this;
    //}

    public BezierConnector Points(float startX, float startY, float endX, float endY)
    {
        _startPoint = new Vector2(startX, startY);
        _endPoint = new Vector2(endX, endY);
        MarkNeedsPaint();
        return this;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // BezierConnector coordinates are relative to the parent Absolute,
        // so DesiredWidth/Height should encompass from origin (0,0) to the max point.
        // This allows the Absolute to properly position us at its origin.
        var (cp1, cp2) = CalculateControlPoints();

        float maxX = Math.Max(Math.Max(_startPoint.X, _endPoint.X), Math.Max(cp1.X, cp2.X));
        float maxY = Math.Max(Math.Max(_startPoint.Y, _endPoint.Y), Math.Max(cp1.Y, cp2.Y));

        // Add padding for stroke width and arrow
        float padding = _strokeWidth + (_showArrow ? _arrowSize : 0);

        // Width/Height from origin to max point
        DesiredWidth = maxX + padding;
        DesiredHeight = maxY + padding;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        var (cp1, cp2) = CalculateControlPoints();

        // Get the offset from parent Absolute (ComputedX/Y includes canvas position)
        // For elements in a Absolute without explicit position, this will be the Absolute origin
        float offsetX = ComputedX;
        float offsetY = ComputedY;

        // Draw the bezier curve at offset positions
        renderer.DrawCubicBezier(
            offsetX + _startPoint.X, offsetY + _startPoint.Y,
            offsetX + cp1.X, offsetY + cp1.Y,
            offsetX + cp2.X, offsetY + cp2.Y,
            offsetX + _endPoint.X, offsetY + _endPoint.Y,
            _strokeColor.PrimaryColor,
            _strokeWidth
        );

        // Draw arrow head if enabled
        if (_showArrow)
        {
            DrawArrowHead(renderer, cp2, _endPoint, offsetX, offsetY);
        }
    }

    private (Vector2 cp1, Vector2 cp2) CalculateControlPoints()
    {
        var direction = _direction;

        // Auto-detect direction based on positions
        if (direction == ConnectorDirection.Auto)
        {
            float dx = Math.Abs(_endPoint.X - _startPoint.X);
            float dy = Math.Abs(_endPoint.Y - _startPoint.Y);
            direction = dx > dy ? ConnectorDirection.Horizontal : ConnectorDirection.Vertical;
        }

        float distance = Vector2.Distance(_startPoint, _endPoint);
        float offset = distance * _curvature;

        Vector2 cp1, cp2;

        if (direction == ConnectorDirection.Horizontal)
        {
            // Horizontal curve - control points offset in X direction
            cp1 = new Vector2(_startPoint.X + offset, _startPoint.Y);
            cp2 = new Vector2(_endPoint.X - offset, _endPoint.Y);
        }
        else
        {
            // Vertical curve - control points offset in Y direction
            cp1 = new Vector2(_startPoint.X, _startPoint.Y + offset);
            cp2 = new Vector2(_endPoint.X, _endPoint.Y - offset);
        }

        return (cp1, cp2);
    }

    private void DrawArrowHead(IRenderer renderer, Vector2 controlPoint, Vector2 endPoint, float offsetX, float offsetY)
    {
        // Calculate direction from control point to end point
        var direction = Vector2.Normalize(endPoint - controlPoint);

        // Calculate perpendicular direction
        var perpendicular = new Vector2(-direction.Y, direction.X);

        // Calculate arrow points
        var arrowBase = endPoint - direction * _arrowSize;
        var arrowLeft = arrowBase + perpendicular * (_arrowSize * 0.5f);
        var arrowRight = arrowBase - perpendicular * (_arrowSize * 0.5f);

        // Draw arrow as filled triangle using lines (simple approach)
        // Apply offset to all coordinates
        renderer.DrawLine(
            offsetX + endPoint.X, offsetY + endPoint.Y,
            offsetX + arrowLeft.X, offsetY + arrowLeft.Y,
            _strokeWidth, _strokeColor);
        renderer.DrawLine(
            offsetX + endPoint.X, offsetY + endPoint.Y,
            offsetX + arrowRight.X, offsetY + arrowRight.Y,
            _strokeWidth, _strokeColor);
        renderer.DrawLine(
            offsetX + arrowLeft.X, offsetY + arrowLeft.Y,
            offsetX + arrowRight.X, offsetY + arrowRight.Y,
            _strokeWidth, _strokeColor);
    }

    /// <summary>
    /// Creates a connector between two nodes (using their center-right and center-left edges)
    /// </summary>
    public static BezierConnector ConnectNodes(
        float node1X, float node1Y, float node1Width, float node1Height,
        float node2X, float node2Y, float node2Width, float node2Height)
    {
        // Connect from right edge of node1 to left edge of node2
        float startX = node1X + node1Width;
        float startY = node1Y + node1Height / 2;
        float endX = node2X;
        float endY = node2Y + node2Height / 2;

        return new BezierConnector()
            .Points(startX, startY, endX, endY);
    }
}
