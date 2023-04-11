#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class PbrMaterial : Material<PbrMaterial, PbrShader>
{
    private readonly Own<Sampler> _sampler;
    private readonly Own<Texture> _albedo;
    private readonly Own<Texture> _metallicRoughness;
    private readonly Own<Texture> _normal;
    private readonly Own<Uniform<UniformValue>> _uniform;

    public TextureView Albedo => _albedo.AsValue().View;
    public TextureView MetallicRoughness => _metallicRoughness.AsValue().View;

    private PbrMaterial(
        PbrShader shader,
        Own<Uniform<UniformValue>> uniform,
        Own<Sampler> sampler,
        Own<Texture> albedo,
        Own<Texture> metallicRoughness,
        Own<Texture> normal,
        Own<BindGroup> bindGroup)
        : base(shader, new[] { bindGroup })
    {
        _uniform = uniform;
        _sampler = sampler;
        _albedo = albedo;
        _metallicRoughness = metallicRoughness;
        _normal = normal;
    }

    protected override void Release(bool manualRelease)
    {
        base.Release(manualRelease);
        if(manualRelease) {
            _uniform.Dispose();
            _sampler.Dispose();
            _albedo.Dispose();
            _metallicRoughness.Dispose();
            _normal.Dispose();
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
        var uniform = Uniform<UniformValue>.Create(screen, default);
        var desc = new BindGroupDescriptor
        {
            Layout = shader.GetBindGroupLayout(0),
            Entries = new BindGroupEntry[]
            {
                BindGroupEntry.Buffer(0, uniform.AsValue().Buffer),
                BindGroupEntry.Sampler(1, sampler.AsValue()),
                BindGroupEntry.TextureView(2, albedo.AsValue().View),
                BindGroupEntry.TextureView(3, metallicRoughness.AsValue().View),
                BindGroupEntry.TextureView(4, normal.AsValue().View),
            },
        };
        var bindGroup = BindGroup.Create(screen, in desc);
        var material = new PbrMaterial(shader, uniform, sampler, albedo, metallicRoughness, normal, bindGroup);
        return CreateOwn(material);
    }

    public void SetUniform(in UniformValue value) => _uniform.AsValue().Set(in value);

    [StructLayout(LayoutKind.Explicit)]
    public struct UniformValue
    {
        [FieldOffset(0)]    // 0 ~ 63   (size: 64)
        private Matrix4 _model;
        [FieldOffset(64)]   // 64 ~ 127 (size: 64)
        private Matrix4 _view;
        [FieldOffset(128)]  // 127 ~ 191 (size: 64)
        private Matrix4 _projection;

        public UniformValue(Matrix4 model, Matrix4 view, Matrix4 projection)
        {
            _model = model;
            _view = view;
            _projection = projection;
        }
    }
}
