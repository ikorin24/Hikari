#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class ShaderModule : IScreenManaged
{
    private readonly Screen _screen;
    private Rust.OptionBox<Wgpu.ShaderModule> _native;

    public Screen Screen => _screen;
    internal Rust.Ref<Wgpu.ShaderModule> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private ShaderModule(Screen screen, Rust.Box<Wgpu.ShaderModule> native)
    {
        _screen = screen;
        _native = native;
    }

    ~ShaderModule() => Release(false);

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

    public static Own<ShaderModule> Create(Screen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shaderModuleNative = screen.AsRefChecked().CreateShaderModule(shaderSource);
        var shaderModule = new ShaderModule(screen, shaderModuleNative);
        return Own.New(shaderModule, static x => SafeCast.As<ShaderModule>(x).Release());
    }
}
