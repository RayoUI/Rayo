using System.Reflection;
using Rayo.Controls;
using Rayo.Rendering;

namespace Rayo.Tests;

public sealed class ImageTexturePipelineTests
{
    [Fact]
    public async Task Image_prepares_off_device_then_uploads_and_draws_on_render()
    {
        var image = new Image(new StreamImageSource([1, 2, 3], "test://image"));
        await WaitForState(image, ImageTextureState.SourceReady);
        var renderer = DispatchProxy.Create<ITestRenderer, RendererProxy>();
        var proxy = (RendererProxy)(object)renderer;

        image.Render(renderer);

        Assert.Equal(ImageTextureState.Prepared, image.TextureState);
        Assert.Equal(1, proxy.PrepareCount);
        Assert.Equal(0, proxy.UploadCount);
        Assert.Equal(0, proxy.DrawCount);

        image.Render(renderer);

        Assert.Equal(ImageTextureState.Uploaded, image.TextureState);
        Assert.Equal(1, proxy.UploadCount);
        Assert.Equal(1, proxy.DrawCount);
    }

    private static async Task WaitForState(Image image, ImageTextureState state)
    {
        for (var attempt = 0; attempt < 100 && image.TextureState != state; attempt++)
            await Task.Delay(1);
        Assert.Equal(state, image.TextureState);
    }

    private interface ITestRenderer : IRenderer, ITexturePreparationRenderer
    {
    }

    private class RendererProxy : DispatchProxy
    {
        private readonly TestTexture _texture = new();

        public int PrepareCount { get; private set; }
        public int UploadCount { get; private set; }
        public int DrawCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                nameof(ITexturePreparationRenderer.TryGetLoadedTexture) => null,
                nameof(ITexturePreparationRenderer.PrepareTextureAsync) => Prepare(),
                nameof(ITexturePreparationRenderer.UploadPreparedTexture) => Upload(),
                nameof(IRenderer.DrawTexture) => Draw(),
                _ => DefaultValue(targetMethod?.ReturnType)
            };
        }

        private Task<IPreparedTexture?> Prepare()
        {
            PrepareCount++;
            return Task.FromResult<IPreparedTexture?>(new TestPreparedTexture());
        }

        private ITexture Upload()
        {
            UploadCount++;
            return _texture;
        }

        private object? Draw()
        {
            DrawCount++;
            return null;
        }

        private static object? DefaultValue(Type? type) =>
            type == null || type == typeof(void) || !type.IsValueType
                ? null
                : Activator.CreateInstance(type);
    }

    private sealed record TestPreparedTexture : IPreparedTexture
    {
        public string CacheKey => "test://image";
    }

    private sealed class TestTexture : ITexture
    {
        public int Width => 24;
        public int Height => 24;
        public void Dispose()
        {
        }
    }
}
