using Rayo.Rendering.OpenGL;
using Rayo.Rendering;

namespace Rayo.Tests;

public sealed class OpenGLTextureBindingTests
{
    [Fact]
    public void Resolve_accepts_layer_cache_render_targets_and_flips_their_vertical_axis()
    {
        var renderTarget = new OpenGLRenderTargetTexture(
            textureId: 42,
            fboId: 7,
            width: 390,
            height: 780,
            gl: null!);

        var binding = OpenGLTextureBinding.Resolve(renderTarget);

        Assert.NotNull(binding);
        Assert.Equal(42u, binding.Value.TextureId);
        Assert.True(binding.Value.FlipVertically);
        Assert.Equal(Color.White, OpenGLTextureBinding.ResolveTint(null));
    }
}
