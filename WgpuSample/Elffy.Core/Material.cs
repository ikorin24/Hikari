#nullable enable
using System;
using System.Threading;

namespace Elffy;

public abstract class Material<TSelf, TShader>
    where TSelf : Material<TSelf, TShader>
    where TShader : Shader<TShader, TSelf>
{
    private readonly TShader _shader;
    private readonly Own<BindGroup>[] _bindGroupOwns;
    private readonly BindGroup[] _bindGroups;
    private IDisposable?[]? _associates;

    public TShader Shader => _shader;
    public ReadOnlyMemory<BindGroup> BindGroups => _bindGroups;
    public Screen Screen => _shader.Screen;

    protected Material(TShader shader, Own<BindGroup>[] bindGroupOwns, IDisposable?[]? associates)
    {
        _shader = shader;
        _bindGroupOwns = bindGroupOwns;
        var bindGroups = new BindGroup[bindGroupOwns.Length];
        for(int i = 0; i < bindGroups.Length; i++) {
            bindGroups[i] = bindGroupOwns[i].AsValue();
        }
        _bindGroups = bindGroups;
        _associates = associates;
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
            var associates = Interlocked.Exchange(ref _associates, null);
            if(associates != null) {
                foreach(var associate in associates) {
                    associate?.Dispose();
                }
            }
        }
    }

    //internal static Own<Material> Create(Shader shader, ReadOnlySpan<BindGroupDescriptor> bindGroupDescs, IDisposable?[]? associates)
    //{
    //    ArgumentNullException.ThrowIfNull(shader);

    //    var bindGroupOwns = new Own<BindGroup>[bindGroupDescs.Length];
    //    for(int i = 0; i < bindGroupDescs.Length; i++) {
    //        bindGroupOwns[i] = BindGroup.Create(shader.Screen, bindGroupDescs[i]);
    //    }
    //    var material = new Material(shader, bindGroupOwns, associates);
    //    return Own.RefType(material, static x => SafeCast.As<Material>(x).Release());
    //}

    protected static Own<TSelf> CreateOwn(TSelf self)
    {
        ArgumentNullException.ThrowIfNull(self);
        return Own.RefType(self, static x => SafeCast.As<TSelf>(x).Release());
    }
}
