namespace Rayo.Layout;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Representa una definición de fila/columna en el Grid
/// </summary>
public class GridLength
{
    public enum GridUnitType
    {
        /// <summary>Tamaño fijo en píxeles</summary>
        Pixel,

        /// <summary>Tamaño automático basado en contenido</summary>
        Auto,

        /// <summary>Tamaño proporcional (estrella)</summary>
        Star
    }

    public GridUnitType Type { get; }
    public float Value { get; }

    private GridLength(GridUnitType type, float value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>Crea un tamaño fijo en píxeles</summary>
    public static GridLength Pixels(float pixels) => new GridLength(GridUnitType.Pixel, pixels);

    /// <summary>Crea un tamaño automático basado en contenido</summary>
    public static GridLength Auto => new GridLength(GridUnitType.Auto, 0);

    /// <summary>Crea un tamaño proporcional (1*)</summary>
    public static GridLength Star => new GridLength(GridUnitType.Star, 1);

    /// <summary>Crea un tamaño proporcional (n*)</summary>
    public static GridLength Stars(float value) => new GridLength(GridUnitType.Star, value);
}

/// <summary>
/// Layout de cuadrícula flexible similar a CSS Grid.
/// Permite organizar elementos en filas y columnas con tamaños flexibles.
/// Migrated to new MAUI-like architecture: inherits from Rayo.Core.Layout<T>
/// </summary>
public class Grid : Layout<Grid>
{
    private Dictionary<VisualElement, (int row, int col, int rowSpan, int colSpan)> _cellPositions = new();
    public List<GridLength> RowDefinitions { get; } = new();
    public List<GridLength> ColumnDefinitions { get; } = new();

    #region RowSpacing
    [LayoutProperty]
    public float RowSpacing
    {
        get;
        set => this.SetProperty(ref field, value);
    }
    #endregion
    [LayoutProperty]
    #region ColumnSpacing
    public float ColumnSpacing
    {
        get;
        set => this.SetProperty(ref field, value);
    } 
    #endregion


    public Grid()
    {
        RowSpacing = 0;
        ColumnSpacing = 0;
        // Like other layouts: Default to Stretch alignment
        // This makes Grid expand to fill available space in Frame containers
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ShouldExpand = false;
    }


    /// <summary>
    /// Agrega un hijo al grid en la celda especificada
    /// </summary>
    public Grid AddChild(VisualElement child, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        base.AddChild(child);
        _cellPositions[child] = (row, column, rowSpan, columnSpan);
        return this;
    }

    /// <summary>
    /// Define las filas del grid
    /// </summary>
    public Grid Rows(params GridLength[] rows)
    {
        RowDefinitions.Clear();
        RowDefinitions.AddRange(rows);
        MarkNeedsLayout();
        return this;
    }

    /// <summary>
    /// Define las columnas del grid
    /// </summary>
    public Grid Columns(params GridLength[] columns)
    {
        ColumnDefinitions.Clear();
        ColumnDefinitions.AddRange(columns);
        MarkNeedsLayout();
        return this;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        const float InfiniteThreshold = float.PositiveInfinity;

        // Calculate auto sizes for Measure
        var autoRowHeights = new float[RowDefinitions.Count];
        var autoColWidths = new float[ColumnDefinitions.Count];

        if (RowDefinitions.Count > 0 && ColumnDefinitions.Count > 0)
        {
            foreach (var child in ChildrenInternal.ToArray())
            {
                if (_cellPositions.TryGetValue(child, out var pos))
                {
                    // Pass available space to children for measurement
                    // Note: This is an approximation as we don't know the exact cell size yet
                    // But for Auto/content sizing, this usually works
                    child.Measure(
                        availableWidth - Padding.Horizontal,
                        availableHeight - Padding.Vertical
                    );

                    // Track maximum size needed for Auto rows/cols
                    if (pos.row < RowDefinitions.Count && RowDefinitions[pos.row].Type == GridLength.GridUnitType.Auto)
                    {
                        float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                        autoRowHeights[pos.row] = Math.Max(autoRowHeights[pos.row], childHeight + child.Margin.Vertical);
                    }

                    if (pos.col < ColumnDefinitions.Count && ColumnDefinitions[pos.col].Type == GridLength.GridUnitType.Auto)
                    {
                        float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;
                        autoColWidths[pos.col] = Math.Max(autoColWidths[pos.col], childWidth + child.Margin.Horizontal);
                    }
                }
            }
        }

        float measuredWidth = Width;
        float measuredHeight = Height;

        if (RowDefinitions.Count == 0 || ColumnDefinitions.Count == 0)
        {
            // Grid without definitions, use minimum size
            // We need to ensure children are measured first!
             foreach (var child in ChildrenInternal.ToArray())
            {
                child.Measure(
                    availableWidth - Padding.Horizontal,
                    availableHeight - Padding.Vertical
                );
            }

            if (!HasExplicitWidth)
            {
                measuredWidth = Padding.Horizontal;
            }
            if (!HasExplicitHeight)
            {
                measuredHeight = Padding.Vertical;
            }

            DesiredWidth = measuredWidth;
            DesiredHeight = measuredHeight;
            return;
        }

        // Calculate desired size based on grid definitions

        // Calculate total width based on column definitions
        float totalWidth = Padding.Horizontal;
        for (int i = 0; i < ColumnDefinitions.Count; i++)
        {
            var colDef = ColumnDefinitions[i];
            if (colDef.Type == GridLength.GridUnitType.Pixel)
            {
                totalWidth += colDef.Value;
            }
            else if (colDef.Type == GridLength.GridUnitType.Auto)
            {
                totalWidth += autoColWidths[i];
            }
        }
        if (ColumnDefinitions.Count > 1)
        {
            totalWidth += ColumnSpacing * (ColumnDefinitions.Count - 1);
        }

        if (!HasExplicitWidth)
        {
            // If we have star columns and available width is finite, use it
            bool hasStarColumns = ColumnDefinitions.Any(c => c.Type == GridLength.GridUnitType.Star);
            if (hasStarColumns && availableWidth < InfiniteThreshold)
            {
                measuredWidth = availableWidth;
            }
            else
            {
                measuredWidth = totalWidth;
            }
        }
        else
        {
             // Ensure we at least report the content size needed to avoid clipping if possible
             // (General fix for scroll scenarios)
             measuredWidth = Width;
        }

        // Calculate total height based on row definitions
        float totalHeight = Padding.Vertical;
        for (int i = 0; i < RowDefinitions.Count; i++)
        {
            var rowDef = RowDefinitions[i];
            if (rowDef.Type == GridLength.GridUnitType.Pixel)
            {
                totalHeight += rowDef.Value;
            }
            else if (rowDef.Type == GridLength.GridUnitType.Auto)
            {
                totalHeight += autoRowHeights[i];
            }
        }
        if (RowDefinitions.Count > 1)
        {
            totalHeight += RowSpacing * (RowDefinitions.Count - 1);
        }

        if (!HasExplicitHeight)
        {
            // If we have star rows and available height is finite, use it
            bool hasStarRows = RowDefinitions.Any(r => r.Type == GridLength.GridUnitType.Star);
            if (hasStarRows && availableHeight < InfiniteThreshold)
            {
                measuredHeight = availableHeight;
            }
            else
            {
                measuredHeight = totalHeight;
            }
        }
        else
        {
             // Ensure we at least report the content size needed to avoid clipping if possible
             // (General fix for scroll scenarios)
             measuredHeight = Height;
        }

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (RowDefinitions.Count == 0 || ColumnDefinitions.Count == 0)
            return;

        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        // Calculate auto sizes based on previous measure pass
        var autoRowHeights = new float[RowDefinitions.Count];
        var autoColWidths = new float[ColumnDefinitions.Count];

        foreach (var child in ChildrenInternal.ToArray())
        {
            if (_cellPositions.TryGetValue(child, out var pos))
            {
                // Only consider children in Auto rows/cols for auto sizing
                if (pos.row < RowDefinitions.Count && RowDefinitions[pos.row].Type == GridLength.GridUnitType.Auto)
                {
                    float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;
                    autoRowHeights[pos.row] = Math.Max(autoRowHeights[pos.row], childHeight + child.Margin.Vertical);
                }

                if (pos.col < ColumnDefinitions.Count && ColumnDefinitions[pos.col].Type == GridLength.GridUnitType.Auto)
                {
                    float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;
                    autoColWidths[pos.col] = Math.Max(autoColWidths[pos.col], childWidth + child.Margin.Horizontal);
                }
            }
        }

        // Calcular tamaños de filas y columnas
        var rowHeights = CalculateTrackSizes(RowDefinitions, contentHeight, RowSpacing, autoRowHeights);
        var colWidths = CalculateTrackSizes(ColumnDefinitions, contentWidth, ColumnSpacing, autoColWidths);

        // Calcular posiciones de inicio de cada fila/columna
        var rowPositions = new float[rowHeights.Length + 1];
        var colPositions = new float[colWidths.Length + 1];

        rowPositions[0] = y + Padding.Top;
        for (int i = 0; i < rowHeights.Length; i++)
        {
            rowPositions[i + 1] = rowPositions[i] + rowHeights[i];
            // Add spacing only if not the last row
            if (i < rowHeights.Length - 1)
            {
                rowPositions[i + 1] += RowSpacing;
            }
        }

        colPositions[0] = x + Padding.Left;
        for (int i = 0; i < colWidths.Length; i++)
        {
            colPositions[i + 1] = colPositions[i] + colWidths[i];
            // Add spacing only if not the last column
            if (i < colWidths.Length - 1)
            {
                colPositions[i + 1] += ColumnSpacing;
            }
        }

        // Posicionar hijos
        foreach (var child in ChildrenInternal.ToArray())
        {
            if (_cellPositions.TryGetValue(child, out var pos))
            {
                int row = Math.Min(pos.row, rowHeights.Length - 1);
                int col = Math.Min(pos.col, colWidths.Length - 1);
                int rowSpan = Math.Min(pos.rowSpan, rowHeights.Length - row);
                int colSpan = Math.Min(pos.colSpan, colWidths.Length - col);

                float cellX = colPositions[col];
                float cellY = rowPositions[row];

                // Calculate cell width and height (with span)
                // colPositions now correctly includes spacing only BETWEEN columns (not after last)
                // so we can simply calculate the difference
                float cellWidth = colPositions[col + colSpan] - colPositions[col];
                float cellHeight = rowPositions[row + rowSpan] - rowPositions[row];

                // Re-measure child with its specific cell size
                // This ensures child knows the actual space available in its cell
                child.Measure(cellWidth, cellHeight);

                // Calcular el espacio disponible para el hijo (restando márgenes)
                float availableWidth = cellWidth - child.Margin.Horizontal;
                float availableHeight = cellHeight - child.Margin.Vertical;

                float childX = cellX + child.Margin.Left;
                float childY = cellY + child.Margin.Top;

                // Use DesiredWidth/Height if explicit Width/Height not set
                float childWidth = child.Width > 0 ? child.Width : child.DesiredWidth;
                float childHeight = child.Height > 0 ? child.Height : child.DesiredHeight;

                // Aplicar alineación horizontal del hijo
                switch (child.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        // childX ya está en la posición correcta (inicio)
                        break;

                    case HorizontalAlignment.Center:
                        childX += (availableWidth - childWidth) / 2;
                        break;

                    case HorizontalAlignment.Right:
                        childX += availableWidth - childWidth;
                        break;

                    case HorizontalAlignment.Stretch:
                        childWidth = availableWidth;
                        break;
                }

                // Aplicar alineación vertical del hijo
                switch (child.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        // childY ya está en la posición correcta (inicio)
                        break;

                    case VerticalAlignment.Center:
                        childY += (availableHeight - childHeight) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        childY += availableHeight - childHeight;
                        break;

                    case VerticalAlignment.Stretch:
                        childHeight = availableHeight;
                        break;
                }

                child.Arrange(childX, childY, childWidth, childHeight);
            }
        }
    }

    private float[] CalculateTrackSizes(List<GridLength> definitions, float availableSpace, float spacing, float[] autoSizes)
    {
        int count = definitions.Count;
        var sizes = new float[count];
        float totalSpacing = spacing * (count - 1);
        float remainingSpace = availableSpace - totalSpacing;

        // Primera pasada: asignar tamaños fijos y auto
        float usedSpace = 0;
        float totalStars = 0;

        for (int i = 0; i < count; i++)
        {
            var def = definitions[i];
            switch (def.Type)
            {
                case GridLength.GridUnitType.Pixel:
                    sizes[i] = def.Value;
                    usedSpace += def.Value;
                    break;

                case GridLength.GridUnitType.Auto:
                    sizes[i] = autoSizes[i];
                    usedSpace += sizes[i];
                    break;

                case GridLength.GridUnitType.Star:
                    totalStars += def.Value;
                    break;
            }
        }

        // Segunda pasada: distribuir espacio restante entre estrellas
        if (totalStars > 0)
        {
            float spacePerStar = Math.Max(0, (remainingSpace - usedSpace) / totalStars);
            for (int i = 0; i < count; i++)
            {
                if (definitions[i].Type == GridLength.GridUnitType.Star)
                {
                    sizes[i] = spacePerStar * definitions[i].Value;
                }
            }
        }

        return sizes;
    }

    public override void Render(IRenderer renderer)
    {
        // Render background if it has color
        if (Background != null && Background.Opacity > 0 && Background.PrimaryColor.A > 0)
        {
            float renderHeight = Math.Max(ComputedHeight, DesiredHeight);
            float renderWidth = Math.Max(ComputedWidth, DesiredWidth);

            renderer.DrawRect(ComputedX, ComputedY, renderWidth, renderHeight, Background);
        }
    }
}
