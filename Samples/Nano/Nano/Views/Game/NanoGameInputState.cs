namespace Nano.Views.Game;

/// <summary>Shared state written by the virtual controls and read by the Lua game loop.</summary>
internal sealed class NanoGameInputState
{
    public float X { get; internal set; }

    public float Y { get; internal set; }

    public bool A { get; internal set; }

    public bool B { get; internal set; }

    public bool Left => X < -0.35f;

    public bool Right => X > 0.35f;

    public bool Up => Y < -0.35f;

    public bool Down => Y > 0.35f;

    public void Reset()
    {
        X = 0;
        Y = 0;
        A = false;
        B = false;
    }
}
