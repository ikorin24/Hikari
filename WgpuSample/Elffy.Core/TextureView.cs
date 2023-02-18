#nullable enable
using Elffy;
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class TextureView : IEngineManaged, ITextureView
{
    private IHostScreen? _screen;
    private Rust.OptionBox<Wgpu.TextureView> _native;

    public IHostScreen? Screen => _screen;

    internal Rust.Ref<Wgpu.TextureView> NativeRef => _native.Unwrap();

    public TextureViewHandle Handle => new TextureViewHandle(_native.Unwrap());

    private TextureView(IHostScreen screen, Rust.Box<Wgpu.TextureView> native, Texture texture)
    {
        _screen = screen;
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
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.TextureView>.None).IsSome(out var native)) {
            native.DestroyTextureView();
            if(manualRelease) {
                _screen = null;
            }
        }
    }

    public static Own<TextureView> Create(Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        texture.ThrowIfNotEngineManaged();
        var screen = texture.GetScreen();
        var view = texture.NativeRef.CreateTextureView(CE.TextureViewDescriptor.Default);
        return Own.New(new TextureView(screen, view, texture), _release);
    }
}
