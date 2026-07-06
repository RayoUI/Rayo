using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace NanoApp.Controls;

internal sealed class EntityPropertiesPanel : Component
{
    private SceneEntity? _entity;

    internal void ShowEntity(SceneEntity? entity)
    {
        if (ReferenceEquals(_entity, entity))
        {
            return;
        }

        _entity = entity;
        Rebuild();
    }

    public override VisualElement Build()
    {
        var content = new VStack()
            .Spacing(14)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                new Label("Properties")
                    .FontSize(16)
                    .FontWeight(FontWeight.Bold)
                    .Foreground(Color.White));

        if (_entity is null)
        {
            content.AddChild(
                new Label("Select an entity")
                    .FontSize(12)
                    .Foreground(new Color(100, 116, 139)));
        }
        else
        {
            content.AddChild(
                BuildStringField("Name", _entity.Name, value => _entity.Name = value));
            content.AddChild(
                BuildStringField("Tag", _entity.Tag, value => _entity.Tag = value));
        }

        return new Frame()
            .Background(new Color(20, 27, 40))
            .BorderBrush(new Color(45, 55, 72))
            .BorderThickness(new Thickness(1, 0, 0, 0))
            .Padding(new Thickness(14))
            .Content(content);
    }

    private static VisualElement BuildStringField(
        string label,
        string value,
        Action<string> onChanged)
    {
        return new VStack()
            .Spacing(6)
            .VerticalAlignment(VerticalAlignment.Top)
            .Children(
                new Label(label)
                    .FontSize(12)
                    .Foreground(new Color(148, 163, 184)),
                new Entry(value)
                    .Height(38)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .BorderBrush(new Color(71, 85, 105))
                    .FocusBorderBrush(new Color(56, 189, 248))
                    .BorderThickness(1)
                    .BorderRadius(6)
                    .OnTextChanged(onChanged));
    }
}
