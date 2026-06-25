using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace MobileApp.Pages;

public class HomePage : Component
{
    private readonly SignalList<TaskItem> _tasks;
    private int _taskCount = 3;
    private VStack _taskList = null!;

    public HomePage()
    {
        _tasks = UseSignalList<TaskItem>();
        _tasks.Add(new TaskItem("Review sprint notes", "Today", new Color(62, 126, 214)));
        _tasks.Add(new TaskItem("Ship mobile polish", "Tomorrow", new Color(34, 150, 94)));
        _tasks.Add(new TaskItem("Check Android build", "Friday", new Color(225, 142, 38)));
    }

    protected override void OnInit()
    {
        UseSubscription(_tasks, () => UIUpdateQueue.EnqueueUIUpdate(RebuildTasks));
    }

    public override VisualElement Build()
    {
        _taskList = new VStack()
            .Spacing(18);
        RebuildTasks();

        return new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Star)
            .AddChild(
                new ScrollView()
                    .Content(
                        new VStack()
                            .Spacing(18)
                            .Padding(new Thickness(20, 20, 20, 96))
                            .Children(
                                BuildHeroCard(),
                                _taskList
                            )),
                0,
                0)
            .AddChild(
                new ButtonFloat(Icons.Add)
                    .Dock(ButtonFloatPlacement.BottomRight, 20)
                    .OnTapped(AddTask),
                0,
                0);
    }

    private VisualElement BuildHeroCard()
    {
        return new Frame()
            .Background(Color.White)
            .BorderRadius(14)
            .Padding(new Thickness(20))
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new Label("ButtonFloat")
                            .FontSize(26)
                            .Foreground(new Color(25, 39, 62)),
                        new Label("The home page now uses a floating action button over scrollable content, matching a common mobile compose action.")
                            .FontSize(14)
                            .LineHeight(1.25f)
                            .Foreground(new Color(91, 103, 122))
                    ));
    }

    private VisualElement BuildTaskCard(string title, string due, Color accent)
    {
        return new Frame()
            .Background(Color.White)
            .BorderRadius(14)
            .Padding(new Thickness(16))
            .Content(
                new HStack()
                    .Spacing(12)
                    .Children(
                        new Frame()
                            .Size(40)
                            .BorderRadius(20)
                            .Background(new Color(accent.R, accent.G, accent.B, 0.18f))
                            .Content(
                                new Icon(Icons.Check)
                                    .Size(18)
                                    .Color(accent)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)),
                        new VStack()
                            .Spacing(3)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Children(
                                new Label(title)
                                    .FontSize(15)
                                    .Foreground(new Color(25, 39, 62)),
                                new Label(due)
                                    .FontSize(12)
                                    .Foreground(new Color(91, 103, 122))
                            )
                    ));
    }

    private void AddTask()
    {
        _taskCount++;
        _tasks.Insert(0, new TaskItem($"New mobile task #{_taskCount}", "Just now", new Color(62, 126, 214)));
        ToastService.ShowSuccess($"Created task #{_taskCount}");
    }

    private void RebuildTasks()
    {
        if (_taskList is null)
        {
            return;
        }

        _taskList.ClearChildren();

        foreach (var task in _tasks)
        {
            _taskList.AddChild(BuildTaskCard(task.Title, task.Due, task.Accent));
        }
    }

    private readonly record struct TaskItem(string Title, string Due, Color Accent);
}
