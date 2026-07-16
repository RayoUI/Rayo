using Rayo.Core.Platform;

namespace Rayo.Tests;

public sealed class VirtualKeyboardManagerTests
{
    [Fact]
    public void Replacing_and_clearing_service_disposes_owned_instances()
    {
        var first = new TestVirtualKeyboardService();
        var second = new TestVirtualKeyboardService();

        VirtualKeyboardManager.SetService(first);
        VirtualKeyboardManager.SetService(second);

        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);

        VirtualKeyboardManager.ClearService(second);

        Assert.True(second.IsDisposed);
    }

    private sealed class TestVirtualKeyboardService : IVirtualKeyboardService, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Show(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys)
        {
        }

        public void Hide()
        {
        }

        public void SetAccessoryKeys(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys)
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
