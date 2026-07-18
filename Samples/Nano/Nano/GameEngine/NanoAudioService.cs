using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace Nano.GameEngine;

/// <summary>Lazy, cross-platform audio playback backed by SoundFlow/miniaudio.</summary>
internal sealed class NanoAudioService(Func<string, byte[]> readAsset) : IDisposable
{
    private readonly Dictionary<int, Playback> _playbacks = [];
    private readonly List<int> _finishedHandles = [];
    private AudioEngine? _engine;
    private AudioPlaybackDevice? _device;
    private int _nextHandle;

    public int ActivePlaybackCount => _playbacks.Count;

    public void Update() => CleanupFinished();

    public int Play(string path, float volume, bool loop)
    {
        CleanupFinished();
        EnsureDevice();
        var stream = new MemoryStream(readAsset(path), writable: false);
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
        StopAll();
        _device?.Stop();
        _device?.Dispose();
        _engine?.Dispose();
    }

    private void EnsureDevice()
    {
        if (_device is not null)
            return;

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
