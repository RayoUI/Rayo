namespace Rayo.Rendering;

public interface IFont : IDisposable
{
    string Name { get; }
    float Size { get; }
    bool IsBold => false;
    bool IsItalic => false;
}