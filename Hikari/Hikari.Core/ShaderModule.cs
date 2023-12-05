#nullable enable
using Hikari.NativeBind;
using System;

namespace Hikari;

public sealed partial class ShaderModule : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.ShaderModule> _native;

    public Screen Screen => _screen;
    internal Rust.Ref<Wgpu.ShaderModule> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    [Owned(nameof(Release))]
    private ShaderModule(Screen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _native = screen.AsRefChecked().CreateShaderModule(shaderSource);
        _screen = screen;
    }

    ~ShaderModule() => Release(false);

    public void Validate() => IScreenManaged.DefaultValidate(this);

    private void Release()
    {
        Release(true);
        GC.SuppressFinalize(this);
    }

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.ShaderModule>.None).IsSome(out var native)) {
            native.DestroyShaderModule();
            if(disposing) {
            }
        }
    }
}
