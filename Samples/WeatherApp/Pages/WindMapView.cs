using Rayo.Core;
using Rayo.Rendering;

namespace WeatherApp.Pages;

public sealed class WindMapView : View<WindMapView>
{
    public int MapZoom { get; private set; } = 4;

    public WindMapView Zoom(int zoom)
    {
        MapZoom = zoom;
        return this;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsPositiveInfinity(availableWidth) ? 800 : availableWidth;
        DesiredHeight = float.IsPositiveInfinity(availableHeight) ? 600 : availableHeight;
    }

    public override void Render(IRenderer renderer)
    {
        var x = ComputedX;
        var y = ComputedY;
        var width = ComputedWidth;
        var height = ComputedHeight;

        renderer.DrawRect(x, y, width, height, new Color(20, 58, 75));

        var grid = new Color(111, 170, 181, 70);
        var step = Math.Max(38, 92 - MapZoom * 7);
        for (var gx = x; gx < x + width; gx += step)
            renderer.DrawLine(gx, y, gx, y + height, 1, grid);
        for (var gy = y; gy < y + height; gy += step)
            renderer.DrawLine(x, gy, x + width, gy, 1, grid);

        var land = new Color(37, 103, 96);
        renderer.DrawRoundedRect(x + width * .08f, y + height * .18f, width * .34f, height * .55f, 40, land);
        renderer.DrawRoundedRect(x + width * .46f, y + height * .10f, width * .42f, height * .64f, 50, land);

        var bands = new[]
        {
            new Color(48, 185, 223, 145),
            new Color(65, 209, 128, 145),
            new Color(241, 210, 63, 145),
            new Color(234, 91, 73, 135)
        };

        for (var i = 0; i < bands.Length; i++)
        {
            var offset = (i + 1) * height / 6f;
            renderer.DrawLine(x + width * .03f, y + offset, x + width * .94f, y + offset + (i % 2 == 0 ? 55 : -35), 26, bands[i]);
        }

        renderer.DrawText($"Interactive wind layer · zoom {MapZoom}", x + 18, y + height - 34, Color.White, 14);
    }
}
