using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTools;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Logger = Rayo.DevTools.DevToolLogger;

namespace ToDoList;

// ─────────────────────────────────────────────────────────────
// Model
// ─────────────────────────────────────────────────────────────
public class TodoItem
{
    public string Text { get; set; } = "";
    public bool IsCompleted { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Root component
// ─────────────────────────────────────────────────────────────
public class ToDoApp : UserControl
{
    private readonly SignalList<TodoItem> _tasks = new();
    private Computed<int?> _taskCount;

    public ToDoApp()
    {
        // Initialize tasks in constructor
        for (int i = 0; i < 15; i++)
        {
            _tasks.Add(new TodoItem { Text = "Task " + (i + 1) });
        }

        _taskCount = new Computed<int?>(() => _tasks.Count);
    }

    public override VisualElement Build()
    {
        return new Frame()
            .Background(new Color(30, 30, 30))
            .Padding(new Thickness(20))
            .Content(
                new Grid()
                    .Rows(GridLength.Auto, GridLength.Auto, GridLength.Star)
                    .Columns(GridLength.Star)
                    .RowSpacing(20)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .AddChild(new ToDoHeader(_taskCount), 0, 0)
                    .AddChild(new ToDoInput(AddTask),1, 0)
                    .AddChild(new ToDoList(_tasks, RemoveTask), 2, 0)
            );
    }

    private void AddTask(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _tasks.Insert(0, new TodoItem { Text = text });
            Logger.Log($"Task added: {text}");
        }
    }

    private void RemoveTask(TodoItem task)
    {
        _tasks.Remove(task);
        Logger.Log($"Removed task: {task.Text}");
    }
}

// ─────────────────────────────────────────────────────────────
// Header
// ─────────────────────────────────────────────────────────────
public class ToDoHeader(IReadableSignal<int?> taskCount) : UserControl
{
    protected override void OnInit()
    {
        this.HorizontalAlignment = HorizontalAlignment.Center;
    }   

    public override VisualElement Build() =>
        new BadgeContainer()
            .Content(
                new Label("My ToDo List")
                    .FontSize(24)
                    .Foreground(Color.White)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
            )
            .Badge(
                new Badge()
                    .ShowZero(true)
                    .Count(taskCount)
                    .Background(new Color(239, 68, 68))
                    .BadgeSize(BadgeSize.Large)
                    .TextColor(Color.White)
            )
            .BadgeHorizontalPosition(HorizontalAlignment.Right)
            .BadgeVerticalPosition(VerticalAlignment.Top)
            .BadgeOffset(new Position(15, 0));
}

// ─────────────────────────────────────────────────────────────
// Input row
// ─────────────────────────────────────────────────────────────
public class ToDoInput(Action<string> onAdd) : UserControl
{
    private Entry _entry = null!;

    public override VisualElement Build() =>
        new HStack()
            .Spacing(10)
            .VerticalAlignment(VerticalAlignment.Top)
            .Height(40)
            .Children(
                new Entry()
                    .Ref(out _entry)
                    .Placeholder("Add a new task...")
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .OnEnter(Submit),
                new Button()
                    .Text("Add")
                    .Width(80)
                    .OnTapped(Submit)
            );

    private void Submit()
    {
        onAdd(_entry.Text);
        _entry.Text("");
    }
}

// ─────────────────────────────────────────────────────────────
// Task list
// ─────────────────────────────────────────────────────────────
public class ToDoList(SignalList<TodoItem> tasks, Action<TodoItem> onDelete) : UserControl
{
    private VStack _listContainer = null!;

    protected override void OnInit()
    {
        // Defer UI updates to avoid modifying the tree during event processing
        tasks.Subscribe(() => UIUpdateQueue.EnqueueUIUpdate(RebuildList));
    }

    public override VisualElement Build()
    {
        var stack = new VStack()
            .Ref(out _listContainer)
            .Spacing(10)
            .Padding(new Thickness(0, 0, 0, 20));

        RebuildList();

        return new ScrollView()
            .Content(stack)
            .Background(new Color(40, 40, 40))
            .BorderRadius(8);
    }

    private void RebuildList()
    {
        if (_listContainer is null) return;

        _listContainer.ClearChildren();

        foreach (var task in tasks)
        {
            _listContainer.AddChild(new ToDoItem(task, onDelete));
        }
    }
}

// ─────────────────────────────────────────────────────────────
// Single task row
// ─────────────────────────────────────────────────────────────
public class ToDoItem(TodoItem task, Action<TodoItem> onDelete) : UserControl
{
    public override VisualElement Build()
    {
        var checkbox = new Checkbox()
            .IsChecked(task.IsCompleted)
            .Background(Color.White)
            .Margin(new Thickness(right: 7))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .OnChanged(isChecked =>
            {
                task.IsCompleted = isChecked;
                Rebuild(); // re-render this item to reflect the new state
            });

        var label = new Label(task.Text)
            .Foreground(task.IsCompleted ? Color.Red : Color.White)
            .When(task.IsCompleted, lbl => lbl.TextDecorations(TextDecorations.Strikethrough))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center);

        var deleteBtn = new Button()
            .Text("X")
            .Background(new Color(200, 50, 50))
            .Size(new Size(40, 30))
            .HorizontalAlignment(HorizontalAlignment.Right)
            .VerticalAlignment(VerticalAlignment.Center)
            .OnTapped(() => onDelete(task));

        return new Grid()
            .Columns(GridLength.Auto, GridLength.Star, GridLength.Auto)
            .Rows(GridLength.Auto)
            .RowSpacing(20)
            .Height(40)
            .Padding(new Thickness(5, 10))
            .AddChild(checkbox, 0, 0)
            .AddChild(label,    0, 1)
            .AddChild(deleteBtn, 0, 2);
    }
}
