#nullable enable
using Elffy.NativeBind;
using System;

namespace Elffy;

public sealed class Shader : IEngineManaged
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

    ~Shader() => Release(false);

    private static readonly Action<Shader> _release = static self =>
    {
        self.Release(true);
        GC.SuppressFinalize(self);
    };

    private void Release(bool disposing)
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

    public static Own<Shader> Create(IHostScreen screen, ReadOnlySpan<byte> shaderSource)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var shader = screen.AsRefChecked().CreateShaderModule(shaderSource);
        return Own.New(new Shader(screen, shader), _release);
    }
}
