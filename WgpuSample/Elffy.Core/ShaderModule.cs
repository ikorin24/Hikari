#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class ShaderModule : IEngineManaged
{
    private readonly HostScreen _screen;
    private Rust.OptionBox<Wgpu.ShaderModule> _native;

    public HostScreen Screen => _screen;
    internal Rust.Ref<Wgpu.ShaderModule> NativeRef => _native.Unwrap();

    public bool IsManaged => _native.IsNone == false;

    private ShaderModule(HostScreen screen, Rust.Box<Wgpu.ShaderModule> native)
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

    public static Own<ShaderModule> Create(HostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shaderModuleNative = screen.AsRefChecked().CreateShaderModule(shaderSource);
        var shaderModule = new ShaderModule(screen, shaderModuleNative);
        return Own.RefType(shaderModule, static x => SafeCast.As<ShaderModule>(x).Release());
    }
}
