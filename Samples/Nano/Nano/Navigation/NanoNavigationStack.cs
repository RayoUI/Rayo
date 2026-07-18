using Rayo.Core;

namespace Nano.Navigation;

/// <summary>Owns Nano's page history while the host displays the top page.</summary>
internal sealed class NanoNavigationStack
{
    private readonly Stack<VisualElement> _pages = new();

    public int Count => _pages.Count;

    public VisualElement Current => _pages.Peek();

    public void SetRoot(VisualElement page)
    {
        _pages.Clear();
        _pages.Push(page);
    }

    public void Push(VisualElement page) => _pages.Push(page);

    public VisualElement? Pop()
    {
        if (_pages.Count <= 1)
            return null;

        _pages.Pop();
        return _pages.Peek();
    }
}
