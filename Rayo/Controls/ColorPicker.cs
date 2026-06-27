namespace Rayo.Controls;

using Rayo;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Core.Input;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using static Rayo.Core.UIHelpers;
using GradientStop = Rayo.Rendering.Brushes.GradientStop;

/// <summary>
/// Modal color picker that mirrors Flutter's dialog UX so it works on desktop and mobile targets.
/// </summary>
public class ColorPicker : Component, Rayo.Core.Interfaces.IGlobalPointerHandler
{
    private readonly Signal<Color> _colorState;

    private bool _showAlpha = true;
    private bool _isOpen;
    private VisualElement? _activeOverlay;
    private Frame? _dialogCard;
    private Action<Color>? _dialogConfirmed;
    private Action? _dialogCanceled;

    public ColorPicker()
    {
        var initial = RayoThemes.Current.Colors.Primary;
        _colorState = new Signal<Color>(initial);
    }

    /// <summary>
    /// Gets or sets the currently selected color.
    /// </summary>
    public Color SelectedColor
    {
        get => _colorState.Value;
        set => SetSelectedColor(value, true);
    }

    /// <summary>
    /// Enables or disables the alpha slider/hex component.
    /// </summary>
    public bool ShowAlpha
    {
        get => _showAlpha;
        set
        {
            if (_showAlpha == value)
            {
                return;
            }

            _showAlpha = value;
        }
    }

    /// <summary>
    /// Raised when the picker commits a new color.
    /// </summary>
    public event Action<Color>? ColorChanged;

    public override VisualElement Build()
    {
        return new Frame()
            .Width(0)
            .Height(0)
            .Background(Color.Transparent);
    }

    public void OpenPicker()
    {
        ClosePicker();

        _activeOverlay = BuildDialogOverlay(ConfirmSelection, CancelSelection);

        _isOpen = true;
        Rayo.Core.OverlayManager.AddOverlay(_activeOverlay);
        Rayo.Core.OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
    }

    public void ClosePicker()
    {
        if (!_isOpen)
        {
            return;
        }
        if (_activeOverlay != null)
        {
            Rayo.Core.OverlayManager.RemoveOverlay(_activeOverlay);
        }

        _isOpen = false;
        _activeOverlay = null;
        _dialogCard = null;

        Rayo.Core.OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);
    }

    private void ConfirmSelection(Color color)
    {
        SetSelectedColor(color, true);
        ClosePicker();

        var confirmedHandler = _dialogConfirmed;
        ClearDialogCallbacks();
        confirmedHandler?.Invoke(color);
    }

    private void CancelSelection()
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
    /// Opens the picker as a standalone overlay that can be launched from any custom trigger.
    /// </summary>
    public static ColorPicker ShowDialog(Color initialColor, Action<Color> onConfirm, Action? onCancel = null, Action<ColorPicker>? configure = null)
    {
        var picker = new ColorPicker
        {
            SelectedColor = initialColor
        };
        configure?.Invoke(picker);
        picker._dialogConfirmed = onConfirm;
        picker._dialogCanceled = onCancel;
        picker.OpenPicker();
        return picker;
    }

    private VisualElement BuildDialogOverlay(Action<Color> onConfirm, Action onCancel)
    {
        Frame overlay = new Frame();
        overlay.Background(new Color(0, 0, 0, 0.65f));
        overlay.HorizontalAlignment(HorizontalAlignment.Stretch);
        overlay.VerticalAlignment(VerticalAlignment.Stretch);

        Frame card = new Frame();
        card.Background(RayoThemes.Current.Colors.Surface);
        card.BorderBrush = RayoThemes.Current.Colors.Border;
        card.BorderThickness = 1;
        card.BorderRadius(new CornerRadius(14));
        card.Padding(new Thickness(16));
        card.HorizontalAlignment = HorizontalAlignment.Center;
        card.VerticalAlignment = VerticalAlignment.Center;
        card.Width(420);
        _dialogCard = card;

        card.Content(BuildPickerContent(onConfirm, onCancel));
        overlay.Content(card);
        return overlay;
    }

    private VisualElement BuildPickerContent(Action<Color> onConfirm, Action onCancel)
    {
        var currentColor = _colorState.Value;
        var hsv = ToHsva(currentColor);
        bool suppressUpdates = false;

        // -- Header -----------------------------------------------------------
        var header = new Label("Pick a color")
            .FontSize(18)
            .Foreground(RayoThemes.Current.Colors.OnSurface);

        // -- Preview card (mirrors TimePicker/DatePicker preview style) --------
        var colorSwatch = new Frame();
        colorSwatch.Width = 40;
        colorSwatch.Height = 40;
        colorSwatch.BorderRadius(new CornerRadius(10));
        colorSwatch.Background = new SolidColorBrush(currentColor);
        colorSwatch.BorderThickness = 1;
        colorSwatch.BorderBrush = RayoThemes.Current.Colors.Border;

        var previewHex = new Label(FormatHex(currentColor, _showAlpha))
            .FontSize(16)
            .Foreground(RayoThemes.Current.Colors.OnSurface);

        var previewSubLabel = new Label("Selected color")
            .FontSize(12)
            .Foreground(RayoThemes.Current.Colors.OnDisabled);

        var previewInfo = new VStack()
            .Spacing(2)
            .VerticalAlignment(VerticalAlignment.Center)
            .Children(previewHex, previewSubLabel);

        var previewRow = new HStack()
            .Spacing(14)
            .Alignment(Alignment.Center)
            .Children(colorSwatch, previewInfo);

        var previewFrame = new Frame();
        previewFrame.Background(RayoThemes.Current.Colors.SurfaceHover);
        previewFrame.BorderRadius(new CornerRadius(12));
        previewFrame.Padding(new Thickness(16, 12, 16, 12));
        previewFrame.HorizontalAlignment(HorizontalAlignment.Stretch);
        previewFrame.Content(previewRow);

        void UpdatePreview(Color next)
        {
            colorSwatch.Background(next);
            previewHex.Text(FormatHex(next, _showAlpha));
        }

        // -- Sliders -----------------------------------------------------------
        var hueValueLabel = CreateValueLabel($"{hsv.H:0}\u00B0");
        var satValueLabel = CreateValueLabel($"{hsv.S * 100:0}%");
        var valValueLabel = CreateValueLabel($"{hsv.V * 100:0}%");
        Label? alphaValueLabel = _showAlpha ? CreateValueLabel($"{currentColor.A * 100:0}%") : null;

        var hueSlider = CreateSlider(0, 360, hsv.H);
        var satSlider = CreateSlider(0, 100, hsv.S * 100f);
        var valSlider = CreateSlider(0, 100, hsv.V * 100f);
        Slider? alphaSlider = _showAlpha ? CreateSlider(0, 100, currentColor.A * 100f) : null;

        void SynchronizeColor()
        {
            if (suppressUpdates) return;
            var updated = FromHsva(
                hueSlider.Value,
                satSlider.Value / 100f,
                valSlider.Value / 100f,
                _showAlpha ? (alphaSlider?.Value ?? 100f) / 100f : 1f);
            currentColor = updated;
            UpdatePreview(updated);
        }

        hueSlider.OnValueChanged(value => { hueValueLabel.Text($"{value:0}\u00B0"); SynchronizeColor(); });
        satSlider.OnValueChanged(value => { satValueLabel.Text($"{value:0}%"); SynchronizeColor(); });
        valSlider.OnValueChanged(value => { valValueLabel.Text($"{value:0}%"); SynchronizeColor(); });

        if (alphaSlider != null && alphaValueLabel != null)
        {
            alphaSlider.OnValueChanged(value => { alphaValueLabel.Text($"{value:0}%"); SynchronizeColor(); });
        }

        var gradientSampler = new GradientSwatchFrame(color =>
        {
            suppressUpdates = true;
            var hsvValue = ToHsva(color);
            hueSlider.Value = hsvValue.H;
            satSlider.Value = hsvValue.S * 100f;
            valSlider.Value = hsvValue.V * 100f;
            if (alphaSlider != null)
            {
                alphaSlider.Value = hsvValue.A * 100f;
                alphaValueLabel?.Text($"{alphaSlider.Value:0}%");
            }
            hueValueLabel.Text($"{hueSlider.Value:0}\u00B0");
            satValueLabel.Text($"{satSlider.Value:0}%");
            valValueLabel.Text($"{valSlider.Value:0}%");
            suppressUpdates = false;
            currentColor = color;
            UpdatePreview(color);
        });

        // -- Selection surface (dark card wrapping gradient + sliders) ---------
        var interactiveContent = new VStack().Spacing(14);
        interactiveContent.AddChild(new Label("Quick colors")
            .FontSize(12)
            .Foreground(RayoThemes.Current.Colors.OnDisabled));
        interactiveContent.AddChild(gradientSampler);
        interactiveContent.AddChild(BuildSliderSection("Hue", hueSlider, hueValueLabel));
        interactiveContent.AddChild(BuildSliderSection("Saturation", satSlider, satValueLabel));
        interactiveContent.AddChild(BuildSliderSection("Brightness", valSlider, valValueLabel));
        if (_showAlpha && alphaSlider != null && alphaValueLabel != null)
        {
            interactiveContent.AddChild(BuildSliderSection("Alpha", alphaSlider, alphaValueLabel));
        }

        var selectionSurface = new Frame();
        selectionSurface.Background(RayoThemes.Current.Colors.Surface);
        selectionSurface.BorderRadius(new CornerRadius(12));
        selectionSurface.BorderThickness(1);
        selectionSurface.BorderBrush = RayoThemes.Current.Colors.Border;
        selectionSurface.Padding(new Thickness(14));
        selectionSurface.HorizontalAlignment(HorizontalAlignment.Stretch);
        selectionSurface.Content(interactiveContent);

        // -- Buttons -----------------------------------------------------------
        var cancelButton = new Button
        {
            Text = "Cancel",
            Width = 100,
            Height = 36,
            Variant = ButtonVariant.Secondary,
            BorderThickness = 0,
            BorderRadius = new CornerRadius(6)
        };
        cancelButton.OnTapped(onCancel);

        var selectButton = new Button
        {
            Text = "Select",
            Width = 100,
            Height = 36,
            Variant = ButtonVariant.Primary,
            BorderThickness = 0,
            BorderRadius = new CornerRadius(6)
        };
        selectButton.OnTapped(() => onConfirm(currentColor));

        var buttons = new HStack()
            .Spacing(10)
            .JustifyContent(JustifyContent.End)
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Right);
        buttons.AddChild(cancelButton);
        buttons.AddChild(selectButton);

        // -- Root content VStack -----------------------------------------------
        var content = new VStack().Spacing(16);
        content.AddChild(header);
        content.AddChild(previewFrame);
        content.AddChild(selectionSurface);
        content.AddChild(buttons);
        return content;
    }

    public bool HandleGlobalPointer(System.Numerics.Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen || _dialogCard == null)
        {
            return false;
        }

        var card = _dialogCard;
        bool insideCard = position.X >= card.ComputedX &&
                          position.X <= card.ComputedX + card.ComputedWidth &&
                          position.Y >= card.ComputedY &&
                          position.Y <= card.ComputedY + card.ComputedHeight;

        if (insideCard) return true;

        CancelSelection();
        return false;
    }

    private static Slider CreateSlider(float min, float max, float value)
    {
        var slider = new Slider(min, max, value);
        slider.Width = 0;
        slider.HorizontalAlignment = HorizontalAlignment.Stretch;
        slider.Margin = new Thickness(0, 6, 0, 0);
        return slider;
    }

    private static Label CreateValueLabel(string text)
    {
        return new Label(text)
            .FontSize(12)
            .Foreground(RayoThemes.Current.Colors.OnDisabled);
    }

    private static VisualElement BuildSliderSection(string title, Slider slider, Label valueLabel)
    {
        var titleLabel = new Label(title)
            .FontSize(12)
            .Foreground(RayoThemes.Current.Colors.OnDisabled);

        var headerRow = new HStack()
            .JustifyContent(JustifyContent.SpaceBetween);
        headerRow.AddChild(titleLabel);
        headerRow.AddChild(valueLabel);

        var container = new VStack()
            .Spacing(6);
        container.AddChild(headerRow);
        container.AddChild(slider);

        return container;
    }

    private void SetSelectedColor(Color color, bool raiseEvent)
    {
        var normalized = Normalize(color);
        _colorState.Value = normalized;

        if (raiseEvent)
        {
            ColorChanged?.Invoke(normalized);
        }
    }

    private static Color Normalize(Color color)
    {
        return new Color(
            Clamp01(color.R),
            Clamp01(color.G),
            Clamp01(color.B),
            Clamp01(color.A));
    }

    private static float Clamp01(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }

    private static string FormatHex(Color color, bool includeAlpha)
    {
        int r = (int)MathF.Round(Clamp01(color.R) * 255f);
        int g = (int)MathF.Round(Clamp01(color.G) * 255f);
        int b = (int)MathF.Round(Clamp01(color.B) * 255f);
        int a = (int)MathF.Round(Clamp01(color.A) * 255f);

        return includeAlpha
            ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a)
            : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", r, g, b);
    }

    private static (float H, float S, float V, float A) ToHsva(Color color)
    {
        float r = Clamp01(color.R);
        float g = Clamp01(color.G);
        float b = Clamp01(color.B);
        float a = Clamp01(color.A);

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        float h = 0f;
        if (delta > 0.00001f)
        {
            if (max == r)
            {
                h = ((g - b) / delta) % 6f;
            }
            else if (max == g)
            {
                h = ((b - r) / delta) + 2f;
            }
            else
            {
                h = ((r - g) / delta) + 4f;
            }

            h *= 60f;
            if (h < 0)
            {
                h += 360f;
            }
        }

        float s = max <= 0f ? 0f : delta / max;
        float v = max;
        return (h, s, v, a);
    }

    private static Color FromHsva(float h, float s, float v, float a)
    {
        h = float.IsFinite(h) ? h % 360f : 0f;
        if (h < 0f)
        {
            h += 360f;
        }

        s = Clamp01(s);
        v = Clamp01(v);
        a = Clamp01(a);

        float c = v * s;
        float x = c * (1f - Math.Abs(((h / 60f) % 2f) - 1f));
        float m = v - c;

        float r1;
        float g1;
        float b1;

        if (h < 60f)
        {
            (r1, g1, b1) = (c, x, 0f);
        }
        else if (h < 120f)
        {
            (r1, g1, b1) = (x, c, 0f);
        }
        else if (h < 180f)
        {
            (r1, g1, b1) = (0f, c, x);
        }
        else if (h < 240f)
        {
            (r1, g1, b1) = (0f, x, c);
        }
        else if (h < 300f)
        {
            (r1, g1, b1) = (x, 0f, c);
        }
        else
        {
            (r1, g1, b1) = (c, 0f, x);
        }

        return new Color(r1 + m, g1 + m, b1 + m, a);
    }

    public sealed class GradientSwatchFrame : Frame, IPointerHandler, IInputHandler
    {
        private static readonly Color TopLightOverlay = new(255, 255, 255, 200);
        private static readonly Color BottomShadeOverlay = new(0, 0, 0, 160);

        private readonly Action<Color> _onColorPicked;
        private bool _isDragging;
        private Vector2? _markerPosition;

        public GradientSwatchFrame(Action<Color> onColorPicked)
        {
            _onColorPicked = onColorPicked;

            Width = 320;
            Height = 140;
            BorderRadius = new CornerRadius(0);
            BorderThickness = 1;
            BorderBrush = RayoThemes.Current.Colors.Border;

            Background = CreateSpectrumBrush();
        }

        public override void Render(IRenderer renderer)
        {
            renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, Background);
            renderer.DrawRect(
                ComputedX,
                ComputedY,
                ComputedWidth,
                ComputedHeight,
                LinearGradientBrush.Vertical(TopLightOverlay, Color.Transparent));
            renderer.DrawRect(
                ComputedX,
                ComputedY,
                ComputedWidth,
                ComputedHeight,
                LinearGradientBrush.Vertical(Color.Transparent, BottomShadeOverlay));

            if (BorderThickness.Left > 0 && BorderBrush.PrimaryColor.A > 0)
            {
                DrawDottedBorder(renderer);
            }

            if (_markerPosition is { } marker)
            {
                float inset = Math.Max(0f, BorderThickness.Left);
                float sampleWidth = Math.Max(1f, ComputedWidth - inset * 2f);
                float sampleHeight = Math.Max(1f, ComputedHeight - inset * 2f);
                float markerX = ComputedX + inset + marker.X * sampleWidth;
                float markerY = ComputedY + inset + marker.Y * sampleHeight;

                renderer.DrawCircleOutline(markerX, markerY, 7f, 3f, new Color(0, 0, 0, 180));
                renderer.DrawCircleOutline(markerX, markerY, 6f, 2f, Color.White);
            }
        }

        private void DrawDottedBorder(IRenderer renderer)
        {
            var color = BorderBrush.PrimaryColor;
            float radius = Math.Max(1f, BorderThickness.Left);
            float step = radius * 4f;
            float left = ComputedX + radius;
            float right = ComputedX + ComputedWidth - radius;
            float top = ComputedY + radius;
            float bottom = ComputedY + ComputedHeight - radius;

            DrawDottedLine(renderer, left, top, right, top, step, radius, color);
            DrawDottedLine(renderer, right, top, right, bottom, step, radius, color);
            DrawDottedLine(renderer, right, bottom, left, bottom, step, radius, color);
            DrawDottedLine(renderer, left, bottom, left, top, step, radius, color);
        }

        private static void DrawDottedLine(
            IRenderer renderer,
            float startX,
            float startY,
            float endX,
            float endY,
            float step,
            float radius,
            Color color)
        {
            var delta = new Vector2(endX - startX, endY - startY);
            float length = delta.Length();

            if (length <= 0f)
            {
                renderer.DrawCircle(startX, startY, radius, color);
                return;
            }

            var direction = delta / length;
            int dotCount = Math.Max(1, (int)MathF.Floor(length / step));

            for (int i = 0; i <= dotCount; i++)
            {
                float distance = Math.Min(length, i * step);
                float x = startX + direction.X * distance;
                float y = startY + direction.Y * distance;
                renderer.DrawCircle(x, y, radius, color);
            }
        }

        public void OnPointerPressed(PointerEventArgs e)
        {
            _isDragging = true;
            SampleColor(e.LocalPosition);
        }

        public void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDragging)
            {
                SampleColor(e.LocalPosition);
            }
        }

        public void OnPointerReleased(PointerEventArgs e)
        {
            _isDragging = false;
        }

        public void OnPointerCanceled(PointerEventArgs e)
        {
            _isDragging = false;
        }

        // -- IInputHandler � pointer capture for desktop drag ------------------
        // IPointerHandler only receives events while the cursor is over this element.
        // On desktop, moving outside the gradient rectangle during drag stops events.
        // Implementing IInputHandler causes EventManager to set _draggedElement = this
        // on mouse-down, which routes all subsequent MouseDrag events here regardless
        // of where the cursor is � matching the behaviour of mobile touch capture.

        bool IInputHandler.CanHandleInput => true;

        bool IInputHandler.HandleInput(InputEventArgs args)
        {
            switch (args.EventType)
            {
                case InputEventType.MouseDown:
                    _isDragging = true;
                    SampleColor(new Vector2(args.Position.X - ComputedX, args.Position.Y - ComputedY));
                    return true; // capture: sets _draggedElement in EventManager

                case InputEventType.MouseDrag:
                    if (_isDragging)
                        SampleColor(new Vector2(args.Position.X - ComputedX, args.Position.Y - ComputedY));
                    return _isDragging;

                case InputEventType.MouseUp:
                    _isDragging = false;
                    return false;

                default:
                    return false;
            }
        }

        private void SampleColor(Vector2 localPosition)
        {
            float width = Math.Max(1f, ComputedWidth > 0 ? ComputedWidth : Width);
            float height = Math.Max(1f, ComputedHeight > 0 ? ComputedHeight : Height);
            float inset = Math.Max(0f, BorderThickness.Left);
            float sampleWidth = Math.Max(1f, width - inset * 2f);
            float sampleHeight = Math.Max(1f, height - inset * 2f);

            float normalizedX = Math.Clamp((localPosition.X - inset) / sampleWidth, 0f, 1f);
            float normalizedY = Math.Clamp((localPosition.Y - inset) / sampleHeight, 0f, 1f);

            _markerPosition = new Vector2(normalizedX, normalizedY);

            var baseColor = Background.GetColorAt(normalizedX, 0.5f);
            var lightened = BlendOver(baseColor, TopLightOverlay.WithAlpha(TopLightOverlay.A * (1f - normalizedY)));
            var shaded = BlendOver(lightened, BottomShadeOverlay.WithAlpha(BottomShadeOverlay.A * normalizedY));
            _onColorPicked(shaded);
            MarkNeedsPaint();
        }

        private static Color BlendOver(Color destination, Color source)
        {
            float sourceAlpha = Math.Clamp(source.A, 0f, 1f);
            float destinationAlpha = Math.Clamp(destination.A, 0f, 1f);
            float outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);

            if (outputAlpha <= 0f)
            {
                return Color.Transparent;
            }

            return new Color(
                (source.R * sourceAlpha + destination.R * destinationAlpha * (1f - sourceAlpha)) / outputAlpha,
                (source.G * sourceAlpha + destination.G * destinationAlpha * (1f - sourceAlpha)) / outputAlpha,
                (source.B * sourceAlpha + destination.B * destinationAlpha * (1f - sourceAlpha)) / outputAlpha,
                outputAlpha);
        }

        private static LinearGradientBrush CreateSpectrumBrush()
        {
            return new LinearGradientBrush
            {
                StartPoint = new Vector2(0, 0.5f),
                EndPoint = new Vector2(1, 0.5f),
                GradientStops = new List<GradientStop>
                {
                    GradientStop.At(0.0f, new Color(255, 0, 0)),
                    GradientStop.At(0.16f, new Color(255, 154, 0)),
                    GradientStop.At(0.32f, new Color(208, 222, 33)),
                    GradientStop.At(0.48f, new Color(79, 220, 74)),
                    GradientStop.At(0.64f, new Color(63, 218, 216)),
                    GradientStop.At(0.80f, new Color(47, 201, 226)),
                    GradientStop.At(1.0f, new Color(204, 0, 255))
                }
            };
        }
    }

    /// <summary>
    /// Simple clickable Frame for custom ColorPicker launch surfaces.
    /// </summary>
    public sealed class ClickableFrame : Frame, Rayo.Core.Input.IPointerHandler
    {
        private readonly Action _onTap;
        private bool _isPressed;
        private Rendering.Brushes.Brush _normalBackground;
        private Rendering.Brushes.Brush _hoverBackground;

        public ClickableFrame(Action onTap)
        {
            _onTap = onTap;
            _normalBackground = Background;
            _hoverBackground = _normalBackground;
        }

        public new Brush Background
        {
            get => base.Background;
            set
            {
                base.Background = value;
                _normalBackground = value;
            }
        }
        
        public Brush HoverBackground
        {
            get => _hoverBackground;
            set => _hoverBackground = value;
        }

        public void OnPointerEntered(PointerEventArgs e)
        {
            Background = _hoverBackground;
            MarkNeedsPaint();
        }

        public void OnPointerExited(PointerEventArgs e)
        {
            Background = _normalBackground;
            _isPressed = false;
            MarkNeedsPaint();
        }

        public void OnPointerPressed(PointerEventArgs e)
        {
            _isPressed = true;
            MarkNeedsPaint();
        }

        public void OnPointerReleased(PointerEventArgs e)
        {
            if (_isPressed)
            {
                _isPressed = false;
                MarkNeedsPaint();
                _onTap?.Invoke();
            }
        }
    }
}
