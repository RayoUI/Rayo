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
        set => this.SetProperty(ref field, value, () => { Rebuild(); SelectionChanged?.Invoke(value); });
    } = -1;
    #endregion

    // Visual components
    private Grid? _grid;
    private ScrollView? _scrollView;
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
        if (_grid == null) return;

        // Create a container Grid for all data rows that shares column definitions with header
        var dataGrid = new Grid();
        dataGrid.RowSpacing = 0;
        dataGrid.ColumnSpacing = 0;
        dataGrid.HorizontalAlignment = HorizontalAlignment.Stretch;

        // Use EXACT same column definitions as the main grid
        foreach (var weight in _columnWeights)
        {
            dataGrid.ColumnDefinitions.Add(GridLength.Stars(weight));
        }

        // Add row definitions for each data row
        for (int row = 0; row < Items.Count; row++)
        {
            dataGrid.RowDefinitions.Add(GridLength.Pixels(RowHeight));
        }

        // Add cells to the data grid
        for (int row = 0; row < Items.Count; row++)
        {
            var item = Items[row];
            int rowIndex = row;
            bool isSelected = row == SelectedIndex;

            // Determine row background color
            Brush rowBg = isSelected ? SelectedRowColor :
                         (AlternatingRows && row % 2 == 1) ? AlternateRowColor :
                         RowBackground;

            for (int col = 0; col < Columns.Count; col++)
            {
                var column = Columns[col];
                var cellValue = GetCellValue(item, column);
                var textColor = isSelected ? SelectedTextColor : new Color(225, 229, 238);

                var cell = new Button();
                cell.Text(cellValue);
                cell.TextColor(textColor);
                cell.FontSize(13);
                cell.Background(rowBg);
                cell.HoverBackground(rowBg);
                cell.PressedBackground(isSelected ? SelectedRowColor : new Color(50, 55, 65));
                cell.BorderColor(ShowGridLines ? GridLineColor : Color.Transparent);
                cell.BorderWidth(ShowGridLines ? 1 : 0);
                cell.BorderRadius(0);
                cell.Padding(new Thickness(8, 0, 8, 0));
                cell.Height(RowHeight);
                cell.HorizontalAlignment(HorizontalAlignment.Stretch);
                cell.VerticalAlignment(VerticalAlignment.Stretch);
                cell.TextAlignment(HorizontalAlignment.Left);
                cell.OnTapped(() => SelectedIndex = rowIndex);

                dataGrid.AddChild(cell, row, col);
            }
        }

        // Wrap data grid in ScrollView
        _scrollView = new ScrollView();
        _scrollView.Content(dataGrid);
        _scrollView.HorizontalAlignment = HorizontalAlignment.Stretch;
        _scrollView.VerticalAlignment = VerticalAlignment.Stretch;

        // Add scrollview to main grid (row 1, spanning all columns including scrollbar spacer)
        _grid.AddChild(_scrollView, 1, 0, 1, Columns.Count + 1);
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

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : availableWidth;
        float measuredHeight = Height > 0 ? Height : availableHeight;

        _grid?.Measure(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        _grid?.Arrange(x, y, width, height);
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
