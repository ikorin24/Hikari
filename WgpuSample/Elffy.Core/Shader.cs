#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class Shader : IEngineManaged
{
    private IHostScreen? _screen;
    private Rust.OptionBox<Wgpu.ShaderModule> _native;

    public IHostScreen? Screen => _screen;
    internal Rust.Ref<Wgpu.ShaderModule> NativeRef => _native.Unwrap();

    private Shader(IHostScreen screen, Rust.Box<Wgpu.ShaderModule> native)
    {
        _screen = screen;
        _native = native;
    }

    ~Shader() => Release(false);

    private static readonly Action<Shader> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
    {
        if(InterlockedEx.Exchange(ref _native, Rust.OptionBox<Wgpu.ShaderModule>.None).IsSome(out var native)) {
            native.DestroyShaderModule();
            if(disposing) {
                _screen = null;
            }
        }
    }

    public static Own<Shader> Create(IHostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shader = screen.AsRefChecked().CreateShaderModule(shaderSource);
        return Own.New(new Shader(screen, shader), _release);
    }
}
