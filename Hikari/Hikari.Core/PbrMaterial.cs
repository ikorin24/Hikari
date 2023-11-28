#nullable enable
using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Hikari;

public sealed partial class PbrMaterial : Material
{
    private readonly MaybeOwn<Texture2D> _albedo;
    private readonly MaybeOwn<Texture2D> _metallicRoughness;
    private readonly MaybeOwn<Texture2D> _normal;
    private readonly MaybeOwn<Sampler> _albedoSampler;
    private readonly MaybeOwn<Sampler> _metallicRoughnessSampler;
    private readonly MaybeOwn<Sampler> _normalSampler;

    private readonly TypedOwnBuffer<UniformValue> _modelUniform;
    private readonly Own<BindGroup> _bindGroup0;
    private readonly BindGroup _bindGroup1;
    private readonly Own<BindGroup> _shadowBindGroup0;
    private readonly ImmutableArray<ImmutableArray<BindGroupData>> _passBindGroups;

    public Texture2D Albedo => _albedo.AsValue();
    public Texture2D MetallicRoughness => _metallicRoughness.AsValue();
    public Texture2D Normal => _normal.AsValue();

    public Sampler AlbedoSampler => _albedoSampler.AsValue();
    public Sampler MetallicRoughnessSampler => _metallicRoughnessSampler.AsValue();
    public Sampler NormalSampler => _normalSampler.AsValue();

    private PbrMaterial(
        PbrShader shader,
        TypedOwnBuffer<UniformValue> uniform,
        MaybeOwn<Texture2D> albedo,
        MaybeOwn<Sampler> albedoSampler,
        MaybeOwn<Texture2D> metallicRoughness,
        MaybeOwn<Sampler> metallicRoughnessSampler,
        MaybeOwn<Texture2D> normal,
        MaybeOwn<Sampler> normalSampler,
        Own<BindGroup> bindGroup0,
        Own<BindGroup> shadowBindGroup0)
        : base(shader)
    {
        _modelUniform = uniform;
        _albedo = albedo;
        _albedoSampler = albedoSampler;
        _metallicRoughness = metallicRoughness;
        _metallicRoughnessSampler = metallicRoughnessSampler;
        _normal = normal;
        _normalSampler = normalSampler;
        _bindGroup0 = bindGroup0;
        _bindGroup1 = Screen.Camera.CameraDataBindGroup;
        _shadowBindGroup0 = shadowBindGroup0;
        _passBindGroups = [
            [new(0, shadowBindGroup0.AsValue())],
            [new(0, _bindGroup0.AsValue()), new(1, _bindGroup1)],
        ];
    }

    public override void Validate()
    {
        base.Validate();
        _albedo.Validate();
        _metallicRoughness.Validate();
        _normal.Validate();
        _bindGroup1.Validate();
    }

    public override ReadOnlySpan<BindGroupData> GetBindGroups(int passIndex)
    {
        return _passBindGroups[passIndex].AsSpan();
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _modelUniform.Dispose();
            _albedo.Dispose();
            _albedoSampler.Dispose();
            _metallicRoughness.Dispose();
            _metallicRoughnessSampler.Dispose();
            _normal.Dispose();
            _normalSampler.Dispose();
            _bindGroup0.Dispose();
            _shadowBindGroup0.Dispose();
        }
    }

    public static Own<PbrMaterial> Create(
        PbrShader shader,
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
        PbrShader shader,
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
        var passes = shader.ShaderPasses;
        var lights = screen.Lights;
        var uniformBuffer = new TypedOwnBuffer<UniformValue>(screen, default, BufferUsages.Uniform | BufferUsages.CopyDst | BufferUsages.Storage);
        var bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = passes[1].Pipeline.Layout.BindGroupLayouts[0],
            Entries =
            [
                BindGroupEntry.Buffer(0, uniformBuffer),
                BindGroupEntry.TextureView(1, albedo.AsValue().View),
                BindGroupEntry.Sampler(2, albedoSampler.AsValue()),
                BindGroupEntry.TextureView(3, metallicRoughness.AsValue().View),
                BindGroupEntry.Sampler(4, metallicRoughnessSampler.AsValue()),
                BindGroupEntry.TextureView(5, normal.AsValue().View),
                BindGroupEntry.Sampler(6, normalSampler.AsValue()),
            ],
        });

        var shadowBindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = passes[0].Pipeline.Layout.BindGroupLayouts[0],
            Entries =
            [
                BindGroupEntry.Buffer(0, uniformBuffer),
                BindGroupEntry.Buffer(1, lights.DirectionalLight.LightMatricesBuffer),
            ],
        });

        var material = new PbrMaterial(
            shader,
            uniformBuffer,
            albedo,
            albedoSampler,
            metallicRoughness,
            metallicRoughnessSampler,
            normal,
            normalSampler,
            bindGroup0,
            shadowBindGroup0
        );
        return CreateOwn(material);
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
