namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Displays one slide at a time with previous/next navigation and optional indicators.
/// </summary>
public enum CarouselNavigationPlacement
{
    Bottom,
    Overlay
}

public class Carousel : CompositeView<Carousel>
{
    private readonly CarouselViewport _viewport;
    private readonly Frame _contentFrame;
    private readonly IconButton _previousButton;
    private readonly IconButton _nextButton;
    private readonly HStack _indicatorStack;
    private readonly HStack _navigationRow;
    private readonly Grid _root;
    private List<VisualElement> _items = [];
    private int _selectedIndex = -1;

    #region Items
    [NotFluent]
    public IList<VisualElement> Items
    {
        get => _items;
        set => SetItems(value);
    }
    #endregion

    #region SelectedIndex
    [LayoutProperty]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectIndex(value, raiseEvent: true);
    }
    #endregion

    #region Loop
    public bool Loop
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshNavigationState);
    } = true;
    #endregion

    #region NavigationPlacement
    [LayoutProperty]
    public CarouselNavigationPlacement NavigationPlacement
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = CarouselNavigationPlacement.Bottom;
    #endregion

    #region ShowNavigationButtons
    [LayoutProperty]
    public bool ShowNavigationButtons
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = true;
    #endregion

    #region ShowIndicators
    [LayoutProperty]
    public bool ShowIndicators
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = true;
    #endregion

    #region SlideBackground
    [PaintProperty]
    public Brush SlideBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame.Background = value);
    } = new Color(35, 38, 46);
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame.BorderColor = value);
    } = new Color(70, 75, 90);
    #endregion

    #region BorderWidth
    [LayoutProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame.BorderWidth = value);
    } = 1f;
    #endregion

    #region NavigationButtonBackground
    [PaintProperty]
    public Brush NavigationButtonBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshNavigationButtonStyles);
    } = new Color(55, 60, 72);
    #endregion

    #region NavigationButtonHoverBackground
    [PaintProperty]
    public Brush NavigationButtonHoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshNavigationButtonStyles);
    } = new Color(75, 82, 98);
    #endregion

    #region NavigationButtonDisabledBackground
    [PaintProperty]
    public Brush NavigationButtonDisabledBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshNavigationState);
    } = new Color(45, 48, 56);
    #endregion

    #region NavigationIconColor
    [PaintProperty]
    public Brush NavigationIconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshNavigationButtonStyles);
    } = Color.White;
    #endregion

    #region NavigationIconDisabledColor
    [PaintProperty]
    public Brush NavigationIconDisabledColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshNavigationState);
    } = new Color(120, 124, 135);
    #endregion

    #region IndicatorColor
    [PaintProperty]
    public Brush IndicatorColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildIndicators);
    } = new Color(105, 112, 128);
    #endregion

    #region IndicatorSelectedColor
    [PaintProperty]
    public Brush IndicatorSelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildIndicators);
    } = new Color(59, 130, 246);
    #endregion

    #region IndicatorSize
    [LayoutProperty]
    public float IndicatorSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildIndicators);
    } = 10f;
    #endregion

    #region NavigationSpacing
    [LayoutProperty]
    public float NavigationSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = 12f;
    #endregion

    #region OverlayNavigationButtonSize
    [LayoutProperty]
    public float OverlayNavigationButtonSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = 56f;
    #endregion

    #region OverlayNavigationInset
    [LayoutProperty]
    public float OverlayNavigationInset
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = 14f;
    #endregion

    #region OverlayNavigationIconSize
    [LayoutProperty]
    public float OverlayNavigationIconSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildNavigation);
    } = 30f;
    #endregion

    public int ItemCount => _items.Count;

    public VisualElement? SelectedItem => IsValidIndex(_selectedIndex) ? _items[_selectedIndex] : null;

    public event Action<int>? SelectedIndexChanged;

    public Carousel()
    {
        Width = 360;
        Height = 260;
        Padding = new Thickness(0);

        _viewport = new CarouselViewport
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _contentFrame = new Frame
        {
            Background = SlideBackground,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            BorderRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _viewport.Content = _contentFrame;

        _previousButton = CreateNavigationButton(Icons.ChevronLeft, Previous);
        _nextButton = CreateNavigationButton(Icons.ChevronRight, Next);

        _indicatorStack = new HStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);

        _navigationRow = new HStack()
            .Alignment(Alignment.Center)
            .JustifyContent(JustifyContent.Center)
            .Spacing(NavigationSpacing)
            .Height(44)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(_previousButton, _indicatorStack, _nextButton);

        _root = new Grid()
            .Rows(GridLength.Star, GridLength.Auto)
            .Columns(GridLength.Star)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddChild(_viewport, 0, 0)
            .AddChild(_navigationRow, 1, 0);

        AddChild(_root);
        RefreshNavigation();
    }

    public Carousel AddSlide(VisualElement slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        _items.Add(slide);
        if (_selectedIndex < 0)
        {
            _selectedIndex = 0;
        }

        RefreshAfterItemsChanged(selectionChanged: _items.Count == 1);
        return this;
    }

    public Carousel AddSlides(params VisualElement[] slides)
    {
        foreach (var slide in slides)
        {
            AddSlide(slide);
        }

        return this;
    }

    public Carousel OnSelectedIndexChanged(Action<int> handler)
    {
        SelectedIndexChanged += handler;
        return this;
    }

    public void Next()
    {
        if (_items.Count == 0)
        {
            return;
        }

        if (_selectedIndex < _items.Count - 1)
        {
            SelectedIndex = _selectedIndex + 1;
        }
        else if (Loop)
        {
            SelectedIndex = 0;
        }
    }

    public void Previous()
    {
        if (_items.Count == 0)
        {
            return;
        }

        if (_selectedIndex > 0)
        {
            SelectedIndex = _selectedIndex - 1;
        }
        else if (Loop)
        {
            SelectedIndex = _items.Count - 1;
        }
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : availableWidth;
        float measuredHeight = Height > 0 ? Height : availableHeight;

        if (float.IsInfinity(measuredWidth) || float.IsNaN(measuredWidth) || measuredWidth <= 0)
        {
            measuredWidth = 360;
        }

        if (float.IsInfinity(measuredHeight) || float.IsNaN(measuredHeight) || measuredHeight <= 0)
        {
            measuredHeight = 260;
        }

        _root.MeasureUpdate(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _root.ArrangeUpdate(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
    }

    private void SetItems(IList<VisualElement>? items)
    {
        _contentFrame.ClearContent();
        _items = items?.Where(item => item != null).ToList() ?? [];
        NormalizeSelectedIndex();
        RefreshAfterItemsChanged(selectionChanged: true);
    }

    private void SelectIndex(int value, bool raiseEvent)
    {
        bool changed = this.SetPropertyCondition(
            ref _selectedIndex,
            value,
            (current, incoming) => incoming != current && IsValidIndex(incoming),
            () =>
            {
                UpdateContent();
                RebuildIndicators();
                RefreshNavigationState();
                RefreshLocalLayout();
            },
            nameof(SelectedIndex));

        if (!changed)
        {
            return;
        }

        if (raiseEvent)
        {
            SelectedIndexChanged?.Invoke(_selectedIndex);
        }
    }

    private void RefreshAfterItemsChanged(bool selectionChanged)
    {
        NormalizeSelectedIndex();
        UpdateContent();
        RefreshNavigation();
        RefreshLocalLayout();

        if (selectionChanged && _selectedIndex >= 0)
        {
            SelectedIndexChanged?.Invoke(_selectedIndex);
        }
    }

    private void NormalizeSelectedIndex()
    {
        if (_items.Count == 0)
        {
            _selectedIndex = -1;
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, _items.Count - 1);
    }

    private void UpdateContent()
    {
        var selected = SelectedItem;
        if (selected == null)
        {
            _contentFrame.ClearContent();
            _contentFrame.MarkNeedsPaint();
            return;
        }

        if (!ReferenceEquals(_contentFrame.Content, selected))
        {
            _contentFrame.ClearContent();
        }

        _contentFrame.Content = selected;
        _contentFrame.InvalidateMeasure();
        _contentFrame.MarkNeedsPaint();
        MarkNeedsPaint();
    }

    private void RefreshLocalLayout()
    {
        if (ComputedWidth > 0 && ComputedHeight > 0)
        {
            ForceMeasure(LastMeasuredAvailableWidth, LastMeasuredAvailableHeight);
            ForceArrange(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }
        else
        {
            InvalidateMeasure();
        }

        MarkNeedsPaint();
        var tree = UIApplication.Current?.Tree ?? UITree.Current;
        tree?.MarkNeedsRender();
    }

    private void RefreshNavigation()
    {
        RebuildNavigation();
        RebuildIndicators();
        RefreshNavigationState();
    }

    private void RebuildNavigation()
    {
        _navigationRow.ClearChildren();
        _navigationRow.Spacing = NavigationSpacing;
        _viewport.ClearOverlayButtons();

        bool useOverlayNavigation = NavigationPlacement == CarouselNavigationPlacement.Overlay;
        _navigationRow.IsVisible = ShowIndicators || (ShowNavigationButtons && !useOverlayNavigation);

        if (ShowNavigationButtons && useOverlayNavigation)
        {
            ConfigureOverlayButton(_previousButton, isPrevious: true);
            ConfigureOverlayButton(_nextButton, isPrevious: false);
            _viewport.SetOverlayButtons(_previousButton, _nextButton, OverlayNavigationInset);
        }

        if (ShowNavigationButtons && !useOverlayNavigation)
        {
            ConfigureBottomButton(_previousButton);
            _navigationRow.AddChild(_previousButton);
        }

        if (ShowIndicators)
        {
            _navigationRow.AddChild(_indicatorStack);
        }

        if (ShowNavigationButtons && !useOverlayNavigation)
        {
            ConfigureBottomButton(_nextButton);
            _navigationRow.AddChild(_nextButton);
        }

        RebuildIndicators();
        RefreshNavigationState();
        InvalidateMeasure();
    }

    private void RebuildIndicators()
    {
        _indicatorStack.ClearChildren();

        if (!ShowIndicators)
        {
            return;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            int index = i;
            bool isSelected = index == _selectedIndex;
            float size = isSelected ? IndicatorSize + 4f : IndicatorSize;

            var indicator = new Button
            {
                Text = string.Empty,
                Width = size,
                Height = IndicatorSize,
                Background = isSelected ? IndicatorSelectedColor : IndicatorColor,
                HoverBackground = isSelected ? IndicatorSelectedColor : NavigationButtonHoverBackground,
                PressedBackground = IndicatorSelectedColor,
                BorderWidth = 0,
                BorderRadius = new CornerRadius(IndicatorSize / 2f),
                Padding = new Thickness(0)
            };

            indicator.Tapped += _ => SelectedIndex = index;
            _indicatorStack.AddChild(indicator);
        }
    }

    private void RefreshNavigationButtonStyles()
    {
        RefreshButtonStyle(_previousButton, CanMovePrevious());
        RefreshButtonStyle(_nextButton, CanMoveNext());
    }

    private void RefreshNavigationState()
    {
        bool canMovePrevious = CanMovePrevious();
        bool canMoveNext = CanMoveNext();

        _previousButton.IsEnabled = canMovePrevious;
        _nextButton.IsEnabled = canMoveNext;

        RefreshButtonStyle(_previousButton, canMovePrevious);
        RefreshButtonStyle(_nextButton, canMoveNext);
    }

    private void RefreshButtonStyle(IconButton button, bool isEnabled)
    {
        button.Background = isEnabled ? NavigationButtonBackground : NavigationButtonDisabledBackground;
        button.HoverBackground = isEnabled ? NavigationButtonHoverBackground : NavigationButtonDisabledBackground;
        button.PressedBackground = isEnabled ? NavigationButtonHoverBackground : NavigationButtonDisabledBackground;
        button.IconColor = isEnabled ? NavigationIconColor : NavigationIconDisabledColor;
    }

    private void ConfigureBottomButton(IconButton button)
    {
        button.Width = 36;
        button.Height = 36;
        button.IconSize = 18;
        button.BorderRadius = new CornerRadius(18);
        button.Padding = new Thickness(0);
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.VerticalAlignment = VerticalAlignment.Top;
        RefreshButtonStyle(button, button.IsEnabled);
    }

    private void ConfigureOverlayButton(IconButton button, bool isPrevious)
    {
        button.Width = OverlayNavigationButtonSize;
        button.Height = OverlayNavigationButtonSize;
        button.IconSize = OverlayNavigationIconSize;
        button.BorderRadius = new CornerRadius(OverlayNavigationButtonSize / 2f);
        button.Padding = new Thickness(0);
        button.HorizontalAlignment = isPrevious ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        button.VerticalAlignment = VerticalAlignment.Center;
        RefreshButtonStyle(button, button.IsEnabled);
    }

    private bool CanMovePrevious()
    {
        return _items.Count > 1 && (Loop || _selectedIndex > 0);
    }

    private bool CanMoveNext()
    {
        return _items.Count > 1 && (Loop || _selectedIndex < _items.Count - 1);
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _items.Count;
    }

    private static IconButton CreateNavigationButton(IconData icon, Action action)
    {
        var button = new IconButton(icon)
        {
            Width = 36,
            Height = 36,
            IconSize = 18,
            BorderWidth = 0,
            BorderRadius = new CornerRadius(18),
            Padding = new Thickness(0)
        };

        button.Tapped += _ => action();
        return button;
    }

    private sealed class CarouselViewport : CompositeView<CarouselViewport>
    {
        private VisualElement? _content;
        private IconButton? _previousButton;
        private IconButton? _nextButton;
        private float _buttonInset;

        public VisualElement? Content
        {
            get => _content;
            set
            {
                if (_content == value)
                {
                    return;
                }

                if (_content != null)
                {
                    RemoveChild(_content);
                }

                _content = value;

                if (_content != null)
                {
                    AddChild(_content);
                }
            }
        }

        public void SetOverlayButtons(IconButton previousButton, IconButton nextButton, float inset)
        {
            _buttonInset = inset;
            _previousButton = previousButton;
            _nextButton = nextButton;

            AddOverlayButton(previousButton);
            AddOverlayButton(nextButton);
            InvalidateMeasure();
        }

        public void ClearOverlayButtons()
        {
            if (_previousButton != null)
            {
                RemoveChild(_previousButton);
            }

            if (_nextButton != null)
            {
                RemoveChild(_nextButton);
            }

            _previousButton = null;
            _nextButton = null;
            InvalidateMeasure();
        }

        protected override void Measure(float availableWidth, float availableHeight)
        {
            _content?.MeasureUpdate(availableWidth, availableHeight);
            _previousButton?.MeasureUpdate(availableWidth, availableHeight);
            _nextButton?.MeasureUpdate(availableWidth, availableHeight);

            DesiredWidth = ResolveLength(Width, HorizontalAlignment, availableWidth, _content?.DesiredWidth ?? 0);
            DesiredHeight = ResolveLength(Height, VerticalAlignment, availableHeight, _content?.DesiredHeight ?? 0);
        }

        protected override void Arrange(float x, float y, float width, float height)
        {
            base.Arrange(x, y, width, height);

            _content?.ArrangeUpdate(x, y, width, height);

            ArrangeOverlayButton(_previousButton, x + _buttonInset, y, height);
            if (_nextButton != null)
            {
                ArrangeOverlayButton(_nextButton, x + width - _buttonInset - _nextButton.DesiredWidth, y, height);
            }
        }

        public override void Render(IRenderer renderer)
        {
        }

        private void AddOverlayButton(IconButton button)
        {
            if (!Children.Contains(button))
            {
                AddChild(button);
            }
        }

        private static void ArrangeOverlayButton(IconButton? button, float x, float y, float viewportHeight)
        {
            if (button == null)
            {
                return;
            }

            float width = button.DesiredWidth > 0 ? button.DesiredWidth : button.Width;
            float height = button.DesiredHeight > 0 ? button.DesiredHeight : button.Height;
            float buttonY = y + (viewportHeight - height) / 2f;
            button.ArrangeUpdate(x, buttonY, width, height);
        }

        private static float ResolveLength(float explicitLength, Enum alignment, float availableLength, float desiredLength)
        {
            if (explicitLength > 0)
            {
                return explicitLength;
            }

            bool isStretch = alignment.Equals(HorizontalAlignment.Stretch) || alignment.Equals(VerticalAlignment.Stretch);
            if (isStretch && !float.IsInfinity(availableLength))
            {
                return availableLength;
            }

            if (desiredLength > 0 && !float.IsNaN(desiredLength) && !float.IsInfinity(desiredLength))
            {
                return desiredLength;
            }

            return !float.IsInfinity(availableLength) && !float.IsNaN(availableLength) ? availableLength : 0;
        }
    }
}
