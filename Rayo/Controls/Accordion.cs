namespace Rayo.Controls;

using Rayo.Animation;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Collections.Generic;

/// <summary>
/// Expandable content section with header
/// Uses Icon control for chevron indicators
/// </summary>
public class Expander : CompositeView<Expander>
{
    private HStack? _headerContainer;
    private Icon? _chevronIcon;
    private Label? _headerLabel;
    private Frame? _headerFrame;  // Store reference to header Frame
    private VisualElement? _headerButton;  // Store reference to the ExpanderHeaderButton wrapper
    private Frame? _contentFrame;
    private string _headerTitle = string.Empty;

    private float _headerMeasuredHeight;
    private float _contentMeasuredHeight;
    private float _contentAnimationProgress;
    private bool _templateReady;
    private bool _suppressAnimation;

    #region HeaderBackground
    [PaintProperty]
    public Rendering.Brushes.Brush HeaderBackground
    {
        get => field;
        set
        {
            field = value;
            UpdateHeaderVisuals();
        }
    } = new Color(245, 245, 245);
    #endregion

    #region HeaderHoverColor
    [PaintProperty]
    public Brush HeaderHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateHeaderVisuals);
    } = new Color(230, 230, 230);
    #endregion

    #region ContentBackground
    [PaintProperty]
    public Rendering.Brushes.Brush ContentBackground
    {
        get => field;
        set
        {
            field = value;
            _contentFrame?.Background(value);
            MarkNeedsPaint();
        }
    } = Color.White;
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame?.BorderColor(value.PrimaryColor));
    } = new Color(220, 220, 220);
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { UpdateHeaderVisuals(); });
    } = Color.Black;
    #endregion

    #region BorderWidth
    [PaintProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame?.BorderWidth(value));
    } = 1;
    #endregion

    #region HeaderPadding
    [LayoutProperty]
    public Thickness HeaderPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _headerFrame?.Padding(value));
    } = new Thickness(16, 12, 16, 12);
    #endregion

    #region ContentPadding
    [LayoutProperty]
    public Thickness ContentPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame?.Padding(value));
    } = new Thickness(16);
    #endregion

    #region AnimationDuration
    [PaintProperty]
    public float AnimationDuration
    {
        get => field;
        set => this.SetProperty(ref field, MathF.Max(0f, value), (Action?)null);
    } = 220f;
    #endregion

    #region EnableAnimations
    [PaintProperty]
    public bool EnableAnimations
    {
        get => field;
        set => this.SetProperty(ref field, value, (Action?)null);
    } = true;
    #endregion

    #region AnimationEasing
    [PaintProperty]
    public Func<float, float> AnimationEasing
    {
        get => field;
        set => this.SetProperty(ref field, value ?? Easing.OutCubic, (Action?)null);
    } = Easing.OutCubic;
    #endregion

    #region IsExpanded
    [LayoutProperty]
    public bool IsExpanded
    {
        get => field;
        set
        {
            this.SetProperty(ref field, value, () => { UpdateExpandedState(); ExpandedChanged?.Invoke(field); });
        }
    }
    #endregion

    #region Header
    [LayoutProperty]
    public string Header
    {
        get => _headerTitle;
        set => this.SetProperty(ref _headerTitle, value, () => { UpdateHeaderVisuals(); });
    }
    #endregion

    private VisualElement? _content;

    #region Content
    [LayoutProperty]
    public VisualElement? Content
    {
        get => _content;
        set => this.SetProperty(ref _content, value, () =>
        {
            if (_contentFrame != null)
            {
                _contentFrame.Content(value);
            }
        });
    }
    #endregion

    // Events
    #region ExpandedChanged
    public event Action<bool>? ExpandedChanged;

    //public Expander OnExpandedChanged(Action<bool> handler)
    //{
    //    ExpandedChanged += handler;
    //    return this;
    //}
    #endregion


    // Constructors and methods
    public Expander()
    {
        BuildComponents();
    }

    public Expander(string header, VisualElement? content = null)
    {
        BuildComponents();
        Header = header;
        if (content != null)
        {
            Content = content;
        }
    }

    private void BuildComponents()
    {
        // Create chevron icon
        _chevronIcon = new Icon(Icons.ChevronRight)
        {
            Width = 16,
            Height = 16,
            Color = TextColor
        };
        _chevronIcon.SetInputTransparent(true);

        // Create header label
        _headerLabel = (Label)new Label()
            .Text(_headerTitle)
            .Foreground(TextColor)
            .FontSize(14)
            .SetInputTransparent(true);

        // Create header container with chevron and label
        _headerContainer = (HStack)new HStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .SetInputTransparent(true);
        _headerContainer.AddChild(_chevronIcon);
        _headerContainer.AddChild(_headerLabel);

        // Create clickable header Frame (store as field)
        _headerFrame = new Frame()
            .Background(HeaderBackground)
            .Padding(HeaderPadding)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .SetInputTransparent(true);  // Let events pass through to parent ExpanderHeaderButton
        _headerFrame.Content(_headerContainer);
        _headerFrame.BorderRadius = new CornerRadius(8, 8, 0, 0);

        // Make the header Frame clickable using IPointerHandler (store as field)
        _headerButton = new ExpanderHeaderButton(this, _headerFrame);
        AddChild(_headerButton);

        _contentFrame = new Frame();
        _contentFrame.Background = ContentBackground;
        _contentFrame.BorderColor = BorderColor.PrimaryColor;
        _contentFrame.BorderWidth = BorderWidth;
        _contentFrame.Padding = ContentPadding;
        _contentFrame.BorderRadius = new CornerRadius(0, 0, 8, 8);
        _contentFrame.IsVisible(false);
        AddChild(_contentFrame);

        _contentAnimationProgress = IsExpanded ? 1f : 0f;
        _templateReady = true;
        _suppressAnimation = true;
        UpdateExpandedState();
        _suppressAnimation = false;
    }

    private void UpdateExpandedState()
    {
        if (_contentFrame == null || _headerContainer == null)
        {
            return;
        }

        UpdateHeaderVisuals();

        if (!_templateReady || _suppressAnimation)
        {
            _contentAnimationProgress = IsExpanded ? 1f : 0f;
            _contentFrame.IsVisible(_contentAnimationProgress > 0f);
            MarkNeedsLayout();
            MarkNeedsPaint();
            return;
        }

        AnimationManager.Instance.StopAnimations(this);
        float target = IsExpanded ? 1f : 0f;

        if (!EnableAnimations || Math.Abs(_contentAnimationProgress - target) < 0.001f)
        {
            _contentAnimationProgress = target;
            _contentFrame.IsVisible(target > 0f);
            MarkNeedsLayout();
            MarkNeedsPaint();
            return;
        }

        var animation = new FloatAnimation(_contentAnimationProgress, target, (_, value) =>
        {
            _contentAnimationProgress = value;
            _contentFrame.IsVisible(value > 0.001f);
            MarkNeedsLayout();
            MarkNeedsPaint();
        })
        {
            Target = this,
            Duration = AnimationDuration,
            EasingFunction = AnimationEasing ?? Easing.OutCubic
        };

        animation.OnComplete += () =>
        {
            _contentAnimationProgress = target;
            _contentFrame.IsVisible(target > 0.001f);
            MarkNeedsLayout();
            MarkNeedsPaint();
        };

        AnimationManager.Instance.Animate(animation);
    }


    private void UpdateHeaderVisuals()
    {
        if (_chevronIcon == null || _headerLabel == null || _headerFrame == null)
        {
            return;
        }

        // Update chevron icon based on expanded state
        _chevronIcon.IconData = IsExpanded ? Icons.ChevronDown : Icons.ChevronRight;
        _chevronIcon.Color = TextColor;

        // Update header label
        _headerLabel.Text(_headerTitle);
        _headerLabel.Foreground(TextColor);

        // Update header Frame background
        _headerFrame.Background(IsExpanded ? HeaderHoverColor : HeaderBackground);
        _headerFrame.BorderRadius = IsExpanded
            ? new CornerRadius(8, 8, 0, 0)
            : new CornerRadius(8, 8, 8, 8);

        MarkNeedsPaint();
    }

    /// <summary>
    /// Internal button-like Frame for the expander header with modern pointer events
    /// </summary>
    private class ExpanderHeaderButton : Frame, 
        Rayo.Core.Input.IPointerHandler,
        Rayo.Core.Input.Gestures.ITappable
    {
        private readonly Expander _owner;
        private readonly Frame _headerFrame;
        private readonly Rayo.Core.Input.Gestures.TapRecognizer _tapRecognizer;
        private bool _isHovered;

        public List<Rayo.Core.Input.Gestures.IGestureRecognizer> GestureRecognizers { get; } = new();
        public event Action<Rayo.Core.Input.Gestures.TapGestureEventArgs>? Tapped;

        public ExpanderHeaderButton(Expander owner, Frame headerFrame)
        {
            _owner = owner;
            _headerFrame = headerFrame;
            
            // CRITICAL: Ensure this Frame can receive input events
            HorizontalAlignment = HorizontalAlignment.Stretch;
            IsInputTransparent = false;  // Make sure we receive events!
            
            // Set a transparent background to ensure hit testing works
            // Many UI systems only hit-test elements with backgrounds
            Background = Color.Transparent;

            Content = _headerFrame;

            _tapRecognizer = new Rayo.Core.Input.Gestures.TapRecognizer(
                maxMovementThreshold: 15f,
                maxPressDurationMs: 500,
                doubleTapWindowMs: 300
            );
            _tapRecognizer.TapDetected += OnTapDetected;
            GestureRecognizers.Add(_tapRecognizer);
        }

        private void OnTapDetected(Rayo.Core.Input.Gestures.TapGestureEventArgs e)
        {
            _owner.IsExpanded = !_owner.IsExpanded;
            Tapped?.Invoke(e);
        }

        public void OnPointerEntered(Rayo.Core.Input.PointerEventArgs e)
        {
            if (e.PointerType == Rayo.Core.Input.PointerType.Mouse)
            {
                _isHovered = true;
                _headerFrame.Background(_owner.HeaderHoverColor);
            }
        }

        public void OnPointerExited(Rayo.Core.Input.PointerEventArgs e)
        {
            if (e.PointerType == Rayo.Core.Input.PointerType.Mouse)
            {
                _isHovered = false;
                _headerFrame.Background(_owner.IsExpanded ? _owner.HeaderHoverColor : _owner.HeaderBackground);
            }
            
            // Cancel gesture on exit
            _tapRecognizer.Reset();
        }

        public void OnPointerPressed(Rayo.Core.Input.PointerEventArgs e)
        {
            _headerFrame.Background(_owner.HeaderHoverColor);
            
            // CRITICAL: Feed event to gesture recognizer
            _tapRecognizer.ProcessPointerEvent(e);
        }

        public void OnPointerReleased(Rayo.Core.Input.PointerEventArgs e)
        {
            _headerFrame.Background(_isHovered || _owner.IsExpanded ? _owner.HeaderHoverColor : _owner.HeaderBackground);

            // CRITICAL: Feed event to gesture recognizer
            _tapRecognizer.ProcessPointerEvent(e);
        }

        public void OnPointerMoved(Rayo.Core.Input.PointerEventArgs e)
        {
            // Feed event to gesture recognizer for movement tracking
            _tapRecognizer.ProcessPointerEvent(e);
        }

        public void OnPointerCanceled(Rayo.Core.Input.PointerEventArgs e)
        {
            _headerFrame.Background(_owner.IsExpanded ? _owner.HeaderHoverColor : _owner.HeaderBackground);
            _tapRecognizer.Reset();
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 0f;

        if (_headerButton != null)
        {
            _headerButton.Measure(availableWidth, availableHeight);
            float headerWidth = _headerButton.DesiredWidth > 0 ? _headerButton.DesiredWidth : _headerButton.Width;
            _headerMeasuredHeight = _headerButton.DesiredHeight > 0 ? _headerButton.DesiredHeight : _headerButton.Height;
            measuredWidth = MathF.Max(measuredWidth, headerWidth);
        }
        else
        {
            _headerMeasuredHeight = 0f;
        }

        if (_contentFrame != null)
        {
            float contentAvailableHeight = float.IsPositiveInfinity(availableHeight)
                ? availableHeight
                : MathF.Max(0, availableHeight - _headerMeasuredHeight);

            _contentFrame.Measure(availableWidth, contentAvailableHeight);
            _contentMeasuredHeight = _contentFrame.DesiredHeight > 0 ? _contentFrame.DesiredHeight : _contentFrame.Height;
            float contentWidth = _contentFrame.DesiredWidth > 0 ? _contentFrame.DesiredWidth : _contentFrame.Width;
            measuredWidth = MathF.Max(measuredWidth, contentWidth);
        }
        else
        {
            _contentMeasuredHeight = 0f;
        }

        DesiredWidth = Width > 0 ? Width : measuredWidth;

        if (Height > 0)
        {
            DesiredHeight = Height;
        }
        else
        {
            float visibleContentHeight = _contentMeasuredHeight * _contentAnimationProgress;
            DesiredHeight = _headerMeasuredHeight + visibleContentHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float headerHeight = MathF.Min(_headerMeasuredHeight, height);
        _headerButton?.Arrange(x, y, width, headerHeight);

        float remainingHeight = MathF.Max(0, height - headerHeight);
        float naturalContentHeight = _contentMeasuredHeight * _contentAnimationProgress;
        float contentHeight = MathF.Min(naturalContentHeight, remainingHeight);

        if (_contentFrame != null)
        {
            _contentFrame.IsVisible(contentHeight > 0.001f);
            _contentFrame.Arrange(x, y + headerHeight, width, contentHeight);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Children are rendered by the render tree traversal
    }
}

/// <summary>
/// Accordion - Container for multiple Expander items with single-expand mode
/// </summary>
public class Accordion : CompositeView<Accordion>
{
    private VStack? _layout;
    private List<Expander> _expanders = new();

    #region Spacing
    [LayoutProperty]
    public float Spacing
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _layout?.Spacing(value));
    }
    #endregion

    #region SingleExpand
    [LayoutProperty]
    public bool SingleExpand 
    { 
        get; 
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    public Accordion()
    {
        Spacing = 4;
        _layout = new VStack()
            .Spacing(Spacing);

        AddChild(_layout);
    }


    public Accordion AddItem(string header, VisualElement content, bool startExpanded = false)
    {
        var expander = new Expander(header, content);
        expander.IsExpanded = startExpanded;

        // Handle single-expand mode
        if (SingleExpand)
        {
            expander.OnExpandedChanged(isExpanded =>
            {
                if (isExpanded)
                {
                    // Collapse all other expanders
                    foreach (var other in _expanders)
                    {
                        if (other != expander && other.IsExpanded)
                        {
                            other.IsExpanded = false;
                        }
                    }
                }
            });
        }

        _expanders.Add(expander);
        _layout?.AddChild(expander);
        MarkNeedsLayout();
        return this;
    }

    public Accordion AddExpander(Expander expander)
    {
        // Handle single-expand mode
        if (SingleExpand)
        {
            expander.OnExpandedChanged(isExpanded =>
            {
                if (isExpanded)
                {
                    // Collapse all other expanders
                    foreach (var other in _expanders)
                    {
                        if (other != expander && other.IsExpanded)
                        {
                            other.IsExpanded = false;
                        }
                    }
                }
            });
        }

        _expanders.Add(expander);
        _layout?.AddChild(expander);
        MarkNeedsLayout();
        return this;
    }

    public Accordion ExpandItem(int index)
    {
        if (index >= 0 && index < _expanders.Count)
        {
            _expanders[index].IsExpanded = true;
        }
        return this;
    }

    public Accordion CollapseAll()
    {
        foreach (var expander in _expanders)
        {
            expander.IsExpanded = false;
        }
        return this;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (_layout != null)
        {
            _layout.Measure(availableWidth, availableHeight);
            DesiredWidth = _layout.DesiredWidth;
            DesiredHeight = _layout.DesiredHeight;
        }
        else
        {
            DesiredWidth = Width > 0 ? Width : 300;
            DesiredHeight = Height > 0 ? Height : 200;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_layout != null)
        {
            _layout.Arrange(x, y, width, height);
        }
    }

    public override void Render(IRenderer renderer)
    {
        _layout?.Render(renderer);
    }
}
