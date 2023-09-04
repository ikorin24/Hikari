#nullable enable
using Hikari.NativeBind;
using System;

namespace Hikari;

public sealed class TextureView : IScreenManaged, ITextureView
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.TextureView> _native;
    private readonly Texture2D _texture;

    public Screen Screen => _screen;

    public bool IsManaged => _native.IsNone == false;

    public Texture2D Texture => _texture;

    internal Rust.Ref<Wgpu.TextureView> NativeRef => _native.Unwrap();

    public TextureViewHandle Handle => new TextureViewHandle(_native.Unwrap());

    private TextureView(Screen screen, Rust.Box<Wgpu.TextureView> native, Texture2D texture)
    {
        _screen = screen;
        _native = native;
        _texture = texture;
    }

    ~TextureView() => Release(false);

    public void Validate()
    {
        IScreenManaged.DefaultValidate(this);
        _texture.Validate();
    }

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool manualRelease)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.TextureView>.None).IsSome(out var native)) {
            native.DestroyTextureView();
            if(manualRelease) {
            }
        }
    }

    public static Own<TextureView> Create(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        texture.ThrowIfNotScreenManaged();
        var screen = texture.Screen;
        var textureViewNative = texture.NativeRef.CreateTextureView(CH.TextureViewDescriptor.Default);
        var textureView = new TextureView(screen, textureViewNative, texture);
        return Own.New(textureView, static x => SafeCast.As<TextureView>(x).Release());
    }
}
