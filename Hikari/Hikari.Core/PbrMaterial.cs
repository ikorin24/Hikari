#nullable enable
using System;

namespace Hikari;

public sealed class PbrMaterial : Material<PbrMaterial, PbrShader, PbrLayer>
{
    private readonly Own<Sampler> _sampler;
    private readonly MaybeOwn<Texture> _albedo;
    private readonly MaybeOwn<Texture> _metallicRoughness;
    private readonly MaybeOwn<Texture> _normal;
    private readonly Own<Buffer> _modelUniform;
    private readonly Own<BindGroup> _bindGroup0;
    private readonly BindGroup _bindGroup1;
    private readonly Own<BindGroup> _shadowBindGroup0;

    public Texture Albedo => _albedo.AsValue();
    public Texture MetallicRoughness => _metallicRoughness.AsValue();
    public Texture Normal => _normal.AsValue();

    internal BufferSlice ModelUniform => _modelUniform.AsValue().Slice();

    internal BindGroup BindGroup0 => _bindGroup0.AsValue();
    internal BindGroup BindGroup1 => _bindGroup1;

    internal BindGroup ShadowBindGroup0 => _shadowBindGroup0.AsValue();

    private PbrMaterial(
        PbrShader shader,
        Own<Buffer> modelUniform,
        Own<Sampler> sampler,
        MaybeOwn<Texture> albedo,
        MaybeOwn<Texture> metallicRoughness,
        MaybeOwn<Texture> normal,
        Own<BindGroup> bindGroup0,
        Own<BindGroup> shadowBindGroup0)
        : base(shader)
    {
        _modelUniform = modelUniform;
        _sampler = sampler;
        _albedo = albedo;
        _metallicRoughness = metallicRoughness;
        _normal = normal;
        _bindGroup0 = bindGroup0;
        _bindGroup1 = Screen.Camera.CameraDataBindGroup;
        _shadowBindGroup0 = shadowBindGroup0;
    }

    public override void Validate()
    {
        base.Validate();
        _albedo.Validate();
        _metallicRoughness.Validate();
        _normal.Validate();
        _bindGroup1.Validate();
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _modelUniform.Dispose();
            _sampler.Dispose();
            _albedo.Dispose();
            _metallicRoughness.Dispose();
            _normal.Dispose();
            _bindGroup0.Dispose();
            _shadowBindGroup0.Dispose();
        }
    }

    public static Own<PbrMaterial> Create(
        PbrShader shader,
        MaybeOwn<Texture> albedo,
        MaybeOwn<Texture> metallicRoughness,
        MaybeOwn<Texture> normal)
    {
        ArgumentNullException.ThrowIfNull(shader);
        albedo.ThrowArgumentExceptionIfNone();
        metallicRoughness.ThrowArgumentExceptionIfNone();
        normal.ThrowArgumentExceptionIfNone();

        var screen = shader.Screen;
        var lights = screen.Lights;
        var modelUniform = Buffer.Create(screen, (usize)Matrix4.SizeInBytes, BufferUsages.Uniform | BufferUsages.CopyDst | BufferUsages.Storage);
        var sampler = Sampler.Create(screen, new()
        {
            AddressModeU = AddressMode.ClampToEdge,
            AddressModeV = AddressMode.ClampToEdge,
            AddressModeW = AddressMode.ClampToEdge,
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            MipmapFilter = FilterMode.Linear,
        });
        var bindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.Operation.BindGroupLayout0,
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Buffer(0, modelUniform.AsValue()),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.TextureView(2, albedo.AsValue().View),
                BindGroupEntry.TextureView(3, metallicRoughness.AsValue().View),
                BindGroupEntry.TextureView(4, normal.AsValue().View),
            },
        });

        var shadowBindGroup0 = BindGroup.Create(screen, new()
        {
            Layout = shader.Operation.ShadowBindGroupLayout0,
            Entries = new[]
            {
                BindGroupEntry.Buffer(0, modelUniform.AsValue()),
                BindGroupEntry.Buffer(1, lights.DirectionalLight.LightMatricesBuffer),
            },
        });

        var material = new PbrMaterial(
            shader,
            modelUniform,
            sampler,
            albedo,
            metallicRoughness,
            normal,
            bindGroup0,
            shadowBindGroup0
        );
        return CreateOwn(material);
    }

    internal void WriteModelUniform(in Matrix4 model)
    {
        _modelUniform.AsValue().WriteData(0, model);
    }
}
