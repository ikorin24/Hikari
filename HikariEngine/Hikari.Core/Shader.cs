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

    public Event<Shader> Disposed => _disposed.Event;

    public Screen Screen => _screen;

    public ImmutableArray<ShaderPassData> ShaderPasses => _shaderPassData;

    [Owned(nameof(Release))]
    private Shader(Screen screen, ReadOnlySpan<ShaderPassDescriptor> passes)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var array = new ShaderPassData[passes.Length];
        for(var i = 0; i < passes.Length; i++) {
            array[i] = passes[i].CreateShaderPassData(this, i, _disposed.Event);
        }
        _shaderPassData = array.AsImmutableArray();
    }

    private void Release()
    {
        Release(true);
    }

    private void Release(bool manualRelease)
    {
        if(Interlocked.Exchange(ref _released, true) == false) {
            _disposed.Invoke(this);
        }
    }
}
