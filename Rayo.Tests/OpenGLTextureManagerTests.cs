using System.Text;
using Rayo.Rendering.OpenGL;

namespace Rayo.Tests;

public sealed class OpenGLTextureManagerTests
{
    [Fact]
    public void DecodeImage_rasterizes_svg_using_its_viewport_size()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg"
                 width="24px" height="24px" viewBox="0 -960 960 960">
              <path fill="#ffffff" d="M120-240v-80h720v80H120Z"/>
            </svg>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));

        var image = OpenGLTextureManager.DecodeImage(stream);

        Assert.NotNull(image);
        Assert.Equal(24, image.Width);
        Assert.Equal(24, image.Height);
        Assert.Contains(image.Data, value => value != 0);
    }
}
