namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Badge variants for different visual styles
/// </summary>
public enum BadgeVariant
{
    /// <summary>Solid background with white text</summary>
    Solid,
    /// <summary>Outlined border with colored text</summary>
    Outline,
    /// <summary>Subtle background with colored text</summary>
    Subtle
}

/// <summary>
/// Badge sizes
/// </summary>
public enum BadgeSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Badge shape options
/// </summary>
public enum BadgeShape
{
    /// <summary>Rounded corners (pill shape)</summary>
    Rounded,
    /// <summary>Small rounded corners</summary>
    Square,
    /// <summary>Circular (best for single digits or dots)</summary>
    Circle
}

/// <summary>
/// Badge - A small visual indicator for notifications, counts, or status labels.
/// Can be used standalone or positioned over other elements.
/// </summary>
public class Badge : CompositeView<Badge>
{
    // Internal state for Text/Count mutual exclusivity
    private string _textValue = "";
    private int? _countValue = null;
    // Backing fields for reactive properties (previously generated)
    private Brush _textColor = Color.White;
    private Brush _borderColor = Color.Transparent;
    private float _borderWidth;
    private float _fontSize;
    private BadgeVariant _variant;
    private BadgeSize _badgeSize;
    private BadgeShape _shape;
    private int _maxCount;
    private bool _showZero;
    private bool _dot;

    // Styling
    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => _textColor;
        set => this.SetProperty(ref _textColor, value);
    }
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => _borderColor;
        set => this.SetProperty(ref _borderColor, value);
    }
    #endregion

    #region BorderWidth
    [PaintProperty]
    public float BorderWidth
    {
        get => _borderWidth;
        set => this.SetProperty(ref _borderWidth, value);
    }
    #endregion

    #region FontSize
    [LayoutProperty]
    public float FontSize
    {
        get => _fontSize;
        set => this.SetProperty(ref _fontSize, value);
    }
    #endregion

    #region Variant
    [PaintProperty]
    public BadgeVariant Variant
    {
        get => _variant;
        set => this.SetProperty(ref _variant, value);
    }
    #endregion

    #region BadgeSize
    [LayoutProperty]
    public BadgeSize BadgeSize
    {
        get => _badgeSize;
        set => this.SetProperty(ref _badgeSize, value);
    }
    #endregion

    #region Shape
    [PaintProperty]
    public BadgeShape Shape
    {
        get => _shape;
        set => this.SetProperty(ref _shape, value);
    }
    #endregion

    #region Text
    [LayoutProperty]
    public string Text
    {
        get => _textValue;
        set => this.SetProperty(ref _textValue, value, () => 
        { 
            _countValue = null;
        });
    }
    #endregion

    #region Count
    [LayoutProperty]
    public int? Count
    {
        get => _countValue;
        set => this.SetProperty(ref _countValue, value, () => 
            { 
                _textValue = "";
            });
    }
    #endregion

    #region MaxCount
    [LayoutProperty]
    public int MaxCount
    {
        get => _maxCount;
        set => this.SetProperty(ref _maxCount, value);
    }
    #endregion

    #region ShowZero
    [LayoutProperty]
    public bool ShowZero
    {
        get => _showZero;
        set => this.SetProperty(ref _showZero, value);
    }
    #endregion

    #region Dot
    [LayoutProperty]
    public bool Dot
    {
        get => _dot;
        set => this.SetProperty(ref _dot, value);
    }
    #endregion


    public Badge()
    {
        // Initialize reactive properties
        Background = new Color(239, 68, 68);
        TextColor = Color.White;
        BorderColor = Background;
        BorderWidth = 0;
        FontSize = 12;
        Variant = BadgeVariant.Solid;
        BadgeSize = BadgeSize.Medium;
        Shape = BadgeShape.Rounded;
        MaxCount = 99;
        ShowZero = false;
        Dot = false;
    }

    public Badge(string text) : this()
    {
        _textValue = text;
    }

    public Badge(int count) : this()
    {
        _countValue = count;
    }


    private string GetDisplayText()
    {
        if (Dot) return "";

        if (_countValue.HasValue)
        {
            if (_countValue.Value == 0 && !ShowZero) return "";
            if (_countValue.Value > MaxCount) return $"{MaxCount}+";
            return _countValue.Value.ToString();
        }

        return _textValue;
    }

    private bool ShouldShow()
    {
        if (Dot) return true;
        if (_countValue.HasValue)
        {
            return _countValue.Value > 0 || ShowZero;
        }
        return !string.IsNullOrEmpty(_textValue);
    }

    private (float paddingH, float paddingV, float fontSize, float minSize) GetSizeMetrics()
    {
        return BadgeSize switch
        {
            BadgeSize.Small => (6f, 2f, 10f, 16f),
            BadgeSize.Medium => (8f, 4f, 12f, 20f),
            BadgeSize.Large => (10f, 6f, 14f, 24f),
            _ => (8f, 4f, 12f, 20f)
        };
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (!ShouldShow())
        {
            DesiredWidth = 0;
            DesiredHeight = 0;
            return;
        }

        var (paddingH, paddingV, fontSize, minSize) = GetSizeMetrics();
        float actualFontSize = FontSize > 0 ? FontSize : fontSize;

        if (Dot)
        {
            float dotSize = BadgeSize switch
            {
                BadgeSize.Small => 8f,
                BadgeSize.Medium => 10f,
                BadgeSize.Large => 12f,
                _ => 10f
            };
            DesiredWidth = dotSize;
            DesiredHeight = dotSize;
            return;
        }

        string displayText = GetDisplayText();
        float textWidth = displayText.Length * (actualFontSize * 0.65f);
        float textHeight = actualFontSize;

        float width = textWidth + paddingH * 2;
        float height = textHeight + paddingV * 2;

        // Ensure minimum size
        width = Math.Max(width, minSize);
        height = Math.Max(height, minSize);

        // For circle shape, make it square
        if (Shape == BadgeShape.Circle)
        {
            float size = Math.Max(width, height);
            width = size;
            height = size;
        }

        DesiredWidth = width;
        DesiredHeight = height;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        if (!ShouldShow()) return;

        float x = ComputedX;
        float y = ComputedY;
        float width = ComputedWidth;
        float height = ComputedHeight;

        // Calculate corner radius based on shape
        float cornerRadius = Shape switch
        {
            BadgeShape.Rounded => height / 2,
            BadgeShape.Square => 4f,
            BadgeShape.Circle => Math.Max(width, height) / 2,
            _ => height / 2
        };

        // Get brushes based on variant
        Brush bgBrush, fgBrush, borderBrush;
        float borderWidth = BorderWidth;

        switch (Variant)
        {
            case BadgeVariant.Outline:
                bgBrush = Color.Transparent;
                fgBrush = Background;
                borderBrush = Background;
                borderWidth = Math.Max(1f, BorderWidth);
                break;

            case BadgeVariant.Subtle:
                var pc = Background.PrimaryColor;
                bgBrush = new Color(pc.R, pc.G, pc.B, 0.15f);
                fgBrush = Background;
                borderBrush = Color.Transparent;
                break;

            case BadgeVariant.Solid:
            default:
                bgBrush = Background;
                fgBrush = TextColor;
                borderBrush = BorderColor;
                break;
        }

        // Draw background
        if (bgBrush.PrimaryColor.A > 0)
        {
            renderer.DrawRoundedRect(x, y, width, height, cornerRadius, bgBrush);
        }

        // Draw border
        if (borderWidth > 0 && borderBrush.PrimaryColor.A > 0)
        {
            renderer.DrawRoundedRectOutline(x, y, width, height, cornerRadius, borderWidth, borderBrush);
        }

        // Draw text (not for dot mode)
        if (!Dot)
        {
            string displayText = GetDisplayText();
            if (!string.IsNullOrEmpty(displayText))
            {
                var (_, _, fontSize, _) = GetSizeMetrics();
                float actualFontSize = FontSize > 0 ? FontSize : fontSize;

                var textSize = renderer.MeasureText(displayText, actualFontSize);
                float textX = x + (width - textSize.X) / 2;
                float textY = y + (height - textSize.Y) / 2;

                renderer.DrawText(displayText, textX, textY, fgBrush, actualFontSize);
            }
        }
    }
}

/// <summary>
/// A container that positions a Badge over its child content.
/// Similar to BadgedBox in Material Design.
/// </summary>
public class BadgeContainer : CompositeView<BadgeContainer>
{
    private float _extraLeft;
    private float _extraRight;
    private float _extraTop;
    private float _extraBottom;
    // Backing fields for properties (replacing generated 'field')
    private VisualElement? _content;
    private Badge? _badge;
    private HorizontalAlignment _badgeHorizontalPosition;
    private VerticalAlignment _badgeVerticalPosition;
    private float _badgeOffsetX;
    private float _badgeOffsetY;

    #region Content
    [LayoutProperty]
    public VisualElement? Content
    {
        get => _content;
        set => this.SetProperty(ref _content, value, () =>
        {
            RebuildChildren();
        });
    }
    #endregion

    #region Badge
    [LayoutProperty]
    public Badge? Badge
    {
        get => _badge;
        set => this.SetProperty(ref _badge, value, () =>
        {
            RebuildChildren();
        });
    }
    #endregion

    #region BadgeHorizontalPosition
    [LayoutProperty]
    public HorizontalAlignment BadgeHorizontalPosition
    {
        get => _badgeHorizontalPosition;
        set => this.SetProperty(ref _badgeHorizontalPosition, value);
    }
    #endregion

    #region BadgeVerticalPosition
    [LayoutProperty]
    public VerticalAlignment BadgeVerticalPosition
    {
        get => _badgeVerticalPosition;
        set => this.SetProperty(ref _badgeVerticalPosition, value);
    }
    #endregion

    #region BadgeOffsetX
    [LayoutProperty]
    public float BadgeOffsetX
    {
        get => _badgeOffsetX;
        set => this.SetProperty(ref _badgeOffsetX, value);
    }
    #endregion

    #region BadgeOffsetY
    [LayoutProperty]
    public float BadgeOffsetY
    {
        get => _badgeOffsetY;
        set => this.SetProperty(ref _badgeOffsetY, value);
    }
    #endregion

    #region BadgeOffset
    public Position BadgeOffset
    {
        get => new Position(BadgeOffsetX, BadgeOffsetY);
        set
        {
            BadgeOffsetX = value.X;
            BadgeOffsetY = value.Y;
        }
    }
    #endregion


    public BadgeContainer()
    {
        // Initialize reactive properties
        BadgeHorizontalPosition = HorizontalAlignment.Right;
        BadgeVerticalPosition = VerticalAlignment.Top;
        BadgeOffsetX = 0;
        BadgeOffsetY = 0;
    }

    public BadgeContainer(VisualElement content, Badge badge) : this()
    {
        Content = content;
        Badge = badge;
    }

    private void RebuildChildren()
    {
        ClearChildren();

        if (_content != null)
        {
            AddChild(_content);
        }

        if (_badge != null)
        {
            AddChild(_badge);
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float contentWidth = 0;
        float contentHeight = 0;

        if (Content != null)
        {
            Content.Measure(availableWidth, availableHeight);
            contentWidth = Content.DesiredWidth > 0 ? Content.DesiredWidth : Content.Width;
            contentHeight = Content.DesiredHeight > 0 ? Content.DesiredHeight : Content.Height;
        }
        else
        {
            contentWidth = Width > 0 ? Width : 0;
            contentHeight = Height > 0 ? Height : 0;
        }

        Badge?.Measure(float.MaxValue, float.MaxValue);

        _extraLeft = _extraRight = _extraTop = _extraBottom = 0;

        if (Badge != null && Badge.DesiredWidth > 0 && Badge.DesiredHeight > 0)
        {
            float badgeWidth = Badge.DesiredWidth;
            float badgeHeight = Badge.DesiredHeight;

            float badgeX = BadgeHorizontalPosition switch
            {
                HorizontalAlignment.Left => -badgeWidth / 2,
                HorizontalAlignment.Center => (contentWidth - badgeWidth) / 2,
                HorizontalAlignment.Right => contentWidth - badgeWidth / 2,
                _ => contentWidth - badgeWidth / 2
            } + BadgeOffsetX;

            float badgeY = BadgeVerticalPosition switch
            {
                VerticalAlignment.Top => -badgeHeight / 2,
                VerticalAlignment.Center => (contentHeight - badgeHeight) / 2,
                VerticalAlignment.Bottom => contentHeight - badgeHeight / 2,
                _ => -badgeHeight / 2
            } + BadgeOffsetY;

            if (badgeX < 0)
            {
                _extraLeft = Math.Max(_extraLeft, -badgeX);
            }
            if (badgeX + badgeWidth > contentWidth)
            {
                _extraRight = Math.Max(_extraRight, badgeX + badgeWidth - contentWidth);
            }
            if (badgeY < 0)
            {
                _extraTop = Math.Max(_extraTop, -badgeY);
            }
            if (badgeY + badgeHeight > contentHeight)
            {
                _extraBottom = Math.Max(_extraBottom, badgeY + badgeHeight - contentHeight);
            }
        }

        float desiredWidth = contentWidth + _extraLeft + _extraRight;
        float desiredHeight = contentHeight + _extraTop + _extraBottom;

        DesiredWidth = Width > 0 ? Width : desiredWidth;
        DesiredHeight = Height > 0 ? Height : desiredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float contentWidth = Math.Max(0, width - (_extraLeft + _extraRight));
        float contentHeight = Math.Max(0, height - (_extraTop + _extraBottom));
        float contentX = x + _extraLeft;
        float contentY = y + _extraTop;

        // Arrange content inside expanded bounds while preserving its own size
        if (Content != null)
        {
            Content.Arrange(contentX, contentY, contentWidth, contentHeight);
        }

        // Position badge
        if (Badge != null && Badge.DesiredWidth > 0 && Badge.DesiredHeight > 0)
        {
            float badgeWidth = Badge.DesiredWidth;
            float badgeHeight = Badge.DesiredHeight;

            float badgeX = BadgeHorizontalPosition switch
            {
                HorizontalAlignment.Left => contentX - badgeWidth / 2,
                HorizontalAlignment.Center => contentX + (contentWidth - badgeWidth) / 2,
                HorizontalAlignment.Right => contentX + contentWidth - badgeWidth / 2,
                _ => contentX + contentWidth - badgeWidth / 2
            };

            float badgeY = BadgeVerticalPosition switch
            {
                VerticalAlignment.Top => contentY - badgeHeight / 2,
                VerticalAlignment.Center => contentY + (contentHeight - badgeHeight) / 2,
                VerticalAlignment.Bottom => contentY + contentHeight - badgeHeight / 2,
                _ => contentY - badgeHeight / 2
            };

            badgeX += BadgeOffsetX;
            badgeY += BadgeOffsetY;

            Badge.Arrange(badgeX, badgeY, badgeWidth, badgeHeight);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Children are rendered by UI tree
    }
}
