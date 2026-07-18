namespace Nano.GameEngine;

/// <summary>Shared state written by the virtual controls and read by the Lua game loop.</summary>
internal sealed class NanoGameInputState
{
    private readonly List<UiHitRegion> _uiHitRegions = [];
    public float X { get; internal set; }

    public float Y { get; internal set; }

    public bool A { get; internal set; }

    public bool B { get; internal set; }

    public bool Left => X < -0.35f;

    public bool Right => X > 0.35f;

    public bool Up => Y < -0.35f;

    public bool Down => Y > 0.35f;

    internal float PointerX { get; set; }
    internal float PointerY { get; set; }
    internal bool PointerDown { get; set; }
    internal bool PointerPressed { get; set; }
    internal bool PointerReleased { get; set; }

    internal bool IsOverUi(float x, float y) =>
        _uiHitRegions.Any(region => region.Contains(x, y));

    internal void SetUiHitRegions(IReadOnlyList<UiHitRegion> regions)
    {
        _uiHitRegions.Clear();
        for (var index = 0; index < regions.Count; index++)
            _uiHitRegions.Add(regions[index]);
    }

    internal void FinishUiFrame()
    {
        PointerPressed = false;
        PointerReleased = false;
    }

    public void Reset()
    {
        X = 0;
        Y = 0;
        A = false;
        B = false;
        PointerDown = false;
        PointerPressed = false;
        PointerReleased = false;
        _uiHitRegions.Clear();
    }
}

internal readonly record struct UiHitRegion(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) =>
        x >= X && y >= Y && x <= X + Width && y <= Y + Height;
}
