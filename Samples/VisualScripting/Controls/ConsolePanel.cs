using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace VisualScripting.Controls;

/// <summary>
/// Horizontal panel docked at the bottom of the editor.
/// Left half shows the generated Lua source; right half shows execution output.
/// Contains Run and Clear buttons in a header bar.
/// </summary>
public class ConsolePanel : CompositeView<ConsolePanel>
{
    public const float PanelHeight  = 210f;
    private const float HeaderHeight = 32f;
    private const float LineHeight   = 15f;
    private const float FontSize     = 10.5f;
    private const float TextPadding   = 8f;

    private static readonly Color ColDim    = new Color(90,  90, 100);
    private static readonly Color ColCode   = new Color(130, 200, 130);
    private static readonly Color ColOutput = new Color(220, 220, 220);
    private static readonly Color ColError  = new Color(255,  90,  90);
    private static readonly Color ColMeta   = new Color(100, 160, 220);

    private readonly VisualScriptingViewModel _viewModel;

    public ConsolePanel(VisualScriptingViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        Height = PanelHeight;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        // Subscribe to console line changes
        _viewModel.ConsoleLines.Subscribe(() => MarkNeedsPaint());

        BuildHeader();
    }

    // -------------------------------------------------------------------------
    // Header (Run + Clear buttons)
    // -------------------------------------------------------------------------

    private void BuildHeader()
    {
        var runBtn = new Button();
        runBtn.Text   = "▶  Ejecutar";
        runBtn.Width  = 100;
        runBtn.Height = 24;
        runBtn.Tapped += _ => _viewModel.ExecuteGraph();

        var clearBtn = new Button();
        clearBtn.Text   = "Limpiar";
        clearBtn.Width  = 70;
        clearBtn.Height = 24;
        clearBtn.Tapped += _ => _viewModel.ClearConsole();

        var header = new HStack();
        header.Height  = HeaderHeight;
        header.Spacing = 8f;
        header.Padding = new Rayo.Thickness(8f, 4f);
        header.VerticalAlignment   = VerticalAlignment.Top;
        header.HorizontalAlignment = HorizontalAlignment.Left;
        header.AddChild(runBtn);
        header.AddChild(clearBtn);

        AddChild(header);
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------

    public override void Measure(float availableWidth, float availableHeight)
    {
        foreach (var child in Children)
            child.Measure(availableWidth, HeaderHeight);

        DesiredWidth  = availableWidth;
        DesiredHeight = PanelHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, PanelHeight);
        foreach (var child in Children)
            child.Arrange(x, y, width, HeaderHeight);
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX, y = ComputedY, w = ComputedWidth, h = ComputedHeight;

        // Panel background
        renderer.DrawRect(x, y, w, h, new Color(16, 16, 20));

        // Top border
        renderer.DrawRect(x, y, w, 1f, new Color(55, 56, 65));

        // Header background
        renderer.DrawRect(x, y + 1f, w, HeaderHeight - 1f, new Color(22, 22, 28));

        // Header label
        var labelSize = renderer.MeasureText("Console", 11f);
        renderer.DrawText("Console",
            x + w - labelSize.X - TextPadding,
            y + (HeaderHeight - labelSize.Y) / 2f,
            ColDim, 11f);

        // Vertical divider between code (left) and output (right)
        float divX   = x + w * 0.5f;
        float bodyY  = y + HeaderHeight;
        float bodyH  = h - HeaderHeight;
        renderer.DrawRect(divX, bodyY, 1f, bodyH, new Color(40, 40, 50));

        // Section labels
        renderer.DrawText("Lua generado", x + TextPadding, bodyY + 3f, ColDim, 9.5f);
        renderer.DrawText("Salida",       divX + TextPadding, bodyY + 3f, ColDim, 9.5f);

        float sectionLabelH = 14f;
        float textStartY    = bodyY + sectionLabelH + 2f;
        float availH        = bodyH - sectionLabelH - 4f;
        int   maxLines      = (int)(availH / LineHeight);

        // Get lines from ViewModel and separate code from output
        var allLines = _viewModel.ConsoleLines.Value;
        var codeLines = allLines.Where(l => l.Type == ConsoleLineType.Code).ToList();
        var outputLines = allLines.Where(l => l.Type != ConsoleLineType.Code).ToList();

        DrawLines(renderer, codeLines,   x + TextPadding,    textStartY, w / 2f - TextPadding * 2f, maxLines);
        DrawLines(renderer, outputLines, divX + TextPadding, textStartY, w / 2f - TextPadding * 2f, maxLines);
    }

    private static void DrawLines(
        IRenderer renderer,
        List<ConsoleLineModel> lines,
        float x, float startY, float maxWidth, int maxVisible)
    {
        int startIdx = Math.Max(0, lines.Count - maxVisible);
        for (int i = startIdx; i < lines.Count; i++)
        {
            float lineY = startY + (i - startIdx) * LineHeight;
            var line = lines[i];

            // Determine color based on type
            Color color = line.CustomColor ?? line.Type switch
            {
                ConsoleLineType.Code => ColCode,
                ConsoleLineType.Error => ColError,
                ConsoleLineType.Meta => ColMeta,
                ConsoleLineType.Output => ColOutput,
                _ => ColOutput
            };

            // Truncate long lines (simple pixel-width guard)
            string text = TruncateText(line.Text, maxWidth);
            renderer.DrawText(text, x, lineY, color, FontSize);
        }
    }

    private static string TruncateText(string text, float maxWidth)
    {
        // Rough char-width estimate: ~6.3px per char at FontSize 10.5
        const float charWidth = 6.3f;
        int maxChars = (int)(maxWidth / charWidth);
        if (text.Length <= maxChars) return text;
        return text[..(maxChars - 1)] + "…";
    }
}
