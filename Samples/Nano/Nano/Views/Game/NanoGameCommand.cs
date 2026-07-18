namespace Nano.Views.Game;

internal readonly record struct GameColor(byte R, byte G, byte B, byte A = 255);

internal abstract record NanoGameCommand;

internal sealed record ClearCommand(GameColor Color) : NanoGameCommand;

internal sealed record RectCommand(
    float X,
    float Y,
    float Width,
    float Height,
    GameColor Color) : NanoGameCommand;

internal sealed record LineCommand(
    float X1,
    float Y1,
    float X2,
    float Y2,
    GameColor Color) : NanoGameCommand;

internal sealed record CircleCommand(
    float CenterX,
    float CenterY,
    float Radius,
    GameColor Color) : NanoGameCommand;
