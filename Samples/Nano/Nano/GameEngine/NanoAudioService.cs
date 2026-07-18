using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace Nano.GameEngine;

/// <summary>Cross-platform audio playback backed by SoundFlow/miniaudio.</summary>
internal sealed class NanoAudioService(Func<string, byte[]> readAsset) : IDisposable
{
    private readonly Dictionary<int, Playback> _playbacks = [];
    private readonly List<int> _finishedHandles = [];
    private readonly Dictionary<string, byte[]> _assetCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _initializationLock = new();
    private AudioEngine? _engine;
    private AudioPlaybackDevice? _device;
    private Task? _initializationTask;
    private int _nextHandle;
    private bool _disposed;

    public int ActivePlaybackCount => _playbacks.Count;

    public bool IsWarmUpPending
    {
        get
        {
            lock (_initializationLock)
                return _initializationTask is { IsCompleted: false };
        }
    }

    public void Update() => CleanupFinished();

    /// <summary>Caches an asset and starts device initialization outside the game frame.</summary>
    public void Preload(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = GetAsset(path);
        WarmUp();
    }

    /// <summary>Starts miniaudio initialization without blocking the caller.</summary>
    public void WarmUp()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = GetInitializationTask();
    }

    public int Play(string path, float volume, bool loop)
    {
        CleanupFinished();
        EnsureDevice();
        var stream = new MemoryStream(GetAsset(path), writable: false);
        try
        {
            var provider = new StreamDataProvider(_engine!, AudioFormat.DvdHq, stream);
            var player = new SoundPlayer(_engine!, AudioFormat.DvdHq, provider)
            {
                Volume = Math.Clamp(volume, 0, 1),
                IsLooping = loop
            };
            _device!.MasterMixer.AddComponent(player);
            player.Play();
            var handle = ++_nextHandle;
            _playbacks[handle] = new Playback(player, provider, stream);
            return handle;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public void Stop(int handle)
    {
        if (!_playbacks.Remove(handle, out var playback))
            return;

        playback.Player.Stop();
        _device?.MasterMixer.RemoveComponent(playback.Player);
        playback.Dispose();
    }

    public void StopAll()
    {
        foreach (var handle in _playbacks.Keys.ToArray())
            Stop(handle);
    }

    public bool IsPlaying(int handle) =>
        _playbacks.TryGetValue(handle, out var playback) &&
        playback.Player.State == PlaybackState.Playing;

    public void SetVolume(int handle, float volume)
    {
        if (_playbacks.TryGetValue(handle, out var playback))
            playback.Player.Volume = Math.Clamp(volume, 0, 1);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        Task? initialization;
        lock (_initializationLock)
            initialization = _initializationTask;
        if (initialization is not null)
        {
            try
            {
                initialization.GetAwaiter().GetResult();
            }
            catch
            {
                // A failed warm-up owns no live device; disposal still continues.
            }
        }

        StopAll();
        _device?.Stop();
        _device?.Dispose();
        _engine?.Dispose();
        _assetCache.Clear();
    }

    private void EnsureDevice()
    {
        GetInitializationTask().GetAwaiter().GetResult();
    }

    private Task GetInitializationTask()
    {
        lock (_initializationLock)
        {
            if (_initializationTask is not null)
                return _initializationTask;

            _initializationTask = Task.Run(InitializeDevice);
            return _initializationTask;
        }
    }

    private void InitializeDevice()
    {
        var engine = new MiniAudioEngine();
        try
        {
            var device = engine.InitializePlaybackDevice(
                null,
                AudioFormat.DvdHq,
                new MiniAudioDeviceConfig());
            device.Start();
            _engine = engine;
            _device = device;
        }
        catch
        {
            engine.Dispose();
            throw;
        }
    }

    private byte[] GetAsset(string path)
    {
        if (_assetCache.TryGetValue(path, out var bytes))
            return bytes;
        bytes = readAsset(path);
        _assetCache[path] = bytes;
        return bytes;
    }

    private void CleanupFinished()
    {
        _finishedHandles.Clear();
        foreach (var pair in _playbacks)
        {
            if (pair.Value.Player.State == PlaybackState.Stopped)
                _finishedHandles.Add(pair.Key);
        }
        foreach (var handle in _finishedHandles)
            Stop(handle);
    }

    private sealed record Playback(
        SoundPlayer Player,
        StreamDataProvider Provider,
        Stream Stream) : IDisposable
    {
        public void Dispose()
        {
            Player.Dispose();
            Provider.Dispose();
            Stream.Dispose();
        }
    }
}
