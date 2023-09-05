#nullable enable
using Hikari.NativeBind;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public sealed class RenderTextureProvider : ITexture2D, IDisposable
{
    private Own<Texture2D> _currentOwn;
    private Texture2D? _current;

    public uint MipLevelCount => _current?.MipLevelCount ?? ThrowAlreadyDisposed<uint>();
    public uint SampleCount => _current?.SampleCount ?? ThrowAlreadyDisposed<uint>();
    public TextureFormat Format => _current?.Format ?? ThrowAlreadyDisposed<TextureFormat>();
    public TextureUsages Usage => _current?.Usage ?? ThrowAlreadyDisposed<TextureUsages>();
    public TextureDimension Dimension => _current?.Dimension ?? ThrowAlreadyDisposed<TextureDimension>();

    public Vector2u Size => _current?.Size ?? ThrowAlreadyDisposed<Vector2u>();

    internal Rust.Ref<Wgpu.Texture> NativeRef
    {
        get
        {
            var current = _current;
            if(current == null) {
                ThrowHelper.ThrowInvalidOperation("already disposed");
            }
            return current.NativeRef;
        }
    }

    internal Rust.Ref<Wgpu.TextureView> ViewNativeRef
    {
        get
        {
            var current = _current;
            if(current == null) {
                ThrowHelper.ThrowInvalidOperation("already disposed");
            }
            return current.View.NativeRef;
        }
    }

    Rust.Ref<Wgpu.Texture> ITexture.NativeRef => NativeRef;

    Rust.Ref<Wgpu.TextureView> ITextureView.ViewNativeRef => ViewNativeRef;

    public RenderTextureProvider(Screen screen, in Texture2DDescriptor desc)
    {
        _currentOwn = Texture2D.Create(screen, desc);
        _current = _currentOwn.AsValue();
    }

    public bool Resize(Vector2u size)
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
