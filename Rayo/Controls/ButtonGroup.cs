namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Group of adjacent selectable buttons, similar to segmented controls and Bootstrap button groups.
/// </summary>
public class ButtonGroup : BorderCompositeView<ButtonGroup>
{
    private readonly List<ButtonGroupItem> _buttons = [];

    #region Items
    [LayoutProperty]
    public IList<string> Items
    {
        get => field;
        set
        {
            field = value ?? [];
            NormalizeSelection();
            RebuildButtons();
        }
    } = [];
    #endregion

    #region SelectedIndex
    [PaintProperty]
    public int SelectedIndex
    {
        get => field;
        set
        {
            int normalized = NormalizeIndex(value);
            if (field == normalized)
            {
                return;
            }

            field = normalized;
            RefreshButtonStates();
            SelectedIndexChanged?.Invoke(field);
            SelectedItemChanged?.Invoke(SelectedItem);
            MarkNeedsPaint();
        }
    } = -1;
    #endregion

    #region Orientation
    [LayoutProperty]
    public Orientation Orientation
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            RefreshButtonStates();
            InvalidateMeasure();
        });
    } = Orientation.Horizontal;
    #endregion

    #region AllowDeselect
    [PaintProperty]
    public bool AllowDeselect
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region FontSize
    [LayoutProperty]
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildButtons);
    } = 14f;
    #endregion

    #region ItemPadding
    [LayoutProperty]
    public Thickness ItemPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildButtons);
    } = new Thickness(14, 7, 14, 7);
    #endregion

    #region BorderRadius
    [PaintProperty]
    public float GroupBorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, MathF.Max(0f, value), RefreshButtonStates);
    } = 6f;
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = Color.White;
    #endregion

    #region HoverBackground
    [PaintProperty]
    public Brush HoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = new Color(245, 248, 252);
    #endregion

    #region PressedBackground
    [PaintProperty]
    public Brush PressedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = new Color(224, 231, 255);
    #endregion

    #region SelectedBackground
    [PaintProperty]
    public Brush SelectedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = new Color(37, 99, 235);
    #endregion

    #region SelectedBorderBrush
    [PaintProperty]
    public Brush SelectedBorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = new Color(37, 99, 235);
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = new Color(51, 65, 85);
    #endregion

    #region SelectedTextColor
    [PaintProperty]
    public Brush SelectedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshButtonStates);
    } = Color.White;
    #endregion

    public string? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    public event Action<int>? SelectedIndexChanged;
    public event Action<string?>? SelectedItemChanged;

    public ButtonGroup()
    {
        Cursor = CursorShape.Hand;
        BorderThickness = 1f;
        BorderBrush = new Color(203, 213, 225);
    }

    protected override void OnBorderBrushChanged()
    {
        base.OnBorderBrushChanged();
        RefreshButtonStates();
    }

    protected override void OnBorderThicknessChanged()
    {
        base.OnBorderThicknessChanged();
        RebuildButtons();
    }

    public ButtonGroup AddItem(string item)
    {
        Items = [..Items, item];
        return this;
    }

    public ButtonGroup AddItems(params string[] items)
    {
        Items = [..Items, ..items];
        return this;
    }

    public ButtonGroup ClearItems()
    {
        Items = [];
        return this;
    }

    public ButtonGroup OnSelectedIndexChanged(Action<int> handler)
    {
        SelectedIndexChanged += handler;
        return this;
    }

    public ButtonGroup OnSelectedItemChanged(Action<string?> handler)
    {
        SelectedItemChanged += handler;
        return this;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float desiredWidth = 0f;
        float desiredHeight = 0f;

        foreach (var button in _buttons)
        {
            button.MeasureUpdate(availableWidth, availableHeight);
            if (Orientation == Orientation.Horizontal)
            {
                desiredWidth += button.DesiredWidth;
                desiredHeight = MathF.Max(desiredHeight, button.DesiredHeight);
            }
            else
            {
                desiredWidth = MathF.Max(desiredWidth, button.DesiredWidth);
                desiredHeight += button.DesiredHeight;
            }
        }

        if (_buttons.Count > 1 && BorderThickness.Left > 0)
        {
            float overlap = BorderThickness.Left * (_buttons.Count - 1);
            if (Orientation == Orientation.Horizontal)
            {
                desiredWidth -= overlap;
            }
            else
            {
                desiredHeight -= overlap;
            }
        }

        DesiredWidth = Width > 0 ? Width : MathF.Max(0, desiredWidth);
        DesiredHeight = Height > 0 ? Height : MathF.Max(0, desiredHeight);
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        float arrangedWidth = HasExplicitWidth ? MathF.Min(Width, width) : width;
        float arrangedHeight = HasExplicitHeight ? MathF.Min(Height, height) : height;
        float arrangedX = x;
        float arrangedY = y;

        if (HasExplicitWidth && width > arrangedWidth)
        {
            arrangedX = HorizontalAlignment switch
            {
                HorizontalAlignment.Center => x + (width - arrangedWidth) / 2f,
                HorizontalAlignment.Right => x + width - arrangedWidth,
                _ => x
            };
        }

        if (HasExplicitHeight && height > arrangedHeight)
        {
            arrangedY = VerticalAlignment switch
            {
                VerticalAlignment.Center => y + (height - arrangedHeight) / 2f,
                VerticalAlignment.Bottom => y + height - arrangedHeight,
                _ => y
            };
        }

        base.Arrange(arrangedX, arrangedY, arrangedWidth, arrangedHeight);

        if (_buttons.Count == 0)
        {
            return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            float fixedWidth = _buttons.Sum(button => button.DesiredWidth) - BorderThickness.Left * MathF.Max(0, _buttons.Count - 1);
            float extraWidth = MathF.Max(0, arrangedWidth - fixedWidth) / _buttons.Count;
            float currentX = arrangedX;

            foreach (var button in _buttons)
            {
                float buttonWidth = button.DesiredWidth + extraWidth;
                button.ArrangeUpdate(currentX, arrangedY, buttonWidth, arrangedHeight);
                currentX += MathF.Max(0, buttonWidth - BorderThickness.Left);
            }
        }
        else
        {
            float fixedHeight = _buttons.Sum(button => button.DesiredHeight) - BorderThickness.Left * MathF.Max(0, _buttons.Count - 1);
            float extraHeight = MathF.Max(0, arrangedHeight - fixedHeight) / _buttons.Count;
            float currentY = arrangedY;

            foreach (var button in _buttons)
            {
                float buttonHeight = button.DesiredHeight + extraHeight;
                button.ArrangeUpdate(arrangedX, currentY, arrangedWidth, buttonHeight);
                currentY += MathF.Max(0, buttonHeight - BorderThickness.Top);
            }
        }
    }

    public override void Render(IRenderer renderer)
    {
    }

    private void RebuildButtons()
    {
        ClearChildren();
        _buttons.Clear();

        for (int i = 0; i < Items.Count; i++)
        {
            var button = new ButtonGroupItem(this, i, Items[i]);
            _buttons.Add(button);
            AddChild(button);
        }

        NormalizeSelection();
        RefreshButtonStates();
        InvalidateMeasure();
    }

    private void RefreshButtonStates()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].UpdateFromOwner(isSelected: i == SelectedIndex);
        }
    }

    private int NormalizeIndex(int index)
    {
        if (index < 0)
        {
            return -1;
        }

        return index < Items.Count ? index : -1;
    }

    private void NormalizeSelection()
    {
        if (SelectedIndex >= Items.Count)
        {
            SelectedIndex = -1;
        }
    }

    private void SelectItem(int index)
    {
        if (AllowDeselect && SelectedIndex == index)
        {
            SelectedIndex = -1;
            return;
        }

        SelectedIndex = index;
    }

    private CornerRadius GetItemRadius(int index)
    {
        bool first = index == 0;
        bool last = index == _buttons.Count - 1;
        float radius = GroupBorderRadius;

        if (_buttons.Count == 1)
        {
            return new CornerRadius(radius);
        }

        return Orientation == Orientation.Horizontal
            ? new CornerRadius(first ? radius : 0, last ? radius : 0, last ? radius : 0, first ? radius : 0)
            : new CornerRadius(first ? radius : 0, first ? radius : 0, last ? radius : 0, last ? radius : 0);
    }

    private sealed class ButtonGroupItem : BorderView<ButtonGroupItem>,
        IPointerHandler,
        ITappable,
        IGestureRecognizerHost
    {
        private readonly ButtonGroup _owner;
        private readonly int _index;
        private readonly TapRecognizer _tapRecognizer;
        private bool _isSelected;
        private float _fontSize;

        public string Text { get; }
        public event Action<TapGestureEventArgs>? Tapped;
        public List<IGestureRecognizer> GestureRecognizers { get; } = [];

        public ButtonGroupItem(ButtonGroup owner, int index, string text)
        {
            _owner = owner;
            _index = index;
            Text = text;
            Cursor = CursorShape.Hand;

            _tapRecognizer = new TapRecognizer(
                maxMovementThreshold: 15f,
                maxPressDurationMs: 500,
                doubleTapWindowMs: 300);
            _tapRecognizer.TapDetected += OnTapDetected;
            GestureRecognizers.Add(_tapRecognizer);

            UpdateFromOwner(isSelected: false);
        }

        public void UpdateFromOwner(bool isSelected)
        {
            _isSelected = isSelected;
            _fontSize = _owner.FontSize;
            Padding = _owner.ItemPadding;
            BorderRadius = _owner.GetItemRadius(_index);
            UpdateZIndex();
            MarkNeedsPaint();
            InvalidateMeasure();
        }

        public void OnPointerEntered(PointerEventArgs e)
        {
            if (e.PointerType == PointerType.Mouse)
            {
                IsHovered = true;
                UpdateZIndex();
                MarkNeedsPaint();
            }
        }

        public void OnPointerExited(PointerEventArgs e)
        {
            if (e.PointerType == PointerType.Mouse)
            {
                IsHovered = false;
                IsPressed = false;
                UpdateZIndex();
                MarkNeedsPaint();
            }
        }

        public void OnPointerPressed(PointerEventArgs e)
        {
            IsPressed = true;
            UpdateZIndex();
            MarkNeedsPaint();
        }

        public void OnPointerReleased(PointerEventArgs e)
        {
            IsPressed = false;
            UpdateZIndex();
            MarkNeedsPaint();
        }

        protected override void Measure(float availableWidth, float availableHeight)
        {
            float textWidth = string.IsNullOrEmpty(Text) ? 0 : Text.Length * (_fontSize * 0.6f);
            DesiredWidth = Width > 0 ? Width : MathF.Max(28f, textWidth + Padding.Horizontal);
            DesiredHeight = Height > 0 ? Height : MathF.Max(32f, _fontSize * 1.35f + Padding.Vertical);
        }

        public override void Render(IRenderer renderer)
        {
            Brush background = GetBackground();
            Brush foreground = _isSelected ? _owner.SelectedTextColor : _owner.TextColor;
            Brush border = _isSelected ? _owner.SelectedBorderBrush : _owner.BorderBrush;

            var path = VectorPath.RoundedRectangle(
                ComputedX,
                ComputedY,
                ComputedWidth,
                ComputedHeight,
                BorderRadius.TopLeft,
                BorderRadius.TopRight,
                BorderRadius.BottomRight,
                BorderRadius.BottomLeft);

            renderer.DrawPath(path, background);
            if (_owner.BorderThickness.Left > 0)
            {
                renderer.DrawPathStroke(path, border.PrimaryColor, _owner.BorderThickness.Left);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                float maxTextWidth = MathF.Max(0, ComputedWidth - Padding.Horizontal);
                string displayText = renderer.TruncateTextToFit(Text, maxTextWidth, _fontSize);
                var textSize = renderer.MeasureText(displayText, _fontSize);
                float textX = ComputedX + (ComputedWidth - textSize.X) / 2f;
                float textY = ComputedY + (ComputedHeight - textSize.Y) / 2f;
                renderer.DrawText(displayText, textX, textY, foreground, _fontSize);
            }
        }

        private Brush GetBackground()
        {
            if (_isSelected)
            {
                return _owner.SelectedBackground;
            }

            if (IsPressed)
            {
                return _owner.PressedBackground;
            }

            if (IsHovered)
            {
                return _owner.HoverBackground;
            }

            return _owner.Background;
        }

        private void UpdateZIndex()
        {
            ZIndex = _isSelected ? 2 : IsHovered || IsPressed ? 1 : 0;
        }

        private void OnTapDetected(TapGestureEventArgs e)
        {
            _owner.SelectItem(_index);
            Tapped?.Invoke(e);
        }
    }
}
