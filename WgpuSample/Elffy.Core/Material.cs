#nullable enable
using System;
using System.Threading;

namespace Elffy;

public class Material
{
    private readonly Shader _shader;
    private readonly Own<BindGroup>[] _bindGroupOwns;
    private readonly BindGroup[] _bindGroups;
    private IDisposable?[]? _associates;

    public Shader Shader => _shader;
    public ReadOnlyMemory<BindGroup> BindGroups => _bindGroups;

    protected Material(Shader shader, Own<BindGroup>[] bindGroupOwns, IDisposable?[]? associates)
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

    internal static Own<Material> Create(Shader shader, ReadOnlySpan<BindGroupDescriptor> bindGroupDescs, IDisposable?[]? associates)
    {
        ArgumentNullException.ThrowIfNull(shader);

        var bindGroupOwns = new Own<BindGroup>[bindGroupDescs.Length];
        for(int i = 0; i < bindGroupDescs.Length; i++) {
            bindGroupOwns[i] = BindGroup.Create(shader.Screen, bindGroupDescs[i]);
        }
        return new Own<Material>(new Material(shader, bindGroupOwns, associates), static self => self.Release(true));
    }

    protected static Own<TMaterial> CreateOwn<TMaterial>(TMaterial self) where TMaterial : Material
    {
        ArgumentNullException.ThrowIfNull(self);
        return new Own<TMaterial>(self, static self => self.Release(true));
    }
}
