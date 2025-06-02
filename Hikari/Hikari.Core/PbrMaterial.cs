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
        MaybeOwn<Texture2D> albedo,
        MaybeOwn<Texture2D> metallicRoughness,
        MaybeOwn<Texture2D> normal)
    {
        var screen = shader.Screen;
        var samplerOwn = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Linear,
        });
        return Create(
            shader,
            albedo, samplerOwn,
            metallicRoughness, samplerOwn.AsValue(),
            normal, samplerOwn.AsValue());
    }

    public static Own<PbrMaterial> Create(
        Shader shader,
        MaybeOwn<Texture2D> albedo,
        MaybeOwn<Sampler> albedoSampler,
        MaybeOwn<Texture2D> metallicRoughness,
        MaybeOwn<Sampler> metallicRoughnessSampler,
        MaybeOwn<Texture2D> normal,
        MaybeOwn<Sampler> normalSampler)
    {
        ArgumentNullException.ThrowIfNull(shader);
        albedo.ThrowArgumentExceptionIfNone();
        albedoSampler.ThrowArgumentExceptionIfNone();
        metallicRoughness.ThrowArgumentExceptionIfNone();
        metallicRoughnessSampler.ThrowArgumentExceptionIfNone();
        normal.ThrowArgumentExceptionIfNone();
        normalSampler.ThrowArgumentExceptionIfNone();

        var screen = shader.Screen;

        var disposables = new DisposableBag();
        var albedoValue = albedo.AddTo(disposables);
        var albedoSamplerValue = albedoSampler.AddTo(disposables);
        var metallicRoughnessValue = metallicRoughness.AddTo(disposables);
        var metallicRoughnessSamplerValue = metallicRoughnessSampler.AddTo(disposables);
        var normalValue = normal.AddTo(disposables);
        var normalSamplerValue = normalSampler.AddTo(disposables);

        var bindGroup1 = BindGroup.Create(screen, new()
        {
            Layout = shader.ShaderPasses[1].Pipeline.Layout.BindGroupLayouts[1],
            Entries =
            [
                BindGroupEntry.TextureView(0, albedoValue.View),
                BindGroupEntry.Sampler(1, albedoSamplerValue),
                BindGroupEntry.TextureView(2, metallicRoughnessValue.View),
                BindGroupEntry.Sampler(3, metallicRoughnessSamplerValue),
                BindGroupEntry.TextureView(4, normalValue.View),
                BindGroupEntry.Sampler(5, normalSamplerValue),
            ],
        }).AddTo(disposables);

        var material = new PbrMaterial(
            shader,
            albedoValue,
            albedoSamplerValue,
            metallicRoughnessValue,
            metallicRoughnessSamplerValue,
            normalValue,
            normalSamplerValue,
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
