#nullable enable
using System;
using System.Diagnostics;

namespace Elffy;

public sealed class DeferredProcessMaterial : Material<DeferredProcessMaterial, DeferredProcessShader>
{
    private readonly BindGroup[] _bindGroups;
    private readonly IDisposable[] _disposables;

    internal BindGroup BindGroup0 => _bindGroups[0];
    internal BindGroup BindGroup1 => _bindGroups[1];

    private DeferredProcessMaterial(DeferredProcessShader shader, GBuffer gBuffer) : base(shader)
    {
        var bindGroups = shader.CreateBindGroups(gBuffer, out var disposables);
        Debug.Assert(bindGroups.Length == 2);
        _bindGroups = bindGroups;
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
