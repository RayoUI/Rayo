using Rayo.Core;
using Rayo.Core.Interfaces;

namespace Rayo.DevTool;

public class DevToolBuilder : IUIBuilder
{
    public VisualElement Build() => new DevToolUI();
}
