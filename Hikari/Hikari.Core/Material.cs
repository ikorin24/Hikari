#nullable enable
using System;

namespace Hikari;

public abstract class Material<TSelf, TShader, TOperation>
    : IScreenManaged
    where TSelf : Material<TSelf, TShader, TOperation>
    where TShader : Shader<TShader, TSelf, TOperation>
    where TOperation : RenderOperation<TOperation, TShader, TSelf>
{
    private readonly TShader _shader;
    private bool _released;

    public TOperation Operation => _shader.Operation;
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
