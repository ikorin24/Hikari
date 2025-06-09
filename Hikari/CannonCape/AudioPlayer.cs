using NAudio.Wave;
using System;
using System.Runtime.InteropServices;

namespace CannonCape;

public static class AudioPlayer
{
    private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static IDisposable Play(string path)
    {
        // 音声再生に使用している NAudio が Windows 以外非対応であるため、それ以外の環境では音声再生を行わない
        if(_isWindows == false) {
            return NonePlayer.Instance;
        }

        var reader = new AudioFileReader(path);
        var waveOut = new WaveOutEvent();
        waveOut.Volume = 1;
        waveOut.Init(reader);
        var player = new SimplePlayer(waveOut);
        waveOut.PlaybackStopped += (_, _) =>
        {
            reader.Dispose();
            waveOut.Dispose();
            player.InvokeStopped();
        };
        waveOut.Play();
        return player;
    }

    private sealed class SimplePlayer : IDisposable
    {
        private readonly WaveOutEvent _waveout;
        public event Action? Stopped;

        public SimplePlayer(WaveOutEvent waveout)
        {
            _waveout = waveout;
        }

        public void Dispose() => _waveout.Stop();

        public void InvokeStopped() => Stopped?.Invoke();
    }

    private sealed class NonePlayer : IDisposable
    {
        public static NonePlayer Instance { get; } = new NonePlayer();

        private NonePlayer()
        {
        }

        public void Dispose()
        {
        }
    }
}
