#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public sealed class RenderTextureProvider : IRenderTextureProvider, IDisposable
{
    private Own<Texture2D> _currentOwn;
    private Texture2D? _current;
    private EventSource<Texture2D> _changed;

    public Event<Texture2D> Changed => _changed.Event;

    public uint MipLevelCount => _current?.MipLevelCount ?? ThrowAlreadyDisposed<uint>();
    public uint SampleCount => _current?.SampleCount ?? ThrowAlreadyDisposed<uint>();
    public TextureFormat Format => _current?.Format ?? ThrowAlreadyDisposed<TextureFormat>();
    public TextureUsages Usage => _current?.Usage ?? ThrowAlreadyDisposed<TextureUsages>();
    public TextureDimension Dimension => _current?.Dimension ?? ThrowAlreadyDisposed<TextureDimension>();

    public RenderTextureProvider(Screen screen, in Texture2DDescriptor desc)
    {
        _currentOwn = Texture2D.Create(screen, desc);
        _current = _currentOwn.AsValue();
    }

    public Texture2D GetCurrent()
    {
        var current = _current;
        if(current == null) {
            ThrowHelper.ThrowInvalidOperation("already disposed");
        }
        return current;
    }

    public bool Resize(Vector2u size)
    {
        var texture = _currentOwn.AsValue();
        if(texture.Dimension != TextureDimension.D2) {
            ThrowHelper.ThrowInvalidOperation("invalid texture dimension");
        }
        return ResizePrivate(size);
    }

    private bool ResizePrivate(Vector2u size)
    {
        var texture = _currentOwn.AsValue();
        if(texture.Size == size) {
            return false;
        }
        var desc = texture.GetDescriptor() with
        {
            Size = size,
        };
        var newTexture = Texture2D.Create(texture.Screen, desc).AsValue(out var newTextureOwn);
        _currentOwn.Dispose();
        _currentOwn = newTextureOwn;
        _current = newTexture;
        _changed.Invoke(newTexture);
        return true;
    }

    public void Dispose()
    {
        _currentOwn.Dispose();
        _current = null;
    }

    [DebuggerHidden]
    [DoesNotReturn]
    private static T ThrowAlreadyDisposed<T>()
    {
        ThrowHelper.ThrowInvalidOperation("already disposed");
        return default!;
    }
}
