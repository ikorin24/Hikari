#nullable enable
using Hikari.Internal;
using System;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class PbrMaterial : IMaterial
{
    private readonly Shader _shader;
    private readonly Texture2D _albedo;
    private readonly Texture2D _metallicRoughness;
    private readonly Texture2D _normal;
    private readonly Sampler _albedoSampler;
    private readonly Sampler _metallicRoughnessSampler;
    private readonly Sampler _normalSampler;
    private readonly BindGroup _bindGroup1;
    private readonly DisposableBag _disposables;

    private EventSource<PbrMaterial> _disposed;

    public Event<PbrMaterial> Disposed => _disposed.Event;
    public Shader Shader => _shader;
    public Screen Screen => _shader.Screen;
    public Texture2D Albedo => _albedo;
    public Texture2D MetallicRoughness => _metallicRoughness;
    public Texture2D Normal => _normal;

    public Sampler AlbedoSampler => _albedoSampler;
    public Sampler MetallicRoughnessSampler => _metallicRoughnessSampler;
    public Sampler NormalSampler => _normalSampler;

    private PbrMaterial(
        Shader shader,
        Texture2D albedo, Sampler albedoSampler,
        Texture2D metallicRoughness, Sampler metallicRoughnessSampler,
        Texture2D normal, Sampler normalSampler,
        BindGroup bindGroup1,
        DisposableBag disposables)
    {
        _shader = shader;
        _albedo = albedo;
        _albedoSampler = albedoSampler;
        _metallicRoughness = metallicRoughness;
        _metallicRoughnessSampler = metallicRoughnessSampler;
        _normal = normal;
        _normalSampler = normalSampler;
        _bindGroup1 = bindGroup1;
        _disposables = disposables;
    }

    private void Release()
    {
        Release(true);
    }

    private void Release(bool manualRelease)
    {
        if(manualRelease) {
            _disposables.Dispose();
            _disposed.Invoke(this);
        }
    }

    public static Own<PbrMaterial> Create(
        Shader shader,
        Texture2D albedo,
        Texture2D metallicRoughness,
        Texture2D normal)
    {
        var screen = shader.Screen;
        var sampler = screen.UtilResource.LinearSampler;
        return Create(
            shader,
            albedo, sampler,
            metallicRoughness, sampler,
            normal, sampler);
    }

    public static Own<PbrMaterial> Create(
        Shader shader,
        Texture2D albedo,
        Sampler albedoSampler,
        Texture2D metallicRoughness,
        Sampler metallicRoughnessSampler,
        Texture2D normal,
        Sampler normalSampler)
    {
        ArgumentNullException.ThrowIfNull(shader);
        ArgumentNullException.ThrowIfNull(albedo);
        ArgumentNullException.ThrowIfNull(albedoSampler);
        ArgumentNullException.ThrowIfNull(metallicRoughness);
        ArgumentNullException.ThrowIfNull(metallicRoughnessSampler);
        ArgumentNullException.ThrowIfNull(normal);
        ArgumentNullException.ThrowIfNull(normalSampler);

        var screen = shader.Screen;
        var disposables = new DisposableBag();
        var bindGroup1 = BindGroup.Create(screen, new()
        {
            Layout = shader.ShaderPasses[1].Pipeline.Layout.BindGroupLayouts[1],
            Entries =
            [
                BindGroupEntry.TextureView(0, albedo.View),
                BindGroupEntry.Sampler(1, albedoSampler),
                BindGroupEntry.TextureView(2, metallicRoughness.View),
                BindGroupEntry.Sampler(3, metallicRoughnessSampler),
                BindGroupEntry.TextureView(4, normal.View),
                BindGroupEntry.Sampler(5, normalSampler),
            ],
        }).AddTo(disposables);

        var material = new PbrMaterial(
            shader,
            albedo,
            albedoSampler,
            metallicRoughness,
            metallicRoughnessSampler,
            normal,
            normalSampler,
            bindGroup1,
            disposables);
        return Own.New(material, static x => SafeCast.As<PbrMaterial>(x).Release());
    }

    public void SetBindGroupsTo(in RenderPass renderPass, int passIndex, Renderer renderer)
    {
        var screen = Screen;
        switch(passIndex) {
            case 0:
                renderPass.SetBindGroup(0, renderer.ModelDataBindGroup);
                renderPass.SetBindGroup(1, screen.Lights.DirectionalLight.RenderShadowBindGroup);
                break;
            case 1:
                renderPass.SetBindGroup(0, renderer.ModelDataBindGroup);
                renderPass.SetBindGroup(1, _bindGroup1);
                renderPass.SetBindGroup(2, screen.Camera.CameraDataBindGroup);
                break;
            default:
                break;
        }
    }

    [BufferDataStruct]
    internal partial struct UniformValue
    {
        [FieldOffset(OffsetOf.Model)]
        public required Matrix4 Model;
        [FieldOffset(OffsetOf.IsUniformScale)]
        public required int IsUniformScale;  // true: 1, false: 0
    }
}
