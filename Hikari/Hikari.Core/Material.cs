#nullable enable
using System;

namespace Hikari;

public abstract class Material : IScreenManaged
{
    private readonly Shader _shader;
    private EventSource<Material> _disposed;
    private bool _released;

    public Shader Shader => _shader;
    public Screen Screen => _shader.Screen;

    public bool IsManaged => _released == false;

    public Event<Material> Disposed => _disposed.Event;

    protected Material(Shader shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        _shader = shader;
    }

    public abstract ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex);

    private void Release()
    {
        if(_released) {
            return;
        }
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
        if(manualRelease) {
            _disposed.Invoke(this);
        }
    }

    protected static Own<TSelf> CreateOwn<TSelf>(TSelf self) where TSelf : Material
    {
        ArgumentNullException.ThrowIfNull(self);
        return Own.New(self, static x => SafeCast.As<Material>(x).Release());
    }

    public virtual void Validate()
    {
        IScreenManaged.DefaultValidate(this);
        _shader.Validate();
    }
}
