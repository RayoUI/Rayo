using Rayo.Core;
using Rayo.Controls;
using Rayo.Layout;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using Kanban.Controls;
using Rayo;

namespace Rayo.Example;

public class KanbanApp : IUIBuilder
{
    public VisualElement Build()
    {
        return new VStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch) // Ensure root fills window
            .Children(
                new Label("Drag & Drop Example - Arrastra las tarjetas a las zonas")
                    .Size(new Size(0, 40)),

                new HStack()
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Children(
                        // Frame izquierdo: tarjetas draggables
                        new VStack()
                            .Background(Color.Gray)
                            .Width(250)
                            .Spacing(10)
                            .Padding(new Thickness(20))
                            .Children(
                                new Label("Draggables").Size(new Size(0, 30)),
                                new DraggableCard("😀 Design", new Color(59, 130, 246)),
                                new DraggableCard("😎 Development", new Color(16, 185, 129)),
                                new DraggableCard("🐰 Deployment", new Color(245, 158, 11)),
                                new DraggableCard("🍉 Analytics", new Color(139, 92, 246))
                            ),

                        // Frame derecho: drop zones
                        new VStack()
                            .Background(Color.Purple)
                            .Spacing(10)
                            .Padding(new Thickness(5))
                            .HorizontalAlignment(HorizontalAlignment.Stretch) 
                            .VerticalAlignment(VerticalAlignment.Stretch)
                            .Children(
                                new Label("Drop Zones").Size(new Size(0, 30)),
                                new HStack()
                                    .Spacing(5)
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .VerticalAlignment(VerticalAlignment.Stretch)
                                    .Children(
                                        new DropZone("TODO", "card"),
                                        new DropZone("IN PROGRESS", "card"),
                                        new DropZone("DONE", "card")
                                    )
                            )
                    )
            );
    }
}