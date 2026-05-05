using PaintApp.Controls;
using PaintApp.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace PaintApp.Views;

/// <summary>
/// Toolbar UserControl: file actions, tool selector, brush size selector, and color picker.
/// </summary>
public class ToolbarView : UserControl
{
    private readonly PaintViewState _vm;
    private readonly PaintCanvas    _canvas;

    private ColorPicker _colorPicker = null!;
    private readonly Dictionary<PaintTool, IconButton> _toolButtons = new();
    private readonly Dictionary<float, Button>         _sizeButtons = new();

    public ToolbarView(PaintViewState vm, PaintCanvas canvas)
    {
        _vm     = vm;
        _canvas = canvas;
    }

    public override VisualElement Build()
    {
        _colorPicker = new ColorPicker()
            .OnColorChanged(c => _vm.PrimaryColor.Value = c);
        _colorPicker.SelectedColor = _vm.PrimaryColor.Value;
        _colorPicker.ShowAlpha     = false;
        _colorPicker.DisplayMode   = PickerDisplayMode.Floating;

        // Keep canvas + picker in sync when primary color changes (e.g. from palette).
        _vm.PrimaryColor.Subscribe(c =>
        {
            _canvas.DrawColor          = c;
            _colorPicker.SelectedColor = c;
        });

        _vm.Tool.Subscribe(t =>
        {
            _canvas.Tool       = t;
            _vm.ToolText.Value = t.ToString();
            ApplyToolStyles(t);
        });

        _vm.BrushSize.Subscribe(s =>
        {
            _canvas.BrushSize = s;
            ApplySizeStyles(s);
        });

        return new Frame()
            .Background(new Color(245, 245, 245))
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new HStack()
                    .Spacing(2)
                    .Padding(new Thickness(6, 4))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Children(
                        // File actions
                        MakeActionButton(Icons.NewFile,   "New",  ShowNewCanvasDialog),
                        MakeActionButton(Icons.Save,      "Save", () => { /* save not implemented in this demo */ }),
                        MakeActionButton(Icons.ArrowLeft, "Undo", () => _canvas.Undo()),

                        Separator(),

                        // Drawing tools
                        MakeToolButton(PaintTool.Pencil,    Icons.Edit,          "Pencil"),
                        MakeToolButton(PaintTool.Brush,     Icons.BrushTool,     "Brush"),
                        MakeToolButton(PaintTool.Eraser,    Icons.Eraser,        "Eraser"),
                        MakeToolButton(PaintTool.Line,      Icons.LineTool,      "Line"),
                        MakeToolButton(PaintTool.Rectangle, Icons.RectangleTool, "Rectangle"),
                        MakeToolButton(PaintTool.Ellipse,   Icons.EllipseTool,   "Ellipse"),
                        MakeToolButton(PaintTool.Fill,      Icons.FillBucket,    "Fill"),

                        Separator(),

                        // Brush size selector
                        new Label()
                            .Text("Size")
                            .FontSize(11)
                            .Foreground(new Color(80, 80, 80))
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Margin(new Thickness(4, 0)),
                        MakeSizeButton(1f,  "XS"),
                        MakeSizeButton(3f,  "S"),
                        MakeSizeButton(7f,  "M"),
                        MakeSizeButton(14f, "L"),

                        Separator(),

                        // Color picker
                        new Label()
                            .Text("Color")
                            .FontSize(11)
                            .Foreground(new Color(80, 80, 80))
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Margin(new Thickness(4, 0)),
                        _colorPicker
                    )
            );
    }

    // ── Button factories ──────────────────────────────────────────────────────

    private TooltipHost MakeActionButton(IconData icon, string label, Action onTap)
    {
        var btn = new IconButton()
            .IconData(icon)
            .IconSize(18)
            .IconColor(new Color(60, 60, 60))
            .Background(new Color(245, 245, 245))
            .HoverBackground(new Color(220, 230, 245))
            .PressedBackground(new Color(190, 210, 240))
            .BorderWidth(1)
            .BorderColor(new Color(210, 210, 210))
            .Width(34)
            .Height(34)
            .BorderRadius(new CornerRadius(4))
            .OnTapped(onTap);

        return new TooltipHost(btn, label, TooltipPlacement.Bottom);
    }

    private TooltipHost MakeToolButton(PaintTool tool, IconData icon, string label)
    {
        var btn = new IconButton()
            .IconData(icon)
            .IconSize(18)
            .Width(34)
            .Height(34)
            .BorderRadius(new CornerRadius(4))
            .BorderWidth(1)
            .OnTapped(() => _vm.Tool.Value = tool);

        ApplyToolButtonStyle(btn, tool == _vm.Tool.Value);
        _toolButtons[tool] = btn;

        return new TooltipHost(btn, label, TooltipPlacement.Bottom);
    }

    private TooltipHost MakeSizeButton(float size, string label)
    {
        var btn = new Button()
            .Text(label)
            .FontSize(11)
            .Width(34)
            .Height(34)
            .BorderRadius(new CornerRadius(4))
            .OnTapped(() => _vm.BrushSize.Value = size);

        ApplySizeButtonStyle(btn, size == _vm.BrushSize.Value);
        _sizeButtons[size] = btn;

        return new TooltipHost(btn, $"Brush size: {label}", TooltipPlacement.Bottom);
    }

    private static VisualElement Separator() =>
        new Frame()
            .Width(1)
            .Height(28)
            .Background(new Color(200, 200, 200))
            .VerticalAlignment(VerticalAlignment.Center)
            .Margin(new Thickness(4, 0));

    // ── Style helpers ─────────────────────────────────────────────────────────

    private void ApplyToolStyles(PaintTool activeTool)
    {
        foreach (var (tool, btn) in _toolButtons)
            ApplyToolButtonStyle(btn, tool == activeTool);
    }

    private void ApplyToolButtonStyle(IconButton btn, bool isActive)
    {
        if (isActive)
        {
            btn.Background      = new Color(60, 120, 210);
            btn.HoverBackground = new Color(80, 140, 230);
            btn.PressedBackground = new Color(40, 100, 190);
            btn.IconColor       = Color.White;
            btn.BorderColor     = new Color(40, 100, 190);
        }
        else
        {
            btn.Background      = new Color(245, 245, 245);
            btn.HoverBackground = new Color(220, 230, 245);
            btn.PressedBackground = new Color(190, 210, 240);
            btn.IconColor       = new Color(60, 60, 60);
            btn.BorderColor     = new Color(210, 210, 210);
        }
        btn.MarkNeedsPaint();
    }

    private void ApplySizeStyles(float activeSize)
    {
        foreach (var (size, btn) in _sizeButtons)
            ApplySizeButtonStyle(btn, size == activeSize);
    }

    private void ApplySizeButtonStyle(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.Background = new Color(60, 120, 210);
            btn.TextColor  = Color.White;
        }
        else
        {
            btn.Background = new Color(245, 245, 245);
            btn.TextColor  = new Color(60, 60, 60);
        }
        btn.MarkNeedsPaint();
    }

    // ── New Canvas dialog ─────────────────────────────────────────────────────

    private void ShowNewCanvasDialog()
    {
        VisualElement? overlay = null;

        // Default to the current computed dimensions (or 800×600 before first layout).
        int initW = Math.Max(100, (int)(_canvas.CanvasWidth  > 0 ? _canvas.CanvasWidth  : _canvas.ComputedWidth));
        int initH = Math.Max(100, (int)(_canvas.CanvasHeight > 0 ? _canvas.CanvasHeight : _canvas.ComputedHeight));

        var widthEntry  = new Entry(initW.ToString()) { IsNumericOnly = true, IsDecimalAllowed = false, Width = 90, Height = 36 };
        var heightEntry = new Entry(initH.ToString()) { IsNumericOnly = true, IsDecimalAllowed = false, Width = 90, Height = 36 };

        void Close()  => OverlayManager.RemoveOverlay(overlay!);

        void Create()
        {
            if (int.TryParse(widthEntry.Text,  out int w) &&
                int.TryParse(heightEntry.Text, out int h) &&
                w >= 50 && h >= 50)
            {
                _canvas.ClearCanvas(w, h);
            }
            Close();
        }

        void ApplyPreset(int w, int h)
        {
            widthEntry.Text  = w.ToString();
            heightEntry.Text = h.ToString();
        }

        // ── Content ───────────────────────────────────────────────────────────

        var dimensionsRow = new HStack()
            .Spacing(12)
            .Alignment(Alignment.Center)
            .Children(
                new VStack().Spacing(4).Children(
                    new Label("Width").FontSize(12).Foreground(new Color(170, 170, 170)),
                    widthEntry
                ),
                new Label("×")
                    .FontSize(18)
                    .Foreground(new Color(130, 130, 130))
                    .VerticalAlignment(VerticalAlignment.Bottom)
                    .Margin(new Thickness(0, 0, 0, 8)),
                new VStack().Spacing(4).Children(
                    new Label("Height").FontSize(12).Foreground(new Color(170, 170, 170)),
                    heightEntry
                )
            );

        var presetsRow = new VStack()
            .Spacing(8)
            .Children(
                new Label("Presets").FontSize(11).Foreground(new Color(130, 130, 130)),
                new HStack()
                    .Spacing(6)
                    .Children(
                        MakePresetButton("800 × 600",   () => ApplyPreset(800,  600)),
                        MakePresetButton("1280 × 720",  () => ApplyPreset(1280, 720)),
                        MakePresetButton("1920 × 1080", () => ApplyPreset(1920, 1080))
                    )
            );

        var content = new VStack().Spacing(20).Children(dimensionsRow, presetsRow);

        // ── Buttons ───────────────────────────────────────────────────────────

        var cancelButton = new Button()
            .Text("Cancel")
            .Width(100).Height(36)
            .Background(new Color(60, 60, 65))
            .HoverBackground(new Color(75, 75, 82))
            .BorderRadius(new CornerRadius(4))
            .BorderWidth(0)
            .OnTapped(Close);

        var createButton = new Button()
            .Text("Create")
            .Width(100).Height(36)
            .Background(new Color(0, 120, 215))
            .HoverBackground(new Color(0, 140, 235))
            .PressedBackground(new Color(0, 100, 195))
            .BorderRadius(new CornerRadius(4))
            .BorderWidth(0)
            .OnTapped(Create);

        // ── Dialog shell — matches Dialog.cs visual style ─────────────────────

        var dialogBox = new Frame();
        dialogBox.Width(420);
        dialogBox.Background = new Color(45, 45, 48);
        dialogBox.BorderRadius(8);
        dialogBox.BorderWidth = 1;
        dialogBox.HorizontalAlignment = HorizontalAlignment.Center;
        dialogBox.VerticalAlignment   = VerticalAlignment.Center;

        dialogBox.Content(new VStack().Spacing(0).Children(
            // Title bar
            new Frame()
                .Background(new Color(40, 40, 42))
                .Padding(new Thickness(24, 20, 24, 20))
                .BorderRadius(new CornerRadius(8, 8, 0, 0))
                .Content(new Label("New Canvas").FontSize(16).Foreground(Color.White)),

            // Body
            new Frame()
                .Padding(new Thickness(24, 20, 24, 20))
                .Content(content),

            // Button strip
            new Frame()
                .Background(new Color(40, 40, 42))
                .Padding(new Thickness(24, 14, 24, 14))
                .BorderRadius(new CornerRadius(0, 0, 8, 8))
                .Content(
                    new HStack()
                        .Spacing(10)
                        .JustifyContent(JustifyContent.End)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .Children(cancelButton, createButton)
                )
        ));

        overlay = new Frame()
            .Background(new Color(0, 0, 0, 150))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(dialogBox);

        OverlayManager.AddOverlay(overlay);
    }

    private static Button MakePresetButton(string label, Action onTap) =>
        new Button()
            .Text(label)
            .FontSize(11)
            .Height(28)
            .Background(new Color(55, 55, 62))
            .HoverBackground(new Color(70, 70, 80))
            .PressedBackground(new Color(45, 45, 52))
            .TextColor(new Color(200, 200, 205))
            .BorderRadius(new CornerRadius(4))
            .BorderWidth(0)
            .OnTapped(onTap);
}
