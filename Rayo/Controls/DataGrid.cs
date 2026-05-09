namespace Rayo.Controls;

using Rayo;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Column definition for DataGrid
/// </summary>
public class DataGridColumn
{
    public string Header { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public float Width { get; set; } = 100;
    public bool CanSort { get; set; } = true;
    public Func<object, string>? ValueFormatter { get; set; }

    public DataGridColumn(string header, string propertyName, float width = 100)
    {
        Header = header;
        PropertyName = propertyName;
        Width = width;
    }
}

/// <summary>
/// Sort direction for DataGrid columns
/// </summary>
public enum SortDirection
{
    None,
    Ascending,
    Descending
}

/// <summary>
/// DataGrid component for displaying tabular data
/// </summary>
public class DataGrid : CompositeView<DataGrid>
{
    private string? _sortColumn = null;
    private SortDirection _sortDirection = SortDirection.None;

    #region Columns
    [LayoutProperty]
    public List<DataGridColumn> Columns
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = new();
    #endregion

    #region Items
    [LayoutProperty]
    public List<object> Items
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { ApplySorting(); Rebuild(); });
    } = new();
    #endregion

    #region SelectedIndex
    public int SelectedIndex
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            Rebuild();
            EnsureSelectedRowVisible();
            SelectionChanged?.Invoke(value);
        });
    } = -1;
    #endregion

    // Visual components
    private Grid? _grid;
    private ScrollView? _scrollView;
    private VirtualizedDataGridRowsPanel? _rowsPanel;
    private readonly List<float> _columnWeights = new();
    private const float ScrollbarSpacerWidth = 8f; // Match ScrollView's default ScrollbarWidth

    // Styling
    #region HeaderBackground
    public Rendering.Brushes.Brush HeaderBackground
    {
        get => field;
        set
        {
            field = value;
            MarkNeedsPaint();
        }
    } = new Color(45, 48, 58);
    #endregion

    #region HeaderTextColor
    public Brush HeaderTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region RowBackground
    public Rendering.Brushes.Brush RowBackground
    {
        get => field;
        set
        {
            field = value;
            MarkNeedsPaint();
        }
    } = new Color(30, 32, 40);
    #endregion

    #region AlternateRowColor
    [PaintProperty]
    public Brush AlternateRowColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(35, 37, 45);
    #endregion

    #region SelectedRowColor
    public Brush SelectedRowColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region SelectedTextColor
    public Brush SelectedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region BorderColor
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(50, 55, 65);
    #endregion

    #region GridLineColor
    public Brush GridLineColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(50, 55, 65);
    #endregion

    #region RowHeight
    public float RowHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 32;
    #endregion

    #region HeaderHeight
    public float HeaderHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 36;
    #endregion

    #region ShowGridLines
    public bool ShowGridLines
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    #region AlternatingRows
    [LayoutProperty]
    public bool AlternatingRows
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    // Events
    public event Action<int>? SelectionChanged;
    public event Action<string, SortDirection>? ColumnSorted;

    public DataGrid()
    {
        Width = 600;
        Height = 400;
        BorderRadius = new CornerRadius(8);
        BuildGrid();
    }

    private void BuildGrid()
    {
        _grid = new Grid();
        _scrollView = new ScrollView();
        _scrollView.HorizontalAlignment = HorizontalAlignment.Stretch;
        _scrollView.VerticalAlignment = VerticalAlignment.Stretch;
        _rowsPanel = new VirtualizedDataGridRowsPanel(_scrollView);
        _scrollView.Content(_rowsPanel);
        ApplyGridStyling();
        AddChild(_grid);
    }

    public DataGrid AddColumn(DataGridColumn column)
    {
        Columns = [..Columns, column];
        Rebuild();
        return this;
    }

    public DataGrid AddColumn(string header, string propertyName, float width = 100)
    {
        return AddColumn(new DataGridColumn(header, propertyName, width));
    }


    public object? GetSelectedItem()
    {
        return SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;
    }

    private void Rebuild()
    {
        if (_grid == null) return;

        // Preserve scroll position before rebuild
        float savedScrollOffset = _scrollView?.VerticalScrollOffset ?? 0;

        _grid.ClearChildren();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        UpdateColumnWeights();
        ApplyGridStyling();

        if (Columns.Count == 0)
            return;

        // Define columns for header (data columns + scrollbar spacer)
        for (int i = 0; i < Columns.Count; i++)
        {
            float weight = _columnWeights.Count > i ? _columnWeights[i] : 1f;
            _grid.ColumnDefinitions.Add(GridLength.Stars(weight));
        }
        // Add scrollbar spacer column to align headers with data (ScrollView scrollbar takes space)
        _grid.ColumnDefinitions.Add(GridLength.Pixels(ScrollbarSpacerWidth));

        // Define rows: 1 header + 1 scrollable area for data
        _grid.RowDefinitions.Add(GridLength.Pixels(HeaderHeight));
        _grid.RowDefinitions.Add(GridLength.Star);

        // Build header cells directly into main grid
        BuildHeaderRow();

        // Build data rows into scrollable area
        BuildDataRows();

        // Restore scroll position after rebuild
        if (_scrollView != null && savedScrollOffset > 0)
        {
            _scrollView.VerticalScrollOffset = savedScrollOffset;
        }

        MarkNeedsLayout();
    }

    private void BuildHeaderRow()
    {
        if (_grid == null) return;

        for (int col = 0; col < Columns.Count; col++)
        {
            var column = Columns[col];

            var headerText = column.Header;

            // Build header text with optional sort icon suffix
            string displayText = headerText;
            if (_sortColumn == column.PropertyName)
            {
                displayText = _sortDirection == SortDirection.Ascending
                    ? $"{headerText} ▲"
                    : $"{headerText} ▼";
            }

            // Use Button directly as header cell for clickable sorting
            var headerCell = new Button();
            headerCell.Text(displayText);
            headerCell.TextColor(HeaderTextColor);
            headerCell.FontSize(14);
            headerCell.Background(HeaderBackground);
            headerCell.HoverBackground(column.CanSort ? (Rendering.Brushes.Brush)new Color(58, 58, 70) : HeaderBackground);
            headerCell.PressedBackground(column.CanSort ? (Rendering.Brushes.Brush)new Color(68, 68, 80) : HeaderBackground);
            headerCell.BorderColor(BorderColor);
            headerCell.BorderWidth(1);
            headerCell.BorderRadius(0);
            headerCell.Padding(new Thickness(8, 0, 8, 0));
            headerCell.Height(HeaderHeight);
            headerCell.HorizontalAlignment(HorizontalAlignment.Stretch);
            headerCell.VerticalAlignment(VerticalAlignment.Stretch);
            headerCell.TextAlignment(HorizontalAlignment.Left);

            if (column.CanSort)
            {
                headerCell.OnTapped(() => SortByColumn(column.PropertyName));
            }

            _grid.AddChild(headerCell, 0, col);
        }

        // Add scrollbar spacer element in header row
        var scrollbarSpacer = new Frame();
        scrollbarSpacer.Background = HeaderBackground;
        scrollbarSpacer.Height = HeaderHeight;
        scrollbarSpacer.HorizontalAlignment = HorizontalAlignment.Stretch;
        scrollbarSpacer.VerticalAlignment = VerticalAlignment.Stretch;
        _grid.AddChild(scrollbarSpacer, 0, Columns.Count);
    }

    private void BuildDataRows()
    {
        if (_grid == null || _scrollView == null || _rowsPanel == null) return;

        _rowsPanel.Configure(
            Items,
            Columns,
            _columnWeights,
            RowHeight,
            AlternatingRows,
            ShowGridLines,
            CreateVirtualizedRow,
            BindVirtualizedRow);

        // Add scrollview to main grid (row 1, spanning all columns including scrollbar spacer)
        _grid.AddChild(_scrollView, 1, 0, 1, Columns.Count + 1);
    }

    private VisualElement CreateVirtualizedRow()
    {
        return new RecyclableDataGridRow();
    }

    private void BindVirtualizedRow(VisualElement element, int rowIndex)
    {
        if (element is not RecyclableDataGridRow row)
            return;

        var item = Items[rowIndex];
        bool isSelected = rowIndex == SelectedIndex;
        Brush rowBg = isSelected ? SelectedRowColor :
                     (AlternatingRows && rowIndex % 2 == 1) ? AlternateRowColor :
                     RowBackground;
        var textColor = isSelected ? SelectedTextColor : new Color(225, 229, 238);
        var pressedBackground = isSelected ? SelectedRowColor : new Color(50, 55, 65);
        var borderBrush = ShowGridLines ? GridLineColor : Color.Transparent;
        float borderWidth = ShowGridLines ? 1 : 0;

        var cellValues = new string[Columns.Count];
        for (int col = 0; col < Columns.Count; col++)
        {
            cellValues[col] = GetCellValue(item, Columns[col]);
        }

        row.Bind(
            cellValues,
            _columnWeights,
            RowHeight,
            rowBg,
            textColor,
            pressedBackground,
            borderBrush,
            borderWidth,
            () => SelectedIndex = rowIndex);
    }

    private void ApplyGridStyling()
    {
        if (_grid == null) return;

        _grid.Background = RowBackground;
        _grid.Padding = new Thickness(0);
    }

    private void UpdateColumnWeights()
    {
        _columnWeights.Clear();

        if (Columns.Count == 0)
        {
            return;
        }

        foreach (var column in Columns)
        {
            float weight = column.Width > 0 ? column.Width : 1f;
            _columnWeights.Add(weight);
        }
    }

    private string GetCellValue(object item, DataGridColumn column)
    {
        try
        {
            var property = item.GetType().GetProperty(column.PropertyName);
            if (property != null)
            {
                var value = property.GetValue(item);

                if (column.ValueFormatter != null)
                {
                    return column.ValueFormatter(item);
                }

                return value?.ToString() ?? "";
            }
        }
        catch
        {
            // Ignore errors
        }

        return "";
    }

    private void SortByColumn(string propertyName)
    {
        if (_sortColumn == propertyName)
        {
            // Toggle sort direction
            _sortDirection = _sortDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }
        else
        {
            _sortColumn = propertyName;
            _sortDirection = SortDirection.Ascending;
        }

        ApplySorting();
        Rebuild();

        ColumnSorted?.Invoke(_sortColumn, _sortDirection);
    }

    private void ApplySorting()
    {
        if (string.IsNullOrEmpty(_sortColumn) || _sortDirection == SortDirection.None)
            return;

        var sortedItems = _sortDirection == SortDirection.Ascending
            ? Items.OrderBy(item => GetPropertyValue(item, _sortColumn)).ToList()
            : Items.OrderByDescending(item => GetPropertyValue(item, _sortColumn)).ToList();

        // Clear and refill the list in-place instead of reassigning
        Items.Clear();
        Items.AddRange(sortedItems);
    }

    private object? GetPropertyValue(object item, string propertyName)
    {
        try
        {
            var property = item.GetType().GetProperty(propertyName);
            return property?.GetValue(item);
        }
        catch
        {
            return null;
        }
    }

    private void EnsureSelectedRowVisible()
    {
        if (_scrollView == null || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        float rowY = SelectedIndex * Math.Max(1, RowHeight);
        _scrollView.EnsureRectVisible(0, rowY, 1, Math.Max(1, RowHeight));
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : availableWidth;
        float measuredHeight = Height > 0 ? Height : availableHeight;

        _grid?.MeasureUpdate(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        _grid?.ArrangeUpdate(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius.TopLeft, RowBackground);

        _grid?.Render(renderer);

        if (BorderColor.PrimaryColor.A > 0)
        {
            renderer.DrawRoundedRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius.TopLeft, 1, BorderColor);
        }
    }
}

internal sealed class VirtualizedDataGridRowsPanel : CompositeView<VirtualizedDataGridRowsPanel>
{
    private readonly ScrollView _ownerScrollView;
    private IList<object> _items = Array.Empty<object>();
    private IReadOnlyList<DataGridColumn> _columns = Array.Empty<DataGridColumn>();
    private IReadOnlyList<float> _columnWeights = Array.Empty<float>();
    private float _rowHeight;
    private bool _alternatingRows;
    private bool _showGridLines;
    private Func<VisualElement>? _rowFactory;
    private Action<VisualElement, int>? _rowBinder;
    private readonly Dictionary<int, VisualElement> _activeRows = new();
    private readonly Stack<VisualElement> _recycledRows = new();
    private int _firstMaterializedRow = -1;
    private int _lastMaterializedRow = -1;
    private int _version;
    private int _materializedVersion = -1;
    private const int OverscanRows = 2;

    public VirtualizedDataGridRowsPanel(ScrollView ownerScrollView)
    {
        _ownerScrollView = ownerScrollView;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public void Configure(
        IList<object> items,
        IReadOnlyList<DataGridColumn> columns,
        IReadOnlyList<float> columnWeights,
        float rowHeight,
        bool alternatingRows,
        bool showGridLines,
        Func<VisualElement> rowFactory,
        Action<VisualElement, int> rowBinder)
    {
        _items = items ?? Array.Empty<object>();
        _columns = columns ?? Array.Empty<DataGridColumn>();
        _columnWeights = columnWeights ?? Array.Empty<float>();
        _rowHeight = rowHeight;
        _alternatingRows = alternatingRows;
        _showGridLines = showGridLines;
        _rowFactory = rowFactory;
        _rowBinder = rowBinder;
        _version++;
        _firstMaterializedRow = -1;
        _lastMaterializedRow = -1;
        MarkNeedsLayout();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsInfinity(availableWidth) || availableWidth <= 0 ? Width : availableWidth;
        DesiredHeight = _items.Count * Math.Max(1, _rowHeight);
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_rowFactory == null || _rowBinder == null || _items.Count == 0 || _columns.Count == 0)
        {
            ClearMaterializedRows();
            return;
        }

        float rowExtent = Math.Max(1, _rowHeight);
        float viewportHeight = Math.Max(0, _ownerScrollView.ComputedHeight - _ownerScrollView.Padding.Vertical);
        float scrollOffset = _ownerScrollView.VerticalScrollOffset;

        int firstVisible = Math.Max(0, (int)MathF.Floor(scrollOffset / rowExtent) - OverscanRows);
        int visibleCount = Math.Max(1, (int)MathF.Ceiling(viewportHeight / rowExtent) + OverscanRows * 2);
        int lastVisible = Math.Min(_items.Count - 1, firstVisible + visibleCount - 1);

        if (firstVisible != _firstMaterializedRow ||
            lastVisible != _lastMaterializedRow ||
            _materializedVersion != _version)
        {
            MaterializeRange(firstVisible, lastVisible);
        }

        foreach (var pair in Children.Select((child, localIndex) => (child, localIndex)))
        {
            int rowIndex = _firstMaterializedRow + pair.localIndex;
            float rowY = y + rowIndex * rowExtent;
            pair.child.ArrangeUpdate(x, rowY, width, rowExtent);
        }
    }

    public override void Render(IRenderer renderer)
    {
    }

    private void MaterializeRange(int firstRow, int lastRow)
    {
        var requiredRows = new HashSet<int>();
        for (int row = firstRow; row <= lastRow; row++)
        {
            requiredRows.Add(row);
        }

        var orderedRows = new List<VisualElement>(requiredRows.Count);

        foreach (var active in _activeRows.ToArray())
        {
            if (requiredRows.Contains(active.Key))
                continue;

            active.Value.Parent = null;
            _activeRows.Remove(active.Key);
            _recycledRows.Push(active.Value);
        }

        for (int row = firstRow; row <= lastRow; row++)
        {
            if (!_activeRows.TryGetValue(row, out var rowElement))
            {
                rowElement = _recycledRows.Count > 0 ? _recycledRows.Pop() : _rowFactory!();
                _activeRows[row] = rowElement;
            }

            _rowBinder!(rowElement, row);
            orderedRows.Add(rowElement);
        }

        Children = orderedRows;
        _firstMaterializedRow = firstRow;
        _lastMaterializedRow = lastRow;
        _materializedVersion = _version;
    }

    private void ClearMaterializedRows()
    {
        if (Children.Count == 0 && _firstMaterializedRow == -1 && _lastMaterializedRow == -1)
            return;

        foreach (var child in Children)
        {
            child.Parent = null;
            _recycledRows.Push(child);
        }

        Children = [];
        _activeRows.Clear();
        _firstMaterializedRow = -1;
        _lastMaterializedRow = -1;
        _materializedVersion = _version;
    }
}

internal sealed class RecyclableDataGridRow : CompositeView<RecyclableDataGridRow>
{
    private readonly List<Button> _cells = new();
    private IReadOnlyList<float> _columnWeights = Array.Empty<float>();
    private float _rowHeight;

    public RecyclableDataGridRow()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public void Bind(
        IReadOnlyList<string> cellValues,
        IReadOnlyList<float> columnWeights,
        float rowHeight,
        Brush rowBackground,
        Brush textColor,
        Brush pressedBackground,
        Brush borderBrush,
        float borderWidth,
        Action onTap)
    {
        EnsureCellCount(cellValues.Count);
        _columnWeights = columnWeights;
        _rowHeight = rowHeight;

        for (int i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            cell.Text = cellValues[i];
            cell.TextColor = textColor;
            cell.FontSize = 13;
            cell.Background = rowBackground;
            cell.HoverBackground = rowBackground;
            cell.PressedBackground = pressedBackground;
            cell.BorderColor = borderBrush;
            cell.BorderWidth = borderWidth;
            cell.BorderRadius = new CornerRadius(0);
            cell.Padding = new Thickness(8, 0, 8, 0);
            cell.Height = rowHeight;
            cell.HorizontalAlignment = HorizontalAlignment.Stretch;
            cell.VerticalAlignment = VerticalAlignment.Stretch;
            cell.TextAlignment = HorizontalAlignment.Left;
            cell.OnTapped(onTap);
        }
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float width = float.IsInfinity(availableWidth) || availableWidth <= 0 ? Width : availableWidth;
        float totalWeight = Math.Max(1f, _columnWeights.Sum());

        for (int i = 0; i < _cells.Count; i++)
        {
            float weight = i < _columnWeights.Count ? _columnWeights[i] : 1f;
            float cellWidth = width * (weight / totalWeight);
            _cells[i].MeasureUpdate(cellWidth, _rowHeight);
        }

        DesiredWidth = width;
        DesiredHeight = _rowHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float totalWeight = Math.Max(1f, _columnWeights.Sum());
        float currentX = x;

        for (int i = 0; i < _cells.Count; i++)
        {
            float weight = i < _columnWeights.Count ? _columnWeights[i] : 1f;
            float cellWidth = i == _cells.Count - 1
                ? Math.Max(0, x + width - currentX)
                : width * (weight / totalWeight);
            _cells[i].ArrangeUpdate(currentX, y, cellWidth, height);
            currentX += cellWidth;
        }
    }

    public override void Render(IRenderer renderer)
    {
    }

    private void EnsureCellCount(int count)
    {
        while (_cells.Count < count)
        {
            var cell = new Button();
            _cells.Add(cell);
            AddChild(cell);
        }

        while (_cells.Count > count)
        {
            var cell = _cells[^1];
            _cells.RemoveAt(_cells.Count - 1);
            RemoveChild(cell);
        }
    }
}
