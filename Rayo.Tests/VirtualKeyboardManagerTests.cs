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

    [Fact]
    public void Pause_and_resume_are_forwarded_with_focused_keyboard_options()
    {
        var service = new TestVirtualKeyboardService();
        var options = new TestKeyboardOptions();
        VirtualKeyboardManager.SetService(service);
        try
        {
            VirtualKeyboardManager.NotifyAppPaused();
            VirtualKeyboardManager.RestoreAfterResume(options);

            Assert.Equal(1, service.PauseCount);
            Assert.Same(options, service.RestoredOptions);
        }
        finally
        {
            VirtualKeyboardManager.ClearService(service);
        }
    }

    private sealed class TestVirtualKeyboardService : IVirtualKeyboardService, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public int PauseCount { get; private set; }
        public IVirtualKeyboardOptions? RestoredOptions { get; private set; }

        public void Show(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys)
        {
        }

        public void Hide()
        {
        }

        public void SetAccessoryKeys(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys)
        {
        }

        public void NotifyAppPaused() => PauseCount++;

        public void RestoreAfterResume(IVirtualKeyboardOptions? options) => RestoredOptions = options;

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class TestKeyboardOptions : IVirtualKeyboardOptions
    {
        public VirtualKeyboardType KeyboardType => VirtualKeyboardType.Default;
        public bool IsMultiline => true;
        public IReadOnlyList<VirtualKeyboardAccessoryKey> KeyboardAccessoryKeys => [];
    }
}
