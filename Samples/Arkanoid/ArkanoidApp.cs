namespace Arkanoid;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Controls;
using Rayo.Rendering;

/// <summary>
/// IUIBuilder entry point
/// </summary>
public class ArkanoidApp : IUIBuilder
{
    public VisualElement Build()
    {
        return new ArkanoidCanvas()
                        .BackgroundColor(new Color(15, 12, 30))
                        .WallColor(new Color(55, 55, 90))
                        .HudColor(new Color(200, 200, 220))
                        .HudFontSize(15f)
                        .OverlayFontSize(34f);
    }
}
