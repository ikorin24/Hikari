using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace CannonCape;

public static class AudioPlayer
{
    public static IAudioPlayer Play(string path)
    {
        var reader = new AudioFileReader(path);
        var waveOut = new WaveOutEvent();
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

    public static Task PlayAwaitable(string path)
    {
        var reader = new AudioFileReader(path);
        var waveOut = new WaveOutEvent();
        waveOut.Init(reader);
        var tcs = new TaskCompletionSource();
        waveOut.PlaybackStopped += (a, b) =>
        {
            reader.Dispose();
            waveOut.Dispose();
            tcs.TrySetResult();
        };
        waveOut.Play();
        return tcs.Task;
    }

    private sealed class SimplePlayer : IAudioPlayer
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
}

public interface IAudioPlayer : IDisposable
{
    event Action? Stopped;
}
