#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public abstract class Shader : IScreenManaged
{
    private readonly Screen _screen;
    private readonly ImmutableArray<MaterialPassData> _materialPassData;
    private EventSource<Shader> _disposed;
    private bool _released;

    public Event<Shader> Disposed => _disposed.Event;

    public bool IsManaged => _released == false;

    public Screen Screen => _screen;

    public ReadOnlySpan<MaterialPassData> MaterialPassData => _materialPassData.AsSpan();

    protected Shader(Screen screen, in ShaderPassDescriptorArray1 passes)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _materialPassData = [
            passes.Pass0.CreateMaterialPassData(this, 0, Disposed),
        ];
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray2 passes)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _materialPassData = [
            passes.Pass0.CreateMaterialPassData(this, 0, Disposed),
            passes.Pass1.CreateMaterialPassData(this, 1, Disposed),
        ];
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray3 passes)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _materialPassData = [
            passes.Pass0.CreateMaterialPassData(this, 0, Disposed),
            passes.Pass1.CreateMaterialPassData(this, 1, Disposed),
            passes.Pass2.CreateMaterialPassData(this, 2, Disposed),
        ];
    }

    protected Shader(Screen screen, in ShaderPassDescriptorArray4 passes)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _materialPassData = [
            passes.Pass0.CreateMaterialPassData(this, 0, Disposed),
            passes.Pass1.CreateMaterialPassData(this, 1, Disposed),
            passes.Pass2.CreateMaterialPassData(this, 2, Disposed),
            passes.Pass3.CreateMaterialPassData(this, 3, Disposed),
        ];
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
}
