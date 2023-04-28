#nullable enable
using System;

namespace Elffy;

public abstract class Material<TSelf, TShader>
    : IScreenManaged
    where TSelf : Material<TSelf, TShader>
    where TShader : Shader<TShader, TSelf>
{
    private readonly TShader _shader;
    private bool _released;

    public TShader Shader => _shader;
    public Screen Screen => _shader.Screen;

    public bool IsManaged => _released == false;

    protected Material(TShader shader)
    {
        _shader = shader;
        _released = false;
    }

    private void Release()
    {
        _released = true;
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
    }

    protected static Own<TSelf> CreateOwn(TSelf self)
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
