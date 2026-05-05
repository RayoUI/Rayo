using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using Rayo;

namespace Kanban.Controls;

/// <summary>
/// Zona de drop que puede recibir tarjetas.
/// </summary>
public class DropZone : Frame, IDropTarget
{
    private string _title;
    private string _acceptedDataType;
    private List<string> _droppedItems = new();
    private Color _normalColor = new Color(243, 244, 246);
    private Color _activeColor = new Color(219, 234, 254);

    public bool IsDropTargetActive { get; set; }

    public DropZone(string title, string acceptedDataType)
    {
        _title = title;
        _acceptedDataType = acceptedDataType;

        // Make responsive: Fill available space
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        this.Background(_normalColor);
        BorderRadius = 8;
        Padding = new Thickness(15);
    }

    public bool CanAcceptDataType(string dataType) => dataType == "card";

    public DropConstraints? Constraints => null;
    public DragDropEffect? AllowedEffects => null;

    public bool OnDragEnter(DragData data)
    {
        // Cambiar a color activo
        this.Background(_activeColor);
        MarkNeedsPaint();
        return true; // Aceptar el drop
    }

    public void OnDragOver(DragData dragData, float mouseX, float mouseY)
    {
        // Opcionalmente podr�amos mostrar un indicador de donde se va a dropear
    }

    public void OnDragLeave(DragData dragData)
    {
        // Volver al color normal
        this.Background(_normalColor);
        MarkNeedsPaint();
    }

    public bool OnDrop(DragData dragData, float mouseX, float mouseY)
    {
        // Agregar el item a nuestra lista
        if (dragData.Data is string text)
        {
            _droppedItems.Add(text);
        }

        // Volver al color normal
        this.Background(_normalColor);
        MarkNeedsPaint();

        return true; // Aceptar el drop
    }

    public override void Render(IRenderer renderer)
    {
        base.Render(renderer);


        // Clip content to prevent overflow
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);

        float currentY = ComputedY + Padding.Top;

        // Renderizar ttulo
        renderer.DrawText(_title, ComputedX + Padding.Left, currentY, new Color(107, 114, 128));
        currentY += 30;

        // Renderizar items dropeados
        foreach (var item in _droppedItems)
        {
            // Renderizar un rectngulo para cada item
            renderer.DrawRoundedRect(
                ComputedX + Padding.Left,
                currentY,
                ComputedWidth - Padding.Horizontal,
                50,
                8,
                new Color(59, 130, 246)
            );

            // Renderizar el texto del item
            var textSize = renderer.MeasureText(item);
            float textX = ComputedX + Padding.Left + 10;
            float textY = currentY + (50 - textSize.Y) / 2;
            renderer.DrawText(item, textX, textY, Color.White, 18);

            currentY += 60;
        }

        renderer.PopScissor();

        // Si est activo, mostrar un indicador
        if (IsDropTargetActive)
        {
            // Dibujar un borde usando rectángulos
            float borderWidth = 3;
            var borderColor = new Color(59, 130, 246);


            // Top
            renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, borderWidth, borderColor);
            // Bottom
            renderer.DrawRect(ComputedX, ComputedY + ComputedHeight - borderWidth, ComputedWidth, borderWidth, borderColor);
            // Left
            renderer.DrawRect(ComputedX, ComputedY, borderWidth, ComputedHeight, borderColor);
            // Right
            renderer.DrawRect(ComputedX + ComputedWidth - borderWidth, ComputedY, borderWidth, ComputedHeight, borderColor);
        }
    }
}

