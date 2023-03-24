#nullable enable
using System;

namespace Elffy;

public abstract class Material<TSelf, TShader>
    where TSelf : Material<TSelf, TShader>
    where TShader : Shader<TShader, TSelf>
{
    private readonly TShader _shader;
    private readonly Own<BindGroup>[] _bindGroupOwns;
    private readonly BindGroup[] _bindGroups;

    public TShader Shader => _shader;
    public ReadOnlyMemory<BindGroup> BindGroups => _bindGroups;
    public Screen Screen => _shader.Screen;

    protected Material(TShader shader, Own<BindGroup>[] bindGroupOwns)
    {
        _shader = shader;
        _bindGroupOwns = bindGroupOwns;
        var bindGroups = new BindGroup[bindGroupOwns.Length];
        for(int i = 0; i < bindGroups.Length; i++) {
            bindGroups[i] = bindGroupOwns[i].AsValue();
        }
        _bindGroups = bindGroups;
    }

    private void Release()
    {
        Release(true);
    }

    protected virtual void Release(bool manualRelease)
    {
        if(manualRelease) {
            foreach(var item in _bindGroupOwns) {
                item.Dispose();
            }
        }
    }

    protected static Own<TSelf> CreateOwn(TSelf self)
    {
        ArgumentNullException.ThrowIfNull(self);
        return Own.RefType(self, static x => SafeCast.As<TSelf>(x).Release());
    }
}
