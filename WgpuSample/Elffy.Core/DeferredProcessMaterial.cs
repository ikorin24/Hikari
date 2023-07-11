#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader, DeferredProcess>
{
    private readonly BindGroup _bindGroup0;
    private readonly BindGroup _bindGroup1;
    private readonly BindGroup _bindGroup2;
    private readonly BindGroup _bindGroup3;
    private readonly IDisposable[] _disposables;

    internal BindGroup BindGroup0 => _bindGroup0;
    internal BindGroup BindGroup1 => _bindGroup1;
    internal BindGroup BindGroup2 => _bindGroup2;
    internal BindGroup BindGroup3 => _bindGroup3;

    private DeferredProcessMaterial(DeferredProcessShader shader, GBuffer gBuffer) : base(shader)
    {
        var bindGroups = shader.CreateBindGroups(gBuffer, out var disposables);
        Debug.Assert(bindGroups.Length == 4);
        _bindGroup0 = bindGroups[0];
        _bindGroup1 = bindGroups[1];
        _bindGroup2 = bindGroups[2];
        _bindGroup3 = bindGroups[3];
        _disposables = disposables;
    }

    public override void Validate()
    {
        base.Validate();
        _bindGroup0.Validate();
        _bindGroup1.Validate();
        _bindGroup2.Validate();
        _bindGroup3.Validate();
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            foreach(var item in _disposables) {
                item.Dispose();
            }
        }
    }

    internal static Own<DeferredProcessMaterial> Create(DeferredProcessShader shader, GBuffer gBuffer)
    {
        var material = new DeferredProcessMaterial(shader, gBuffer);
        return CreateOwn(material);
    }
}
