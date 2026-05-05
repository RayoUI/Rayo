using PaintApp.Controls;
using Rayo;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace PaintApp.ViewModels;

public class PaintViewState
{
    public Signal<PaintTool> Tool         { get; } = new(PaintTool.Pencil);
    public Signal<Color>     PrimaryColor { get; } = new(Color.Black);
    public Signal<float>     BrushSize    { get; } = new(3f);
    public Signal<string>    PositionText { get; } = new("");
    public Signal<string>    ToolText     { get; } = new("Pencil");
}
