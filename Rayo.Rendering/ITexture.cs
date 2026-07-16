namespace Rayo.Rendering;

public enum TextureSamplingMode
{
    Smooth,
    Nearest
}

public interface ITexture : IDisposable
{
    int Width { get; }
    int Height { get; }
    TextureSamplingMode SamplingMode => TextureSamplingMode.Smooth;
}
