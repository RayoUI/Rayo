using System.Reflection;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Styling;

namespace Rayo.Tests;

public sealed class BuiltInThemeContractTests
{
    [Fact]
    public void Every_constructible_builtin_control_inherits_runtime_scope_changes()
    {
        var controlTypes = typeof(Button).Assembly
            .GetTypes()
            .Where(type =>
                type.IsPublic &&
                !type.IsAbstract &&
                !type.ContainsGenericParameters &&
                typeof(VisualElement).IsAssignableFrom(type) &&
                type.Namespace == typeof(Button).Namespace &&
                type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.FullName)
            .ToArray();

        var failures = new List<string>();
        foreach (var controlType in controlTypes)
        {
            try
            {
                var control = (VisualElement)Activator.CreateInstance(controlType)!;
                var scope = new ThemeScope(RayoThemes.Light, control);
                scope.Theme = RayoThemes.Dark;

                if (!ReferenceEquals(control.EffectiveTheme, RayoThemes.Dark))
                    failures.Add($"{controlType.Name}: did not inherit the changed scope.");
            }
            catch (Exception exception)
            {
                failures.Add($"{controlType.Name}: {Unwrap(exception).Message}");
            }
        }

        Assert.NotEmpty(controlTypes);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException invocation
            ? invocation.InnerException ?? exception
            : exception;
}
