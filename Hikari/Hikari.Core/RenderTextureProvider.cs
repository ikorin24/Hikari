﻿#nullable enable
using Hikari.NativeBind;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hikari;

public sealed partial class RenderTextureProvider : ITexture2DProvider
{
    private Own<Texture2D> _currentOwn;
    private Texture2D? _current;
    private EventSource<ITexture2DProvider> _textureChanged;
    private EventSource<ITextureViewProvider> _textureViewChanged;

    public uint MipLevelCount => _current?.MipLevelCount ?? ThrowAlreadyDisposed<uint>();
    public uint SampleCount => _current?.SampleCount ?? ThrowAlreadyDisposed<uint>();
    public TextureFormat Format => _current?.Format ?? ThrowAlreadyDisposed<TextureFormat>();
    public TextureUsages Usage => _current?.Usage ?? ThrowAlreadyDisposed<TextureUsages>();
    public TextureDimension Dimension => _current?.Dimension ?? ThrowAlreadyDisposed<TextureDimension>();

    public Event<ITexture2DProvider> TextureChanged => _textureChanged.Event;
    public Event<ITextureViewProvider> TextureViewChanged => _textureViewChanged.Event;

    internal Rust.Ref<Wgpu.Texture> GetCurrentTexture()
    {
        var current = _current;
        if(current == null) {
            ThrowHelper.ThrowInvalidOperation("already disposed");
        }
        return current.NativeRef;
    }

    internal Rust.Ref<Wgpu.TextureView> GetCurrentTextureView()
    {
        var current = _current;
        if(current == null) {
            ThrowHelper.ThrowInvalidOperation("already disposed");
        }
        return current.View.NativeRef;
    }

    Rust.Ref<Wgpu.Texture> ITextureProvider.GetCurrentTexture() => GetCurrentTexture();

    Rust.Ref<Wgpu.TextureView> ITextureViewProvider.GetCurrentTextureView() => GetCurrentTextureView();

    [Owned(nameof(Release))]
    private RenderTextureProvider(Screen screen, in Texture2DDescriptor desc)
    {
        _currentOwn = Texture2D.Create(screen, desc);
        _current = _currentOwn.AsValue();
    }

    [Owned(nameof(Release))]
    private RenderTextureProvider(Screen screen)
    {
        throw null!;
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
        _textureChanged.Invoke(this);
        _textureViewChanged.Invoke(this);
        return true;
    }

    private void Release()
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

    public Vector2u GetCurrentSize()
    {
        return _current?.Size ?? ThrowAlreadyDisposed<Vector2u>();
    }

    uint ITexture2DProvider.GetCurrentMipLevelCount() => MipLevelCount;

    uint ITexture2DProvider.GetCurrentSampleCount() => SampleCount;

    TextureFormat ITexture2DProvider.GetCurrentFormat() => Format;

    TextureUsages ITexture2DProvider.GetCurrentUsage() => Usage;

    TextureDimension ITexture2DProvider.GetCurrentDimension() => Dimension;
}

//partial class RenderTextureProvider
//{
//    public static Own<RenderTextureProvider> Create(Screen screen, in Texture2DDescriptor desc)
//    {
//        var self = new RenderTextureProvider(screen, desc);
//        return Own.New(self, static self =>
//        {
//            SafeCast.As<RenderTextureProvider>(self).Release();
//        });
//    }
//}

//[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
//[global::System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
//internal sealed class OwnedAttribute : global::System.Attribute
//{
//    public OwnedAttribute(string releaseMethod)
//    {
//    }
//}
