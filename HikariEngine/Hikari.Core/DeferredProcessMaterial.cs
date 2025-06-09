#nullable enable
using System;
using System.Collections.Immutable;

namespace Hikari;

public sealed class DeferredProcessMaterial : IMaterial
{
    private readonly DeferredProcessShader _shader;
    private ImmutableArray<BindGroupData> _pass0BindGroups;
    private EventSubscription<IGBufferProvider> _subscription;

    public DeferredProcessShader Shader => _shader;
    ITypedShader IMaterial.Shader => Shader;

    public Screen Screen => _shader.Screen;


    private DeferredProcessMaterial(DeferredProcessShader shader, IGBufferProvider gBuffer)
    {
        ArgumentNullException.ThrowIfNull(gBuffer);
        ArgumentNullException.ThrowIfNull(shader);
        _shader = shader;
        _subscription = gBuffer.Observe(gBuffer =>
        {
            var screen = gBuffer.Screen;
            _pass0BindGroups = [
                new BindGroupData(0, gBuffer.BindGroup),
                new BindGroupData(1, screen.Camera.CameraDataBindGroup),
                new BindGroupData(2, screen.Lights.DataBindGroup),
                new BindGroupData(3, screen.Lights.DirectionalLight.ShadowMapBindGroup),
            ];
        });
    }

    public void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        switch(passIndex) {
            case 0:
                renderPass.SetBindGroups(_pass0BindGroups);
                break;
            default:
                break;
        }
    }

    private void Release()
    {
        _subscription.Dispose();
    }

    internal static Own<DeferredProcessMaterial> Create(DeferredProcessShader shader, IGBufferProvider gBuffer)
    {
        var material = new DeferredProcessMaterial(shader, gBuffer);
        return Own.New(material, static x => SafeCast.As<DeferredProcessMaterial>(x).Release());
    }
}
