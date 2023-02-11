#nullable enable
using Elffy.Bind;
using System;

namespace Elffy;

public sealed class Shader : IEngineManaged, IDisposable
{
    private IHostScreen? _screen;
    private Box<Wgpu.ShaderModule> _native;

    public IHostScreen? Screen => _screen;
    internal Ref<Wgpu.ShaderModule> NativeRef => _native;

    private Shader(IHostScreen screen, Box<Wgpu.ShaderModule> native)
    {
        _screen = screen;
        _native = native;
    }

    public static Shader Create(IHostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shader = screen.AsRefChecked().CreateShaderModule(shaderSource);
        return new Shader(screen, shader);
    }

    public void Dispose()
    {
        if(_native.IsInvalid) {
            return;
        }
        _native.DestroyShaderModule();
        _native = Box<Wgpu.ShaderModule>.Invalid;
        _screen = null;
    }
}
