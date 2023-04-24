#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader>
{
    private readonly BindGroup _bindGroup0;
    private readonly BindGroup _bindGroup1;
    private readonly BindGroup _bindGroup2;
    private readonly IDisposable[] _disposables;

    internal BindGroup BindGroup0 => _bindGroup0;
    internal BindGroup BindGroup1 => _bindGroup1;
    internal BindGroup BindGroup2 => _bindGroup2;

    private DeferredProcessMaterial(DeferredProcessShader shader, GBuffer gBuffer) : base(shader)
    {
        var bindGroups = shader.CreateBindGroups(gBuffer, out var disposables);
        Debug.Assert(bindGroups.Length == 3);
        _bindGroup2 = bindGroups[2];
        _bindGroup0 = bindGroups[0];
        _bindGroup1 = bindGroups[1];
        _disposables = disposables;
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
