using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Graphics.VectorGraphics;
using System.Numerics;
using IRenderer = Rayo.Rendering.IRenderer;

namespace PaintApp.Controls;

/// <summary>
/// Custom drawing surface for the PaintApp.
///
/// Each pointer press→drag→release produces one "stroke" (a List of DrawCommands).
/// All strokes are replayed every frame, preserving correct paint ordering.
/// Undo removes the most recent stroke and disposes any GPU textures it held.
///
/// Flood fill works entirely on the CPU:
///   1. Rasterise all existing strokes into a byte[] pixel buffer.
///   2. Run a BFS flood-fill from the click position.
///   3. Store the result as a TextureFillCmd whose transparent background lets
///      previous strokes show through when drawn with DrawTexture.
/// </summary>
public class PaintCanvas : View<PaintCanvas>, IPointerHandler
{
    private readonly List<List<DrawCommand>> _strokes = new();
    private List<DrawCommand>? _currentStroke;
    private DrawCommand?       _preview;        // shape preview while dragging
    private bool               _isPointerDown;
    private Vector2            _pressPos;
    private Vector2            _lastPos;

    // ── Public properties ─────────────────────────────────────────────────────

    public PaintTool Tool             { get; set; } = PaintTool.Pencil;
    public Color     DrawColor        { get; set; } = Color.Black;
    public Color     CanvasBackground { get; set; } = Color.White;
    public float     BrushSize        { get; set; } = 3f;

    private float _canvasWidth  = 0;
    private float _canvasHeight = 0;
    // True once the canvas has been frozen at the first real layout size.
    private bool _autoSized = false;

    // Pan state (right-button drag)
    private float   _panOffsetX    = 0f;
    private float   _panOffsetY    = 0f;
    private bool    _isPanDragging = false;
    private Vector2 _panLastPos;

    /// <summary>
    /// Fixed canvas width in pixels.
    /// When 0 the canvas stretches to fill all available space (default).
    /// Set via <see cref="ClearCanvas(float,float)"/>.
    /// </summary>
    public float CanvasWidth  => _canvasWidth;

    /// <summary>
    /// Fixed canvas height in pixels.
    /// When 0 the canvas stretches to fill all available space (default).
    /// Set via <see cref="ClearCanvas(float,float)"/>.
    /// </summary>
    public float CanvasHeight => _canvasHeight;

    /// <summary>Invoked on every pointer move. Position is (-1,-1) when outside.</summary>
    public Action<Vector2>? OnPositionChanged;

    public PaintCanvas OnPositionChangedHandler(Action<Vector2> handler)
    {
        OnPositionChanged = handler;
        return this;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    /// <summary>Width of the actual drawing surface (may be smaller than the element).</summary>
    private float EffectiveCanvasW => _canvasWidth  > 0 ? _canvasWidth  : ComputedWidth;
    /// <summary>Height of the actual drawing surface.</summary>
    private float EffectiveCanvasH => _canvasHeight > 0 ? _canvasHeight : ComputedHeight;

    /// <summary>Screen X of the drawing surface's top-left corner (accounts for centering + pan).</summary>
    private float ContentOriginX => ComputedX + (ComputedWidth  - EffectiveCanvasW) / 2f - _panOffsetX;
    /// <summary>Screen Y of the drawing surface's top-left corner.</summary>
    private float ContentOriginY => ComputedY + (ComputedHeight - EffectiveCanvasH) / 2f - _panOffsetY;

    /// <summary>Convert a screen position to canvas-local coordinates (0..canvasWidth).</summary>
    private Vector2 ToCanvasLocal(Vector2 screen)
        => new(screen.X - ContentOriginX, screen.Y - ContentOriginY);

    public PaintCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment   = VerticalAlignment.Stretch;
    }

    // ── Public operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Clears all strokes, disposes GPU textures, and optionally changes the canvas size.
    /// When <paramref name="canvasWidth"/> and <paramref name="canvasHeight"/> are both &gt; 0
    /// the canvas uses those fixed dimensions (centered in its parent).
    /// When both are 0 (default) the canvas returns to stretch-to-fill mode.
    /// </summary>
    public void ClearCanvas(float canvasWidth = 0, float canvasHeight = 0)
    {
        foreach (var stroke in _strokes)
            foreach (var cmd in stroke)
                if (cmd is TextureFillCmd tfc) tfc.Texture?.Dispose();

        _strokes.Clear();
        _currentStroke = null;
        _preview       = null;

        _canvasWidth  = canvasWidth;
        _canvasHeight = canvasHeight;
        _panOffsetX   = 0f;
        _panOffsetY   = 0f;

        if (canvasWidth == 0 && canvasHeight == 0)
            _autoSized = false; // allow re-capture on next layout

        MarkNeedsLayout();
    }

    /// <summary>Removes the most recently committed stroke.</summary>
    public void Undo()
    {
        if (_strokes.Count == 0) return;

        var last = _strokes[^1];
        foreach (var cmd in last)
            if (cmd is TextureFillCmd tfc) tfc.Texture?.Dispose();

        _strokes.RemoveAt(_strokes.Count - 1);
        MarkNeedsPaint();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (_canvasWidth > 0 && _canvasHeight > 0)
        {
            // Fixed-size mode: use the explicitly requested dimensions.
            DesiredWidth  = _canvasWidth;
            DesiredHeight = _canvasHeight;
        }
        else
        {
            // Stretch mode: fill available space (default behaviour).
            DesiredWidth = HorizontalAlignment == HorizontalAlignment.Stretch
                ? (availableWidth  > 0 && !float.IsInfinity(availableWidth)  ? availableWidth  : 800f)
                : Width;
            DesiredHeight = VerticalAlignment == VerticalAlignment.Stretch
                ? (availableHeight > 0 && !float.IsInfinity(availableHeight) ? availableHeight : 600f)
                : Height;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        // ── Auto-size on first real layout ────────────────────────────────────
        // When no explicit size is set and this is the first valid layout pass,
        // freeze the canvas at the current viewport dimensions so the user gets a
        // canvas that exactly matches the available drawing area.
        // First real layout: freeze the canvas size slightly smaller than the
        // container so the gray surround is visible as a document boundary.
        if (!_autoSized && _canvasWidth == 0
            && width > 0 && height > 0
            && !float.IsInfinity(width) && !float.IsInfinity(height))
        {
            _autoSized    = true;
            const float Pad = 20f;
            _canvasWidth  = MathF.Max(200f, MathF.Floor(width)  - Pad * 2f);
            _canvasHeight = MathF.Max(150f, MathF.Floor(height) - Pad * 2f);
        }

        // The element always fills its parent; centering and pan are applied in Render.
        base.Arrange(x, y, width, height);
    }

    // ── Render ────────────────────────────────────────────────────────────────

    public override void Render(IRenderer renderer)
    {
        // Clip everything to the element bounds (the gray area).
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);

        float ox = ContentOriginX, oy = ContentOriginY;
        float cw = EffectiveCanvasW,  ch = EffectiveCanvasH;

        // White drawing surface, centred + panned within the element.
        renderer.DrawRect(ox, oy, cw, ch, CanvasBackground);

        // Tighten the clip to the canvas surface so strokes cannot bleed into the gray surround.
        renderer.PushScissor(ox, oy, cw, ch);

        foreach (var stroke in _strokes)
            foreach (var cmd in stroke)
                ExecuteCommand(renderer, ox, oy, cmd);

        if (_currentStroke != null)
            foreach (var cmd in _currentStroke)
                ExecuteCommand(renderer, ox, oy, cmd);

        if (_preview != null && _isPointerDown)
            ExecuteCommand(renderer, ox, oy, _preview);

        renderer.PopScissor(); // canvas scissor
        renderer.PopScissor(); // element scissor
    }

    /// <summary>
    /// Renders a single draw command.
    /// <paramref name="ox"/> and <paramref name="oy"/> are the screen-space origin of the
    /// drawing surface (ContentOriginX/Y), which converts canvas-local coords to screen coords.
    /// </summary>
    private void ExecuteCommand(IRenderer renderer, float ox, float oy, DrawCommand cmd)
    {
        switch (cmd)
        {
            case LineCmd lc:
                renderer.DrawLine(lc.X1 + ox, lc.Y1 + oy, lc.X2 + ox, lc.Y2 + oy, lc.Size, lc.C);
                break;

            case CircleCmd cc:
                renderer.DrawCircle(cc.X + ox, cc.Y + oy, cc.R, cc.C);
                break;

            case RectCmd rc:
                if (rc.W < 0.5f || rc.H < 0.5f) break;
                if (rc.Filled)
                    renderer.DrawRect(rc.X + ox, rc.Y + oy, rc.W, rc.H, rc.C);
                else
                    renderer.DrawRectOutline(rc.X + ox, rc.Y + oy, rc.W, rc.H, rc.Size, rc.C);
                break;

            case EllipseCmd ec:
                if (ec.Rx < 0.5f || ec.Ry < 0.5f) break;
                if (ec.Filled)
                    renderer.DrawEllipse(ec.Cx + ox, ec.Cy + oy, ec.Rx, ec.Ry, ec.C);
                else
                    renderer.DrawPathStroke(
                        VectorPath.Ellipse(ec.Cx + ox, ec.Cy + oy, ec.Rx, ec.Ry), ec.C, ec.Size);
                break;

            case TextureFillCmd tfc:
                tfc.Texture ??= renderer.CreateTextureFromPixels(tfc.Pixels, tfc.W, tfc.H);
                // CanvasX/CanvasY are canvas-local (0,0); add content origin for screen coords.
                renderer.DrawTexture(tfc.Texture, tfc.CanvasX + ox, tfc.CanvasY + oy, tfc.CanvasW, tfc.CanvasH);
                break;
        }
    }

    // ── IPointerHandler ───────────────────────────────────────────────────────

    void IPointerHandler.OnPointerPressed(PointerEventArgs e)
    {
        // Right button (2) — start panning the viewport
        if (e.Button == 2)
        {
            _isPanDragging = true;
            _panLastPos    = e.Position;
            return;
        }

        if (e.Button != 0) return;

        var local = ToCanvasLocal(e.Position);

        // Ignore presses that land outside the canvas surface.
        if (local.X < 0 || local.X >= EffectiveCanvasW || local.Y < 0 || local.Y >= EffectiveCanvasH)
            return;

        _isPointerDown = true;
        _pressPos      = local;
        _lastPos       = local;
        _currentStroke = new List<DrawCommand>();

        switch (Tool)
        {
            case PaintTool.Pencil:
                _currentStroke.Add(new CircleCmd(local.X, local.Y, BrushSize / 2f, DrawColor));
                break;
            case PaintTool.Brush:
                _currentStroke.Add(new CircleCmd(local.X, local.Y, BrushSize * 1.5f, DrawColor));
                break;
            case PaintTool.Eraser:
                _currentStroke.Add(new CircleCmd(local.X, local.Y, BrushSize * 2f, CanvasBackground));
                break;
            case PaintTool.Fill:
                PerformFloodFill(e.Position);
                break;
        }

        MarkNeedsPaint();
        OnPositionChanged?.Invoke(e.Position);
    }

    void IPointerHandler.OnPointerMoved(PointerEventArgs e)
    {
        OnPositionChanged?.Invoke(e.Position);

        // Right-button pan: shift the content origin so the drawing surface "follows" the cursor.
        if (_isPanDragging)
        {
            var delta = e.Position - _panLastPos;
            _panLastPos = e.Position;
            // Dragging right (delta.X > 0) → content moves right → panOffsetX decreases.
            _panOffsetX -= delta.X;
            _panOffsetY -= delta.Y;
            // Clamp: keep at least 100 px of the drawing surface visible.
            const float Margin = 100f;
            float halfW = MathF.Max(0f, (ComputedWidth  + EffectiveCanvasW) / 2f - Margin);
            float halfH = MathF.Max(0f, (ComputedHeight + EffectiveCanvasH) / 2f - Margin);
            _panOffsetX = Math.Clamp(_panOffsetX, -halfW, halfW);
            _panOffsetY = Math.Clamp(_panOffsetY, -halfH, halfH);
            MarkNeedsPaint();
            return;
        }

        if (!_isPointerDown) return;

        var cur = ToCanvasLocal(e.Position);

        // For freehand tools clamp to the canvas surface so dragging outside the white area
        // does not store out-of-bounds coordinates (shape tools let the scissor do the clipping).
        float canvasW = EffectiveCanvasW, canvasH = EffectiveCanvasH;
        bool isFreehand = Tool is PaintTool.Pencil or PaintTool.Brush or PaintTool.Eraser;
        if (isFreehand)
            cur = new Vector2(
                Math.Clamp(cur.X, 0f, canvasW - 1f),
                Math.Clamp(cur.Y, 0f, canvasH - 1f));

        // All coordinates in canvas-local space (0..canvasWidth).
        switch (Tool)
        {
            case PaintTool.Pencil:
                _currentStroke!.Add(new LineCmd(_lastPos.X, _lastPos.Y, cur.X, cur.Y, DrawColor, BrushSize));
                _currentStroke.Add(new CircleCmd(cur.X, cur.Y, BrushSize / 2f, DrawColor));
                break;

            case PaintTool.Brush:
            {
                float r = BrushSize * 1.5f;
                _currentStroke!.Add(new LineCmd(_lastPos.X, _lastPos.Y, cur.X, cur.Y, DrawColor, r * 2f));
                _currentStroke.Add(new CircleCmd(cur.X, cur.Y, r, DrawColor));
                break;
            }

            case PaintTool.Eraser:
            {
                float r = BrushSize * 2f;
                _currentStroke!.Add(new LineCmd(_lastPos.X, _lastPos.Y, cur.X, cur.Y, CanvasBackground, r * 2f));
                _currentStroke.Add(new CircleCmd(cur.X, cur.Y, r, CanvasBackground));
                break;
            }

            case PaintTool.Line:
                _preview = new LineCmd(_pressPos.X, _pressPos.Y, cur.X, cur.Y, DrawColor, BrushSize);
                break;

            case PaintTool.Rectangle:
            {
                float x = MathF.Min(_pressPos.X, cur.X), y = MathF.Min(_pressPos.Y, cur.Y);
                float w = MathF.Abs(cur.X - _pressPos.X), h = MathF.Abs(cur.Y - _pressPos.Y);
                _preview = new RectCmd(x, y, w, h, DrawColor, BrushSize, false);
                break;
            }

            case PaintTool.Ellipse:
            {
                float cx = (_pressPos.X + cur.X) / 2f, cy = (_pressPos.Y + cur.Y) / 2f;
                float rx = MathF.Abs(cur.X - _pressPos.X) / 2f, ry = MathF.Abs(cur.Y - _pressPos.Y) / 2f;
                _preview = new EllipseCmd(cx, cy, rx, ry, DrawColor, BrushSize, false);
                break;
            }
        }

        _lastPos = cur;
        MarkNeedsPaint();
    }

    void IPointerHandler.OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button == 2)
        {
            _isPanDragging = false;
            return;
        }

        if (!_isPointerDown) return;
        _isPointerDown = false;

        if (_preview != null)
        {
            _currentStroke?.Add(_preview);
            _preview = null;
        }

        CommitCurrentStroke();
        MarkNeedsPaint();
    }

    void IPointerHandler.OnPointerExited(PointerEventArgs e)
        => OnPositionChanged?.Invoke(new Vector2(-1, -1));

    private void CommitCurrentStroke()
    {
        if (_currentStroke != null && _currentStroke.Count > 0)
            _strokes.Add(_currentStroke);
        _currentStroke = null;
    }

    // ── Flood fill (CPU) ──────────────────────────────────────────────────────

    private void PerformFloodFill(Vector2 clickScreenPos)
    {
        // Use the actual drawing surface size, not the element size.
        int bw = (int)EffectiveCanvasW;
        int bh = (int)EffectiveCanvasH;
        if (bw <= 0 || bh <= 0) return;

        // Convert click position to canvas-local coordinates (same space as draw commands).
        int lx = (int)(clickScreenPos.X - ContentOriginX);
        int ly = (int)(clickScreenPos.Y - ContentOriginY);
        if (lx < 0 || lx >= bw || ly < 0 || ly >= bh) return;

        // Build a CPU image of all committed strokes so far.
        byte[] canvas = BuildCpuCanvas(bw, bh);

        // Run BFS flood fill; result is transparent except for filled pixels.
        byte[]? result = FloodFillBfs(canvas, bw, bh, lx, ly, DrawColor);
        if (result == null) return;

        // Expand the fill 1 pixel into adjacent stroke pixels so the fill texture
        // overlaps the GPU anti-aliased inner edge, eliminating white-fringe artifacts.
        byte bgR = (byte)(CanvasBackground.R * 255 + 0.5f);
        byte bgG = (byte)(CanvasBackground.G * 255 + 0.5f);
        byte bgB = (byte)(CanvasBackground.B * 255 + 0.5f);
        byte fr  = (byte)(DrawColor.R * 255 + 0.5f);
        byte fg  = (byte)(DrawColor.G * 255 + 0.5f);
        byte fb  = (byte)(DrawColor.B * 255 + 0.5f);
        DilateFillIntoStroke(result, canvas, bw, bh, bgR, bgG, bgB, fr, fg, fb);

        // Store at canvas-local origin (0,0); ExecuteCommand adds ContentOriginX/Y at render time.
        var fillCmd = new TextureFillCmd(result, bw, bh, 0, 0, bw, bh);
        _currentStroke!.Add(fillCmd);
        CommitCurrentStroke(); // commit immediately
    }

    /// <summary>
    /// Rasterises all committed strokes to a CPU RGBA buffer initialised with the canvas background.
    /// </summary>
    private byte[] BuildCpuCanvas(int bw, int bh)
    {
        byte[] buf = new byte[bw * bh * 4];

        // Fill with canvas background color.
        byte br = (byte)(CanvasBackground.R * 255 + 0.5f);
        byte bg = (byte)(CanvasBackground.G * 255 + 0.5f);
        byte bb = (byte)(CanvasBackground.B * 255 + 0.5f);
        for (int i = 0; i < bw * bh; i++)
        {
            buf[i * 4]     = br;
            buf[i * 4 + 1] = bg;
            buf[i * 4 + 2] = bb;
            buf[i * 4 + 3] = 255;
        }

        // Draw commands store canvas-local coordinates starting at 0, so no offset needed.
        foreach (var stroke in _strokes)
            foreach (var cmd in stroke)
                CpuRasterizer.Rasterize(buf, bw, bh, 0f, 0f, cmd);

        return buf;
    }

    // ── BFS flood fill ────────────────────────────────────────────────────────

    /// <summary>
    /// BFS from (sx, sy). Returns a byte[] where flooded pixels are <paramref name="fillColor"/>
    /// (opaque) and all others are transparent. Returns null if the target is already the fill color.
    /// </summary>
    private static byte[]? FloodFillBfs(byte[] canvas, int bw, int bh, int sx, int sy, Color fillColor)
    {
        int si = (sy * bw + sx) * 4;
        byte tr = canvas[si], tg = canvas[si + 1], tb = canvas[si + 2], ta = canvas[si + 3];

        byte fr = (byte)(fillColor.R * 255 + 0.5f);
        byte fg = (byte)(fillColor.G * 255 + 0.5f);
        byte fb = (byte)(fillColor.B * 255 + 0.5f);

        // Nothing to do if the clicked pixel is already the fill color.
        if (tr == fr && tg == fg && tb == fb) return null;

        byte[] result  = new byte[bw * bh * 4]; // all transparent
        bool[] visited = new bool[bw * bh];

        var queue = new Queue<int>(Math.Min(bw * bh, 65536));
        int startIdx = sy * bw + sx;
        visited[startIdx] = true;
        queue.Enqueue(startIdx);

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            int x   = idx % bw;
            int y   = idx / bw;

            // Paint this pixel in the result buffer.
            int ri = idx * 4;
            result[ri]     = fr;
            result[ri + 1] = fg;
            result[ri + 2] = fb;
            result[ri + 3] = 255;

            // Enqueue 4-connected neighbours that match the target color.
            if (x > 0)      TryEnqueue(canvas, bw, bh, x - 1, y, tr, tg, tb, ta, visited, queue);
            if (x < bw - 1) TryEnqueue(canvas, bw, bh, x + 1, y, tr, tg, tb, ta, visited, queue);
            if (y > 0)      TryEnqueue(canvas, bw, bh, x, y - 1, tr, tg, tb, ta, visited, queue);
            if (y < bh - 1) TryEnqueue(canvas, bw, bh, x, y + 1, tr, tg, tb, ta, visited, queue);
        }

        return result;
    }

    private static void TryEnqueue(byte[] canvas, int bw, int bh,
                                    int x, int y,
                                    byte tr, byte tg, byte tb, byte ta,
                                    bool[] visited, Queue<int> queue)
    {
        int i = y * bw + x;
        if (visited[i]) return;
        int pi = i * 4;
        if (canvas[pi] == tr && canvas[pi + 1] == tg && canvas[pi + 2] == tb && canvas[pi + 3] == ta)
        {
            visited[i] = true;
            queue.Enqueue(i);
        }
    }

    // ── Dilation ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Expands the fill result by 1 pixel into adjacent stroke pixels (CPU-canvas pixels that
    /// differ from the canvas background). This closes the anti-aliased fringe left at the inner
    /// edge of GPU-rendered strokes, preventing white sawtooth artifacts along shape boundaries.
    ///
    /// Algorithm: two-pass so dilation cannot cascade — first collect candidates, then apply.
    /// Only non-background pixels adjacent to a filled pixel are dilated, so the fill never
    /// bleeds outside the stroke's outer edge.
    /// </summary>
    private static void DilateFillIntoStroke(
        byte[] result, byte[] canvas, int bw, int bh,
        byte bgR, byte bgG, byte bgB,
        byte fr, byte fg, byte fb)
    {
        bool[] toFill = new bool[bw * bh];

        for (int y = 0; y < bh; y++)
        {
            for (int x = 0; x < bw; x++)
            {
                int i = (y * bw + x) * 4;
                if (result[i + 3] > 0) continue;   // already filled

                // Only expand into stroke pixels (not background).
                if (canvas[i] == bgR && canvas[i + 1] == bgG && canvas[i + 2] == bgB) continue;

                // Check 4-connected neighbours for an existing fill pixel.
                bool adj =
                    (x > 0      && result[i - 4 + 3]                  > 0) ||
                    (x < bw - 1 && result[i + 4 + 3]                  > 0) ||
                    (y > 0      && result[((y - 1) * bw + x) * 4 + 3] > 0) ||
                    (y < bh - 1 && result[((y + 1) * bw + x) * 4 + 3] > 0);

                if (adj) toFill[y * bw + x] = true;
            }
        }

        for (int i = 0; i < bw * bh; i++)
        {
            if (!toFill[i]) continue;
            int ri = i * 4;
            result[ri]     = fr;
            result[ri + 1] = fg;
            result[ri + 2] = fb;
            result[ri + 3] = 255;
        }
    }
}
