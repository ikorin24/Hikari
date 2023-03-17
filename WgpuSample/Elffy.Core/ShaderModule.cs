#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class ShaderModule : IEngineManaged
{
    private IHostScreen? _screen;
    private Rust.OptionBox<Wgpu.ShaderModule> _native;

    public IHostScreen? Screen => _screen;
    internal Rust.Ref<Wgpu.ShaderModule> NativeRef => _native.Unwrap();

    private ShaderModule(IHostScreen screen, Rust.Box<Wgpu.ShaderModule> native)
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
                _screen = null;
            }
        }
    }

    public static Own<ShaderModule> Create(IHostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shaderModuleNative = screen.AsRefChecked().CreateShaderModule(shaderSource);
        var shaderModule = new ShaderModule(screen, shaderModuleNative);
        return Own.RefType(shaderModule, static x => SafeCast.As<ShaderModule>(x).Release());
    }
}
