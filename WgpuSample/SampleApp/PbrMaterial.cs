#nullable enable
using System;

namespace Elffy;

public sealed class PbrMaterial : Material<PbrMaterial, PbrShader>
{
    private readonly Own<Sampler> _sampler;
    private readonly Own<Texture> _albedo;
    private readonly Own<Texture> _metallicRoughness;
    private readonly Own<Texture> _normal;
    private readonly Own<Buffer> _modelUniform;
    private readonly Own<BindGroup> _bindGroup0;

    public Texture Albedo => _albedo.AsValue();
    public Texture MetallicRoughness => _metallicRoughness.AsValue();

    internal BufferSlice<byte> ModelUniform => _modelUniform.AsValue().Slice();

    public BindGroup BindGroup0 => _bindGroup0.AsValue();
    public BindGroup BindGroup1 => Screen.Camera.CameraDataBindGroup;

    private PbrMaterial(
        PbrShader shader,
        Own<Buffer> modelUniform,
        Own<Sampler> sampler,
        Own<Texture> albedo,
        Own<Texture> metallicRoughness,
        Own<Texture> normal,
        Own<BindGroup> bindGroup)
        : base(shader)
    {
        _modelUniform = modelUniform;
        _sampler = sampler;
        _albedo = albedo;
        _metallicRoughness = metallicRoughness;
        _normal = normal;
        _bindGroup0 = bindGroup;
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
        Own<Sampler> sampler,
        Own<Texture> albedo,
        Own<Texture> metallicRoughness,
        Own<Texture> normal)
    {
        ArgumentNullException.ThrowIfNull(shader);
        sampler.ThrowArgumentExceptionIfNone();
        albedo.ThrowArgumentExceptionIfNone();
        metallicRoughness.ThrowArgumentExceptionIfNone();
        normal.ThrowArgumentExceptionIfNone();

        var screen = shader.Screen;
        var modelUniform = Buffer.Create(screen, default(Matrix4), BufferUsages.Uniform | BufferUsages.CopyDst);
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
