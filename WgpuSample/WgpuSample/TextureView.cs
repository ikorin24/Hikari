#nullable enable
using Elffy.Bind;
using System;

namespace Elffy;

public sealed class TextureView : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.TextureView> _native;
    private Texture? _texture;

    public IHostScreen? Screen => _screen;

    internal Ref<Wgpu.TextureView> NativeRef => _native;

    private TextureView(IHostScreen screen, Box<Wgpu.TextureView> native, Texture texture)
    {
        _screen = screen;
        _texture = texture;
        _native = native;
    }

    public static TextureView Create(Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        texture.ThrowIfNotEngineManaged();
        var screen = texture.GetScreen();
        var view = texture.NativeRef.CreateTextureView();
        return new TextureView(screen, view, texture);
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _texture = null;
        _native.DestroyTextureView();
        _native = Box<Wgpu.TextureView>.Invalid;
        _screen = null;
    }
}
