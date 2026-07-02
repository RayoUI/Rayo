namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;
using Rayo.Styling;

/// <summary>
/// TimePicker component - Time selection with hour/minute/period dropdowns.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
public class TimePicker : BorderCompositeView<TimePicker>,
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
    private ButtonIcon? _clockIcon;
    private Frame? _pickerFrame;
    private Frame? _dialogOverlay;
    private TimeSpan _originalSelectedTime;
    private bool _commitSelection;
    private bool _isRebuilding;
    private Action<TimeSpan>? _dialogConfirmed;
    private Action? _dialogCanceled;

    /// <summary>
    /// Gets or sets how the time selector is presented. Auto preserves the dialog presentation.
    /// </summary>
    public PickerDisplayMode DisplayMode { get; set; } = PickerDisplayMode.Auto;

    // Track the currently open timepicker globally
    private static TimePicker? _currentlyOpenTimePicker;

    #region HeaderColor
    public Brush HeaderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region SelectedColor
    public Brush SelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region HoverColor
    public Brush HoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region TextColor
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region MutedTextColor
    public Brush MutedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region PickerBackground
    public Rendering.Brushes.Brush PickerBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, MarkNeedsPaint);
    } = Color.Transparent;
    #endregion

    #region PickerBorderBrush
    public Brush PickerBorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
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
        InitializeTheme();
        UpdateFromTimeSpan(SelectedTime);
        BuildComponents();

        if (_timeButton != null)
        {
            AddChild(_timeButton);
        }
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(Background), (Brush)palette.Surface, value => Background = value);
        SetThemeValue(nameof(HeaderColor), (Brush)palette.Primary, value => HeaderColor = value);
        SetThemeValue(nameof(SelectedColor), (Brush)palette.Primary, value => SelectedColor = value);
        SetThemeValue(nameof(HoverColor), (Brush)palette.SurfaceHover, value => HoverColor = value);
        SetThemeValue(nameof(TextColor), (Brush)palette.OnSurface, value => TextColor = value);
        SetThemeValue(nameof(MutedTextColor), (Brush)palette.OnDisabled, value => MutedTextColor = value);
        SetThemeValue(nameof(PickerBackground), (Brush)palette.Surface, value => PickerBackground = value);
        SetThemeValue(nameof(PickerBorderBrush), (Brush)palette.Border, value => PickerBorderBrush = value);
        SetThemeValue(nameof(BorderBrush), (Brush)palette.Border, value => BorderBrush = value);

        ApplyInputAppearance();

        if (_isOpen)
            RebuildPicker();
    }

    protected override void OnBorderBrushChanged()
    {
        base.OnBorderBrushChanged();
        _timeButton?.BorderBrush(BorderBrush);
    }

    protected override void OnBorderThicknessChanged()
    {
        base.OnBorderThicknessChanged();
        _timeButton?.BorderThickness(BorderThickness);
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

        _clockIcon = new ButtonIcon(Icons.Clock)
            .Variant(ButtonVariant.Ghost)
            .IconSize(18)
            .IconColor(MutedTextColor)
            .BorderRadius(new CornerRadius(6))
            .Size(32);
        _clockIcon.OnTapped(() => TogglePicker());

        var inputContent = new HStack
        {
            Spacing = 12,
            Alignment = Alignment.Center,
            JustifyContent = JustifyContent.SpaceBetween,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        inputContent.AddChild(_timeText);
        inputContent.AddChild(_clockIcon);

        _timeButton = new Frame
        {
            Background = Background,
            BorderBrush = BorderBrush,
            BorderThickness = BorderThickness,
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderRadius = new CornerRadius(FieldCornerRadius)
        };
        _timeButton.Content(inputContent);
    }

    private void ApplyInputAppearance()
    {
        if (_timeText != null)
            _timeText.Foreground = TextColor;

        if (_clockIcon != null)
        {
            _clockIcon.IconColor = MutedTextColor;
            _clockIcon.Background = PickerBackground;
            _clockIcon.HoverBackground = HoverColor;
        }

        if (_timeButton != null)
        {
            _timeButton.Background = Background;
            _timeButton.BorderBrush = BorderBrush;
        }
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
        _dialogOverlay = UsesPopupPresentation()
            ? BuildPopupOverlay(_pickerFrame)
            : BuildDialogOverlay(_pickerFrame);

        Rayo.Core.OverlayManager.AddOverlay(_dialogOverlay, this);
        Rayo.Core.OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void ClosePicker()
    {
        if (!_isOpen || _dialogOverlay == null) return;

        Rayo.Core.OverlayManager.RemoveOverlay(_dialogOverlay);
        _isOpen = false;
        _pickerFrame = null;
        _dialogOverlay = null;
        if (!_commitSelection && !_isRebuilding && !UsesPopupPresentation())
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
        picker.DisplayMode = PickerDisplayMode.Dialog;
        picker._dialogConfirmed = onConfirm;
        picker._dialogCanceled = onCancel;
        picker.OpenPicker();
        return picker;
    }

    private bool UsesPopupPresentation()
    {
        return DisplayMode == PickerDisplayMode.Popup;
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

    private Frame BuildPopupOverlay(Frame popup)
    {
        popup.HorizontalAlignment = HorizontalAlignment.Left;
        popup.VerticalAlignment = VerticalAlignment.Top;
        return new AnchoredPopup(this, popup);
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
            Foreground = TextColor,
            FontSize = 18
        };

        // Build the preview: large time digits + optional AM/PM suffix label beside them.
        var (timePartText, suffixText) = SplitTimeAndSuffix();

        var previewTimeLabel = new Label
        {
            Text = timePartText,
            Foreground = TextColor,
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
        previewFrame.Background(HoverColor);
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
        selectionSurface.Background(PickerBackground);
        selectionSurface.BorderRadius(new CornerRadius(12));
        selectionSurface.BorderThickness(1);
        selectionSurface.BorderBrush(PickerBorderBrush);
        selectionSurface.HorizontalAlignment(HorizontalAlignment.Left);
        selectionSurface.Content(selectionArea);

        // Popup mode uses the interactive selection surface directly. The
        // adjacent field already displays the selected time.
        if (UsesPopupPresentation())
        {
            return selectionSurface;
        }

        var cancelButton = new Button
        {
            Text = "Cancel",
            Variant = ButtonVariant.Secondary,
            BorderThickness = 0,
            BorderRadius = new CornerRadius(6),
            Width = 100,
            Height = 36
        };
        cancelButton.Tapped += (_) => CancelPicker();

        var okButton = new Button
        {
            Text = "OK",
            Variant = ButtonVariant.Primary,
            BorderThickness = 0,
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
            .BorderBrush(PickerBorderBrush)
            .BorderThickness(1)
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
                TextColor = isSelected ? (Brush)EffectiveTheme.Colors.OnPrimary : TextColor,
                BorderThickness = 0,
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
        // We set the offset directly on _verticalScrollOffset field equivalent � ScrollView.VerticalScrollOffset
        // clamps against MaxVerticalScroll, but at build time ComputedHeight is 0, so we compute the
        // raw pixel target and let VerticalScrollOffset clamp it on first Arrange.
        float selectedItemTop = selectedIndex * ItemStride;
        float centredOffset = selectedItemTop - (ViewportHeight / 2f - ItemHeight / 2f);
        scroll.VerticalScrollOffset = Math.Max(0, centredOffset);

        var scrollContainer = new Frame()
            .Width(60)
            .Height((int)ViewportHeight);
        scrollContainer.Background(Color.Transparent);
        scrollContainer.BorderThickness(0);
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
            TextColor = !_isPM ? (Brush)EffectiveTheme.Colors.OnPrimary : TextColor,
            BorderThickness = 0,
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
            TextColor = _isPM ? (Brush)EffectiveTheme.Colors.OnPrimary : TextColor,
            BorderThickness = 0,
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


    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 180;
        float measuredHeight = Height > 0 ? Height : 44;

        _timeButton?.MeasureUpdate(measuredWidth, measuredHeight);

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _timeButton?.ArrangeUpdate(x, y, width, height);
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

            // If a child element (e.g. the clock ButtonIcon) already handled this release
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
