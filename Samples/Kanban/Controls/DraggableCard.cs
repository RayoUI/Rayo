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
/// Tarjeta draggable que puede ser arrastrada.
/// </summary>
public class DraggableCard : Frame, IDraggable
{
    private string _text;
    private Color _color;
    private Color _originalColor;
    private float _dragStartX;
    private float _dragStartY;

    public bool IsDragging { get; set; }

    public DraggableCard(string text, Color color)
    {
        _text = text;
        _color = color;
        _originalColor = color;

        Height = 40; // Fixed height only
        HorizontalAlignment = HorizontalAlignment.Stretch; // Stretch width
        Background = color;
        BorderRadius = 8;
        Padding = new Thickness(0);
    }

    public DragData? OnDragStart(float mouseX, float mouseY)
    {
        _dragStartX = ComputedX;
        _dragStartY = ComputedY;

        // Hacer el elemento original semi-transparente durante el drag
        this.Background(new Color(_originalColor.R, _originalColor.G, _originalColor.B, 0.4f));

        // Crear los datos del drag
        return new DragData("card", _text, this)
        {
            StartX = mouseX,
            StartY = mouseY
        }.WithMetadata("color", _originalColor);
    }

    public void OnDragging(float mouseX, float mouseY)
    {
        // Opcionalmente podr�amos mover visualmente la tarjeta
        // Por ahora solo cambiamos la opacidad
        MarkNeedsPaint();
    }

    public void OnDragEnd(bool wasDropped)
    {
        if (!wasDropped)
        {
            // Si no se dropeo en ning�n lugar, volver al color original
            this.Background(_originalColor);
        }
        else
        {
            // Si se dropeo exitosamente, hacerse invisible
            IsVisible = false;
        }

        MarkNeedsPaint();
    }

    public override void Render(IRenderer renderer)
    {
        // Si se está arrastrando, dibujar una sombra primero
        if (IsDragging)
        {
            // Sombra desplazada ligeramente
            renderer.DrawRoundedRect(
                ComputedX + 4,
                ComputedY + 4,
                ComputedWidth,
                ComputedHeight,
                8,
                new Color(0, 0, 0, 100)
            );
        }

        base.Render(renderer);

        // Renderizar el texto centrado
        if (!string.IsNullOrEmpty(_text))
        {
            var textSize = renderer.MeasureText(_text);
            float textX = ComputedX + (ComputedWidth - textSize.X) / 2;
            float textY = ComputedY + (ComputedHeight - textSize.Y) / 2;
            renderer.DrawText(_text, textX, textY, Color.White, 18);
        }
    }
}