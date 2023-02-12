#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class TextureView : IEngineManaged
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

    ~TextureView() => Release(false);

    private static readonly Action<TextureView> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool manualRelease)
    {
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        native.DestroyTextureView();
        if(manualRelease) {
            _screen = null;
            _texture = null;
        }
    }

    public static Own<TextureView> Create(Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        texture.ThrowIfNotEngineManaged();
        var screen = texture.GetScreen();
        var view = texture.NativeRef.CreateTextureView();
        return Own.New(new TextureView(screen, view, texture), _release);
    }
}
