namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Card component - Content container with optional header and footer
/// </summary>
public class Card : Rayo.Core.CompositeView<Card>
{
    private Frame? _cardFrame;
    private VStack? _layout;
    private Frame? _headerFrame;
    private Frame? _contentFrame;
    private Frame? _footerFrame;

    private static readonly Shadow DefaultShadow = new(new Color(0, 0, 0, 30), 2, 2);

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            base.Background = value;
            _cardFrame?.Background(value);
            _contentFrame?.Background(value);
        });
    }
    #endregion

    #region HeaderBackground
    [PaintProperty]
    public Brush HeaderBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            _headerFrame?.Background(value);
        });
    } = new Color(245, 245, 245);
    #endregion

    #region FooterBackground
    [PaintProperty]
    public Brush FooterBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            _footerFrame?.Background(value);
        });
    } = new Color(245, 245, 245);
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _cardFrame?.BorderColor(value.PrimaryColor));
    } = new Color(220, 220, 220);
    #endregion

    #region BorderWidth
    [PaintProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _cardFrame?.BorderWidth(value));
    } = 1;
    #endregion

    #region CornerRadius
    [PaintProperty]
    public CornerRadius CornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_cardFrame != null) _cardFrame.BorderRadius = value;
            UpdateSectionCornerRadii();
        });
    } = new CornerRadius(8);
    #endregion

    #region Padding
    [LayoutProperty]
    public new Thickness Padding
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _contentFrame?.Padding(value));
    } = new Thickness(16);
    #endregion

    #region HeaderPadding
    [LayoutProperty]
    public Thickness HeaderPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _headerFrame?.Padding(value));
    } = new Thickness(16, 12, 16, 12);
    #endregion

    #region FooterPadding
    [LayoutProperty]
    public Thickness FooterPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, () => _footerFrame?.Padding(value));
    } = new Thickness(16, 12, 16, 12);
    #endregion

    #region Shadow
    [PaintProperty]
    public Shadow Shadow
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = DefaultShadow;
    #endregion

    #region Header
    [LayoutProperty]
    public VisualElement? Header
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_layout == null) return;
            UpdateSectionFrame(ref _headerFrame, value, HeaderBackground, HeaderPadding);
            NotifySectionStructureChanged();
        });
    }
    #endregion

    #region Content
    [LayoutProperty]
    public VisualElement? Content
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { if (_layout == null) return; UpdateSectionFrame(ref _contentFrame, value, Background, Padding); NotifySectionStructureChanged(); });
    }
    #endregion

    #region Footer
    [LayoutProperty]
    public VisualElement? Footer
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_layout == null) return;
            UpdateSectionFrame(ref _footerFrame, value, FooterBackground, FooterPadding);
            NotifySectionStructureChanged();
        });
    }
    #endregion


    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public Card()
    {
        Background = Color.White;
        BuildComponents();
    }

    private void BuildComponents()
    {
        _layout = new VStack();
        _layout.BorderRadius(CornerRadius);
        _layout.Spacing(0);

        _cardFrame = (Frame)new Frame()
            .Background(Background)
            .BorderColor(BorderColor.PrimaryColor)
            .BorderWidth(BorderWidth)
            .BorderRadius(CornerRadius);
        _cardFrame.Content(_layout);

        // Add _cardFrame as child so UITree renders it and its children
        AddChild(_cardFrame);
        UpdateSectionCornerRadii();
    }

    private void RebuildLayout()
    {
        if (_layout == null) return;

        // Clear and rebuild in correct order: header, content, footer
        _layout.ClearChildren();

        if (_headerFrame != null)
        {
            _layout.AddChild(_headerFrame);
        }

        if (_contentFrame != null)
        {
            _layout.AddChild(_contentFrame);
        }

        if (_footerFrame != null)
        {
            _layout.AddChild(_footerFrame);
        }
    }

    private void UpdateSectionFrame(ref Frame? targetFrame, VisualElement? content, Rendering.Brushes.Brush background, Thickness padding)
    {
        if (_layout == null)
        {
            return;
        }

        if (targetFrame != null)
        {
            _layout.RemoveChild(targetFrame);
            targetFrame = null;
        }

        if (content != null)
        {
            targetFrame = new Frame()
                .Background(background)
                .Padding(padding)
                .Content(content);
        }
    }

    private void NotifySectionStructureChanged()
    {
        if (_layout == null)
        {
            return;
        }

        RebuildLayout();
        UpdateSectionCornerRadii();
        MarkNeedsLayout();
    }

    private void UpdateSectionCornerRadii()
    {
        if (_layout == null)
        {
            return;
        }

        Frame? first = null;
        Frame? last = null;

        foreach (var Frame in new Frame?[] { _headerFrame, _contentFrame, _footerFrame })
        {
            if (Frame == null)
            {
                continue;
            }

            first ??= Frame;
            last = Frame;
        }

        if (first == null)
        {
            _layout.BorderRadius(CornerRadius);
            return;
        }

        _layout.BorderRadius(CornerRadius.None);

        foreach (var Frame in new Frame?[] { _headerFrame, _contentFrame, _footerFrame })
        {
            if (Frame == null)
            {
                continue;
            }

            float topLeft = 0;
            float topRight = 0;
            float bottomRight = 0;
            float bottomLeft = 0;

            if (Frame == first)
            {
                topLeft = CornerRadius.TopLeft;
                topRight = CornerRadius.TopRight;
            }

            if (Frame == last)
            {
                bottomRight = CornerRadius.BottomRight;
                bottomLeft = CornerRadius.BottomLeft;
            }

            Frame.BorderRadius(new CornerRadius(topLeft, topRight, bottomRight, bottomLeft));
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (_cardFrame != null)
        {
            _cardFrame.Measure(availableWidth, availableHeight);
            DesiredWidth = _cardFrame.DesiredWidth;
            DesiredHeight = _cardFrame.DesiredHeight;
        }
        else
        {
            DesiredWidth = Width > 0 ? Width : 200;
            DesiredHeight = Height > 0 ? Height : 100;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_cardFrame != null)
        {
            _cardFrame.Arrange(x, y, width, height);
        }
    }

    public override void Render(IRenderer renderer)
    {
        Shadow.Render(renderer, ComputedX, ComputedY, ComputedWidth, ComputedHeight, CornerRadius);

        // _cardFrame and its children are rendered by UITree automatically
    }
}
