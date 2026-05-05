namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interactions;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// ComboBox/Dropdown component for selecting from a list of options.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
public class ComboBox : Rayo.Core.CompositeView<ComboBox>,
    Rayo.Core.Input.IPointerHandler,
    IGlobalPointerHandler
{
    private bool _isOpen = false;

    // Visual components
    private Frame? _dropdownButton;
    private Label? _selectedText;
    private Icon? _chevronIcon;  // Chevron icon (now using Icon control)
    private Frame? _dropdownFrame;
    private VStack? _itemsStack;

    // Track the currently open combobox globally to close it when another opens
    private static ComboBox? _currentlyOpenComboBox;

    static ComboBox()
    {
        ScrollInteractionNotifier.ScrollActivity += OnScrollActivity;
    }

    private static void OnScrollActivity(VisualElement source)
    {
        if (_currentlyOpenComboBox == null) return;

        // If the scroll originated inside the dropdown panel, don't close it
        if (IsInsideDropdown(source)) return;

        CloseCurrentComboBox();
    }

    private static bool IsInsideDropdown(VisualElement source)
    {
        var dropdownFrame = _currentlyOpenComboBox?._dropdownFrame;
        if (dropdownFrame == null) return false;

        var current = (VisualElement?)source;
        while (current != null)
        {
            if (current == dropdownFrame) return true;
            current = current.Parent;
        }
        return false;
    }

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => base.Background;
        set
        {
            base.Background = value;
            _dropdownButton?.Background(value);
        }
    }
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { _dropdownButton?.BorderColor(value); });
    } = new Color(200, 200, 200);
    #endregion

    #region HoverColor
    [PaintProperty]
    public Brush HoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(240, 240, 240);
    #endregion

    #region SelectedColor
    [PaintProperty]
    public Brush SelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(0, 120, 215);
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            UpdateSelectedText();
            if (_chevronIcon != null)
            {
                _chevronIcon.Color = value.PrimaryColor;  // Update chevron color
            }
        });
    } = Color.Black;
    #endregion

    #region ItemHeight
    [LayoutProperty]
    public float ItemHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 32;
    #endregion

    #region ItemTextAlignment
    [LayoutProperty]
    public HorizontalAlignment ItemTextAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = HorizontalAlignment.Left;
    #endregion

    #region MaxDropdownHeight
    [LayoutProperty]
    public float MaxDropdownHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 200;
    #endregion

    #region Items
    [LayoutProperty]
    public List<string> Items
    {
        get => field;
        set => this.SetProperty(ref field, value ?? new List<string>(), () =>
        {
            if (SelectedIndex >= field.Count)
            {
                SelectedIndex = -1;
            }
            RebuildItems();
        });
    } = new();
    #endregion

    #region SelectedIndex
    [LayoutProperty]
    public int SelectedIndex
    {
        get => field;
        set => this.SetPropertyCondition(ref field, value, 
        (current, incoming) => (current != incoming && incoming >= -1 && incoming < Items.Count)
        , () =>
        {
            UpdateSelectedText();
            SelectionChanged?.Invoke(field);
        });
    } = -1;
    #endregion

    #region SelectedItem
    public string? SelectedItem
    {
        get => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;
        set
        {
            int index = value != null ? Items.IndexOf(value) : -1;
            SelectedIndex = index;
        }
    }
    #endregion

    #region Placeholder
    [PaintProperty]
    public string Placeholder
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateSelectedText);
    } = "Select an item...";
    #endregion

    // Internal state management
    private bool _isPressed;

    // =========================================================================
    // EVENTS
    // =========================================================================

    public event Action<int>? SelectionChanged;

    // =========================================================================
    // FLUENT API MANUAL METHODS
    // =========================================================================

    public ComboBox AddItem(string item)
    {
        Items.Add(item);
        RebuildItems();
        return this;
    }

    public ComboBox AddItems(params string[] items)
    {
        Items.AddRange(items);
        RebuildItems();
        return this;
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================


    /// <summary>
    /// Closes the currently open combobox if any (called when clicking anywhere)
    /// </summary>
    public static void CloseCurrentComboBox()
    {
        if (_currentlyOpenComboBox != null)
        {
            _currentlyOpenComboBox.CloseDropdown();
        }
    }

    internal static void HandleGlobalPointer(Vector2 position, VisualElement? hitElement)
    {
        _currentlyOpenComboBox?.HandleGlobalPointerInternal(position, hitElement);
    }

    /// <summary>
    /// IGlobalPointerHandler implementation - instance method
    /// </summary>
    bool IGlobalPointerHandler.HandleGlobalPointer(Vector2 position, VisualElement? hitElement)
    {
        return HandleGlobalPointerInternal(position, hitElement);
    }

    private bool HandleGlobalPointerInternal(Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen)
        {
            return false;  // Not consuming event
        }

        if (IsElementWithinCombo(hitElement))
        {
            return true;  // Event is for us, consume it
        }

        if (IsPointInsideCombo(position) || IsPointInsideDropdown(position))
        {
            return true;  // Event is for us, consume it
        }

        // Click outside - close dropdown
        CloseDropdown();
        return false;  // Allow other handlers to process
    }

    public ComboBox()
    {
        Background = Color.White;
        Width = 200;
        Height = 32;
        BuildComponents();

        // Add the dropdown button as a child so it's part of the UI tree
        if (_dropdownButton != null)
        {
            AddChild(_dropdownButton);
        }
    }

    private void BuildComponents()
    {
        // Create the selected text label
        _selectedText = (Label)new Label()
            .Text(Placeholder)
            .Foreground(new Color(128, 128, 128))
            .FontSize(14)
            .SetInputTransparent(true);

        // Create the chevron icon using Icon control
        _chevronIcon = new Icon(Icons.ChevronDown)
        {
            Width = 16,
            Height = 16,
            Color = TextColor.PrimaryColor
        };
        _chevronIcon.SetInputTransparent(true);

        // Create container with text and icon
        // JustifyContent.SpaceBetween pushes chevron to the right edge
        var contentStack = new HStack()
            .JustifyContent(JustifyContent.SpaceBetween)
            .Alignment(Alignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
        contentStack.AddChild(_selectedText);
        contentStack.AddChild(_chevronIcon);

        _dropdownButton = (Frame)new Frame()
            .Background(Background)
            .BorderColor(BorderColor)
            .BorderWidth(1)
            .Padding(new Thickness(12, 8, 12, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .SetInputTransparent(true); // Make Frame transparent to input
        _dropdownButton.Content(contentStack);
        _dropdownButton.BorderRadius = new CornerRadius(4);
    }



    private void RebuildItems()
    {
        if (_itemsStack == null) return;

        _itemsStack.ClearChildren();

        for (int i = 0; i < Items.Count; i++)
        {
            int index = i; // Capture for closure
            string item = Items[i];

            bool isSelected = index == SelectedIndex;

            // Create clickable button for each item
            var itemButton = new Button
            {
                Text = item,
                Height = ItemHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = ItemTextAlignment,
                Background = isSelected ? SelectedColor : Color.Transparent,
                TextColor = isSelected ? Color.White : TextColor,
                HoverBackground = HoverColor,
                BorderWidth = 0,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            itemButton.Tapped += (_) =>
            {
                SelectedIndex = index;
                CloseDropdown();
            };

            _itemsStack.AddChild(itemButton);
        }
    }

    private void UpdateSelectedText()
    {
        if (_selectedText == null) return;

        if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
        {
            _selectedText.Text(Items[SelectedIndex])
                        .Foreground(TextColor);
        }
        else
        {
            _selectedText.Text(Placeholder)
                        .Foreground(new Color(128, 128, 128));
        }
    }


    public void OpenDropdown()
    {
        var app = UIApplication.Current;
        if (_isOpen) return;

        // Close any previously open combobox before opening this one
        if (_currentlyOpenComboBox != null && _currentlyOpenComboBox != this)
        {
            _currentlyOpenComboBox.CloseDropdown();
        }

        _isOpen = true;
        _currentlyOpenComboBox = this;

        // Create dropdown Frame
        _itemsStack = new VStack()
            .Spacing(0);

        RebuildItems();

        // Calculate dropdown height
        float dropdownHeight = Math.Min(Items.Count * ItemHeight, MaxDropdownHeight);

        // Calculate dropdown width (same as ComboBox, clamped to window)
        float windowWidth = app?.Window.Width ?? Rayo.Core.OverlayManager.WindowWidth;
        float dropdownWidth = ComputedWidth;
        if (windowWidth > 0)
        {
            dropdownWidth = Math.Min(dropdownWidth, windowWidth);
        }
        
        // Create ScrollView with explicit size
        var scrollView = new ScrollView();
        scrollView.Width = dropdownWidth;
        scrollView.Height = dropdownHeight;
        scrollView.HorizontalAlignment = HorizontalAlignment.Left;  // Prevent stretch expansion
        scrollView.VerticalAlignment = VerticalAlignment.Top;        // Prevent stretch expansion
        scrollView.Content(_itemsStack);
        
        // Create dropdown Frame with NO padding (content fills completely)
        _dropdownFrame = new Frame();
        _dropdownFrame.Width = dropdownWidth;
        _dropdownFrame.Height = dropdownHeight;
        _dropdownFrame.Background = Background;
        _dropdownFrame.BorderColor = BorderColor;
        _dropdownFrame.BorderWidth = 1;
        _dropdownFrame.Padding = new Thickness(0);  // FIX: Remove padding so content fits exactly
        _dropdownFrame.HorizontalAlignment = HorizontalAlignment.Left;  // Prevent stretch
        _dropdownFrame.VerticalAlignment = VerticalAlignment.Top;        // Prevent stretch
        _dropdownFrame.Content(scrollView);
        _dropdownFrame.BorderRadius = new CornerRadius(4);

        // Position below the combobox
        float x = ComputedX;
        float y = ComputedY + ComputedHeight + 4;

        // Keep within window bounds
        float windowHeight = app?.Window.Height ?? Rayo.Core.OverlayManager.WindowHeight;
        windowWidth = app?.Window.Width ?? Rayo.Core.OverlayManager.WindowWidth;
        if (windowHeight > 0 && y + dropdownHeight > windowHeight)
        {
            y = ComputedY - dropdownHeight - 4; // Show above if not enough space below
        }

        if (windowWidth > 0 && x + _dropdownFrame.Width > windowWidth)
        {
            x = Math.Max(0, windowWidth - _dropdownFrame.Width);
        }

        // Set position using SetX/Y (like Menu does)
        _dropdownFrame.X(x);
        _dropdownFrame.Y(y);

        Rayo.Core.OverlayManager.AddOverlay(_dropdownFrame);
        
        // Register for global pointer events
        Rayo.Core.OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void CloseDropdown()
    {
        if (!_isOpen || _dropdownFrame == null) return;

        Rayo.Core.OverlayManager.RemoveOverlay(_dropdownFrame);
        _isOpen = false;
        _dropdownFrame = null;
        _itemsStack = null;

        // Unregister from global pointer events
        Rayo.Core.OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);

        // Clear the global reference if this was the currently open combobox
        if (_currentlyOpenComboBox == this)
        {
            _currentlyOpenComboBox = null;
        }
    }

    public void ToggleDropdown()
    {
        if (_isOpen)
            CloseDropdown();
        else
            OpenDropdown();
    }

    private bool IsElementWithinCombo(VisualElement? element)
    {
        var current = element;
        while (current != null)
        {
            if (current == this || current == _dropdownFrame)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private bool IsPointInsideCombo(Vector2 position)
    {
        return position.X >= ComputedX && position.X <= ComputedX + ComputedWidth &&
               position.Y >= ComputedY && position.Y <= ComputedY + ComputedHeight;
    }

    private bool IsPointInsideDropdown(Vector2 position)
    {
        if (_dropdownFrame == null)
        {
            return false;
        }

        return position.X >= _dropdownFrame.ComputedX &&
               position.X <= _dropdownFrame.ComputedX + _dropdownFrame.ComputedWidth &&
               position.Y >= _dropdownFrame.ComputedY &&
               position.Y <= _dropdownFrame.ComputedY + _dropdownFrame.ComputedHeight;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 200;
        float measuredHeight = Height > 0 ? Height : 32;

        // Let base class measure children automatically
        base.Measure(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        
        // Explicitly arrange the dropdown button to fill the ComboBox area
        _dropdownButton?.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // The dropdown button and its children (including chevron) are rendered automatically by UITree
        // No need to manually render the chevron here
    }

    // =========================================================================
    // IPOINTERHANDLER IMPLEMENTATION
    // =========================================================================

    void Rayo.Core.Input.IPointerHandler.OnPointerPressed(Rayo.Core.Input.PointerEventArgs e)
    {
        _isPressed = true;
        MarkNeedsPaint();
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerReleased(Rayo.Core.Input.PointerEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            MarkNeedsPaint();

            // Handle click - toggle dropdown if release is inside bounds
            bool isInsideBounds = e.Position.X >= ComputedX && e.Position.X <= ComputedX + ComputedWidth &&
                                  e.Position.Y >= ComputedY && e.Position.Y <= ComputedY + ComputedHeight;

            if (isInsideBounds)
            {
                // Close any other open combobox first
                if (_currentlyOpenComboBox != null && _currentlyOpenComboBox != this)
                {
                    _currentlyOpenComboBox.CloseDropdown();
                }

                ToggleDropdown();
            }
        }
    }
}
