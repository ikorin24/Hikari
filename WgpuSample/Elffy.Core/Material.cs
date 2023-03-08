#nullable enable
using System;

namespace Elffy;

public sealed class Material
{
    private readonly Shader _shader;
    private readonly Own<BindGroup>[] _bindGroupOwns;
    private readonly BindGroup[] _bindGroups;

    public Shader Shader => _shader;
    public ReadOnlyMemory<BindGroup> BindGroups => _bindGroups;

    private Material(Shader shader, Own<BindGroup>[] bindGroupOwns)
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
        foreach(var item in _bindGroupOwns) {
            item.Dispose();
        }
    }

    internal static Own<Material> Create(Shader shader, in BindGroupDescriptor bindGroupDesc)
    {
        return Create(shader, new ReadOnlySpan<BindGroupDescriptor>(in bindGroupDesc));
    }

    internal static Own<Material> Create(Shader shader, ReadOnlySpan<BindGroupDescriptor> bindGroupDescs)
    {
        ArgumentNullException.ThrowIfNull(shader);

        var bindGroupOwns = new Own<BindGroup>[bindGroupDescs.Length];
        for(int i = 0; i < bindGroupDescs.Length; i++) {
            bindGroupOwns[i] = BindGroup.Create(shader.Screen, bindGroupDescs[i]);
        }
        return new Own<Material>(new Material(shader, bindGroupOwns), static self => self.Release());
    }
}
