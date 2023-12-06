#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public abstract class Shader : IScreenManaged
{
    private readonly Screen _screen;
    private readonly ImmutableArray<ShaderPassData> _materialPassData;
    private EventSource<Shader> _disposed;
    private bool _released;
    private readonly Action<FrameObject, Material>? _prepareForRender;

    public Event<Shader> Disposed => _disposed.Event;

    public bool IsManaged => _released == false;

    public Screen Screen => _screen;

    public ReadOnlySpan<ShaderPassData> ShaderPasses => _materialPassData.AsSpan();

    protected Shader(Screen screen, ReadOnlySpan<ShaderPassDescriptor> passes, Action<FrameObject, Material>? prepareForRender)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        var array = new ShaderPassData[passes.Length];
        for(var i = 0; i < passes.Length; i++) {
            array[i] = passes[i].CreateMaterialPassData(this, i, Disposed);
        }
        _materialPassData = array.AsImmutableArray();
        _prepareForRender = prepareForRender;
    }

    private void Release()
    {
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
    }

    protected static Own<T> CreateOwn<T>(T shader) where T : Shader
    {
        ArgumentNullException.ThrowIfNull(shader);
        return Own.New(shader, static x => SafeCast.As<Shader>(x).Release());
    }

    public virtual void Validate()
    {
        IScreenManaged.DefaultValidate(this);
    }

    internal void PrepareForRender(FrameObject frameObject, Material material)
    {
        _prepareForRender?.Invoke(frameObject, material);
    }
}
