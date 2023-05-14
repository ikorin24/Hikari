#nullable enable
using System;

namespace Elffy;

public sealed class PbrMaterial : Material<PbrMaterial, PbrShader>
{
    private readonly MaybeOwn<Sampler> _sampler;
    private readonly MaybeOwn<Texture> _albedo;
    private readonly MaybeOwn<Texture> _metallicRoughness;
    private readonly MaybeOwn<Texture> _normal;
    private readonly Own<Buffer> _modelUniform;
    private readonly Own<BindGroup> _bindGroup0;
    private readonly BindGroup _bindGroup1;

    public Texture Albedo => _albedo.AsValue();
    public Texture MetallicRoughness => _metallicRoughness.AsValue();

    internal BufferSlice<byte> ModelUniform => _modelUniform.AsValue().Slice();

    internal (BindGroup BindGroup0, BindGroup BindGroup1) GetBindGroups()
    {
        return (_bindGroup0.AsValue(), _bindGroup1);
    }

    private PbrMaterial(
        PbrShader shader,
        Own<Buffer> modelUniform,
        MaybeOwn<Sampler> sampler,
        MaybeOwn<Texture> albedo,
        MaybeOwn<Texture> metallicRoughness,
        MaybeOwn<Texture> normal,
        Own<BindGroup> bindGroup)
        : base(shader)
    {
        _modelUniform = modelUniform;
        _sampler = sampler;
        _albedo = albedo;
        _metallicRoughness = metallicRoughness;
        _normal = normal;
        _bindGroup0 = bindGroup;
        _bindGroup1 = Screen.Camera.CameraDataBindGroup;
    }

    public override void Validate()
    {
        base.Validate();
        _sampler.Validate();
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
        }
    }

    public static Own<PbrMaterial> Create(
        PbrShader shader,
        MaybeOwn<Sampler> sampler,
        MaybeOwn<Texture> albedo,
        MaybeOwn<Texture> metallicRoughness,
        MaybeOwn<Texture> normal)
    {
        ArgumentNullException.ThrowIfNull(shader);
        sampler.ThrowArgumentExceptionIfNone();
        albedo.ThrowArgumentExceptionIfNone();
        metallicRoughness.ThrowArgumentExceptionIfNone();
        normal.ThrowArgumentExceptionIfNone();

        var screen = shader.Screen;
        var modelUniform = Buffer.Create(screen, default(Matrix4), BufferUsages.Uniform | BufferUsages.CopyDst | BufferUsages.Storage);
        var desc = new BindGroupDescriptor
        {
            Layout = shader.BindGroupLayout0,
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Buffer(0, modelUniform.AsValue()),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.TextureView(2, albedo.AsValue().View),
                BindGroupEntry.TextureView(3, metallicRoughness.AsValue().View),
                BindGroupEntry.TextureView(4, normal.AsValue().View),
            },
        };
        var bindGroup = BindGroup.Create(screen, in desc);
        var material = new PbrMaterial(shader, modelUniform, sampler, albedo, metallicRoughness, normal, bindGroup);
        return CreateOwn(material);
    }

    internal void WriteModelUniform(in Matrix4 model)
    {
        _modelUniform.AsValue().Write(0, model);
    }
}
