namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.ComponentModel.DataAnnotations;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Component for displaying vector icons
/// Migrated to new MAUI-like architecture: inherits from View<Icon>
/// </summary>
public class Icon : View<Icon>
{

    // =========================================================================
    // PROPERTIES
    // =========================================================================
    #region IconData
    [PaintProperty]
    public IconData? IconData
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Color
    [PaintProperty]
    public Brush Color
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion


#pragma warning disable CS0067
    // =========================================================================
    // EVENT HANDLERS
    // =========================================================================
    // Define the missing events
    public event Action<IconData?>? IconDataChanged;
#pragma warning restore CS0067

    // =========================================================================
    // INITIALIZATION
    // =========================================================================
    private const float DefaultIconSize = 24f;

    public Icon()
    {
        Color = Rayo.Rendering.Color.White;
        Size(DefaultIconSize);
    }

    public Icon(IconData iconData)
        : this()
    {
        IconData = iconData;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (Width == 0) Width = DefaultIconSize;
        if (Height == 0) Height = DefaultIconSize;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        if (IconData != null)
        {
            RenderVectorIcon(renderer);
        }
    }

    private void RenderVectorIcon(IRenderer renderer)
    {
        if (IconData == null) return;

        float scaleX = ComputedWidth / IconData.ViewBoxWidth;
        float scaleY = ComputedHeight / IconData.ViewBoxHeight;
        float scale = Math.Min(scaleX, scaleY);

        float offsetX = 0;
        float offsetY = 0;

        if (scaleX > scaleY)
        {
            offsetX = (ComputedWidth - (IconData.ViewBoxWidth * scale)) / 2;
        }
        else if (scaleY > scaleX)
        {
            offsetY = (ComputedHeight - (IconData.ViewBoxHeight * scale)) / 2;
        }

        float renderX = ComputedX + offsetX;
        float renderY = ComputedY + offsetY;

        foreach (var command in IconData.Commands)
        {
            command.Draw(renderer, renderX, renderY, scale, Color.PrimaryColor);
        }
    }
}
