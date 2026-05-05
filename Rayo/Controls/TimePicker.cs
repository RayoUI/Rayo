namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;

/// <summary>
/// TimePicker component - Time selection with hour/minute/period dropdowns.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
public class TimePicker : CompositeView<TimePicker>,
    Rayo.Core.Input.IPointerHandler,
    Rayo.Core.Interfaces.IGlobalPointerHandler
{
    private bool _isOpen = false;
    private int _selectedHour = 12;
    private int _selectedMinute = 0;
    private bool _is24HourFormat = false;
    private bool _isPM = false;

    // Visual components
    private Frame? _timeButton;
    private Label? _timeText;
    private Frame? _pickerFrame;
    private Frame? _dialogOverlay;
    private TimeSpan _originalSelectedTime;
    private bool _commitSelection;
    private bool _isRebuilding;
    private Action<TimeSpan>? _dialogConfirmed;
    private Action? _dialogCanceled;

    // Track the currently open timepicker globally
    private static TimePicker? _currentlyOpenTimePicker;

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region HeaderColor
    public Brush HeaderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region SelectedColor
    public Brush SelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region HoverColor
    public Brush HoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(50, 50, 55);
    #endregion

    #region TextColor
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region MutedTextColor
    public Brush MutedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(128, 128, 128);
    #endregion

    #region PickerBackground
    public Rendering.Brushes.Brush PickerBackground
    {
        get => field;
        set
        {
            field = value;
            MarkNeedsPaint();
        }
    } = new Color(30, 30, 35);
    #endregion

    #region PickerBorderColor
    public Brush PickerBorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(50, 55, 65);
    #endregion

    #region FieldCornerRadius
    public float FieldCornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 4;
    #endregion

    #region TimeFormat
    public string TimeFormat
    {
        get => field;
        set
        {
            this.SetProperty(ref field, value, () =>
            {
                _is24HourFormat = value.Contains("HH") || value.Contains("H");
                UpdateTimeText();
            });
        }
    } = "hh:mm tt";
    #endregion

    #region SelectedTime
    public TimeSpan SelectedTime
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            UpdateFromTimeSpan(value);
            UpdateTimeText();
            TimeChanged?.Invoke(value);
        });
    } = new TimeSpan(12, 0, 0);
    #endregion

    #region MinuteIncrement
    public int MinuteIncrement
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(1, Math.Min(30, value)));
    } = 1;
    #endregion

    // Events
    public event Action<TimeSpan>? TimeChanged;

    public TimePicker()
    {
        Background = new Color(40, 45, 60);
        Width = 180;
        Height = 44;
        UpdateFromTimeSpan(SelectedTime);
        BuildComponents();

        if (_timeButton != null)
        {
            AddChild(_timeButton);
        }
    }

    private void UpdateFromTimeSpan(TimeSpan time)
    {
        int hours = time.Hours;
        _selectedMinute = time.Minutes;

        if (_is24HourFormat)
        {
            _selectedHour = hours;
        }
        else
        {
            _isPM = hours >= 12;
            _selectedHour = hours % 12;
            if (_selectedHour == 0) _selectedHour = 12;
        }
    }

    private TimeSpan GetSelectedTimeSpan()
    {
        int hours = _selectedHour;
        if (!_is24HourFormat)
        {
            if (_isPM && hours != 12) hours += 12;
            else if (!_isPM && hours == 12) hours = 0;
        }
        return new TimeSpan(hours, _selectedMinute, 0);
    }

    private void BuildComponents()
    {
        _timeText = new Label
        {
            Text = FormatTime(),
            Foreground = TextColor,
            FontSize = 15,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var clockIcon = new IconButton(Icons.Clock)
            .IconSize(18)
            .IconColor(MutedTextColor)
            .Background(new Color(45, 50, 67))
            .HoverBackground(new Color(55, 60, 77))
            .PressedBackground(new Color(35, 40, 57))
            .BorderRadius(new CornerRadius(6))
            .Size(32);
        clockIcon.OnTapped(() => TogglePicker());

        var inputContent = new HStack
        {
            Spacing = 12,
            Alignment = Alignment.Center,
            JustifyContent = JustifyContent.SpaceBetween,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        inputContent.AddChild(_timeText);
        inputContent.AddChild(clockIcon);

        _timeButton = new Frame
        {
            Background = Background,
            BorderColor = BorderColor,
            BorderWidth = 1,
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderRadius = new CornerRadius(FieldCornerRadius)
        };
        _timeButton.Content(inputContent);
    }

    private string FormatTime()
    {
        var time = GetSelectedTimeSpan();
        var dateTime = DateTime.Today.Add(time);
        return dateTime.ToString(TimeFormat);
    }

    private void UpdateTimeText()
    {
        if (_timeText == null) return;
        _timeText.Text = FormatTime();
    }

    /// <summary>
    /// Splits the formatted time into a numeric part and an optional AM/PM suffix.
    /// For 12-hour formats that include "tt" the suffix ("AM"/"PM") is returned
    /// separately so the preview can render it in a smaller label beside the digits.
    /// </summary>
    private (string timePart, string suffix) SplitTimeAndSuffix()
    {
        var formatted = FormatTime();

        if (_is24HourFormat)
        {
            // No suffix in 24-hour mode
            return (formatted, string.Empty);
        }

        // Strip trailing AM/PM (and any space before it) from the formatted string.
        // DateTime.ToString("hh:mm tt") produces e.g. "12:00 AM".
        foreach (var suffix in new[] { " AM", " PM", "AM", "PM" })
        {
            if (formatted.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return (formatted[..^suffix.Length].TrimEnd(), suffix.Trim());
            }
        }

        return (formatted, string.Empty);
    }

    public void OpenPicker()
    {
        if (_isOpen) return;

        if (_currentlyOpenTimePicker != null && _currentlyOpenTimePicker != this)
        {
            _currentlyOpenTimePicker.ClosePicker();
        }

        _isOpen = true;
        _currentlyOpenTimePicker = this;
        if (!_isRebuilding)
        {
            _originalSelectedTime = SelectedTime;
        }
        _commitSelection = false;

        _pickerFrame = BuildPicker();
        _dialogOverlay = BuildDialogOverlay(_pickerFrame);

        Rayo.Core.OverlayManager.AddOverlay(_dialogOverlay);
        Rayo.Core.OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void ClosePicker()
    {
        if (!_isOpen || _dialogOverlay == null) return;

        Rayo.Core.OverlayManager.RemoveOverlay(_dialogOverlay);
        _isOpen = false;
        _pickerFrame = null;
        _dialogOverlay = null;
        if (!_commitSelection && !_isRebuilding)
        {
            SelectedTime = _originalSelectedTime;
        }
        _commitSelection = false;

        if (_currentlyOpenTimePicker == this)
        {
            _currentlyOpenTimePicker = null;
        }

        Rayo.Core.OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);
    }

    private void ConfirmPicker()
    {
        var confirmedTime = SelectedTime;
        _commitSelection = true;
        ClosePicker();

        var confirmedHandler = _dialogConfirmed;
        ClearDialogCallbacks();
        confirmedHandler?.Invoke(confirmedTime);
    }

    private void CancelPicker()
    {
        ClosePicker();

        var canceledHandler = _dialogCanceled;
        ClearDialogCallbacks();
        canceledHandler?.Invoke();
    }

    private void ClearDialogCallbacks()
    {
        _dialogConfirmed = null;
        _dialogCanceled = null;
    }

    /// <summary>
    /// Opens the picker as a standalone modal dialog that can be triggered from any custom control.
    /// </summary>
    public static TimePicker ShowDialog(TimeSpan initialTime, Action<TimeSpan> onConfirm, Action? onCancel = null, Action<TimePicker>? configure = null)
    {
        var picker = new TimePicker();
        picker.SelectedTime = initialTime;
        configure?.Invoke(picker);
        picker._dialogConfirmed = onConfirm;
        picker._dialogCanceled = onCancel;
        picker.OpenPicker();
        return picker;
    }

    private Frame BuildDialogOverlay(VisualElement content)
    {
        var overlay = new DialogOverlayFrame()
            .Background(new Color(0, 0, 0, 0.65f))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Center;
        overlay.Content(content);
        return overlay;
    }

    public void TogglePicker()
    {
        if (_isOpen)
            ClosePicker();
        else
            OpenPicker();
    }

    private Frame BuildPicker()
    {
        var content = new VStack { Spacing = 16 };
        content.HorizontalAlignment(HorizontalAlignment.Left);

        var headerLabel = new Label
        {
            Text = "Pick a time",
            Foreground = Color.White,
            FontSize = 18
        };

        // Build the preview: large time digits + optional AM/PM suffix label beside them.
        var (timePartText, suffixText) = SplitTimeAndSuffix();

        var previewTimeLabel = new Label
        {
            Text = timePartText,
            Foreground = Color.White,
            FontSize = 24
        };

        var previewContent = new HStack
        {
            Spacing = 6,
            Alignment = Alignment.End  // align baseline
        };
        previewContent.AddChild(previewTimeLabel);

        if (!string.IsNullOrEmpty(suffixText))
        {
            var previewSuffixLabel = new Label
            {
                Text = suffixText,
                Foreground = MutedTextColor,
                FontSize = 14
            };
            previewContent.AddChild(previewSuffixLabel);
        }

        var previewFrame = new Frame();
        previewFrame.Background(new Color(37, 39, 48));
        previewFrame.BorderRadius(new CornerRadius(12));
        previewFrame.Padding(new Thickness(16, 12, 16, 12));
        previewFrame.HorizontalAlignment(HorizontalAlignment.Left);
        previewFrame.Content(previewContent);

        var selectionArea = new HStack
        {
            Spacing = 8,
            Alignment = Alignment.Center,
            Padding = new Thickness(12)
        };
        selectionArea.HorizontalAlignment(HorizontalAlignment.Left);

        // Hour column
        selectionArea.AddChild(BuildNumberColumn("Hour", _is24HourFormat ? 0 : 1, _is24HourFormat ? 23 : 12, _selectedHour, (val) =>
        {
            _selectedHour = val;
            SelectedTime = GetSelectedTimeSpan();
            RebuildPicker();
        }));

        // Separator
        var separator = new Label
        {
            Text = ":",
            Foreground = TextColor,
            FontSize = 24
        };
        selectionArea.AddChild(separator);

        // Minute column
        selectionArea.AddChild(BuildNumberColumn("Min", 0, 59, _selectedMinute, (val) =>
        {
            _selectedMinute = val;
            SelectedTime = GetSelectedTimeSpan();
            RebuildPicker();
        }, MinuteIncrement));

        // AM/PM column (only for 12-hour format)
        if (!_is24HourFormat)
        {
            selectionArea.AddChild(BuildAmPmColumn());
        }

        var selectionSurface = new Frame();
        selectionSurface.Background(new Color(34, 36, 44));
        selectionSurface.BorderRadius(new CornerRadius(12));
        selectionSurface.BorderWidth(1);
        selectionSurface.BorderColor(PickerBorderColor);
        selectionSurface.HorizontalAlignment(HorizontalAlignment.Left);
        selectionSurface.Content(selectionArea);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Background = new Color(45, 45, 52),
            HoverBackground = new Color(55, 55, 62),
            TextColor = Color.White,
            BorderWidth = 0,
            BorderRadius = new CornerRadius(6),
            Width = 100,
            Height = 36
        };
        cancelButton.Tapped += (_) => CancelPicker();

        var okButton = new Button
        {
            Text = "OK",
            Background = HeaderColor,
            HoverBackground = new Color(79, 150, 255),
            TextColor = Color.White,
            BorderWidth = 0,
            BorderRadius = new CornerRadius(6),
            Width = 100,
            Height = 36
        };
        okButton.Tapped += (_) => ConfirmPicker();

        var buttons = new HStack()
            .Spacing(10)
            .JustifyContent(JustifyContent.End)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Right);
        buttons.AddChild(cancelButton);
        buttons.AddChild(okButton);

        content.AddChild(headerLabel);
        content.AddChild(previewFrame);
        content.AddChild(selectionSurface);
        content.AddChild(buttons);

        var Frame = (Frame)new Frame()
            .Background(PickerBackground)
            .BorderColor(PickerBorderColor)
            .BorderWidth(1)
            .BorderRadius(new CornerRadius(14))
            .Padding(new Thickness(16))
            .HorizontalAlignment(HorizontalAlignment.Center);
        Frame.Content(content);

        return Frame;
    }

    private VisualElement BuildNumberColumn(string label, int min, int max, int selected, Action<int> onSelect, int increment = 1)
    {
        const float ItemHeight = 36f;
        const float ItemSpacing = 2f;
        const float ItemStride = ItemHeight + ItemSpacing; // pixels per item row
        const float ViewportHeight = 180f;

        var column = new VStack { Spacing = 4 };

        var headerLabel = new Label
        {
            Text = label,
            Foreground = MutedTextColor,
            FontSize = 12
        };
        column.AddChild(headerLabel);

        var numbers = new VStack { Spacing = (int)ItemSpacing };

        int selectedIndex = 0;
        int itemIndex = 0;
        for (int i = min; i <= max; i += increment)
        {
            int value = i;
            bool isSelected = value == selected;

            if (isSelected)
                selectedIndex = itemIndex;

            var button = new Button
            {
                Text = value.ToString("D2"),
                Width = 50,
                Height = (int)ItemHeight,
                Background = isSelected ? SelectedColor : (Brush)Color.Transparent,
                HoverBackground = isSelected ? SelectedColor : HoverColor,
                TextColor = isSelected ? (Brush)Color.White : TextColor,
                BorderWidth = 0,
                BorderRadius = new CornerRadius(6)
            };
            button.Tapped += (_) => onSelect(value);
            numbers.AddChild(button);
            itemIndex++;
        }

        // Wrap the ScrollView in a Frame with explicit dimensions.
        // ScrollView shadows HasExplicitWidth/Height with private-set fields, so SetWidth/SetHeight on
        // ScrollView itself has no effect in Measure. Giving a parent Frame an explicit size ensures
        // the ScrollView receives a finite availableWidth/Height from the layout system.
        var scroll = new ScrollView
        {
            ShowHorizontalScrollbar = false,
            ShowVerticalScrollbar = true
        };
        scroll.Content(numbers);

        // Scroll so the selected item is centred in the viewport.
        // We set the offset directly on _verticalScrollOffset field equivalent — ScrollView.VerticalScrollOffset
        // clamps against MaxVerticalScroll, but at build time ComputedHeight is 0, so we compute the
        // raw pixel target and let VerticalScrollOffset clamp it on first Arrange.
        float selectedItemTop = selectedIndex * ItemStride;
        float centredOffset = selectedItemTop - (ViewportHeight / 2f - ItemHeight / 2f);
        scroll.VerticalScrollOffset = Math.Max(0, centredOffset);

        var scrollContainer = new Frame()
            .Width(60)
            .Height((int)ViewportHeight);
        scrollContainer.Background(Color.Transparent);
        scrollContainer.BorderWidth(0);
        scrollContainer.Content(scroll);

        column.AddChild(scrollContainer);

        return column;
    }

    private VisualElement BuildAmPmColumn()
    {
        var column = new VStack { Spacing = 4 };

        var headerLabel = new Label
        {
            Text = "",
            Foreground = MutedTextColor,
            FontSize = 12
        };
        column.AddChild(headerLabel);

        var buttons = new VStack { Spacing = 4, Padding = new Thickness(0, 20, 0, 0) };

        var amButton = new Button
        {
            Text = "AM",
            Width = 50,
            Height = 40,
            Background = !_isPM ? SelectedColor : (Brush)Color.Transparent,
            HoverBackground = !_isPM ? SelectedColor : HoverColor,
            TextColor = !_isPM ? (Brush)Color.White : TextColor,
            BorderWidth = 0,
            BorderRadius = new CornerRadius(6)
        };
        amButton.Tapped += (_) =>
        {
            _isPM = false;
            SelectedTime = GetSelectedTimeSpan();
            RebuildPicker();
        };
        buttons.AddChild(amButton);

        var pmButton = new Button
        {
            Text = "PM",
            Width = 50,
            Height = 40,
            Background = _isPM ? SelectedColor : (Brush)Color.Transparent,
            HoverBackground = _isPM ? SelectedColor : HoverColor,
            TextColor = _isPM ? (Brush)Color.White : TextColor,
            BorderWidth = 0,
            BorderRadius = new CornerRadius(6)
        };
        pmButton.Tapped += (_) =>
        {
            _isPM = true;
            SelectedTime = GetSelectedTimeSpan();
            RebuildPicker();
        };
        buttons.AddChild(pmButton);

        column.AddChild(buttons);

        return column;
    }

    private void RebuildPicker()
    {
        if (!_isOpen) return;
        _isRebuilding = true;
        ClosePicker();
        OpenPicker();
        _isRebuilding = false;
    }


    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 180;
        float measuredHeight = Height > 0 ? Height : 44;

        _timeButton?.Measure(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _timeButton?.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Visual rendering handled by _timeButton child
    }

    bool Rayo.Core.Interfaces.IGlobalPointerHandler.HandleGlobalPointer(System.Numerics.Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen || _pickerFrame == null)
        {
            return false;
        }

        if (IsPointInsidePicker(position))
        {
            return true;
        }

        CancelPicker();
        return false;
    }

    private bool IsPointInsidePicker(System.Numerics.Vector2 position)
    {
        if (_pickerFrame == null) return false;
        return position.X >= _pickerFrame.ComputedX &&
               position.X <= _pickerFrame.ComputedX + _pickerFrame.ComputedWidth &&
               position.Y >= _pickerFrame.ComputedY &&
               position.Y <= _pickerFrame.ComputedY + _pickerFrame.ComputedHeight;
    }

    private sealed class DialogOverlayFrame : Frame, Rayo.Core.Input.IPointerHandler
    {
    }

    // =========================================================================
    // IPOINTERHANDLER IMPLEMENTATION
    // =========================================================================

    private bool _isPressed;

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

            // If a child element (e.g. the clock IconButton) already handled this release
            // via its TapRecognizer, do not toggle the picker a second time.
            if (e.Handled) return;

            // Handle click - toggle picker if release is inside bounds
            bool isInsideBounds = e.Position.X >= ComputedX && e.Position.X <= ComputedX + ComputedWidth &&
                                  e.Position.Y >= ComputedY && e.Position.Y <= ComputedY + ComputedHeight;

            if (isInsideBounds)
            {
                if (_currentlyOpenTimePicker != null && _currentlyOpenTimePicker != this)
                {
                    _currentlyOpenTimePicker.ClosePicker();
                }

                TogglePicker();
            }
        }
    }
}
