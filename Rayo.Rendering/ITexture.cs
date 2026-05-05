namespace Rayo.Rendering;

public interface ITexture : IDisposable
{
    int Width { get; }
    int Height { get; }
}