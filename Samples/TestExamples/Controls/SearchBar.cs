namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// SearchBar component - Text input with search icon and clear button
/// </summary>
public class SearchBar : CompositeView<SearchBar>, 
    IInputHandler, 
    IFocusable
{
    private Entry? _entry;
    private Frame? _container;
    private Button? _clearButton;
    private Label? _searchIcon;

    #region Text
    [PaintProperty]
    public string Text
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_entry != null && _entry.Text != value)
                _entry.Text = value;
            UpdateClearButtonVisibility();
            TextChanged?.Invoke(value);
        });
    } = "";
    #endregion

    #region Placeholder
    [PaintProperty]
    public string Placeholder
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_entry != null)
                _entry.Placeholder = value;
        });
    } = "Search...";
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_container != null && !IsFocused)
                _container.BorderColor = value.PrimaryColor;
        });
    } = new Color(100, 100, 100);
    #endregion

    #region FocusedBorderColor
    [PaintProperty]
    public Brush FocusedBorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_container != null && IsFocused)
                _container.BorderColor = value.PrimaryColor;
        });
    } = new Color(59, 130, 246);
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_entry != null)
                _entry.TextColor = value.PrimaryColor;
        });
    } = Color.White;
    #endregion

    #region PlaceholderColor
    [PaintProperty]
    public Brush PlaceholderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_entry != null)
                _entry.PlaceholderColor = value.PrimaryColor;
        });
    } = new Color(128, 128, 128);
    #endregion

    #region Background
    public new Brush Background
    {
        get => base.Background;
        set
        {
            base.Background = value;
            if (_container != null)
                _container.Background = value;
        }
    }
    #endregion

    #region IconColor
    [PaintProperty]
    public Brush IconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_searchIcon != null)
                _searchIcon.Foreground = value.PrimaryColor;
        });
    } = new Color(128, 128, 128);
    #endregion

    #region CornerRadius
    [LayoutProperty]
    public float CornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_container != null)
                _container.BorderRadius = new CornerRadius(value);
        });
    } = 20;
    #endregion

    #region ShowClearButton
    [PaintProperty]
    public bool ShowClearButton
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateClearButtonVisibility);
    } = true;
    #endregion

    #region SearchOnEnter
    public bool SearchOnEnter
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    #region SearchAsYouType
    public bool SearchAsYouType
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = false;
    #endregion

    #region IsFocused
    [NotFluent]
    public bool IsFocused { get; set; }
    #endregion

    // Events
    public event Action<string>? TextChanged;
    public event Action<string>? SearchSubmitted;

    public SearchBar()
    {
        Background = new Color(40, 40, 45);
        Width = 300;
        Height = 44;
        BuildComponents();

        if (_container != null)
        {
            AddChild(_container);
        }
    }

    private void BuildComponents()
    {
        _searchIcon = new Label
        {
            Text = "\U0001F50D", // Magnifying glass
            Foreground = IconColor.PrimaryColor,
            FontSize = 16,
            Padding = new Thickness(right: -15)
        };

        _entry = new Entry
        {
            Placeholder = Placeholder,
            Background = Color.Transparent,
            FocusBackground = Color.Transparent,
            FocusBorderColor = Color.Transparent,
            BorderWidth = 0,
            TextColor = TextColor.PrimaryColor,
            PlaceholderColor = PlaceholderColor.PrimaryColor,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        _entry.TextChanged += (text) =>
        {
            Text = text;
            if (SearchAsYouType)
            {
                SearchSubmitted?.Invoke(text);
            }
        };

        _entry.Enter += () =>
        {
            if (SearchOnEnter)
            {
                SearchSubmitted?.Invoke(Text);
            }
        };

        _clearButton = new Button
        {
            Text = "\u2715", // X symbol
            Width = 24,
            Height = 24,
            Background = Color.Transparent,
            HoverBackground = new Color(60, 60, 65),
            TextColor = IconColor.PrimaryColor,
            BorderWidth = 0,
            BorderRadius = new CornerRadius(12),
            IsVisible = false
        };

        _clearButton.Tapped += (_) => ClearText();

        var content = new HStack
        {
            Spacing = 10,
            Alignment = Alignment.Center,
            Padding = new Thickness(14, 8, 10, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        content.AddChild(_searchIcon);
        content.AddChild(_entry);
        content.AddChild(_clearButton);

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
        ApplyCurrentProperties();
    }

    private void ApplyCurrentProperties()
    {
        if (_container != null)
        {
            _container.Background = Background;
            _container.BorderRadius = new CornerRadius(CornerRadius);
            _container.BorderColor = (IsFocused ? FocusedBorderColor : BorderColor).PrimaryColor;
        }

        if (_entry != null)
        {
            _entry.Placeholder = Placeholder;
            _entry.TextColor = TextColor.PrimaryColor;
            _entry.PlaceholderColor = PlaceholderColor.PrimaryColor;
            if (_entry.Text != Text)
                _entry.Text = Text;
        }

        if (_searchIcon != null)
            _searchIcon.Foreground = IconColor.PrimaryColor;

        if (_clearButton != null)
            _clearButton.TextColor = IconColor.PrimaryColor;

        UpdateClearButtonVisibility();
    }

    private void UpdateClearButtonVisibility()
    {
        if (_clearButton != null)
        {
            _clearButton.IsVisible = ShowClearButton && !string.IsNullOrEmpty(Text);
        }
    }

    public void ClearText()
    {
        Text = "";
        if (_entry != null)
        {
            _entry.Text = "";
        }
        SearchSubmitted?.Invoke("");
    }

    public void Focus()
    {
        var app = UIApplication.Current;
        if (app?.EventManager != null && _entry != null)
        {
            app.EventManager.SetFocus(_entry);
        }
    }

    // IInputHandler
    public bool CanHandleInput => true;

    public bool HandleInput(InputEventArgs args)
    {
        // Delegate to entry
        if (_entry is IInputHandler handler)
        {
            return handler.HandleInput(args);
        }
        return false;
    }

    // IFocusable
    public void OnFocusGained()
    {
        IsFocused = true;
        if (_container != null)
        {
            _container.BorderColor = FocusedBorderColor.PrimaryColor;
        }
        MarkNeedsPaint();
    }

    public void OnFocusLost()
    {
        IsFocused = false;
        if (_container != null)
        {
            _container.BorderColor = BorderColor.PrimaryColor;
        }
        MarkNeedsPaint();
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 300;
        float measuredHeight = Height > 0 ? Height : 44;

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
