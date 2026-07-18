using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Rendering;

namespace Nano.Views.Game;

/// <summary>A multi-touch virtual joystick with two action buttons.</summary>
internal sealed class VirtualGameControls(NanoGameInputState input)
    : View<VirtualGameControls>, IPointerHandler, IExclusiveTouchHandler
{
    private const float JoystickRadius = 58f;
    private const float KnobRadius = 25f;
    private const float ButtonARadius = 37f;
    private const float ButtonBRadius = 31f;

    private readonly Dictionary<int, ControlTarget> _activePointers = [];
    private int? _joystickPointerId;

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth)
            ? availableWidth
            : 390;
        DesiredHeight = availableHeight > 0 && !float.IsInfinity(availableHeight)
            ? availableHeight
            : 720;
    }

    public void OnPointerPressed(PointerEventArgs args)
    {
        var target = HitControl(args.Position);
        if (target == ControlTarget.None)
            return;

        if (target == ControlTarget.Joystick)
        {
            if (_joystickPointerId is not null)
                return;

            _joystickPointerId = args.PointerId;
            UpdateJoystick(args.Position);
        }

        _activePointers[args.PointerId] = target;
        UpdateButtons();
        MarkNeedsPaint();
        args.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs args)
    {
        if (!_activePointers.TryGetValue(args.PointerId, out var target))
            return;

        if (target == ControlTarget.Joystick)
            UpdateJoystick(args.Position);

        MarkNeedsPaint();
        args.Handled = true;
    }

    public void OnPointerReleased(PointerEventArgs args) => Release(args);

    public void OnPointerCanceled(PointerEventArgs args) => Release(args);

    protected override void OnUnmounted()
    {
        _activePointers.Clear();
        _joystickPointerId = null;
        input.Reset();
        base.OnUnmounted();
    }

    public override void Render(IRenderer renderer)
    {
        var joystickCenter = GetJoystickCenter();
        var knobCenter = joystickCenter + new Vector2(
            input.X * (JoystickRadius - KnobRadius),
            input.Y * (JoystickRadius - KnobRadius));
        var (buttonA, buttonB) = GetButtonCenters();

        renderer.DrawCircle(
            joystickCenter.X,
            joystickCenter.Y,
            JoystickRadius,
            new Color(15, 23, 42, 145));
        renderer.DrawCircleOutline(
            joystickCenter.X,
            joystickCenter.Y,
            JoystickRadius,
            2,
            new Color(170, 190, 220, 150));
        renderer.DrawCircle(
            knobCenter.X,
            knobCenter.Y,
            KnobRadius,
            new Color(105, 145, 205, 210));
        renderer.DrawCircleOutline(
            knobCenter.X,
            knobCenter.Y,
            KnobRadius,
            2,
            new Color(225, 235, 250, 210));

        DrawButton(renderer, buttonB, ButtonBRadius, "B", input.B, new Color(235, 105, 115));
        DrawButton(renderer, buttonA, ButtonARadius, "A", input.A, new Color(75, 200, 140));
    }

    private void Release(PointerEventArgs args)
    {
        if (!_activePointers.Remove(args.PointerId, out var target))
            return;

        if (target == ControlTarget.Joystick && _joystickPointerId == args.PointerId)
        {
            _joystickPointerId = null;
            input.X = 0;
            input.Y = 0;
        }

        UpdateButtons();
        MarkNeedsPaint();
        args.Handled = true;
    }

    private void UpdateJoystick(Vector2 position)
    {
        var offset = position - GetJoystickCenter();
        if (offset.LengthSquared() > JoystickRadius * JoystickRadius)
            offset = Vector2.Normalize(offset) * JoystickRadius;

        var normalized = offset / JoystickRadius;
        const float deadZone = 0.12f;
        if (normalized.Length() < deadZone)
            normalized = Vector2.Zero;

        input.X = Math.Clamp(normalized.X, -1, 1);
        input.Y = Math.Clamp(normalized.Y, -1, 1);
    }

    private void UpdateButtons()
    {
        input.A = _activePointers.Values.Any(target => target == ControlTarget.A);
        input.B = _activePointers.Values.Any(target => target == ControlTarget.B);
    }

    private ControlTarget HitControl(Vector2 position)
    {
        if (Vector2.Distance(position, GetJoystickCenter()) <= JoystickRadius * 1.35f)
            return ControlTarget.Joystick;

        var (buttonA, buttonB) = GetButtonCenters();
        if (Vector2.Distance(position, buttonA) <= ButtonARadius * 1.25f)
            return ControlTarget.A;
        if (Vector2.Distance(position, buttonB) <= ButtonBRadius * 1.25f)
            return ControlTarget.B;
        return ControlTarget.None;
    }

    private Vector2 GetJoystickCenter() => new(
        ComputedX + Math.Min(88, ComputedWidth * 0.24f),
        ComputedY + ComputedHeight - 92);

    private (Vector2 A, Vector2 B) GetButtonCenters()
    {
        var bottom = ComputedY + ComputedHeight;
        var right = ComputedX + ComputedWidth;
        return (
            new Vector2(right - 65, bottom - 102),
            new Vector2(right - 142, bottom - 64));
    }

    private static void DrawButton(
        IRenderer renderer,
        Vector2 center,
        float radius,
        string label,
        bool pressed,
        Color color)
    {
        var fill = new Color(color.R, color.G, color.B, pressed ? (byte)245 : (byte)165);
        renderer.DrawCircle(center.X, center.Y, radius, fill);
        renderer.DrawCircleOutline(
            center.X,
            center.Y,
            radius,
            2,
            new Color(245, 248, 255, 190));
        var size = renderer.MeasureText(label, 19);
        renderer.DrawText(
            label,
            center.X - size.X / 2,
            center.Y - size.Y / 2,
            Color.White,
            19);
    }

    private enum ControlTarget
    {
        None,
        Joystick,
        A,
        B
    }
}
