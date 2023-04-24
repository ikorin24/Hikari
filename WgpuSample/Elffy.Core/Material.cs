#nullable enable
using System;

namespace Elffy;

public abstract class Material<TSelf, TShader>
    where TSelf : Material<TSelf, TShader>
    where TShader : Shader<TShader, TSelf>
{
    private readonly TShader _shader;

    public TShader Shader => _shader;
    public Screen Screen => _shader.Screen;

    protected Material(TShader shader)
    {
        _shader = shader;
    }

    private void Release()
    {
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
}
