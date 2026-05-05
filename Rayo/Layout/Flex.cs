namespace Rayo.Layout;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Flex similar a CSS Flexbox y MAUI Flex.
/// Proporciona un contenedor flexible que puede organizar sus hijos en filas o columnas
/// con soporte para wrapping, alineación y distribución de espacio.
/// Migrated to new MAUI-like architecture: inherits from Rayo.Core.Layout<T>
/// </summary>
public class Flex : Layout<Flex>
{
    #region Properties

    /// <summary>
    /// Dirección del eje principal (row, column, row-reverse, column-reverse)
    /// </summary>
    [LayoutProperty]
    public FlexDirection Direction
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = FlexDirection.Row;

    /// <summary>
    /// Comportamiento de wrap cuando los elementos no caben en una línea
    /// </summary>
    [LayoutProperty]
    public FlexWrap Wrap
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = FlexWrap.NoWrap;

    /// <summary>
    /// Alineación de elementos en el eje principal
    /// </summary>
    [LayoutProperty]
    public JustifyContent JustifyContent
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = JustifyContent.Start;

    /// <summary>
    /// Alineación de elementos en el eje transversal (perpendicular)
    /// </summary>
    [LayoutProperty]
    public Alignment AlignItems
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Alignment.Stretch;

    /// <summary>
    /// Alineación de líneas cuando hay wrap (múltiples líneas)
    /// </summary>
    [LayoutProperty]
    public FlexAlignContent AlignContent
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = FlexAlignContent.Stretch;

    /// <summary>
    /// Espacio entre elementos en el eje principal
    /// </summary>
    [LayoutProperty]
    public float Gap
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    /// <summary>
    /// Espacio entre líneas cuando hay wrap
    /// </summary>
    [LayoutProperty]
    public float RowGap
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    #endregion

    #region Attached Properties (per-child)

    // Dictionary para almacenar propiedades de los hijos
    private readonly Dictionary<VisualElement, FlexItemProperties> _itemProperties = new();

    private class FlexItemProperties
    {
        public int Order { get; set; } = 0;
        public float Grow { get; set; } = 0;
        public float Shrink { get; set; } = 1;
        public float Basis { get; set; } = float.NaN; // NaN = auto
        public Alignment? AlignSelf { get; set; } = null; // null = usar AlignItems del contenedor
    }

    private FlexItemProperties GetItemProperties(VisualElement element)
    {
        if (!_itemProperties.TryGetValue(element, out var props))
        {
            props = new FlexItemProperties();
            _itemProperties[element] = props;
        }
        return props;
    }

    #endregion

    #region Fluent API for Child Properties

    /// <summary>
    /// Establece el orden del elemento (menor = primero)
    /// </summary>
    public Flex Order(VisualElement child, int order)
    {
        GetItemProperties(child).Order = order;
        MarkNeedsLayout();
        return this;
    }

    /// <summary>
    /// Establece el factor de crecimiento (0 = no crece, 1 = crece proporcionalmente)
    /// </summary>
    public Flex Grow(VisualElement child, float grow)
    {
        GetItemProperties(child).Grow = Math.Max(0, grow);
        MarkNeedsLayout();
        return this;
    }

    /// <summary>
    /// Establece el factor de encogimiento (0 = no encoge, 1 = encoge proporcionalmente)
    /// </summary>
    public Flex Shrink(VisualElement child, float shrink)
    {
        GetItemProperties(child).Shrink = Math.Max(0, shrink);
        MarkNeedsLayout();
        return this;
    }

    /// <summary>
    /// Establece el tamaño base del elemento antes de aplicar grow/shrink
    /// </summary>
    public Flex Basis(VisualElement child, float basis)
    {
        GetItemProperties(child).Basis = basis;
        MarkNeedsLayout();
        return this;
    }

    /// <summary>
    /// Establece la alineación individual del elemento (override de AlignItems)
    /// </summary>
    public Flex AlignSelf(VisualElement child, Alignment alignSelf)
    {
        GetItemProperties(child).AlignSelf = alignSelf;
        MarkNeedsLayout();
        return this;
    }

    #endregion

    #region Constructor

    public Flex()
    {
        // Default to stretch like other layouts
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ShouldExpand = false;
    }

    public Flex(params VisualElement[] children) : this()
    {
        foreach (var child in children)
        {
            AddChild(child);
        }
    }
    #endregion

    #region Layout Algorithm

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Aplicar tamaño explícito si existe
        float containerWidth = HasExplicitWidth ? Width : availableWidth;
        float containerHeight = HasExplicitHeight ? Height : availableHeight;

        // Espacio disponible después de padding
        float contentWidth = containerWidth - Padding.Horizontal;
        float contentHeight = containerHeight - Padding.Vertical;

        // Determinar si el eje principal es horizontal o vertical
        bool isRow = Direction == FlexDirection.Row || Direction == FlexDirection.RowReverse;

        // Ordenar hijos por Order
        var orderedChildren = Children.OrderBy(c => GetItemProperties(c).Order).ToList();

        if (orderedChildren.Count == 0)
        {
            DesiredWidth = HasExplicitWidth ? Width : Padding.Horizontal;
            DesiredHeight = HasExplicitHeight ? Height : Padding.Vertical;
            return;
        }

        // Medir todos los hijos
        var childMeasurements = new List<ChildMeasurement>();
        foreach (var child in orderedChildren)
        {
            var props = GetItemProperties(child);

            // Calcular tamaño de medición
            float measureWidth = isRow ? float.PositiveInfinity : contentWidth;
            float measureHeight = isRow ? contentHeight : float.PositiveInfinity;

            child.Measure(measureWidth, measureHeight);

            float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;
            float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;

            childMeasurements.Add(new ChildMeasurement
            {
                Element = child,
                Properties = props,
                Width = childWidth,
                Height = childHeight
            });
        }

        // Calcular líneas (wrapping)
        var lines = CalculateLines(childMeasurements, contentWidth, contentHeight, isRow);

        // Calcular tamaño deseado del contenedor
        float totalWidth = 0;
        float totalHeight = 0;

        if (isRow)
        {
            // Para row: width = max de las líneas, height = suma de las líneas
            totalWidth = lines.Max(l => l.MainSize);
            totalHeight = lines.Sum(l => l.CrossSize) + (lines.Count - 1) * RowGap;
        }
        else
        {
            // Para column: width = suma de las líneas, height = max de las líneas
            totalWidth = lines.Sum(l => l.CrossSize) + (lines.Count - 1) * RowGap;
            totalHeight = lines.Max(l => l.MainSize);
        }

        DesiredWidth = HasExplicitWidth ? Width : totalWidth + Padding.Horizontal;
        DesiredHeight = HasExplicitHeight ? Height : totalHeight + Padding.Vertical;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Espacio de contenido
        float contentX = x + Padding.Left;
        float contentY = y + Padding.Top;
        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        // Determinar dirección
        bool isRow = Direction == FlexDirection.Row || Direction == FlexDirection.RowReverse;
        bool isReverse = Direction == FlexDirection.RowReverse || Direction == FlexDirection.ColumnReverse;

        // Ordenar hijos
        var orderedChildren = Children.OrderBy(c => GetItemProperties(c).Order).ToList();

        if (orderedChildren.Count == 0) return;

        // Medir y crear lista de mediciones
        var childMeasurements = new List<ChildMeasurement>();
        foreach (var child in orderedChildren)
        {
            var props = GetItemProperties(child);

            float measureWidth = isRow ? float.PositiveInfinity : contentWidth;
            float measureHeight = isRow ? contentHeight : float.PositiveInfinity;

            child.Measure(measureWidth, measureHeight);

            float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;
            float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;

            childMeasurements.Add(new ChildMeasurement
            {
                Element = child,
                Properties = props,
                Width = childWidth,
                Height = childHeight
            });
        }

        // Calcular líneas con wrapping
        var lines = CalculateLines(childMeasurements, contentWidth, contentHeight, isRow);

        // Aplicar grow/shrink a cada línea
        foreach (var line in lines)
        {
            float availableSpace = isRow ? contentWidth : contentHeight;
            ApplyFlexGrowShrink(line, availableSpace, isRow);
        }

        // Posicionar líneas según AlignContent
        float crossStart = isRow ? contentY : contentX;
        float crossSize = isRow ? contentHeight : contentWidth;
        PositionLines(lines, crossStart, crossSize, isRow);

        // Posicionar elementos dentro de cada línea
        foreach (var line in lines)
        {
            PositionItemsInLine(line, contentX, contentY, contentWidth, contentHeight, isRow, isReverse);
        }
    }

    #endregion

    #region Helper Methods

    private class ChildMeasurement
    {
        public VisualElement Element { get; set; } = null!;
        public FlexItemProperties Properties { get; set; } = null!;
        public float Width { get; set; }
        public float Height { get; set; }
        public float FinalMainSize { get; set; }
        public float FinalCrossSize { get; set; }
    }

    private class FlexLine
    {
        public List<ChildMeasurement> Items { get; set; } = new();
        public float MainSize { get; set; }
        public float CrossSize { get; set; }
        public float CrossPosition { get; set; }
    }

    private List<FlexLine> CalculateLines(List<ChildMeasurement> items, float maxWidth, float maxHeight, bool isRow)
    {
        var lines = new List<FlexLine>();
        var currentLine = new FlexLine();
        lines.Add(currentLine);

        float currentMainSize = 0;

        foreach (var item in items)
        {
            float itemMainSize = isRow ? item.Width : item.Height;
            float itemCrossSize = isRow ? item.Height : item.Width;

            // Aplicar basis si está definido
            if (!float.IsNaN(item.Properties.Basis))
            {
                itemMainSize = item.Properties.Basis;
            }

            // Agregar gap si no es el primer elemento de la línea
            float gapSize = currentLine.Items.Count > 0 ? Gap : 0;
            float maxMainSize = isRow ? maxWidth : maxHeight;

            // Verificar si necesitamos wrap
            if (Wrap != FlexWrap.NoWrap && currentLine.Items.Count > 0 &&
                currentMainSize + gapSize + itemMainSize > maxMainSize)
            {
                // Crear nueva línea
                currentLine = new FlexLine();
                lines.Add(currentLine);
                currentMainSize = 0;
                gapSize = 0;
            }

            // Agregar a la línea actual
            item.FinalMainSize = itemMainSize;
            item.FinalCrossSize = itemCrossSize;
            currentLine.Items.Add(item);
            currentMainSize += gapSize + itemMainSize;

            // Actualizar tamaños de línea
            currentLine.MainSize = currentMainSize;
            currentLine.CrossSize = Math.Max(currentLine.CrossSize, itemCrossSize);
        }

        // Invertir líneas si es WrapReverse
        if (Wrap == FlexWrap.WrapReverse)
        {
            lines.Reverse();
        }

        return lines;
    }

    private void ApplyFlexGrowShrink(FlexLine line, float availableSpace, bool isRow)
    {
        if (line.Items.Count == 0) return;

        float totalItemSize = line.Items.Sum(i => i.FinalMainSize);
        float totalGap = (line.Items.Count - 1) * Gap;
        float usedSpace = totalItemSize + totalGap;
        float freeSpace = availableSpace - usedSpace;

        if (freeSpace > 0)
        {
            // Aplicar grow
            float totalGrow = line.Items.Sum(i => i.Properties.Grow);
            if (totalGrow > 0)
            {
                foreach (var item in line.Items)
                {
                    if (item.Properties.Grow > 0)
                    {
                        float growAmount = (freeSpace * item.Properties.Grow) / totalGrow;
                        item.FinalMainSize += growAmount;
                    }
                }
            }
        }
        else if (freeSpace < 0)
        {
            // Aplicar shrink
            float totalShrink = line.Items.Sum(i => i.Properties.Shrink * i.FinalMainSize);
            if (totalShrink > 0)
            {
                foreach (var item in line.Items)
                {
                    if (item.Properties.Shrink > 0)
                    {
                        float shrinkAmount = (Math.Abs(freeSpace) * item.Properties.Shrink * item.FinalMainSize) / totalShrink;
                        item.FinalMainSize = Math.Max(0, item.FinalMainSize - shrinkAmount);
                    }
                }
            }
        }
    }

    private void PositionLines(List<FlexLine> lines, float crossStart, float crossSize, bool isRow)
    {
        if (lines.Count == 0) return;

        float totalCrossSize = lines.Sum(l => l.CrossSize);
        float totalGap = (lines.Count - 1) * RowGap;
        float usedCrossSize = totalCrossSize + totalGap;
        float freeCrossSpace = crossSize - usedCrossSize;

        float currentPos = crossStart;

        switch (AlignContent)
        {
            case FlexAlignContent.Start:
                foreach (var line in lines)
                {
                    line.CrossPosition = currentPos;
                    currentPos += line.CrossSize + RowGap;
                }
                break;

            case FlexAlignContent.End:
                currentPos += freeCrossSpace;
                foreach (var line in lines)
                {
                    line.CrossPosition = currentPos;
                    currentPos += line.CrossSize + RowGap;
                }
                break;

            case FlexAlignContent.Center:
                currentPos += freeCrossSpace / 2;
                foreach (var line in lines)
                {
                    line.CrossPosition = currentPos;
                    currentPos += line.CrossSize + RowGap;
                }
                break;

            case FlexAlignContent.SpaceBetween:
                if (lines.Count == 1)
                {
                    lines[0].CrossPosition = crossStart;
                }
                else
                {
                    float spacing = freeCrossSpace / (lines.Count - 1);
                    foreach (var line in lines)
                    {
                        line.CrossPosition = currentPos;
                        currentPos += line.CrossSize + spacing;
                    }
                }
                break;

            case FlexAlignContent.SpaceAround:
                float halfSpace = freeCrossSpace / (lines.Count * 2);
                currentPos += halfSpace;
                foreach (var line in lines)
                {
                    line.CrossPosition = currentPos;
                    currentPos += line.CrossSize + halfSpace * 2;
                }
                break;

            case FlexAlignContent.Stretch:
                if (freeCrossSpace > 0 && lines.Count > 0)
                {
                    float additionalCross = freeCrossSpace / lines.Count;
                    foreach (var line in lines)
                    {
                        line.CrossPosition = currentPos;
                        line.CrossSize += additionalCross;
                        currentPos += line.CrossSize + RowGap;
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        line.CrossPosition = currentPos;
                        currentPos += line.CrossSize + RowGap;
                    }
                }
                break;
        }
    }

    private void PositionItemsInLine(FlexLine line, float contentX, float contentY,
        float contentWidth, float contentHeight, bool isRow, bool isReverse)
    {
        if (line.Items.Count == 0) return;

        float totalMainSize = line.Items.Sum(i => i.FinalMainSize);
        float totalGap = (line.Items.Count - 1) * Gap;
        float usedMainSize = totalMainSize + totalGap;

        float mainStart = isRow ? contentX : contentY;
        float mainSize = isRow ? contentWidth : contentHeight;
        float freeMainSpace = mainSize - usedMainSize;

        float currentMainPos = mainStart;

        // Aplicar JustifyContent
        switch (JustifyContent)
        {
            case JustifyContent.Start:
                // Ya está en start
                break;

            case JustifyContent.End:
                currentMainPos += freeMainSpace;
                break;

            case JustifyContent.Center:
                currentMainPos += freeMainSpace / 2;
                break;

            case JustifyContent.SpaceBetween:
                // Espacio entre elementos, se calcula por elemento
                break;

            case JustifyContent.SpaceAround:
                // Espacio alrededor de elementos
                currentMainPos += (freeMainSpace / line.Items.Count) / 2;
                break;

            case JustifyContent.SpaceEvenly:
                currentMainPos += freeMainSpace / (line.Items.Count + 1);
                break;
        }

        // Calcular spacing para SpaceBetween y SpaceAround
        float itemSpacing = Gap;
        if (JustifyContent == JustifyContent.SpaceBetween && line.Items.Count > 1)
        {
            itemSpacing = Gap + freeMainSpace / (line.Items.Count - 1);
        }
        else if (JustifyContent == JustifyContent.SpaceAround)
        {
            itemSpacing = Gap + freeMainSpace / line.Items.Count;
        }
        else if (JustifyContent == JustifyContent.SpaceEvenly)
        {
            itemSpacing = freeMainSpace / (line.Items.Count + 1);
        }

        // Invertir si es reverse
        var itemsToArrange = isReverse ? line.Items.AsEnumerable().Reverse().ToList() : line.Items;

        foreach (var item in itemsToArrange)
        {
            // Determinar alineación cross
            var alignment = item.Properties.AlignSelf ?? AlignItems;
            float crossPos = line.CrossPosition;
            float crossItemSize = item.FinalCrossSize;

            switch (alignment)
            {
                case Alignment.Start:
                    // Ya está en start
                    break;

                case Alignment.End:
                    crossPos += line.CrossSize - crossItemSize;
                    break;

                case Alignment.Center:
                    crossPos += (line.CrossSize - crossItemSize) / 2;
                    break;

                case Alignment.Stretch:
                    crossItemSize = line.CrossSize;
                    break;
            }

            // Posicionar elemento
            float finalX, finalY, finalWidth, finalHeight;

            if (isRow)
            {
                finalX = currentMainPos;
                finalY = crossPos;
                finalWidth = item.FinalMainSize;
                finalHeight = crossItemSize;
            }
            else
            {
                finalX = crossPos;
                finalY = currentMainPos;
                finalWidth = crossItemSize;
                finalHeight = item.FinalMainSize;
            }

            item.Element.Arrange(finalX, finalY, finalWidth, finalHeight);

            currentMainPos += item.FinalMainSize + itemSpacing;
        }
    }

    #endregion

    #region Rendering

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

    #endregion
}
