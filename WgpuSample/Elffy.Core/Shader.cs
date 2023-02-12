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

    ~Shader() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        var native = Box.SwapClear(ref _native);
        if(native.IsInvalid) {
            return;
        }
        native.DestroyShaderModule();
        if(disposing) {
            _screen = null;
        }
    }

    public static Shader Create(IHostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shader = screen.AsRefChecked().CreateShaderModule(shaderSource);
        return new Shader(screen, shader);
    }
}
