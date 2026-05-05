namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;

/// <summary>
/// Stepper component - Increment/decrement control for numeric values
/// </summary>
public class Stepper : Rayo.Core.CompositeView<Stepper>
{
    private Frame? _container;
    private Button? _decrementButton;
    private Button? _incrementButton;
    private Label? _valueLabel;

    #region Value
    [LayoutProperty]
    public double Value
    {
        get => field;
        set
        {
            var clampedValue = Math.Clamp(value, Minimum, Maximum);
            this.SetProperty(ref field, clampedValue, () =>
            {
                UpdateValueLabel();
                ValueChanged?.Invoke(clampedValue);
            });
        }
    } = 0;
    #endregion

    #region Minimum
    public double Minimum
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (Value < value) Value = value;

            UpdateButtonStates();
        });
    } = 0;
    #endregion

    #region Maximum
    public double Maximum
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (Value > value) Value = value;
            UpdateButtonStates();
        });
    } = 100;
    #endregion

    #region Increment
    public double Increment
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(0.001, value));
    } = 1;
    #endregion

    #region ShowValue
    [LayoutProperty]
    public bool ShowValue
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_valueLabel != null)
                _valueLabel.IsVisible = value;
        });
    } = true;
    #endregion

    #region ValueFormat
    public string ValueFormat
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateValueLabel);
    } = "0";
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region ButtonColor
    public Brush ButtonColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region ButtonHoverColor
    public Brush ButtonHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(79, 150, 255);
    #endregion

    #region DisabledButtonColor
    public Brush DisabledButtonColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(60, 60, 65);
    #endregion

    #region TextColor
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region CornerRadius
    public float CornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 6;
    #endregion

    #region ButtonWidth
    public float ButtonWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 36;
    #endregion

    #region Orientation
    public Orientation Orientation
    {
        get => field;
        set => this.SetProperty(ref field, value, () => BuildComponents());
    } = Orientation.Horizontal;
    #endregion

    // Events
    public event Action<double>? ValueChanged;

    public Stepper()
    {
        Background = new Color(40, 40, 45);
        Width = 140;
        Height = 36;
        BuildComponents();

        if (_container != null)
        {
            AddChild(_container);
        }
    }

    private void BuildComponents()
    {
        // Clear existing children
        ClearChildren();
        _container = null;

        _decrementButton = new Button
        {
            Text = "-",
            Width = ButtonWidth,
            Height = Orientation == Orientation.Horizontal ? Height : ButtonWidth,
            Background = ButtonColor,
            HoverBackground = ButtonHoverColor,
            TextColor = TextColor,
            FontSize = 18,
            BorderWidth = 0,
            BorderRadius = Orientation == Orientation.Horizontal
                ? new CornerRadius(CornerRadius, 0, 0, CornerRadius)
                : new CornerRadius(0, 0, CornerRadius, CornerRadius)
        };
        _decrementButton.Tapped += (_) => Decrement();

        _valueLabel = new Label
        {
            Text = Value.ToString(ValueFormat),
            Foreground = TextColor,
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = ShowValue
        };

        var valueLabelContainer = new Frame
        {
            Background = Background,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(8, 0, 8, 0)
        };
        valueLabelContainer.Content(_valueLabel);

        _incrementButton = new Button
        {
            Text = "+",
            Width = ButtonWidth,
            Height = Orientation == Orientation.Horizontal ? Height : ButtonWidth,
            Background = ButtonColor,
            HoverBackground = ButtonHoverColor,
            TextColor = TextColor,
            FontSize = 18,
            BorderWidth = 0,
            BorderRadius = Orientation == Orientation.Horizontal
                ? new CornerRadius(0, CornerRadius, CornerRadius, 0)
                : new CornerRadius(CornerRadius, CornerRadius, 0, 0)
        };
        _incrementButton.Tapped += (_) => IncrementValue();

        VisualElement content;
        if (Orientation == Orientation.Horizontal)
        {
            var hstack = new HStack
            {
                Spacing = 0,
                Alignment = Alignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            hstack.AddChild(_decrementButton);
            hstack.AddChild(valueLabelContainer);
            hstack.AddChild(_incrementButton);
            content = hstack;
        }
        else
        {
            var vstack = new VStack
            {
                Spacing = 0,
                Alignment = Alignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            vstack.AddChild(_incrementButton);
            vstack.AddChild(valueLabelContainer);
            vstack.AddChild(_decrementButton);
            content = vstack;
        }

        _container = new Frame
        {
            Background = Background,
            BorderColor = BorderColor.PrimaryColor,
            BorderWidth = 1,
            BorderRadius = new CornerRadius(CornerRadius),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _container.Content(content);

        AddChild(_container);
        UpdateButtonStates();
    }

    private void UpdateValueLabel()
    {
        if (_valueLabel != null)
        {
            _valueLabel.Text = Value.ToString(ValueFormat);
        }
    }

    private void UpdateButtonStates()
    {
        if (_decrementButton != null)
        {
            bool canDecrement = Value > Minimum;
            _decrementButton.Background = canDecrement ? ButtonColor : DisabledButtonColor;
            _decrementButton.HoverBackground = canDecrement ? ButtonHoverColor : DisabledButtonColor;
        }

        if (_incrementButton != null)
        {
            bool canIncrement = Value < Maximum;
            _incrementButton.Background = canIncrement ? ButtonColor : DisabledButtonColor;
            _incrementButton.HoverBackground = canIncrement ? ButtonHoverColor : DisabledButtonColor;
        }
    }

    public void Decrement()
    {
        if (Value > Minimum)
        {
            Value = Math.Max(Minimum, Value - Increment);
            UpdateButtonStates();
        }
    }

    public void IncrementValue()
    {
        if (Value < Maximum)
        {
            Value = Math.Min(Maximum, Value + Increment);
            UpdateButtonStates();
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth, measuredHeight;

        if (Orientation == Orientation.Horizontal)
        {
            measuredWidth = Width > 0 ? Width : 140;
            measuredHeight = Height > 0 ? Height : 36;
        }
        else
        {
            measuredWidth = Width > 0 ? Width : 36;
            measuredHeight = Height > 0 ? Height : 100;
        }

        _container?.Measure(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _container?.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Visual rendering handled by _container child
    }
}

/// <summary>
/// Orientation for layout direction
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical
}
