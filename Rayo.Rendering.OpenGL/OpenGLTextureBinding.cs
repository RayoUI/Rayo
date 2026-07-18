namespace Rayo.Rendering.OpenGL;

internal readonly record struct OpenGLTextureBinding(uint TextureId, bool FlipVertically)
{
    public static Color ResolveTint(Color? tint) => tint ?? Color.White;

    public static OpenGLTextureBinding? Resolve(ITexture texture) => texture switch
    {
        OpenGLTexture uploaded => new OpenGLTextureBinding(uploaded.Id, false),
        OpenGLRenderTargetTexture renderTarget =>
            new OpenGLTextureBinding(renderTarget.TextureId, true),
        _ => null
    };
}
