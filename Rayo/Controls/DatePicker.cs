namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;
using System.Globalization;
using Rayo.Styling;

/// <summary>
/// DatePicker component - Calendar-based date selection.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
public class DatePicker : BorderCompositeView<DatePicker>,
    Rayo.Core.Input.IPointerHandler,
    IGlobalPointerHandler
{
    private DateTime _displayMonth = DateTime.Today;
    private bool _isOpen = false;
    private bool _isRebuildingCalendar = false;

    // Visual components
    private Frame? _dateButton;
    private Label? _dateText;
    private ButtonIcon? _calendarIcon;
    private HStack? _inputContent;
    private Frame? _calendarFrame;
    private DateTime _originalSelectedDate;
    private DateTime _pendingSelectedDate;
    private bool _commitSelection;
    private Frame? _dialogOverlay;
    private Action<DateTime>? _dialogConfirmed;
    private Action? _dialogCanceled;

    /// <summary>
    /// Gets or sets how the calendar is presented. Auto preserves the dialog presentation.
    /// </summary>
    public PickerDisplayMode DisplayMode { get; set; } = PickerDisplayMode.Auto;

    // Track the currently open datepicker globally to close it when another opens
    private static DatePicker? _currentlyOpenDatePicker;

    // Styling
    #region HeaderColor
    public Brush HeaderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region SelectedDateColor
    public Brush SelectedDateColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region TodayColor
    public Brush TodayColor
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

    #region HeaderTextColor
    public Brush HeaderTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region CalendarBackground
    public Rendering.Brushes.Brush CalendarBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, MarkNeedsPaint);
    } = Color.Transparent;
    #endregion

    #region CalendarBorderBrush
    public Brush CalendarBorderBrush
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

    #region FieldCornerRadius
    public float FieldCornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 4;
    #endregion

    #region DateFormat
    [LayoutProperty]
    public string DateFormat
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateDateText);
    } = "MM/dd/yyyy";
    #endregion

    #region SelectedDate
    public DateTime SelectedDate
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            _displayMonth = new DateTime(value.Year, value.Month, 1);
            UpdateDateText();
            DateChanged?.Invoke(value);
        });
    } = DateTime.Today;
    #endregion

    // Events
    public event Action<DateTime>? DateChanged;

    /// <summary>
    /// Closes the currently open datepicker if any (called when clicking anywhere)
    /// </summary>
    public static void CloseCurrentDatePicker()
    {
        if (_currentlyOpenDatePicker != null)
        {
            _currentlyOpenDatePicker.CloseCalendar();
        }
    }

    internal static void HandleGlobalPointer(System.Numerics.Vector2 position, VisualElement? hitElement)
    {
        _currentlyOpenDatePicker?.HandleGlobalPointerInternal(position, hitElement);
    }

    /// <summary>
    /// IGlobalPointerHandler implementation - instance method
    /// </summary>
    bool IGlobalPointerHandler.HandleGlobalPointer(System.Numerics.Vector2 position, VisualElement? hitElement)
    {
        return HandleGlobalPointerInternal(position, hitElement);
    }

    private bool HandleGlobalPointerInternal(System.Numerics.Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen)
        {
            return false;  // Not consuming event
        }

        if (IsElementWithinDatePicker(hitElement))
        {
            return true;  // Event is for us, consume it
        }

        if (IsPointInsideDatePicker(position) || IsPointInsideCalendar(position))
        {
            return true;  // Event is for us, consume it
        }

        // Click outside - close calendar
        CancelSelection();
        return false;  // Allow other handlers to process
    }

    private bool IsElementWithinDatePicker(VisualElement? element)
    {
        var current = element;
        while (current != null)
        {
            if (current == this || current == _calendarFrame)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private bool IsPointInsideDatePicker(System.Numerics.Vector2 position)
    {
        return position.X >= ComputedX && position.X <= ComputedX + ComputedWidth &&
               position.Y >= ComputedY && position.Y <= ComputedY + ComputedHeight;
    }

    private bool IsPointInsideCalendar(System.Numerics.Vector2 position)
    {
        if (_calendarFrame == null)
        {
            return false;
        }

        return position.X >= _calendarFrame.ComputedX &&
               position.X <= _calendarFrame.ComputedX + _calendarFrame.ComputedWidth &&
               position.Y >= _calendarFrame.ComputedY &&
               position.Y <= _calendarFrame.ComputedY + _calendarFrame.ComputedHeight;
    }

    public DatePicker()
    {
        InitializeTheme();
        Width = 240;
        Height = 44;
        BorderThickness = 1;
        BuildComponents();

        // Add the date button as a child so it's part of the UI tree
        if (_dateButton != null)
        {
            AddChild(_dateButton);
        }
    }

    protected override void OnThemeApplied(Theme theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(Background), (Brush)palette.Surface, value => Background = value);
        SetThemeValue(nameof(HeaderColor), (Brush)palette.Primary, value => HeaderColor = value);
        SetThemeValue(nameof(SelectedDateColor), (Brush)palette.Primary, value => SelectedDateColor = value);
        SetThemeValue(nameof(TodayColor), (Brush)palette.Info, value => TodayColor = value);
        SetThemeValue(nameof(HoverColor), (Brush)palette.SurfaceHover, value => HoverColor = value);
        SetThemeValue(nameof(TextColor), (Brush)palette.OnSurface, value => TextColor = value);
        SetThemeValue(nameof(HeaderTextColor), (Brush)palette.OnPrimary, value => HeaderTextColor = value);
        SetThemeValue(nameof(CalendarBackground), (Brush)palette.Surface, value => CalendarBackground = value);
        SetThemeValue(nameof(CalendarBorderBrush), (Brush)palette.Border, value => CalendarBorderBrush = value);
        SetThemeValue(nameof(MutedTextColor), (Brush)palette.OnDisabled, value => MutedTextColor = value);
        SetThemeValue(nameof(BorderBrush), (Brush)palette.Border, value => BorderBrush = value);

        ApplyInputAppearance();

        if (_isOpen)
            RebuildCalendar();
    }

    protected override void OnBorderBrushChanged()
    {
        base.OnBorderBrushChanged();
        _dateButton?.BorderBrush(BorderBrush);
    }

    protected override void OnBorderThicknessChanged()
    {
        base.OnBorderThicknessChanged();
        _dateButton?.BorderThickness(BorderThickness);
    }

    private void BuildComponents()
    {
        _dateText = new Label
        {
            Text = SelectedDate.ToString(DateFormat),
            Foreground = TextColor,
            FontSize = 15,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _calendarIcon = new ButtonIcon(Icons.Calendar)
            .Variant(ButtonVariant.Ghost)
            .IconSize(18)
            .IconColor(MutedTextColor)
            .BorderRadius(new CornerRadius(6))
            .Size(32);
        _calendarIcon.OnTapped(() => ToggleCalendar());

        _inputContent = new HStack
        {
            Spacing = 12,
            Alignment = Alignment.Center,
            JustifyContent = JustifyContent.SpaceBetween,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _inputContent.AddChild(_dateText);
        _inputContent.AddChild(_calendarIcon);

        _dateButton = new Frame
        {
            Background = Background,
            BorderBrush = BorderBrush,
            BorderThickness = BorderThickness,
            Padding = new Thickness(14, 6, 14, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderRadius = new CornerRadius(FieldCornerRadius)
        };
        _dateButton.Content(_inputContent);
    }

    private void ApplyInputAppearance()
    {
        if (_dateText != null)
            _dateText.Foreground = TextColor;

        if (_calendarIcon != null)
        {
            _calendarIcon.IconColor = MutedTextColor;
        }

        if (_dateButton != null)
        {
            _dateButton.Background = Background;
            _dateButton.BorderBrush = BorderBrush;
        }
    }

    private void UpdateDateText()
    {
        if (_dateText == null) return;

        _dateText.Text = SelectedDate.ToString(DateFormat);
    }

    public void OpenCalendar()
    {
        if (_isOpen) return;

        // Close any previously open datepicker before opening this one
        if (_currentlyOpenDatePicker != null && _currentlyOpenDatePicker != this)
        {
            _currentlyOpenDatePicker.CloseCalendar();
        }

        _isOpen = true;
        _currentlyOpenDatePicker = this;
        if (!_isRebuildingCalendar)
        {
            _originalSelectedDate = SelectedDate;
            _pendingSelectedDate = SelectedDate;
        }
        _commitSelection = false;

        if (!_isRebuildingCalendar)
        {
            _displayMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
        }
        _calendarFrame = BuildCalendar();
        _dialogOverlay = UsesPopupPresentation()
            ? BuildPopupOverlay()
            : BuildDialogOverlay();

        Rayo.Core.OverlayManager.AddOverlay(_dialogOverlay);
        Rayo.Core.OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void CloseCalendar()
    {
        if (!_isOpen || _dialogOverlay == null) return;

        Rayo.Core.OverlayManager.RemoveOverlay(_dialogOverlay);
        _isOpen = false;
        _calendarFrame = null;
        _dialogOverlay = null;

        if (!_commitSelection && !_isRebuildingCalendar && !UsesPopupPresentation())
        {
            SelectedDate = _originalSelectedDate;
        }

        _commitSelection = false;

        // Clear the global reference if this was the currently open datepicker
        if (_currentlyOpenDatePicker == this)
        {
            _currentlyOpenDatePicker = null;
        }

        Rayo.Core.OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);
    }

    private void ConfirmSelection(DateTime selectedDate)
    {
        _commitSelection = true;
        SelectedDate = selectedDate;
        CloseCalendar();

        var confirmedHandler = _dialogConfirmed;
        ClearDialogCallbacks();
        confirmedHandler?.Invoke(selectedDate);
    }

    private void CancelSelection()
    {
        CloseCalendar();

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
    /// Opens the picker as a standalone modal dialog that can be launched from any custom trigger.
    /// </summary>
    public static DatePicker ShowDialog(DateTime initialDate, Action<DateTime> onConfirm, Action? onCancel = null, Action<DatePicker>? configure = null)
    {
        var picker = new DatePicker();
        picker.SelectedDate = initialDate;
        configure?.Invoke(picker);
        picker.DisplayMode = PickerDisplayMode.Dialog;
        picker._dialogConfirmed = onConfirm;
        picker._dialogCanceled = onCancel;
        picker.OpenCalendar();
        return picker;
    }

    private bool UsesPopupPresentation()
    {
        return DisplayMode == PickerDisplayMode.Popup;
    }

    public void ToggleCalendar()
    {
        if (_isOpen)
            CloseCalendar();
        else
            OpenCalendar();
    }

    private Frame BuildCalendar()
    {
        bool isPopup = UsesPopupPresentation();

        // -- Month navigation header ------------------------------------------
        var prevButton = new ButtonIcon(Icons.ChevronLeft)
            .Variant(ButtonVariant.Ghost)
            .IconSize(14)
            .IconColor(HeaderTextColor)
            .BorderRadius(new CornerRadius(4))
            .Padding(new Thickness(0))
            .Size(isPopup ? 28 : 30);
        prevButton.OnTapped(() => PreviousMonth());

        var monthLabel = new Label
        {
            Text = _displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            Foreground = HeaderTextColor,
            FontSize = 16
        };

        var nextButton = new ButtonIcon(Icons.ChevronRight)
            .Variant(ButtonVariant.Ghost)
            .IconSize(14)
            .IconColor(HeaderTextColor)
            .BorderRadius(new CornerRadius(4))
            .Padding(new Thickness(0))
            .Size(isPopup ? 28 : 30);
        nextButton.OnTapped(() => NextMonth());

        var headerContent = new HStack
        {
            Spacing = 8,
            Alignment = Alignment.Center,
            JustifyContent = JustifyContent.SpaceBetween,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        headerContent.AddChild(prevButton);
        headerContent.AddChild(monthLabel);
        headerContent.AddChild(nextButton);

        var monthHeader = new Frame();
        monthHeader.Height(isPopup ? 46 : 56);
        monthHeader.Background(HeaderColor);
        monthHeader.Padding(isPopup
            ? new Thickness(12, 8, 12, 8)
            : new Thickness(16, 12, 16, 12));
        monthHeader.BorderRadius(new CornerRadius(12, 12, 0, 0));
        monthHeader.Content(headerContent);

        // -- Day-of-week header row (S M T W T F S) --------------------------
        var dayHeaders = new HStack
        {
            Spacing = 0,
            Alignment = Alignment.Center,
            Padding = isPopup
                ? new Thickness(12, 6, 12, 2)
                : new Thickness(14, 8, 14, 4)
        };

        string[] dayNames = { "S", "M", "T", "W", "T", "F", "S" };
        foreach (var day in dayNames)
        {
            var dayLabel = new Label { Text = day, Foreground = MutedTextColor, FontSize = 12 };
            var dayFrame = new Frame()
                .Width(36)
                .Height(isPopup ? 22 : 26);
            dayFrame.Content(dayLabel);
            dayHeaders.AddChild(dayFrame);
        }

        // -- Days grid --------------------------------------------------------
        var daysGrid = BuildDaysGrid();

        // -- Selection surface (same dark card as TimePicker) -----------------
        var calendarContent = new VStack { Spacing = 0 };
        calendarContent.AddChild(monthHeader);
        calendarContent.AddChild(dayHeaders);
        calendarContent.AddChild(daysGrid);

        var selectionSurface = new Frame();
        selectionSurface.Background(CalendarBackground);
        selectionSurface.BorderRadius(new CornerRadius(12));
        selectionSurface.BorderThickness(1);
        selectionSurface.BorderBrush(CalendarBorderBrush);
        selectionSurface.Width(isPopup ? 278 : 308);
        selectionSurface.Padding(isPopup
            ? new Thickness(0, 0, 0, 6)
            : new Thickness(0, 0, 0, 8));
        selectionSurface.HorizontalAlignment(HorizontalAlignment.Left);
        selectionSurface.Content(calendarContent);

        // Popup mode uses the calendar surface directly. The field beside it
        // already shows the selected value, so an outer card and title only add
        // unnecessary size and visual duplication.
        if (isPopup)
        {
            return selectionSurface;
        }

        // -- Main content VStack ----------------------------------------------
        var content = new VStack { Spacing = 16 };
        content.HorizontalAlignment(HorizontalAlignment.Left);
        content.AddChild(new Label("Pick a date")
        {
            Foreground = TextColor,
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextHorizontalAlignment = HorizontalAlignment.Center
        });

        HStack? buttons = null;
        if (!UsesPopupPresentation())
        {
            var previewLabel = new Label
            {
                Text = _pendingSelectedDate.ToString(DateFormat),
                Foreground = TextColor,
                FontSize = 20
            };
            var previewFrame = new Frame()
                .Background(HoverColor)
                .BorderRadius(new CornerRadius(12))
                .Padding(new Thickness(16, 12, 16, 12))
                .HorizontalAlignment(HorizontalAlignment.Left)
                .Content(previewLabel);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Variant = ButtonVariant.Secondary,
                BorderThickness = 0,
                BorderRadius = new CornerRadius(6),
                Width = 100,
                Height = 36
            };
            cancelButton.Tapped += (_) => CancelSelection();

            var okButton = new Button
            {
                Text = "OK",
                Variant = ButtonVariant.Primary,
                BorderThickness = 0,
                BorderRadius = new CornerRadius(6),
                Width = 100,
                Height = 36
            };
            okButton.Tapped += (_) => ConfirmSelection(_pendingSelectedDate);

            buttons = new HStack()
                .Spacing(10)
                .JustifyContent(JustifyContent.End)
                .VerticalAlignment(VerticalAlignment.Top)
                .HorizontalAlignment(HorizontalAlignment.Right);
            buttons.AddChild(cancelButton);
            buttons.AddChild(okButton);
            content.AddChild(previewFrame);
        }

        content.AddChild(selectionSurface);
        if (buttons != null)
        {
            content.AddChild(buttons);
        }

        // -- Outer picker frame (same style as TimePicker's outer frame) ------
        var pickerFrame = new Frame()
            .Background(CalendarBackground)
            .BorderBrush(CalendarBorderBrush)
            .BorderThickness(1)
            .BorderRadius(new CornerRadius(14))
            .Padding(new Thickness(16))
            .HorizontalAlignment(HorizontalAlignment.Center);
        pickerFrame.Content(content);

        return pickerFrame;
    }

    private Frame BuildDialogOverlay()
    {
        var overlay = new DialogOverlayFrame()
            .Background(new Color(0, 0, 0, 0.65f))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        _calendarFrame!.HorizontalAlignment = HorizontalAlignment.Center;
        _calendarFrame!.VerticalAlignment = VerticalAlignment.Center;
        overlay.Content(_calendarFrame!);
        return overlay;
    }

    private Frame BuildPopupOverlay()
    {
        var popup = _calendarFrame!;
        popup.HorizontalAlignment = HorizontalAlignment.Left;
        popup.VerticalAlignment = VerticalAlignment.Top;
        return new AnchoredPopup(this, popup);
    }

    private VisualElement BuildDaysGrid()
    {
        var grid = new VStack { Spacing = 0 };
        bool isPopup = UsesPopupPresentation();
        float cellWidth = isPopup ? 36 : 40;
        float cellHeight = isPopup ? 34 : 40;

        DateTime firstDay = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
        int startDayOfWeek = (int)firstDay.DayOfWeek;

        int currentDay = 1;

        int weekCount = isPopup
            ? (startDayOfWeek + daysInMonth + 6) / 7
            : 6;
        for (int row = 0; row < weekCount; row++)
        {
            var weekRow = new HStack
            {
                Spacing = 0,
                Alignment = Alignment.Center,
                Padding = isPopup
                    ? new Thickness(12, 1, 12, 1)
                    : new Thickness(14, 2, 14, 2)
            };

            for (int col = 0; col < 7; col++)
            {
                int cellIndex = row * 7 + col;
                Frame dayFrame;

                if (cellIndex < startDayOfWeek || currentDay > daysInMonth)
                {
                    // Empty / out-of-range cell � transparent placeholder keeps the row height.
                    dayFrame = new Frame()
                        .Width(cellWidth)
                        .Height(cellHeight)
                        .Background(Color.Transparent);
                }
                else
                {
                    DateTime cellDate = new DateTime(_displayMonth.Year, _displayMonth.Month, currentDay);
                    DateTime selectedDate = UsesPopupPresentation()
                        ? SelectedDate
                        : _pendingSelectedDate;
                    bool isSelected = cellDate.Date == selectedDate.Date;
                    bool isToday = cellDate.Date == DateTime.Today;

                    Brush bgColor = isSelected ? SelectedDateColor :
                                   isToday ? TodayColor :
                                   (Brush)Color.Transparent;

                    Brush fgColor = isSelected ? (Brush)RayoThemes.Current.Colors.OnPrimary : TextColor;

                    int day = currentDay; // Capture for closure
                    var button = new Button
                    {
                        Text = currentDay.ToString(),
                        Width = cellWidth,
                        Height = cellHeight,
                        Background = bgColor,
                        TextColor = fgColor,
                        HoverBackground = HoverColor,
                        PressedBackground = HoverColor,
                        BorderThickness = 0,
                        BorderRadius = new CornerRadius(8),
                        Padding = new Thickness(0)
                    };
                    button.Tapped += (args) => SelectDay(day);

                    dayFrame = new Frame()
                        .Width(cellWidth)
                        .Height(cellHeight);
                    dayFrame.Content(button);

                    currentDay++;
                }

                weekRow.AddChild(dayFrame);
            }

            grid.AddChild(weekRow);
        }

        return grid;
    }

    private void SelectDay(int day)
    {
        var selectedDate = new DateTime(_displayMonth.Year, _displayMonth.Month, day);
        if (UsesPopupPresentation())
        {
            ConfirmSelection(selectedDate);
            return;
        }

        _pendingSelectedDate = selectedDate;
        RebuildCalendar();
    }

    private sealed class DialogOverlayFrame : Frame, Rayo.Core.Input.IPointerHandler
    {
    }

    private void PreviousMonth()
    {
        _displayMonth = _displayMonth.AddMonths(-1);
        RebuildCalendar();
    }

    private void NextMonth()
    {
        _displayMonth = _displayMonth.AddMonths(1);
        RebuildCalendar();
    }

    private void RebuildCalendar()
    {
        if (!_isOpen) return;

        _isRebuildingCalendar = true;
        CloseCalendar();
        OpenCalendar();
        _isRebuildingCalendar = false;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 240;
        float measuredHeight = Height > 0 ? Height : 44;

        // Measure the date button child
        if (_dateButton != null)
        {
            _dateButton.MeasureUpdate(measuredWidth, measuredHeight);
        }

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Arrange the date button to fill the entire DatePicker area
        if (_dateButton != null)
        {
            _dateButton.ArrangeUpdate(x, y, width, height);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // The _dateButton is a child and will be rendered automatically by UITree
        // Nothing to render here since all visuals are in _dateButton
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

            // If a child element (e.g. the calendar ButtonIcon) already handled this
            // release via its TapRecognizer, do not toggle the calendar a second time.
            if (e.Handled) return;

            // Handle click - toggle calendar if release is inside bounds
            bool isInsideBounds = e.Position.X >= ComputedX && e.Position.X <= ComputedX + ComputedWidth &&
                                  e.Position.Y >= ComputedY && e.Position.Y <= ComputedY + ComputedHeight;

            if (isInsideBounds)
            {
                // Close any other open datepicker first
                if (_currentlyOpenDatePicker != null && _currentlyOpenDatePicker != this)
                {
                    _currentlyOpenDatePicker.CloseCalendar();
                }

                ToggleCalendar();
            }
        }
    }
}
