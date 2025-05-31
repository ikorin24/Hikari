#nullable enable
using Hikari.Internal;
using System;
using System.Collections.Immutable;
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
    private readonly DisposableBag _disposables;

    private readonly TypedOwnBuffer<UniformValue> _modelUniform;
    private readonly ImmutableArray<ImmutableArray<BindGroupData>> _passBindGroups;
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
        TypedOwnBuffer<UniformValue> uniform,
        Texture2D albedo, Sampler albedoSampler,
        Texture2D metallicRoughness, Sampler metallicRoughnessSampler,
        Texture2D normal, Sampler normalSampler,
        ImmutableArray<ImmutableArray<BindGroupData>> passBindGroups,
        DisposableBag disposables)
    {
        _shader = shader;
        _modelUniform = uniform;
        _albedo = albedo;
        _albedoSampler = albedoSampler;
        _metallicRoughness = metallicRoughness;
        _metallicRoughnessSampler = metallicRoughnessSampler;
        _normal = normal;
        _normalSampler = normalSampler;
        _passBindGroups = passBindGroups;
        _disposables = disposables;
    }

    public ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex)
    {
        return _passBindGroups[passIndex].AsSpan();
    }

    private void Release()
    {
        Release(true);
    }

    private void Release(bool manualRelease)
    {
        if(manualRelease) {
            _modelUniform.Dispose();
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
        var uniformBuffer = new TypedOwnBuffer<UniformValue>(screen, default, BufferUsages.Uniform | BufferUsages.CopyDst | BufferUsages.Storage);

        var disposables = new DisposableBag();
        var albedoValue = albedo.AddTo(disposables);
        var albedoSamplerValue = albedoSampler.AddTo(disposables);
        var metallicRoughnessValue = metallicRoughness.AddTo(disposables);
        var metallicRoughnessSamplerValue = metallicRoughnessSampler.AddTo(disposables);
        var normalValue = normal.AddTo(disposables);
        var normalSamplerValue = normalSampler.AddTo(disposables);

        var shadowBindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.ShaderPasses[0].Pipeline.Layout.BindGroupLayouts[0],
            Entries =
            [
                BindGroupEntry.Buffer(0, uniformBuffer),
            ],
        }).AddTo(disposables);

        var bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.ShaderPasses[1].Pipeline.Layout.BindGroupLayouts[0],
            Entries =
            [
                BindGroupEntry.Buffer(0, uniformBuffer),
                BindGroupEntry.TextureView(1, albedoValue.View),
                BindGroupEntry.Sampler(2, albedoSamplerValue),
                BindGroupEntry.TextureView(3, metallicRoughnessValue.View),
                BindGroupEntry.Sampler(4, metallicRoughnessSamplerValue),
                BindGroupEntry.TextureView(5, normalValue.View),
                BindGroupEntry.Sampler(6, normalSamplerValue),
            ],
        }).AddTo(disposables);

        ImmutableArray<ImmutableArray<BindGroupData>> passBindGroups =
        [
            [
                new BindGroupData(0, shadowBindGroup0),
                new BindGroupData(1, screen.Lights.DirectionalLight.RenderShadowBindGroup),
            ],
            [
                new BindGroupData(0, bindGroup0),
                new BindGroupData(1, screen.Camera.CameraDataBindGroup),
            ],
        ];

        var material = new PbrMaterial(
            shader,
            uniformBuffer,
            albedoValue,
            albedoSamplerValue,
            metallicRoughnessValue,
            metallicRoughnessSamplerValue,
            normalValue,
            normalSamplerValue,
            passBindGroups,
            disposables);
        return Own.New(material, static x => SafeCast.As<PbrMaterial>(x).Release());
    }

    internal void WriteModelUniform(in UniformValue value)
    {
        _modelUniform.WriteData(value);
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
