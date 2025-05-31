#nullable enable
using System;
using System.Collections.Immutable;
using System.Threading;

namespace Hikari;

public sealed partial class Shader
{
    private readonly Screen _screen;
    private readonly ImmutableArray<ShaderPassData> _shaderPassData;
    private EventSource<Shader> _disposed;
    private bool _released;
    private readonly Action<FrameObject, IMaterial>? _prepareForRender;

    public Event<Shader> Disposed => _disposed.Event;

    public Screen Screen => _screen;

    public ReadOnlySpan<ShaderPassData> ShaderPasses => _shaderPassData.AsSpan();

    [Owned(nameof(Release))]
    private Shader(Screen screen, ReadOnlySpan<ShaderPassDescriptor> passes, Action<FrameObject, IMaterial>? prepareForRender)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var array = new ShaderPassData[passes.Length];
        for(var i = 0; i < passes.Length; i++) {
            array[i] = passes[i].CreateShaderPassData(this, i, Disposed);
        }
        _shaderPassData = array.AsImmutableArray();
        _prepareForRender = prepareForRender;
    }

    private void Release()
    {
        Release(true);
    }

    private void Release(bool manualRelease)
    {
        if(Interlocked.Exchange(ref _released, true) == false) {
        }
    }

    internal void PrepareForRender(FrameObject frameObject, IMaterial material)
    {
        _prepareForRender?.Invoke(frameObject, material);
    }
}
