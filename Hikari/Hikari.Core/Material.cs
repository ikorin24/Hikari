#nullable enable
using System;

namespace Hikari;

public abstract class Material<TSelf, TShader>
    : IScreenManaged
    where TSelf : Material<TSelf, TShader>
    where TShader : Shader<TShader, TSelf>
{
    private readonly TShader _shader;
    private EventSource<TSelf> _disposed;
    private bool _released;

    public Event<TSelf> Disposed => _disposed.Event;
    public TShader Shader => _shader;
    public Screen Screen => _shader.Screen;

    public bool IsManaged => _released == false;

    public abstract ReadOnlySpan<MaterialPassData> Passes { get; }

    protected Material(TShader shader)
    {
        if(this is not TSelf) {
            throw new InvalidOperationException("invalid self type");
        }
        _shader = shader;
        _released = false;
    }

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
            _disposed.Invoke(SafeCast.As<TSelf>(this));
        }
    }

    protected static Own<T> CreateOwn<T>(T self) where T : TSelf
    {
        ArgumentNullException.ThrowIfNull(self);
        return Own.New(self, static x => SafeCast.As<TSelf>(x).Release());
    }

    public virtual void Validate()
    {
        IScreenManaged.DefaultValidate(this);
        _shader.Validate();
    }
}
