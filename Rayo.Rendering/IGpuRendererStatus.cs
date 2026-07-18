namespace Rayo.Rendering;

/// <summary>Exposes whether a renderer is currently backed by a hardware GPU surface.</summary>
public interface IGpuRendererStatus
{
    bool IsGpuAccelerated { get; }
}
